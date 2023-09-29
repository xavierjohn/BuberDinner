namespace BuberDinner.Domain.Tests;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.Entities;
using BuberDinner.Domain.Menu.ValueObject;
using FunctionalDDD.Results.Errors;

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
            : MenuItemId.New("2F45ACF9-6E51-4DC7-8732-DBE7F260E951").Value;
        Name? name = field == nameof(Name)
            ? default
            : Name.New("Item Name").Value;
        Description? description = field == nameof(Description)
            ? default
            : Description.New("Item Description").Value;

        // Act
        Result<MenuItem> menuItemResult = MenuItem.New(id!, name!, description!);

        // Assert
        menuItemResult.IsFailure.Should().BeTrue();
        menuItemResult.Error.Should().BeOfType<ValidationError>();
        ValidationError validationError = (ValidationError)menuItemResult.Error;
        validationError.Errors[0].Message.Should().EndWith($" must not be empty."); ;
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
            : MenuSectionId.New("2F45ACF9-6E51-4DC7-8732-DBE7F260E952").Value;
        Name? name = field == nameof(Name)
            ? default
            : Name.New("Section Name").Value;
        Description? description = field == nameof(Description)
            ? default
            : Description.New("Section Description").Value;
        IReadOnlyList<MenuItem> items = field == nameof(MenuSection.Items)
            ? new List<MenuItem>()
            : new List<MenuItem>()
            {
                MenuItem.New(
                    Name.New("Item Name").Value,
                    Description.New("Item Description").Value).Value
            };

        // Act
        Result<MenuSection> menuSectionResult = MenuSection.New(id!, name!, description!, items);

        // Assert
        menuSectionResult.IsFailure.Should().BeTrue();
        menuSectionResult.Error.Should().BeOfType<ValidationError>();
        ValidationError validationError = (ValidationError)menuSectionResult.Error;
        validationError.Errors[0].Message.Should().EndWith($" must not be empty."); ;
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
            : MenuId.New("2F45ACF9-6E51-4DC7-8732-DBE7F260E953").Value;
        Name? name = field == nameof(Name)
            ? default
            : Name.New("Menu Name").Value;
        Description? description = field == nameof(Description)
            ? default
            : Description.New("Menu Description").Value;
        IReadOnlyList<MenuSection> sections = field == nameof(Menu.Sections)
            ? new List<MenuSection>()
            : new List<MenuSection>()
            {
                MenuSection.New(
                    Name.New("Section Name").Value,
                    Description.New("Section Description").Value,
                    new List<MenuItem>()
                    {
                        MenuItem.New(
                            Name.New("Item Name").Value,
                            Description.New("Item Description").Value).Value
                    }).Value
            };
        HostId? hostId = field == nameof(HostId)
            ? default
            : HostId.New("2F45ACF9-6E51-4DC7-8732-DBE7F260E954").Value;

        // Act
        Result<Menu> menuResult = Menu.New(
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
        menuResult.Error.Should().BeOfType<ValidationError>();
        ValidationError validationError = (ValidationError)menuResult.Error;
        validationError.Errors[0].Message.Should().EndWith($" must not be empty."); ;
    }
}
