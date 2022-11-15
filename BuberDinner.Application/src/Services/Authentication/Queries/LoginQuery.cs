namespace BuberDinner.Application.Services.Authentication.Queries;

using BuberDinner.Application.Services.Authentication.Common;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Errors;
using Mediator;

public record LoginQuery(string Email, string Password)
    : IRequest<Result<AuthenticationResult, ErrorList>>;
