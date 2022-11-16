namespace BuberDinner.Api.Controllers;

using Buber.Dinner.Contracts.Authentication;
using BuberDinner.Application.Services.Authentication.Commands;
using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Application.Services.Authentication.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
public class AuthenticationController : ApiControllerBase
{
    private readonly ISender _sender;

    public AuthenticationController(ISender sender) => _sender = sender;

    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResult>> Register(RegisterRequest request)
    {
        var command = new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password);
        var result = await _sender.Send(command);
        return MapToOkObjectResult(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResult>> Login(LoginRequest request)
    {
        var command = new LoginQuery(request.Email, request.Password);
        var result = await _sender.Send(command);
        return MapToOkObjectResult(result);
    }
}
