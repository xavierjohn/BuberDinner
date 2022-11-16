namespace BuberDinner.Api.Controllers;

using CSharpFunctionalExtensions.Asp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]")]
[ApiController]
[Authorize]
public class ApiControllerBase : CSharpFunctionalBase
{
}
