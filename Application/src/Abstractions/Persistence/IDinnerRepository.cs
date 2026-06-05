namespace BuberDinner.Application.Abstractions.Persistence;

using System.Collections.Generic;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Host.ValueObject;

/// <summary>
/// Dinner-specific repository surface. Extends the generic <see cref="IRepository{T}"/>
/// with a per-host listing primitive so the application layer doesn't have to depend on
/// any particular persistence implementation (or filter in memory after a full load).
/// </summary>
/// <remarks>
/// PR 2 ships the in-memory implementation only; PR 3 will introduce a paginated
/// equivalent (<c>Page&lt;Dinner&gt;</c> + <c>Cursor</c>) and the EF Core implementation.
/// </remarks>
public interface IDinnerRepository : IRepository<Dinner>
{
    IReadOnlyList<Dinner> GetForHost(HostId hostId);
}
