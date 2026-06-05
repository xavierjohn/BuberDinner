namespace BuberDinner.Application.MenuReviews.Queries;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Entities;
using Mediator;

public sealed class ListReviewsForMenuQuery : IRequest<Result<Page<MenuReview>>>
{
    public MenuId MenuId { get; }
    public Cursor? Cursor { get; }
    public int? Limit { get; }

    public ListReviewsForMenuQuery(MenuId menuId, Cursor? cursor = null, int? limit = null)
    {
        MenuId = menuId;
        Cursor = cursor;
        Limit = limit;
    }
}

public sealed class ListReviewsForMenuQueryHandler
    : IRequestHandler<ListReviewsForMenuQuery, Result<Page<MenuReview>>>
{
    private readonly IMenuReviewRepository _repo;

    public ListReviewsForMenuQueryHandler(IMenuReviewRepository repo)
    {
        _repo = repo;
    }

    public ValueTask<Result<Page<MenuReview>>> Handle(
        ListReviewsForMenuQuery request, CancellationToken cancellationToken)
    {
        var pageSize = PageSize.FromRequested(request.Limit);

        System.Guid? afterId = null;
        if (request.Cursor is { } cursor)
        {
            var decoded = CursorCodec.TryDecode<System.Guid>(cursor, fieldName: "cursor");
            if (!decoded.TryGetValue(out var id, out var cursorError))
                return ValueTask.FromResult(Result.Fail<Page<MenuReview>>(cursorError));
            afterId = id;
        }

        var overFetched = _repo.GetPageForMenu(request.MenuId, pageSize, afterId);
        var page = PageBuilder.FromOverFetch(overFetched, pageSize, r => r.Id.Value);
        return ValueTask.FromResult(Result.Ok(page));
    }
}
