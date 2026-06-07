namespace BuberDinner.Infrastructure.Persistence.Memory;

using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.User.Entities;
using System.Collections.Generic;
using System.Linq;

internal class UserInMemoryRepository : IRepository<User>
{
    private static readonly List<User> s_users = new();
    public IEnumerable<User> GetAll(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Add(User user, CancellationToken cancellationToken)
    {
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

    public ValueTask<Maybe<User>> FindById(string id, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Maybe.From(s_users.SingleOrDefault(u => u.Id == id)));
}
