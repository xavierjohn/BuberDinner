namespace BuberDinner.Api.Tests._2022_12_21;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Dinners.Events;
using BuberDinner.Domain.Dinner.Events;
using Mediator;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Trellis.Mediator;
using Xunit.Priority;

/// <summary>
/// End-to-end coverage for the Dinner aggregate, state machine, and domain-event dispatch.
/// Each test boots a fresh <see cref="WebApplicationFactory{TEntryPoint}"/> with the
/// captured-events handler swapped in so the test can assert "this event was published
/// with this OccurredAt" after the request returns.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class DinnerStateMachineTests
{
    private const string ApiVersionQuery = "?api-version=2022-10-01";

    [Fact, Priority(3)]
    public async Task Schedule_dinner_returns_201_with_etag_and_publishes_DinnerScheduled()
    {
        var capture = new CapturedEvents();
        var (client, hostId, menuId) = await SetupHostAndMenuAsync(capture);

        var response = await SendScheduleAsync(client, hostId, menuId, "Brunch", "Sunday brunch");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith($"/hosts/{hostId}/dinners/");

        var body = await response.Content.ReadAsAsync<DinnerResponseBody>();
        body!.Status.Should().Be("Upcoming");

        capture.Scheduled.Should().ContainSingle().Which.HostId.Value.ToString().Should().Be(hostId);
    }

    [Fact, Priority(3)]
    public async Task Schedule_dinner_with_foreign_menu_returns_404()
    {
        var capture = new CapturedEvents();
        var (clientA, hostA, _) = await SetupHostAndMenuAsync(capture);

        // Register a second host (with its own menu) using the SAME WebApplicationFactory so
        // both Hosts live in the same in-memory store the controller will read.
        var (_, _, foreignMenuId) = await SetupSecondHostInSameFactoryAsync(clientA);

        var response = await SendScheduleAsync(clientA, hostA, foreignMenuId, "x", "y");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsAsync<ProblemDetailsWithCode>();
        body!.Detail.Should().Contain("Menu does not belong to the specified host.");
    }

    [Fact, Priority(3)]
    public async Task Full_lifecycle_Upcoming_to_Ended_publishes_all_three_events()
    {
        var capture = new CapturedEvents();
        var (client, hostId, menuId) = await SetupHostAndMenuAsync(capture);
        var dinnerId = await ScheduleDinnerAsync(client, hostId, menuId);

        var startResponse = await client.PostAsync(TransitionUrl(hostId, dinnerId, "start"), EmptyJsonBody());
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await startResponse.Content.ReadAsAsync<DinnerResponseBody>())!.Status.Should().Be("InProgress");

        var endResponse = await client.PostAsync(TransitionUrl(hostId, dinnerId, "end"), EmptyJsonBody());
        endResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ended = await endResponse.Content.ReadAsAsync<DinnerResponseBody>();
        ended!.Status.Should().Be("Ended");
        ended.StartedAt.Should().NotBeNull();
        ended.EndedAt.Should().NotBeNull();
        ended.EndedAt!.Value.Should().BeAfter(ended.StartedAt!.Value);

        capture.Scheduled.Should().HaveCount(1);
        capture.Started.Should().HaveCount(1);
        capture.Ended.Should().HaveCount(1);
        capture.Cancelled.Should().BeEmpty();
    }

    [Fact, Priority(3)]
    public async Task Start_when_already_InProgress_returns_422_with_state_machine_reason_code()
    {
        var (client, hostId, menuId) = await SetupHostAndMenuAsync(new CapturedEvents());
        var dinnerId = await ScheduleDinnerAsync(client, hostId, menuId);
        (await client.PostAsync(TransitionUrl(hostId, dinnerId, "start"), EmptyJsonBody())).EnsureSuccessStatusCode();

        var response = await client.PostAsync(TransitionUrl(hostId, dinnerId, "start"), EmptyJsonBody());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadAsAsync<ProblemDetailsWithRules>();
        problem!.Rules.Should().NotBeNull().And.Subject!.Should().ContainSingle()
            .Which.Code.Should().Be("state.machine.invalid.transition");
    }

    [Fact, Priority(3)]
    public async Task Cancel_from_Upcoming_returns_200_with_reason_and_publishes_DinnerCancelled()
    {
        var capture = new CapturedEvents();
        var (client, hostId, menuId) = await SetupHostAndMenuAsync(capture);
        var dinnerId = await ScheduleDinnerAsync(client, hostId, menuId);

        var response = await client.PostAsync(
            TransitionUrl(hostId, dinnerId, "cancel"),
            JsonBody(new { reason = "host illness" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsAsync<DinnerResponseBody>();
        body!.Status.Should().Be("Cancelled");
        body.CancellationReason.Should().Be("host illness");
        body.CancelledAt.Should().NotBeNull();
        body.EndedAt.Should().BeNull("Cancelled and Ended are semantically distinct");
        capture.Cancelled.Should().ContainSingle().Which.Reason.Should().Be("host illness");
    }

    [Fact, Priority(3)]
    public async Task Cancel_from_InProgress_returns_422_and_does_not_mutate_state()
    {
        var (client, hostId, menuId) = await SetupHostAndMenuAsync(new CapturedEvents());
        var dinnerId = await ScheduleDinnerAsync(client, hostId, menuId);
        (await client.PostAsync(TransitionUrl(hostId, dinnerId, "start"), EmptyJsonBody())).EnsureSuccessStatusCode();

        var response = await client.PostAsync(
            TransitionUrl(hostId, dinnerId, "cancel"),
            JsonBody(new { reason = "too late" }));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var get = await client.GetAsync(DinnerUrl(hostId, dinnerId));
        var current = await get.Content.ReadAsAsync<DinnerResponseBody>();
        current!.Status.Should().Be("InProgress", "rejected transitions must not mutate state");
        current.CancellationReason.Should().BeNull();
    }

    [Fact, Priority(3)]
    public async Task Cross_host_start_returns_403_forbidden()
    {
        var (ownerClient, hostId, menuId) = await SetupHostAndMenuAsync(new CapturedEvents());
        var dinnerId = await ScheduleDinnerAsync(ownerClient, hostId, menuId);

        // Reuse the same WebApplicationFactory by registering a second user on the same client
        // surface. We need a separate token, so we build a fresh client and register independently.
        var (intruderClient, _, _) = await SetupHostAndMenuAsync(new CapturedEvents());

        var response = await intruderClient.PostAsync(TransitionUrl(hostId, dinnerId, "start"), EmptyJsonBody());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadAsAsync<ProblemDetailsWithCode>();
        problem!.Code.Should().Be("dinners.owner");
    }

    [Fact, Priority(3)]
    public async Task List_dinners_returns_only_host_owned_dinners()
    {
        var (client, hostId, menuId) = await SetupHostAndMenuAsync(new CapturedEvents());
        await ScheduleDinnerAsync(client, hostId, menuId);
        await ScheduleDinnerAsync(client, hostId, menuId);

        var response = await client.GetAsync(DinnersUrl(hostId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // PR 3 wraps the list in a Trellis.Asp.PagedResponse<T> envelope.
        var page = await response.Content.ReadAsAsync<PagedDinnerEnvelope>();
        page!.Items.Length.Should().BeGreaterOrEqualTo(2);
        page.Items.Should().OnlyContain(d => d.HostId == hostId);
    }

    private sealed record PagedDinnerEnvelope(DinnerResponseBody[] Items);

    // ------------------------ helpers ------------------------

    private static string DinnersUrl(string hostId) => $"hosts/{hostId}/dinners{ApiVersionQuery}";
    private static string DinnerUrl(string hostId, string dinnerId) =>
        $"hosts/{hostId}/dinners/{dinnerId}{ApiVersionQuery}";
    private static string TransitionUrl(string hostId, string dinnerId, string action) =>
        $"hosts/{hostId}/dinners/{dinnerId}/{action}{ApiVersionQuery}";

    /// <summary>
    /// Spins up a fresh WebApplicationFactory wired with the supplied <see cref="CapturedEvents"/>
    /// sink (each Dinner event handler appends to the matching list), registers a unique user,
    /// creates a Host owned by them, and a Menu under that Host. Returns an authenticated client
    /// + the new HostId + MenuId.
    /// </summary>
    private async Task<(HttpClient client, string hostId, string menuId)> SetupHostAndMenuAsync(CapturedEvents capture)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.AddSingleton(capture);
                services.AddScoped<IDomainEventHandler<DinnerScheduled>, CapturingScheduledHandler>();
                services.AddScoped<IDomainEventHandler<DinnerStarted>, CapturingStartedHandler>();
                services.AddScoped<IDomainEventHandler<DinnerEnded>, CapturingEndedHandler>();
                services.AddScoped<IDomainEventHandler<DinnerCancelled>, CapturingCancelledHandler>();
            }));

        var client = factory.CreateClient();
        var userId = $"u_{Guid.NewGuid():N}".Substring(0, 16);
        var registerBody = $$"""
            {
              "userId": "{{userId}}",
              "firstName": "F",
              "lastName": "L",
              "email": "{{userId}}@example.com",
              "password": "SecretP2$$word"
            }
            """;
        var registerResponse = await client.PostAsync("authentication/register",
            new StringContent(registerBody, Encoding.UTF8, "application/json"));
        registerResponse.EnsureSuccessStatusCode();
        var token = (await registerResponse.Content.ReadAsAsync<TokenReply>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var hostResponse = await client.PostAsync($"hosts{ApiVersionQuery}",
            JsonBody(new { displayName = "Test Kitchen" }));
        hostResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var hostId = (await hostResponse.Content.ReadAsAsync<IdAndOwner>())!.Id;

        var menuResponse = await client.PostAsync($"hosts/{hostId}/menus/create{ApiVersionQuery}", JsonBody(new
        {
            name = "Brunch Menu",
            description = "Sunday brunch",
            sections = new[]
            {
                new
                {
                    name = "Eggs",
                    description = "Egg dishes",
                    items = new[] { new { name = "Omelette", description = "Three-egg omelette" } }
                }
            }
        }));
        menuResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var menuId = (await menuResponse.Content.ReadAsAsync<IdOnly>())!.Id;

        return (client, hostId, menuId);
    }

    /// <summary>
    /// Registers a second user against the supplied client's WebApplicationFactory (so the same
    /// in-memory User repo is reused) and creates a Host + Menu owned by THAT user. Used by the
    /// "foreign menu" test to manufacture cross-host data without spinning up a separate
    /// WebApplicationFactory (which would have its own in-memory store).
    /// </summary>
    private static async Task<(string token, string hostId, string menuId)> SetupSecondHostInSameFactoryAsync(HttpClient existingClient)
    {
        // Recreate the HTTP client off the original factory by sending a register request — the
        // user goes into the same static in-memory store. We clone DefaultRequestHeaders via a
        // separate "client", but the actual factory backing it is shared globally via static
        // state in *InMemoryRepository.
        var userId = $"u_{Guid.NewGuid():N}".Substring(0, 16);
        var registerBody = $$"""
            {
              "userId": "{{userId}}", "firstName": "F", "lastName": "L",
              "email": "{{userId}}@example.com", "password": "SecretP2$$word"
            }
            """;
        var registerResponse = await existingClient.PostAsync("authentication/register",
            new StringContent(registerBody, Encoding.UTF8, "application/json"));
        registerResponse.EnsureSuccessStatusCode();
        var token = (await registerResponse.Content.ReadAsAsync<TokenReply>())!.Token;

        // Build a sibling request with the new user's token (don't mutate the caller's auth header).
        var hostReq = new HttpRequestMessage(HttpMethod.Post, $"hosts{ApiVersionQuery}")
        {
            Content = JsonBody(new { displayName = "Foreign Kitchen" })
        };
        hostReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var hostResponse = await existingClient.SendAsync(hostReq);
        hostResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var hostId = (await hostResponse.Content.ReadAsAsync<IdAndOwner>())!.Id;

        var menuReq = new HttpRequestMessage(HttpMethod.Post, $"hosts/{hostId}/menus/create{ApiVersionQuery}")
        {
            Content = JsonBody(new
            {
                name = "Foreign Menu",
                description = "Other host's menu",
                sections = new[]
                {
                    new
                    {
                        name = "S",
                        description = "d",
                        items = new[] { new { name = "I", description = "d" } }
                    }
                }
            })
        };
        menuReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var menuResponse = await existingClient.SendAsync(menuReq);
        menuResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var menuId = (await menuResponse.Content.ReadAsAsync<IdOnly>())!.Id;

        return (token, hostId, menuId);
    }

    private static async Task<HttpResponseMessage> SendScheduleAsync(
        HttpClient client, string hostId, string menuId, string name, string description) =>
        await client.PostAsync(DinnersUrl(hostId), JsonBody(new
        {
            name,
            description,
            menuId,
            startDateTime = "2026-07-01T18:00:00Z",
            endDateTime = "2026-07-01T21:00:00Z",
        }));

    private static async Task<string> ScheduleDinnerAsync(HttpClient client, string hostId, string menuId)
    {
        var response = await SendScheduleAsync(client, hostId, menuId, "Brunch", "Sunday brunch");
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsAsync<DinnerResponseBody>();
        return body!.Id;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static StringContent EmptyJsonBody() => new(string.Empty, Encoding.UTF8, "application/json");

    // ----- Captured-events sink: scoped per WebApplicationFactory (each test owns one). -----

    public sealed class CapturedEvents
    {
        public List<DinnerScheduled> Scheduled { get; } = new();
        public List<DinnerStarted> Started { get; } = new();
        public List<DinnerEnded> Ended { get; } = new();
        public List<DinnerCancelled> Cancelled { get; } = new();
    }

    private sealed class CapturingScheduledHandler : IDomainEventHandler<DinnerScheduled>
    {
        private readonly CapturedEvents _events;
        public CapturingScheduledHandler(CapturedEvents events) => _events = events;
        public ValueTask HandleAsync(DinnerScheduled n, CancellationToken ct)
        {
            lock (_events.Scheduled) _events.Scheduled.Add(n);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingStartedHandler : IDomainEventHandler<DinnerStarted>
    {
        private readonly CapturedEvents _events;
        public CapturingStartedHandler(CapturedEvents events) => _events = events;
        public ValueTask HandleAsync(DinnerStarted n, CancellationToken ct)
        {
            lock (_events.Started) _events.Started.Add(n);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingEndedHandler : IDomainEventHandler<DinnerEnded>
    {
        private readonly CapturedEvents _events;
        public CapturingEndedHandler(CapturedEvents events) => _events = events;
        public ValueTask HandleAsync(DinnerEnded n, CancellationToken ct)
        {
            lock (_events.Ended) _events.Ended.Add(n);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingCancelledHandler : IDomainEventHandler<DinnerCancelled>
    {
        private readonly CapturedEvents _events;
        public CapturingCancelledHandler(CapturedEvents events) => _events = events;
        public ValueTask HandleAsync(DinnerCancelled n, CancellationToken ct)
        {
            lock (_events.Cancelled) _events.Cancelled.Add(n);
            return ValueTask.CompletedTask;
        }
    }

    // ----- wire shapes -----

    private sealed record TokenReply(string Token);

    private sealed record IdAndOwner(string Id, string OwnerId, string DisplayName);

    private sealed record IdOnly(string Id);

    private sealed record DinnerResponseBody(
        string Id, string Name, string Description, string HostId, string MenuId,
        string Status, DateTimeOffset StartDateTime, DateTimeOffset EndDateTime,
        DateTimeOffset? StartedAt, DateTimeOffset? EndedAt, DateTimeOffset? CancelledAt,
        string? CancellationReason);

    private sealed record ProblemDetailsWithCode(int Status, string? Code, string? Kind, string? Detail);

    private sealed record ProblemDetailsWithRules(int Status, string? Code, List<RuleEntry>? Rules);

    private sealed record RuleEntry(string Code, string? Detail);
}
