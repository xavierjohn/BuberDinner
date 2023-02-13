namespace BuberDinner.Api.Netural.Controllers;

using Asp.Versioning;
using BuberDinner.Api.Netural.Models.Authentication;
using MapsterMapper;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Authentication Controller
/// </summary>
[AllowAnonymous]
[ApiVersionNeutral]
[Route("[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="mapper"></param>
    public AuthenticationController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
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
        .MapAsync(_mapper.Map<AuthenticationResponse>)
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
        .MapAsync(_mapper.Map<AuthenticationResponse>)
        .ToOkActionResultAsync(this);
}
