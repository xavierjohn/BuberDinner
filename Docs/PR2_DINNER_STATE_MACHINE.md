# PR 2 — Dinner state machine + domain events

> *Showcase: how a real BuberDinner aggregate composes Trellis's `LazyStateMachine<,>`,
> the `ICommand<TAggregate>` + `DomainEventDispatchBehavior` wiring, and the existing
> resource-authorization plumbing — without leaking any state-machine internals to
> handler or controller code.*

---

## What this PR adds (at a glance)

| Capability | Where | Cookbook |
|---|---|---|
| `Dinner` aggregate with state machine | `Domain/src/Dinner/Entities/Dinner.cs` | Recipe 9 |
| `DinnerStatus` / `DinnerTrigger` as `RequiredEnum<TSelf>` | `Domain/src/Dinner/ValueObjects/` | Recipe 9 |
| 4 domain events, `OccurredAt` only | `Domain/src/Dinner/Events/DinnerEvents.cs` | Recipe 17 |
| `POST /hosts/{hostId}/dinners` — schedule + validates Menu ownership | `DinnersController.ScheduleDinner` | n/a |
| `GET /hosts/{hostId}/dinners/{dinnerId}` with strong ETag | `DinnersController.GetDinner` | Recipe 6 |
| `GET /hosts/{hostId}/dinners` — list (paginated in PR 3) | `DinnersController.ListDinners` | (Recipe 3 coming) |
| `POST .../start` `/end` `/cancel` — state transitions, body-less per Recipe 23 | `DinnersController` | Recipe 23 |
| Resource auth on every dinner mutation (`dinners.owner`) | `*Command : IAuthorizeResource<Host>` | Recipe 7 |
| Per-handler `IDomainEventHandler<TEvent>` registration | `Api/src/DependencyInjection.cs` | Mediator §547 |
| `TimeProvider.System` injection | DI + `Dinner.TryCreate(..., TimeProvider clock)` | Recipe 17 §1319 |
| `Trellis.StateMachine` + `Stateless` packages | `Directory.Packages.props`, `Domain.csproj` | Recipe 9 |

## The state machine (entire configuration in 9 lines)

```csharp
private static void ConfigureMachine(StateMachine<DinnerStatus, DinnerTrigger> machine)
{
    machine.Configure(DinnerStatus.Upcoming)
           .Permit(DinnerTrigger.Start,  DinnerStatus.InProgress)
           .Permit(DinnerTrigger.Cancel, DinnerStatus.Cancelled);

    machine.Configure(DinnerStatus.InProgress)
           .Permit(DinnerTrigger.End,    DinnerStatus.Ended);

    // Ended and Cancelled are terminal — no transitions configured.
}
```

The aggregate holds a `LazyStateMachine<DinnerStatus, DinnerTrigger>` field that defers
construction until first `FireResult(...)`, accesses `Status` through the supplied
getter, and writes the new state through the supplied setter:

```csharp
_machine = new LazyStateMachine<DinnerStatus, DinnerTrigger>(
    stateAccessor: () => Status,
    stateMutator: s => Status = s,
    configure: ConfigureMachine);

public Result<Dinner> Start(TimeProvider clock) =>
    _machine.FireResult(DinnerTrigger.Start)
        .Map(_ =>
        {
            var occurredAt = clock.GetUtcNow();
            StartedAt = occurredAt;
            DomainEvents.Add(new DinnerStarted(Id, HostId, MenuId, occurredAt));
            return this;
        });
```

An invalid trigger short-circuits to `Error.InvalidInput` with reason code
`state.machine.invalid.transition` — HTTP 422 at the boundary, with a typed rule
violation a caller can pattern-match on. No exception parsing, no string sniffing.

## The wire dump (verified end-to-end)

