# Upgrading BuberDinner from FunctionalDdd 2.1.10 to Trellis v3 (alpha.337)

> One PR, four commits, .NET 8 → .NET 10. Zero net regression in the test suite.
> This document is the shareable upgrade story — what landed, what got better,
> what got worse, and what we want the Trellis maintainers to look at.

---

## TL;DR

**Verdict: ship it.** The Trellis V3 closed-`Error` ADT, the safer `Result<T>` surface, and the RFC 9457-compliant ASP boundary are unambiguous wins. The migration cost was eight files of mechanical edits (CRTP base classes, `Result.Ok`/`Result.Fail`, `ToHttpResponseAsync.AsActionResultAsync`) plus a tracked set of `.Value` rewrites driven by the deliberate removal of `Result<T>.Value`. We hit one genuinely concerning silent semantic change (`RequiredString` now accepts empty strings by default) and three smaller surface-area regressions worth taking back to the framework team.

| Metric | Before | After |
|---|---|---|
| Framework | `FunctionalDdd 2.1.10` | `Trellis 3.0.0-alpha.337` |
| Runtime | `net8.0` (SDK 8.0.414) | `net10.0` (SDK 10.0.300) |
| Build | 0 warnings / 0 errors | 0 warnings / 0 errors |
| Tests passing (excl. live-Cosmos) | 47 / 47 | 47 / 47 |
| Total commit count | n/a | 4 |
| Total source files edited | n/a | 32 |
| Total `.cs` LoC delta | n/a | +1,478 / −165 |

---

## Why this PR exists

BuberDinner is the showcase app for the framework. The framework rebranded from FunctionalDdd to Trellis between major versions, and v3 ships a deliberately closer-to-final API. Keeping BuberDinner pinned to `FunctionalDdd 2.1.10` makes it a misleading example. This PR catches up.

## Migration story (commit-by-commit)

Each commit can be read in isolation. The PR tip is green.

### Commit 1 — `chore: pin nuget.org as only package source for BuberDinner restore`

Before any framework work, BuberDinner needed a project-local `nuget.config` with a single `nuget.org` source. Central Package Management (CPM) hits `NU1507` on machines with the global user `NuGet.Config` containing multiple package sources, which is common when a developer also publishes Trellis (GitHub Packages source) or has a local feed configured. The fix is one file, no source code touched.

### Commit 2 — `refactor: migrate to Trellis 3.0.0-alpha.337 on .NET 10`

The big mechanical sweep. The `Directory.Build.props` global `Using` switches from `FunctionalDdd` to `Trellis`. `Directory.Packages.props` swaps the six `FunctionalDdd.*` package versions for four `Trellis.*` packages (Core, Asp, Primitives, FluentValidation). `global.json` and `Directory.Build.props` retarget to .NET 10 / SDK 10.0.300. Every `.csproj` `PackageReference` updates accordingly. Then the mechanical API renames:

- `Result.Success` / `Result.Failure` → `Result.Ok` / `Result.Fail`
- `.ToActionResultAsync(this)` → `.ToHttpResponseAsync().AsActionResultAsync<T>()`
- `MenuId.NewUnique()` → `MenuId.NewUniqueV7()` (time-ordered, the new framework default for primary keys)
- `class UserId : RequiredString` → `class UserId : RequiredString<UserId>` (CRTP) across 12 value objects
- Implicit `T → Result<T>` conversion removed at 5 `TryCreate` factories — explicit `Result.Ok(...)` everywhere
- `UnauthorizedError`, `ConflictError`, `NotFoundError` (open hierarchy) → `Error.AuthenticationRequired`, `Error.Conflict`, `Error.NotFound` (closed union)

Production code builds clean after C2. Test code remains red because `Result<T>.Value` and `ValidationError` are gone — fixed in C3.

### Commit 3 — `refactor: port tests + DTO reconstruction to Trellis V3 closed Error union`

