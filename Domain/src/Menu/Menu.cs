namespace BuberDinner.Domain.Menu;

using System.Collections.Generic;
using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu.Entities;
using BuberDinner.Domain.Menu.ValueObject;
using FluentValidation;

public class Menu : Aggregate<MenuId>
{
    public Name Name { get; private set; }
    public Description Description { get; private set; }
    public decimal? AverageRating { get; }
    public IReadOnlyList<MenuSection> Sections => _menuSections.AsReadOnly();
    public HostId HostId { get; }
    public IReadOnlyList<DinnerId> DinnerIds => _dinnerIds.AsReadOnly();
    public IReadOnlyList<MenuReviewId> MenuReviewIds => _menuReviewIds.AsReadOnly();

    private readonly List<MenuSection> _menuSections = new();
    private readonly List<DinnerId> _dinnerIds = new();
    private readonly List<MenuReviewId> _menuReviewIds = new();

    public static Result<Menu> TryCreate(
        Name name,
        Description description,
        IReadOnlyList<MenuSection> sections,
        HostId host)
    {
        return TryCreate(
            MenuId.NewUniqueV7(),
            name,
            description,
            null,
            sections,
            host,
            null,
            null);
    }

    public static Result<Menu> TryCreate(
        MenuId menuId,
        Name name,
        Description description,
        decimal? averageRating,
        IReadOnlyList<MenuSection> sections,
        HostId host,
        IReadOnlyList<DinnerId>? dinnerIds,
        IReadOnlyList<MenuReviewId>? menuReviewIds)
    {
        Menu menu = new(menuId, name, description, averageRating, host);
        menu._menuSections.AddRange(sections);

        if (dinnerIds is not null)
        {
            menu._dinnerIds.AddRange(dinnerIds);
        }

        if (menuReviewIds is not null)
        {
            menu._menuReviewIds.AddRange(menuReviewIds);
        }

        return s_validator.ValidateToResult(menu);
    }


    private Menu(
        MenuId menuId,
        Name name,
        Description description,
        decimal? averageRating,
        HostId hostId)
        : base(menuId)
    {
        Name = name;
        Description = description;
        AverageRating = averageRating;
        HostId = hostId;
    }

    /// <summary>
    /// Updates the menu's name and description. Returns a validated <see cref="Result{Menu}"/>
    /// so callers can compose on the Result track. Mutation is applied speculatively and rolled
    /// back if aggregate-invariant validation fails — important because in-memory repositories
    /// hand out live aggregate references, so a half-applied mutation would otherwise leak into
    /// subsequent reads. Caller is responsible for the persistence commit (which bumps the
    /// optimistic-concurrency ETag) — see Recipe 23 in the cookbook.
    /// </summary>
    public Result<Menu> Update(Name name, Description description)
    {
        var previousName = Name;
        var previousDescription = Description;
        Name = name;
        Description = description;
        var result = s_validator.ValidateToResult(this);
        if (result.IsFailure)
        {
            Name = previousName;
            Description = previousDescription;
        }
        return result;
    }

    static readonly InlineValidator<Menu> s_validator = new()
    {
        v => v.RuleFor(x => x.Id).NotEmpty(),
        v => v.RuleFor(x => x.Name).NotEmpty(),
        v => v.RuleFor(x => x.Description).NotEmpty(),
        v => v.RuleFor(x => x.Sections).NotEmpty(),
        v => v.RuleFor(x => x.HostId).NotEmpty()
    };
}
