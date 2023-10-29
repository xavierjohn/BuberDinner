namespace BuberDinner._2022_12_21.Controllers;

using Asp.Versioning;
using BuberDinner.Api._2022_12_21.Models.Menus;
using FunctionalDDD.Asp;
using Mapster;
using Mediator;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// CRUD for menu.
/// </summary>
[ApiVersion("2022-10-01")]
[Route("hosts/{hostId}/menus")]
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
    /// Create a new menu
    /// </summary>
    /// <param name="request">The <see cref="CreateMenuRequest"/></param>
    /// <param name="hostId">The id of the host creating the menu</param>
    /// <returns>A <see cref="CreateMenuResponse"/> result containing the newly created menu</returns>
    [HttpPost("create")]
    public async ValueTask<ActionResult<CreateMenuResponse>> CreateMenu(CreateMenuRequest request, string hostId) =>
        await request
            .ToCreateMenuCommand(hostId)
            .BindAsync(command => _sender.Send(command))
            .MapAsync(menu => menu.Adapt<CreateMenuResponse>())
            .ToOkActionResultAsync(this);

    // TODO: Replace ToOkActionResultAsync(this); with the below code once the Get operation is implemented.
    //
    //        .FinallyAsync(
    //             result => (ActionResult<CreateMenuResponse>)CreatedAtAction(
    //                "Get",
    //                new { id = result.Id },
    //                result),
    //             result => result.ToErrorActionResult<CreateMenuResponse>(this));
}
