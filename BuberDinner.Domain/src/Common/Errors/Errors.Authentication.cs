namespace BuberDinner.Domain.Errors;

public partial class Errors
{
    public static class Authentication
    {
        public static Error InvalidCredentials => Error.Unauthorized(
            code: "Authentication.InvalidCredentials",
            message: "Invalid credentials.");
    }

}
