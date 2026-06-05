namespace BuberDinner.Application.MenuReviews.Commands;

using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Entities;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

public sealed class SubmitMenuReviewCommand : ICommand<Result<MenuReview>>
{
    public MenuId MenuId { get; }
    public DinnerId DinnerId { get; }
    public UserId GuestUserId { get; }
    public int Rating { get; }
    public string Comment { get; }

    public SubmitMenuReviewCommand(MenuId menuId, DinnerId dinnerId, UserId guestUserId, int rating, string comment)
    {
        MenuId = menuId;
        DinnerId = dinnerId;
        GuestUserId = guestUserId;
        Rating = rating;
        Comment = comment;
    }
}
