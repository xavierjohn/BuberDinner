namespace BuberDinner._2022_12_21.Controllers;

using Asp.Versioning;
using BuberDinner.Api._2022_12_21.Models.Menus;
using BuberDinner.Application.Menus.Queries;
using BuberDinner.Domain.Host.ValueObject;
using BuberDinner.Domain.Menu;
using BuberDinner.Domain.Menu.ValueObject;
using Mapster;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Trellis;
using Trellis.Asp;
/// <summary>
/// CRUD for menu.
/// </summary>
[ApiVersion("2022-10-01")]
[Route("hosts/{hostId:HostId}/menus")]
[Consumes("application/json")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status201Created)]
public class MenusController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Creates an instance of the <see cref="MenusController" /> class
    /// </summary>
    /// <param name="sender">The <see cref="ISender"/> instance</param>
    public MenusController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lists menus owned by the route host with cursor-based pagination (Cookbook Recipe 3).
    /// Mirrors the shape of <see cref="DinnersController.ListDinners"/> exactly — same query
    /// params (<c>?cursor</c> / <c>?limit</c>), same <c>PagedResponse&lt;T&gt;</c> envelope,
    /// same RFC 8288 <c>Link</c> header.
    /// </summary>
    /// <param name="hostId">The id of the host whose menus are being listed.</param>
    /// <param name="cursor">Opaque continuation token from the previous page's <c>next</c>. Null on the first page.</param>
    /// <param name="limit">Requested page size (clamped to <c>PageSize.Max</c>); falls back to <c>PageSize.Default</c> when null/non-positive.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<PagedResponse<MenuResponse>>> ListMenus(
        HostId hostId,
        [FromQuery(Name = "cursor")] string? cursor,
        [FromQuery(Name = "limit")] int? limit,
        CancellationToken cancellationToken)
    {
        // Framework contract (trellis-api-asp.md:86): nextUrlBuilder must return an absolute URL.
        var origin = $"{Request.Scheme}://{Request.Host}";
        var basePath = $"{origin}/hosts/{hostId.Value}/menus";
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "2022-10-01";

        return await _sender.Send(
                new ListMenusForHostQuery(
                    hostId,
                    cursor is { Length: > 0 } token ? new Cursor(token) : (Cursor?)null,
                    limit),
                cancellationToken)
            .ToHttpResponseAsync(
                nextUrlBuilder: (nextCursor, appliedLimit) =>
                    $"{basePath}?cursor={Uri.EscapeDataString(nextCursor.Token)}&limit={appliedLimit}&api-version={apiVersion}",
                body: menu => menu.Adapt<MenuResponse>())
            .AsActionResultAsync<PagedResponse<MenuResponse>>();
    }

    /// <summary>
    /// Create a new menu
    /// </summary>
    /// <param name="request">The <see cref="CreateMenuRequest"/></param>
    /// <param name="hostId">The id of the host creating the menu</param>
    /// <returns>A <see cref="CreateMenuResponse"/> result containing the newly created menu</returns>
    [HttpPost("create")]
    public async ValueTask<ActionResult<CreateMenuResponse>> CreateMenu(CreateMenuRequest request, HostId hostId) =>
        await request
            .ToCreateMenuCommand(hostId.Value.ToString())
            .BindAsync(command => _sender.Send(command))
            .ToHttpResponseAsync(
                body: menu => menu.Adapt<CreateMenuResponse>(),
                configure: opts => opts.Created(menu => $"/hosts/{hostId.Value}/menus/{menu.Id}"))
            .AsActionResultAsync<CreateMenuResponse>();

    /// <summary>
    /// Get a menu. Emits a strong ETag so clients can revalidate with If-None-Match (304) or
    /// fail-on-stale with If-Match (412). Per Cookbook Recipe 6.
    /// </summary>
    /// <param name="hostId">The id of the host that owns the menu.</param>
    /// <param name="menuId">The id of the menu to retrieve.</param>
    [HttpGet("{menuId:MenuId}")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async ValueTask<ActionResult<MenuResponse>> GetMenu(HostId hostId, MenuId menuId) =>
        await _sender.Send(new GetMenuQuery(hostId, menuId))
            .ToHttpResponseAsync(
                body: menu => menu.Adapt<MenuResponse>(),
                configure: opts => opts
                    .WithETag(menu => EntityTagValue.Strong(menu.ETag))
                    .EvaluatePreconditions())
            .AsActionResultAsync<MenuResponse>();

    /// <summary>
    /// Update a menu with full optimistic-concurrency protection. Requires an
    /// <c>If-Match</c> header carrying the current ETag (RFC 9110 §13.1.1 + RFC 6585):
    /// missing → 428 Precondition Required; stale → 412 Precondition Failed.
    /// Resource-based authorization gates by Host ownership (Cookbook Recipes 7 + 23).
    /// </summary>
    [HttpPut("{menuId:MenuId}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async ValueTask<ActionResult<MenuResponse>> UpdateMenu(
        HostId hostId,
        MenuId menuId,
        [FromBody] UpdateMenuRequest request,
        CancellationToken cancellationToken) =>
        await request.ToUpdateMenuCommand(hostId, menuId, ETagHelper.ParseIfMatch(HttpContext.Request))
            .BindAsync(command => _sender.Send(command, cancellationToken))
            .ToHttpResponseAsync(
                body: menu => menu.Adapt<MenuResponse>(),
                configure: opts => opts.WithETag(menu => EntityTagValue.Strong(menu.ETag)))
            .AsActionResultAsync<MenuResponse>();
}
