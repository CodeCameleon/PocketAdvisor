using Microsoft.Extensions.Logging;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to users.
/// </summary>
public sealed class UserService
    : BaseService<UserService>, IUserService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    public UserService(ILogger<UserService> logger, IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
        
    }
    
    #endregion
}
