namespace BuberDinner.Infrastructure.Tests;

using System.Threading.Tasks;
using BuberDinner.Infrastructure.Persistence;
using Microsoft.Azure.Cosmos;

public class CosmosDbFixture : IAsyncLifetime
{
    public CosmosDbFixture()
    {
        Environment.SetEnvironmentVariable("Persistence", "CosmosDb");
        var cosmosDbClientSettings = new CosmosDbClientSettings()
        {
            AccountEndPoint = "https://localhost:8081",
            AuthKeyOrResourceToken = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
        };
        CosmosClient = CosmosClientFactory.InitializeCosmosClientInstance(cosmosDbClientSettings);
    }

    public CosmosClient CosmosClient { get; }

    public async Task InitializeAsync()
    {
        UserCosmosDbContainerSettings settings = new();
        Database database = await CosmosClient.CreateDatabaseIfNotExistsAsync(settings.DatabaseName);
        await database.CreateContainerIfNotExistsAsync(settings.ContainerName, "/id");
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
