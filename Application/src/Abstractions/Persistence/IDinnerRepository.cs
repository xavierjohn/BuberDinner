namespace BuberDinner.Application.Abstractions.Persistence;

using System.Collections.Generic;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Host.ValueObject;

/// <summary>
/// Dinner-specific repository surface. Extends the generic <see cref="IRepository{T}"/>
/// with per-host primitives so the application layer doesn't have to depend on any
/// particular persistence implementation (or filter in memory after a full load).
/// </summary>
public interface IDinnerRepository : IRepository<Dinner>
{
    /// <summary>
    /// Returns every dinner owned by the supplied host, ordered by <see cref="Dinner.Id"/>
    /// (V7 GUIDs sort chronologically by creation time).
    /// </summary>
    IReadOnlyList<Dinner> GetForHost(HostId hostId);

    /// <summary>
    /// Returns an over-fetched, ordered, host-filtered slice for cursor-based pagination.
    /// The handler in <c>ListDinnersForHostQueryHandler</c> passes the result to
    /// <c>PageBuilder.FromOverFetch(...)</c>, which trims to <paramref name="pageSize"/>
    /// items and emits a <see cref="System.Guid"/>-encoded <c>Next</c> cursor when more
    /// rows exist beyond the page. Implementations MUST:
    ///   1. Order by <see cref="Dinner.Id"/> ascending (V7 GUIDs sort chronologically).
    ///   2. Filter to <paramref name="hostId"/> BEFORE seeking — every cursor seek is
    ///      scoped to the same host so a cross-page navigation cannot leak rows from a
    ///      different host even if a caller hands a stolen cursor to the wrong route host.
    ///   3. Apply the seek predicate <c>Id.Value &gt; afterId</c> when <paramref name="afterId"/>
    ///      is non-null (i.e. caller is on page 2+).
    ///   4. Take at most <c>pageSize.Applied + 1</c> rows so the page builder can detect
    ///      "is there a next page?" without an extra COUNT(*) query.
    /// </summary>
    IReadOnlyList<Dinner> GetPageForHost(HostId hostId, Trellis.PageSize pageSize, System.Guid? afterId);
}
