namespace BuberDinner.Api.Netural.Models.Authentication;

using BuberDinner.Application.Services.Authentication.Queries;
using BuberDinner.Domain.User.ValueObjects;

/// <summary>
/// Login request model.
/// </summary>
/// <param name="userId">User Id</param>
/// <param name="password">Password</param>
public record LoginRequest(
    string userId,
    string password
)
{
    internal Result<LoginQuery, Error> ToLoginQuery() =>
        UserId.New(userId)
        .Combine(Password.New(password))
        .Bind((userId, pwd) => LoginQuery.New(userId, pwd));
}
