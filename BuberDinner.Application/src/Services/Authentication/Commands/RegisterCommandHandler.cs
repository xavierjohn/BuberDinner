namespace BuberDinner.Application.Services.Authentication.Commands;

using System.Threading.Tasks;
using BuberDinner.Application.Common.Interfaces.Authentication;
using BuberDinner.Application.Common.Interfaces.Persistence;
using BuberDinner.Application.Services.Authentication.Common;
using BuberDinner.Domain.Errors;
using BuberDinner.Domain.Menu.ValueObject;
using BuberDinner.Domain.User.Entities;
using Mediator;


public class RegisterCommandHandler :
    IRequestHandler<RegisterCommand, Result<AuthenticationResult>>
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUserRepository _userRepository;

    public RegisterCommandHandler(IJwtTokenGenerator jwtTokenGenerator, IUserRepository userRepository)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _userRepository = userRepository;
    }

    public ValueTask<Result<AuthenticationResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return ValidateUserDoesNotExist(request.Email, cancellationToken)
            .BindAsync(email => CreateUser(request))
            .BindAsync(user =>
            {
                var token = _jwtTokenGenerator.GenerateToken(user);
                return Result.Success<AuthenticationResult>(new AuthenticationResult(user, token));
            });
    }

    private Result<User> CreateUser(RegisterCommand command) =>
        User.Create(UserId.CreateUnique(), command.FirstName, command.LastName, command.Email, command.Password)
        .Tap(_userRepository.Add);

    private async ValueTask<Result<string>> ValidateUserDoesNotExist(string email, CancellationToken cancellationToken)
    {
        var maybeUser = await _userRepository.GetUserByEmail(email, cancellationToken);
        if (maybeUser.HasValue)
            return Result.Failure<string>(new ErrorList { Errors.User.AlreadyExists(email) });
        return Result.Success<string>(email);
    }

}
