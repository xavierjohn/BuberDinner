namespace BuberDinner._2022_12_21.Controllers;

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// CRUD for dinner.
/// </summary>
[ApiVersion("2022-10-01")]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status200OK)]
public class DinnersController : ControllerBase
{
    /// <summary>
    /// Get all the dinners.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult ListDinners()
    {
        return Ok(Array.Empty<string>());
    }
}
