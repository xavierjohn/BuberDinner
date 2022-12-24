namespace BuberDinner.Api.Netural.Controllers;

using BuberDinner.Api.Netural.Models.Authentication;
using MapsterMapper;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
public class AuthenticationController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public AuthenticationController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResponse>> Register(RegisterRequest request) =>
        await request.ToRegisterCommand()
        .BindAsync(command => _sender.Send(command))
        .MapAsync(_mapper.Map<AuthenticationResponse>)
        .FinallyAsync(result => MapToActionResult(result));

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(LoginRequest request) =>
        await request.ToLoginQuery()
        .BindAsync(command => _sender.Send(command))
        .MapAsync(_mapper.Map<AuthenticationResponse>)
        .FinallyAsync(result => MapToActionResult(result));
}
