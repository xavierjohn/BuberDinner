namespace BuberDinner.Domain.Errors;

public partial class Errors
{
    public static class User
    {
        public static Error AlreadyExists(string id) => Error.Conflict(
            code: "User.DuplicateUserId",
            message: $"User with this id {id} already exists.");

        public static Error DoesNotExist(string id) => Error.NotFound(
        code: "User.DoesNotExist",
        message: $"User with this id {id} does not exist.");
    }
}
