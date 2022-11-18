namespace BuberDinner.Domain.User.ValueObjects
{
    using CSharpFunctionalExtensions;
    using CSharpFunctionalExtensions.Errors;

    public class LastName : SimpleValueObject<string>
    {
        private LastName(string value) : base(value)
        {
        }

        public static Result<LastName, ErrorList> Create(string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                return Result.Failure<LastName, ErrorList>(Error.Validation(nameof(lastName), "Last name cannot be empty."));

            return new LastName(lastName);
        }
    }
}
