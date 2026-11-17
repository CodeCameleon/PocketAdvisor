using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts.Interfaces;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Requests.Accounts;
using PocketAdvisor.Responses.Accounts;
using PocketAdvisor.Services.Extensions;
using PocketAdvisor.Services.Interfaces;
using PocketAdvisor.Services.Resources;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to accounts.
/// </summary>
public sealed partial class AccountService
    : BaseDatabaseService<AccountService>, IAccountService
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="transactionManager">The transaction manager of the database.</param>
    /// <param name="accountRepository">The account repository instance.</param>
    /// <param name="createAccountRequestValidator">
    /// The validator for the <see cref="CreateAccountRequest" /> model.
    /// </param>
    /// <param name="updateAccountNameRequestValidator">
    /// The validator for the <see cref="UpdateAccountNameRequest" /> model.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public AccountService(ILogger<AccountService> logger, ITransactionManager transactionManager,
        IAccountRepository accountRepository,
        IValidator<CreateAccountRequest> createAccountRequestValidator,
        IValidator<UpdateAccountNameRequest> updateAccountNameRequestValidator)
        : base(logger, transactionManager)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(createAccountRequestValidator);
        ArgumentNullException.ThrowIfNull(updateAccountNameRequestValidator);

        AccountRepository = accountRepository;
        CreateAccountRequestValidator = createAccountRequestValidator;
        UpdateAccountNameRequestValidator = updateAccountNameRequestValidator;
    }

    #endregion

    #region Properties

    /// <summary>
    /// The account repository instance.
    /// </summary>
    private IAccountRepository AccountRepository { get; }

    /// <summary>
    /// The validator for the <see cref="CreateAccountRequest" /> model.
    /// </summary>
    private IValidator<CreateAccountRequest> CreateAccountRequestValidator { get; }

    /// <summary>
    /// The validator for the <see cref="UpdateAccountNameRequest" /> model.
    /// </summary>
    private IValidator<UpdateAccountNameRequest> UpdateAccountNameRequestValidator { get; }

    #endregion

    #region LogMessages

    /// <summary>
    /// Logs that a new account is being created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Creating new account...")]
    private static partial void LogCreatingAccount(ILogger logger);

    /// <summary>
    /// Logs that the create account request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Validation failed for CreateAccountRequest: {Errors}")]
    private static partial void LogCreateAccountRequestInvalid(ILogger logger, IEnumerable<ValidationFailure> errors);

    /// <summary>
    /// Logs that the new account was created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "New account created successfully.")]
    private static partial void LogAccountCreated(ILogger logger);

    /// <summary>
    /// Logs that an account is being deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountId">The ID of the account.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Deleting account '{AccountId}'...")]
    private static partial void LogDeletingAccount(ILogger logger, Guid accountId);

    /// <summary>
    /// Logs that the account was not found for the user.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountId">The ID of the account.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
        Message = "Account '{AccountId}' was not found for user '{UserId}'.")]
    private static partial void LogAccountNotFound(ILogger logger, Guid accountId, Guid userId);

    /// <summary>
    /// Logs that the account was deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountId">The ID of the account.</param>
    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Account '{AccountId}' deleted successfully.")]
    private static partial void LogAccountDeleted(ILogger logger, Guid accountId);

    /// <summary>
    /// Logs that the accounts of the user are being retrieved.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Retrieving accounts for user '{UserId}'...")]
    private static partial void LogRetrievingAccounts(ILogger logger, Guid userId);

    /// <summary>
    /// Logs the number of retrieved accounts.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of retrieved accounts.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 8, Level = LogLevel.Information,
        Message = "Retrieved {Count} accounts for user '{UserId}'.")]
    private static partial void LogAccountsRetrieved(ILogger logger, int count, Guid userId);

    /// <summary>
    /// Logs that the name of an account is being updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountId">The ID of the account.</param>
    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Updating name of account '{AccountId}'...")]
    private static partial void LogUpdatingAccountName(ILogger logger, Guid accountId);

    /// <summary>
    /// Logs that the update account name request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 10, Level = LogLevel.Warning,
        Message = "Validation failed for UpdateAccountNameRequest: {Errors}")]
    private static partial void LogUpdateAccountNameRequestInvalid(ILogger logger,
        IEnumerable<ValidationFailure> errors);

    /// <summary>
    /// Logs that the name of the account was updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountId">The ID of the account.</param>
    [LoggerMessage(EventId = 11, Level = LogLevel.Information,
        Message = "Account '{AccountId}' name updated successfully.")]
    private static partial void LogAccountNameUpdated(ILogger logger, Guid accountId);

    #endregion

    #region CreateAccountAsync

    /// <inheritdoc />
    public async Task<Result> CreateAccountAsync(CreateAccountRequest request, Guid userId)
    {
        LogCreatingAccount(Logger);

        ValidationResult validationResult = await CreateAccountRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogCreateAccountRequestInvalid(Logger, validationResult.Errors);

            return Result.Fail(validationResult.Errors.ToErrorList());
        }

        string normalizedName = request.Name!.Trim();

        bool nameExists = await AccountRepository.ExistsAsync(
            a => a.UserId == userId && a.Name == normalizedName
        );

        if (nameExists)
        {
            return Result.Fail(
                CreateError(ValidationMessages.AccountNameAlreadyExists, nameof(request.Name))
            );
        }

        await TransactionManager.BeginTransactionAsync();

        Account account = new()
        {
            Name = normalizedName,
            Balance = request.Balance!.Value,
            CurrencyCode = request.CurrencyCode!.Value,
            UserId = userId
        };
        await AccountRepository.CreateAsync(account);

        await TransactionManager.CommitTransactionAsync();

        LogAccountCreated(Logger);
        return Result.Ok();
    }

    #endregion

    #region DeleteAccountAsync

    /// <inheritdoc />
    public async Task<Result> DeleteAccountAsync(Guid accountId, Guid userId)
    {
        LogDeletingAccount(Logger, accountId);

        Account? account = await AccountRepository.GetSingleOrDefaultAsync(
            a => a.Id == accountId && a.UserId == userId
        );

        if (account is null)
        {
            LogAccountNotFound(Logger, accountId, userId);

            return Result.Fail(CreateNotFoundError());
        }

        await TransactionManager.BeginTransactionAsync();

        AccountRepository.Delete(account);

        await TransactionManager.CommitTransactionAsync();

        LogAccountDeleted(Logger, accountId);

        return Result.Ok();
    }

    #endregion

    #region GetAccountsAsync

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(Guid userId)
    {
        LogRetrievingAccounts(Logger, userId);

        IReadOnlyList<Account> accounts = await AccountRepository.GetAllAsync(
            a => a.UserId == userId,
            asSplitQuery: true,
            includes: q => q
                .Include(a => a.IncomingTransactions!)
                    .ThenInclude(t => t.TransactionItems!)
                .Include(a => a.OutgoingTransactions!)
                    .ThenInclude(t => t.TransactionItems!)
        );

        List<AccountResponse> response = accounts.Select(a =>
        {
            decimal incoming = a.IncomingTransactions?
                .SelectMany(t => t.TransactionItems ?? [])
                .Sum(ti => ti.TotalPrice) ?? 0m;

            decimal outgoing = a.OutgoingTransactions?
                .SelectMany(t => t.TransactionItems ?? [])
                .Sum(ti => ti.TotalPrice) ?? 0m;

            return new AccountResponse
            {
                Id = a.Id,
                Name = a.Name,
                CalculatedBalance = a.Balance + incoming - outgoing,
                CurrencyCode = a.CurrencyCode
            };
        }).ToList();

        LogAccountsRetrieved(Logger, response.Count, userId);

        return response;
    }

    #endregion

    #region UpdateAccountNameAsync

    /// <inheritdoc />
    public async Task<Result> UpdateAccountNameAsync(Guid accountId, UpdateAccountNameRequest request, Guid userId)
    {
        LogUpdatingAccountName(Logger, accountId);

        ValidationResult validationResult = await UpdateAccountNameRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogUpdateAccountNameRequestInvalid(Logger, validationResult.Errors);

            return Result.Fail(validationResult.Errors.ToErrorList());
        }

        string normalizedName = request.Name!.Trim();

        Account? account = await AccountRepository.GetSingleOrDefaultAsync(
            a => a.Id == accountId && a.UserId == userId,
            asTracking: true
        );

        if (account is null)
        {
            LogAccountNotFound(Logger, accountId, userId);

            return Result.Fail(CreateNotFoundError());
        }

        bool nameExists = await AccountRepository.ExistsAsync(
            a => a.UserId == userId && a.Name == normalizedName && a.Id != accountId
        );

        if (nameExists)
        {
            return Result.Fail(
                CreateError(ValidationMessages.AccountNameAlreadyExists, nameof(request.Name))
            );
        }

        await TransactionManager.BeginTransactionAsync();

        account.Name = normalizedName;

        await TransactionManager.CommitTransactionAsync();

        LogAccountNameUpdated(Logger, accountId);

        return Result.Ok();
    }

    #endregion
}
