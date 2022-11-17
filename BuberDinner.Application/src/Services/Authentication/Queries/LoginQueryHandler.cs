namespace BuberDinner.Application.Services.Authentication.Queries
{
    using System.Threading.Tasks;
    using BuberDinner.Application.Services.Authentication.Common;
    using CSharpFunctionalExtensions.Errors;
    using CSharpFunctionalExtensions;
    using Mediator;
    using System.Threading;
    using BuberDinner.Application.Common.Interfaces.Authentication;
    using BuberDinner.Application.Common.Interfaces.Persistence;
    using BuberDinner.Domain.Errors;

    internal class LoginQueryHandler :
        IRequestHandler<LoginQuery, Result<AuthenticationResult, ErrorList>>
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IUserRepository _userRepository;

        public LoginQueryHandler(IJwtTokenGenerator jwtTokenGenerator, IUserRepository userRepository)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
            _userRepository = userRepository;
        }

        public async ValueTask<Result<AuthenticationResult, ErrorList>> Handle(LoginQuery request, CancellationToken cancellationToken) =>
            await _userRepository.GetUserByEmail(request.Email, cancellationToken)
                .ToResult(new ErrorList { Errors.User.DoesNotExist(request.Email) })
                .Ensure(user => user.Password == request.Password, new ErrorList { Errors.Authentication.InvalidCredentials })
                .Bind(user =>
                {
                    var token = _jwtTokenGenerator.GenerateToken(user);
                    return Result.Success<AuthenticationResult, ErrorList>(new AuthenticationResult(user, token));
                });
    }
}
