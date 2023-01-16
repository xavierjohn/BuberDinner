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
    internal Result<LoginQuery> ToLoginQuery() =>
        UserId.Create(userId)
        .Combine(Password.Create(password))
        .Bind((userId, pwd) => LoginQuery.Create(userId, pwd));
}
