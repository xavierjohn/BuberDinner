namespace DomainTests;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.User.Entities;
using BuberDinner.Domain.User.ValueObjects;
using CSharpFunctionalExtensions.Errors;
using FluentAssertions;
using Xunit;

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
        var password = field == nameof(User.Password) ? string.Empty : "you can't crack this.";

        // Act
#pragma warning disable CS8604 // Possible null reference argument.
        var caseResult = User.Create(firstName, lastName, email, password);
#pragma warning restore CS8604 // Possible null reference argument.

        // Assert
        caseResult.IsFailure.Should().BeTrue();
        caseResult.Error[0].Should().BeOfType<Validation>();
        caseResult.Error[0].Message.Should().EndWith($" must not be empty."); ;
    }
}
