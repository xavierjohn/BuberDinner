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
        : MenuItem.New(
            MenuItemId.New(menuItemDto.Id).Value,
            Name.New(menuItemDto.Name).Value,
            Description.New(menuItemDto.Description).Value).Value;
}
