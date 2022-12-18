namespace BuberDinner.Application.Services.Authentication.Commands;

using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.User.ValueObjects;
using FunctionalDDD;
using Mediator;

public record RegisterCommand(FirstName FirstName, LastName LastName, EmailAddress Email, string Password)
    : IRequest<Result<AuthenticationResult>>
{
    public static Result<RegisterCommand> Create(string firstName, string lastName, string email, string password)
    {
        return FirstName.Create(firstName)
            .Combine(LastName.Create(lastName))
            .Combine(EmailAddress.Create(email))
            .Map((firstName, lastName, email) => new RegisterCommand(firstName, lastName, email, password));
    }
}
