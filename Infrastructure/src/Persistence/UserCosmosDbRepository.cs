namespace BuberDinner.Infrastructure.Persistence;

using System.Collections.Generic;
using BuberDinner.Application.Abstractions.Persistence;
using Microsoft.Azure.Cosmos;
using User = Domain.User.Entities.User;

internal class UserCosmosDbRepository : IRepository<User>
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

    public IEnumerable<User> GetAll(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        var userDto = user.ToDto();
        await container.UpsertItemAsync(userDto, new PartitionKey(userDto.Id), cancellationToken: cancellationToken);
    }

    public Task Update(User entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Delete(User entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    public async Task<Maybe<User>> FindById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<UserDto>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return response.Resource.ToUser();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Maybe.None<User>();
        }
    }
}
