namespace BuberDinner.Application.Common.Interfaces.Persistence;

using BuberDinner.Domain.User.Entities;

public interface IUserRepository
{
    Task<Maybe<User>> GetUserByEmail(EmailAddress email, CancellationToken cancellationToken);

    Task Add(User user, CancellationToken cancellationToken);
}
