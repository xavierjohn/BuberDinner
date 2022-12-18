namespace BuberDinner.Application.Tests.Authentication
{
    using System.Threading.Tasks;
    using BuberDinner.Application.Services.Authentication.Commands;
    using BuberDinner.Domain.Common.ValueObjects;
    using BuberDinner.Domain.User.ValueObjects;
    using FluentAssertions;
    using FunctionalDDD;
    using Mediator;
    using Xunit;

    public class AuthenticationTests
    {
        private readonly ISender _sender;

        public AuthenticationTests(ISender sender)
        {
            _sender = sender;
        }

        [Theory]
        [InlineData(nameof(FirstName))]
        [InlineData(nameof(LastName))]
        [InlineData(nameof(EmailAddress))]
        public void Required_parameters_are_validated(string field)
        {
            // Arrange
            var firstName = field == nameof(FirstName) ? string.Empty : "Xavier";
            var lastName = field == nameof(LastName) ? string.Empty : "John";
            var email = field == nameof(EmailAddress) ? "bad email" : "xavier@somewhere.com";

            // Act
            var result = RegisterCommand.Create(firstName, lastName, email, "password");

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Errors.Should().HaveCount(1);
            result.Error.Should().BeOfType(typeof(Validation));
        }

        [Fact]
        public void Multiple_parameters_are_validated()
        {
            // Arrange
            var badFirstName = new Validation("firstName", "First name cannot be empty");
            var badEmail = new Validation("email", "Email is not valid");


            // Act
            var result = RegisterCommand.Create(string.Empty, "John", "bad email", "password");

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Errors.Should().HaveCount(2);
            result.Errors[0].Should().BeOfType(typeof(Validation));
            result.Errors[0].Should().Be(badFirstName);
            result.Errors[1].Should().BeOfType(typeof(Validation));
            result.Errors[1].Should().Be(badEmail);
        }


        [Fact]
        public async Task Can_Register_new_User()
        {
            // Arrange
            var firstName = FirstName.Create("Xavier").Value;
            var lastName = LastName.Create("John").Value;
            var email = EmailAddress.Create("xavier@somewhere.com").Value;
            var command = new RegisterCommand(firstName, lastName, email, "you can't crack this.");

            // Act
            var result = await _sender.Send(command);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Token.Should().NotBeEmpty();
            var user = result.Value.User;
            user.FirstName.Should().Be(firstName);
            user.LastName.Should().Be(lastName);
            user.Email.Should().Be(email);

        }
    }
}
