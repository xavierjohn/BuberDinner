namespace BuberDinner.Domain.Errors;

public partial class Errors
{
    public static class User
    {
        public static Error AlreadyExists(string id) => new ConflictError("User Id already exists.", "User.DuplicateUserId", id);

        public static Error DoesNotExist(string id) => new NotFoundError("User.DoesNotExist", "User Id does not exist.", id);
    }
}
