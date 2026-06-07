namespace BuberDinner.Infrastructure.Persistence.Memory;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.User.ValueObjects;

internal sealed class ReservationInMemoryRepository : IReservationRepository
{
    private static readonly List<Reservation> s_reservations = new();
    private static readonly ConcurrentDictionary<string, string> s_etags = new(StringComparer.Ordinal);
    private static readonly object s_lock = new();

    public IEnumerable<Reservation> GetAll(CancellationToken cancellationToken)
    {
        lock (s_lock)
            return s_reservations.ToList();
    }

    public ValueTask Add(Reservation reservation, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            s_reservations.Add(reservation);
            BumpETag(reservation);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask Update(Reservation reservation, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            int index = s_reservations.FindIndex(r => r.Id == reservation.Id);
            if (index < 0)
                s_reservations.Add(reservation);
            else
                s_reservations[index] = reservation;
            BumpETag(reservation);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(Reservation reservation, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            s_reservations.RemoveAll(r => r.Id == reservation.Id);
            s_etags.TryRemove(reservation.Id.Value.ToString(), out _);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<Maybe<Reservation>> FindById(string id, CancellationToken cancellationToken)
    {
        Reservation? reservation;
        lock (s_lock)
            reservation = s_reservations.SingleOrDefault(r => r.Id.Value.ToString() == id);
        if (reservation is not null && s_etags.TryGetValue(id, out var etag))
            AggregateETagWriter.SetETag(reservation, etag);
        return ValueTask.FromResult(Maybe.From(reservation));
    }

    public IReadOnlyList<Reservation> GetPageForDinner(DinnerId dinnerId, Trellis.PageSize pageSize, System.Guid? afterId)
    {
        lock (s_lock)
        {
            IEnumerable<Reservation> source = s_reservations
                .Where(r => r.DinnerId == dinnerId)
                .OrderBy(r => r.Id.Value);
            if (afterId is { } cursorId)
                source = source.Where(r => r.Id.Value.CompareTo(cursorId) > 0);
            return source.Take(pageSize.Applied + 1).ToList();
        }
    }

    public IReadOnlyList<Reservation> GetPageForGuest(UserId guestUserId, Trellis.PageSize pageSize, System.Guid? afterId)
    {
        lock (s_lock)
        {
            IEnumerable<Reservation> source = s_reservations
                .Where(r => r.GuestUserId == guestUserId)
                .OrderBy(r => r.Id.Value);
            if (afterId is { } cursorId)
                source = source.Where(r => r.Id.Value.CompareTo(cursorId) > 0);
            return source.Take(pageSize.Applied + 1).ToList();
        }
    }

    public ValueTask<Maybe<Reservation>> FindByDinnerAndGuest(DinnerId dinnerId, UserId guestUserId, CancellationToken cancellationToken)
    {
        Reservation? reservation;
        lock (s_lock)
            reservation = s_reservations.FirstOrDefault(r =>
                r.DinnerId == dinnerId && r.GuestUserId == guestUserId);
        return ValueTask.FromResult(Maybe.From(reservation));
    }

    private static void BumpETag(Reservation reservation)
    {
        var newETag = Guid.NewGuid().ToString("N");
        s_etags[reservation.Id.Value.ToString()] = newETag;
        AggregateETagWriter.SetETag(reservation, newETag);
    }
}
