namespace BuberDinner.Infrastructure.Tests;

using BuberDinner.Domain.User.Entities;
using BuberDinner.Domain.User.ValueObjects;
using BuberDinner.Infrastructure.Persistence.Cosmos;
using Xunit.Categories;

[Category("ComponentTests")]
public class UserRespositoryTests : IClassFixture<CosmosDbFixture>
{
    private readonly CosmosDbFixture _cosmosDbFixture;

    public UserRespositoryTests(CosmosDbFixture cosmosDbFixture) =>
        _cosmosDbFixture = cosmosDbFixture;

    [Fact]
    public async Task Can_read_and_write_User_from_storage()
    {
        // Arrange
        UserCosmosDbRepository rep = new(_cosmosDbFixture.CosmosClient, new UserCosmosDbContainerSettings());
        var userId = UserId.TryCreate("xavierjohn2023").Value;
        var firstName = FirstName.TryCreate("Xavier").Value;
        var lastName = LastName.TryCreate("John").Value;
        var email = EmailAddress.TryCreate("xavier@somewhere.com").Value;
        var password = Password.TryCreate("Amiko1232!").Value;
        User user = User.TryCreate(userId, firstName, lastName, email, password).Value;


        // Act
        await rep.Add(user, CancellationToken.None);
        User? dbuser = await rep.FindById(userId, CancellationToken.None);

        // Assert
        dbuser.Should().NotBeNull();
        if (dbuser == null) return;
        dbuser.Should().Be(user); // For entity only Id is checked for equality.
        dbuser.Id.Should().Be(userId);
        dbuser.FirstName.Should().Be(firstName);
        dbuser.LastName.Should().Be(lastName);
        dbuser.Email.Should().Be(email);
        dbuser.Password.Should().Be(password);
    }
}
