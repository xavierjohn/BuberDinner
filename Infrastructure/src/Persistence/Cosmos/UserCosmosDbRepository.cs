namespace BuberDinner.Infrastructure.Persistence.Cosmos;

using BuberDinner.Infrastructure.Persistence.Dto;
using Microsoft.Azure.Cosmos;
using User = Domain.User.Entities.User;

internal sealed class UserCosmosDbRepository : CosmosDbRepositoryBase<User, UserDto>
{
    public UserCosmosDbRepository(CosmosClient cosmosClient, UserCosmosDbContainerSettings containerSettings)
        : base(cosmosClient, containerSettings)
    {
    }

    protected override UserDto ToDto(User entity) => entity.ToDto();

    protected override User ToEntity(UserDto dto) => dto.ToUser()!;

    protected override string GetId(UserDto dto) => dto.Id;
}
