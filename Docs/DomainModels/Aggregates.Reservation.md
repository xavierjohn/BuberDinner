# Reservation aggregate

A `Reservation` is a guest's claim to one or more seats at an upcoming `Dinner`.
Lifecycle: `Reserved` → `Cancelled` (one-transition `LazyStateMachine<,>` per
Cookbook Recipe 9). Owned by both the `Guest` (via `UserId`) and a `Dinner` (via
`DinnerId`).

### Status

```text
Reserved ──Cancel(reason)──▶ Cancelled    (terminal)
```

### Domain events

| Event | When | Payload |
|---|---|---|
| `ReservationCreated`   | `TryCreate` succeeds              | `ReservationId, DinnerId, GuestUserId, GuestCount, OccurredAt` |
| `ReservationCancelled` | `Cancel(reason, clock)` succeeds  | `ReservationId, DinnerId, GuestUserId, Reason, OccurredAt` |

Per Cookbook Recipe 17, `OccurredAt` is the only timestamp on a domain event; the
event type name carries the "Reserved"/"Cancelled" semantic. The aggregate also tracks
`ReservedAt` and `CancelledAt` as projection-friendly fields (sourced from the same
`clock.GetUtcNow()` that stamps the event's `OccurredAt`).

### Wire shape

```json
{
    "id": "019e9690-10f4-7584-8ab7-5af569f6983a",
    "dinnerId": "019e9690-0fdc-7aa6-9807-7f0a56f08c42",
    "guestUserId": "guest_ab989d50",
    "guestCount": 2,
    "status": "Reserved",
    "reservedAt": "2026-06-05T06:54:44.4687691+00:00",
    "cancelledAt": null,
    "cancellationReason": null
}
```

### Multi-aggregate orchestration (Cookbook Recipe 22)

The create handler loads the parent `Dinner` first; a missing Dinner is `404`, not a
silent orphan row. See `Application/src/Reservations/Commands/CreateReservationCommandHandler.cs`.

### Idempotency (Cookbook Recipe 29)

`POST /reservations` opts into the IETF `Idempotency-Key` middleware via the
`[Idempotent]` attribute. A retry with the same key + same body returns the cached 201
byte-for-byte with an extra `Idempotent-Replayed: true` header. Same key + different
body → 422 (fingerprint mismatch). Missing key on the opted-in endpoint → 400 with
`code: idempotency.key_required`.

### Deferred to future PRs

- **Capacity tracking on Dinner** (`MaxGuests` + `ReservedGuestCount`) — would let the
  reservation handler also call `dinner.Reserve(guestCount)` and demonstrate the full
  Recipe 25 two-pass validate-then-mutate pattern.
- **Host cancel** — currently only the owning guest can cancel.
- **`Bill`, `Guest`, `MenuReview` aggregates** — stubs in the design docs only.
