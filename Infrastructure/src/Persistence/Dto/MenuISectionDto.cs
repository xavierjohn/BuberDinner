namespace BuberDinner.Infrastructure.Persistence.Dto;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Menu.Entities;
using BuberDinner.Domain.Menu.ValueObject;

public class MenuSectionDto
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IList<MenuItemDto> Items { get; init; } = new List<MenuItemDto>();
}

public static class MenuSectionDtoExtensions
{
    public static MenuSectionDto ToDto(this MenuSection menuSection) =>
        new()
        {
            Id = menuSection.Id,
            Name = menuSection.Name,
            Description = menuSection.Description,
            Items = menuSection.Items.Select(item => item.ToDto()).ToList()
        };

    public static MenuSection? ToMenuSection(this MenuSectionDto? menuSectionDto) =>
        menuSectionDto is null
        ? null
        : MenuSection.TryCreate(
            MenuSectionId.TryCreate(menuSectionDto.Id).GetValueOrThrow(nameof(menuSectionDto.Id)),
            Name.TryCreate(menuSectionDto.Name).GetValueOrThrow(nameof(menuSectionDto.Name)),
            Description.TryCreate(menuSectionDto.Description).GetValueOrThrow(nameof(menuSectionDto.Description)),
            menuSectionDto.Items.Select(itemDto => itemDto.ToMenuItem()!).ToList()).GetValueOrThrow(nameof(MenuSection));
}
