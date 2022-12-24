namespace BuberDinner.Api;

using FunctionalDDD.Asp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]")]
[Authorize]
public class ApiControllerBase : FunctionalDDDBase
{
}