namespace BuberDinner.Infrastructure.Persistence;

using Azure.Identity;
using Microsoft.Azure.Cosmos;

public static class CosmosClientFactory
{
    public static CosmosClient InitializeCosmosClientInstance(CosmosDbClientSettings cosmosDbClientSettings)
    {
        var policy = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        return string.IsNullOrEmpty(cosmosDbClientSettings.AuthKeyOrResourceToken)
            ? new CosmosClient(cosmosDbClientSettings.AccountEndPoint, new DefaultAzureCredential(), policy)
            : new CosmosClient(cosmosDbClientSettings.AccountEndPoint, cosmosDbClientSettings.AuthKeyOrResourceToken, policy);
    }
}
