namespace BuberDinner.Application.Menus.Queries;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using Mediator;

/// <summary>
/// Lists menus owned by the supplied host with cursor-based pagination (Cookbook Recipe 3).
/// Same shape as <see cref="Dinners.Queries.ListDinnersForHostQuery"/>.
/// </summary>
public sealed class ListMenusForHostQuery : IRequest<Result<Page<Menu>>>
{
    public HostId HostId { get; }
    public Cursor? Cursor { get; }
    public int? Limit { get; }

    public ListMenusForHostQuery(HostId hostId, Cursor? cursor = null, int? limit = null)
    {
        HostId = hostId;
        Cursor = cursor;
        Limit = limit;
    }
}

public sealed class ListMenusForHostQueryHandler
    : IRequestHandler<ListMenusForHostQuery, Result<Page<Menu>>>
{
    private readonly IMenuRepository _repo;

    public ListMenusForHostQueryHandler(IMenuRepository repo)
    {
        _repo = repo;
    }

    public ValueTask<Result<Page<Menu>>> Handle(
        ListMenusForHostQuery request, CancellationToken cancellationToken)
    {
        var pageSize = PageSize.FromRequested(request.Limit);

        System.Guid? afterId = null;
        if (request.Cursor is { } cursor)
        {
            var decoded = CursorCodec.TryDecode<System.Guid>(cursor, fieldName: "cursor");
            if (!decoded.TryGetValue(out var id, out var cursorError))
                return ValueTask.FromResult(Result.Fail<Page<Menu>>(cursorError));
            afterId = id;
        }

        var overFetched = _repo.GetPageForHost(request.HostId, pageSize, afterId);
        var page = PageBuilder.FromOverFetch(overFetched, pageSize, m => m.Id.Value);
        return ValueTask.FromResult(Result.Ok(page));
    }
}
