namespace BuberDinner.Domain.MenuReview.Entities;

using System;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Events;
using BuberDinner.Domain.MenuReview.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using FluentValidation;

public sealed class MenuReview : Aggregate<MenuReviewId>
{
    public MenuId MenuId { get; }
    public DinnerId DinnerId { get; }
    public UserId GuestUserId { get; }
    public int Rating { get; private set; }
    public string Comment { get; private set; }

    public static Result<MenuReview> TryCreate(
        MenuId menuId,
        DinnerId dinnerId,
        UserId guestUserId,
        int rating,
        string comment,
        TimeProvider clock) =>
        s_inputValidator.ValidateToResult(new ContentInputs(rating, comment ?? string.Empty))
            .Map(inputs =>
            {
                var review = new MenuReview(
                    MenuReviewId.NewUniqueV7(), menuId, dinnerId, guestUserId,
                    inputs.Rating, inputs.Comment);
                review.DomainEvents.Add(new MenuReviewSubmitted(
                    review.Id, review.MenuId, review.DinnerId, review.GuestUserId,
                    review.Rating, clock.GetUtcNow()));
                return review;
            });

    private MenuReview(
        MenuReviewId id, MenuId menuId, DinnerId dinnerId, UserId guestUserId,
        int rating, string comment)
        : base(id)
    {
        MenuId = menuId;
        DinnerId = dinnerId;
        GuestUserId = guestUserId;
        Rating = rating;
        Comment = comment;
    }

    public Result<MenuReview> UpdateContent(int rating, string comment, TimeProvider clock) =>
        s_inputValidator.ValidateToResult(new ContentInputs(rating, comment ?? string.Empty))
            .Map(inputs =>
            {
                Rating = inputs.Rating;
                Comment = inputs.Comment;
                DomainEvents.Add(new MenuReviewUpdated(Id, Rating, clock.GetUtcNow()));
                return this;
            });

    private sealed record ContentInputs(int Rating, string Comment);

    static readonly InlineValidator<ContentInputs> s_inputValidator = new()
    {
        v => v.RuleFor(x => x.Rating)
              .InclusiveBetween(1, 5)
              .WithMessage("Rating must be between 1 and 5."),
        v => v.RuleFor(x => x.Comment)
              .NotEmpty().WithMessage("Comment is required.")
              .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters."),
    };
}
