namespace BuberDinner.Api;

using FunctionalDDD.Asp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]")]
[ApiController]
[Authorize]
public class ApiControllerBase : FunctionalDDDBase
{
}
