namespace BuberDinner.Api.Netural.Models.Authentication;

/// <summary>
/// Authentication Response
/// </summary>
public class AuthenticationResponse
{
    /// <summary>
    /// User Id
    /// </summary>
    public string? UserId { get; set; }


    /// <summary>
    /// First Name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last Name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Token
    /// </summary>
    public string? Token { get; set; }
}
