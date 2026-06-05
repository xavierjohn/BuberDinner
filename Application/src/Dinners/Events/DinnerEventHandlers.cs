namespace BuberDinner.Application.Dinners.Events;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Domain.Dinner.Events;
using Trellis.Mediator;
using Microsoft.Extensions.Logging;

/// <summary>
/// Side-effect-only handler that logs <see cref="DinnerScheduled"/> events. The real-world
/// equivalent would publish to an outbox, notify guests, etc. — kept as a log line so the
/// showcase demonstrates the pipeline wiring without dragging in a notification stack.
/// </summary>
public sealed class LogDinnerScheduledHandler : IDomainEventHandler<DinnerScheduled>
{
    private readonly ILogger<LogDinnerScheduledHandler> _logger;

    public LogDinnerScheduledHandler(ILogger<LogDinnerScheduledHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(DinnerScheduled notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Dinner {DinnerId} scheduled for host {HostId} from {StartDateTime:o} to {EndDateTime:o}",
            notification.DinnerId.Value, notification.HostId.Value,
            notification.StartDateTime, notification.EndDateTime);
        return ValueTask.CompletedTask;
    }
}

/// <summary>Logs <see cref="DinnerStarted"/> events.</summary>
public sealed class LogDinnerStartedHandler : IDomainEventHandler<DinnerStarted>
{
    private readonly ILogger<LogDinnerStartedHandler> _logger;

    public LogDinnerStartedHandler(ILogger<LogDinnerStartedHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(DinnerStarted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Dinner {DinnerId} started at {OccurredAt:o}",
            notification.DinnerId.Value, notification.OccurredAt);
        return ValueTask.CompletedTask;
    }
}

/// <summary>Logs <see cref="DinnerEnded"/> events.</summary>
public sealed class LogDinnerEndedHandler : IDomainEventHandler<DinnerEnded>
{
    private readonly ILogger<LogDinnerEndedHandler> _logger;

    public LogDinnerEndedHandler(ILogger<LogDinnerEndedHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(DinnerEnded notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Dinner {DinnerId} ended at {OccurredAt:o}",
            notification.DinnerId.Value, notification.OccurredAt);
        return ValueTask.CompletedTask;
    }
}

/// <summary>Logs <see cref="DinnerCancelled"/> events, including the host-supplied reason.</summary>
public sealed class LogDinnerCancelledHandler : IDomainEventHandler<DinnerCancelled>
{
    private readonly ILogger<LogDinnerCancelledHandler> _logger;

    public LogDinnerCancelledHandler(ILogger<LogDinnerCancelledHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(DinnerCancelled notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Dinner {DinnerId} cancelled at {OccurredAt:o} — reason: {Reason}",
            notification.DinnerId.Value, notification.OccurredAt, notification.Reason);
        return ValueTask.CompletedTask;
    }
}


