namespace BuberDinner.Infrastructure.Persistence.Dto;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.ValueObject;

public class MenuDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? AverageRating { get; set; } = null;
    public IList<MenuSectionDto> Sections { get; init; } = new List<MenuSectionDto>();
    public Guid HostId { get; set; } = Guid.Empty;
    public IList<Guid> DinnerIds { get; init; } = new List<Guid>();
    public IList<Guid> MenuReviewIds { get; init; } = new List<Guid>();
}

public static class MenuDtoExtensions
{
    public static MenuDto ToDto(this Menu menu) =>
        new()
        {
            Id = menu.Id.ToString(),
            Name = menu.Name,
            Description = menu.Description,
            AverageRating = menu.AverageRating,
            Sections = menu.Sections.Select(section => section.ToDto()).ToList(),
            HostId = menu.HostId,
            DinnerIds = menu.DinnerIds.Select(dinnerId => dinnerId.Value).ToList(),
            MenuReviewIds = menu.MenuReviewIds.Select(menuReviewId => menuReviewId.Value).ToList()
        };

    public static Menu? ToMenu(this MenuDto? menuDto) =>
        menuDto is null
        ? null
        : Menu.New(
            MenuId.New(Guid.Parse(menuDto.Id)).Value,
            Name.New(menuDto.Name).Value,
            Description.New(menuDto.Description).Value,
            menuDto.AverageRating,
            menuDto.Sections.Select(sectionDto => sectionDto.ToMenuSection()!).ToList(),
            HostId.New(menuDto.HostId).Value,
            menuDto.DinnerIds.Select(dinnerId => DinnerId.New(dinnerId).Value).ToList(),
            menuDto.MenuReviewIds.Select(menuReviewId => MenuReviewId.New(menuReviewId).Value).ToList()).Value;
}
