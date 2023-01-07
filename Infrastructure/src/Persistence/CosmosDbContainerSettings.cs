namespace BuberDinner.Infrastructure.Persistence
{
    public abstract class CosmosDbContainerSettings
    {
        public string DatabaseName { get; set; } = "burberDinner";
        public abstract string ContainerName { get; set; }
    }
}
