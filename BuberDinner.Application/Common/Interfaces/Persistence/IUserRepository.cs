namespace BuberDinner.Application.Common.Interfaces.Persistence;

using BuberDinner.Domain.Entities;

public interface IUserRepository
{
    User? GetUserByEmail(string email);

    void Add(User user);
}
