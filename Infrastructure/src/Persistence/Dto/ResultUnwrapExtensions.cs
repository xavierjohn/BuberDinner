namespace BuberDinner.Infrastructure.Persistence.Dto;

internal static class ResultUnwrapExtensions
{
    // Local helper: Trellis.Core deliberately removed Result<T>.Value to prevent
    // accidental throwing-on-success-track (see win-004 / TRLS003). DTO reconstruction
    // from already-validated persisted data is the legitimate "cannot fail" case.
    // See reg-003-no-result-unwrap — when Trellis ships `Result<T>.UnwrapOrThrow()`,
    // delete this helper and inline the framework version.
    public static T UnwrapOrThrow<T>(this Result<T> result, string? context = null) =>
        result.Match(
            value => value,
            error => throw new InvalidOperationException(
                context is null
                    ? $"Cannot unwrap failed Result<{typeof(T).Name}>: {error}"
                    : $"Cannot unwrap failed Result<{typeof(T).Name}> ({context}): {error}"));
}
