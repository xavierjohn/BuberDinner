namespace BuberDinner.Application.MenuReviews.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Entities;
using Mediator;

public sealed class SubmitMenuReviewCommandHandler
    : ICommandHandler<SubmitMenuReviewCommand, Result<MenuReview>>
{
    private readonly IMenuReviewRepository _reviewRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly TimeProvider _clock;

    public SubmitMenuReviewCommandHandler(
        IMenuReviewRepository reviewRepository,
        IRepository<Menu> menuRepository,
        TimeProvider clock)
    {
        _reviewRepository = reviewRepository;
        _menuRepository = menuRepository;
        _clock = clock;
    }

    public async ValueTask<Result<MenuReview>> Handle(
        SubmitMenuReviewCommand request, CancellationToken cancellationToken) =>
        await LoadMenuAsync(request.MenuId, cancellationToken)
            .BindAsync(_ => MenuReview.TryCreate(
                request.MenuId, request.DinnerId, request.GuestUserId,
                request.Rating, request.Comment, _clock))
            .TapAsync(review => _reviewRepository.Add(review, cancellationToken));

    private async ValueTask<Result<Menu>> LoadMenuAsync(MenuId menuId, CancellationToken cancellationToken) =>
        (await _menuRepository.FindById(menuId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Menu>(menuId)));
}
