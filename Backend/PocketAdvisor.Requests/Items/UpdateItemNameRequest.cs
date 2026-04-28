namespace PocketAdvisor.Requests.Items;

/// <summary>
/// The request model for updating the name of an existing item.
/// </summary>
public sealed class UpdateItemNameRequest
{
    /// <summary>
    /// The new name of the item.
    /// </summary>
    public string? Name { get; set; }
}
