namespace BuberDinner.Infrastructure;

using Microsoft.Azure.Cosmos;

public static class CosmosClientFactory
{
    public static CosmosClient InitializeCosmosClientInstance()
    {
        var policy = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Direct,
            MaxRetryAttemptsOnRateLimitedRequests = 3,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
            RequestTimeout = TimeSpan.FromSeconds(10),
            AllowBulkExecution = true,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        var wellKnownCosmosDbEmulatorAuthenticationKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        return new CosmosClient("https://localhost:8081", wellKnownCosmosDbEmulatorAuthenticationKey, policy);
    }
}
