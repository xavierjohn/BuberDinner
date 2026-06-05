# PR 3 — Cursor pagination on per-host list endpoints

> *Third in the 5-PR showcase arc. PR 1 (#27) added ETag + resource auth; PR 2 (#28) added
> the Dinner state machine + domain events. PR 3 plugs in Trellis's cursor-pagination
> primitives — `Page<T>` / `PageSize` / `CursorCodec` / `PageBuilder.FromOverFetch` —
> across both new list endpoints, sharing one wire envelope between them.*

---

## What this PR adds (at a glance)

| Capability | Where | Cookbook |
|---|---|---|
| `GET /hosts/{hostId}/dinners?cursor=...&limit=...` | `DinnersController.ListDinners` | Recipe 3 |
| `GET /hosts/{hostId}/menus?cursor=...&limit=...` (new endpoint) | `MenusController.ListMenus` | Recipe 3 |
| Repository over-fetch primitive: `GetPageForHost(host, pageSize, afterId)` | `IDinnerRepository` + `IMenuRepository` + InMemory impls | — |
| `ListDinnersForHostQuery(HostId, Cursor?, int?) : IRequest<Result<Page<Dinner>>>` | `Application/src/Dinners/Queries/` | Recipe 3 |
| `ListMenusForHostQuery(HostId, Cursor?, int?) : IRequest<Result<Page<Menu>>>` | `Application/src/Menus/Queries/` | Recipe 3 |
| Framework `PagedResponse<T>` envelope + RFC 8288 `Link: <…>; rel="next"` header | `Trellis.Asp.ToHttpResponseAsync<T, TBody>(nextUrlBuilder, body, …)` | Asp §86 |
| Malformed cursor → 422 with `cursor.malformed` | `CursorCodec.TryDecode<Guid>` returns `Result.Fail` (ROP, no throw) | Recipe 3 §352 |

## The handler in 15 lines

```csharp
public ValueTask<Result<Page<Dinner>>> Handle(
    ListDinnersForHostQuery request, CancellationToken cancellationToken)
{
    var pageSize = PageSize.FromRequested(request.Limit);

    System.Guid? afterId = null;
    if (request.Cursor is { } cursor)
    {
        var decoded = CursorCodec.TryDecode<System.Guid>(cursor, fieldName: "cursor");
        if (!decoded.TryGetValue(out var id, out var cursorError))
            return ValueTask.FromResult(Result.Fail<Page<Dinner>>(cursorError));
        afterId = id;
    }

    var overFetched = _repo.GetPageForHost(request.HostId, pageSize, afterId);
    var page = PageBuilder.FromOverFetch(overFetched, pageSize, d => d.Id.Value);
    return ValueTask.FromResult(Result.Ok(page));
}
```

Three Trellis pieces do all the work:

| Primitive | What it owns |
|---|---|
| `PageSize.FromRequested(int?)` | Lenient parse: `null`/`<=0` → `PageSize.Default (50)`; preserves the caller's value as `RequestedLimit` and clamps `Applied` to `PageSize.Max (100)` so `WasCapped` round-trips. |
| `CursorCodec.TryDecode<Guid>(cursor, "cursor")` | Base64 → typed Guid via railway. Bad input → `Error.InvalidInput(cursor.malformed)` → HTTP 422. Hand-rolling `Guid.Parse(cursor)` would throw and escape as a 500. |
| `PageBuilder.FromOverFetch(items, pageSize, idSelector)` | Detects "is there a next page?" by counting the over-fetch (Applied+1) instead of issuing a separate COUNT. Returns a `Page<T>` with the right `Next` cursor and `DeliveredCount`. |

## The controller in 12 lines

```csharp
[HttpGet]
public async ValueTask<ActionResult<PagedResponse<DinnerResponse>>> ListDinners(
    HostId hostId,
    [FromQuery(Name = "cursor")] string? cursor,
    [FromQuery(Name = "limit")]  int? limit,
    CancellationToken cancellationToken)
{
    var basePath = $"/hosts/{hostId.Value}/dinners";
    var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "2022-10-01";

    return await _sender.Send(
            new ListDinnersForHostQuery(hostId,
                cursor is { Length: > 0 } token ? new Cursor(token) : (Cursor?)null,
                limit),
            cancellationToken)
        .ToHttpResponseAsync(
            nextUrlBuilder: (nextCursor, appliedLimit) =>
                $"{basePath}?cursor={Uri.EscapeDataString(nextCursor.Token)}&limit={appliedLimit}&api-version={apiVersion}",
            body: dinner => dinner.Adapt<DinnerResponse>())
        .AsActionResultAsync<PagedResponse<DinnerResponse>>();
}
```

`Trellis.Asp.ToHttpResponseAsync<T, TBody>(nextUrlBuilder, body, ...)` is the
**paginated** overload — distinct from the plain `Result<T>` overload PR 2 uses. It:

1. Projects each item through `body` (Mapster `Adapt<DinnerResponse>()`).
2. Calls `nextUrlBuilder(cursor, appliedLimit)` to build absolute href URLs.
3. Wraps everything in `PagedResponse<TResponse>(items, next, previous, requestedLimit, appliedLimit, deliveredCount, wasCapped)`.
4. Emits the RFC 8288 `Link: <href>; rel="next"` header when `Page.Next` is non-null.

`ListMenusForHostQuery` + `MenusController.ListMenus` mirror the shape exactly so the wire
contract is identical across both endpoints — once a client knows how to page dinners, it
knows how to page menus.

## The wire dump (verified end-to-end)

```http
# Page 1 — no cursor.
GET /hosts/{H}/dinners?api-version=2022-10-01&limit=5

HTTP/1.1 200 OK
Link: </hosts/{H}/dinners?cursor=MDE5ZTk2NjctMTQxZi03ZmQ1...&limit=5&api-version=2022-10-01>; rel="next"
Content-Type: application/json
{
  "items":  [ ... 5 ... ],
  "next":   { "cursor": "MDE5ZTk2NjctMTQxZi03ZmQ1...", "href": "/hosts/{H}/dinners?cursor=...&limit=5&api-version=2022-10-01" },
  "previous": null,
  "requestedLimit": 5, "appliedLimit": 5, "deliveredCount": 5, "wasCapped": false
}

# Page 2 — follow next.cursor; same shape; next is null on last page.
GET /hosts/{H}/dinners?api-version=2022-10-01&limit=5&cursor=MDE5ZTk2NjctMTQxZi03ZmQ1...
HTTP/1.1 200 OK
{ "items": [ ... ], "next": null, "previous": null, ... }

# Oversized limit — clamped to PageSize.Max.
GET /hosts/{H}/dinners?api-version=2022-10-01&limit=500
HTTP/1.1 200 OK
{ "requestedLimit": 500, "appliedLimit": 100, "wasCapped": true, ... }

# Malformed cursor — 422, NOT 500.
GET /hosts/{H}/dinners?api-version=2022-10-01&cursor=NOT-A-VALID-CURSOR-!!!
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json
{ "status":422, "code":"invalid-input",
  "errors":{"cursor":["Cursor is not a valid URL-safe base64 token."]} }
```

## Why over-fetch + `PageBuilder.FromOverFetch` instead of "load all + slice"

The naive approach for an in-memory store would be `s_dinners.Where(...).Skip(N).Take(N)`.
That doesn't translate to real persistence — every page would do a full scan and the
"is there a next page?" check would need a `COUNT(*)` or a separate query. The over-fetch
pattern (`Take(Applied + 1)`) detects "more rows exist" from the result itself:

- Over-fetched **<= Applied** → this is the last page; `Next = null`.
- Over-fetched **= Applied + 1** → trim to `Applied` items; `Next = Encode(lastItem.Id)`.

This is the same shape EF Core's `ToPageAsync` uses internally per Cookbook Recipe 3. By
exposing the over-fetched list on `IDinnerRepository.GetPageForHost(...)`, the in-memory
showcase impl produces a `Page<T>` indistinguishable from one an EF Core impl would
produce — when PR 4+ adds Cosmos repositories, only the Infrastructure layer changes.

## Per-host scope is enforced at the repo, not in the handler

```csharp
public IReadOnlyList<Dinner> GetPageForHost(HostId hostId, PageSize pageSize, Guid? afterId)
{
    lock (s_lock)
    {
        IEnumerable<Dinner> source = s_dinners
            .Where(d => d.HostId == hostId)            // <-- filter BEFORE the cursor seek
            .OrderBy(d => d.Id.Value);
        if (afterId is { } cursorId)
            source = source.Where(d => d.Id.Value.CompareTo(cursorId) > 0);
        return source.Take(pageSize.Applied + 1).ToList();
    }
}
```

The host filter is applied **before** the cursor seek so a caller cannot hand a stolen
cursor from another host's page and slip it into their own list query — the seek is scoped
to rows the caller already owns. Locked in by
`Pagination_filters_by_host_so_one_hosts_cursor_does_not_leak_anothers_rows`.

## Why V7 GUID ordering "just works"

V7 GUIDs (`MenuId.NewUniqueV7()`, `DinnerId.NewUniqueV7()`) embed a millisecond timestamp
in their first 6 bytes. `Guid.CompareTo` compares the first 4 bytes as a `uint32` and the
next 2 as a `uint16` (each in the canonical struct layout used by the string form), so V7
IDs sort **chronologically** under `OrderBy(d => d.Id.Value)` for the timestamp portion.
Within the same millisecond, the random tail provides a stable secondary key. Net effect:
the over-fetch pattern + Id-ordering produces pages in creation order without an explicit
`CreatedAt` column.

## Build, test, wire-verify

- **0 warnings / 0 errors**
- **Domain 31/31** (unchanged)
- **Application 1/1** (unchanged)
- **Api 51/51** (+7 new pagination tests, +1 existing `List_dinners_returns_only_host_owned_dinners` updated for the new envelope)

Wire scenarios covered by `PaginationTests`:
- Multi-page traversal, every id unique, ascending order, last page `Next = null`
- RFC 8288 `Link: <…>; rel="next"` header on intermediate pages
- `?limit=500` → server clamps to 100, `wasCapped=true`
- Malformed cursor → 422 with `cursor.malformed`
- Omitted `?limit` → `RequestedLimit = AppliedLimit = 50` (`PageSize.Default`)
- Same shape across `ListDinners` and `ListMenus`
- Cross-host scoping at the repo (foreign cursors can't leak)

## Files that show off the most

| File | Why look here |
|---|---|
| `Application/src/Dinners/Queries/ListDinnersForHostQuery.cs` | The whole pagination handler — three Trellis primitives, no plumbing |
| `Application/src/Menus/Queries/ListMenusForHostQuery.cs` | Identical structural pattern across two aggregates — proof the abstraction generalises |
| `Api/src/2022-12-21/Controllers/DinnersController.cs` (ListDinners action) | `ToHttpResponseAsync<T, TBody>(nextUrlBuilder, body, …)` — one call emits envelope + Link header |
| `Application/src/Abstractions/Persistence/IDinnerRepository.cs` | The over-fetch contract that any future EF/Cosmos impl plugs into |
| `Infrastructure/src/Persistence/Memory/DinnerInMemoryRepository.cs` | Host-filter-first, then seek, then `Take(Applied + 1)` |
| `Api/tests/2022-12-21/PaginationTests.cs` | Seven wire scenarios including cross-host scoping |
| `Requests/Dinners/ListDinners-WithCursor.http` | Shows the continuation workflow |

## What's NOT in this PR (deferred)

- **`previous` cursor** — always `null`. Trellis ships forward-only seek today; reverse-seek
  comes when the framework does.
- **Total count** — deliberately absent from the envelope. Counting requires a separate
  query and most paged APIs are better off without it (consumers that need totals can
  page until `next = null`).
- **ETag on list responses** — `WithETag(...)` on a paginated response is technically
  supported by `HttpResponseOptionsBuilder<Page<T>>` but would only be meaningful with
  a stable hash over the items + cursors. Deferred.
- **`Page<T>` projection in handler vs controller** — the handler currently returns
  `Result<Page<Dinner>>` (aggregate) and the controller's `body:` selector projects to
  `DinnerResponse`. The cookbook Recipe 3 example projects in the handler via
  `page.Map(...)`; both are valid — chose controller-side here to keep BuberDinner's
  Mapster-at-the-boundary convention.
- **Pagination on `GET /authentication` or `GET /hosts`** — no list endpoints exist for
  those today; their list shapes will be designed alongside the use case in later PRs.

## Roadmap remaining

- **PR 4**: Bookings + idempotency (`UseTrellisIdempotency`) + multi-aggregate (Recipe 22)
- **PR 5**: Reviews + `AddTrellisFluentValidation` at command boundary + `Trellis.Testing` + ServiceDefaults
