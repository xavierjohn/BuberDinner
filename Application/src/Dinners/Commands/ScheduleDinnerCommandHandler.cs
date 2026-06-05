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
        // Recipe 22 — fail-loud on missing related aggregate. Without this the create
        // command would silently succeed against a non-existent menu, persisting an orphan
        // dinner pointing at nothing. NotFound (not Forbidden) keeps existence private from
        // the caller. The two cases — Menu missing vs Menu owned by another host — are
        // detected separately so the Detail string accurately reflects which one fired.
        var menu = await _menuRepository.FindById(request.MenuId.Value.ToString(), cancellationToken);
        if (menu is null)
            return Result.Fail<Dinner>(new Error.NotFound(ResourceRef.For<Menu>(request.MenuId)));
        if (menu.HostId != request.HostId)
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