Test assertions migrate from `ValidationError`/`FieldError(name, [details])` to `Error.InvalidInput`/`FieldViolation(InputPointer, code) { Detail }`. The new shape compares on JSON Pointer paths (`"/firstName"`, `"/email"`) — Trellis normalizes property names to JSON Pointers at the boundary.

`Result<T>.Value` is gone (deliberately — see win-004). We introduce **one** shared helper at `Domain/src/Common/TrellisResultExtensions.cs`:

```csharp
public static T UnwrapOrThrow<T>(this Result<T> result, string? context = null) =>
    result.Match(
        value => value,
        error => throw new InvalidOperationException(
            context is null
                ? $"Cannot unwrap failed Result<{typeof(T).Name}>: {error}"
                : $"Cannot unwrap failed Result<{typeof(T).Name}> ({context}): {error}"));
```

It covers the legitimate "this validation already happened — reconstruct or fail loudly" case (DTO reconstruction in `Infrastructure/Persistence/Dto/*`, test arrangement in `Domain/tests/*`, `Application/tests/*`, `Infrastructure/tests/*`). One canonical implementation, lifted into the global `Using` so every project picks it up. **When Trellis ships `Result<T>.UnwrapOrThrow` natively (see reg-003), delete the helper and the global Using.**

The C3 commit also catches up two stragglers from C2 (`Api/src/2022-12-21/Models/Menus/CreateMenuRequest.cs` had two private `.Value` sites the C2 verification missed because the Api project's compile was masked by an Infrastructure metadata failure), and applies two `IDE0055` formatting fixes (the .NET 10 analyzers are stricter about blank lines between namespace declarations and `using` directives).

### Commit 4 — `feat(mediator): adopt Trellis.Mediator pipeline behaviors`

A three-line change: add `Trellis.Mediator` package reference, `using Trellis.Mediator;` in `Application/src/DependencyInjection.cs`, call `services.AddTrellisBehaviors()`. This lights up Exception, Tracing, and Logging behaviors immediately. Authorization and Validation behaviors register too but no-op at request time when no `IActorProvider` or `IMessageValidator<>` is configured (see win-008). All 47 tests still pass.

---

## Before / after dependency matrix

| Concern | Before | After |
|---|---|---|
| Domain types (Result, Error, Entity, RequiredString, RequiredGuid) | `FunctionalDdd.DomainDrivenDesign 2.1.10` | `Trellis.Core 3.0.0-alpha.337` |
| Railway operators (Bind, Map, Tap, Ensure, Combine) | `FunctionalDdd.RailwayOrientedProgramming 2.1.10` (collapsed) | `Trellis.Core 3.0.0-alpha.337` |
| Value-object source generator | `FunctionalDdd.CommonValueObjectGenerator 2.1.10` | bundled inside `Trellis.Core 3.0.0-alpha.337` (analyzers/dotnet/cs) |
| Primitive value objects (`EmailAddress`, `Money`) | `FunctionalDdd.CommonValueObjects 2.1.10` | `Trellis.Primitives 3.0.0-alpha.337` |
| FluentValidation → Result adapter | `FunctionalDdd.FluentValidation 2.1.10` | `Trellis.FluentValidation 3.0.0-alpha.337` |
| ASP boundary (`AddTrellisAsp`, `ToHttpResponse`) | `FunctionalDdd.Asp 2.1.10` | `Trellis.Asp 3.0.0-alpha.337` |
| Mediator pipeline behaviors | (not adopted) | `Trellis.Mediator 3.0.0-alpha.337` |
| Mediator (martinothamar) | `2.1.7` | `3.0.2` (forced by `Trellis.Mediator` transitive — see reg-002) |
| ASP.NET Core / Microsoft.Extensions | `8.0.x` | `10.0.x` |

---

## Wins (rally material — share these)

### 🥇 The big-deal architectural wins

**[win-009 — added during audit] `RequiredString<TSelf>` is strict-by-default in alpha.337**
This wasn't visible to me when I read the older `TrellisFramework` source snapshot during the migration — that snapshot showed `[NotDefault]` as opt-in. The published API ref `.github/trellis-api-core.md:1730` is unambiguous:

> *"Every `Required*<TSelf>` base is **strict by default**. The generated `TryCreate` rejects `null` for every base and also rejects each base's sentinel value where one exists... `[NotDefault]` and `[Trim]` are now vestigial no-ops: the generator ignores them and emits informational diagnostics (`TRLS046`, `TRLS047`)."*

For `RequiredString<TSelf>` specifically, `null`, `""`, and whitespace-only are all rejected by default. To accept empties you opt **out** via `[AllowEmpty]` / `[AllowWhitespace]` / `[NoTrim]` — the inverse of the v2.x model. **This is the right default** and matches how every other strict primitive library converges. The previously-feared silent semantic regression (originally tracked as `reg-004`) does not exist in the shipped framework — it was an artifact of reading a stale snapshot.

### 🥇 The big-deal architectural wins (continued)

**[win-004] `Result<T>.Value` is gone**
`Trellis.Core/src/Result/Result{TValue}.cs:163` explicitly documents the removal: *"in v1 there was a `public TValue Value { get; }` property that threw `InvalidOperationException` on failure — the primary cause of TRLS003. It was removed from the current API."* Callers can no longer silently bypass the failure case. Replacement APIs (`TryGetValue`, `Match`, `Deconstruct`, `GetValueOrDefault`) all force the caller to acknowledge the failure path. **The single most important safety improvement in the V3 surface.**

**[win-005] Implicit `T → Result<T>` is gone**
Factory methods like `public static Result<T> TryCreate(...) => new T(...);` no longer silently lift to a `Result` — you have to type `Result.Ok(new T(...))`. Same philosophy as `.Value` removal: no surprise lifts. Cost: 5 trivial edits in BuberDinner. Benefit: every "is this a success or a wrapped failure?" question is answered at the construction site.

**[win-007] RFC 9457 `application/problem+json` for failure responses**
`AddTrellisAspWithScalarValidation()` + `.ToHttpResponseAsync()` now emits the IETF-standard problem-details content type for all failure responses. The pre-V3 pipeline emitted plain `application/json` for errors — technically non-compliant. Any external HTTP client that sniffs Content-Type to decide "is this a problem details payload?" (Polly, OpenAPI client generators, anything implementing the spec) now works out of the box.

### 🥈 The substantial-but-quieter wins

**[win-001] .NET 10 SDK catches `NU1902` transitive-dep vulnerabilities**
Pre-existing supply-chain dirt that `dotnet 8.0.421` silently restored becomes a hard build error on `dotnet 10.0.300`. For BuberDinner this surfaced `OpenTelemetry.Api 1.9.0` (transitively pulled by FunctionalDdd 2.1.10). The Trellis swap also resolves it because the V3 packages depend on a current OpenTelemetry. **The migration motivated us to act on a security finding that was already there.**

**[win-002] CRTP on `RequiredString<TSelf>` / `RequiredGuid<TSelf>`**
The base classes now take the derived type as a generic parameter. Cost: 12 one-line edits in BuberDinner (`: RequiredString` → `: RequiredString<UserId>`). Benefit: the source generator emits a fully-typed `TryCreate(string) → Result<TSelf>`, typed `IScalarValue<TSelf, string>.Value` implementation, typed value-object equality, and a JSON converter pinned to `TSelf`. The entire downstream type chain is type-safe end to end. Worth the 12 line edits.

**[win-003] `NewUnique()` → `NewUniqueV4()` / `NewUniqueV7()`**
The old name hid the choice between random and time-ordered UUIDs. The new generator forces an explicit pick. `NewUniqueV7()` is the right default for primary keys (sortable, monotonic, index-friendly) — picking it is a single character of code. Breaking change for callers but the right kind: makes a meaningful semantic choice visible.

**[win-008] Pipeline behaviors no-op gracefully when no provider is registered**
`services.AddTrellisBehaviors()` is genuinely a single-line adoption. The Authorization and Validation behaviors detect "no `IActorProvider` / `IMessageValidator<>`" at request time and pass through. Lets a project consume Exception + Tracing + Logging immediately without committing to actor-provider scaffolding or moving validation from the DTO layer to the command boundary. This is good framework citizenship — explicit support for partial adoption.

### 🥉 The "didn't know I wanted this" win

**[win-006] Trellis packages auto-deposit per-package API reference markdown into `.github/`**
After `dotnet restore`, the consumer repo gets `.github/trellis-api-core.md`, `trellis-api-asp.md`, `trellis-api-primitives.md`, `trellis-api-fluentvalidation.md`, `trellis-api-mediator.md`, `trellis-api-authorization.md`, `trellis-api-http-abstractions.md`, and `trellis-api-cookbook.md`. Any AI assistant (Copilot, Claude, etc.) reads the actual current-version API surface, not stale training data. BuberDinner now ships AI-friendly API references for free by virtue of adopting Trellis. Zero hand-written instructions.

---

## Regressions (take-back-to-team material — these need framework-side action)

> **Audit correction (2026-06-04):** an audit pass against the auto-deposited API ref docs in `.github/trellis-api-*.md` invalidated the original tier-1 regression `reg-004` ("RequiredString silently accepts empty strings"). The shipped alpha.337 framework rejects empty strings by default; the `[NotDefault, Trim]` attributes the migration originally added were vestigial no-ops and have been removed. See [`win-009`](#-the-big-deal-architectural-wins) above. **Three regressions remain.** Every remaining entry below satisfies the falsifiability rule: same user intent + observable worsening + not just mechanical churn + concrete repro artifact + the fix is owned by the framework, not by BuberDinner.

### ~~🚨 Tier 1 (correctness): `[reg-004]` `RequiredString` silently accepts empty strings~~ — RETRACTED

The original claim was based on a stale `Trellis.Core/src/Primitives/RequiredString.cs` snapshot that showed `[NotDefault]` as opt-in. The published `Trellis.Core 3.0.0-alpha.337` reverses that default — see `.github/trellis-api-core.md:1730`. BuberDinner's tests pass with **no** `[NotDefault, Trim]` attributes, confirming the framework rejects empty strings by default. Issue draft `files/issue-reg-004-required-empty-string.md` has been marked **do not file**. This is a meaningful methodology correction: source-tree snapshots can lag published packages; the API ref docs auto-deposited by the package are the actual contract.

### 🚨 Tier 1 — (none remaining after audit)

### Tier 2 (annoyance): `[reg-001]` Closed `Error` union has no `ReasonCode` on `AuthenticationRequired`

| | |
|---|---|
| **Area** | `Trellis.Core.Error` |
| **Severity** | `annoyance` — observable telemetry/wire-data loss |
| **File** | `Domain/src/Common/Errors/Errors.Authentication.cs` |

```csharp
// Before (v2.x)
public static Error InvalidCredentials =>
    new UnauthorizedError("Invalid credentials.", "Authentication.InvalidCredentials");

// After (V3) — the "Authentication.InvalidCredentials" machine code has no home
public static Error InvalidCredentials =>
    new Error.AuthenticationRequired(Scheme: "Bearer") { Detail = "Invalid credentials." };
```

`Trellis.Core/src/Errors/Error.cs:329` defines `AuthenticationRequired(string? Scheme = null)` — only the `WWW-Authenticate` scheme is a discriminator. The old open hierarchy carried a free-form `Code` slot that distinguished "wrong password" from "no token". Telemetry that filtered on the code, dashboards that counted invalid-credential 401s separately, and clients that branched on the code all lose the distinction. Only the human-readable `Detail` survives.

**Proposed framework-side fix.** Either:

- (a) Add an optional `string? ReasonCode = null` to `Error.AuthenticationRequired` — mirrors the slot on `Conflict`, `InvariantViolation`, `Unavailable`, `Unexpected`. Non-breaking.
- (b) Introduce a sibling case `Error.InvalidCredentials(string? Scheme = null, string? ReasonCode = null)` so the closed catalog can carry the distinction explicitly.

We picked (a)'s equivalent at the BuberDinner layer (carrying the reason via `Detail`) but lost the structured slot.

### Tier 2 (annoyance): `[reg-002]` `Trellis.FluentValidation` forces a transitive `Trellis.Mediator` dependency

| | |
|---|---|
| **Area** | `Trellis.FluentValidation` packaging |
| **Severity** | `annoyance` — forces unrelated major-version bumps on consumers |
| **Files** | `Domain/src/BuberDinner.Domain.csproj`, `Directory.Packages.props` |

`Trellis.FluentValidation 3.0.0-alpha.337` has a hard `ProjectReference` on `Trellis.Mediator` (confirmed via `TrellisFramework/Trellis.FluentValidation/src/Trellis.FluentValidation.csproj:21` and the published nuspec). FluentValidation is a domain-layer concern; Mediator is an application-layer concern. Pulling FluentValidation into the Domain project transitively drags `Trellis.Mediator` (with its Authorization, OTel tracing, logging infrastructure) into the domain. It also forces consumers using `martinothamar/Mediator 2.x` onto `3.x` even if they never adopt `AddTrellisBehaviors()`.

For BuberDinner this surfaced as `NU1605` the moment FunctionalDdd was swapped out. We had to bump `Mediator.Abstractions` from `2.1.7` to `3.0.2` to make the restore work.

The user loses the "incremental adoption" option for `Trellis.FluentValidation` alone — and a Domain project has no business depending on a mediator framework.

**Proposed framework-side fix.** Either:

- Split the `IMessageValidator` adapter into a separate `Trellis.Mediator.FluentValidation` package so `Trellis.FluentValidation` can stay Domain-friendly with zero Mediator coupling.
- Or invert the dependency direction: move the adapter into `Trellis.Mediator/FluentValidation/` so `Trellis.Mediator` depends on `Trellis.FluentValidation`. Then pulling FluentValidation alone stays Domain-pure.

### Tier 2 (annoyance): `[reg-003]` No `Result<T>.UnwrapOrThrow()` for DTO reconstruction

| | |
|---|---|
| **Area** | `Trellis.Core` `Result<T>` |
| **Severity** | `annoyance` — visual noise on every DTO mapper / test setup |
| **Files** | `Infrastructure/src/Persistence/Dto/*.cs` (18 call sites), various test files |

`Result<T>` removed `.Value` for safety (rightly so — see win-004). `Maybe<T>` kept `.Value` (which forwards to `GetValueOrThrow()`) AND exposes `GetValueOrThrow(string? errorMessage = null)` as a named opt-in. `Result<T>` ships neither — and the DTO-reconstruction pattern (where the database-side data is already validated, so reconstruction "cannot fail") is universal in any persistence layer.

Consumers have three bad choices:

1. Inline `.Match(v => v, e => throw new InvalidOperationException(e.ToString()))` 18 times in BuberDinner.
2. Write their own `Unwrap()` extension in app code (re-invents the wheel in every Trellis adopter).
3. Use `GetValueOrDefault(null!)` and risk silent `NullReferenceException` later.

We adopted option 2 — `Domain/src/Common/TrellisResultExtensions.cs` is the single canonical helper, lifted into the global `Using` so all projects pick it up. The visual noise is real even in 50 lines of DTO mapping; it would be untenable in a serialization-heavy codebase.

**Proposed framework-side fix.** Ship `Result<T>.UnwrapOrThrow(string? errorContext = null)` mirroring `Maybe<T>.GetValueOrThrow`. The name and "throws on failure" contract are explicit; analyzer TRLS003 can keep flagging unsafe uses but allow this one named escape hatch. Documentation should call out the DTO reconstruction pattern as the canonical use case.

---

## Deferred / not migrated (intentional)

- **xunit v2 → xunit.v3.** The current Trellis Showcase uses `xunit.v3` + `Microsoft.Testing.Platform`. BuberDinner stays on `xunit v2` + `xunit.runner.visualstudio` because the test runner migration is independent of the framework migration and would muddy the diff. Track separately.
- **Authorization behavior wiring.** `Trellis.Mediator`'s `AuthorizationBehavior` requires `IActorProvider`. BuberDinner has no policy-protected commands yet, so we let the behavior register and no-op. Adoption lands naturally when the first authorization use case appears.
- **Validation behavior via `IMessageValidator<TCommand>`.** BuberDinner runs validation at the DTO layer (`Request.ToCommand()`) before `Send()`. Moving validation to the command boundary is an architectural shift, not a framework-migration concern. Defer.
- **`AddTrellisFluentValidation()`.** Same reasoning — FluentValidation is used inside the Domain validators (`User`, `Menu`, `MenuItem`, `MenuSection`), not as a Mediator pre-handler.
- **Infrastructure tests requiring a live Cosmos DB.** `Infrastructure.Tests` failed 2/2 on the baseline (no Cosmos), and fails 2/2 after the migration. Not migration-related.

---

## Final validation evidence

Final commit: `feat(mediator): adopt Trellis.Mediator pipeline behaviors`.

```text
> dotnet build --nologo --no-restore
Build succeeded.
    0 Warning(s)
    0 Error(s)

> dotnet test Domain/tests        →  Passed!  Failed: 0, Passed: 19, Total: 19
> dotnet test Application/tests   →  Passed!  Failed: 0, Passed:  1, Total:  1
> dotnet test Api/tests           →  Passed!  Failed: 0, Passed: 27, Total: 27
> dotnet test Infrastructure/tests → Failed!  Failed: 2, Passed:  0, Total:  2  (pre-existing: live Cosmos required)
```

Pre-Cosmos test pass rate: **47 / 47, matching baseline.**

---

## Falsifiability rule for this document

Every regression in this document satisfies all five of:

1. **Same user intent** — the old and new code were trying to express the same thing.
2. **Observable worsening** — more code, less type information, lost machine-readable code, broken wire compatibility, or required workaround.
3. **Not just one-time churn** — mechanical renames with no semantic loss are paper cuts, not regressions.
4. **Repro artifact** — file path, before/after snippet, and failing build/test output where applicable.
5. **Framework-owned fix exists** — the fix belongs in Trellis, not in BuberDinner.

If a future entry doesn't meet all five, demote it to "paper cut" or drop it.

---

## Recommendations back to Trellis maintainers

In priority order:

1. ~~**`reg-004` (correctness)** — document the `RequiredString` empty-string semantic change loudly in `MIGRATION_v3.md`, AND ship an analyzer.~~ **RETRACTED** after API-ref audit (see win-009). Framework already ships strict-by-default.
2. **`reg-003`** — ship `Result<T>.UnwrapOrThrow(string? context = null)`. DTO reconstruction is the canonical use case; every framework adopter will write this helper themselves otherwise.
3. **`reg-002`** — split `Trellis.FluentValidation` from `Trellis.Mediator`. Domain projects should never have to depend on a mediator framework.
4. **`reg-001`** — add `ReasonCode` to `Error.AuthenticationRequired` (non-breaking) or introduce `Error.InvalidCredentials`.

Each one is small, principled, and lifts the framework toward "obvious choices only" — the same direction `win-004`, `win-005`, `win-007`, and `win-009` already point.
