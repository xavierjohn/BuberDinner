namespace BuberDinner.Infrastructure.Persistence.Cosmos;

public abstract class CosmosDbContainerSettings
{
    public string DatabaseName { get; set; } = "burberDinner";
    public abstract string ContainerName { get; set; }
    public abstract string PartitionKeyPath { get; set; }
}
