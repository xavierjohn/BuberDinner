namespace BuberDinner.Domain.Tests;

using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Entities;
using BuberDinner.Domain.MenuReview.Events;
using BuberDinner.Domain.User.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Trellis.Testing;

public class MenuReviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 18, 30, 0, TimeSpan.Zero);
    private static readonly MenuId Menu = MenuId.NewUniqueV7();
    private static readonly DinnerId Dinner = DinnerId.NewUniqueV7();
    private static readonly UserId Guest = UserId.TryCreate("guest_42").GetValueOrThrow();

    private static FakeTimeProvider Clock() => new(Now);

    [Fact]
    public void TryCreate_with_valid_rating_and_comment_succeeds_and_raises_event()
    {
        var result = MenuReview.TryCreate(Menu, Dinner, Guest, rating: 4, comment: "Great brunch", Clock());

        var review = result.Should().BeSuccess().Which;
        review.Rating.Should().Be(4);
        review.Comment.Should().Be("Great brunch");

        var ev = review.UncommittedEvents().Should().ContainSingle().Which.Should().BeOfType<MenuReviewSubmitted>().Subject;
        ev.OccurredAt.Should().Be(Now);
        ev.Rating.Should().Be(4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    public void TryCreate_rejects_out_of_range_rating(int rating)
    {
        var result = MenuReview.TryCreate(Menu, Dinner, Guest, rating, "ok", Clock());

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void TryCreate_rejects_empty_comment()
    {
        var result = MenuReview.TryCreate(Menu, Dinner, Guest, rating: 4, comment: "", Clock());

        result.Should().BeFailureOfType<Error.InvalidInput>();
    }

    [Fact]
    public void UpdateContent_succeeds_and_raises_MenuReviewUpdated()
    {
        var clock = Clock();
        var review = MenuReview.TryCreate(Menu, Dinner, Guest, 3, "okay", clock).Unwrap();
        review.AcceptChanges();
        clock.Advance(TimeSpan.FromMinutes(5));

        var result = review.UpdateContent(rating: 5, comment: "Actually amazing", clock);

        result.Should().BeSuccess();
        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Actually amazing");
        var ev = review.UncommittedEvents().Should().ContainSingle().Which.Should().BeOfType<MenuReviewUpdated>().Subject;
        ev.NewRating.Should().Be(5);
        ev.OccurredAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void UpdateContent_with_invalid_rating_rolls_back_and_keeps_previous_state()
    {
        var clock = Clock();
        var review = MenuReview.TryCreate(Menu, Dinner, Guest, 3, "okay", clock).Unwrap();
        review.AcceptChanges();

        var result = review.UpdateContent(rating: 99, comment: "rolled-back comment", clock);

        result.Should().BeFailureOfType<Error.InvalidInput>();
        review.Rating.Should().Be(3, "rejected update must not mutate state");
        review.Comment.Should().Be("okay");
        review.UncommittedEvents().Should().BeEmpty("rejected update must not raise events");
    }
}
