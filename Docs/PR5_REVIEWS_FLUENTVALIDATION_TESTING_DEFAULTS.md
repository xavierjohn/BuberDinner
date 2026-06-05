# PR 5 — Reviews + FluentValidation + Trellis.Testing + Trellis.ServiceDefaults

> *Capstone of the 5-PR showcase arc. Adds the final aggregate (`MenuReview`), bundles
> three previously-unwired Trellis features (`AddTrellisFluentValidation`,
> `Trellis.Testing` matchers, `Trellis.ServiceDefaults` composition builder), and
> demonstrates how a real composition root collapses from a dozen `Add*` calls into one
> fluent `AddTrellis(t => t.Use*(...)...)` expression.*

---

## What this PR adds (at a glance)

| Capability | Where | Cookbook |
|---|---|---|
| `MenuReview` aggregate (`Submit`, `UpdateContent`) | `Domain/src/MenuReview/Entities/MenuReview.cs` | Recipe 1 |
| 2 domain events (`MenuReviewSubmitted`, `MenuReviewUpdated`) | `Domain/src/MenuReview/Events/` | Recipe 17 |
| `POST /menu-reviews` / `PUT /menu-reviews/{id}` / `GET /menu-reviews/{id}` / `GET /menu-reviews/for-menu/{menuId}` | `MenuReviewsController` | Recipes 3 + 22 |
| **FluentValidation at the command boundary** via `IValidator<TCommand>` + auto-wired Mediator behavior | `SubmitMenuReviewCommandValidator`, `UpdateMenuReviewCommandValidator` | Mediator §validation |
| **`Trellis.Testing` matchers** (`.Should().BeSuccess()`, `.BeFailureOfType<>()`, `.Unwrap()`) | `Domain/tests/MenuReviewTests.cs` | trellis-api-testing.md |
| **`Trellis.ServiceDefaults` composition builder** — `services.AddTrellis(t => t.Use*(...)...)` replaces 10+ individual `Add*` calls | `Api/src/DependencyInjection.cs` | trellis-api-servicedefaults.md |
| Stub cleanup: orphan `MenuReviewId` in the wrong namespace | `Domain/src/Menu/Menu.cs`, `Infrastructure/src/Persistence/Dto/MenuDto.cs`, `Infrastructure/tests/MenuRepositoryTests.cs` | — |

## FluentValidation kicking in at the wire

```http
POST /menu-reviews
{ "menuId": "...", "dinnerId": "...", "rating": 99, "comment": "" }

HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json
{
  "status": 422, "code": "invalid-input",
  "errors": {
    "Rating":  ["Rating must be between 1 and 5."],
    "Comment": ["Comment is required."]
  }
}
```

The `SubmitMenuReviewCommand` and `UpdateMenuReviewCommand` types each have a
matching `AbstractValidator<TCommand>` in `Application/MenuReviews/Validators/`.
`services.AddTrellis(t => t.UseFluentValidation(typeof(SubmitMenuReviewCommandValidator).Assembly))`
scans the assembly, registers every `IValidator<T>` as scoped, and plugs them into
the existing `ValidationBehavior<,>` mediator slot. Failures aggregate into a single
`Error.InvalidInput`, which the ASP boundary serialises as RFC 4918 Problem Details.

Per-handler `AddScoped<IValidator<X>, XValidator>()` registrations are also supported
(AOT-safe variant) — the assembly-scanning overload is just the convenient default.

## The composition root before vs after

**Before** (PR 4 tip — `Api/src/DependencyInjection.cs`, ~70 lines of `Add*` calls):
```csharp
services.AddTrellisAspWithScalarValidation();
services.AddTrellisRouteConstraint<HostId>(nameof(HostId));
// ... 4 more route constraints
services.AddClaimsActorProvider(opts => opts.ActorIdClaim = "sub");
services.AddResourceAuthorization(typeof(UpdateMenuCommand).Assembly, ...);
services.AddDomainEventDispatch();
services.AddDomainEventHandler<DinnerScheduled, LogDinnerScheduledHandler>();
// ... 5 more event handlers
services.AddTrellisIdempotency(opts => { ... });
services.AddInMemoryIdempotencyStore();
services.AddSingleton(TimeProvider.System);
```

**After** (PR 5 — one fluent builder):
```csharp
services.AddTrellis(t => t
    .UseAsp()
    .UseScalarValueValidation()
    .UseProblemDetails()
    .UseMediator()
    .UseClaimsActorProvider(opts => opts.ActorIdClaim = "sub")
    .UseResourceAuthorization(typeof(UpdateMenuCommand).Assembly, typeof(HostResourceLoader).Assembly)
    .UseFluentValidation(typeof(SubmitMenuReviewCommandValidator).Assembly)
    .UseDomainEvents()
    .UseIdempotency(opts => { opts.Ttl = TimeSpan.FromHours(24); opts.MaxRequestBodyBytes = 256 * 1024; }));
services.AddInMemoryIdempotencyStore();
```

