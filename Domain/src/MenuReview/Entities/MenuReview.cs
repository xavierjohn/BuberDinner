namespace BuberDinner.Domain.MenuReview.Entities;

using System;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Events;
using BuberDinner.Domain.MenuReview.ValueObject;
using BuberDinner.Domain.User.ValueObjects;

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
        ValidateContentInputs(rating, comment)
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
        ValidateContentInputs(rating, comment)
            .Map(inputs =>
            {
                Rating = inputs.Rating;
                Comment = inputs.Comment;
                DomainEvents.Add(new MenuReviewUpdated(Id, Rating, clock.GetUtcNow()));
                return this;
            });

    private static Result<(int Rating, string Comment)> ValidateContentInputs(int rating, string comment) =>
        Result.Ok((Rating: rating, Comment: comment ?? string.Empty))
            .Ensure(t => t.Rating is >= 1 and <= 5,
                Error.InvalidInput.ForField("rating", "menu-review.invalid.rating",
                    "Rating must be between 1 and 5."))
            .Ensure(t => !string.IsNullOrWhiteSpace(t.Comment),
                Error.InvalidInput.ForField("comment", "menu-review.invalid.comment-required",
                    "Comment is required."))
            .Ensure(t => t.Comment.Length <= 1000,
                Error.InvalidInput.ForField("comment", "menu-review.invalid.comment-too-long",
                    "Comment must not exceed 1000 characters."));
}
