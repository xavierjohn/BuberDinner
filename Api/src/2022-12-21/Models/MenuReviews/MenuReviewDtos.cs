namespace BuberDinner.Api._2022_12_21.Models.MenuReviews;

using System;
using BuberDinner.Application.MenuReviews.Commands;
using BuberDinner.Domain.MenuReview.Entities;
using BuberDinner.Domain.User.ValueObjects;
using Mapster;
using DinnerIdClass = BuberDinner.Domain.Dinner.ValueObject.DinnerId;
using MenuIdClass = BuberDinner.Domain.Menu.ValueObject.MenuId;

/// <summary>Wire shape for a menu review.</summary>
public record MenuReviewResponse(
    string Id,
    string MenuId,
    string DinnerId,
    string GuestUserId,
    int Rating,
    string Comment);

/// <summary>POST /menu-reviews body.</summary>
public record SubmitMenuReviewRequest(string MenuId, string DinnerId, int Rating, string Comment)
{
    /// <summary>Lifts the request into a validated command via the Result pipeline.</summary>
    public Result<SubmitMenuReviewCommand> ToSubmitMenuReviewCommand(UserId guestUserId) =>
        MenuIdClass.TryCreate(this.MenuId, nameof(MenuId))
            .Combine(DinnerIdClass.TryCreate(this.DinnerId, nameof(DinnerId)))
            .Map((menuId, dinnerId) =>
                new SubmitMenuReviewCommand(menuId, dinnerId, guestUserId, this.Rating, this.Comment));
}

/// <summary>PUT /menu-reviews/{id} body.</summary>
public record UpdateMenuReviewRequest(int Rating, string Comment);

/// <summary>Mapster registration for MenuReview → MenuReviewResponse.</summary>
public sealed class MenuReviewMappingConfig : IRegister
{
    /// <inheritdoc />
    public void Register(TypeAdapterConfig config) =>
        config.NewConfig<MenuReview, MenuReviewResponse>()
            .Map(dest => dest.Id, src => src.Id.Value.ToString())
            .Map(dest => dest.MenuId, src => src.MenuId.Value.ToString())
            .Map(dest => dest.DinnerId, src => src.DinnerId.Value.ToString())
            .Map(dest => dest.GuestUserId, src => src.GuestUserId.Value);
}
