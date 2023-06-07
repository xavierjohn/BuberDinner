namespace BuberDinner.Domain.Menu.Entities;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Menu.ValueObject;
using FluentValidation;

public class MenuItem : Entity<MenuItemId>
{
    public Name Name { get; }
    public Description Description { get; }

    public static Result<MenuItem> New(Name name, Description description)
    {
        return New(MenuItemId.NewUnique(), name, description);
    }

    public static Result<MenuItem> New(MenuItemId menuItemId, Name name, Description description)
    {
        MenuItem menuItem = new(menuItemId, name, description);
        return s_validator.ValidateToResult(menuItem);
    }

    private MenuItem(MenuItemId menuItemId, Name name, Description description)
        : base(menuItemId)
    {
        Name = name;
        Description = description;
    }

    static readonly InlineValidator<MenuItem> s_validator = new()
    {
        v => v.RuleFor(x => x.Name).NotEmpty(),
        v => v.RuleFor(x => x.Description).NotEmpty(),
    };
}
