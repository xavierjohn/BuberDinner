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
        TimeProvider clock)
    {
        var review = new MenuReview(
            MenuReviewId.NewUniqueV7(), menuId, dinnerId, guestUserId, rating, comment ?? string.Empty);

        var validation = s_validator.ValidateToResult(review);
        if (validation.IsFailure)
            return validation;

        review.DomainEvents.Add(new MenuReviewSubmitted(
            review.Id, review.MenuId, review.DinnerId, review.GuestUserId, review.Rating, clock.GetUtcNow()));
        return Result.Ok(review);
    }

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

    public Result<MenuReview> UpdateContent(int rating, string comment, TimeProvider clock)
    {
        var previousRating = Rating;
        var previousComment = Comment;
        Rating = rating;
        Comment = comment ?? string.Empty;

        var validation = s_validator.ValidateToResult(this);
        if (validation.IsFailure)
        {
            Rating = previousRating;
            Comment = previousComment;
            return validation;
        }

        DomainEvents.Add(new MenuReviewUpdated(Id, Rating, clock.GetUtcNow()));
        return Result.Ok(this);
    }

    static readonly InlineValidator<MenuReview> s_validator = new()
    {
        v => v.RuleFor(x => x.Id).NotEmpty(),
        v => v.RuleFor(x => x.MenuId).NotEmpty(),
        v => v.RuleFor(x => x.DinnerId).NotEmpty(),
        v => v.RuleFor(x => x.GuestUserId).NotEmpty(),
        v => v.RuleFor(x => x.Rating).InclusiveBetween(1, 5),
        v => v.RuleFor(x => x.Comment).NotEmpty().MaximumLength(1000),
    };
}
