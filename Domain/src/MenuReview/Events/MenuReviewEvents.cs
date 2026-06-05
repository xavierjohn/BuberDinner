namespace BuberDinner.Domain.MenuReview.Events;

using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.ValueObject;
using BuberDinner.Domain.User.ValueObjects;

public sealed record MenuReviewSubmitted(
    MenuReviewId ReviewId,
    MenuId MenuId,
    DinnerId DinnerId,
    UserId GuestUserId,
    int Rating,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record MenuReviewUpdated(
    MenuReviewId ReviewId,
    int NewRating,
    DateTimeOffset OccurredAt) : IDomainEvent;
