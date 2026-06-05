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
        ListReservationsForDinnerQuery request, CancellationToken cancellationToken) =>
        await LoadOwnedDinnerAsync(request.DinnerId, request.HostId, cancellationToken)
            .BindAsync(_ => BuildPageAsync(request));

    private async ValueTask<Result<BuberDinner.Domain.Dinner.Entities.Dinner>> LoadOwnedDinnerAsync(
        DinnerId dinnerId, HostId hostId, CancellationToken cancellationToken) =>
        (await _dinnerRepo.FindById(dinnerId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<BuberDinner.Domain.Dinner.Entities.Dinner>(dinnerId)))
            .Ensure(
                d => d.HostId == hostId,
                new Error.NotFound(ResourceRef.For<BuberDinner.Domain.Dinner.Entities.Dinner>(dinnerId))
                {
                    Detail = "Dinner does not belong to the specified host.",
                });

    private ValueTask<Result<Page<Reservation>>> BuildPageAsync(ListReservationsForDinnerQuery request)
    {
        var pageSize = PageSize.FromRequested(request.Limit);
        var afterIdResult = DecodeCursor(request.Cursor);
        if (!afterIdResult.TryGetValue(out var afterId, out var cursorError))
            return ValueTask.FromResult(Result.Fail<Page<Reservation>>(cursorError));
        var overFetched = _reservationRepo.GetPageForDinner(request.DinnerId, pageSize, afterId);
        var page = PageBuilder.FromOverFetch(overFetched, pageSize, r => r.Id.Value);
        return ValueTask.FromResult(Result.Ok(page));
    }

    private static Result<System.Guid?> DecodeCursor(Cursor? cursor)
    {
        if (cursor is not { } c)
            return Result.Ok<System.Guid?>(null);
        return CursorCodec.TryDecode<System.Guid>(c, fieldName: "cursor")
            .Map<System.Guid, System.Guid?>(id => id);
    }
}
