namespace BuberDinner.Infrastructure.Persistence;

using BuberDinner.Application.Common.Interfaces.Persistence;
using BuberDinner.Domain.User.Entities;
using FunctionalDDD;
using System.Collections.Generic;
using System.Linq;

internal class UserInMemoryRepository : IUserRepository
{
    private static readonly List<User> s_users = new();

    public void Add(User user) => s_users.Add(user);

    public Task Add(User user, CancellationToken cancellationToken)
    {
        s_users.Add(user);
        return Task.CompletedTask;
    }

    public Task<Maybe<User>> GetUserByEmail(EmailAddress email, CancellationToken cancellationToken) =>
        Task.FromResult(s_users.SingleOrDefault(u => u.Email == email) ?? Maybe.None<User>());
}
