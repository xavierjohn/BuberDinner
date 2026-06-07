namespace BuberDinner.Infrastructure.Persistence.Cosmos;

using BuberDinner.Application.Abstractions.Persistence;
using Microsoft.Azure.Cosmos;

/// <summary>
/// Shared Cosmos DB implementation of <see cref="IRepository{TEntity}"/>. Derived classes
/// supply only the entity↔DTO conversions and the DTO partition-key extractor — every
/// SDK touchpoint (container provisioning, upsert, 404→<see cref="Maybe{T}.None"/>) lives
/// here.
/// </summary>
internal abstract class CosmosDbRepositoryBase<TEntity, TDto> : IRepository<TEntity>
    where TEntity : class
    where TDto : class
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbContainerSettings _containerSettings;

    protected CosmosDbRepositoryBase(CosmosClient cosmosClient, CosmosDbContainerSettings containerSettings)
    {
        _cosmosClient = cosmosClient;
        _containerSettings = containerSettings;
    }

    protected abstract TDto ToDto(TEntity entity);

    protected abstract TEntity ToEntity(TDto dto);

    protected abstract string GetId(TDto dto);

    public IEnumerable<TEntity> GetAll(CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public async ValueTask Add(TEntity entity, CancellationToken cancellationToken)
    {
        TDto dto = ToDto(entity);
        Container container = await GetContainer(cancellationToken);
        await container.UpsertItemAsync(
            dto,
            new PartitionKey(GetId(dto)),
            cancellationToken: cancellationToken);
    }

    public ValueTask Update(TEntity entity, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public ValueTask Delete(TEntity entity, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public async ValueTask<Maybe<TEntity>> FindById(string id, CancellationToken cancellationToken)
    {
        try
        {
            Container container = await GetContainer(cancellationToken);
            ItemResponse<TDto> response = await container.ReadItemAsync<TDto>(
                id,
                new PartitionKey(id),
                cancellationToken: cancellationToken);
            return Maybe.From(ToEntity(response.Resource));
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Maybe<TEntity>.None;
        }
    }

    private async Task<Container> GetContainer(CancellationToken cancellationToken)
    {
        await _cosmosClient.CreateDatabaseIfNotExistsAsync(
            _containerSettings.DatabaseName,
            cancellationToken: cancellationToken);
        Database database = _cosmosClient.GetDatabase(_containerSettings.DatabaseName);
        await database.CreateContainerIfNotExistsAsync(
            _containerSettings.ContainerName,
            _containerSettings.PartitionKeyPath,
            cancellationToken: cancellationToken);
        return database.GetContainer(_containerSettings.ContainerName);
    }
}
