namespace BuberDinner.Infrastructure.Persistence.Cosmos;

using BuberDinner.Domain.Menu;
using BuberDinner.Infrastructure.Persistence.Dto;
using Microsoft.Azure.Cosmos;

internal sealed class MenuCosmosDbRepository : CosmosDbRepositoryBase<Menu, MenuDto>
{
    public MenuCosmosDbRepository(CosmosClient cosmosClient, MenuCosmosDbContainerSettings containerSettings)
        : base(cosmosClient, containerSettings)
    {
    }

    protected override MenuDto ToDto(Menu entity) => entity.ToDto();

    protected override Menu ToEntity(MenuDto dto) => dto.ToMenu()!;

    protected override string GetId(MenuDto dto) => dto.Id;
}
