namespace BuberDinner.Infrastructure.Persistence;

public class CosmosDbClientSettings
{
    public string AccountEndPoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account key.
    /// When null use DefaultAzureCredential  https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential
    /// </summary>
    public string? AuthKeyOrResourceToken { get; set; }
}
