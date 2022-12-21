namespace BuberDinner.Domain.Menu
{
    using System.Collections.Generic;
    using BuberDinner.Domain.Menu.Entities;
    using BuberDinner.Domain.Menu.ValueObject;
    using FluentValidation;
    using FunctionalDDD;
    using FunctionalDDD.FluentValidation;

    public class Menu : AggregateRoot<MenuId>
    {
        public string Name { get; }
        public string Description { get; }
        public decimal AverageRating { get; }
        public IReadOnlyList<MenuSection> Section => _menuSections.AsReadOnly();
        public HostId HostId { get; }
        public IReadOnlyList<DinnerId> DinnerIds => _dinnerIds.AsReadOnly();

        private readonly List<MenuSection> _menuSections = new();
        private readonly List<DinnerId> _dinnerIds = new();

        public static Result<Menu> Create(string name, string description, HostId host)
        {
            Menu menu = new(MenuId.CreateUnique(), name, description, host);
            return s_validator.ValidateToResult(menu);
        }

        private Menu(MenuId menuId, string name, string description, HostId hostId) : base(menuId)
        {
            Name = name;
            Description = description;
            HostId = hostId;
        }

        static readonly InlineValidator<Menu> s_validator = new()
        {
            v => v.RuleFor(x => x.Name).NotEmpty(),
            v => v.RuleFor(x => x.Description).NotEmpty(),
        };
    }
}
