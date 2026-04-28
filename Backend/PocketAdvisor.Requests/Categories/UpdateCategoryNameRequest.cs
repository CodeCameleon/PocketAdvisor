namespace PocketAdvisor.Requests.Categories;

/// <summary>
/// The request model for updating the name of an existing category.
/// </summary>
public sealed class UpdateCategoryNameRequest
{
    /// <summary>
    /// The new name of the category.
    /// </summary>
    public string? Name { get; set; }
}
