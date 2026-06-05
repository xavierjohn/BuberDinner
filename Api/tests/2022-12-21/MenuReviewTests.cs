namespace BuberDinner.Api.Tests._2022_12_21;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Priority;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class MenuReviewTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ApiVersion = "?api-version=2022-10-01";
    private readonly WebApplicationFactory<Program> _factory;

    public MenuReviewTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact, Priority(6)]
    public async Task SubmitReview_returns_201_with_etag_and_location()
    {
        var (_, _, _, _, menuId, dinnerId) = await SeedHostMenuAndDinnerAsync();
        var guest = await NewGuestClientAsync();

        var response = await PostReviewAsync(guest.client, menuId, dinnerId, rating: 4, comment: "Great brunch!");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/menu-reviews/");
        var body = await response.Content.ReadAsAsync<ReviewBody>();
        body!.Rating.Should().Be(4);
        body.Comment.Should().Be("Great brunch!");
        body.GuestUserId.Should().Be(guest.userId);
    }

    [Fact, Priority(6)]
    public async Task SubmitReview_with_rating_out_of_range_returns_422_via_FluentValidation()
    {
        var (_, _, _, _, menuId, dinnerId) = await SeedHostMenuAndDinnerAsync();
        var guest = await NewGuestClientAsync();

        var response = await PostReviewAsync(guest.client, menuId, dinnerId, rating: 99, comment: "x");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadAsAsync<ProblemWithErrors>();
        problem!.Code.Should().Be("invalid-input");
        problem.Errors.Should().ContainKey("Rating",
            "FluentValidation surfaces field-bound failures through the standard 422 problem-details shape");
        problem.Errors!["Rating"].Should().Contain(e => e.Contains("between 1 and 5"));
    }

    [Fact, Priority(6)]
    public async Task SubmitReview_with_empty_comment_returns_422_via_FluentValidation()
    {
        var (_, _, _, _, menuId, dinnerId) = await SeedHostMenuAndDinnerAsync();
        var guest = await NewGuestClientAsync();

        var response = await PostReviewAsync(guest.client, menuId, dinnerId, rating: 3, comment: "");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadAsAsync<ProblemWithErrors>();
        problem!.Errors.Should().ContainKey("Comment");
    }

    [Fact, Priority(6)]
    public async Task SubmitReview_against_missing_menu_returns_404_per_Recipe_22_fail_loud()
    {
        var (_, _, _, _, _, dinnerId) = await SeedHostMenuAndDinnerAsync();
        var guest = await NewGuestClientAsync();
        var phantomMenuId = Guid.NewGuid().ToString();

        var req = new HttpRequestMessage(HttpMethod.Post, $"menu-reviews{ApiVersion}")
        {
            Content = JsonBody(new { menuId = phantomMenuId, dinnerId, rating = 3, comment = "ok" }),
        };
        var response = await guest.client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact, Priority(6)]
    public async Task UpdateReview_with_invalid_rating_returns_422_via_FluentValidation_before_handler_runs()
    {
        var (_, _, _, _, menuId, dinnerId) = await SeedHostMenuAndDinnerAsync();
        var guest = await NewGuestClientAsync();
        var first = await PostReviewAsync(guest.client, menuId, dinnerId, rating: 3, comment: "okay");
        var id = (await first.Content.ReadAsAsync<ReviewBody>())!.Id;

        var response = await guest.client.PutAsync($"menu-reviews/{id}{ApiVersion}",
            JsonBody(new { rating = 0, comment = "rolled-back" }));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadAsAsync<ProblemWithErrors>();
        problem!.Errors.Should().ContainKey("Rating");
    }

    [Fact, Priority(6)]
    public async Task UpdateReview_as_different_guest_returns_404_NotFound_leak_shield()
    {
        var (_, _, _, _, menuId, dinnerId) = await SeedHostMenuAndDinnerAsync();
        var owner = await NewGuestClientAsync();
        var first = await PostReviewAsync(owner.client, menuId, dinnerId, rating: 4, comment: "ok");
        var id = (await first.Content.ReadAsAsync<ReviewBody>())!.Id;

        var intruder = await NewGuestClientAsync();
        var response = await intruder.client.PutAsync($"menu-reviews/{id}{ApiVersion}",
            JsonBody(new { rating = 1, comment = "vandalism" }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact, Priority(6)]
    public async Task ListReviewsForMenu_paginates()
    {
        var (_, _, _, _, menuId, dinnerId) = await SeedHostMenuAndDinnerAsync();
        var guest = await NewGuestClientAsync();
        for (int i = 0; i < 7; i++)
            (await PostReviewAsync(guest.client, menuId, dinnerId, rating: 5, comment: $"Review {i}"))
                .EnsureSuccessStatusCode();

        var url = $"menu-reviews/for-menu/{menuId}{ApiVersion}&limit=3";
        var p1 = await (await guest.client.GetAsync(url)).Content.ReadAsAsync<PagedEnvelope<ReviewBody>>();
        p1!.Items.Count.Should().Be(3);
        p1.Next.Should().NotBeNull();

        var p2 = await (await guest.client.GetAsync($"{url}&cursor={Uri.EscapeDataString(p1.Next!.Cursor)}"))
            .Content.ReadAsAsync<PagedEnvelope<ReviewBody>>();
        p2!.Items.Count.Should().Be(3);

        var p3 = await (await guest.client.GetAsync($"{url}&cursor={Uri.EscapeDataString(p2.Next!.Cursor)}"))
            .Content.ReadAsAsync<PagedEnvelope<ReviewBody>>();
        p3!.Items.Count.Should().Be(1);
        p3.Next.Should().BeNull();
    }

    // ---------------- helpers ----------------

    private async Task<(HttpClient hostClient, string hostToken, string hostId, string hostUserId, string menuId, string dinnerId)>
        SeedHostMenuAndDinnerAsync()
    {
        var client = _factory.CreateClient();
        var userId = $"h_{Guid.NewGuid():N}".Substring(0, 14);
        var token = await RegisterAsync(client, userId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var hostResp = await client.PostAsync($"hosts{ApiVersion}", JsonBody(new { displayName = "PR5" }));
        var hostId = (await hostResp.Content.ReadAsAsync<IdOnly>())!.Id;

        var menuResp = await client.PostAsync($"hosts/{hostId}/menus/create{ApiVersion}", JsonBody(new
        {
            name = "M",
            description = "d",
            sections = new[] { new { name = "s", description = "d", items = new[] { new { name = "i", description = "d" } } } },
        }));
        var menuId = (await menuResp.Content.ReadAsAsync<IdOnly>())!.Id;

        var dinnerResp = await client.PostAsync($"hosts/{hostId}/dinners{ApiVersion}", JsonBody(new
        {
            name = "Dinner",
            description = "d",
            menuId,
            startDateTime = "2026-07-01T18:00:00Z",
            endDateTime = "2026-07-01T21:00:00Z",
        }));
        var dinnerId = (await dinnerResp.Content.ReadAsAsync<IdOnly>())!.Id;

        return (client, token, hostId, userId, menuId, dinnerId);
    }

    private async Task<(HttpClient client, string userId, string token)> NewGuestClientAsync()
    {
        var client = _factory.CreateClient();
        var userId = $"g_{Guid.NewGuid():N}".Substring(0, 14);
        var token = await RegisterAsync(client, userId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, userId, token);
    }

    private static async Task<string> RegisterAsync(HttpClient client, string userId)
    {
        var body = $$"""
            { "userId":"{{userId}}", "firstName":"F", "lastName":"L",
              "email":"{{userId}}@example.com", "password":"SecretP5$$word" }
            """;
        var resp = await client.PostAsync("authentication/register",
            new StringContent(body, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadAsAsync<TokenReply>())!.Token;
    }

    private static Task<HttpResponseMessage> PostReviewAsync(
        HttpClient client, string menuId, string dinnerId, int rating, string comment) =>
        client.PostAsync($"menu-reviews{ApiVersion}", JsonBody(new { menuId, dinnerId, rating, comment }));

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private sealed record TokenReply(string Token);
    private sealed record IdOnly(string Id);
    private sealed record ReviewBody(string Id, string MenuId, string DinnerId, string GuestUserId, int Rating, string Comment);
    private sealed record ProblemWithErrors(int Status, string? Code, Dictionary<string, string[]>? Errors);
    private sealed record PagedEnvelope<T>(List<T> Items, PageLink? Next);
    private sealed record PageLink(string Cursor, string Href);
}
