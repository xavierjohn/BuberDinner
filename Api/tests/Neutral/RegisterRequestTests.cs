namespace BuberDinner.Api.Tests.Neutral;

using BuberDinner.Api.Neutral.Models.Authentication;
using BuberDinner.Domain.User.ValueObjects;
using FunctionalDDD.Domain;
using FunctionalDDD.Results.Errors;

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
        result.Error.Should().BeOfType(typeof(ValidationError));
    }

    [Fact]
    public void Multiple_parameters_are_validated()
    {
        // Arrange
        var badFirstName = new ValidationError.ModelError("First Name cannot be empty.", "firstName");
        var badEmail = new ValidationError.ModelError("Email address is not valid.", "email");

        var request = new RegisterRequest("id", string.Empty, "John", "bad email", "password");

        // Act
        var result = request.ToRegisterCommand();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType(typeof(ValidationError));
        var validationError = (ValidationError)result.Error;
        validationError.Errors[0].Should().Be(badFirstName);
        validationError.Errors[1].Should().Be(badEmail);
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
        var registerCommand = result.Value;
        registerCommand.UserId.Should().Be(UserId.New("id").Value);
        registerCommand.FirstName.Should().Be(FirstName.New("Xavier").Value);
        registerCommand.LastName.Should().Be(LastName.New("John").Value);
        registerCommand.Email.Should().Be(EmailAddress.New("xavier@somewhere.com").Value);
        registerCommand.Password.Should().Be(Password.New("password").Value);
    }
}
