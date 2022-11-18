namespace BuberDinner.Domain.Common.ValueObjects;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Errors;

public partial class EmailAddress : SimpleValueObject<string>
{
    private EmailAddress(string value) : base(value) { }

    public static Result<EmailAddress, ErrorList> Create(string emailString, string? fieldName = null)
    {
        var isEmail = EmailRegEx().IsMatch(emailString);
        if (isEmail) return new EmailAddress(emailString);

        return Result.Failure<EmailAddress, ErrorList>(Error.Validation(fieldName ?? nameof(emailString), "Bad email address"));
    }

    [GeneratedRegex("\\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\\Z", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex EmailRegEx();
}

