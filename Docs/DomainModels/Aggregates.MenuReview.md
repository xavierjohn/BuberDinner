# Menu Review aggregate

A guest who attended a dinner can leave a rating + comment on the menu they
experienced. Owned by both the guest (`GuestUserId`) and the menu (`MenuId`),
with the dinner they attended captured as a foreign key (`DinnerId`).

### Status

No state machine — reviews don't transition. Two operations exist:
`TryCreate` (initial submit) and `UpdateContent` (edit the rating + comment).
Only the owning guest can update. Reviews are not deleted.

### Domain events

| Event | When |
|---|---|
| `MenuReviewSubmitted` | `TryCreate` succeeds |
| `MenuReviewUpdated`   | `UpdateContent` succeeds |

### Validation

Validation lives in four places, each with a distinct job:

1. **Content invariants (domain)** — `MenuReview.s_inputValidator` is a private
   `InlineValidator<ContentInputs>` that runs over a `(rating, comment)` record
   BEFORE any mutation. It enforces rating ∈ [1, 5], non-empty comment ≤ 1000
   chars. Failures aggregate into one `Error.InvalidInput`.
2. **FK integrity (wire)** — `SubmitMenuReviewRequest.ToSubmitMenuReviewCommand(UserId)`
   parses the inbound primitive `menuId`/`dinnerId` strings into `MenuId`/`DinnerId`
   value objects via the `TryCreate`/`Combine`/`Map` Result pipeline — a malformed
   GUID produces a 422 before the handler runs.
3. **Cross-aggregate gates (handler)** — `SubmitMenuReviewCommandHandler` chains the
   following ROP gates so an orphan/unauthorised review can't be created:
   1. Menu exists → 404.
   2. Dinner exists → 404.
   3. Dinner.MenuId == request.MenuId → 404 (leak-shielded).
   4. Caller has a non-cancelled reservation for the dinner → 404 (leak-shielded —
      identical shape to the missing-dinner case to avoid revealing dinner existence
      to non-attendees).
   5. Dinner.Status == `Ended` → 422 `review.dinner-not-ended` (status revealed
      only AFTER the caller is proven to be an attendee).
4. **Command boundary (PR 5 showcase)** — `SubmitMenuReviewCommandValidator` and
   `UpdateMenuReviewCommandValidator` re-state the rating/comment rules as
   FluentValidation rules wired into the Mediator pipeline via
   `AddTrellisFluentValidation(...)`. These run BEFORE the handler and surface
   field-bound 422s with the standard Trellis Problem Details shape.

### Wire shape

```json
{
    "id": "019e98be-8ad3-7d2c-9daf-4c0e5d018ba9",
    "menuId": "019e98be-8934-72c5-83e4-8a0052d4c70b",
    "dinnerId": "019e98be-89c2-7601-80c9-438d26589e35",
    "guestUserId": "guest_551d1b36",
    "rating": 4,
    "comment": "Loved the brunch — three-egg omelette was perfect."
}
```

### Deferred to future PRs

- **Aggregate audit timestamps** — `Aggregate<TId>` ships `CreatedAt`/`LastModified`
  populated by `Trellis.EntityFrameworkCore` at row-write time. For the in-memory
  showcase they remain `default(DateTimeOffset)`. EF persistence is its own future PR.
- **Resource-based authorization for updates** — currently the owning-guest check
  lives in `UpdateMenuReviewCommandHandler.LoadReviewOwnedByAsync`. A future
  `SharedResourceLoaderById<MenuReview, MenuReviewId>` would let it use
  `IAuthorizeResource<MenuReview>` declaratively on the command.
