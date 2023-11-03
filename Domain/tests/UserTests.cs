namespace DomainTests;

using BuberDinner.Domain.User.Entities;
using BuberDinner.Domain.User.ValueObjects;
#pragma warning disable IDE0007 // Use var keyword

public class UserTests
{
    [Theory]
    [InlineData(nameof(User.Id))]
    [InlineData(nameof(User.FirstName))]
    [InlineData(nameof(User.LastName))]
    [InlineData(nameof(User.Email))]
    [InlineData(nameof(User.Password))]
    public void Required_parameters_are_validated(string field)
    {
        // Arrange
        UserId? id = field == nameof(User.Id) ? default : UserId.TryCreate("xavierjohn2023").Value;
        FirstName? firstName = field == nameof(User.FirstName) ? default : FirstName.TryCreate("Xavier").Value;
        LastName? lastName = field == nameof(User.LastName) ? default : LastName.TryCreate("John").Value;
        EmailAddress? email = field == nameof(User.Email) ? default : EmailAddress.TryCreate("xavier@somewhere.com").Value;
        Password? password = field == nameof(User.Password) ? default : Password.TryCreate("you can't crack this.").Value;

        // Act
#pragma warning disable CS8604 // Possible null reference argument.
        var userResult = User.TryCreate(id, firstName, lastName, email, password);
#pragma warning restore CS8604 // Possible null reference argument.

        // Assert
        userResult.IsFailure.Should().BeTrue();
        userResult.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)userResult.Error;
        validationError.Errors[0].Message.Should().EndWith($" must not be empty."); ;
    }

    [Fact]
    public void Different_passwords_are_not_the_same()
    {
        // Arrange
        Password pwd1 = Password.TryCreate("Hello").Value;
        Password pwd2 = Password.TryCreate("There").Value;

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
        Password pwd1 = Password.TryCreate("Hello").Value;
        Password pwd2 = Password.TryCreate("Hello").Value;

        // Act
        bool result1 = pwd1 == pwd2;
        bool result2 = pwd1.Equals(pwd2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();

    }
}
