namespace BuberDinner.Api.Netural.Models.Authentication;

using BuberDinner.Application.Services.Authentication.Queries;
using BuberDinner.Domain.User.ValueObjects;
using FunctionalDDD;
using FunctionalDDD.CommonValueObjects;

public record LoginRequest(
    string email,
    string password
)
{
    public Result<LoginQuery> ToLoginQuery() =>
        EmailAddress.Create(email)
        .Combine(Password.Create(password))
        .Bind((email, pwd) => LoginQuery.Create(email, pwd));
}
