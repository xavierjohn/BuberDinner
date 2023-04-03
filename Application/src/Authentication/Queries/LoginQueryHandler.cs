namespace BuberDinner.Application.Services.Authentication.Queries
{
    using System.Threading.Tasks;
    using BuberDinner.Application.Services.Authentication.Common;
    using Mediator;
    using System.Threading;
    using BuberDinner.Domain.Errors;
    using BuberDinner.Application.Abstractions.Authentication;
    using BuberDinner.Application.Abstractions.Persistence;
    using BuberDinner.Domain.User.Entities;

    internal class LoginQueryHandler :
        IRequestHandler<LoginQuery, Result<AuthenticationResult>>
    {
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IRepository<User> _userRepository;

        public LoginQueryHandler(IJwtTokenGenerator jwtTokenGenerator, IRepository<User> userRepository)
        {
            _jwtTokenGenerator = jwtTokenGenerator;
            _userRepository = userRepository;
        }

        public async ValueTask<Result<AuthenticationResult>> Handle(LoginQuery request, CancellationToken cancellationToken) =>
            await _userRepository.FindById(request.UserId, cancellationToken)
                .ToResultAsync(Errors.Authentication.InvalidCredentials)
                .EnsureAsync(user => user.Password == request.Password, Errors.Authentication.InvalidCredentials)
                .BindAsync(user =>
                {
                    var token = _jwtTokenGenerator.GenerateToken(user);
                    return Result.Success(new AuthenticationResult(user, token));
                });
    }
}
