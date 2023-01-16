namespace BuberDinner.Application.Services.Authentication.Common;

using BuberDinner.Domain.User.Entities;

public record AuthenticationResult
(
    User User,
    string Token
);