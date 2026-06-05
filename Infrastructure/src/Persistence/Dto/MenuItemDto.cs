namespace BuberDinner.Infrastructure.Persistence.Dto;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Menu.Entities;
using BuberDinner.Domain.Menu.ValueObject;

public class MenuItemDto
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public static class MenuItemDtoExtensions
{
    public static MenuItemDto ToDto(this MenuItem menuItem) =>
        new()
        {
            Id = menuItem.Id,
            Name = menuItem.Name,
            Description = menuItem.Description
        };

    public static MenuItem? ToMenuItem(this MenuItemDto? menuItemDto) =>
        menuItemDto is null
        ? null
        : MenuItem.TryCreate(
            MenuItemId.TryCreate(menuItemDto.Id).GetValueOrThrow(nameof(menuItemDto.Id)),
            Name.TryCreate(menuItemDto.Name).GetValueOrThrow(nameof(menuItemDto.Name)),
            Description.TryCreate(menuItemDto.Description).GetValueOrThrow(nameof(menuItemDto.Description))).GetValueOrThrow(nameof(MenuItem));
}
