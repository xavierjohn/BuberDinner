﻿namespace BuberDinner.Application.Services.Authentication.Commands;

using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Authentication;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Domain.Errors;
using BuberDinner.Domain.User.Entities;
using BuberDinner.Domain.User.ValueObjects;
using Mediator;


public class RegisterCommandHandler :
    IRequestHandler<RegisterCommand, Result<AuthenticationResult, Error>>
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRepository<User> _userRepository;

    public RegisterCommandHandler(IJwtTokenGenerator jwtTokenGenerator, IRepository<User> userRepository)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _userRepository = userRepository;
    }

    public ValueTask<Result<AuthenticationResult, Error>> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        ValidateUserDoesNotExist(request.UserId, cancellationToken)
            .BindAsync(email => CreateUser(request, cancellationToken))
            .BindAsync(user =>
            {
                var token = _jwtTokenGenerator.GenerateToken(user);
                return Result.Success(new AuthenticationResult(user, token));
            });

    private async ValueTask<Result<User, Error>> CreateUser(RegisterCommand command, CancellationToken cancellationToken) =>
        await User.New(command.UserId, command.FirstName, command.LastName, command.Email, command.Password)
        .TeeAsync(user => _userRepository.Add(user, cancellationToken));

    private async ValueTask<Result<string, Error>> ValidateUserDoesNotExist(UserId id, CancellationToken cancellationToken)
    {
        var maybeUser = await _userRepository.FindById(id, cancellationToken);
        if (maybeUser.HasValue)
            return Result.Failure<string>(Errors.User.AlreadyExists(id));
        return Result.Success<string>(id);
    }

}
