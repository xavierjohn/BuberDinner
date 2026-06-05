namespace BuberDinner.Infrastructure.Persistence.Memory;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Dinner.Entities;
using BuberDinner.Domain.Host.ValueObject;

/// <summary>
/// In-memory <see cref="IRepository{T}"/> for <see cref="Dinner"/>. Mirrors
/// <see cref="MenuInMemoryRepository"/>: a static aggregate list plus a parallel ETag
/// dictionary, with the ETag written back onto the aggregate via reflection
/// (<see cref="AggregateETagWriter"/>) because the framework's ETag setter is internal
/// (framework regression reg-005).
/// </summary>
internal class DinnerInMemoryRepository : IDinnerRepository
{
    private static readonly List<Dinner> s_dinners = new();
    private static readonly ConcurrentDictionary<string, string> s_etags = new(StringComparer.Ordinal);
    private static readonly object s_lock = new();

    public IEnumerable<Dinner> GetAll(CancellationToken cancellationToken)
    {
        lock (s_lock)
            return s_dinners.ToList();
    }

    /// <summary>Reads every dinner owned by the supplied host. Used by the per-host list query.</summary>
    public IReadOnlyList<Dinner> GetForHost(HostId hostId)
    {
        lock (s_lock)
            return s_dinners.Where(d => d.HostId == hostId).ToList();
    }

    public ValueTask Add(Dinner dinner, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            s_dinners.Add(dinner);
            BumpETag(dinner);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask Update(Dinner dinner, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            int index = s_dinners.FindIndex(d => d.Id == dinner.Id);
            if (index < 0)
                s_dinners.Add(dinner);
            else
                s_dinners[index] = dinner;
            BumpETag(dinner);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(Dinner dinner, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            s_dinners.RemoveAll(d => d.Id == dinner.Id);
            s_etags.TryRemove(dinner.Id.Value.ToString(), out _);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<Dinner?> FindById(string id, CancellationToken cancellationToken)
    {
        Dinner? dinner;
        lock (s_lock)
            dinner = s_dinners.SingleOrDefault(d => d.Id.Value.ToString() == id);
        if (dinner is not null && s_etags.TryGetValue(id, out var etag))
            AggregateETagWriter.SetETag(dinner, etag);
        return ValueTask.FromResult(dinner);
    }

    private static void BumpETag(Dinner dinner)
    {
        var newETag = Guid.NewGuid().ToString("N");
        s_etags[dinner.Id.Value.ToString()] = newETag;
        AggregateETagWriter.SetETag(dinner, newETag);
    }
}
