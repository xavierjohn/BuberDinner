namespace BuberDinner.Application.Services.Authentication.Commands;

using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

public class RegisterCommand
    : IRequest<Result<AuthenticationResult, Error>>
{
    public static Result<RegisterCommand, Error> New(UserId id, FirstName firstName, LastName lastName, EmailAddress email, Password password) =>
            new RegisterCommand(id, firstName, lastName, email, password);

    public UserId UserId { get; }
    public FirstName FirstName { get; }
    public LastName LastName { get; }
    public EmailAddress Email { get; }
    public Password Password { get; }

    private RegisterCommand(UserId id, FirstName firstName, LastName lastName, EmailAddress email, Password password)
    {
        UserId = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
    }
}
