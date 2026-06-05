namespace BuberDinner.Domain.Errors;

public partial class Errors
{
    public static class Authentication
    {
        // Trellis 3.0.0-alpha.342 added the `ReasonCode` slot on Error.AuthenticationRequired
        // (Option A from the reg-001 issue draft). Restores the v2.x machine-readable code
        // distinction without forcing telemetry/clients to parse Detail.
        public static Error InvalidCredentials =>
            new Error.AuthenticationRequired(Scheme: "Bearer", ReasonCode: "Authentication.InvalidCredentials")
            {
                Detail = "Invalid credentials.",
            };
    }
}
