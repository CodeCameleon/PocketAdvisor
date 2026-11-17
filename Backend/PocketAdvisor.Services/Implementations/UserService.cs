using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PocketAdvisor.DbContexts.Interfaces;
using PocketAdvisor.Entities;
using PocketAdvisor.Enums;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Requests.Users;
using PocketAdvisor.Responses.Users;
using PocketAdvisor.Services.Clients.Interfaces;
using PocketAdvisor.Services.Configurations;
using PocketAdvisor.Services.Extensions;
using PocketAdvisor.Services.Interfaces;
using PocketAdvisor.Services.Resources;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to users.
/// </summary>
public sealed partial class UserService
    : BaseDatabaseService<UserService>, IUserService
{
    #region GeneratedToken

    /// <summary>
    /// Represents a generated token with its plain and hash values.
    /// </summary>
    /// <param name="Plain">The plain value of the token to send back.</param>
    /// <param name="Hash">The hash value of the token to store.</param>
    private sealed record GeneratedToken(string Plain, string Hash);

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
    /// <param name="transactionManager">The transaction manager of the database.</param>
    /// <param name="jsonWebTokenOptions">
    /// The JSON web token options for accessing the JSON web token configuration values.
    /// </param>
    /// <param name="tokenExpirationsOptions">
    /// The token expirations options for accessing the token expirations configuration values.
    /// </param>
    /// <param name="tokenSecretsOptions">
    /// The token secrets options for accessing the token secrets configuration values.
    /// </param>
    /// <param name="passwordHasher">The password hasher for hashing user passwords.</param>
    /// <param name="emailClient">The email client for sending out emails.</param>
    /// <param name="tokenRepository">The token repository instance.</param>
    /// <param name="userRepository">The user repository instance.</param>
    /// <param name="createUserRequestValidator">
    /// The validator for the <see cref="CreateUserRequest" /> model.
    /// </param>
    /// <param name="forgotPasswordRequestValidator">
    /// The validator for the <see cref="ForgotPasswordRequest" /> model.
    /// </param>
    /// <param name="loginRequestValidator">The validator for the <see cref="LoginRequest" /> model.</param>
    /// <param name="refreshRequestValidator">The validator for the <see cref="RefreshRequest" /> model.</param>
    /// <param name="resetPasswordRequestValidator">
    /// The validator for the <see cref="ResetPasswordRequest" /> model.
    /// </param>
    /// <param name="verifyEmailRequestValidator">
    /// The validator for the <see cref="VerifyEmailRequest" /> model.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public UserService(ILogger<UserService> logger, ITransactionManager transactionManager,
        IOptions<JsonWebTokenOptions> jsonWebTokenOptions, IOptions<TokenExpirationsOptions> tokenExpirationsOptions,
        IOptions<TokenSecretsOptions> tokenSecretsOptions, IPasswordHasher<User> passwordHasher,
        IEmailClient emailClient, ITokenRepository tokenRepository, IUserRepository userRepository,
        IValidator<CreateUserRequest> createUserRequestValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordRequestValidator,
        IValidator<LoginRequest> loginRequestValidator, IValidator<RefreshRequest> refreshRequestValidator,
        IValidator<ResetPasswordRequest> resetPasswordRequestValidator,
        IValidator<VerifyEmailRequest> verifyEmailRequestValidator)
        : base(logger, transactionManager)
    {
        ArgumentNullException.ThrowIfNull(jsonWebTokenOptions);
        ArgumentNullException.ThrowIfNull(tokenExpirationsOptions);
        ArgumentNullException.ThrowIfNull(tokenSecretsOptions);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(emailClient);
        ArgumentNullException.ThrowIfNull(tokenRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(createUserRequestValidator);
        ArgumentNullException.ThrowIfNull(forgotPasswordRequestValidator);
        ArgumentNullException.ThrowIfNull(loginRequestValidator);
        ArgumentNullException.ThrowIfNull(refreshRequestValidator);
        ArgumentNullException.ThrowIfNull(resetPasswordRequestValidator);
        ArgumentNullException.ThrowIfNull(verifyEmailRequestValidator);

        JsonWebTokenOptions = jsonWebTokenOptions;
        TokenExpirationsOptions = tokenExpirationsOptions;
        TokenSecretsOptions = tokenSecretsOptions;
        PasswordHasher = passwordHasher;
        EmailClient = emailClient;
        TokenRepository = tokenRepository;
        UserRepository = userRepository;
        CreateUserRequestValidator = createUserRequestValidator;
        ForgotPasswordRequestValidator = forgotPasswordRequestValidator;
        LoginRequestValidator = loginRequestValidator;
        RefreshRequestValidator = refreshRequestValidator;
        ResetPasswordRequestValidator = resetPasswordRequestValidator;
        VerifyEmailRequestValidator = verifyEmailRequestValidator;
    }

    #endregion

    #region Properties

    /// <summary>
    /// The JSON web token options for accessing the JSON web token configuration values.
    /// </summary>
    private IOptions<JsonWebTokenOptions> JsonWebTokenOptions { get; }

    /// <summary>
    /// The token expirations options for accessing the token expirations configuration values.
    /// </summary>
    private IOptions<TokenExpirationsOptions> TokenExpirationsOptions { get; }

    /// <summary>
    /// The token secrets options for accessing the token secrets configuration values.
    /// </summary>
    private IOptions<TokenSecretsOptions> TokenSecretsOptions { get; }

    /// <summary>
    /// The password hasher for hashing user passwords.
    /// </summary>
    private IPasswordHasher<User> PasswordHasher { get; }

    /// <summary>
    /// The email client for sending out emails.
    /// </summary>
    private IEmailClient EmailClient { get; }

    /// <summary>
    /// The token repository instance.
    /// </summary>
    private ITokenRepository TokenRepository { get; }

    /// <summary>
    /// The user repository instance.
    /// </summary>
    private IUserRepository UserRepository { get; }

    /// <summary>
    /// The validator for the <see cref="CreateUserRequest" /> model.
    /// </summary>
    private IValidator<CreateUserRequest> CreateUserRequestValidator { get; }

    /// <summary>
    /// The validator for the <see cref="ForgotPasswordRequest" /> model.
    /// </summary>
    private IValidator<ForgotPasswordRequest> ForgotPasswordRequestValidator { get; }

    /// <summary>
    /// The validator for the <see cref="LoginRequest" /> model.
    /// </summary>
    private IValidator<LoginRequest> LoginRequestValidator { get; }

    /// <summary>
    /// The validator for the <see cref="RefreshRequest" /> model.
    /// </summary>
    private IValidator<RefreshRequest> RefreshRequestValidator { get; }

    /// <summary>
    /// The validator for the <see cref="ResetPasswordRequest" /> model.
    /// </summary>
    private IValidator<ResetPasswordRequest> ResetPasswordRequestValidator { get; }

    /// <summary>
    /// The validator for the <see cref="VerifyEmailRequest" /> model.
    /// </summary>
    private IValidator<VerifyEmailRequest> VerifyEmailRequestValidator { get; }

    #endregion

    #region LogMessages

    /// <summary>
    /// Logs that a new user is being created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Creating new user...")]
    private static partial void LogCreatingUser(ILogger logger);

    /// <summary>
    /// Logs that the create user request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Validation failed for CreateUserRequest: {Errors}")]
    private static partial void LogCreateUserRequestInvalid(ILogger logger,
        IEnumerable<FluentValidation.Results.ValidationFailure> errors);

    /// <summary>
    /// Logs that the new user was created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "New user created successfully.")]
    private static partial void LogUserCreated(ILogger logger);

    /// <summary>
    /// Logs that a forgot password request is being processed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Processing forgot password request...")]
    private static partial void LogProcessingForgotPassword(ILogger logger);

    /// <summary>
    /// Logs that the forgot password request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
        Message = "Validation failed for ForgotPasswordRequest: {Errors}")]
    private static partial void LogForgotPasswordRequestInvalid(ILogger logger,
        IEnumerable<FluentValidation.Results.ValidationFailure> errors);

    /// <summary>
    /// Logs that the password reset token was generated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Password reset token generated successfully.")]
    private static partial void LogPasswordResetTokenGenerated(ILogger logger);

    /// <summary>
    /// Logs that a user is being authenticated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Authenticating user...")]
    private static partial void LogAuthenticatingUser(ILogger logger);

    /// <summary>
    /// Logs that the login request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Validation failed for LoginRequest: {Errors}")]
    private static partial void LogLoginRequestInvalid(ILogger logger,
        IEnumerable<FluentValidation.Results.ValidationFailure> errors);

    /// <summary>
    /// Logs that the password hash of the user was upgraded.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 9, Level = LogLevel.Information,
        Message = "Password hash for user '{UserId}' was upgraded successfully.")]
    private static partial void LogPasswordHashUpgraded(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that the user was authenticated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "User authenticated successfully.")]
    private static partial void LogUserAuthenticated(ILogger logger);

    /// <summary>
    /// Logs that a session is being ended.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Ending session...")]
    private static partial void LogEndingSession(ILogger logger);

    /// <summary>
    /// Logs that no valid refresh token was presented, so there is no session to end.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 12, Level = LogLevel.Information,
        Message = "No valid refresh token was presented, there is no session to end.")]
    private static partial void LogNoRefreshTokenPresented(ILogger logger);

    /// <summary>
    /// Logs that the refresh token was not found, so there is no session to end.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 13, Level = LogLevel.Information,
        Message = "The refresh token was not found, there is no session to end.")]
    private static partial void LogRefreshTokenNotFound(ILogger logger);

    /// <summary>
    /// Logs that the session was ended.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "Session ended successfully.")]
    private static partial void LogSessionEnded(ILogger logger);

    /// <summary>
    /// Logs that a session is being refreshed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "Refreshing session...")]
    private static partial void LogRefreshingSession(ILogger logger);

    /// <summary>
    /// Logs that the refresh request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 16, Level = LogLevel.Warning, Message = "Validation failed for RefreshRequest: {Errors}")]
    private static partial void LogRefreshRequestInvalid(ILogger logger,
        IEnumerable<FluentValidation.Results.ValidationFailure> errors);

    /// <summary>
    /// Logs that the session was refreshed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "Session refreshed successfully.")]
    private static partial void LogSessionRefreshed(ILogger logger);

    /// <summary>
    /// Logs that the password of a user is being reset.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "Resetting user password...")]
    private static partial void LogResettingPassword(ILogger logger);

    /// <summary>
    /// Logs that the reset password request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 19, Level = LogLevel.Warning,
        Message = "Validation failed for ResetPasswordRequest: {Errors}")]
    private static partial void LogResetPasswordRequestInvalid(ILogger logger,
        IEnumerable<FluentValidation.Results.ValidationFailure> errors);

    /// <summary>
    /// Logs that the password of the user was reset.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "User password reset successfully.")]
    private static partial void LogPasswordReset(ILogger logger);

    /// <summary>
    /// Logs that an email address is being verified.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "Verifying email address...")]
    private static partial void LogVerifyingEmail(ILogger logger);

    /// <summary>
    /// Logs that the verify email request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 22, Level = LogLevel.Warning,
        Message = "Validation failed for VerifyEmailRequest: {Errors}")]
    private static partial void LogVerifyEmailRequestInvalid(ILogger logger,
        IEnumerable<FluentValidation.Results.ValidationFailure> errors);

    /// <summary>
    /// Logs that the email address was verified.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 23, Level = LogLevel.Information, Message = "Email address verified successfully.")]
    private static partial void LogEmailVerified(ILogger logger);

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
    public async Task<Result> CreateUserAsync(CreateUserRequest request)
    {
        LogCreatingUser(Logger);

        ValidationResult validationResult = await CreateUserRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogCreateUserRequestInvalid(Logger, validationResult.Errors);

            return Result.Fail(validationResult.Errors.ToErrorList());
        }

        await TransactionManager.BeginTransactionAsync();

        string normalizedEmail = request.Email!.Trim().ToLowerInvariant();
        bool emailExists = await UserRepository.ExistsAsync(u => u.Email == normalizedEmail);

        if (emailExists)
        {
            return Result.Fail(
                CreateError(ValidationMessages.EmailAlreadyExists, nameof(request.Email))
            );
        }

        User user = new()
        {
            Email = normalizedEmail,
            PasswordHash = string.Empty,
            Role = EUserRole.User
        };
        user.PasswordHash = PasswordHasher.HashPassword(user, request.Password);

        await UserRepository.CreateAsync(user);

        await TransactionManager.SaveChangesAsync();

        GeneratedToken generatedToken = GenerateToken(TokenSecretsOptions.Value.EmailVerification);

        Token token = new()
        {
            Hash = generatedToken.Hash,
            ExpiryAt = DateTime.UtcNow.AddHours(TokenExpirationsOptions.Value.EmailVerificationHours),
            Type = ETokenType.EmailVerification,
            UserId = user.Id
        };
        await TokenRepository.CreateAsync(token);

        await TransactionManager.CommitTransactionAsync();

        LogUserCreated(Logger);

        await EmailClient.SendEmailVerificationAsync(user.Email, generatedToken.Plain);

        return Result.Ok();
    }

    #endregion

    #region ForgotPasswordAsync

    /// <inheritdoc />
    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        LogProcessingForgotPassword(Logger);

        ValidationResult validationResult = await ForgotPasswordRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogForgotPasswordRequestInvalid(Logger, validationResult.Errors);

            return Result.Fail(validationResult.Errors.ToErrorList());
        }

        string normalizedEmail = request.Email!.Trim().ToLowerInvariant();

        User? user = await UserRepository.GetSingleOrDefaultAsync(
            u => u.Email == normalizedEmail
        );

        if (user is null)
        {
            return Result.Fail(
                CreateError(ValidationMessages.UserNotFound, nameof(request.Email))
            );
        }

        await TransactionManager.BeginTransactionAsync();

        GeneratedToken generatedToken = GenerateToken(TokenSecretsOptions.Value.PasswordReset);

        Token token = new()
        {
            Hash = generatedToken.Hash,
            ExpiryAt = DateTime.UtcNow.AddMinutes(TokenExpirationsOptions.Value.PasswordResetMinutes),
            Type = ETokenType.PasswordReset,
            UserId = user.Id
        };
        await TokenRepository.CreateAsync(token);

        await TransactionManager.CommitTransactionAsync();

        LogPasswordResetTokenGenerated(Logger);

        await EmailClient.SendPasswordResetAsync(user.Email, generatedToken.Plain);

        return Result.Ok();
    }

    #endregion

    #region LoginAsync

    /// <inheritdoc />
    public async Task<Result<SessionResponse>> LoginAsync(LoginRequest request)
    {
        LogAuthenticatingUser(Logger);

        ValidationResult validationResult = await LoginRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogLoginRequestInvalid(Logger, validationResult.Errors);

            return Result.Fail(validationResult.Errors.ToErrorList());
        }

        string normalizedEmail = request.Email!.Trim().ToLowerInvariant();

        User? user = await UserRepository.GetSingleOrDefaultAsync(
            u => u.Email == normalizedEmail,
            asTracking: true
        );

        if (user is null)
        {
            return Result.Fail(ValidationMessages.InvalidCredentials);
        }

        PasswordVerificationResult passwordResult = PasswordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password!
        );

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Result.Fail(ValidationMessages.InvalidCredentials);
        }

        if (!user.IsEmailVerified)
        {
            return Result.Fail(
                CreateError(ValidationMessages.EmailNotVerified, nameof(request.Email))
            );
        }

        await TransactionManager.BeginTransactionAsync();

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = PasswordHasher.HashPassword(user, request.Password!);

            await TransactionManager.SaveChangesAsync();

            LogPasswordHashUpgraded(Logger, user.Id);
        }

        GeneratedToken generatedRefreshToken = GenerateToken(TokenSecretsOptions.Value.Refresh);

        Token refreshToken = new()
        {
            Hash = generatedRefreshToken.Hash,
            ExpiryAt = DateTime.UtcNow.AddDays(TokenExpirationsOptions.Value.RefreshDays),
            Type = ETokenType.Refresh,
            UserId = user.Id
        };
        await TokenRepository.CreateAsync(refreshToken);

        await TransactionManager.CommitTransactionAsync();

        LogUserAuthenticated(Logger);

        return Result.Ok(new SessionResponse
        {
            JsonWebToken = GenerateJsonWebToken(user),
            RefreshToken = generatedRefreshToken.Plain,
            RefreshTokenExpiryAt = refreshToken.ExpiryAt
        });
    }

    #endregion

    #region LogoutAsync

    /// <inheritdoc />
    public async Task<Result> LogoutAsync(RefreshRequest request)
    {
        LogEndingSession(Logger);

        ValidationResult validationResult = await RefreshRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogNoRefreshTokenPresented(Logger);
            return Result.Ok();
        }

        using HMACSHA256 hmacSha256 = new(Encoding.UTF8.GetBytes(TokenSecretsOptions.Value.Refresh));
        byte[] hashBytes = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(request.RefreshToken!));
        string incomingHash = Convert.ToBase64String(hashBytes);

        Token? existingToken = await TokenRepository.GetSingleOrDefaultAsync(
            t => t.Hash == incomingHash && t.Type == ETokenType.Refresh
        );

        if (existingToken is null)
        {
            LogRefreshTokenNotFound(Logger);
            return Result.Ok();
        }

        await TransactionManager.BeginTransactionAsync();

        TokenRepository.Delete(existingToken);

        await TransactionManager.CommitTransactionAsync();

        LogSessionEnded(Logger);

        return Result.Ok();
    }

    #endregion

    #region RefreshAsync

    /// <inheritdoc />
    public async Task<Result<SessionResponse>> RefreshAsync(RefreshRequest request)
    {
        LogRefreshingSession(Logger);

        ValidationResult validationResult = await RefreshRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogRefreshRequestInvalid(Logger, validationResult.Errors);

            return Result.Fail(validationResult.Errors.ToErrorList());
        }

        using HMACSHA256 hmacSha256 = new(Encoding.UTF8.GetBytes(TokenSecretsOptions.Value.Refresh));
        byte[] hashBytes = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(request.RefreshToken!));
        string incomingHash = Convert.ToBase64String(hashBytes);

        Token? existingToken = await TokenRepository.GetSingleOrDefaultAsync(
            t => t.Hash == incomingHash && t.Type == ETokenType.Refresh,
            includes: q => q.Include(t => t.User!)
        );

        if (existingToken is null || existingToken.ExpiryAt <= DateTime.UtcNow)
        {
            return Result.Fail(
                CreateError(ValidationMessages.InvalidRefreshToken, nameof(request.RefreshToken))
            );
        }

        await TransactionManager.BeginTransactionAsync();

        TokenRepository.Delete(existingToken);

        GeneratedToken generatedRefreshToken = GenerateToken(TokenSecretsOptions.Value.Refresh);

        Token newRefreshToken = new()
        {
            Hash = generatedRefreshToken.Hash,
            ExpiryAt = DateTime.UtcNow.AddDays(TokenExpirationsOptions.Value.RefreshDays),
            Type = ETokenType.Refresh,
            UserId = existingToken.UserId
        };
        await TokenRepository.CreateAsync(newRefreshToken);

        await TransactionManager.CommitTransactionAsync();

        LogSessionRefreshed(Logger);

        return Result.Ok(new SessionResponse
        {
            JsonWebToken = GenerateJsonWebToken(existingToken.User!),
            RefreshToken = generatedRefreshToken.Plain,
            RefreshTokenExpiryAt = newRefreshToken.ExpiryAt
        });
    }

    #endregion

    #region ResetPasswordAsync

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
    {
        LogResettingPassword(Logger);

        ValidationResult validationResult = await ResetPasswordRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogResetPasswordRequestInvalid(Logger, validationResult.Errors);

            return Result.Fail(validationResult.Errors.ToErrorList());
        }

        using HMACSHA256 hmacSha256 = new(Encoding.UTF8.GetBytes(TokenSecretsOptions.Value.PasswordReset));
        byte[] hashBytes = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(request.Token!));
        string incomingHash = Convert.ToBase64String(hashBytes);

        Token? existingToken = await TokenRepository.GetSingleOrDefaultAsync(
            t => t.Hash == incomingHash && t.Type == ETokenType.PasswordReset,
            asTracking: true,
            includes: q => q.Include(t => t.User!)
        );

        if (existingToken is null || existingToken.ExpiryAt <= DateTime.UtcNow)
        {
            return Result.Fail(
                CreateError(ValidationMessages.InvalidPasswordResetToken, nameof(request.Token))
            );
        }

        await TransactionManager.BeginTransactionAsync();

        existingToken.User!.PasswordHash = PasswordHasher.HashPassword(existingToken.User, request.Password!);

        TokenRepository.Delete(existingToken);

        await TransactionManager.CommitTransactionAsync();

        LogPasswordReset(Logger);
        return Result.Ok();
    }

    #endregion

    #region VerifyEmailAsync

    /// <inheritdoc />
    public async Task<Result> VerifyEmailAsync(VerifyEmailRequest request)
    {
        LogVerifyingEmail(Logger);

        ValidationResult validationResult = await VerifyEmailRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogVerifyEmailRequestInvalid(Logger, validationResult.Errors);

            return Result.Fail(validationResult.Errors.ToErrorList());
        }

        using HMACSHA256 hmacSha256 = new(Encoding.UTF8.GetBytes(TokenSecretsOptions.Value.EmailVerification));
        byte[] hashBytes = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(request.Token!));
        string incomingHash = Convert.ToBase64String(hashBytes);

        Token? existingToken = await TokenRepository.GetSingleOrDefaultAsync(
            t => t.Hash == incomingHash && t.Type == ETokenType.EmailVerification,
            asTracking: true,
            includes: q => q.Include(t => t.User!)
        );

        if (existingToken is null || existingToken.ExpiryAt <= DateTime.UtcNow)
        {
            return Result.Fail(
                CreateError(ValidationMessages.InvalidEmailVerificationToken, nameof(request.Token))
            );
        }

        await TransactionManager.BeginTransactionAsync();

        existingToken.User!.IsEmailVerified = true;

        TokenRepository.Delete(existingToken);

        await TransactionManager.CommitTransactionAsync();

        LogEmailVerified(Logger);
        return Result.Ok();
    }

    #endregion

    #region GenerateJsonWebToken

    /// <summary>
    /// Generates a signed JSON Web Token for the specified user.
    /// </summary>
    /// <param name="user">The authenticated user for whom the token is issued.</param>
    /// <returns>The compact serialized JWT string.</returns>
    private string GenerateJsonWebToken(User user)
    {
        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(TokenSecretsOptions.Value.JsonWeb));
        SigningCredentials signingCredentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        ];

        DateTime now = DateTime.UtcNow;

        JwtSecurityToken token = new(
            issuer: JsonWebTokenOptions.Value.Issuer,
            audience: JsonWebTokenOptions.Value.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(TokenExpirationsOptions.Value.JsonWebMinutes),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}
