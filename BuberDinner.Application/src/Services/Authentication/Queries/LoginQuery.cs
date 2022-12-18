namespace BuberDinner.Application.Services.Authentication.Queries;

using BuberDinner.Application.Services.Authentication.Common;
using FunctionalDDD;
using Mediator;

public record LoginQuery(string Email, string Password)
    : IRequest<Result<AuthenticationResult>>;
