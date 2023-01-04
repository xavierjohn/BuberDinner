namespace BuberDinner.Api.Netural.Controllers;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

/// <summary>
/// Unhandled error controller.
/// </summary>
[ApiVersionNeutral]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("[controller]")]
public class ErrorController : ControllerBase
{
    /// <summary>
    /// Show error
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("/error")]
    public IActionResult Error()
    {
        return Problem();
    }
}

