namespace BuberDinner.Api.Netural.Models.Authentication;

using BuberDinner.Application.Services.Authentication.Commands;
using BuberDinner.Domain.User.ValueObjects;

/// <summary>
/// Register request model.
/// </summary>
/// <param name="userId">User Id</param>
/// <param name="firstName">First Name</param>
/// <param name="lastName">Last Name</param>
/// <param name="email">Email address</param>
/// <param name="password">Password</param>
public record RegisterRequest(
    string userId,
    string firstName,
    string lastName,
    string email,
    string password
)
{

    internal Result<RegisterCommand, Error> ToRegisterCommand() =>
        UserId.New(userId)
        .Combine(FirstName.New(firstName))
        .Combine(LastName.New(lastName))
        .Combine(EmailAddress.New(email))
        .Combine(Password.New(password))
        .Bind(RegisterCommand.New);
};