```http
# 1. Schedule a dinner → 201 + ETag + Location, status = "Upcoming".
HTTP/1.1 201 Created
ETag: "d14d94c7c9b64426ae88d5be7a5bcde6"
Location: /hosts/.../dinners/019e95a2-5735-71c1-a435-f78c5c2d6182
{ "status": "Upcoming", "startedAt": null, "endedAt": null, "cancelledAt": null, ... }

# 2. POST .../start → 200, status = "InProgress", startedAt populated.
HTTP/1.1 200 OK
ETag: "<new>"
{ "status": "InProgress", "startedAt": "2026-06-05T02:35:16.010Z", ... }

# 3. POST .../start AGAIN → 422 with state.machine.invalid.transition.
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json
{ "status": 422, "code": "invalid-input",
  "rules": [{ "code": "state.machine.invalid.transition",
              "detail": "Trigger 'Start' is not permitted from state 'InProgress'." }] }

# 4. POST .../end → 200, status = "Ended", endedAt populated.
HTTP/1.1 200 OK
{ "status": "Ended", "startedAt": "...", "endedAt": "2026-06-05T02:35:24.227Z" }

# 5. Cancel from Upcoming → 200, status = "Cancelled", cancelledAt set, endedAt STILL null.
HTTP/1.1 200 OK
{ "status": "Cancelled",
  "cancelledAt": "2026-06-05T02:35:48.949Z",
  "cancellationReason": "host illness",
  "startedAt": null, "endedAt": null }

# 6. Cancel from InProgress → 422.
HTTP/1.1 422 Unprocessable Entity
{ rules: [{ code: "state.machine.invalid.transition",
            detail: "Trigger 'Cancel' is not permitted from state 'InProgress'." }] }

# 7. Start as a different user → 403 Forbidden via IAuthorizeResource<Host>.
HTTP/1.1 403 Forbidden
{ "status": 403, "code": "dinners.owner", "kind": "forbidden" }

# 8. Schedule with a menu owned by a different host → 404.
HTTP/1.1 404 Not Found
{ "status": 404, "detail": "Menu does not belong to the specified host.", ... }

# 9. Server log (every successful transition fires one IDomainEventHandler<TEvent>):
info: LogDinnerScheduledHandler[0] Dinner X scheduled for host Y from … to …
info: LogDinnerStartedHandler[0]   Dinner X started at 2026-06-05T02:35:16Z
info: LogDinnerEndedHandler[0]     Dinner X ended at 2026-06-05T02:35:24Z
info: LogDinnerCancelledHandler[0] Dinner Z cancelled at … — reason: host illness
```

## Why `ICommand<Result<Dinner>>` (and not `IRequest<Result<Dinner>>`)

The `DomainEventDispatchBehavior` constraint is *specifically* `where TMessage : ICommand<TResponse>`
(per `trellis-api-mediator.md:677`). `IRequest` and `ICommand` are sibling interfaces in
the Mediator package — both implement `IMessage` but neither extends the other — so a
command marked `IRequest<Result<Dinner>>` is silently ignored by the dispatch behavior,
and zero domain events fire. **Every BuberDinner command that returns `Result<TAggregate>`
where `TAggregate : IAggregate` must use `ICommand<...>`.** The existing
`CreateMenuCommand`/`UpdateMenuCommand` still use `IRequest` because they don't raise
events; migrating them is harmless follow-on work.

## Why the dispatch behavior fires AFTER the handler writes

`DomainEventDispatchBehavior` snapshots `aggregate.UncommittedEvents()` after the inner
pipeline returns success, dispatches that snapshot to every registered
`IDomainEventHandler<TEvent>`, then calls `aggregate.AcceptChanges()` if dispatch is
clean. So handlers observe COMMITTED state — they never see a transient mutation that the
repository never persisted. For the in-memory repository the distinction is academic, but
when this same code runs against EF Core with `TransactionalCommandBehavior` registered
innermost, dispatch fires AFTER the transaction commits and a cascade exception cannot
roll back the database write.

## Why per-handler registration over assembly scanning

```csharp
services.AddDomainEventDispatch();
services.AddDomainEventHandler<DinnerScheduled, LogDinnerScheduledHandler>();
services.AddDomainEventHandler<DinnerStarted,   LogDinnerStartedHandler>();
services.AddDomainEventHandler<DinnerEnded,     LogDinnerEndedHandler>();
services.AddDomainEventHandler<DinnerCancelled, LogDinnerCancelledHandler>();
```

Per-handler registration is AOT-safe (no `RequiresUnreferencedCode` warning), idempotent,
and — most importantly — makes wire-up intent obvious at the registration site. Future
readers can grep `AddDomainEventHandler` once and see every event/handler pair the
service publishes; assembly scanning hides that contract.

