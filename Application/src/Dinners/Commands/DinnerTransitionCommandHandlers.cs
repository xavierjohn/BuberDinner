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
        CancellationToken cancellationToken)
    {
        var dinner = await repo.FindById(dinnerId.Value.ToString(), cancellationToken);
        if (dinner is null)
            return Result.Fail<Dinner>(new Error.NotFound(ResourceRef.For<Dinner>(dinnerId)));
        if (dinner.HostId != expectedHostId)
            return Result.Fail<Dinner>(new Error.NotFound(ResourceRef.For<Dinner>(dinnerId))
            {
                Detail = "Dinner does not belong to the specified host.",
            });

        var transitionResult = transition(dinner);
        if (transitionResult.IsFailure)
            return transitionResult;

        await repo.Update(dinner, cancellationToken);
        return transitionResult;
    }
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
