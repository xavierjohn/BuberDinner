namespace BuberDinner.Domain.Errors;

using BuberDinner.Domain.User.ValueObjects;
using UserEntity = BuberDinner.Domain.User.Entities.User;

public partial class Errors
{
    public static class User
    {
        public static Error AlreadyExists(UserId id) =>
            new Error.Conflict(ResourceRef.For<UserEntity>(id), "user.duplicate_id") { Detail = "User Id already exists." };

        public static Error DoesNotExist(UserId id) =>
            new Error.NotFound(ResourceRef.For<UserEntity>(id)) { Detail = "User Id does not exist." };
    }
}
