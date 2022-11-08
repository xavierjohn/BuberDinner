namespace BuberDinner.Application.Common.Interfaces.Authentication;

using System;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string firstName, string lastName);
}
