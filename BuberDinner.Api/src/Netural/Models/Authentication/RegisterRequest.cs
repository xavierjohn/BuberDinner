namespace BuberDinner.Api.Netural.Models.Authentication;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string EmailAddress,
    string Password
);
