# PR 4 — Reservations + IETF Idempotency-Key + multi-aggregate fail-loud

> *Fourth in the 5-PR showcase arc. Adds the Reservation aggregate, opts the create endpoint
> into the IETF `Idempotency-Key` middleware (Cookbook Recipe 29) so network retries can't
> double-book, and demonstrates the Recipe 22 fail-loud-on-missing-related pattern via the
> Dinner ↔ Reservation relationship.*

## What this PR adds (at a glance)

| Capability | Where | Cookbook |
|---|---|---|
| `Reservation` aggregate with one-transition `LazyStateMachine<,>` | `Domain/src/Reservation/Entities/Reservation.cs` | Recipe 9 |
| `ReservationCreated` / `ReservationCancelled` events (`OccurredAt` only) | `Domain/src/Reservation/Events/` | Recipe 17 |
| `POST /reservations` opted-in to IETF `Idempotency-Key` via `[Idempotent]` | `ReservationsController.CreateReservation` | Recipe 29 |
| Recipe 22 fail-loud: `CreateReservationHandler` loads Dinner first; missing → 404 | `CreateReservationCommandHandler` | Recipe 22 |
| `POST /reservations/{id}/cancel` with NotFound-leak-shielded ownership check | `CancelReservationCommandHandler` | — |
| `GET /reservations/{id}` (owner-only) | `ReservationsController.GetReservation` | — |
| `GET /reservations/mine` (paginated, guest view) | `ListMyReservationsQuery` + handler | Recipe 3 |
| `GET /hosts/{hostId}/dinners/{dinnerId}/reservations` (paginated, host view, `IAuthorizeResource<Host>`) | `ListReservationsForDinnerQuery` + handler | Recipes 3 + 7 |
| `services.AddTrellisIdempotency(...) + AddInMemoryIdempotencyStore() + app.UseTrellisIdempotency()` | `Api/src/DependencyInjection.cs`, `Program.cs` | Recipe 29 |

## The idempotency contract (wire-verified)

```http
# 1. First POST with an Idempotency-Key → 201 + Location + ETag.
POST /reservations HTTP/1.1
Idempotency-Key: 12345678-1234-1234-1234-1234567890ab
Content-Type: application/json
{ "dinnerId": "...", "guestCount": 2 }

HTTP/1.1 201 Created
ETag: "0c3c7a80336049ae992bba0456801b05"
Location: /reservations/019e9690-10f4-7584-8ab7-5af569f6983a
{ "id": "019e9690-10f4-7584-8ab7-5af569f6983a", "status": "Reserved", ... }

# 2. Retry — same key, same body → 201 + IDENTICAL body + Idempotent-Replayed: true.
POST /reservations HTTP/1.1
Idempotency-Key: 12345678-1234-1234-1234-1234567890ab
{ "dinnerId": "...", "guestCount": 2 }

HTTP/1.1 201 Created
ETag: "0c3c7a80336049ae992bba0456801b05"   ← same
Location: /reservations/019e9690-10f4-7584-8ab7-5af569f6983a   ← same id
Idempotent-Replayed: true                                       ← framework's marker

# 3. Same key, DIFFERENT body → 422 fingerprint mismatch.
POST /reservations HTTP/1.1
Idempotency-Key: 12345678-1234-1234-1234-1234567890ab
{ "dinnerId": "...", "guestCount": 5 }       ← mutated

HTTP/1.1 422 Unprocessable Entity

# 4. Missing Idempotency-Key on an [Idempotent] endpoint → 400.
POST /reservations HTTP/1.1
{ "dinnerId": "...", "guestCount": 1 }

HTTP/1.1 400 Bad Request
{ "status":400, "code":"idempotency.key_required",
  "detail":"This endpoint requires the Idempotency-Key header." }
```

The framework does all the work:

- Reads the configured header (default `Idempotency-Key`, RFC 8941 `sf-string` subset).
- Buffers the request body up to `MaxRequestBodyBytes`, computes SHA-256 over
  `(method, path, normalized headers, body)`.
- Resolves the scope through `DefaultIdempotencyScopeResolver` (per-actor, via the JWT
  `sub` claim picked up by `ClaimsActorProvider`) so two different users with the same
  Idempotency-Key partition the store separately.
- On a successful response, snapshots `(status, headers, body)` against an
  `InMemoryIdempotencyStore`.
- On the next matching call within `Ttl` (24h here), replays the snapshot byte-for-byte
  with an extra `Idempotent-Replayed: true` header.

Per-actor scoping is critical — mounting `UseTrellisIdempotency()` BEFORE `UseAuthentication()`
would let every authenticated request fall back to the shared `anonymous` scope, where
different users could collide on the same key.

## Recipe 22 fail-loud in action

The Reservation handler:

```csharp
public async ValueTask<Result<Reservation>> Handle(
    CreateReservationCommand request, CancellationToken cancellationToken)
{
    // Recipe 22 — load the related aggregate FIRST. If missing, fail-loud with NotFound
    // so the post-condition "every reservation references a real dinner" holds.
    var dinner = await _dinnerRepository.FindById(request.DinnerId.Value.ToString(), cancellationToken);
    if (dinner is null)
        return Result.Fail<Reservation>(new Error.NotFound(ResourceRef.For<Dinner>(request.DinnerId)));

    // State precondition: only Upcoming dinners take reservations. Started / Ended /
    // Cancelled → 422 (the dinner exists, it just doesn't accept new reservations).
    if (dinner.Status != DinnerStatus.Upcoming)
        return Result.Fail<Reservation>(
            Error.InvalidInput.ForRule("reservation.dinner-not-upcoming",
                $"Cannot reserve against a dinner whose status is {dinner.Status.Value}."));

    var reservationResult = Reservation.TryCreate(request.DinnerId, request.GuestUserId,
        request.GuestCount, _clock);
    if (reservationResult.IsFailure) return reservationResult;

    await _reservationRepository.Add(reservationResult.GetValueOrThrow("r"), cancellationToken);
    return reservationResult;
}
```

