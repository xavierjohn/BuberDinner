namespace BuberDinner.Api.Tests._2022_12_21;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Priority;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class MenusControllerTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MenusControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact, Priority(1)]
    public async Task Not_Logged_In()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        string url = @"hosts/4F82063C-AE9D-4F5B-B676-DD781C14EFA0/menus/create?api-version=2022-10-01";
        string json = """
            {
              "name": "Muffins 'R' Us",
              "description": "Menu for Muffins 'R' Us",
              "sections": [
                {
                  "name": "Muffins",
                  "description": "The Muffins section",
                  "items": [
                    {
                      "name": "Blueberry",
                      "description": "A Blueberry Muffin"
                    }
                  ]
                }
              ]
            }
            """;

        // Act
        HttpResponseMessage response = await client.PostAsync(
            url,
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact, Priority(1)]
    public async Task Api_Version_Not_Specified()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        string url = @"hosts/4F82063C-AE9D-4F5B-B676-DD781C14EFA0/menus/create";
        string json = """
            {
              "name": "Muffins 'R' Us",
              "description": "Menu for Muffins 'R' Us",
              "sections": [
                {
                  "name": "Muffins",
                  "description": "The Muffins section",
                  "items": [
                    {
                      "name": "Blueberry",
                      "description": "A Blueberry Muffin"
                    }
                  ]
                }
              ]
            }
            """;

        // Act
        HttpResponseMessage response = await client.PostAsync(
            url,
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact, Priority(1)]
    public async Task Create_menu()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        string token = await Register(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        string url = @"hosts/4F82063C-AE9D-4F5B-B676-DD781C14EFA0/menus/create?api-version=2022-10-01";
        string json = """
            {
              "name": "Muffins 'R' Us",
              "description": "Menu for Muffins 'R' Us",
              "sections": [
                {
                  "name": "Muffins",
                  "description": "The Muffins section",
                  "items": [
                    {
                      "name": "Blueberry",
                      "description": "A Blueberry Muffin"
                    },
                    {
                      "name": "Chocolate",
                      "description": "A Chocolate Muffin"
                    },
                    {
                      "name": "Lemon",
                      "description": "A Lemon Muffin"
                    }
                  ]
                },
                {
                  "name": "Not Muffins",
                  "description": "Anything that is not a muffin",
                  "items": [
                    {
                      "name": "Cookie",
                      "description": "Probably oatmeal raisin or something"
                    },
                    {
                      "name": "Cupcake",
                      "description": "Its trying to be a muffin, but its not fooling anyone"
                    }
                  ]
                }
              ]
            }
            """;

        // Act
        HttpResponseMessage response = await client.PostAsync(
            url,
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await ValidateMenuResponse(response);
    }

    private static async Task<string> Register(HttpClient client)
    {
        string url = @"authentication/register";
        string json = """
            {
              "userId": "rbeldnar",
              "firstName": "Randlebomus",
              "lastName": "Beldnar",
              "email": "rbeldnar@sumo.net",
              "password": "SuperStr0ngPa$$word"
            }
            """;
        HttpResponseMessage response = await client.PostAsync(
            url,
            new StringContent(json, Encoding.UTF8, "application/json"));
        var registeredUser = await response.Content.ReadAsExample(new { token = default(string) });
        return registeredUser!.token!;
    }

    private static async Task ValidateMenuResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");

        var exampleItem = new { id = default(string), name = default(string), description = default(string) };
        var exampleSection = new
        {
            id = default(string),
            name = default(string),
            description = default(string),
            items = Enumerable.Repeat(exampleItem, 1).ToList()
        };

        var createdMenu = await response.Content.ReadAsExample(new
        {
            id = default(string),
            name = default(string),
            description = default(string),
            averageRating = default(decimal),
            hostId = default(string),
            sections = Enumerable.Repeat(exampleSection, 1).ToList(),
            dinnerIds = default(List<string>),
            menuReviewIds = default(List<string>)
        });

        if (createdMenu == null) return;
        createdMenu.id.Should().NotBeEmpty();
        createdMenu.name.Should().Be("Muffins 'R' Us");
        createdMenu.description.Should().Be("Menu for Muffins 'R' Us");
        createdMenu.averageRating.Should().Be(0);
        createdMenu.hostId.Should().BeEquivalentTo("4F82063C-AE9D-4F5B-B676-DD781C14EFA0");
        createdMenu.sections.Should().HaveCount(2);
        createdMenu.dinnerIds.Should().BeEmpty();
        createdMenu.menuReviewIds.Should().BeEmpty();

        var createdSection0 = createdMenu.sections[0];
        createdSection0.id.Should().NotBeEmpty();
        createdSection0.name.Should().Be("Muffins");
        createdSection0.description.Should().Be("The Muffins section");
        createdSection0.items.Should().HaveCount(3);

        var createdSection1 = createdMenu.sections[1];
        createdSection1.id.Should().NotBeEmpty();
        createdSection1.name.Should().Be("Not Muffins");
        createdSection1.description.Should().Be("Anything that is not a muffin");
        createdSection1.items.Should().HaveCount(2);

        var createdItem0_0 = createdSection0.items[0];
        createdItem0_0.id.Should().NotBeEmpty();
        createdItem0_0.name.Should().Be("Blueberry");
        createdItem0_0.description.Should().Be("A Blueberry Muffin");

        var createdItem0_1 = createdSection0.items[1];
        createdItem0_1.id.Should().NotBeEmpty();
        createdItem0_1.name.Should().Be("Chocolate");
        createdItem0_1.description.Should().Be("A Chocolate Muffin");

        var createdItem0_2 = createdSection0.items[2];
        createdItem0_2.id.Should().NotBeEmpty();
        createdItem0_2.name.Should().Be("Lemon");
        createdItem0_2.description.Should().Be("A Lemon Muffin");

        var createdItem1_0 = createdSection1.items[0];
        createdItem1_0.id.Should().NotBeEmpty();
        createdItem1_0.name.Should().Be("Cookie");
        createdItem1_0.description.Should().Be("Probably oatmeal raisin or something");

        var createdItem1_1 = createdSection1.items[1];
        createdItem1_1.id.Should().NotBeEmpty();
        createdItem1_1.name.Should().Be("Cupcake");
        createdItem1_1.description.Should().Be("Its trying to be a muffin, but its not fooling anyone");
    }
}
