namespace BuberDinner.Api.Netural.Models.Authentication;

using BuberDinner.Application.Services.Authentication.Commands;
using BuberDinner.Domain.User.ValueObjects;

/// <summary>
/// Register request model.
/// </summary>
/// <param name="firstName">First Name</param>
/// <param name="lastName">Last Name</param>
/// <param name="email">Email address</param>
/// <param name="password">Password</param>
public record RegisterRequest(
    string firstName,
    string lastName,
    string email,
    string password
)
{

    internal Result<RegisterCommand> ToRegisterCommand() =>
        FirstName.Create(firstName)
         .Combine(LastName.Create(lastName))
         .Combine(EmailAddress.Create(email))
         .Combine(Password.Create(password))
         .Bind((firstName, lastName, email, pwd) => RegisterCommand.Create(firstName, lastName, email, pwd));
};

