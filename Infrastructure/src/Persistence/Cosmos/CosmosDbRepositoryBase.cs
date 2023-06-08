namespace BuberDinner.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;

internal abstract class CosmosDbRepositoryBase
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbContainerSettings _containerSettings;

    public CosmosDbRepositoryBase(CosmosClient cosmosClient, CosmosDbContainerSettings containerSettings)
    {
        _cosmosClient = cosmosClient;
        _containerSettings = containerSettings;
    }

    protected async Task<Container> GetContainer()
    {
        await _cosmosClient.CreateDatabaseIfNotExistsAsync(_containerSettings.DatabaseName);
        Database database = _cosmosClient.GetDatabase(_containerSettings.DatabaseName);
        await database.CreateContainerIfNotExistsAsync(
            _containerSettings.ContainerName,
            _containerSettings.PartitionKeyPath);
        return database.GetContainer(_containerSettings.ContainerName);
    }
}
