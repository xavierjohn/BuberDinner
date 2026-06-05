namespace BuberDinner.Application.Dinners.Queries;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Host.ValueObject;
using Mediator;

/// <summary>
/// Lists dinners owned by the supplied host with cursor-based pagination (Cookbook Recipe 3).
/// </summary>
/// <remarks>
/// Both pagination knobs are protocol-level optionals: <see cref="Cursor"/> is <c>null</c>
/// on the first page; <see cref="Limit"/> falls back to <see cref="PageSize.Default"/> when
/// null or non-positive. Failures surface as <see cref="Error.InvalidInput"/> with reason
/// code <c>cursor.malformed</c> (HTTP 422) per <c>CursorCodec.TryDecode</c>.
/// </remarks>
public sealed class ListDinnersForHostQuery : IRequest<Result<Page<Dinner>>>
{
    public HostId HostId { get; }
    public Cursor? Cursor { get; }
    public int? Limit { get; }

    public ListDinnersForHostQuery(HostId hostId, Cursor? cursor = null, int? limit = null)
    {
        HostId = hostId;
        Cursor = cursor;
        Limit = limit;
    }
}

public sealed class ListDinnersForHostQueryHandler
    : IRequestHandler<ListDinnersForHostQuery, Result<Page<Dinner>>>
{
    private readonly IDinnerRepository _repo;

    public ListDinnersForHostQueryHandler(IDinnerRepository repo)
    {
        _repo = repo;
    }

    public ValueTask<Result<Page<Dinner>>> Handle(
        ListDinnersForHostQuery request, CancellationToken cancellationToken)
    {
        var pageSize = PageSize.FromRequested(request.Limit);

        // Cursor parsing must be ROP, not throwing (Cookbook Recipe 3 §352). Hand-rolling
        // Guid.Parse would throw on bad input and escape the handler as a 500; the
        // CursorCodec path returns Error.InvalidInput(cursor.malformed) -> HTTP 422.
        System.Guid? afterId = null;
        if (request.Cursor is { } cursor)
        {
            var decoded = CursorCodec.TryDecode<System.Guid>(cursor, fieldName: "cursor");
            if (!decoded.TryGetValue(out var id, out var cursorError))
                return ValueTask.FromResult(Result.Fail<Page<Dinner>>(cursorError));
            afterId = id;
        }

        var overFetched = _repo.GetPageForHost(request.HostId, pageSize, afterId);
        var page = PageBuilder.FromOverFetch(overFetched, pageSize, d => d.Id.Value);
        return ValueTask.FromResult(Result.Ok(page));
    }
}
