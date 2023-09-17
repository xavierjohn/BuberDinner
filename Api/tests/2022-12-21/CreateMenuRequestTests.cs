namespace BuberDinner.Api.Tests._2022_12_21;

using BuberDinner.Api._2022_12_21.Models.Menus;
using BuberDinner.Application.Menus.Commands;
using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Host.ValueObject;
using FunctionalDDD.RailwayOrientedProgramming.Errors;

public class CreateMenuRequestTests
{
    [Theory]
    [InlineData(nameof(Name))]
    [InlineData(nameof(Description))]
    [InlineData(nameof(HostId))]
    public void ToCreateMenuCommand_Required_parameters_are_validated(string field)
    {
        // Arrange
        string hostId = field == nameof(HostId) ? "Not really a guid" : "62DB3B1E-B53A-4494-9462-B220B0A83A4B";
        CreateMenuRequest request = new(
            field == nameof(Name) ? string.Empty : "Muffins 'R' Us",
            field == nameof(Description) ? string.Empty : "Menu for Muffins 'R' Us",
            new List<MenuSectionRequest>());

        // Act
        Result<CreateMenuCommand> result = request.ToCreateMenuCommand(hostId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType(typeof(ValidationError));
    }

    [Theory]
    [InlineData(nameof(Name))]
    [InlineData(nameof(Description))]
    public void ToMenuSectionCommand_Required_parameters_are_validated(string field)
    {
        // Arrange
        MenuSectionRequest request = new(
            field == nameof(Name) ? string.Empty : "Muffins",
            field == nameof(Description) ? string.Empty : "The Muffins section",
            new List<MenuItemRequest>());

        // Act
        Result<MenuSectionCommand> result = request.ToMenuSectionCommand();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType(typeof(ValidationError));
    }

    [Theory]
    [InlineData(nameof(Name))]
    [InlineData(nameof(Description))]
    public void ToMenuItemCommand_Required_parameters_are_validated(string field)
    {
        // Arrange
        MenuItemRequest request = new(
            field == nameof(Name) ? string.Empty : "Muffins",
            field == nameof(Description) ? string.Empty : "The Muffins section");

        // Act
        Result<MenuItemCommand> result = request.ToMenuItemCommand();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType(typeof(ValidationError));
    }

    [Fact]
    public void ToCreateMenuCommand_Multiple_parameters_are_validated()
    {
        // Arrange
        ValidationError.ModelError badName = new("Name cannot be empty.", "name");
        ValidationError.ModelError badDescription = new("Description cannot be empty.", "description");

        CreateMenuRequest request = new(string.Empty, string.Empty, new List<MenuSectionRequest>());

        // Act
        Result<CreateMenuCommand> result = request.ToCreateMenuCommand("62DB3B1E-B53A-4494-9462-B220B0A83A4B");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType(typeof(ValidationError));
        ValidationError validationError = (ValidationError)result.Error;
        validationError.Errors[0].Should().Be(badName);
        validationError.Errors[1].Should().Be(badDescription);
    }

    [Fact]
    public void ToMenuSectionCommand_Multiple_parameters_are_validated()
    {
        // Arrange
        ValidationError.ModelError badName = new("Name cannot be empty.", "name");
        ValidationError.ModelError badDescription = new("Description cannot be empty.", "description");

        MenuSectionRequest request = new(string.Empty, string.Empty, new List<MenuItemRequest>());

        // Act
        Result<MenuSectionCommand> result = request.ToMenuSectionCommand();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType(typeof(ValidationError));
        ValidationError validationError = (ValidationError)result.Error;
        validationError.Errors[0].Should().Be(badName);
        validationError.Errors[1].Should().Be(badDescription);
    }

    [Fact]
    public void ToMenuItemCommand_Multiple_parameters_are_validated()
    {
        // Arrange
        ValidationError.ModelError badName = new("Name cannot be empty.", "name");
        ValidationError.ModelError badDescription = new("Description cannot be empty.", "description");

        MenuItemRequest request = new(string.Empty, string.Empty);

        // Act
        Result<MenuItemCommand> result = request.ToMenuItemCommand();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType(typeof(ValidationError));
        ValidationError validationError = (ValidationError)result.Error;
        validationError.Errors[0].Should().Be(badName);
        validationError.Errors[1].Should().Be(badDescription);
    }

    [Fact]
    public void Can_create_CreateMenuCommand()
    {
        // Arrange
        string hostId = "62DB3B1E-B53A-4494-9462-B220B0A83A4B";
        CreateMenuRequest request = new(
            "Muffins 'R' Us",
            "Menu for Muffins 'R' Us",
            new List<MenuSectionRequest>()
            {
                new(
                    "Muffins",
                    "The Muffins section",
                    new List<MenuItemRequest>()
                    {
                        new("Blueberry", "A Blueberry Muffin"),
                        new("Chocolate", "A Chocolate Muffin"),
                        new("Lemon", "A Lemon Muffin")
                    }),
                new(
                    "Not Muffins",
                    "Anything that is not a muffin",
                    new List<MenuItemRequest>()
                    {
                        new("Cookie", "Probably oatmeal raisin or something"),
                        new("Cupcake", "Its trying to be a muffin, but its not fooling anyone")
                    })
            });

        // Act
        Result<CreateMenuCommand> result = request.ToCreateMenuCommand(hostId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        CreateMenuCommand createMenuCommand = result.Value;
        createMenuCommand.Name.Should().Be(Name.New(request.Name).Value);
        createMenuCommand.Description.Should().Be(Description.New(request.Description).Value);
        createMenuCommand.HostId.Should().Be(HostId.New(Guid.Parse(hostId)).Value);
        createMenuCommand.Sections.Count.Should().Be(2);

        MenuSectionRequest section0Request = request.Sections[0];
        MenuSectionCommand section0Command = createMenuCommand.Sections[0];
        section0Command.Name.Should().Be(Name.New(section0Request.Name).Value);
        section0Command.Description.Should().Be(Description.New(section0Request.Description).Value);
        section0Command.Items.Count.Should().Be(3);

        MenuSectionRequest section1Request = request.Sections[1];
        MenuSectionCommand section1Command = createMenuCommand.Sections[1];
        section1Command.Name.Should().Be(Name.New(section1Request.Name).Value);
        section1Command.Description.Should().Be(Description.New(section1Request.Description).Value);
        section1Command.Items.Count.Should().Be(2);

        MenuItemRequest item0_0Request = section0Request.Items[0];
        MenuItemCommand item0_0Command = section0Command.Items[0];
        item0_0Command.Name.Should().Be(Name.New(item0_0Request.Name).Value);
        item0_0Command.Description.Should().Be(Description.New(item0_0Request.Description).Value);

        MenuItemRequest item0_1Request = section0Request.Items[1];
        MenuItemCommand item0_1Command = section0Command.Items[1];
        item0_1Command.Name.Should().Be(Name.New(item0_1Request.Name).Value);
        item0_1Command.Description.Should().Be(Description.New(item0_1Request.Description).Value);

        MenuItemRequest item0_2Request = section0Request.Items[2];
        MenuItemCommand item0_2Command = section0Command.Items[2];
        item0_2Command.Name.Should().Be(Name.New(item0_2Request.Name).Value);
        item0_2Command.Description.Should().Be(Description.New(item0_2Request.Description).Value);

        MenuItemRequest item1_0Request = section1Request.Items[0];
        MenuItemCommand item1_0Command = section1Command.Items[0];
        item1_0Command.Name.Should().Be(Name.New(item1_0Request.Name).Value);
        item1_0Command.Description.Should().Be(Description.New(item1_0Request.Description).Value);

        MenuItemRequest item1_1Request = section1Request.Items[1];
        MenuItemCommand item1_1Command = section1Command.Items[1];
        item1_1Command.Name.Should().Be(Name.New(item1_1Request.Name).Value);
        item1_1Command.Description.Should().Be(Description.New(item1_1Request.Description).Value);
    }
}
