namespace BuberDinner.Api;

using FunctionalDDD.Asp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// API Base Controller.
/// </summary>
[Route("[controller]")]
[Authorize]
public class ApiControllerBase : FunctionalDDDBase
{
}
