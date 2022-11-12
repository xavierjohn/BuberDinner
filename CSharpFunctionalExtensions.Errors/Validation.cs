namespace CSharpFunctionalExtensions.Errors;

public sealed class Validation : Error
{
    public Validation() : base("validation.error", "Validation error.")
    {
    }
    public Validation(string code, string message) : base(code, message)
    {
    }
}
