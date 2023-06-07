namespace BuberDinner._2022_12_21.Controllers;

using Asp.Versioning;
using BuberDinner.Api._2022_12_21.Models.Menus;
using MapsterMapper;
using Mediator;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// CRUD for menu.
/// </summary>
[ApiVersion("2022-10-01")]
[Route("hosts/{hostId}/menus")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status200OK)]
public class MenusController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    /// <summary>
    /// Creates an instance of the <see cref="MenusController" /> class
    /// </summary>
    /// <param name="sender">The <see cref="ISender"/> instance</param>
    /// <param name="mapper">The <see cref="IMapper"/> instance</param>
    public MenusController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    /// <summary>
    /// Create a new menu
    /// </summary>
    /// <param name="request">The <see cref="CreateMenuRequest"/></param>
    /// <param name="hostId">The id of the host creating the menu</param>
    /// <returns>A <see cref="CreateMenuResponse"/> result containing the newly created menu</returns>
    [HttpPost]
    public async ValueTask<ActionResult<CreateMenuResponse>> CreateMenu(CreateMenuRequest request, string hostId) =>
         await request.ToCreateMenuCommand(hostId)
        .BindAsync(command => _sender.Send(command))
        .MapAsync(_mapper.Map<CreateMenuResponse>)
        .ToOkActionResultAsync(this);

}
