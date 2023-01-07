namespace BuberDinner.Infrastructure.Persistence
{
    public class UserCosmosDbContainerSettings : CosmosDbContainerSettings
    {
        public override string ContainerName { get; set; } = "users";
    }
}
