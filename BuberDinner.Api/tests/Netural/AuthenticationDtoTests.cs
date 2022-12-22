namespace BuberDinner.Api.Tests.Netural;

using BuberDinner.Api.Netural.Models.Authentication;
using BuberDinner.Domain.User.ValueObjects;
using FunctionalDDD;
using FunctionalDDD.CommonValueObjects;
using Xunit;

public class RegisterRequestDtoTests
{
    [Theory]
    [InlineData(nameof(FirstName))]
    [InlineData(nameof(LastName))]
    [InlineData(nameof(EmailAddress))]
    [InlineData(nameof(Password))]
    public void Required_parameters_are_validated(string field)
    {
        // Arrange
        var request = new RegisterRequest(
        field == nameof(FirstName) ? string.Empty : "Xavier",
        field == nameof(LastName) ? string.Empty : "John",
        field == nameof(EmailAddress) ? "bad email" : "xavier@somewhere.com",
        field == nameof(Password) ? string.Empty : "SuperStrongPassword"
        );

        // Act
        var result = request.ToRegisterCommand();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Error.Should().BeOfType(typeof(Validation));
    }

    [Fact]
    public void Multiple_parameters_are_validated()
    {
        // Arrange
        var badFirstName = new Validation("firstName", "First Name cannot be empty");
        var badEmail = new Validation("email", "Email address is not valid");

        var request = new RegisterRequest(string.Empty, "John", "bad email", "password");

        // Act
        var result = request.ToRegisterCommand();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors[0].Should().BeOfType(typeof(Validation));
        result.Errors[0].Should().Be(badFirstName);
        result.Errors[1].Should().BeOfType(typeof(Validation));
        result.Errors[1].Should().Be(badEmail);
    }

    [Fact]
    public void Can_create_RegisterCommand()
    {
        // Arrange
        var request = new RegisterRequest("Xavier", "John", "xavier@somewhere.com", "password");

        // Act
        var result = request.ToRegisterCommand();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var registerCommand = result.Value;
        registerCommand.FirstName.Should().Be(FirstName.Create("Xavier").Value);
        registerCommand.LastName.Should().Be(LastName.Create("John").Value);
        registerCommand.EmailAddress.Should().Be(EmailAddress.Create("xavier@somewhere.com").Value);
        registerCommand.Password.Should().Be(Password.Create("password").Value);
    }
}
