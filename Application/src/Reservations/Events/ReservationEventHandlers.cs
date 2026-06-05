namespace BuberDinner.Application.Reservations.Events;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Domain.Reservation.Events;
using Microsoft.Extensions.Logging;
using Trellis.Mediator;

/// <summary>Logs <see cref="ReservationCreated"/> events. In a real deployment this would
/// fan out to the host's notifications channel ("Alice booked 2 seats for your Brunch").</summary>
public sealed class LogReservationCreatedHandler : IDomainEventHandler<ReservationCreated>
{
    private readonly ILogger<LogReservationCreatedHandler> _logger;

    public LogReservationCreatedHandler(ILogger<LogReservationCreatedHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(ReservationCreated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reservation {ReservationId} created by guest {GuestUserId} for dinner {DinnerId} ({GuestCount} seats) at {OccurredAt:o}",
            notification.ReservationId.Value, notification.GuestUserId.Value,
            notification.DinnerId.Value, notification.GuestCount, notification.OccurredAt);
        return ValueTask.CompletedTask;
    }
}

/// <summary>Logs <see cref="ReservationCancelled"/> events, including the guest-supplied reason.</summary>
public sealed class LogReservationCancelledHandler : IDomainEventHandler<ReservationCancelled>
{
    private readonly ILogger<LogReservationCancelledHandler> _logger;

    public LogReservationCancelledHandler(ILogger<LogReservationCancelledHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(ReservationCancelled notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reservation {ReservationId} cancelled by guest {GuestUserId} at {OccurredAt:o} — reason: {Reason}",
            notification.ReservationId.Value, notification.GuestUserId.Value,
            notification.OccurredAt, notification.Reason);
        return ValueTask.CompletedTask;
    }
}
