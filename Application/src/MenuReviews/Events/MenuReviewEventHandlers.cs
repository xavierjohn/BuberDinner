namespace BuberDinner.Application.MenuReviews.Events;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Domain.MenuReview.Events;
using Microsoft.Extensions.Logging;
using Trellis.Mediator;

public sealed class LogMenuReviewSubmittedHandler : IDomainEventHandler<MenuReviewSubmitted>
{
    private readonly ILogger<LogMenuReviewSubmittedHandler> _logger;

    public LogMenuReviewSubmittedHandler(ILogger<LogMenuReviewSubmittedHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(MenuReviewSubmitted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Review {ReviewId} submitted by guest {GuestUserId} for menu {MenuId}: {Rating}/5 at {OccurredAt:o}",
            notification.ReviewId.Value, notification.GuestUserId.Value,
            notification.MenuId.Value, notification.Rating, notification.OccurredAt);
        return ValueTask.CompletedTask;
    }
}

public sealed class LogMenuReviewUpdatedHandler : IDomainEventHandler<MenuReviewUpdated>
{
    private readonly ILogger<LogMenuReviewUpdatedHandler> _logger;

    public LogMenuReviewUpdatedHandler(ILogger<LogMenuReviewUpdatedHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(MenuReviewUpdated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Review {ReviewId} updated to {NewRating}/5 at {OccurredAt:o}",
            notification.ReviewId.Value, notification.NewRating, notification.OccurredAt);
        return ValueTask.CompletedTask;
    }
}
