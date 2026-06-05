namespace BuberDinner.Application.Reservations.Events;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Domain.Reservation.Events;
using Microsoft.Extensions.Logging;
using Trellis.Mediator;

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
