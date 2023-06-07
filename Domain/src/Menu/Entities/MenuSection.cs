namespace BuberDinner.Domain.Menu.Entities;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Menu.ValueObject;
using FluentValidation;

public class MenuSection : Entity<MenuSectionId>
{
    public Name Name { get; }
    public Description Description { get; }

    public IReadOnlyList<MenuItem> Items => _menuItems.AsReadOnly();

    private readonly List<MenuItem> _menuItems = new();

    public static Result<MenuSection> New(
        Name name,
        Description description,
        IReadOnlyList<MenuItem> items)
    {
        return New(MenuSectionId.NewUnique(), name, description, items);
    }

    public static Result<MenuSection> New(
        MenuSectionId menuSectionId,
        Name name,
        Description description,
        IReadOnlyList<MenuItem> items)
    {
        MenuSection menuSection = new(menuSectionId, name, description);
        menuSection._menuItems.AddRange(items);
        return s_validator.ValidateToResult(menuSection);
    }

    private MenuSection(MenuSectionId menuItemId, Name name, Description description)
        : base(menuItemId)
    {
        Name = name;
        Description = description;
    }

    static readonly InlineValidator<MenuSection> s_validator = new()
    {
        v => v.RuleFor(x => x.Name).NotEmpty(),
        v => v.RuleFor(x => x.Description).NotEmpty(),
    };
}
