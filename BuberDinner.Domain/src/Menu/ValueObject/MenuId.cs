namespace BuberDinner.Domain.Menu.ValueObject
{
    using System;
    using CSharpFunctionalExtensions;
    using CSharpFunctionalExtensions.Errors;

    public class MenuId : SimpleValueObject<Guid>
    {
        private MenuId(Guid value) : base(value)
        {
        }

        public static Result<MenuId, ErrorList> Create(Guid id)
        {
            if (id == Guid.Empty)
                return Result.Failure<MenuId, ErrorList>(Error.Validation(nameof(id), "Id cannot be empty"));

            return new MenuId(id);
        }

        internal static MenuId CreateUnique() =>
            new MenuId(Guid.NewGuid());
    }
}
