namespace PocketAdvisor.DbContexts.Interfaces;

/// <summary>
/// Defines the interface for seeding initial data into the database.
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Seeds the initial data into the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
