namespace BuberDinner.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

public class DinnersController : ApiControllerBase
{
    public IActionResult ListDinners()
    {
        return Ok(Array.Empty<string>());
    }
}
