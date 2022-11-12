namespace CSharpFunctionalExtensions.Errors;

public sealed class Conflict : Error
{
    public Conflict(string? id = null) : base("record.already.exists", ModifyMessage(id))
    {
    }

    public Conflict(string code, string message) : base(code, message)
    {
    }

    private static string ModifyMessage(string? id)
    {
        string forId = id == null ? "" : $" for Id '{id}'";
        return $"Record already exists {forId}.";
    }

}
