namespace BuberDinner.Infrastructure.Persistence.Memory;

using System.Collections.Generic;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Host.Entities;

internal class HostInMemoryRepository : IRepository<Host>
{
    private static readonly List<Host> s_hosts = new();
    private static readonly object s_lock = new();

    public IEnumerable<Host> GetAll(CancellationToken cancellationToken)
    {
        lock (s_lock)
            return s_hosts.ToList();
    }

    public ValueTask Add(Host host, CancellationToken cancellationToken)
    {
        lock (s_lock)
            s_hosts.Add(host);
        return ValueTask.CompletedTask;
    }

    public ValueTask Update(Host host, CancellationToken cancellationToken)
    {
        lock (s_lock)
        {
            int index = s_hosts.FindIndex(h => h.Id == host.Id);
            if (index < 0)
                s_hosts.Add(host);
            else
                s_hosts[index] = host;
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask Delete(Host host, CancellationToken cancellationToken)
    {
        lock (s_lock)
            s_hosts.RemoveAll(h => h.Id == host.Id);
        return ValueTask.CompletedTask;
    }

    public ValueTask<Host?> FindById(string id, CancellationToken cancellationToken)
    {
        Host? host;
        lock (s_lock)
            host = s_hosts.SingleOrDefault(h => h.Id.Value.ToString() == id);
        return ValueTask.FromResult(host);
    }
}
