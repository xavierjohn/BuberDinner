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
        var dinner = await _dinnerRepo.FindById(request.DinnerId.Value.ToString(), cancellationToken);
        if (dinner is null)
            return Result.Fail<Page<Reservation>>(
                new Error.NotFound(ResourceRef.For<BuberDinner.Domain.Dinner.Entities.Dinner>(request.DinnerId)));
        if (dinner.HostId != request.HostId)
            return Result.Fail<Page<Reservation>>(
                new Error.NotFound(ResourceRef.For<BuberDinner.Domain.Dinner.Entities.Dinner>(request.DinnerId))
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
