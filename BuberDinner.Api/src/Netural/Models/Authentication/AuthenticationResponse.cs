namespace BuberDinner.Api.Netural.Models.Authentication;

public class AuthenticationResponse
{
    public string? UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Token { get; set; }
}
