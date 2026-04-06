namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The request model for creating a new user in the system.
/// </summary>
public sealed class CreateUserRequest
{
    /// <summary>
    /// The email address of the user.
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// The password of the user.
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// The confirmation of the password.
    /// </summary>
    public string? ConfirmPassword { get; set; }
}
