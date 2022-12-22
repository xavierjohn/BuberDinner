namespace BuberDinner.Application.Services.Authentication.Queries
{
    using System.Threading.Tasks;
    using BuberDinner.Application.Services.Authentication.Common;
    using Mediator;
    using System.Threading;
    using BuberDinner.Application.Common.Interfaces.Authentication;
    using BuberDinner.Application.Common.Interfaces.Persistence;
    using BuberDinner.Domain.Errors;
    using FunctionalDDD;

    internal class LoginQueryHandler :
        IRequestHandler<LoginQuery, Result<AuthenticationResult>>
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IUserRepository _userRepository;

        public LoginQueryHandler(IJwtTokenGenerator jwtTokenGenerator, IUserRepository userRepository)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
            _userRepository = userRepository;
        }

        public async ValueTask<Result<AuthenticationResult>> Handle(LoginQuery request, CancellationToken cancellationToken) =>
            await _userRepository.GetUserByEmail(request.Email, cancellationToken)
                .ToResultAsync(Errors.Authentication.InvalidCredentials)
                .EnsureAsync(user => user.Password == request.Password, Errors.Authentication.InvalidCredentials)
                .BindAsync(user =>
                {
                    var token = _jwtTokenGenerator.GenerateToken(user);
                    return Result.Success(new AuthenticationResult(user, token));
                });
    }
}
