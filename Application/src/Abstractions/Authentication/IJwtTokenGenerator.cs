namespace BuberDinner.Application.Abstractions.Authentication;

using BuberDinner.Domain.User.Entities;
using System;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
