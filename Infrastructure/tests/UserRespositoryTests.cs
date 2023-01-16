namespace BuberDinner.Infrastructure.Tests;

using BuberDinner.Domain.User.Entities;
using BuberDinner.Domain.User.ValueObjects;
using BuberDinner.Infrastructure.Persistence;
using Xunit.Categories;

[Category("Integration")]
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
        var userId = UserId.Create("xavierjohn2023").Value;
        var firstName = FirstName.Create("Xavier").Value;
        var lastName = LastName.Create("John").Value;
        var email = EmailAddress.Create("xavier@somewhere.com").Value;
        var password = Password.Create("Amiko1232!").Value;
        User user = User.Create(userId, firstName, lastName, email, password).Value;


        // Act
        await rep.Add(user, CancellationToken.None);
        var maybeUser = await rep.FindById(userId, CancellationToken.None);

        // Assert
        maybeUser.HasValue.Should().BeTrue();
        User dbuser = maybeUser.Value;
        dbuser.Should().Be(user); // For entity only Id is checked for equality.
        dbuser.Id.Should().Be(userId);
        dbuser.FirstName.Should().Be(firstName);
        dbuser.LastName.Should().Be(lastName);
        dbuser.Email.Should().Be(email);
        dbuser.Password.Should().Be(password);
    }
}
