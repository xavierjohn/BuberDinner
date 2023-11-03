namespace BuberDinner.Api.Neutral.Models.Authentication;

using BuberDinner.Application.Services.Authentication.Queries;
using PasswordClass = Domain.User.ValueObjects.Password;
using UserIdClass = Domain.User.ValueObjects.UserId;

/// <summary>
/// Login request model.
/// </summary>
/// <param name="UserId">User Id</param>
/// <param name="Password">Password</param>
public record LoginRequest(
    string UserId,
    string Password)
{
    internal Result<LoginQuery> ToLoginQuery() =>
        UserIdClass.TryCreate(UserId)
        .Combine(PasswordClass.TryCreate(Password))
        .Bind((userId, pwd) => LoginQuery.New(userId, pwd));
}
