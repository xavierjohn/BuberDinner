namespace BuberDinner.Application.Services.Authentication.Commands;

using System.Threading.Tasks;
using BuberDinner.Application.Common.Interfaces.Authentication;
using BuberDinner.Application.Common.Interfaces.Persistence;
using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Domain.Entities;
using BuberDinner.Domain.Errors;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Errors;
using MediatR;


public class RegisterCommandHandler :
    IRequestHandler<RegisterCommand, Result<AuthenticationResult, ErrorList>>
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUserRepository _userRepository;

    public RegisterCommandHandler(IJwtTokenGenerator jwtTokenGenerator, IUserRepository userRepository)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _userRepository = userRepository;
    }

    public Task<Result<AuthenticationResult, ErrorList>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return ValidateUserDoesNotExist(request.Email)
            .Bind(email => CreateUser(request))
            .Bind(user =>
            {
                var token = _jwtTokenGenerator.GenerateToken(user);
                return Result.Success<AuthenticationResult, ErrorList>(new AuthenticationResult(user, token));
            });
    }

    private Result<User, ErrorList> CreateUser(RegisterCommand command) =>
        User.Create(command.FirstName, command.LastName, command.Email, command.Password)
            .Tap(user => _userRepository.Add(user));

    private async Task<Result<string, ErrorList>> ValidateUserDoesNotExist(string email)
    {
        var maybeUser = await _userRepository.GetUserByEmail(email);
        if (maybeUser.HasValue)
            return Result.Failure<string, ErrorList>(new ErrorList { Errors.User.AlreadyExists(email) });
        return Result.Success<string, ErrorList>(email);
    }

}
