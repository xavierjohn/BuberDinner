namespace BuberDinner.Domain.User.ValueObjects
{
    using CSharpFunctionalExtensions;
    using CSharpFunctionalExtensions.Errors;

    public class FirstName : SimpleValueObject<string>
    {
        private FirstName(string value) : base(value)
        {
        }

        public static Result<FirstName, ErrorList> Create(string firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure<FirstName, ErrorList>(Error.Validation(nameof(firstName), "First name cannot be empty"));

            return new FirstName(firstName);
        }
    }
}
