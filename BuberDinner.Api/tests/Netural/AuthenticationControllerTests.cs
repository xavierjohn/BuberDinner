namespace BuberDinner.Api.Tests.Netural;

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Priority;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class AuthenticationControllerTests
: IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    [Fact, Priority(1)]
    public async Task Login_with_unregistered_user()
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = @"authentication/login";
        var json = """
            {
                "email":"someone@somewhere.com",
                "password":"Amiko1232!"
            }
            """;

        // Act
        var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        response.Content.Headers.ContentType.ToString().Should().Be("application/json; charset=utf-8");
#pragma warning restore CS8602 // Dereference of a possibly null reference.        
    }

    [Fact, Priority(2)]
    public async Task Register_user()
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = @"authentication/register";
        var json = """
            {
                "firstName":"Xavier",
                "lastName":"John",
                "email":"someone@somewhere.com",
                "password":"Amiko1232!"
            }
            """;

        // Act
        var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        await ValidateAuthenticationResponse(response);
    }

    [Fact, Priority(3)]
    public async Task Login_with_registered_user()
    {
        // Arrange
        var client = _factory.CreateClient();
        var url = @"authentication/login";
        var json = """
            {
                "email":"someone@somewhere.com",
                "password":"Amiko1232!"
            }
            """;

        // Act
        var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert
        await ValidateAuthenticationResponse(response);
    }

    private static async Task ValidateAuthenticationResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        response.Content.Headers.ContentType.ToString().Should().Be("application/json; charset=utf-8");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        var registeredUser = await response.Content.ReadAsExample(new { userId = default(string), firstName = default(string), lastName = default(string), email = default(string) });
        registeredUser.Should().BeEquivalentTo(new
        {
            firstName = "Xavier",
            lastName = "John",
            email = "someone@somewhere.com"
        });

        if (registeredUser == null) return;
        Guid.TryParse(registeredUser.userId, out var parsedUserId).Should().BeTrue();
    }
}
