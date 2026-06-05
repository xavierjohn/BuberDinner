namespace BuberDinner.Api._2022_12_21.Models.Dinners;

using System;
using BuberDinner.Application.Dinners.Commands;
using BuberDinner.Domain.Host.ValueObject;
using DescriptionClass = BuberDinner.Domain.Common.ValueObjects.Description;
using MenuIdClass = BuberDinner.Domain.Menu.ValueObject.MenuId;
using NameClass = BuberDinner.Domain.Common.ValueObjects.Name;

/// <summary>Schedule-dinner request body (POST /hosts/{hostId}/dinners).</summary>
public record ScheduleDinnerRequest(
    string Name,
    string Description,
    string MenuId,
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime)
{
    /// <summary>Validates the request fields and lifts them into a <see cref="ScheduleDinnerCommand"/>.</summary>
    public Result<ScheduleDinnerCommand> ToScheduleDinnerCommand(HostId hostId) =>
        NameClass.TryCreate(this.Name)
            .Combine(DescriptionClass.TryCreate(this.Description))
            .Combine(MenuIdClass.TryCreate(this.MenuId))
            .Bind((name, description, menuId) =>
                ScheduleDinnerCommand.TryCreate(
                    hostId, menuId, name, description,
                    this.StartDateTime, this.EndDateTime));
}

/// <summary>Cancel-dinner request body (POST /hosts/{hostId}/dinners/{dinnerId}/cancel).</summary>
public record CancelDinnerRequest(string Reason);
