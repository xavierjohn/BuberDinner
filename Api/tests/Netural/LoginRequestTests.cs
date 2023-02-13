namespace BuberDinner.Api.Tests.Netural;

using BuberDinner.Api.Netural.Models.Authentication;
using BuberDinner.Domain.User.ValueObjects;

public class LoginRequestTests
{
    [Theory]
    [InlineData(nameof(UserId))]
    [InlineData(nameof(Password))]
    public void Required_parameters_are_validated(string field)
    {
        // Arrange
        var request = new LoginRequest(
        field == nameof(UserId) ? string.Empty : "xavierjohn2023",
        field == nameof(Password) ? string.Empty : "SuperStrongPassword"
        );

        // Act
        var result = request.ToLoginQuery();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType(typeof(ValidationError));
    }

    [Fact]
    public void Can_create_LoginQuery()
    {
        // Arrange
        var request = new LoginRequest("xavierjohn2023", "password");

        // Act
        var result = request.ToLoginQuery();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var registerCommand = result.Value;
        registerCommand.UserId.Should().Be(UserId.New("xavierjohn2023").Value);
        registerCommand.Password.Should().Be(Password.New("password").Value);
    }
}
