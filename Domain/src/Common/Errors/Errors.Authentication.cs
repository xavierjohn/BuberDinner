namespace BuberDinner.Domain.Errors;

public partial class Errors
{
    public static class Authentication
    {
        // NOTE: Trellis V3's closed Error union has no `Code` slot on AuthenticationRequired.
        // The old "Authentication.InvalidCredentials" machine code is preserved only via Detail.
        // See Docs/MIGRATION_TO_TRELLIS_V3.md (reg-001-auth-no-reason-code).
        public static Error InvalidCredentials =>
            new Error.AuthenticationRequired(Scheme: "Bearer") { Detail = "Invalid credentials." };
    }
}
