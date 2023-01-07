namespace DomainTests;

using BuberDinner.Domain.User.Entities;
using BuberDinner.Domain.User.ValueObjects;
#pragma warning disable IDE0007 // Use var keyword

public class UserTests
{
    [Theory]
    [InlineData(nameof(User.FirstName))]
    [InlineData(nameof(User.LastName))]
    [InlineData(nameof(User.Email))]
    [InlineData(nameof(User.Password))]
    public void Required_parameters_are_validated(string field)
    {
        // Arrange
        FirstName? firstName = field == nameof(User.FirstName) ? default : FirstName.Create("Xavier").Value;
        LastName? lastName = field == nameof(User.LastName) ? default : LastName.Create("John").Value;
        EmailAddress? email = field == nameof(User.Email) ? default : EmailAddress.Create("xavier@somewhere.com").Value;
        Password? password = field == nameof(User.Password) ? default : Password.Create("you can't crack this.").Value;

        // Act
#pragma warning disable CS8604 // Possible null reference argument.
        var userResult = User.Create(firstName, lastName, email, password);
#pragma warning restore CS8604 // Possible null reference argument.

        // Assert
        userResult.IsFailure.Should().BeTrue();
        userResult.Error.Should().BeOfType<Validation>();
        userResult.Error.Message.Should().EndWith($" must not be empty."); ;
    }

    [Fact]
    public void Different_passwords_are_not_the_same()
    {
        // Arrange
        Password pwd1 = Password.Create("Hello").Value;
        Password pwd2 = Password.Create("There").Value;

        // Act
        bool result1 = pwd1 == pwd2;
        bool result2 = pwd1.Equals(pwd2);

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();

    }

    [Fact]
    public void Two_passwords_of_the_same_content_are_equal()
    {
        // Arrange
        Password pwd1 = Password.Create("Hello").Value;
        Password pwd2 = Password.Create("Hello").Value;

        // Act
        bool result1 = pwd1 == pwd2;
        bool result2 = pwd1.Equals(pwd2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();

    }
}
