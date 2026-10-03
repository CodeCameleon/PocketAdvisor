namespace PocketAdvisor.Services.Clients.Interfaces;

/// <summary>
/// Defines the client interface for sending out emails.
/// </summary>
public interface IEmailClient
{
    /// <summary>
    /// Sends an email verification email to the given email address asynchronously.
    /// </summary>
    /// <param name="email">The email address to send the email to.</param>
    /// <param name="token">The plain-text email verification token to embed in the link.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">
    /// If any of the given parameters is empty or consists only of white-space characters.
    /// </exception>
    Task SendEmailVerificationAsync(string email, string token);
    
    /// <summary>
    /// Sends a password reset email to the given email address asynchronously.
    /// </summary>
    /// <param name="email">The email address to send the email to.</param>
    /// <param name="token">The plain-text password reset token to embed in the link.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">
    /// If any of the given parameters is empty or consists only of white-space characters.
    /// </exception>
    Task SendPasswordResetAsync(string email, string token);
}
