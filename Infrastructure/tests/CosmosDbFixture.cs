namespace BuberDinner.Infrastructure.Tests;

using System.Threading.Tasks;
using BuberDinner.Infrastructure.Persistence;
using Microsoft.Azure.Cosmos;

public class CosmosDbFixture : IAsyncLifetime
{
    public CosmosDbFixture() => Environment.SetEnvironmentVariable("Persistence", "CosmosDb");

    public CosmosClient CosmosClient { get; } = CosmosClientFactory.InitializeCosmosClientInstance();

    public async Task InitializeAsync()
    {
        UserCosmosDbContainerSettings settings = new();
        Database database = await CosmosClient.CreateDatabaseIfNotExistsAsync(settings.DatabaseName);
        await database.CreateContainerIfNotExistsAsync(settings.ContainerName, "/id");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
