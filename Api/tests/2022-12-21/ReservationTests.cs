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
/// End-to-end coverage for the Reservation endpoints — happy path, fail-loud-on-missing-dinner
/// (Cookbook Recipe 22), the leak-shielded ownership check, and the IETF Idempotency-Key
/// replay/mismatch contract (Cookbook Recipe 29).
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class ReservationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ApiVersion = "?api-version=2022-10-01";
    private readonly WebApplicationFactory<Program> _factory;

    public ReservationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact, Priority(5)]
    public async Task CreateReservation_returns_201_with_etag_and_location()
    {
        var (hostClient, _, hostId, _, dinnerId) = await SeedHostAndDinnerAsync();
        var guest = await NewGuestClientAsync();

        var response = await PostReservationAsync(guest.client, dinnerId, guestCount: 2);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/reservations/");
        var body = await response.Content.ReadAsAsync<ReservationBody>();
        body!.Status.Should().Be("Reserved");
        body.GuestCount.Should().Be(2);
        body.DinnerId.Should().Be(dinnerId);
        body.GuestUserId.Should().Be(guest.userId);
    }

    [Fact, Priority(5)]
    public async Task CreateReservation_against_missing_dinner_returns_404_per_Recipe_22_fail_loud()
    {
        var guest = await NewGuestClientAsync();
        var phantomDinnerId = Guid.NewGuid().ToString();

        var response = await PostReservationAsync(guest.client, phantomDinnerId, guestCount: 1);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadAsAsync<ProblemDetailsBody>();
        problem!.Code.Should().Be("not-found");
    }

    [Fact, Priority(5)]
    public async Task CreateReservation_against_InProgress_dinner_returns_422()
    {
        var (hostClient, hostToken, hostId, _, dinnerId) = await SeedHostAndDinnerAsync();
        // Host starts the dinner so it's no longer Upcoming.
        var startResponse = await hostClient.PostAsync(
            $"hosts/{hostId}/dinners/{dinnerId}/start{ApiVersion}", new StringContent("", Encoding.UTF8, "application/json"));
        startResponse.EnsureSuccessStatusCode();

        var guest = await NewGuestClientAsync();
        var response = await PostReservationAsync(guest.client, dinnerId, guestCount: 2);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadAsAsync<ProblemDetailsWithRules>();
        problem!.Rules.Should().NotBeNull().And.Subject!
            .Should().ContainSingle().Which.Code.Should().Be("reservation.dinner-not-upcoming");
    }

    [Fact, Priority(5)]
    public async Task Idempotency_replay_returns_cached_201_with_Idempotent_Replayed_header_and_no_double_booking()
    {
        var (_, _, _, _, dinnerId) = await SeedHostAndDinnerAsync();
        var guest = await NewGuestClientAsync();
        var key = Guid.NewGuid().ToString();

        var first = await PostReservationAsync(guest.client, dinnerId, guestCount: 2, idempotencyKey: key);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await first.Content.ReadAsAsync<ReservationBody>();

        var replay = await PostReservationAsync(guest.client, dinnerId, guestCount: 2, idempotencyKey: key);
        replay.StatusCode.Should().Be(HttpStatusCode.Created, "framework replays the original snapshot, including the status code");
        replay.Headers.TryGetValues("Idempotent-Replayed", out var replayedValues).Should().BeTrue(
            "framework adds the Idempotent-Replayed: true marker on cached responses");
        replayedValues!.Single().Should().Be("true");
        var replayBody = await replay.Content.ReadAsAsync<ReservationBody>();
        replayBody!.Id.Should().Be(firstBody!.Id, "no double-booking — replay returns the original reservation id");

        // Verify only ONE reservation actually exists in the guest's list.
        var mine = await guest.client.GetAsync($"reservations/mine{ApiVersion}");
        var page = await mine.Content.ReadAsAsync<MyReservationsPage>();
        page!.Items.Count(r => r.Id == firstBody.Id).Should().Be(1,
            "Idempotency-Key replay must NOT produce a second reservation row");
    }

    [Fact, Priority(5)]
    public async Task Idempotency_same_key_different_body_returns_422_fingerprint_mismatch()
    {
        var (_, _, _, _, dinnerId) = await SeedHostAndDinnerAsync();
        var guest = await NewGuestClientAsync();
        var key = Guid.NewGuid().ToString();

        var first = await PostReservationAsync(guest.client, dinnerId, guestCount: 2, idempotencyKey: key);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var mutated = await PostReservationAsync(guest.client, dinnerId, guestCount: 5, idempotencyKey: key);

        mutated.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "framework rejects same-key + different-body so the client knows the key was reused with a mutated payload");
    }

    [Fact, Priority(5)]
    public async Task Cross_guest_cancel_returns_404_NotFound_leak_shield()
    {
        var (_, _, _, _, dinnerId) = await SeedHostAndDinnerAsync();
        var owner = await NewGuestClientAsync();
        var first = await PostReservationAsync(owner.client, dinnerId, guestCount: 1);
        var reservationId = (await first.Content.ReadAsAsync<ReservationBody>())!.Id;

        var intruder = await NewGuestClientAsync();
        var response = await intruder.client.PostAsync(
            $"reservations/{reservationId}/cancel{ApiVersion}",
            JsonBody(new { reason = "hack attempt" }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "ownership mismatches return NotFound (not Forbidden) to avoid leaking existence");
    }

    [Fact, Priority(5)]
    public async Task Host_can_list_reservations_for_their_dinner_via_resource_auth()
    {
        var (hostClient, _, hostId, _, dinnerId) = await SeedHostAndDinnerAsync();
        var guest = await NewGuestClientAsync();
        await PostReservationAsync(guest.client, dinnerId, guestCount: 2);

        var response = await hostClient.GetAsync($"hosts/{hostId}/dinners/{dinnerId}/reservations{ApiVersion}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadAsAsync<MyReservationsPage>();
        page!.Items.Should().HaveCount(1).And.OnlyContain(r => r.DinnerId == dinnerId);
    }

    [Fact, Priority(5)]
    public async Task Foreign_host_cannot_list_reservations_for_anothers_dinner_returns_403()
    {
        var (_, _, ownerHostId, _, ownerDinnerId) = await SeedHostAndDinnerAsync();
        var (foreignClient, _, _, _, _) = await SeedHostAndDinnerAsync();

        var response = await foreignClient.GetAsync(
            $"hosts/{ownerHostId}/dinners/{ownerDinnerId}/reservations{ApiVersion}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadAsAsync<ProblemDetailsBody>();
        problem!.Code.Should().Be("reservations.host.owner");
    }

    [Fact, Priority(5)]
    public async Task CreateReservation_without_Idempotency_Key_returns_400_idempotency_key_required()
    {
        var (_, _, _, _, dinnerId) = await SeedHostAndDinnerAsync();
        var guest = await NewGuestClientAsync();

        // Hand-build the request to skip the helper's default-Guid header injection.
        var req = new HttpRequestMessage(HttpMethod.Post, $"reservations{ApiVersion}")
        {
            Content = JsonBody(new { dinnerId, guestCount = 1 }),
        };
        var response = await guest.client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "[Idempotent] endpoints enforce the Idempotency-Key header at the middleware before the handler runs");
        var problem = await response.Content.ReadAsAsync<ProblemDetailsBody>();
        problem!.Code.Should().Be("idempotency.key_required");
    }

    // -------------------- helpers --------------------

    private async Task<(HttpClient hostClient, string hostToken, string hostId, string menuId, string dinnerId)>
        SeedHostAndDinnerAsync()
    {
        var client = _factory.CreateClient();
        var userId = $"h_{Guid.NewGuid():N}".Substring(0, 14);
        var token = await RegisterAsync(client, userId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var hostResp = await client.PostAsync($"hosts{ApiVersion}", JsonBody(new { displayName = "PR4 Host" }));
        var hostId = (await hostResp.Content.ReadAsAsync<IdOnly>())!.Id;

        var menuResp = await client.PostAsync($"hosts/{hostId}/menus/create{ApiVersion}", JsonBody(new
        {
            name = "M",
            description = "d",
            sections = new[] { new { name = "s", description = "d", items = new[] { new { name = "i", description = "d" } } } }
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

        return (client, token, hostId, menuId, dinnerId);
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
              "email":"{{userId}}@example.com", "password":"SecretP4$$word" }
            """;
        var resp = await client.PostAsync("authentication/register",
            new StringContent(body, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadAsAsync<TokenReply>())!.Token;
    }

    private static async Task<HttpResponseMessage> PostReservationAsync(
        HttpClient client, string dinnerId, int guestCount, string? idempotencyKey = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"reservations{ApiVersion}")
        {
            Content = JsonBody(new { dinnerId, guestCount }),
        };
        // The [Idempotent] middleware enforces presence: a missing Idempotency-Key on an
        // opted-in endpoint returns 400 with code 'idempotency.key_required'. Default to a
        // fresh Guid so callers that don't care about replay still hit the success path.
        req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        return await client.SendAsync(req);
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private sealed record TokenReply(string Token);
    private sealed record IdOnly(string Id);
    private sealed record ReservationBody(
        string Id, string DinnerId, string GuestUserId, int GuestCount, string Status,
        DateTimeOffset ReservedAt, DateTimeOffset? CancelledAt, string? CancellationReason);
    private sealed record MyReservationsPage(List<ReservationBody> Items);
    private sealed record ProblemDetailsBody(int Status, string? Code, string? Kind, string? Detail);
    private sealed record ProblemDetailsWithRules(int Status, string? Code, List<RuleEntry>? Rules);
    private sealed record RuleEntry(string Code, string? Detail);
}
