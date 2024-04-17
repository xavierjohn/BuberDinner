namespace BuberDinner.Domain.Errors;

public partial class Errors
{
    public static class Authentication
    {
        public static Error InvalidCredentials => new UnauthorizedError("Invalid credentials.", "Authentication.InvalidCredentials");
    }

}
