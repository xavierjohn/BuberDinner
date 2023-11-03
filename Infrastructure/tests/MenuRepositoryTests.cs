namespace BuberDinner.Infrastructure.Tests;

using BuberDinner.Domain.Common.ValueObjects;
using BuberDinner.Domain.Dinner.ValueObject;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.Entities;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Infrastructure.Persistence.Cosmos;
using Xunit.Categories;

[Category("ComponentTests")]
public class MenuRepositoryTests : IClassFixture<CosmosDbFixture>
{
    private readonly CosmosDbFixture _cosmosDbFixture;

    public MenuRepositoryTests(CosmosDbFixture cosmosDbFixture) =>
        _cosmosDbFixture = cosmosDbFixture;

    [Fact]
    public async Task Can_read_and_write_Menu_from_storage()
    {
        // Arrange
        MenuCosmosDbRepository rep = new(
            _cosmosDbFixture.CosmosClient,
            new MenuCosmosDbContainerSettings());
        MenuId menuId = MenuId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E955").Value;
        Name name = Name.TryCreate("Menu Name").Value;
        Description description = Description.TryCreate("Menu Description").Value;
        HostId hostId = HostId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E955").Value;
        decimal? averageRating = 3.8m;
        MenuSectionId sectionId = MenuSectionId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E956").Value;
        Name sectionName = Name.TryCreate("Section Name").Value;
        Description sectionDescription = Description.TryCreate("Section Description").Value;
        MenuItemId itemId = MenuItemId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E957").Value;
        Name itemName = Name.TryCreate("Item Name").Value;
        Description itemDescription = Description.TryCreate("Item Description").Value;
        MenuItem item = MenuItem.New(itemId, itemName, itemDescription).Value;
        MenuSection section = MenuSection.New(
            sectionId,
            sectionName,
            sectionDescription,
            new List<MenuItem>() { item }).Value;
        DinnerId dinnerId = DinnerId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E958").Value;
        MenuReviewId menuReviewId = MenuReviewId.TryCreate("2F45ACF9-6E51-4DC7-8732-DBE7F260E959").Value;
        Menu menu = Menu.New(
            menuId,
            name,
            description,
            averageRating,
            new List<MenuSection>() { section },
            hostId,
            new List<DinnerId>() { dinnerId },
            new List<MenuReviewId>() { menuReviewId }).Value;


        // Act
        await rep.Add(menu, CancellationToken.None);
        Menu? dbMenu = await rep.FindById(menuId.ToString(), CancellationToken.None);

        // Assert
        dbMenu.Should().NotBeNull();
        if (dbMenu == null) return;
        dbMenu.Should().Be(menu); // For entity only Id is checked for equality.
        dbMenu.Id.Should().Be(menuId);
        dbMenu.Name.Should().Be(name);
        dbMenu.Description.Should().Be(description);
        dbMenu.HostId.Should().Be(hostId);
        dbMenu.AverageRating.Should().Be(averageRating);
        dbMenu.Sections.Should().NotBeNull().And.HaveCount(1);
        dbMenu.Sections[0].Should().NotBeNull();
        dbMenu.Sections[0].Id.Should().Be(sectionId);
        dbMenu.Sections[0].Name.Should().Be(sectionName);
        dbMenu.Sections[0].Description.Should().Be(sectionDescription);
        dbMenu.Sections[0].Items.Should().NotBeNull().And.HaveCount(1);
        dbMenu.Sections[0].Items[0].Should().NotBeNull();
        dbMenu.Sections[0].Items[0].Id.Should().Be(itemId);
        dbMenu.Sections[0].Items[0].Name.Should().Be(itemName);
        dbMenu.Sections[0].Items[0].Description.Should().Be(itemDescription);
        dbMenu.DinnerIds.Should().NotBeNull().And.HaveCount(1);
        dbMenu.DinnerIds[0].Should().Be(dinnerId);
        dbMenu.MenuReviewIds.Should().NotBeNull().And.HaveCount(1);
        dbMenu.MenuReviewIds[0].Should().Be(menuReviewId);
    }
}
