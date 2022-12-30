namespace BuberDinner.Domain.Errors;

public partial class Errors
{
    public static class User
    {
        public static Error AlreadyExists(string email) => Error.Conflict(
            code: "User.DuplicateEmail",
            message: $"User with this email {email} already exists.");

        public static Error DoesNotExist(string email) => Error.NotFound(
        code: "User.DoesNotExist",
        message: $"User with this email {email} does not exist.");
    }
}
