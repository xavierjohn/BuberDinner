namespace BuberDinner.Api.Neutral.Controllers;

using Asp.Versioning;
using BuberDinner.Api.Neutral.Models.Authentication;
using FunctionalDDD.Asp;
using Mapster;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Authentication Controller
/// </summary>
[AllowAnonymous]
[ApiVersionNeutral]
[Consumes("application/json")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status200OK)]
[Route("[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="sender"></param>
    public AuthenticationController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("register")]
    public async ValueTask<ActionResult<AuthenticationResponse>> Register(RegisterRequest request) =>
        await request.ToRegisterCommand()
        .BindAsync(command => _sender.Send(command))
        .MapAsync(authResult => authResult.Adapt<AuthenticationResponse>())
        .ToOkActionResultAsync(this);

    /// <summary>
    /// Login for existing user.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(LoginRequest request) =>
        await request.ToLoginQuery()
        .BindAsync(command => _sender.Send(command))
        .MapAsync(authResult => authResult.Adapt<AuthenticationResponse>())
        .ToOkActionResultAsync(this);
}
