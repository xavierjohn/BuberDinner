namespace BuberDinner.Application.Services.Authentication.Queries;

using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Domain.User.ValueObjects;
using FunctionalDDD;
using FunctionalDDD.CommonValueObjects;
using Mediator;

public class LoginQuery : IRequest<Result<AuthenticationResult>>
{
    private LoginQuery(EmailAddress email, Password password)
    {
        Email = email;
        Password = password;
    }

    public EmailAddress Email { get; }
    public Password Password { get; }

    public static Result<LoginQuery> Create(EmailAddress email, Password password) =>
        new LoginQuery(email, password);
}
