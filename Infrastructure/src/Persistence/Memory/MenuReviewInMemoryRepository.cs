namespace BuberDinner.Infrastructure.Persistence.Memory;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Entities;

internal sealed class MenuReviewInMemoryRepository : IMenuReviewRepository
{
    private static readonly List<MenuReview> s_reviews = new();
    private static readonly ConcurrentDictionary<string, string> s_etags = new(StringComparer.Ordinal);
    private static readonly object s_lock = new();

    public IEnumerable<MenuReview> GetAll(CancellationToken cancellationToken)
    {
        lock (s_lock)
            return s_reviews.ToList();
    }

    public ValueTask Add(MenuReview review, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            s_reviews.Add(review);
            BumpETag(review);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask Update(MenuReview review, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            int index = s_reviews.FindIndex(r => r.Id == review.Id);
            if (index < 0)
                s_reviews.Add(review);
            else
                s_reviews[index] = review;
            BumpETag(review);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(MenuReview review, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            s_reviews.RemoveAll(r => r.Id == review.Id);
            s_etags.TryRemove(review.Id.Value.ToString(), out _);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<MenuReview?> FindById(string id, CancellationToken cancellationToken)
    {
        MenuReview? review;
        lock (s_lock)
            review = s_reviews.SingleOrDefault(r => r.Id.Value.ToString() == id);
        if (review is not null && s_etags.TryGetValue(id, out var etag))
            AggregateETagWriter.SetETag(review, etag);
        return ValueTask.FromResult(review);
    }

    public IReadOnlyList<MenuReview> GetPageForMenu(MenuId menuId, Trellis.PageSize pageSize, System.Guid? afterId)
    {
        lock (s_lock)
        {
            IEnumerable<MenuReview> source = s_reviews
                .Where(r => r.MenuId == menuId)
                .OrderBy(r => r.Id.Value);
            if (afterId is { } cursorId)
                source = source.Where(r => r.Id.Value.CompareTo(cursorId) > 0);
            return source.Take(pageSize.Applied + 1).ToList();
        }
    }

    private static void BumpETag(MenuReview review)
    {
        var newETag = Guid.NewGuid().ToString("N");
        s_etags[review.Id.Value.ToString()] = newETag;
        AggregateETagWriter.SetETag(review, newETag);
    }
}
