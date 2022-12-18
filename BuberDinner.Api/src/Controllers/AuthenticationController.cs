namespace BuberDinner.Api.Controllers;

using Buber.Dinner.Contracts.Authentication;
using BuberDinner.Application.Services.Authentication.Commands;
using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Application.Services.Authentication.Queries;
using FunctionalDDD;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
public class AuthenticationController : ApiControllerBase
{
    private readonly ISender _sender;

    public AuthenticationController(ISender sender) => _sender = sender;

    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResult>> Register(RegisterRequest request) =>
        await RegisterCommand.Create(request.FirstName, request.LastName, request.Email, request.Password)
            .BindAsync(command => _sender.Send(command))
            .FinallyAsync(result => MapToActionResult(result));

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResult>> Login(LoginRequest request)
    {
        var command = new LoginQuery(request.Email, request.Password);
        var result = await _sender.Send(command);
        return MapToActionResult(result);
    }
}
