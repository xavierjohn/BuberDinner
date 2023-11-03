namespace BuberDinner.Api._2022_12_21.Models.Menus;

using BuberDinner.Application.Menus.Commands;
using BuberDinner.Domain.Host.ValueObject;
using DescriptionClass = Domain.Common.ValueObjects.Description;
using NameClass = Domain.Common.ValueObjects.Name;

/// <summary>
/// Create menu request model
/// </summary>
/// <param name="Name">The menu name</param>
/// <param name="Description">The menu description</param>
/// <param name="Sections">List of menu sections</param>
public record CreateMenuRequest(
    string Name,
    string Description,
    List<MenuSectionRequest> Sections)
{
    internal Result<CreateMenuCommand> ToCreateMenuCommand(string hostId) =>
        NameClass.TryCreate(this.Name)
        .Combine(DescriptionClass.TryCreate(this.Description))
        .Combine(this.GetMenuSectionCommands())
        .Combine(HostId.TryCreate(hostId))
        .Bind(CreateMenuCommand.New);

    private Result<IReadOnlyList<MenuSectionCommand>> GetMenuSectionCommands() =>
        this.Sections
            .Select(ms => ms.ToMenuSectionCommand().Value)
            .ToList();
}

/// <summary>
/// Menu section model
/// </summary>
/// <param name="Name">The menu section name</param>
/// <param name="Description">The menu section description</param>
/// <param name="Items">List of menu items</param>
public record MenuSectionRequest(
    string Name,
    string Description,
    List<MenuItemRequest> Items)
{
    internal Result<MenuSectionCommand> ToMenuSectionCommand() =>
        NameClass.TryCreate(this.Name)
        .Combine(DescriptionClass.TryCreate(this.Description))
        .Combine(this.GetMenuItemCommands())
        .Bind(MenuSectionCommand.New);

    private Result<IReadOnlyList<MenuItemCommand>> GetMenuItemCommands() =>
        this.Items
            .Select(i => i.ToMenuItemCommand().Value)
            .ToList();
}

/// <summary>
/// Menu item model
/// </summary>
/// <param name="Name">The menu item name</param>
/// <param name="Description">The menu item description</param>
public record MenuItemRequest(
    string Name,
    string Description)
{
    internal Result<MenuItemCommand> ToMenuItemCommand() =>
        NameClass.TryCreate(this.Name)
        .Combine(DescriptionClass.TryCreate(this.Description))
        .Bind(MenuItemCommand.New);
}
