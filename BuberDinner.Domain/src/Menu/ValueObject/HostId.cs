namespace BuberDinner.Domain.Menu.ValueObject
{
    using System;
    using CSharpFunctionalExtensions;
    using CSharpFunctionalExtensions.Errors;

    public class HostId : SimpleValueObject<Guid>
    {
        private HostId(Guid value) : base(value)
        {
        }

        public static Result<HostId, ErrorList> Create(Guid id)
        {
            if (id == Guid.Empty)
                return Result.Failure<HostId, ErrorList>(Error.Validation(nameof(id), "Id cannot be empty"));

            return new HostId(id);
        }
    }
}
