﻿namespace DomainTests;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.User.Entities;
using BuberDinner.Domain.User.ValueObjects;
using FluentAssertions;
using FunctionalDDD;
using Xunit;
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
        var password = field == nameof(User.Password) ? string.Empty : "you can't crack this.";

        // Act
#pragma warning disable CS8604 // Possible null reference argument.
        var caseResult = User.Create(UserId.CreateUnique(), firstName, lastName, email, password);
#pragma warning restore CS8604 // Possible null reference argument.

        // Assert
        caseResult.IsFailure.Should().BeTrue();
        caseResult.Error.Should().BeOfType<Validation>();
        caseResult.Error.Message.Should().EndWith($" must not be empty."); ;
    }
}
