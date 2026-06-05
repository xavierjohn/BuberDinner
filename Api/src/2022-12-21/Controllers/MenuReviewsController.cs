namespace BuberDinner._2022_12_21.Controllers;

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using BuberDinner.Api._2022_12_21.Models.MenuReviews;
using BuberDinner.Application.MenuReviews.Commands;
using BuberDinner.Application.MenuReviews.Queries;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.MenuReview.Entities;
using BuberDinner.Domain.MenuReview.ValueObject;
using BuberDinner.Domain.User.ValueObjects;
using Mapster;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Trellis;
using Trellis.Asp;

/// <summary>Menu review endpoints.</summary>
[ApiVersion("2022-10-01")]
[Route("menu-reviews")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class MenuReviewsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initialises a new <see cref="MenuReviewsController"/>.</summary>
    public MenuReviewsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Submit a review for a menu. Validated by FluentValidation at the Mediator pipeline.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<MenuReviewResponse>> SubmitReview(
        [FromBody] SubmitMenuReviewRequest request,
        CancellationToken cancellationToken)
    {
        var guestIdResult = ResolveCallerGuestId();
        if (guestIdResult.IsFailure)
            return Unauthorized();
        var guestId = guestIdResult.GetValueOrThrow("guestId");

        return await request.ToSubmitMenuReviewCommand(guestId)
            .BindAsync(command => _sender.Send(command, cancellationToken))
            .ToHttpResponseAsync(
                body: review => review.Adapt<MenuReviewResponse>(),
                configure: opts => opts
                    .Created(review => $"/menu-reviews/{review.Id.Value}")
                    .WithETag(review => EntityTagValue.Strong(review.ETag)))
            .AsActionResultAsync<MenuReviewResponse>();
    }

    /// <summary>Get a single review.</summary>
    [HttpGet("{reviewId:MenuReviewId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MenuReviewResponse>> GetReview(
        MenuReviewId reviewId, CancellationToken cancellationToken) =>
        await _sender.Send(new GetMenuReviewQuery(reviewId), cancellationToken)
            .ToHttpResponseAsync(
                body: review => review.Adapt<MenuReviewResponse>(),
                configure: opts => opts
                    .WithETag(review => EntityTagValue.Strong(review.ETag))
                    .EvaluatePreconditions())
            .AsActionResultAsync<MenuReviewResponse>();

    /// <summary>Update an existing review. Only the owning guest can update.</summary>
    [HttpPut("{reviewId:MenuReviewId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<MenuReviewResponse>> UpdateReview(
        MenuReviewId reviewId,
        [FromBody] UpdateMenuReviewRequest request,
        CancellationToken cancellationToken)
    {
        var guestIdResult = ResolveCallerGuestId();
        if (guestIdResult.IsFailure)
            return Unauthorized();
        var guestId = guestIdResult.GetValueOrThrow("guestId");

        var command = new UpdateMenuReviewCommand(reviewId, guestId, request.Rating, request.Comment ?? string.Empty);
        return await _sender.Send(command, cancellationToken)
            .ToHttpResponseAsync(
                body: review => review.Adapt<MenuReviewResponse>(),
                configure: opts => opts.WithETag(review => EntityTagValue.Strong(review.ETag)))
            .AsActionResultAsync<MenuReviewResponse>();
    }

    /// <summary>Paginated list of reviews for a menu.</summary>
    [HttpGet("for-menu/{menuId:MenuId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<PagedResponse<MenuReviewResponse>>> ListReviewsForMenu(
        MenuId menuId,
        [FromQuery(Name = "cursor")] string? cursor,
        [FromQuery(Name = "limit")] int? limit,
        CancellationToken cancellationToken)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        var basePath = $"{origin}/menu-reviews/for-menu/{menuId.Value}";
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "2022-10-01";

        return await _sender.Send(
                new ListReviewsForMenuQuery(
                    menuId,
                    cursor is { Length: > 0 } token ? new Cursor(token) : (Cursor?)null,
                    limit),
                cancellationToken)
            .ToHttpResponseAsync(
                nextUrlBuilder: (nextCursor, appliedLimit) =>
                    $"{basePath}?cursor={Uri.EscapeDataString(nextCursor.Token)}&limit={appliedLimit}&api-version={apiVersion}",
                body: review => review.Adapt<MenuReviewResponse>())
            .AsActionResultAsync<PagedResponse<MenuReviewResponse>>();
    }

    private Result<UserId> ResolveCallerGuestId()
    {
        var raw = HttpContext.User.FindFirst("sub")?.Value
                  ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(raw))
            return Result.Fail<UserId>(new Error.AuthenticationRequired());
        return UserId.TryCreate(raw);
    }
}
