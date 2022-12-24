namespace BuberDinner.Api.Netural.Models.Authentication;

using BuberDinner.Application.Services.Authentication.Queries;
using BuberDinner.Domain.User.ValueObjects;

/// <summary>
/// Login request model.
/// </summary>
/// <param name="email">Email address</param>
/// <param name="password">Password</param>
public record LoginRequest(
    string email,
    string password
)
{
    internal Result<LoginQuery> ToLoginQuery() =>
        EmailAddress.Create(email)
        .Combine(Password.Create(password))
        .Bind((email, pwd) => LoginQuery.Create(email, pwd));
}
