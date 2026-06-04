namespace BuberDinner.Domain.Common;

/// <summary>
/// Local helper while Trellis.Core does not ship a Result&lt;T&gt;.UnwrapOrThrow() ergonomic.
/// See Docs/MIGRATION_TO_TRELLIS_V3.md (reg-003-no-result-unwrap) — when Trellis ships the
/// framework-level UnwrapOrThrow, delete this class and update call sites accordingly.
/// </summary>
/// <remarks>
/// Trellis V3 deliberately removed <c>Result&lt;T&gt;.Value</c> (was the root cause of analyzer
/// TRLS003: throwing-getter-on-success-track). Use Match/TryGetValue/Deconstruct/GetValueOrDefault
/// for normal call sites. This helper is for the legitimate
/// "the value is already validated; reconstruct or fail loudly" case — DTO/JSON
/// deserialization and tests that arrange known-valid inputs.
/// </remarks>
public static class TrellisResultExtensions
{
    /// <summary>
    /// Returns the success value, or throws <see cref="InvalidOperationException"/> if the result is a failure.
    /// The thrown message includes the value type and (optional) <paramref name="context"/>
    /// so the stack trace alone identifies the failing site.
    /// </summary>
    public static T UnwrapOrThrow<T>(this Result<T> result, string? context = null) =>
        result.Match(
            value => value,
            error => throw new InvalidOperationException(
                context is null
                    ? $"Cannot unwrap failed Result<{typeof(T).Name}>: {error}"
                    : $"Cannot unwrap failed Result<{typeof(T).Name}> ({context}): {error}"));
}
