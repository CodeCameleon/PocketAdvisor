using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Implementations;

/// <summary>
/// Represents the repository implementation for performing operations on the <see cref="User" /> entities.
/// </summary>
public sealed class UserRepository
    : BaseRepository<User, UserRepository>, IUserRepository
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    public UserRepository(ILogger<UserRepository> logger, PocketAdvisorDbContext context)
        : base(logger, context, c => c.Users)
    {
        
    }
    
    #endregion
}
