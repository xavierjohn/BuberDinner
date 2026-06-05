namespace BuberDinner.Application.Dinners.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.ValueObject;
using Mediator;

/// <summary>
/// Handles <see cref="ScheduleDinnerCommand"/>: verifies the menu exists and belongs to
/// the route host, builds the aggregate via <see cref="Dinner.TryCreate"/>, persists it,
/// and lets <c>DomainEventDispatchBehavior</c> publish the <c>DinnerScheduled</c> event
/// after the handler returns.
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

    public async ValueTask<Result<Dinner>> Handle(ScheduleDinnerCommand request, CancellationToken cancellationToken) =>
        await LoadMenuOwnedByHostAsync(request.MenuId, request.HostId, cancellationToken)
            .BindAsync(_ => Dinner.TryCreate(
                request.Name, request.Description,
                request.HostId, request.MenuId,
                request.StartDateTime, request.EndDateTime,
                _clock))
            .TapAsync(dinner => _dinnerRepository.Add(dinner, cancellationToken));

    private async ValueTask<Result<Menu>> LoadMenuOwnedByHostAsync(
        MenuId menuId, HostId hostId, CancellationToken cancellationToken) =>
        (await _menuRepository.FindById(menuId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Menu>(menuId)))
            .Ensure(
                m => m.HostId == hostId,
                new Error.NotFound(ResourceRef.For<Menu>(menuId))
                {
                    Detail = "Menu does not belong to the specified host.",
                });
}
