namespace BuberDinner.Domain.Tests;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.Entities;
using BuberDinner.Domain.Menu.ValueObject;

public class MenuTests
{
    [Theory]
    [InlineData(nameof(MenuItem.Id))]
    [InlineData(nameof(MenuItem.Name))]
    [InlineData(nameof(MenuItem.Description))]
    public void MenuItem_Required_parameters_are_validated(string field)
    {
        // Arrange
        MenuItemId? id = field == nameof(MenuItem.Id)
            ? default
            : MenuItemId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E951").UnwrapOrThrow();
        Name? name = field == nameof(Name)
            ? default
            : Name.TryCreate("Item Name").UnwrapOrThrow();
        Description? description = field == nameof(Description)
            ? default
            : Description.TryCreate("Item Description").UnwrapOrThrow();

        // Act
        Result<MenuItem> menuItemResult = MenuItem.TryCreate(id!, name!, description!);

        // Assert
        menuItemResult.IsFailure.Should().BeTrue();
        menuItemResult.Error.Should().BeOfType<Error.InvalidInput>();
        Error.InvalidInput invalidInput = (Error.InvalidInput)menuItemResult.Error!;
        invalidInput.Fields.Items[0].Detail.Should().EndWith($" must not be empty.");
    }

    [Theory]
    [InlineData(nameof(MenuSection.Id))]
    [InlineData(nameof(MenuSection.Name))]
    [InlineData(nameof(MenuSection.Description))]
    [InlineData(nameof(MenuSection.Items))]
    public void MenuSection_Required_parameters_are_validated(string field)
    {
        // Arrange
        MenuSectionId? id = field == nameof(MenuSection.Id)
            ? default
            : MenuSectionId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E952").UnwrapOrThrow();
        Name? name = field == nameof(Name)
            ? default
            : Name.TryCreate("Section Name").UnwrapOrThrow();
        Description? description = field == nameof(Description)
            ? default
            : Description.TryCreate("Section Description").UnwrapOrThrow();
        IReadOnlyList<MenuItem> items = field == nameof(MenuSection.Items)
            ? new List<MenuItem>()
            : new List<MenuItem>()
            {
                MenuItem.TryCreate(
                    Name.TryCreate("Item Name").UnwrapOrThrow(),
                    Description.TryCreate("Item Description").UnwrapOrThrow()).UnwrapOrThrow()
            };

        // Act
        Result<MenuSection> menuSectionResult = MenuSection.TryCreate(id!, name!, description!, items);

        // Assert
        menuSectionResult.IsFailure.Should().BeTrue();
        menuSectionResult.Error.Should().BeOfType<Error.InvalidInput>();
        Error.InvalidInput invalidInput = (Error.InvalidInput)menuSectionResult.Error!;
        invalidInput.Fields.Items[0].Detail.Should().EndWith($" must not be empty.");
    }

    [Theory]
    [InlineData(nameof(Menu.Id))]
    [InlineData(nameof(Menu.Name))]
    [InlineData(nameof(Menu.Description))]
    [InlineData(nameof(Menu.Sections))]
    [InlineData(nameof(Menu.HostId))]
    public void Menu_Required_parameters_are_validated(string field)
    {
        // Arrange
        MenuId? id = field == nameof(Menu.Id)
            ? default
            : MenuId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E953").UnwrapOrThrow();
        Name? name = field == nameof(Name)
            ? default
            : Name.TryCreate("Menu Name").UnwrapOrThrow();
        Description? description = field == nameof(Description)
            ? default
            : Description.TryCreate("Menu Description").UnwrapOrThrow();
        IReadOnlyList<MenuSection> sections = field == nameof(Menu.Sections)
            ? new List<MenuSection>()
            : new List<MenuSection>()
            {
                MenuSection.TryCreate(
                    Name.TryCreate("Section Name").UnwrapOrThrow(),
                    Description.TryCreate("Section Description").UnwrapOrThrow(),
                    new List<MenuItem>()
                    {
                        MenuItem.TryCreate(
                            Name.TryCreate("Item Name").UnwrapOrThrow(),
                            Description.TryCreate("Item Description").UnwrapOrThrow()).UnwrapOrThrow()
                    }).UnwrapOrThrow()
            };
        HostId? hostId = field == nameof(HostId)
            ? default
            : HostId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E954").UnwrapOrThrow();

        // Act
        Result<Menu> menuResult = Menu.TryCreate(
            id!,
            name!,
            description!,
            null,
            sections,
            hostId!,
            null,
            null);

        // Assert
        menuResult.IsFailure.Should().BeTrue();
        menuResult.Error.Should().BeOfType<Error.InvalidInput>();
        Error.InvalidInput invalidInput = (Error.InvalidInput)menuResult.Error!;
        invalidInput.Fields.Items[0].Detail.Should().EndWith($" must not be empty.");
    }
}
