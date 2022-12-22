namespace BuberDinner.Api.Netural.Controllers;

using BuberDinner.Api.Netural.Models.Authentication;
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
        await request.ToRegisterCommand()
            .BindAsync(command => _sender.Send(command))
            .FinallyAsync(result => MapToActionResult(result));

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResult>> Login(LoginRequest request)
    {
        return await LoginQuery.Create(request.Email, request.Password)
    .BindAsync(command => _sender.Send(command))
    .FinallyAsync(result => MapToActionResult(result));
    }
}
