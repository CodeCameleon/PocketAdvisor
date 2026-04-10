using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Services.Configurations;
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
    /// <param name="tokenSecretsOptions">
    /// The token secrets options for accessing the token secrets configuration values.
    /// </param>
    /// <param name="passwordHasher">The password hasher for hashing user passwords.</param>
    /// <param name="tokenRepository">The token repository instance.</param>
    /// <param name="userRepository">The user repository instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public UserService(ILogger<UserService> logger, IServiceProvider serviceProvider,
        IOptions<TokenSecretsOptions> tokenSecretsOptions, IPasswordHasher<User> passwordHasher,
        ITokenRepository tokenRepository, IUserRepository userRepository)
        : base(logger, serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(tokenSecretsOptions);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(tokenRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        
        TokenSecretsOptions = tokenSecretsOptions;
        PasswordHasher = passwordHasher;
        TokenRepository = tokenRepository;
        UserRepository = userRepository;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The token secrets options for accessing the token secrets configuration values.
    /// </summary>
    private IOptions<TokenSecretsOptions> TokenSecretsOptions { get; }
    
    /// <summary>
    /// The password hasher for hashing user passwords.
    /// </summary>
    private IPasswordHasher<User> PasswordHasher { get; }
    
    /// <summary>
    /// The token repository instance.
    /// </summary>
    private ITokenRepository TokenRepository { get; }
    
    /// <summary>
    /// The user repository instance.
    /// </summary>
    private IUserRepository UserRepository { get; }
    
    #endregion
}
