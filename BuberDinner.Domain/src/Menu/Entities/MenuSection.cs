namespace BuberDinner.Domain.Menu.Entities;
using BuberDinner.Domain.Menu.ValueObject;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Errors;
using FluentValidation;

public class MenuSection : Entity<MenuSectionId>
{
    public string Name { get; }
    public string Description { get; }

    public IReadOnlyList<MenuItem> Items => _menuItems.AsReadOnly();

    private readonly List<MenuItem> _menuItems = new();

    public static Result<MenuSection, ErrorList> Create(string name, string description)
    {
        MenuSection menuItem = new(MenuSectionId.CreateUnique(), name, description);
        return s_validator.ValidateToResult(menuItem);
    }

    private MenuSection(MenuSectionId menuItemId, string name, string description)
    {
        Id = menuItemId;
        Name = name;
        Description = description;
    }

    static readonly InlineValidator<MenuSection> s_validator = new()
    {
        v => v.RuleFor(x => x.Name).NotEmpty(),
        v => v.RuleFor(x => x.Description).NotEmpty(),
    };
}
