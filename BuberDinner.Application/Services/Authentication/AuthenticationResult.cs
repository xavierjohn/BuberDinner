namespace BuberDinner.Application.Services.Authentication;

using BuberDinner.Domain.Entities;

public record AuthenticationResult
(
    User User,
    string Token
);