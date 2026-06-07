namespace BuberDinner.Infrastructure.Persistence.Memory;

using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.User.Entities;
using System.Collections.Generic;
using System.Linq;

internal class UserInMemoryRepository : IRepository<User>
{
    private static readonly List<User> s_users = new();
    private static readonly object s_lock = new();

    public IEnumerable<User> GetAll(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Add(User user, CancellationToken cancellationToken)
    {
        lock (s_lock)
            s_users.Add(user);
        return ValueTask.CompletedTask;
    }

    public ValueTask Update(User entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Delete(User entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Maybe<User>> FindById(string id, CancellationToken cancellationToken)
    {
        User? user;
        lock (s_lock)
            user = s_users.SingleOrDefault(u => u.Id == id);
        return ValueTask.FromResult(Maybe.From(user));
    }
}
