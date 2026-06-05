# Domain Aggregates

## Dinner

Lifecycle-driven aggregate. State transitions enforced via a Stateless state machine
configured inside the aggregate; transitions return `Result<Dinner>` so the handler
chain composes cleanly. Every successful transition adds one entry to
`DomainEvents`, dispatched by `DomainEventDispatchBehavior` after the handler returns
success — registered handlers fire post-persistence.

### Status

```text
Upcoming ──Start──▶ InProgress ──End──▶ Ended    (terminal)
   │
   └──────Cancel(reason)──▶ Cancelled            (terminal)
```

`Ended` and `Cancelled` are both terminal but semantically distinct:

- `Cancelled` = the dinner *never happened*. Set from `Upcoming` only.
- `Ended` = the dinner *ran to completion*. Set from `InProgress` only.

### Domain events

| Event | When | Payload |
|---|---|---|
| `DinnerScheduled` | `TryCreate` succeeds | `DinnerId, HostId, MenuId, StartDateTime, EndDateTime, OccurredAt` |
| `DinnerStarted`   | `Start(clock)` succeeds | `DinnerId, HostId, MenuId, OccurredAt` |
| `DinnerEnded`     | `End(clock)` succeeds | `DinnerId, HostId, MenuId, OccurredAt` |
| `DinnerCancelled` | `Cancel(reason, clock)` succeeds | `DinnerId, HostId, MenuId, Reason, OccurredAt` |

Per cookbook Recipe 17: `OccurredAt` is the *only* timestamp on a domain event; the
event type name carries the semantic. Don't add `StartedAt`/`EndedAt`/`CancelledAt`
aliases to the events — those live on the aggregate.

### Wire shape

```json
{
    "id": "019e95a2-5735-71c1-a435-f78c5c2d6182",
    "name": "Brunch with friends",
    "description": "Casual Sunday brunch",
    "hostId": "019e95a2-5601-7868-9d20-7fb072bd5757",
    "menuId": "019e95a2-567e-739e-a05e-fe55ca12e19e",
    "status": "Upcoming",
    "startDateTime": "2026-07-01T18:00:00+00:00",
    "endDateTime":   "2026-07-01T21:00:00+00:00",
    "startedAt": null,
    "endedAt": null,
    "cancelledAt": null,
    "cancellationReason": null
}
```

### Reservations / Location / Price (deferred)

The earlier aspirational shape included `reservations[]`, `location`, `price`, and
`imageUrl`. Those land in later PRs:

- **PR 4**: `Reservation` entities under Dinner + `Bookings` aggregate + idempotency-key
  bookings + multi-aggregate orchestration.
- **PR 5+**: `Location` value object (composite), `Money` from `Trellis.Primitives`, optional
  image URL with `Maybe<UrlValue>` per Recipe 14.
