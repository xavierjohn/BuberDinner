namespace BuberDinner.Domain.Menu.ValueObject
{
    using System;
    using CSharpFunctionalExtensions;
    using CSharpFunctionalExtensions.Errors;

    public class DinnerId : SimpleValueObject<Guid>
    {
        private DinnerId(Guid value) : base(value)
        {
        }

        public static Result<DinnerId, ErrorList> Create(Guid id)
        {
            if (id == Guid.Empty)
                return Result.Failure<DinnerId, ErrorList>(Error.Validation(nameof(id), "Id cannot be empty"));

            return new DinnerId(id);
        }
    }
}
