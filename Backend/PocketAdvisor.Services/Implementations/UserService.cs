using System.Security.Cryptography;
using System.Text;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PocketAdvisor.Entities;
using PocketAdvisor.Enums;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Requests.Users;
using PocketAdvisor.Services.Configurations;
using PocketAdvisor.Services.Extensions;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to users.
/// </summary>
public sealed class UserService
    : BaseService<UserService>, IUserService
{
    #region GeneratedToken
    
    /// <summary>
    /// Represents a generated token with its plain and hash values.
    /// </summary>
    /// <param name="Plain">The plain value of the token to send back.</param>
    /// <param name="Hash">The hash value of the token to store.</param>
    private record GeneratedToken(string Plain, string Hash);
    
    #endregion
    
    #region Constants
    
    /// <summary>
    /// The size of the tokens in bytes.
    /// </summary>
    private const int TokenSize = 32;
    
    #endregion
    
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
    
    #region GenerateToken
    
    /// <summary>
    /// Generates a token using HMACSHA256 with the provided secret.
    /// </summary>
    /// <param name="secret">The secret to use for hashing.</param>
    /// <returns>The created token.</returns>
    private static GeneratedToken GenerateToken(string secret)
    {
        string plain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSize));
        
        using HMACSHA256 hmacSha256 = new(Encoding.UTF8.GetBytes(secret));
        byte[] hashBytes = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(plain));
        
        return new(plain, Convert.ToBase64String(hashBytes));
    }
    
    #endregion
    
    #region CreateUserAsync
    
    /// <inheritdoc />
    public async Task<Result<string>> CreateUserAsync(CreateUserRequest request)
    {
        Logger.LogInformation("Creating new user...");
        
        IValidator<CreateUserRequest> validator = GetValidator<CreateUserRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for CreateUserRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        User user = new()
        {
            Email = request.Email!,
            PasswordHash = string.Empty,
            Role = EUserRole.User
        };
        user.PasswordHash = PasswordHasher.HashPassword(user, request.Password);
        
        await UserRepository.CreateAsync(user);
        
        await TransactionManager.Value.SaveChangesAsync();
        
        GeneratedToken generatedToken = GenerateToken(TokenSecretsOptions.Value.EmailVerification);
        
        Token token = new()
        {
            Hash = generatedToken.Hash,
            ExpiryAt = DateTime.UtcNow.AddHours(1),
            Type = ETokenType.EmailVerification,
            UserId = user.Id
        };
        await TokenRepository.CreateAsync(token);
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        Logger.LogInformation("New user created successfully.");
        return Result.Ok(generatedToken.Plain);
    }
    
    #endregion
}