## Why `Cancelled` and `Ended` are kept distinct (not collapsed to `Ended` + reason)

| Status | Semantic | When | Persisted timestamps |
|---|---|---|---|
| `Cancelled` | The dinner **never happened**. | `Cancel(reason)` from `Upcoming` only | `CancelledAt`, `CancellationReason`; `EndedAt` stays null |
| `Ended` | The dinner **ran to completion** (possibly early). | `End()` from `InProgress` | `StartedAt`, `EndedAt` |

This matters downstream. Refund/no-show logic answers "did the event happen?", calendars
filter past events on `EndedAt`, audit reports differentiate "host called it off" from
"event ran". Overloading `Ended` with an optional `CancellationReason` would force every
consumer to know the convention; explicit separation keeps the semantic on the type.

If we want "host called the event off after it started" later, the right answer is a
separate `EndEarly(reason)` transition that goes `InProgress → EndedEarly` (a fifth
status), not "make `Cancel` valid from `InProgress`".

## Validation gates (request → command → aggregate)

1. **Route binding** — `:HostId` and `:DinnerId` route constraints (Trellis.Asp.Routing)
   reject non-GUID paths at routing time (404 before the controller runs).
2. **Request DTO** — `ScheduleDinnerRequest.ToScheduleDinnerCommand` parses primitives
   into typed VOs via `Name.TryCreate`, `Description.TryCreate`, `MenuId.TryCreate`. Any
   parse failure is `Error.InvalidInput` (422) with field-bound violations.
3. **Resource auth** — `IAuthorizeResource<Host>` loads the route Host and verifies the
   actor owns it. Failure: 403 with `code: "dinners.owner"`.
4. **Handler-side referential integrity** — `ScheduleDinnerCommandHandler` loads the
   supplied `MenuId` and rejects if `Menu.HostId != request.HostId`. Failure: 404.
5. **Aggregate invariants** — `Dinner.TryCreate` rejects `EndDateTime <= StartDateTime`
   (422) and runs the FluentValidation rule set; `Cancel(reason)` rejects blank reasons
   before consulting the state machine.
6. **State machine** — `FireResult(trigger)` rejects invalid transitions (422 with
   `state.machine.invalid.transition`).

## Files that show off the most

| File | Why look here |
|---|---|
| `Domain/src/Dinner/Entities/Dinner.cs` | The whole state machine in one file — config helper, transition methods, event emission |
| `Application/src/Dinners/Commands/ScheduleDinnerCommand.cs` | `ICommand<Result<Dinner>>` + `IAuthorizeResource<Host>` on one record |
| `Application/src/Dinners/Commands/ScheduleDinnerCommandHandler.cs` | Cross-aggregate referential check (Recipe 22 in miniature) before constructing the aggregate |
| `Application/src/Dinners/Events/DinnerEventHandlers.cs` | Four `IDomainEventHandler<TEvent>` impls — log-only, idempotent, side-effect-only |
| `Api/src/2022-12-21/Controllers/DinnersController.cs` | Body-less state-transition POSTs with no `[Consumes("application/json")]` on `/start` and `/end` |
| `Api/src/DependencyInjection.cs` | `AddDomainEventDispatch()` + 4 `AddDomainEventHandler<,>` lines + `AddSingleton(TimeProvider.System)` |
| `Api/tests/2022-12-21/DinnerStateMachineTests.cs` | Per-test `CapturedEvents` sink validates events were published with the right `OccurredAt` |

## Stop-gaps and follow-ons

- **In-memory Dinner repository** falls back from the Cosmos branch via `AddCosmosDb`
  too — same pattern as `Host` (PR 1, Cosmos DI gap fix). Marked `// TODO: replace with
  DinnerCosmosDbRepository when implemented`.
- **List endpoint** is a simple non-paginated array today. PR 3 will swap this for
  `Page<Dinner>` + `Cursor` per Recipe 3.
- **Domain event handlers** are log-only. PR 4 (Bookings + outbox + reservations) will
  add a real `DinnerScheduledIntegrationEventPublisher` that pushes guest notifications.
- Trellis package `Trellis.StateMachine` does not currently ship a
  `trellis-api-statemachine.md` reference doc in the cookbook bundle (only a passing
  `[see also]` link). Acknowledged; not blocking for PR 2.
