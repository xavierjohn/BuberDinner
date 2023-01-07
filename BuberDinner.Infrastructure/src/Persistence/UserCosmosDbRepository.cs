namespace BuberDinner.Infrastructure.Persistence;

using BuberDinner.Application.Common.Interfaces.Persistence;
using Microsoft.Azure.Cosmos;
using User = Domain.User.Entities.User;

internal class UserCosmosDbRepository : IUserRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly UserCosmosDbContainerSettings _containerSettings;

    public UserCosmosDbRepository(CosmosClient cosmosClient, UserCosmosDbContainerSettings containerSettings)
    {
        _cosmosClient = cosmosClient;
        _containerSettings = containerSettings;
    }

    private Container container
    {
        get => _cosmosClient.GetDatabase(_containerSettings.DatabaseName).GetContainer(_containerSettings.ContainerName);
    }

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        var userDto = user.ToDto();
        await container.UpsertItemAsync(userDto, new PartitionKey(userDto.Email), cancellationToken: cancellationToken);
    }

    public async Task<Maybe<User>> GetUserByEmail(EmailAddress email, CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<UserDto>(email, new PartitionKey(email), cancellationToken: cancellationToken);
            return response.Resource.ToUser();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Maybe.None<User>();
        }
    }
}