The anti-pattern would be: silently insert the reservation row and let it dangle. The
fail-loud version surfaces the missing aggregate at the API boundary as a structured 404
with the Dinner's `ResourceRef`, and the in-memory store never sees the orphan row.

## Reservation state machine (one transition)

```csharp
private static void ConfigureMachine(StateMachine<ReservationStatus, ReservationTrigger> machine)
{
    machine.Configure(ReservationStatus.Reserved)
           .Permit(ReservationTrigger.Cancel, ReservationStatus.Cancelled);
    // Cancelled is terminal — cancelling twice returns 422 with state.machine.invalid.transition.
}
```

One transition still earns the `LazyStateMachine<,>` framing — keeps the pattern
consistent with Dinner from PR 2 so adding future transitions (`CheckedIn`, `NoShow`) is
mechanical.

## Authorization shape

| Endpoint | Auth |
|---|---|
| `POST /reservations` | Authenticated; guest id is read from JWT `sub` claim |
| `GET /reservations/{id}` | Authenticated; handler returns 404 if `reservation.GuestUserId != caller` (leak-shielded) |
| `POST /reservations/{id}/cancel` | Same NotFound-leak-shield as above |
| `GET /reservations/mine` | Authenticated; scoped to caller's UserId |
| `GET /hosts/{hostId}/dinners/{dinnerId}/reservations` | Resource auth via `IAuthorizeResource<Host>` — only the host's owner; 403 with `code: "reservations.host.owner"` otherwise |

The guest-owns-reservation check is intentionally in the handler (not via
`IAuthorizeResource<Reservation>`) because the existing resource-auth machinery is keyed
by Host id — wiring a new `SharedResourceLoaderById<Reservation, ReservationId>` would be
mechanical follow-on work but isn't needed for PR 4 scope.

## Build, test, wire-verify

- **0 warnings / 0 errors**
- **Domain 37/37** (+6 new Reservation tests including state-machine guards + blank-reason rejection)
- **Application 1/1** (unchanged)
- **Api 60/60** (+9 new Reservation integration tests including idempotency replay/mismatch/missing-key and cross-guest 404)

Wire scenarios covered by `ReservationTests`:
1. Happy create → 201 + ETag + Location
2. POST against missing Dinner → 404 (Recipe 22)
3. POST against InProgress Dinner → 422 with `reservation.dinner-not-upcoming`
4. Idempotency replay → 201 + `Idempotent-Replayed: true` + no double-booking
5. Same key + different body → 422 fingerprint mismatch
6. Missing Idempotency-Key on `[Idempotent]` endpoint → 400 with `idempotency.key_required`
7. Cross-guest cancel → 404 (leak-shield)
8. Host can list reservations for their own dinner
9. Foreign host listing another's dinner reservations → 403 with `reservations.host.owner`

## Files that show off the most

| File | Why look here |
|---|---|
| `Application/src/Reservations/Commands/CreateReservationCommandHandler.cs` | Recipe 22 fail-loud in one handler |
| `Api/src/2022-12-21/Controllers/ReservationsController.cs` | `[Idempotent]` on the action + JWT `sub`-claim → `UserId` mapping |
| `Api/src/DependencyInjection.cs` | `AddTrellisIdempotency(opts => { ... }) + AddInMemoryIdempotencyStore()` |
| `Api/src/Program.cs` | `UseTrellisIdempotency()` MUST sit after `UseAuthentication`/`UseAuthorization` |
| `Domain/src/Reservation/Entities/Reservation.cs` | Same `LazyStateMachine<,>` pattern from PR 2, one transition |
| `Api/tests/2022-12-21/ReservationTests.cs` | Idempotency replay, fingerprint-mismatch, and missing-key contracts all asserted |

## What's NOT in this PR (deferred)

- **`MaxGuests` / capacity tracking on Dinner** — would have required updating
  `Dinner.TryCreate` signature + breaking PR 2/3 tests. Recipe 22 is still showcased via
  the existence check; capacity tracking can land in a later PR (or a dedicated PR 5).
- **`IAuthorizeResource<Reservation>`** — guest-owns-reservation check is in the handler
  for now. Wiring a new `SharedResourceLoaderById<Reservation, ReservationId>` is
  mechanical follow-on.
- **EF-backed `IIdempotencyStore`** — production-grade, multi-instance store. PR 4 ships
  the in-memory dev/test store only.
- **Reverse-seek (`Previous` cursor)** on list endpoints — still framework-deferred.
- **`Trellis.Testing` matchers** for the wire assertions — coming in PR 5 alongside
  ServiceDefaults + `AddTrellisFluentValidation`.

## Roadmap remaining

- **PR 5** (final): Reviews aggregate + `AddTrellisFluentValidation` at command boundary +
  `Trellis.Testing` matchers + `Trellis.ServiceDefaults` composition root
