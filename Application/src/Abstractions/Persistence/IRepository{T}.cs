namespace BuberDinner.Application.Abstractions.Persistence;

public interface IRepository<T> where T : class
{
    IEnumerable<T> GetAll(CancellationToken cancellationToken);
    ValueTask Add(T entity, CancellationToken cancellationToken);
    ValueTask Update(T entity, CancellationToken cancellationToken);
    ValueTask Delete(T entity, CancellationToken cancellationToken);
    ValueTask<T?> FindById(string id, CancellationToken cancellationToken);
}
