namespace BuberDinner.Domain.Entities;

using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Errors;
using FluentValidation;

public class User : Entity<Guid>
{
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }
    public string Password { get; }

    public static Result<User, ErrorList> Create(string firstName, string lastName, string email, string password)
    {
        var user = new User(firstName, lastName, email, password);
        return s_validator.ValidateToResult(user);
    }


    private User(string firstName, string lastName, string email, string password)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
    }

    static readonly InlineValidator<User> s_validator = new()
    {
        v => v.RuleFor(x => x.FirstName).NotEmpty(),
        v => v.RuleFor(x => x.LastName).NotEmpty(),
        v => v.RuleFor(x => x.Email).NotEmpty().EmailAddress(),
        v => v.RuleFor(x => x.Password).NotEmpty()
    };
}
