namespace BuberDinner.Api.Tests.Neutral;

using BuberDinner.Api.Neutral.Models.Authentication;
using BuberDinner.Api.Tests;
using BuberDinner.Domain.User.ValueObjects;

public class RegisterRequestTests
{
    [Theory]
    [InlineData(nameof(UserId))]
    [InlineData(nameof(FirstName))]
    [InlineData(nameof(LastName))]
    [InlineData(nameof(EmailAddress))]
    [InlineData(nameof(Password))]
    public void Required_parameters_are_validated(string field)
    {
        // Arrange
        var request = new RegisterRequest(
        field == nameof(UserId) ? string.Empty : "XavierJohn2013",
        field == nameof(FirstName) ? string.Empty : "Xavier",
        field == nameof(LastName) ? string.Empty : "John",
        field == nameof(EmailAddress) ? "bad email" : "xavier@somewhere.com",
        field == nameof(Password) ? string.Empty : "SuperStrongPassword"
        );

        // Act
        var result = request.ToRegisterCommand();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<Error.InvalidInput>();
    }

    [Fact]
    public void Multiple_parameters_are_validated()
    {
        // Arrange
        var request = new RegisterRequest("id", string.Empty, "John", "bad email", "password");

        // Act
        var result = request.ToRegisterCommand();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<Error.InvalidInput>();
        var invalidInput = (Error.InvalidInput)result.Error!;
        var fieldPointers = invalidInput.Fields.Items.Select(v => v.Field.Path).ToArray();
        fieldPointers.Should().Contain("/firstName");
        fieldPointers.Should().Contain("/email");
    }

    [Fact]
    public void Can_create_RegisterCommand()
    {
        // Arrange
        var request = new RegisterRequest("id", "Xavier", "John", "xavier@somewhere.com", "password");

        // Act
        var result = request.ToRegisterCommand();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var registerCommand = result.UnwrapOrThrow();
        registerCommand.UserId.Should().Be(UserId.TryCreate("id").UnwrapOrThrow());
        registerCommand.FirstName.Should().Be(FirstName.TryCreate("Xavier").UnwrapOrThrow());
        registerCommand.LastName.Should().Be(LastName.TryCreate("John").UnwrapOrThrow());
        registerCommand.Email.Should().Be(EmailAddress.TryCreate("xavier@somewhere.com").UnwrapOrThrow());
        registerCommand.Password.Should().Be(Password.TryCreate("password").UnwrapOrThrow());
    }
}
