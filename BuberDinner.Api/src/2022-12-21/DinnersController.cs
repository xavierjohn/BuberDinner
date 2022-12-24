namespace BuberDinner._2022_12_21.Controllers;

using Asp.Versioning;
using BuberDinner.Api;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// CRUD for dinner.
/// </summary>
[ApiVersion("2022-10-01")]
public class DinnersController : ApiControllerBase
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
