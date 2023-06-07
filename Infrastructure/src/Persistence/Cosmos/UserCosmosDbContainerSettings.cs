namespace BuberDinner.Infrastructure.Persistence.Cosmos;

public class UserCosmosDbContainerSettings : CosmosDbContainerSettings
{
    public override string ContainerName { get; set; } = "users";
    public override string PartitionKeyPath { get; set; } = "/id";
}
