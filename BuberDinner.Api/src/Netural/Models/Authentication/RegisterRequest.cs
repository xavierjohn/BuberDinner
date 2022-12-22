namespace BuberDinner.Api.Netural.Models.Authentication;

using BuberDinner.Application.Services.Authentication.Commands;
using BuberDinner.Domain.User.ValueObjects;

public record RegisterRequest(
    string firstName,
    string lastName,
    string email,
    string password
)
{

    public Result<RegisterCommand> ToRegisterCommand() =>
        FirstName.Create(firstName)
         .Combine(LastName.Create(lastName))
         .Combine(EmailAddress.Create(email))
         .Combine(Password.Create(password))
         .Bind((firstName, lastName, email, pwd) => RegisterCommand.Create(firstName, lastName, email, pwd));
};

