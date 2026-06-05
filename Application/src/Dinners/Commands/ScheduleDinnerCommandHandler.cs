namespace BuberDinner.Application.Dinners.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Menu;
using Mediator;

/// <summary>
/// Handles <see cref="ScheduleDinnerCommand"/>: validates that the chosen <c>MenuId</c>
/// belongs to the route <c>HostId</c>, builds the aggregate via <see cref="Dinner.TryCreate"/>,
/// persists it, and lets <c>DomainEventDispatchBehavior</c> publish the
/// <c>DinnerScheduled</c> event after the handler returns.
/// </summary>
public sealed class ScheduleDinnerCommandHandler : ICommandHandler<ScheduleDinnerCommand, Result<Dinner>>
{
    private readonly IRepository<Dinner> _dinnerRepository;
    private readonly IRepository<Menu> _menuRepository;
    private readonly TimeProvider _clock;

    public ScheduleDinnerCommandHandler(
        IRepository<Dinner> dinnerRepository,
        IRepository<Menu> menuRepository,
        TimeProvider clock)
    {
        _dinnerRepository = dinnerRepository;
        _menuRepository = menuRepository;
        _clock = clock;
    }

    public async ValueTask<Result<Dinner>> Handle(ScheduleDinnerCommand request, CancellationToken cancellationToken)
    {
        // Resource auth proves the caller owns the route Host. It does NOT prove the supplied
        // MenuId exists or belongs to that host. Without this load, a host could schedule a
        // dinner against another host's menu (cookbook Recipe 22 — fail-loud on missing related
        // aggregates).
        var menu = await _menuRepository.FindById(request.MenuId.Value.ToString(), cancellationToken);
        if (menu is null || menu.HostId != request.HostId)
            return Result.Fail<Dinner>(new Error.NotFound(ResourceRef.For<Menu>(request.MenuId))
            {
                Detail = "Menu does not belong to the specified host.",
            });

        var dinnerResult = Dinner.TryCreate(
            request.Name, request.Description,
            request.HostId, request.MenuId,
            request.StartDateTime, request.EndDateTime,
            _clock);
        if (dinnerResult.IsFailure)
            return dinnerResult;
        var dinner = dinnerResult.GetValueOrThrow("dinner");

        await _dinnerRepository.Add(dinner, cancellationToken);
        return Result.Ok(dinner);
    }
}
