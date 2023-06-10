namespace BuberDinner.Infrastructure.Persistence.Cosmos;

using System.Collections.Generic;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Menu;
using BuberDinner.Infrastructure.Persistence.Dto;
using Microsoft.Azure.Cosmos;

internal class MenuCosmosDbRepository : CosmosDbRepositoryBase, IRepository<Menu>
{
    public MenuCosmosDbRepository(CosmosClient cosmosClient, MenuCosmosDbContainerSettings containerSettings)
        : base(cosmosClient, containerSettings)
    {
    }

    public IEnumerable<Menu> GetAll(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async ValueTask Add(Menu menu, CancellationToken cancellationToken)
    {
        MenuDto menuDto = menu.ToDto();
        Container container = await GetContainer();
        await container.UpsertItemAsync(
            menuDto,
            new PartitionKey(menuDto.Id),
            cancellationToken: cancellationToken);
    }

    public ValueTask Update(Menu menu, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Delete(Menu menu, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    public async ValueTask<Menu?> FindById(string id, CancellationToken cancellationToken)
    {
        try
        {
            Container container = await GetContainer();
            ItemResponse<MenuDto> response = await container.ReadItemAsync<MenuDto>(
                id,
                new PartitionKey(id),
                cancellationToken: cancellationToken);
            return response.Resource.ToMenu();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
