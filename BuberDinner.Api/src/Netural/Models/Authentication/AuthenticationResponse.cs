namespace BuberDinner.Api.Netural.Models.Authentication;

public record AuthenticationResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string Token
);
