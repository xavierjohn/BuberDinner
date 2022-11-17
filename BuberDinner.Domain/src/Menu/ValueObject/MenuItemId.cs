namespace BuberDinner.Domain.Menu.ValueObject
{
    using System;
    using CSharpFunctionalExtensions;
    using CSharpFunctionalExtensions.Errors;

    public class MenuItemId : SimpleValueObject<Guid>
    {
        private MenuItemId(Guid value) : base(value)
        {
        }

        public static Result<MenuItemId, ErrorList> Create(Guid id)
        {
            if (id == Guid.Empty)
                return Result.Failure<MenuItemId, ErrorList>(Error.Validation(nameof(id), "Id cannot be empty"));

            return new MenuItemId(id);
        }

        public static MenuItemId CreateUnique() => new(Guid.NewGuid());
    }
}
