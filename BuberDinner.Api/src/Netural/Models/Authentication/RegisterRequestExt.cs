namespace BuberDinner.Api.Netural.Models.Authentication
{
    using BuberDinner.Application.Services.Authentication.Commands;
    using BuberDinner.Domain.User.ValueObjects;
    using FunctionalDDD;
    using FunctionalDDD.CommonValueObjects;

    internal static class RegisterRequestExt
    {
        public static Result<RegisterCommand> ToRegisterCommand(this RegisterRequest request) =>
            FirstName.Create(request.FirstName)
             .Combine(LastName.Create(request.LastName))
             .Combine(EmailAddress.Create(request.EmailAddress))
             .Combine(Password.Create(request.Password))
             .Bind((firstName, lastName, email, pwd) => RegisterCommand.Create(firstName, lastName, email, pwd));
    }
}
