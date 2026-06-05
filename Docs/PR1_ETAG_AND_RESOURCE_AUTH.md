# PR 1 — ETag, Conditional Requests, and Resource-Based Authorization

> *Showcase: how a real BuberDinner endpoint composes the framework's HTTP-precondition
> machinery, scalar-VO route constraints, and resource-based authorization into a single
> chain — no per-controller plumbing, no manual `If-Match` parsing, no scattered "is this
> the owner?" checks.*

---

## What this PR adds (at a glance)

| Capability | Before | After | Cookbook |
|---|---|---|---|
| GET a menu | n/a — repository threw `NotImplementedException` | `GET /hosts/{hostId:HostId}/menus/{menuId:MenuId}` returns 200 + strong ETag | Recipe 6 |
| Conditional GET | n/a | `If-None-Match` → 304 Not Modified | Recipe 6 |
| Update a menu | n/a | `PUT` with `If-Match` → 200 + bumped ETag | Recipe 23 |
| Missing precondition | n/a | `PUT` without `If-Match` → 428 Precondition Required | Recipe 23 |
| Lost-update protection | n/a | `PUT` with stale `If-Match` → 412 Precondition Failed | Recipe 23 |
| Resource ownership | not checked — any user could mutate any host's menu | `IAuthorizeResource<Host>` → 403 if `Actor.Id != host.OwnerId` | Recipe 7 |
| Typed route params | string GUID round-trip on every controller | `[Route("hosts/{hostId:HostId}/...")]` — scalar VO bound directly | n/a (Trellis.Asp.Routing) |
| Host aggregate | did not exist | `Host` aggregate with `OwnerId(UserId)` + `DisplayName(Name)` | n/a |

Everything observed at the wire. Six precondition/auth scenarios are covered by the
new `MenuEtagAndAuthTests` class + five new `.http` files under `Requests/Hosts` and
`Requests/Menus`.

## The wire dump (the part you can show in a demo)

```http
# 1. POST /hosts → 201, OwnerId comes from the JWT `sub` claim.
HTTP/1.1 201 Created
Location: /hosts/019e955c-89a3-7f92-a289-35f8823bc135

# 2. GET …/menus/{id} → strong ETag.
HTTP/1.1 200 OK
ETag: "e1234e51a87e4710855993de1842d29f"

# 3. GET with matching If-None-Match → 304, no body.
HTTP/1.1 304 Not Modified
ETag: "e1234e51a87e4710855993de1842d29f"

# 4. PUT without If-Match → 428, RFC 6585.
HTTP/1.1 428 Precondition Required

# 5. PUT with stale If-Match → 412, RFC 9110 §15.5.13.
HTTP/1.1 412 Precondition Failed

# 6. PUT with valid If-Match → 200, ETag bumped.
HTTP/1.1 200 OK
ETag: "0ada920e5adc4c7ca9b843350fe5f093"

# 7. PUT by a different user → 403 Forbidden.
HTTP/1.1 403 Forbidden
Content-Type: application/problem+json
{ "status": 403, "code": "menus.owner", "kind": "forbidden", ... }
```

## How small the controller is

```csharp
[HttpGet("{menuId:MenuId}")]
public async ValueTask<ActionResult<MenuResponse>> GetMenu(HostId hostId, MenuId menuId) =>
    await _sender.Send(new GetMenuQuery(menuId))
        .ToHttpResponseAsync(
            body: menu => menu.Adapt<MenuResponse>(),
            configure: opts => opts
                .WithETag(menu => EntityTagValue.Strong(menu.ETag))
                .EvaluatePreconditions())
        .AsActionResultAsync<MenuResponse>();
```

That's the entire GET. No `if (Request.Headers.TryGetValue("If-None-Match", …))`,
no `Response.StatusCode = 304`, no `HttpContext.Response.Headers["ETag"] = …`.
`WithETag(...).EvaluatePreconditions()` does the precondition algorithm; the body lambda
only runs if the precondition lets the response through.

The PUT is just as small, with the extra precondition check moved inside the handler
chain (Recipe 23) so the framework can short-circuit between load and mutate:

```csharp
[HttpPut("{menuId:MenuId}")]
public async ValueTask<ActionResult<MenuResponse>> UpdateMenu(
    HostId hostId, MenuId menuId, [FromBody] UpdateMenuRequest request, CancellationToken ct) =>
    await request.ToUpdateMenuCommand(hostId, menuId, ETagHelper.ParseIfMatch(HttpContext.Request))
        .BindAsync(command => _sender.Send(command, ct))
        .ToHttpResponseAsync(
            body: menu => menu.Adapt<MenuResponse>(),
            configure: opts => opts.WithETag(menu => EntityTagValue.Strong(menu.ETag)))
        .AsActionResultAsync<MenuResponse>();
```

And the handler chain that drives it:

