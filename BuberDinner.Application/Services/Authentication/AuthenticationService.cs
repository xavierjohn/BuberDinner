namespace BuberDinner.Application.Services.Authentication;

using BuberDinner.Application.Common.Interfaces.Authentication;
using BuberDinner.Application.Common.Interfaces.Persistence;
using BuberDinner.Domain.Entities;
using System;

public class AuthenticationService : IAuthenticationService
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUserRepository _userRepository;

    public AuthenticationService(IJwtTokenGenerator jwtTokenGenerator, IUserRepository userRepository)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _userRepository = userRepository;
    }

    public AuthenticationResult Login(string email, string password)
    {
        var user = GetUserByEmail(email);
        ValidatePasswordIsCorrect(user, password);
        var token = _jwtTokenGenerator.GenerateToken(user);
        return new AuthenticationResult(user, token);
    }

    public AuthenticationResult Register(string firstName, string lastName, string email, string password)
    {
        ValidateUserDoesNotExist(email);
        User user = CreateUser(firstName, lastName, email, password);

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new AuthenticationResult(user, token);
    }

    private User CreateUser(string firstName, string lastName, string email, string password)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Password = password
        };
        _userRepository.Add(user);
        return user;
    }

    private void ValidateUserDoesNotExist(string email)
    {
        if (_userRepository.GetUserByEmail(email) != null)
            throw new Exception("User already exists.");
    }

    private User GetUserByEmail(string email)
    {
        if (_userRepository.GetUserByEmail(email) is not User user)
            throw new Exception("User does not exist.");

        return user;
    }

    private void ValidatePasswordIsCorrect(User user, string password)
    {
        if (user.Password != password)
            throw new Exception("Invalid password");
    }
}
