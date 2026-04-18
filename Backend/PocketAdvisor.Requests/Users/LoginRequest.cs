namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The request model for authenticating an existing user in the system.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// The email address of the user.
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// The password of the user.
    /// </summary>
    public string? Password { get; set; }
}