```csharp
return await LoadMenuAsync(request, ct)
    .RequireETagAsync(request.IfMatch)
    .BindAsync(menu => menu.Update(request.Name, request.Description))
    .TapAsync(_repo.Update);
```

Three operators: `RequireETag` (412 if stale, 428 if missing), `Bind` (run the domain
mutation only if the precondition passed), `Tap` (persist only if the mutation succeeded).

## Resource-based authorization — what we plugged in

The `UpdateMenuCommand` declares its authorization shape directly on the command type:

```csharp
public sealed record UpdateMenuCommand(...)
    : ICommand<Result<Menu>>,
      IAuthorizeResource<Host>,           // gate this command by a Host
      IIdentifyResource<Host, HostId>     // identify the Host from `command.HostId`
{
    public bool IsAuthorized(Host resource, Actor actor) =>
        resource.OwnerId.Value == actor.Id;

    public string AuthorizationCode => "menus.owner"; // shows up in ProblemDetails.code
}
```

The Mediator pipeline (`AddResourceAuthorization`) takes care of:
1. Loading the `Host` resource (via the `HostResourceLoader` we registered).
2. Resolving the current `Actor` from the JWT `sub` claim
   (`AddClaimsActorProvider(opts => opts.ActorIdClaim = "sub")`).
3. Calling `IsAuthorized` and returning `Error.AuthorizationFailed("menus.owner")` if it returns false.
4. Translating that error into `403 Forbidden` with `application/problem+json` at the boundary.

If we plug a second `IAuthorizeResource<TParent>` onto the same command, the framework runs them all. There is no controller-level `[Authorize(Policy = ...)]` to maintain and no per-handler "did I forget the ownership check?" risk.

## Scalar VO route constraints — the un-glamorous big win

Before this PR, every controller signature took `string hostId, string menuId` and
the first line of every action was a `Guid.TryParse` or `HostId.TryCreate`. We were
paying for the route binding twice: once when ASP.NET parsed the URL, again when we
hand-converted the string to a typed value.

```csharp
// before
public async Task<ActionResult> CreateMenu(CreateMenuRequest request, string hostId) =>
    await request.ToCreateMenuCommand(hostId)   // does TryParse internally
        .BindAsync(...)
        ...

// after
public async Task<ActionResult> CreateMenu(CreateMenuRequest request, HostId hostId) =>
    await request.ToCreateMenuCommand(hostId.Value.ToString())  // already typed
        .BindAsync(...)
        ...
```

Registered with two lines in `Api/src/DependencyInjection.cs`:

```csharp
services.AddTrellisRouteConstraint<HostId>(nameof(HostId));
services.AddTrellisRouteConstraint<MenuId>(nameof(MenuId));
```

And consumed in the route template:

```csharp
[Route("hosts/{hostId:HostId}/menus/{menuId:MenuId}")]
```

If the URL contains a non-GUID, ASP.NET returns 404 *before* the action ever runs — no
hand-rolled validator, no Result threading for parse failures. (One pre-existing test
needed a tweak because `Guid.ToString()` normalises to lowercase; we now compare GUIDs
case-insensitively in the Location-header assertion, per RFC 4122 §3.)

## Annoyance worth filing back (reg-005)

`Aggregate<TId>.ETag` has an `internal` setter. The intended consumer is
`Trellis.EntityFrameworkCore`, which sets the property after a successful round-trip to
the database. We're using an in-memory repository here (just like the EF integration tests
do) so we need to mimic that hand-off — and the only path open to us is reflection. The
workaround lives in `Infrastructure/src/Persistence/Memory/AggregateETagWriter.cs`:
walks the runtime type hierarchy looking for `ETag` (declared on the concrete aggregate
or any base type) and falls back to the `<ETag>k__BackingField` compiler-generated field.
Caches the writer per type in a `ConcurrentDictionary` so the reflection cost is paid once.

This file should go away the moment the framework exposes a `protected void
SetETag(string)` hook, or accepts the ETag in `Aggregate<TId>`'s constructor. We've left
a `// TODO(framework reg-005)` marker in the file.

## Files that show off the most

| File | Why look here |
|---|---|
| `Api/src/2022-12-21/Controllers/MenusController.cs` | 30 lines of handler glue for GET+PUT, including the precondition algorithm and ETag emission |
| `Application/src/Menus/Commands/UpdateMenuCommand.cs` | `IAuthorizeResource<Host>` + `IIdentifyResource<Host, HostId>` declared on the command itself |
| `Application/src/Menus/Commands/UpdateMenuCommandHandler.cs` | Three-operator chain: load → `RequireETag` → mutate → persist |
| `Api/src/DependencyInjection.cs` | All wiring for route constraints, claims actor provider, resource auth in <10 lines |
| `Api/tests/2022-12-21/MenuEtagAndAuthTests.cs` | Six precondition / auth scenarios at the wire |
| `Requests/Menus/UpdateMenu-IfMatchMismatch.http` | All three PUT failure modes side-by-side |
