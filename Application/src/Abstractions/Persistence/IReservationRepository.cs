namespace BuberDinner.Application.Abstractions.Persistence;

using System.Collections.Generic;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.User.ValueObjects;

public interface IReservationRepository : IRepository<Reservation>
{
    IReadOnlyList<Reservation> GetPageForDinner(DinnerId dinnerId, Trellis.PageSize pageSize, System.Guid? afterId);

    IReadOnlyList<Reservation> GetPageForGuest(UserId guestUserId, Trellis.PageSize pageSize, System.Guid? afterId);
}
