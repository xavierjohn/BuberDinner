namespace BuberDinner.Application.Common.Interfaces.Persistence;

using BuberDinner.Domain.User.Entities;
using CSharpFunctionalExtensions;

public interface IUserRepository
{
    Task<Maybe<User>> GetUserByEmail(string email, CancellationToken cancellationToken);

    void Add(User user);
}
