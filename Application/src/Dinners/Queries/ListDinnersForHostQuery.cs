namespace BuberDinner.Application.Dinners.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Host.ValueObject;
using Mediator;

/// <summary>
/// Lists all dinners owned by the supplied <see cref="HostId"/>. Simple (non-paginated)
/// for PR 2 — full <c>Page&lt;T&gt;</c> + <c>Cursor</c> pagination lands in PR 3.
/// </summary>
public sealed class ListDinnersForHostQuery : IRequest<Result<IReadOnlyList<Dinner>>>
{
    public HostId HostId { get; }

    public ListDinnersForHostQuery(HostId hostId)
    {
        HostId = hostId;
    }
}

public sealed class ListDinnersForHostQueryHandler
    : IRequestHandler<ListDinnersForHostQuery, Result<IReadOnlyList<Dinner>>>
{
    private readonly IDinnerRepository _repo;

    public ListDinnersForHostQueryHandler(IDinnerRepository repo)
    {
        _repo = repo;
    }

    public ValueTask<Result<IReadOnlyList<Dinner>>> Handle(
        ListDinnersForHostQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Dinner> dinners = _repo.GetForHost(request.HostId);
        return ValueTask.FromResult(Result.Ok(dinners));
    }
}
