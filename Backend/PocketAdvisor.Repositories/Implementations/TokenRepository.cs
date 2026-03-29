using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Implementations;

/// <summary>
/// Represents the repository implementation for performing operations on the <see cref="Token" /> entities.
/// </summary>
public sealed class TokenRepository
    : BaseRepository<Token, TokenRepository>, ITokenRepository
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRepository" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    public TokenRepository(ILogger<TokenRepository> logger, PocketAdvisorDbContext context)
        : base(logger, context, c => c.Tokens)
    {
        
    }
    
    #endregion
}
