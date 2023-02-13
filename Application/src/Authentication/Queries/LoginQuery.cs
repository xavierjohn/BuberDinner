namespace BuberDinner.Application.Services.Authentication.Queries;

using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

public class LoginQuery : IRequest<Result<AuthenticationResult, Error>>
{
    private LoginQuery(UserId userId, Password password)
    {
        UserId = userId;
        Password = password;
    }

    public UserId UserId { get; }
    public Password Password { get; }

    public static Result<LoginQuery, Error> New(UserId userId, Password password) =>
        new LoginQuery(userId, password);
}
