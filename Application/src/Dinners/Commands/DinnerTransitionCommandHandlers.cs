namespace BuberDinner.Application.Dinners.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using Mediator;

/// <summary>
/// Shared transition shape: load the dinner, verify route-host membership, invoke the
/// supplied domain transition (returns <see cref="Result{Dinner}"/>), persist on success.
/// </summary>
internal static class DinnerTransitionPipeline
{
    public static async ValueTask<Result<Dinner>> ApplyAsync(
        IRepository<Dinner> repo,
        BuberDinner.Domain.Host.ValueObject.HostId expectedHostId,
        BuberDinner.Domain.Dinner.ValueObject.DinnerId dinnerId,
        Func<Dinner, Result<Dinner>> transition,
        CancellationToken cancellationToken) =>
        await LoadOwnedDinnerAsync(repo, dinnerId, expectedHostId, cancellationToken)
            .BindAsync(dinner => transition(dinner))
            .TapAsync(dinner => repo.Update(dinner, cancellationToken));

    private static async ValueTask<Result<Dinner>> LoadOwnedDinnerAsync(
        IRepository<Dinner> repo,
        BuberDinner.Domain.Dinner.ValueObject.DinnerId dinnerId,
        BuberDinner.Domain.Host.ValueObject.HostId expectedHostId,
        CancellationToken cancellationToken) =>
        (await repo.FindById(dinnerId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Dinner>(dinnerId)))
            .Ensure(
                d => d.HostId == expectedHostId,
                new Error.NotFound(ResourceRef.For<Dinner>(dinnerId))
                {
                    Detail = "Dinner does not belong to the specified host.",
                });
}

public sealed class StartDinnerCommandHandler : ICommandHandler<StartDinnerCommand, Result<Dinner>>
{
    private readonly IRepository<Dinner> _repo;
    private readonly TimeProvider _clock;

    public StartDinnerCommandHandler(IRepository<Dinner> repo, TimeProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public ValueTask<Result<Dinner>> Handle(StartDinnerCommand request, CancellationToken cancellationToken) =>
        DinnerTransitionPipeline.ApplyAsync(_repo, request.HostId, request.DinnerId,
            dinner => dinner.Start(_clock), cancellationToken);
}

public sealed class EndDinnerCommandHandler : ICommandHandler<EndDinnerCommand, Result<Dinner>>
{
    private readonly IRepository<Dinner> _repo;
    private readonly TimeProvider _clock;

    public EndDinnerCommandHandler(IRepository<Dinner> repo, TimeProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public ValueTask<Result<Dinner>> Handle(EndDinnerCommand request, CancellationToken cancellationToken) =>
        DinnerTransitionPipeline.ApplyAsync(_repo, request.HostId, request.DinnerId,
            dinner => dinner.End(_clock), cancellationToken);
}

public sealed class CancelDinnerCommandHandler : ICommandHandler<CancelDinnerCommand, Result<Dinner>>
{
    private readonly IRepository<Dinner> _repo;
    private readonly TimeProvider _clock;

    public CancelDinnerCommandHandler(IRepository<Dinner> repo, TimeProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public ValueTask<Result<Dinner>> Handle(CancelDinnerCommand request, CancellationToken cancellationToken) =>
        DinnerTransitionPipeline.ApplyAsync(_repo, request.HostId, request.DinnerId,
            dinner => dinner.Cancel(request.Reason, _clock), cancellationToken);
}
