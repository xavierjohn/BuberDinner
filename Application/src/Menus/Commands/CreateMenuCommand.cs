namespace BuberDinner.Application.Menus.Commands;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using Mediator;

public class CreateMenuCommand : IRequest<Result<Menu>>
{
    public static Result<CreateMenuCommand> TryCreate(
        Name name,
        Description description,
        IReadOnlyList<MenuSectionCommand> sections,
        HostId hostId) =>
            new CreateMenuCommand(name, description, sections, hostId);

    public Name Name { get; }
    public Description Description { get; }
    public IReadOnlyList<MenuSectionCommand> Sections { get; }
    public HostId HostId { get; }

    private CreateMenuCommand(
        Name name,
        Description description,
        IReadOnlyList<MenuSectionCommand> sections,
        HostId hostId)
    {
        Name = name;
        Description = description;
        Sections = sections;
        HostId = hostId;
    }
}

public class MenuSectionCommand
{
    public static Result<MenuSectionCommand> TryCreate(
        Name name,
        Description description,
        IReadOnlyList<MenuItemCommand> items) =>
            new MenuSectionCommand(name, description, items);

    public Name Name { get; }
    public Description Description { get; }
    public IReadOnlyList<MenuItemCommand> Items { get; }

    private MenuSectionCommand(Name name, Description description, IReadOnlyList<MenuItemCommand> items)
    {
        Name = name;
        Description = description;
        Items = items;
    }
}

public class MenuItemCommand
{
    public static Result<MenuItemCommand> TryCreate(
        Name name,
        Description description) =>
            new MenuItemCommand(name, description);

    public Name Name { get; }
    public Description Description { get; }

    private MenuItemCommand(Name name, Description description)
    {
        Name = name;
        Description = description;
    }
}
