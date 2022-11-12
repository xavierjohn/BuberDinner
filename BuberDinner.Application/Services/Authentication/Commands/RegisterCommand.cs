namespace BuberDinner.Application.Services.Authentication.Commands;

using BuberDinner.Application.Services.Authentication.Common;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Errors;
using MediatR;

public record RegisterCommand(string FirstName, string LastName, string Email, string Password)
    : IRequest<Result<AuthenticationResult, ErrorList>>;
