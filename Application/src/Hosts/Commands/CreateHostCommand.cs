namespace BuberDinner.Application.Hosts.Commands;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Host.Entities;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

public sealed class CreateHostCommand : IRequest<Result<Host>>
{
    public UserId OwnerId { get; }
    public Name DisplayName { get; }

    public static Result<CreateHostCommand> TryCreate(UserId ownerId, Name displayName) =>
        Result.Ok(new CreateHostCommand(ownerId, displayName));

    private CreateHostCommand(UserId ownerId, Name displayName)
    {
        OwnerId = ownerId;
        DisplayName = displayName;
    }
}
