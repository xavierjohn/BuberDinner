namespace BuberDinner.Infrastructure.Persistence.Cosmos;

using System.Collections.Generic;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Infrastructure.Persistence.Dto;
using Microsoft.Azure.Cosmos;
using User = Domain.User.Entities.User;

internal class UserCosmosDbRepository : CosmosDbRepositoryBase, IRepository<User>
{
    public UserCosmosDbRepository(CosmosClient cosmosClient, UserCosmosDbContainerSettings containerSettings)
        : base(cosmosClient, containerSettings)
    {
    }

    public IEnumerable<User> GetAll(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async ValueTask Add(User user, CancellationToken cancellationToken)
    {
        UserDto userDto = user.ToDto();
        Container container = await GetContainer();
        await container.UpsertItemAsync(
            userDto,
            new PartitionKey(userDto.Id),
            cancellationToken: cancellationToken);
    }

    public ValueTask Update(User entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Delete(User entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<User?> FindById(string id, CancellationToken cancellationToken)
    {
        try
        {
            Container container = await GetContainer();
            ItemResponse<UserDto> response = await container.ReadItemAsync<UserDto>(
                id,
                new PartitionKey(id),
                cancellationToken: cancellationToken);
            return response.Resource.ToUser();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
