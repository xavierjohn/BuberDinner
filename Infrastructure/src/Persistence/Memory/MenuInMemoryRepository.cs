namespace BuberDinner.Infrastructure.Persistence.Memory;

using System.Collections.Concurrent;
using System.Collections.Generic;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu;

internal class MenuInMemoryRepository : IRepository<Menu>
{
    private static readonly List<Menu> s_menus = new();
    // Parallel ETag store: in a real persistence layer the ETag is the row-version that the
    // database emits on write. The in-memory equivalent is just a monotonic GUID per save —
    // good enough to demonstrate If-Match concurrency control end-to-end.
    private static readonly ConcurrentDictionary<string, string> s_etags = new(StringComparer.Ordinal);
    private static readonly object s_lock = new();

    public IEnumerable<Menu> GetAll(CancellationToken cancellationToken)
    {
        lock (s_lock)
            return s_menus.ToList();
    }

    public ValueTask Add(Menu menu, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            s_menus.Add(menu);
            BumpETag(menu);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask Update(Menu menu, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            int index = s_menus.FindIndex(m => m.Id == menu.Id);
            if (index < 0)
                s_menus.Add(menu);
            else
                s_menus[index] = menu;
            BumpETag(menu);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(Menu menu, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            s_menus.RemoveAll(m => m.Id == menu.Id);
            s_etags.TryRemove(menu.Id.Value.ToString(), out _);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<Menu?> FindById(string id, CancellationToken cancellationToken)
    {
        Menu? menu;
        lock (s_lock)
            menu = s_menus.SingleOrDefault(m => m.Id.Value.ToString() == id);
        if (menu is not null && s_etags.TryGetValue(id, out var etag))
            AggregateETagWriter.SetETag(menu, etag);
        return ValueTask.FromResult(menu);
    }

    private static void BumpETag(Menu menu)
    {
        var newETag = Guid.NewGuid().ToString("N");
        s_etags[menu.Id.Value.ToString()] = newETag;
        AggregateETagWriter.SetETag(menu, newETag);
    }
}
