namespace BuberDinner.Domain.Menu;

using System.Collections.Generic;
using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu.Entities;
using BuberDinner.Domain.Menu.ValueObject;
using FluentValidation;
public class Menu : AggregateRoot<MenuId>
{
    public Name Name { get; }
    public Description Description { get; }
    public decimal? AverageRating { get; }
    public IReadOnlyList<MenuSection> Sections => _menuSections.AsReadOnly();
    public HostId HostId { get; }
    public IReadOnlyList<DinnerId> DinnerIds => _dinnerIds.AsReadOnly();
    public IReadOnlyList<MenuReviewId> MenuReviewIds => _menuReviewIds.AsReadOnly();

    private readonly List<MenuSection> _menuSections = new();
    private readonly List<DinnerId> _dinnerIds = new();
    private readonly List<MenuReviewId> _menuReviewIds = new();

    public static Result<Menu> New(
        Name name,
        Description description,
        IReadOnlyList<MenuSection> sections,
        HostId host)
    {
        return New(
            MenuId.NewUnique(),
            name,
            description,
            null,
            sections,
            host,
            null,
            null);
    }

    public static Result<Menu> New(
        MenuId menuId,
        Name name,
        Description description,
        decimal? averageRating,
        IReadOnlyList<MenuSection> sections,
        HostId host,
        IReadOnlyList<DinnerId>? dinnerIds,
        IReadOnlyList<MenuReviewId>? menuReviewIds)
    {
        Menu menu = new(menuId, name, description, averageRating, host);
        menu._menuSections.AddRange(sections);

        if (dinnerIds is not null)
        {
            menu._dinnerIds.AddRange(dinnerIds);
        }

        if (menuReviewIds is not null)
        {
            menu._menuReviewIds.AddRange(menuReviewIds);
        }

        return s_validator.ValidateToResult(menu);
    }


    private Menu(
        MenuId menuId,
        Name name,
        Description description,
        decimal? averageRating,
        HostId hostId)
        : base(menuId)
    {
        Name = name;
        Description = description;
        AverageRating = averageRating;
        HostId = hostId;
    }

    static readonly InlineValidator<Menu> s_validator = new()
    {
        v => v.RuleFor(x => x.Name).NotEmpty(),
        v => v.RuleFor(x => x.Description).NotEmpty(),
        v => v.RuleFor(x => x.Sections).NotEmpty()
    };
}