The builder enforces the mutually-required call order so misconfigurations fail at
startup, not at request time. Per-type registrations (`AddTrellisRouteConstraint<HostId>`,
`AddDomainEventHandler<DinnerScheduled, ...>`, `AddInMemoryIdempotencyStore`) stay
separate — those are explicitly "application-owned" per the docs.

## Test-side cleanup with `Trellis.Testing` matchers

**Before** (PR 2-4 domain tests):
```csharp
result.IsSuccess.Should().BeTrue();
var error = result.Match(_ => null!, e => e).Should().BeOfType<Error.InvalidInput>().Subject;
error.Rules.Items.Should().ContainSingle().Which.ReasonCode.Should().Be("...");
```

**After** (PR 5 `MenuReviewTests`):
```csharp
result.Should().BeSuccess();
result.Should().BeFailureOfType<Error.InvalidInput>();
var review = result.Unwrap();  // test-only, doesn't leak into production
```

I kept the older Dinner/Reservation tests in their existing style to avoid churn —
just used the new matchers for the 8 new `MenuReviewTests` so future authors have a
side-by-side reference. The framework also ships `FakeRepository<TAggregate, TId>`,
`TestActorProvider`, and other handler-level helpers documented in
`.github/trellis-api-testing.md`.

## Domain-aggregate cleanup (drive-by)

A pre-existing stub `Domain/src/MenuReview/ValueObjects/MenuId.cs` defined
`MenuReviewId` in the **wrong** namespace (`BuberDinner.Domain.Menu.ValueObject`
instead of `BuberDinner.Domain.MenuReview.ValueObject`). `Menu.cs` consumed it
through that wrong namespace, papering over the bug. PR 5 deletes the stub, adds
the canonical `MenuReviewId.cs` under the right namespace, and updates the 3
consumers (`Menu.cs`, `MenuDto.cs`, `MenuRepositoryTests.cs`) to use the canonical
path.

## Build, test, wire-verify

- **0 warnings / 0 errors**
- **Domain 45/45** (+8 new `MenuReviewTests` using `Trellis.Testing` matchers)
- **Application 1/1** (unchanged)
- **Api 69/69** (+7 new `MenuReviewTests`)
- Infrastructure 0/2 (live-Cosmos baseline, pre-existing)

Wire scenarios covered by `MenuReviewTests` (and verified by curl):
- Submit happy path → 201 + ETag + Location
- Submit rating=99 → 422 with `errors.Rating` (FluentValidation)
- Submit comment="" → 422 with `errors.Comment` (FluentValidation)
- Submit both wrong → 422 with BOTH errors aggregated
- Submit against missing menu → 404 (Recipe 22 fail-loud)
- Update with rating=0 → 422 with `errors.Rating` (validator runs on Update too)
- Cross-guest update → 404 (NotFound-leak-shield)
- Paginated list → 3+3+1 of 7 items, last page `next=null`

## Commit walkthrough (6 commits)

| # | Scope |
|---|---|
| 1 | `chore(deps)`: Trellis.Mediator.FluentValidation + Trellis.Testing + Trellis.ServiceDefaults; FluentAssertions bumped to 7.2.2 (Trellis.Testing requirement) |
| 2 | `feat(domain)`: MenuReview aggregate + 2 events + 8 tests; orphan-stub cleanup |
| 3 | `feat(infra+app)`: MenuReviewInMemoryRepository + 4 commands/queries + 2 validators + 2 log handlers |
| 4 | `feat(api)`: MenuReviewsController + DTOs |
| 5 | `refactor(api)`: ServiceDefaults composition builder collapses ~30 lines of Add* into one fluent expression |
| 6 | `test(api) + docs`: 7 integration tests + 4 .http files + Aggregates.MenuReview.md + PR5_*.md |

## Roadmap complete

This is the final PR in the showcase arc. After merge, BuberDinner exercises every
Trellis V3 package shipped today: `Trellis.Core` / `Trellis.Primitives` /
`Trellis.FluentValidation` / `Trellis.Mediator` / `Trellis.Mediator.FluentValidation` /
`Trellis.Asp` / `Trellis.Http.Abstractions` / `Trellis.Authorization` /
`Trellis.StateMachine` / `Trellis.Testing` / `Trellis.ServiceDefaults`.

The only Trellis V3 package NOT exercised is `Trellis.EntityFrameworkCore` —
in-memory persistence is intentional for the showcase. An EF Core swap is a clean
follow-up (the `IRepository<T>` abstraction is the only thing the application layer
sees; swapping the implementation requires no Application/Api changes).
