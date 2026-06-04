namespace BuberDinner.Application.Tests.Authentication
{
    using System.Threading.Tasks;
    using BuberDinner.Application.Services.Authentication.Commands;
    using BuberDinner.Domain.User.ValueObjects;
    using BuberDinner.Infrastructure.Persistence.Dto;
    using Mediator;

    public class AuthenticationTests
    {
        private readonly ISender _sender;

        public AuthenticationTests(ISender sender)
        {
            _sender = sender;
        }


        [Fact]
        public async Task Can_Register_new_User()
        {
            // Arrange
            var userId = UserId.TryCreate("xavierjohn2023").UnwrapOrThrow();
            var firstName = FirstName.TryCreate("Xavier").UnwrapOrThrow();
            var lastName = LastName.TryCreate("John").UnwrapOrThrow();
            var email = EmailAddress.TryCreate("xavier@somewhere.com").UnwrapOrThrow();
            var password = Password.TryCreate("SuperStrongPassword").UnwrapOrThrow();
            var command = RegisterCommand.TryCreate(userId, firstName, lastName, email, password).UnwrapOrThrow();

            // Act
            Result<Services.Authentication.Common.AuthenticationResult> result = await _sender.Send(command);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var authResult = result.UnwrapOrThrow();
            authResult.Token.Should().NotBeEmpty();
            var user = authResult.User;
            user.FirstName.Should().Be(firstName);
            user.LastName.Should().Be(lastName);
            user.Email.Should().Be(email);

        }
    }
}
