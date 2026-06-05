namespace BuberDinner.Application.MenuReviews.Queries;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.MenuReview.Entities;
using BuberDinner.Domain.MenuReview.ValueObject;
using Mediator;

public sealed class GetMenuReviewQuery : IRequest<Result<MenuReview>>
{
    public MenuReviewId ReviewId { get; }
    public GetMenuReviewQuery(MenuReviewId reviewId) => ReviewId = reviewId;
}

public sealed class GetMenuReviewQueryHandler : IRequestHandler<GetMenuReviewQuery, Result<MenuReview>>
{
    private readonly IMenuReviewRepository _repo;

    public GetMenuReviewQueryHandler(IMenuReviewRepository repo)
    {
        _repo = repo;
    }

    public async ValueTask<Result<MenuReview>> Handle(GetMenuReviewQuery request, CancellationToken cancellationToken) =>
        (await _repo.FindById(request.ReviewId.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<MenuReview>(request.ReviewId)));
}
