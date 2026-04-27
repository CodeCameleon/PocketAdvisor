namespace PocketAdvisor.Requests.Accounts;

/// <summary>
/// The request model for updating the name of an existing account.
/// </summary>
public sealed class UpdateAccountNameRequest
{
    /// <summary>
    /// The new name of the account.
    /// </summary>
    public string? Name { get; set; }
}
