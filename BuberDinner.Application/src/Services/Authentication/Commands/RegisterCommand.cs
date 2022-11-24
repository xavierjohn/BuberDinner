namespace BuberDinner.Application.Services.Authentication.Commands;

using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.User.ValueObjects;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Errors;
using Mediator;

public record RegisterCommand(FirstName FirstName, LastName LastName, EmailAddress Email, string Password)
    : IRequest<Result<AuthenticationResult, ErrorList>>
{
    public static Result<RegisterCommand, ErrorList> Create(string firstName, string lastName, string email, string password)
    {
        var rFirstName = FirstName.Create(firstName);
        var rLastName = LastName.Create(lastName);
        var rEmail = EmailAddress.Create(email);

        return ErrorList.Combine(rFirstName, rLastName, rEmail)
            .Map(x => new RegisterCommand(rFirstName.Value, rLastName.Value, rEmail.Value, password));
    }
}
