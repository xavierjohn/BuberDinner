namespace BuberDinner.Application.Dinners.Queries;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using Mediator;

public sealed class GetDinnerQuery : IRequest<Result<Dinner>>
{
    public HostId HostId { get; }
    public DinnerId DinnerId { get; }

    public GetDinnerQuery(HostId hostId, DinnerId dinnerId)
    {
        HostId = hostId;
        DinnerId = dinnerId;
    }
}

public sealed class GetDinnerQueryHandler : IRequestHandler<GetDinnerQuery, Result<Dinner>>
{
    private readonly IRepository<Dinner> _repo;

    public GetDinnerQueryHandler(IRepository<Dinner> repo)
    {
        _repo = repo;
    }

    public async ValueTask<Result<Dinner>> Handle(GetDinnerQuery request, CancellationToken cancellationToken) =>
        (await _repo.FindById(request.DinnerId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Dinner>(request.DinnerId)))
            .Ensure(
                d => d.HostId == request.HostId,
                new Error.NotFound(ResourceRef.For<Dinner>(request.DinnerId))
                {
                    Detail = "Dinner does not belong to the specified host.",
                });
}
