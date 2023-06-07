namespace BuberDinner.Api.Neutral.Models.Authentication;

using BuberDinner.Application.Services.Authentication.Commands;
using FirstNameClass = Domain.User.ValueObjects.FirstName;
using LastNameClass = Domain.User.ValueObjects.LastName;
using PasswordClass = Domain.User.ValueObjects.Password;
using UserIdClass = Domain.User.ValueObjects.UserId;

/// <summary>
/// Register request model.
/// </summary>
/// <param name="UserId">User Id</param>
/// <param name="FirstName">First Name</param>
/// <param name="LastName">Last Name</param>
/// <param name="Email">Email address</param>
/// <param name="Password">Password</param>
public record RegisterRequest(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string Password
)
{

    internal Result<RegisterCommand> ToRegisterCommand() =>
        UserIdClass.New(UserId)
        .Combine(FirstNameClass.New(FirstName))
        .Combine(LastNameClass.New(LastName))
        .Combine(EmailAddress.New(Email))
        .Combine(PasswordClass.New(Password))
        .Bind(RegisterCommand.New);
};

