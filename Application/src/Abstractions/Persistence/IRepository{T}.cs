namespace BuberDinner.Application.Abstractions.Persistence;

using FunctionalDDD;

public interface IRepository<T> where T : class
{
    IEnumerable<T> GetAll(CancellationToken cancellationToken);
    Task Add(T entity, CancellationToken cancellationToken);
    Task Update(T entity, CancellationToken cancellationToken);
    Task Delete(T entity, CancellationToken cancellationToken);
    Task<Maybe<T>> FindById(string id, CancellationToken cancellationToken);
}
