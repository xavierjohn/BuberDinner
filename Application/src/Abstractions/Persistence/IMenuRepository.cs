namespace BuberDinner.Application.Abstractions.Persistence;

using System.Collections.Generic;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;

/// <summary>
/// Menu-specific repository surface. Same pattern as <see cref="IDinnerRepository"/>:
/// extends the generic <see cref="IRepository{T}"/> with per-host paginated reads so
/// the application layer doesn't have to leak persistence details.
/// </summary>
public interface IMenuRepository : IRepository<Menu>
{
    /// <summary>
    /// Over-fetched, host-filtered, id-ordered slice for cursor pagination. Same contract
    /// as <see cref="IDinnerRepository.GetPageForHost"/> — see that XML doc for invariants.
    /// </summary>
    IReadOnlyList<Menu> GetPageForHost(HostId hostId, Trellis.PageSize pageSize, System.Guid? afterId);
}
