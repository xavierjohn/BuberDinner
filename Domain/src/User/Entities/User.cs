namespace BuberDinner.Domain.User.Entities;

using BuberDinner.Domain.User.ValueObjects;
using FluentValidation;

public class User : Entity<UserId>
{
    public FirstName FirstName { get; }
    public LastName LastName { get; }
    public EmailAddress Email { get; }
    public Password Password { get; }

    public static Result<User> TryCreate(UserId id, FirstName firstName, LastName lastName, EmailAddress email, Password password)
    {
        var user = new User(id, firstName, lastName, email, password);
        return s_validator.ValidateToResult(user);
    }

    private User(UserId id, FirstName firstName, LastName lastName, EmailAddress email, Password password)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
    }

    static readonly InlineValidator<User> s_validator = new()
    {
        v => v.RuleFor(x => x.Id).NotNull(),
        v => v.RuleFor(x => x.FirstName).NotEmpty(),
        v => v.RuleFor(x => x.LastName).NotEmpty(),
        v => v.RuleFor(x => x.Email).NotNull(),
        v => v.RuleFor(x => x.Password).NotEmpty()
    };
}
