namespace CSharpFunctionalExtensions.Errors;

public sealed class Unauthorized : Error
{
    public Unauthorized(string code, string message) : base(code, message)
    {
    }
}
