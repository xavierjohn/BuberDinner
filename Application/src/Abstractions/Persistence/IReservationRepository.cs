namespace BuberDinner.Application.Abstractions.Persistence;

using System.Collections.Generic;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.User.ValueObjects;

/// <summary>
/// Reservation-specific repository surface. Adds two per-relationship paginated reads to
/// the generic <see cref="IRepository{T}"/>: one scoped to a Dinner (host's view of who's
/// coming), one scoped to a Guest (the guest's view of their own reservations).
/// </summary>
public interface IReservationRepository : IRepository<Reservation>
{
    /// <summary>
    /// Over-fetched, dinner-filtered, id-ordered slice. Same shape as
    /// <see cref="IDinnerRepository.GetPageForHost"/>. The host's view of dinner attendance.
    /// </summary>
    IReadOnlyList<Reservation> GetPageForDinner(DinnerId dinnerId, Trellis.PageSize pageSize, System.Guid? afterId);

    /// <summary>
    /// Over-fetched, guest-filtered, id-ordered slice. The guest's view of their own
    /// reservations across every dinner.
    /// </summary>
    IReadOnlyList<Reservation> GetPageForGuest(UserId guestUserId, Trellis.PageSize pageSize, System.Guid? afterId);
}
