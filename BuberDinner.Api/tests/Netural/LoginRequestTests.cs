namespace BuberDinner.Api.Tests.Netural;

using BuberDinner.Api.Netural.Models.Authentication;
using BuberDinner.Domain.User.ValueObjects;
using FunctionalDDD;
using FunctionalDDD.CommonValueObjects;
using Xunit;

public class LoginRequestTests
{
    [Theory]
    [InlineData(nameof(EmailAddress))]
    [InlineData(nameof(Password))]
    public void Required_parameters_are_validated(string field)
    {
        // Arrange
        var request = new LoginRequest(
        field == nameof(EmailAddress) ? "bad email" : "xavier@somewhere.com",
        field == nameof(Password) ? string.Empty : "SuperStrongPassword"
        );

        // Act
        var result = request.ToLoginQuery();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Error.Should().BeOfType(typeof(Validation));
    }

    [Fact]
    public void Can_create_LoginQuery()
    {
        // Arrange
        var request = new LoginRequest("xavier@somewhere.com", "password");

        // Act
        var result = request.ToLoginQuery();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var registerCommand = result.Value;
        registerCommand.Email.Should().Be(EmailAddress.Create("xavier@somewhere.com").Value);
        registerCommand.Password.Should().Be(Password.Create("password").Value);
    }
}
