namespace BuberDinner.Application.Reservations.Queries;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.Entities;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Reservation.Entities;
using Mediator;
using Trellis.Authorization;

/// <summary>
/// Paginated list of reservations against a single dinner — the host's view of who's coming.
/// Gated by Host ownership via <see cref="IAuthorizeResource{TResource}"/>: only the host
/// that owns the dinner's parent host can list its reservations.
/// </summary>
public sealed class ListReservationsForDinnerQuery
    : IRequest<Result<Page<Reservation>>>, IAuthorizeResource<Host>, IIdentifyResource<Host, HostId>
{
    public HostId HostId { get; }
    public DinnerId DinnerId { get; }
    public Cursor? Cursor { get; }
    public int? Limit { get; }

    public ListReservationsForDinnerQuery(HostId hostId, DinnerId dinnerId, Cursor? cursor = null, int? limit = null)
    {
        HostId = hostId;
        DinnerId = dinnerId;
        Cursor = cursor;
        Limit = limit;
    }

    public HostId GetResourceId() => HostId;

    public IResult Authorize(Actor actor, Host host) =>
        host.OwnerId.Value == actor.Id.Value
            ? Result.Ok()
            : Result.Fail(new Error.Forbidden("reservations.host.owner", ResourceRef.For<Host>(HostId)));
}

public sealed class ListReservationsForDinnerQueryHandler
    : IRequestHandler<ListReservationsForDinnerQuery, Result<Page<Reservation>>>
{
    private readonly IReservationRepository _reservationRepo;
    private readonly IRepository<BuberDinner.Domain.Dinner.Entities.Dinner> _dinnerRepo;

    public ListReservationsForDinnerQueryHandler(
        IReservationRepository reservationRepo,
        IRepository<BuberDinner.Domain.Dinner.Entities.Dinner> dinnerRepo)
    {
        _reservationRepo = reservationRepo;
        _dinnerRepo = dinnerRepo;
    }

    public async ValueTask<Result<Page<Reservation>>> Handle(
        ListReservationsForDinnerQuery request, CancellationToken cancellationToken)
    {
        // Defense-in-depth: route auth already proved the caller owns the route host.
        // The dinner must additionally belong to that host (route-hierarchy check, mirrors
        // the GetMenu/GetDinner pattern from PR 1/2). NotFound on mismatch keeps the leak
        // surface consistent with the rest of the codebase.
        var dinner = await _dinnerRepo.FindById(request.DinnerId.Value.ToString(), cancellationToken);
        if (dinner is null || dinner.HostId != request.HostId)
            return Result.Fail<Page<Reservation>>(new Error.NotFound(ResourceRef.For<BuberDinner.Domain.Dinner.Entities.Dinner>(request.DinnerId))
            {
                Detail = "Dinner does not belong to the specified host.",
            });

        var pageSize = PageSize.FromRequested(request.Limit);

        System.Guid? afterId = null;
        if (request.Cursor is { } cursor)
        {
            var decoded = CursorCodec.TryDecode<System.Guid>(cursor, fieldName: "cursor");
            if (!decoded.TryGetValue(out var id, out var cursorError))
                return Result.Fail<Page<Reservation>>(cursorError);
            afterId = id;
        }

        var overFetched = _reservationRepo.GetPageForDinner(request.DinnerId, pageSize, afterId);
        var page = PageBuilder.FromOverFetch(overFetched, pageSize, r => r.Id.Value);
        return Result.Ok(page);
    }
}
