namespace BuberDinner.Api._2022_12_21.Models.Menus;

using BuberDinner.Application.Menus.Commands;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu.ValueObject;
using DescriptionClass = BuberDinner.Domain.Common.ValueObjects.Description;
using NameClass = BuberDinner.Domain.Common.ValueObjects.Name;

/// <summary>
/// Update-menu request body (PUT /hosts/{hostId}/menus/{menuId}).
/// </summary>
public record UpdateMenuRequest(string Name, string Description)
{
    /// <summary>
    /// Validates the request fields and lifts them into an <see cref="UpdateMenuCommand"/>
    /// carrying the parsed <c>If-Match</c> tokens for the handler to enforce.
    /// </summary>
    public Result<UpdateMenuCommand> ToUpdateMenuCommand(HostId hostId, MenuId menuId, EntityTagValue[]? ifMatch) =>
        NameClass.TryCreate(this.Name)
            .Combine(DescriptionClass.TryCreate(this.Description))
            .Bind((name, description) => UpdateMenuCommand.TryCreate(hostId, menuId, name, description, ifMatch));
}
