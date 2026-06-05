namespace BuberDinner.Api.Tests._2022_12_21;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Priority;

/// <summary>
/// Integration tests for the resource-authorization and ETag/precondition behaviour added by PR 1:
///   * GET   /hosts/{hostId:HostId}/menus/{menuId:MenuId} — Cookbook Recipe 6 (WithETag + EvaluatePreconditions)
///   * PUT   /hosts/{hostId:HostId}/menus/{menuId:MenuId} — Cookbook Recipe 23 (RequireETag in handler chain)
///   * POST  /hosts                                       — establishes the Host that gates auth on PUT
///
/// Each test runs end-to-end through the WebApplicationFactory and shares no state with peers
/// (each test registers a fresh user so the in-memory User repo stays consistent).
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class MenuEtagAndAuthTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MenuEtagAndAuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact, Priority(2)]
    public async Task Create_host_returns_201_with_owner_id()
    {
        var (client, _, userId) = await NewAuthenticatedClient();

        var (status, body) = await CreateHostAsync(client, "Bistro 33");

        status.Should().Be(HttpStatusCode.Created);
        body!.Id.Should().NotBeNullOrEmpty();
        body.OwnerId.Should().Be(userId);
        body.DisplayName.Should().Be("Bistro 33");
    }

    [Fact, Priority(2)]
    public async Task Get_menu_returns_200_with_strong_etag()
    {
        var (client, _, _) = await NewAuthenticatedClient();
        var hostId = (await CreateHostAsync(client, "Eggs Inc")).Body!.Id;
        var menuId = await CreateMenuAsync(client, hostId);

        var response = await client.GetAsync(MenuUrl(hostId, menuId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag!.IsWeak.Should().BeFalse("Recipe 6 emits a strong ETag");
        response.Headers.ETag.Tag.Should().StartWith("\"").And.EndWith("\"");
    }

    [Fact, Priority(2)]
    public async Task Get_menu_with_matching_if_none_match_returns_304()
    {
        var (client, _, _) = await NewAuthenticatedClient();
        var hostId = (await CreateHostAsync(client, "Brunch Co")).Body!.Id;
        var menuId = await CreateMenuAsync(client, hostId);

        var firstGet = await client.GetAsync(MenuUrl(hostId, menuId));
        var etag = firstGet.Headers.ETag!.Tag;

        var conditional = new HttpRequestMessage(HttpMethod.Get, MenuUrl(hostId, menuId));
        conditional.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var notModified = await client.SendAsync(conditional);

        notModified.StatusCode.Should().Be(HttpStatusCode.NotModified);
        notModified.Headers.ETag!.Tag.Should().Be(etag);
    }

    [Fact, Priority(2)]
    public async Task Put_menu_without_if_match_returns_428()
    {
        var (client, _, _) = await NewAuthenticatedClient();
        var hostId = (await CreateHostAsync(client, "No-Precondition Cafe")).Body!.Id;
        var menuId = await CreateMenuAsync(client, hostId);

        var put = new HttpRequestMessage(HttpMethod.Put, MenuUrl(hostId, menuId))
        {
            Content = JsonBody(new { name = "Updated", description = "Updated description" })
        };
        var response = await client.SendAsync(put);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionRequired);
    }

    [Fact, Priority(2)]
    public async Task Put_menu_with_stale_if_match_returns_412()
    {
        var (client, _, _) = await NewAuthenticatedClient();
        var hostId = (await CreateHostAsync(client, "Stale Co")).Body!.Id;
        var menuId = await CreateMenuAsync(client, hostId);

        var put = new HttpRequestMessage(HttpMethod.Put, MenuUrl(hostId, menuId))
        {
            Content = JsonBody(new { name = "Updated", description = "Updated description" })
        };
        put.Headers.TryAddWithoutValidation("If-Match", "\"deadbeefdeadbeefdeadbeefdeadbeef\"");
        var response = await client.SendAsync(put);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact, Priority(2)]
    public async Task Put_menu_with_valid_if_match_returns_200_and_new_etag()
    {
        var (client, _, _) = await NewAuthenticatedClient();
        var hostId = (await CreateHostAsync(client, "Fresh Co")).Body!.Id;
        var menuId = await CreateMenuAsync(client, hostId);

        var get = await client.GetAsync(MenuUrl(hostId, menuId));
        var originalEtag = get.Headers.ETag!.Tag;

        var put = new HttpRequestMessage(HttpMethod.Put, MenuUrl(hostId, menuId))
        {
            Content = JsonBody(new { name = "Brunch v2", description = "Updated brunch menu" })
        };
        put.Headers.TryAddWithoutValidation("If-Match", originalEtag);
        var response = await client.SendAsync(put);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag!.Tag.Should().NotBe(originalEtag, "the handler bumps the ETag after a successful update");

        var body = await response.Content.ReadAsAsync<MenuBody>();
        body!.Name.Should().Be("Brunch v2");
        body.Description.Should().Be("Updated brunch menu");
    }

    [Fact, Priority(2)]
    public async Task Put_menu_as_different_user_returns_403_forbidden()
    {
        // Owner registers + creates Host + Menu
        var (ownerClient, _, _) = await NewAuthenticatedClient();
        var hostId = (await CreateHostAsync(ownerClient, "Owned by user A")).Body!.Id;
        var menuId = await CreateMenuAsync(ownerClient, hostId);

        var get = await ownerClient.GetAsync(MenuUrl(hostId, menuId));
        var etag = get.Headers.ETag!.Tag;

        // A SECOND user logs in (separate token) and attempts to PUT the same menu.
        var (intruderClient, _, _) = await NewAuthenticatedClient();
        var put = new HttpRequestMessage(HttpMethod.Put, MenuUrl(hostId, menuId))
        {
            Content = JsonBody(new { name = "Pwned", description = "Updated by a different user" })
        };
        put.Headers.TryAddWithoutValidation("If-Match", etag);
        var response = await intruderClient.SendAsync(put);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // ProblemDetails.code is set to "menus.owner" by UpdateMenuCommand.IAuthorizeResource<Host>
        var problem = await response.Content.ReadAsAsync<ProblemDetailsWithCode>();
        problem!.Status.Should().Be(403);
        problem.Code.Should().Be("menus.owner");
    }

    // ---------------- helpers ----------------

    private static string MenuUrl(string hostId, string menuId) =>
        $"hosts/{hostId}/menus/{menuId}?api-version=2022-10-01";

    private async Task<(HttpClient client, string token, string userId)> NewAuthenticatedClient()
    {
        var userId = $"user_{Guid.NewGuid():N}".Substring(0, 16);
        var client = _factory.CreateClient();
        var body = $$"""
            {
              "userId": "{{userId}}",
              "firstName": "First",
              "lastName": "Last",
              "email": "{{userId}}@example.com",
              "password": "SuperStr0ngPa$$word"
            }
            """;
        var response = await client.PostAsync("authentication/register",
            new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadAsAsync<RegisterReply>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token, userId);
    }

    private static async Task<(HttpStatusCode Status, HostBody? Body)> CreateHostAsync(HttpClient client, string displayName)
    {
        var json = JsonSerializer.Serialize(new { displayName });
        var response = await client.PostAsync("hosts?api-version=2022-10-01",
            new StringContent(json, Encoding.UTF8, "application/json"));
        var body = response.IsSuccessStatusCode ? await response.Content.ReadAsAsync<HostBody>() : null;
        return (response.StatusCode, body);
    }

    private static async Task<string> CreateMenuAsync(HttpClient client, string hostId)
    {
        var json = """
            {
              "name": "Brunch",
              "description": "Sunday brunch menu",
              "sections": [
                {
                  "name": "Eggs",
                  "description": "Egg dishes",
                  "items": [ { "name": "Omelette", "description": "Three-egg omelette" } ]
                }
              ]
            }
            """;
        var response = await client.PostAsync($"hosts/{hostId}/menus/create?api-version=2022-10-01",
            new StringContent(json, Encoding.UTF8, "application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var menu = await response.Content.ReadAsAsync<MenuBody>();
        return menu!.Id;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private sealed record RegisterReply(string Token);

    private sealed record HostBody(string Id, string OwnerId, string DisplayName);

    private sealed record MenuBody(string Id, string Name, string Description);

    private sealed record ProblemDetailsWithCode(int Status, string? Code, string? Kind);
}
