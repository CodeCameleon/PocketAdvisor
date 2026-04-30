using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
public sealed class AccountService
    : BaseService<AccountService>, IAccountService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="accountRepository">The account repository instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public AccountService(ILogger<AccountService> logger, IServiceProvider serviceProvider,
        IAccountRepository accountRepository)
        : base(logger, serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        
        AccountRepository = accountRepository;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The account repository instance.
    /// </summary>
    private IAccountRepository AccountRepository { get; }
    
    #endregion
    
    #region CreateAccountAsync
    
    /// <inheritdoc />
    public async Task<Result> CreateAccountAsync(CreateAccountRequest request, Guid userId)
    {
        Logger.LogInformation("Creating new account...");
        
        IValidator<CreateAccountRequest> validator = GetValidator<CreateAccountRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for CreateAccountRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
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
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        Account account = new()
        {
            Name = normalizedName,
            Balance = request.Balance!.Value,
            CurrencyCode = request.CurrencyCode!.Value,
            UserId = userId
        };
        await AccountRepository.CreateAsync(account);
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        Logger.LogInformation("New account created successfully.");
        return Result.Ok();
    }
    
    #endregion
    
    #region DeleteAccountAsync
    
    /// <inheritdoc />
    public async Task<Result> DeleteAccountAsync(Guid accountId, Guid userId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Deleting account '{AccountId}'...", accountId);
        }
        
        Account? account = await AccountRepository.GetSingleOrDefaultAsync(
            a => a.Id == accountId && a.UserId == userId
        );
        
        if (account is null)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Account '{AccountId}' was not found for user '{UserId}'.",
                    accountId,
                    userId
                );
            }
            
            return Result.Fail(CreateNotFoundError());
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        AccountRepository.Delete(account);
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Account '{AccountId}' deleted successfully.", accountId);
        }
        
        return Result.Ok();
    }
    
    #endregion
    
    #region GetAccountsAsync
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(Guid userId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Retrieving accounts for user '{UserId}'...", userId);
        }
        
        IReadOnlyList<Account> accounts = await AccountRepository.GetAllAsync(
            a => a.UserId == userId,
            asSplitQuery: true,
            includes: q => q
                .Include(a =>a.IncomingTransactions!)
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
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Retrieved {Count} accounts for user '{UserId}'.", response.Count, userId);
        }
        
        return response;
    }
    
    #endregion
    
    #region UpdateAccountNameAsync
    
    /// <inheritdoc />
    public async Task<Result> UpdateAccountNameAsync(Guid accountId, UpdateAccountNameRequest request, Guid userId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Updating name of account '{AccountId}'...", accountId);
        }
        
        IValidator<UpdateAccountNameRequest> validator = GetValidator<UpdateAccountNameRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for UpdateAccountNameRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        Account? account = await AccountRepository.GetSingleOrDefaultAsync(
            a => a.Id == accountId && a.UserId == userId,
            asTracking: true
        );
        
        if (account is null)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Account '{AccountId}' was not found for user '{UserId}'.",
                    accountId,
                    userId
                );
            }
            
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
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        account.Name = normalizedName;
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Account '{AccountId}' name updated successfully.", accountId);
        }
        
        return Result.Ok();
    }
    
    #endregion
}
