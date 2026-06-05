namespace BuberDinner.Domain.Host.Entities;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using FluentValidation;

/// <summary>
/// A Host owns Menus and (later) Dinners. Resource-based authorization on Menu mutations
/// is gated by the Host's <see cref="OwnerId"/> matching the calling actor's id.
/// </summary>
public class Host : Aggregate<HostId>
{
    public UserId OwnerId { get; }
    public Name DisplayName { get; }

    public static Result<Host> TryCreate(UserId ownerId, Name displayName) =>
        TryCreate(HostId.NewUniqueV7(), ownerId, displayName);

    public static Result<Host> TryCreate(HostId id, UserId ownerId, Name displayName)
    {
        Host host = new(id, ownerId, displayName);
        return s_validator.ValidateToResult(host);
    }

    private Host(HostId id, UserId ownerId, Name displayName)
        : base(id)
    {
        OwnerId = ownerId;
        DisplayName = displayName;
    }

    static readonly InlineValidator<Host> s_validator = new()
    {
        v => v.RuleFor(x => x.Id).NotEmpty(),
        v => v.RuleFor(x => x.OwnerId).NotNull(),
        v => v.RuleFor(x => x.DisplayName).NotNull(),
    };
}
