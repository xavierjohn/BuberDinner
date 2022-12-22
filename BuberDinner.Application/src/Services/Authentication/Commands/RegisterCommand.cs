namespace BuberDinner.Application.Services.Authentication.Commands;

using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Domain.User.ValueObjects;
using FunctionalDDD;
using FunctionalDDD.CommonValueObjects;
using Mediator;

public class RegisterCommand
    : IRequest<Result<AuthenticationResult>>
{
    public static Result<RegisterCommand> Create(FirstName firstName, LastName lastName, EmailAddress email, Password password) =>
            new RegisterCommand(firstName, lastName, email, password);

    public FirstName FirstName { get; }
    public LastName LastName { get; }
    public EmailAddress Email { get; }
    public Password Password { get; }

    private RegisterCommand(FirstName firstName, LastName lastName, EmailAddress email, Password password)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
    }

}
