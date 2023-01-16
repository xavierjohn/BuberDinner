namespace BuberDinner.Infrastructure.Persistence;

using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.User.Entities;
using FunctionalDDD;
using System.Collections.Generic;
using System.Linq;

internal class UserInMemoryRepository : IRepository<User>
{
    private static readonly List<User> s_users = new();
    public IEnumerable<User> GetAll(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Add(User user, CancellationToken cancellationToken)
    {
        s_users.Add(user);
        return Task.CompletedTask;
    }
    public Task Update(User entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Delete(User entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Maybe<User>> FindById(string id, CancellationToken cancellationToken) =>
    Task.FromResult(s_users.SingleOrDefault(u => u.Id == id) ?? Maybe.None<User>());

}
