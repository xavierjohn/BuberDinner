namespace BuberDinner.Application.Services.Authentication.Queries;

using System;
using BuberDinner.Application.Services.Authentication.Common;
using FunctionalDDD;
using Mediator;

public class LoginQuery : IRequest<Result<AuthenticationResult>>
{
    private LoginQuery(string email, string password)
    {
        Email = email;
        Password = password;
    }

    public string Email { get; }
    public string Password { get; }

    public static Result<LoginQuery> Create(string email, string password)
    {
        throw new NotImplementedException();
    }
}
