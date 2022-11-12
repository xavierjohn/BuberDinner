namespace BuberDinner.Application.Common.Interfaces.Persistence;

using BuberDinner.Domain.Entities;
using CSharpFunctionalExtensions;

public interface IUserRepository
{
    Task<Maybe<User>> GetUserByEmail(string email);

    void Add(User user);
}
