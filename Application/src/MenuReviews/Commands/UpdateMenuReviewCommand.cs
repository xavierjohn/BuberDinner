namespace BuberDinner.Application.MenuReviews.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.MenuReview.Entities;
using BuberDinner.Domain.MenuReview.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

public sealed class UpdateMenuReviewCommand : ICommand<Result<MenuReview>>
{
    public MenuReviewId ReviewId { get; }
    public UserId CallerGuestUserId { get; }
    public int Rating { get; }
    public string Comment { get; }

    public UpdateMenuReviewCommand(MenuReviewId reviewId, UserId callerGuestUserId, int rating, string comment)
    {
        ReviewId = reviewId;
        CallerGuestUserId = callerGuestUserId;
        Rating = rating;
        Comment = comment;
    }
}

public sealed class UpdateMenuReviewCommandHandler
    : ICommandHandler<UpdateMenuReviewCommand, Result<MenuReview>>
{
    private readonly IMenuReviewRepository _repo;
    private readonly TimeProvider _clock;

    public UpdateMenuReviewCommandHandler(IMenuReviewRepository repo, TimeProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async ValueTask<Result<MenuReview>> Handle(
        UpdateMenuReviewCommand request, CancellationToken cancellationToken) =>
        await LoadReviewOwnedByAsync(request.ReviewId, request.CallerGuestUserId, cancellationToken)
            .BindAsync(review => review.UpdateContent(request.Rating, request.Comment, _clock))
            .TapAsync(review => _repo.Update(review, cancellationToken));

    private async ValueTask<Result<MenuReview>> LoadReviewOwnedByAsync(
        MenuReviewId reviewId, UserId callerGuestUserId, CancellationToken cancellationToken) =>
        (await _repo.FindById(reviewId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<MenuReview>(reviewId)))
            .Ensure(
                r => r.GuestUserId == callerGuestUserId,
                new Error.NotFound(ResourceRef.For<MenuReview>(reviewId))
                {
                    Detail = "Review does not belong to the calling guest.",
                });
}
