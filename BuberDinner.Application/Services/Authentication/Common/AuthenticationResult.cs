namespace BuberDinner.Application.Services.Authentication.Common;

using BuberDinner.Domain.Entities;

public record AuthenticationResult
(
    User User,
    string Token
);