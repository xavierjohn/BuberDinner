namespace BuberDinner.Domain.Menu.ValueObject
{
    using System;
    using CSharpFunctionalExtensions;
    using CSharpFunctionalExtensions.Errors;

    public class MenuSectionId : SimpleValueObject<Guid>
    {
        private MenuSectionId(Guid value) : base(value)
        {
        }

        public static Result<MenuSectionId, ErrorList> Create(Guid id)
        {
            if (id == Guid.Empty)
                return Result.Failure<MenuSectionId, ErrorList>(Error.Validation(nameof(id), "Id cannot be empty"));

            return new MenuSectionId(id);
        }

        internal static MenuSectionId CreateUnique() =>
            new(Guid.NewGuid());
    }
}
