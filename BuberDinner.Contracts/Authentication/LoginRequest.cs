namespace Buber.Dinner.Contracts.Authentication;

public record LoginRequest(
    string Email,
    string Password
);