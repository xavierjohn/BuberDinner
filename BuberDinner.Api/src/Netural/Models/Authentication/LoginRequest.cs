namespace BuberDinner.Api.Netural.Models.Authentication;

public record LoginRequest(
    string Email,
    string Password
);
