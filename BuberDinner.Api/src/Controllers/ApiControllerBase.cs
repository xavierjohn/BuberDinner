namespace BuberDinner.Api.Controllers;

using FunctionalDDD.Asp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]")]
[ApiController]
[Authorize]
public class ApiControllerBase : FunctionalDDDBase
{
}
