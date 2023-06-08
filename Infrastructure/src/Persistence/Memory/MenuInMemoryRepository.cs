namespace BuberDinner.Infrastructure.Persistence.Memory;

using System.Collections.Generic;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu;

internal class MenuInMemoryRepository : IRepository<Menu>
{
    private static readonly List<Menu> s_menus = new();
    public IEnumerable<Menu> GetAll(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Add(Menu menu, CancellationToken cancellationToken)
    {
        s_menus.Add(menu);
        return ValueTask.CompletedTask;
    }
    public ValueTask Update(Menu menu, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Delete(Menu menu, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Menu?> FindById(string id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
