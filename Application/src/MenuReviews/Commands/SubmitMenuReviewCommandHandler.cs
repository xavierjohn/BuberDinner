namespace BuberDinner.Application.MenuReviews.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Entities;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.Reservation.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

public sealed class SubmitMenuReviewCommandHandler
    : ICommandHandler<SubmitMenuReviewCommand, Result<MenuReview>>
{
    private readonly IMenuReviewRepository _reviewRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly IRepository<Dinner> _dinnerRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly TimeProvider _clock;

    public SubmitMenuReviewCommandHandler(
        IMenuReviewRepository reviewRepository,
        IRepository<Menu> menuRepository,
        IRepository<Dinner> dinnerRepository,
        IReservationRepository reservationRepository,
        TimeProvider clock)
    {
        _reviewRepository = reviewRepository;
        _menuRepository = menuRepository;
        _dinnerRepository = dinnerRepository;
        _reservationRepository = reservationRepository;
        _clock = clock;
    }

    public async ValueTask<Result<MenuReview>> Handle(
        SubmitMenuReviewCommand request, CancellationToken cancellationToken) =>
        await LoadMenuAsync(request.MenuId, cancellationToken)
            .BindAsync(_ => LoadDinnerAsync(request.DinnerId, cancellationToken))
            .EnsureAsync(
                d => d.MenuId == request.MenuId,
                new Error.NotFound(ResourceRef.For<Dinner>(request.DinnerId))
                {
                    Detail = "Dinner is not associated with the specified menu.",
                })
            .BindAsync(d => EnsureCallerReservedAsync(d, request.GuestUserId, cancellationToken))
            .EnsureAsync(
                d => d.Status == DinnerStatus.Ended,
                d => Error.InvalidInput.ForRule("review.dinner-not-ended",
                    $"Cannot review a dinner whose status is {d.Status.Value}. Only Ended dinners can be reviewed."))
            .BindAsync(_ => MenuReview.TryCreate(
                request.MenuId, request.DinnerId, request.GuestUserId,
                request.Rating, request.Comment, _clock))
            .TapAsync(review => _reviewRepository.Add(review, cancellationToken));

    private ValueTask<Result<Menu>> LoadMenuAsync(MenuId menuId, CancellationToken cancellationToken) =>
        _menuRepository.FindById(menuId.Value.ToString(), cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Menu>(menuId)));

    private ValueTask<Result<Dinner>> LoadDinnerAsync(DinnerId dinnerId, CancellationToken cancellationToken) =>
        _dinnerRepository.FindById(dinnerId.Value.ToString(), cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Dinner>(dinnerId)));

    private ValueTask<Result<Dinner>> EnsureCallerReservedAsync(
        Dinner dinner, UserId guestUserId, CancellationToken cancellationToken) =>
        _reservationRepository.FindByDinnerAndGuest(dinner.Id, guestUserId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<Dinner>(dinner.Id))
            {
                Detail = "Caller did not reserve this dinner.",
            })
            .EnsureAsync(
                r => r.Status == ReservationStatus.Reserved,
                new Error.NotFound(ResourceRef.For<Dinner>(dinner.Id))
                {
                    Detail = "Caller did not reserve this dinner.",
                })
            .MapAsync(_ => dinner);
}
