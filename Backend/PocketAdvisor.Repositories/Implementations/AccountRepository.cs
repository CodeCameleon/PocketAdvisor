using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Implementations;

/// <summary>
/// Represents the repository implementation for performing operations on the <see cref="Account" /> entities.
/// </summary>
public sealed class AccountRepository
    : BaseRepository<Account, AccountRepository>, IAccountRepository
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepository" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    public AccountRepository(ILogger<AccountRepository> logger, PocketAdvisorDbContext context)
        : base(logger, context, c => c.Accounts)
    {
        
    }
    
    #endregion
}
