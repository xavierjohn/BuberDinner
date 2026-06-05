namespace BuberDinner.Api._2022_12_21.Models.Hosts;

using BuberDinner.Application.Hosts.Commands;
using BuberDinner.Domain.User.ValueObjects;
using NameClass = BuberDinner.Domain.Common.ValueObjects.Name;

/// <summary>
/// Request to register a new Host owned by the authenticated user.
/// </summary>
public record CreateHostRequest(string DisplayName)
{
    /// <summary>Validates the request and lifts it into a <see cref="CreateHostCommand"/>.</summary>
    public Result<CreateHostCommand> ToCreateHostCommand(UserId ownerId) =>
        NameClass.TryCreate(this.DisplayName)
            .Bind(name => CreateHostCommand.TryCreate(ownerId, name));
}

/// <summary>
/// Wire representation of a Host.
/// </summary>
public record HostResponse(string Id, string OwnerId, string DisplayName);
