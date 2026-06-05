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

/// <summary>
/// Integration coverage for cursor-based pagination on the per-host list endpoints
/// (<c>GET /hosts/{hostId}/dinners</c> and <c>GET /hosts/{hostId}/menus</c>) per
/// Cookbook Recipe 3.
///
/// Each test creates its own host so the static in-memory store doesn't leak across tests.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class PaginationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ApiVersion = "?api-version=2022-10-01";
    private readonly WebApplicationFactory<Program> _factory;

    public PaginationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact, Priority(4)]
    public async Task ListDinners_paginates_across_pages_then_terminates_with_null_next()
    {
        var (client, hostId, menuId) = await SetupHostAndMenuAsync();
        await ScheduleNDinnersAsync(client, hostId, menuId, count: 12);

        var p1 = await GetPageAsync<DinnerBody>(client, $"hosts/{hostId}/dinners", limit: 5);
        p1.Items.Count.Should().Be(5);
        p1.Next.Should().NotBeNull("the first page must carry a forward cursor when more rows exist");
        p1.Next!.Cursor.Should().NotBeNullOrEmpty();
        p1.Next.Href.Should().Contain("cursor=").And.Contain("limit=5");
        // Framework contract (trellis-api-asp.md:86): next.href must be an absolute URL so
        // out-of-band consumers (queued cursors, scheduled jobs, share-the-cursor flows) can
        // hand it to `new Uri(...)` without a base.
        Uri.TryCreate(p1.Next.Href, UriKind.Absolute, out var nextUri).Should().BeTrue(
            "next.href must be an absolute URL, but got '{0}'", p1.Next.Href);
        nextUri!.Scheme.Should().BeOneOf("http", "https");
        p1.WasCapped.Should().BeFalse();
        p1.RequestedLimit.Should().Be(5);
        p1.AppliedLimit.Should().Be(5);

        var p2 = await GetPageAsync<DinnerBody>(client, $"hosts/{hostId}/dinners", limit: 5, cursor: p1.Next.Cursor);
        p2.Items.Count.Should().Be(5);
        p2.Next.Should().NotBeNull();

        var p3 = await GetPageAsync<DinnerBody>(client, $"hosts/{hostId}/dinners", limit: 5, cursor: p2.Next!.Cursor);
        p3.Items.Count.Should().Be(2, "12 total - 2 prior pages of 5 = 2 remaining");
        p3.Next.Should().BeNull("the last page must NOT advertise a next cursor");

        var allIds = p1.Items.Concat(p2.Items).Concat(p3.Items).Select(d => d.Id).ToList();
        allIds.Should().OnlyHaveUniqueItems("no row may appear on two pages");
        allIds.Should().BeInAscendingOrder("V7 GUIDs sort chronologically; the repo orders by Id.Value");
    }

    [Fact, Priority(4)]
    public async Task ListDinners_returns_RFC8288_Link_header_for_next_page()
    {
        var (client, hostId, menuId) = await SetupHostAndMenuAsync();
        await ScheduleNDinnersAsync(client, hostId, menuId, count: 3);

        var raw = await client.GetAsync($"hosts/{hostId}/dinners{ApiVersion}&limit=2");

        raw.StatusCode.Should().Be(HttpStatusCode.OK);
        raw.Headers.TryGetValues("Link", out var linkValues).Should().BeTrue();
        linkValues!.Should().ContainSingle()
            .Which.Should().Contain("rel=\"next\"").And.Contain("cursor=").And.Contain("limit=2");
    }

    [Fact, Priority(4)]
    public async Task ListDinners_clamps_oversized_limit_and_surfaces_WasCapped()
    {
        var (client, hostId, menuId) = await SetupHostAndMenuAsync();
        await ScheduleNDinnersAsync(client, hostId, menuId, count: 3);

        var page = await GetPageAsync<DinnerBody>(client, $"hosts/{hostId}/dinners", limit: 500);

        page.RequestedLimit.Should().Be(500);
        page.AppliedLimit.Should().Be(100, "PageSize.Max is the server-side ceiling");
        page.WasCapped.Should().BeTrue();
        page.Items.Count.Should().Be(3);
        page.Next.Should().BeNull();
    }

    [Fact, Priority(4)]
    public async Task ListDinners_malformed_cursor_returns_422_with_cursor_malformed_reason_code()
    {
        var (client, hostId, _) = await SetupHostAndMenuAsync();

        var response = await client.GetAsync($"hosts/{hostId}/dinners{ApiVersion}&cursor=NOT-A-VALID-CURSOR-!!!");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadAsAsync<ProblemDetailsWithErrors>();
        problem!.Status.Should().Be(422);
        problem.Code.Should().Be("invalid-input");
        problem.Errors.Should().ContainKey("cursor");
    }

    [Fact, Priority(4)]
    public async Task ListDinners_default_limit_is_PageSize_Default_when_param_omitted()
    {
        var (client, hostId, menuId) = await SetupHostAndMenuAsync();
        await ScheduleNDinnersAsync(client, hostId, menuId, count: 3);

        var page = await GetPageAsync<DinnerBody>(client, $"hosts/{hostId}/dinners");

        page.RequestedLimit.Should().Be(50, "PageSize.Default per Trellis convention");
        page.AppliedLimit.Should().Be(50);
        page.WasCapped.Should().BeFalse();
    }

    [Fact, Priority(4)]
    public async Task ListMenus_paginates_with_same_envelope_as_ListDinners()
    {
        var (client, hostId, _) = await SetupHostAndMenuAsync();
        // SetupHostAndMenuAsync already created 1 menu; add 11 more for a clean 12.
        for (int i = 0; i < 11; i++)
            await CreateMenuAsync(client, hostId);

        var p1 = await GetPageAsync<MenuBody>(client, $"hosts/{hostId}/menus", limit: 5);
        var p2 = await GetPageAsync<MenuBody>(client, $"hosts/{hostId}/menus", limit: 5, cursor: p1.Next!.Cursor);
        var p3 = await GetPageAsync<MenuBody>(client, $"hosts/{hostId}/menus", limit: 5, cursor: p2.Next!.Cursor);

        p1.Items.Count.Should().Be(5);
        p2.Items.Count.Should().Be(5);
        p3.Items.Count.Should().Be(2);
        p3.Next.Should().BeNull();

        var allIds = p1.Items.Concat(p2.Items).Concat(p3.Items).Select(m => m.Id).ToList();
        allIds.Should().OnlyHaveUniqueItems().And.BeInAscendingOrder();
    }

    [Fact, Priority(4)]
    public async Task Pagination_filters_by_host_so_one_hosts_cursor_does_not_leak_anothers_rows()
    {
        var (ownerClient, ownerHost, ownerMenu) = await SetupHostAndMenuAsync();
        await ScheduleNDinnersAsync(ownerClient, ownerHost, ownerMenu, count: 4);

        // Foreign host with its own data, in the SAME static repo.
        var (foreignClient, foreignHost, foreignMenu) = await SetupHostAndMenuAsync();
        await ScheduleNDinnersAsync(foreignClient, foreignHost, foreignMenu, count: 7);

        var ownerPage = await GetPageAsync<DinnerBody>(ownerClient, $"hosts/{ownerHost}/dinners", limit: 50);
        var foreignPage = await GetPageAsync<DinnerBody>(foreignClient, $"hosts/{foreignHost}/dinners", limit: 50);

        ownerPage.Items.Should().HaveCount(4).And.OnlyContain(d => d.HostId == ownerHost);
        foreignPage.Items.Should().HaveCount(7).And.OnlyContain(d => d.HostId == foreignHost);

        // The actual scenario this test's name advertises: take a cursor produced by host A's
        // pagination and submit it against host B's URL. The host filter is applied BEFORE the
        // cursor seek in the repo, so the response must contain ONLY host B's rows — never any
        // row owned by host A — regardless of how the cursor was constructed.
        var ownerPaged = await GetPageAsync<DinnerBody>(ownerClient, $"hosts/{ownerHost}/dinners", limit: 2);
        ownerPaged.Next.Should().NotBeNull("setup needs a non-null cursor to swap onto the foreign host");
        var crossPage = await GetPageAsync<DinnerBody>(
            foreignClient, $"hosts/{foreignHost}/dinners", limit: 2, cursor: ownerPaged.Next!.Cursor);

        crossPage.Items.Should().OnlyContain(d => d.HostId == foreignHost,
            "host filter is applied BEFORE the cursor seek; a stolen cursor cannot leak another host's rows");
        crossPage.Items.Should().NotContain(d => ownerPage.Items.Any(o => o.Id == d.Id),
            "no owner-owned id may appear in the foreign host's page even when the cursor is swapped");
    }

    // ---------------- helpers ----------------

    private async Task<(HttpClient client, string hostId, string menuId)> SetupHostAndMenuAsync()
    {
        var client = _factory.CreateClient();
        var userId = $"page_{Guid.NewGuid():N}".Substring(0, 16);
        var registerBody = $$"""
            { "userId":"{{userId}}", "firstName":"F", "lastName":"L",
              "email":"{{userId}}@example.com", "password":"SecretP3$$word" }
            """;
        var register = await client.PostAsync("authentication/register",
            new StringContent(registerBody, Encoding.UTF8, "application/json"));
        register.EnsureSuccessStatusCode();
        var token = (await register.Content.ReadAsAsync<TokenReply>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var hostResp = await client.PostAsync($"hosts{ApiVersion}", JsonBody(new { displayName = "PageTest" }));
        hostResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var hostId = (await hostResp.Content.ReadAsAsync<IdOnly>())!.Id;

        var menuId = await CreateMenuAsync(client, hostId);
        return (client, hostId, menuId);
    }

    private static async Task<string> CreateMenuAsync(HttpClient client, string hostId)
    {
        var menuResp = await client.PostAsync($"hosts/{hostId}/menus/create{ApiVersion}", JsonBody(new
        {
            name = $"M{Guid.NewGuid():N}".Substring(0, 8),
            description = "d",
            sections = new[]
            {
                new { name = "s", description = "d", items = new[] { new { name = "i", description = "d" } } }
            }
        }));
        menuResp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await menuResp.Content.ReadAsAsync<IdOnly>())!.Id;
    }

    private static async Task ScheduleNDinnersAsync(HttpClient client, string hostId, string menuId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var body = new
            {
                name = $"D{i}",
                description = "d",
                menuId,
                startDateTime = "2026-07-01T18:00:00Z",
                endDateTime = "2026-07-01T21:00:00Z",
            };
            var resp = await client.PostAsync($"hosts/{hostId}/dinners{ApiVersion}", JsonBody(body));
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    private static async Task<PageEnvelope<T>> GetPageAsync<T>(
        HttpClient client, string path, int? limit = null, string? cursor = null)
    {
        var url = $"{path}{ApiVersion}";
        if (limit is { } l) url += $"&limit={l}";
        if (cursor is { Length: > 0 }) url += $"&cursor={Uri.EscapeDataString(cursor)}";
        var resp = await client.GetAsync(url);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await resp.Content.ReadAsAsync<PageEnvelope<T>>();
        return page!;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    // ----- wire shapes -----

    private sealed record TokenReply(string Token);
    private sealed record IdOnly(string Id);
    private sealed record DinnerBody(string Id, string Name, string HostId);
    private sealed record MenuBody(string Id);

    private sealed record PageEnvelope<T>(
        List<T> Items,
        PageLink? Next,
        PageLink? Previous,
        int RequestedLimit,
        int AppliedLimit,
        int DeliveredCount,
        bool WasCapped);

    private sealed record PageLink(string Cursor, string Href);

    private sealed record ProblemDetailsWithErrors(int Status, string? Code, Dictionary<string, string[]>? Errors);
}
