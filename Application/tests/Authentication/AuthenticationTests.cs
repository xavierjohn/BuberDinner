namespace BuberDinner.Application.Tests.Authentication
{
    using System.Threading.Tasks;
    using BuberDinner.Application.Services.Authentication.Commands;
    using BuberDinner.Domain.User.ValueObjects;
    using FunctionalDDD.CommonValueObjects;
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
            var userId = UserId.New("xavierjohn2023").Value;
            var firstName = FirstName.New("Xavier").Value;
            var lastName = LastName.New("John").Value;
            var email = EmailAddress.New("xavier@somewhere.com").Value;
            var password = Password.New("SuperStrongPassword").Value;
            var command = RegisterCommand.New(userId, firstName, lastName, email, password).Value;

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
