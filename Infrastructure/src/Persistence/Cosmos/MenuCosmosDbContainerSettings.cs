namespace BuberDinner.Infrastructure.Persistence.Cosmos;

public class MenuCosmosDbContainerSettings : CosmosDbContainerSettings
{
    public override string ContainerName { get; set; } = "menus";
    public override string PartitionKeyPath { get; set; } = "/id";
}
