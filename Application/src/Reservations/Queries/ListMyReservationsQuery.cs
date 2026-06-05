namespace BuberDinner.Application.Reservations.Queries;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Reservation.Entities;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;

/// <summary>
/// Paginated list of the calling guest's own reservations across every dinner.
/// Same cursor-pagination shape as PR 3's ListDinners / ListMenus.
/// </summary>
public sealed class ListMyReservationsQuery : IRequest<Result<Page<Reservation>>>
{
    public UserId CallerGuestUserId { get; }
    public Cursor? Cursor { get; }
    public int? Limit { get; }

    public ListMyReservationsQuery(UserId callerGuestUserId, Cursor? cursor = null, int? limit = null)
    {
        CallerGuestUserId = callerGuestUserId;
        Cursor = cursor;
        Limit = limit;
    }
}

public sealed class ListMyReservationsQueryHandler
    : IRequestHandler<ListMyReservationsQuery, Result<Page<Reservation>>>
{
    private readonly IReservationRepository _repo;

    public ListMyReservationsQueryHandler(IReservationRepository repo)
    {
        _repo = repo;
    }

    public ValueTask<Result<Page<Reservation>>> Handle(
        ListMyReservationsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = PageSize.FromRequested(request.Limit);

        System.Guid? afterId = null;
        if (request.Cursor is { } cursor)
        {
            var decoded = CursorCodec.TryDecode<System.Guid>(cursor, fieldName: "cursor");
            if (!decoded.TryGetValue(out var id, out var cursorError))
                return ValueTask.FromResult(Result.Fail<Page<Reservation>>(cursorError));
            afterId = id;
        }

        var overFetched = _repo.GetPageForGuest(request.CallerGuestUserId, pageSize, afterId);
        var page = PageBuilder.FromOverFetch(overFetched, pageSize, r => r.Id.Value);
        return ValueTask.FromResult(Result.Ok(page));
    }
}
