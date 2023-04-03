namespace BuberDinner.Domain.Menu.Entities;
using BuberDinner.Domain.Menu.ValueObject;
using FluentValidation;

public class MenuItem : Entity<MenuItemId>
{
    public string Name { get; }
    public string Description { get; }

    public static Result<MenuItem> Create(string name, string description)
    {
        MenuItem menuItem = new(MenuItemId.NewUnique(), name, description);
        return s_validator.ValidateToResult(menuItem);
    }

    private MenuItem(MenuItemId menuItemId, string name, string description)
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
