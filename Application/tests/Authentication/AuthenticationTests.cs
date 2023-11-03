namespace BuberDinner.Application.Tests.Authentication
{
    using System.Threading.Tasks;
    using BuberDinner.Application.Services.Authentication.Commands;
    using BuberDinner.Domain.User.ValueObjects;
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
            var userId = UserId.TryCreate("xavierjohn2023").Value;
            var firstName = FirstName.TryCreate("Xavier").Value;
            var lastName = LastName.TryCreate("John").Value;
            var email = EmailAddress.TryCreate("xavier@somewhere.com").Value;
            var password = Password.TryCreate("SuperStrongPassword").Value;
            var command = RegisterCommand.TryCreate(userId, firstName, lastName, email, password).Value;

            // Act
            Result<Services.Authentication.Common.AuthenticationResult> result = await _sender.Send(command);

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
