using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using PocketAdvisor.Entities;
using PocketAdvisor.Enums.Extensions;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Requests.Transactions;
using PocketAdvisor.Services.Extensions;
using PocketAdvisor.Services.Interfaces;
using PocketAdvisor.Services.Resources;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to transactions.
/// </summary>
public sealed class TransactionService
    : BaseService<TransactionService>, ITransactionService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="accountRepository">The account repository instance.</param>
    /// <param name="categoryRepository">The category repository instance.</param>
    /// <param name="itemRepository">The item repository instance.</param>
    /// <param name="transactionItemRepository">The transaction item repository instance.</param>
    /// <param name="transactionRepository">The transaction repository instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public TransactionService(ILogger<TransactionService> logger, IServiceProvider serviceProvider,
        IAccountRepository accountRepository, ICategoryRepository categoryRepository,
        IItemRepository itemRepository, ITransactionItemRepository transactionItemRepository,
        ITransactionRepository transactionRepository)
        : base(logger, serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(itemRepository);
        ArgumentNullException.ThrowIfNull(transactionItemRepository);
        ArgumentNullException.ThrowIfNull(transactionRepository);
        
        AccountRepository = accountRepository;
        CategoryRepository = categoryRepository;
        ItemRepository = itemRepository;
        TransactionItemRepository = transactionItemRepository;
        TransactionRepository = transactionRepository;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The account repository instance.
    /// </summary>
    private IAccountRepository AccountRepository { get; }
    
    /// <summary>
    /// The category repository instance.
    /// </summary>
    private ICategoryRepository CategoryRepository { get; }
    
    /// <summary>
    /// The item repository instance.
    /// </summary>
    private IItemRepository ItemRepository { get; }
    
    /// <summary>
    /// The transaction item repository instance.
    /// </summary>
    private ITransactionItemRepository TransactionItemRepository { get; }
    
    /// <summary>
    /// The transaction repository instance.
    /// </summary>
    private ITransactionRepository TransactionRepository { get; }
    
    #endregion
    
    #region CreateTransactionAsync
    
    /// <inheritdoc />
    public async Task<Result> CreateTransactionAsync(CreateTransactionRequest request, Guid userId)
    {
        Logger.LogInformation("Creating new transaction...");
        
        IValidator<CreateTransactionRequest> validator = GetValidator<CreateTransactionRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for CreateTransactionRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        bool categoryExists = await CategoryRepository.ExistsAsync(
            c => c.Id == request.CategoryId!.Value && (c.UserId == null || c.UserId == userId)
        );
        
        if (!categoryExists)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Category '{CategoryId}' was not found for user '{UserId}'.",
                    request.CategoryId,
                    userId
                );
            }
            
            return Result.Fail(string.Empty);
        }
        
        if (request.FromAccountId.HasValue)
        {
            bool fromAccountExists = await AccountRepository.ExistsAsync(
                a => a.Id == request.FromAccountId.Value && a.UserId == userId
            );
            
            if (!fromAccountExists)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                {
                    Logger.LogWarning(
                        "Source account '{AccountId}' was not found for user '{UserId}'.",
                        request.FromAccountId,
                        userId
                    );
                }
                
                return Result.Fail(string.Empty);
            }
        }
        
        if (request.ToAccountId.HasValue)
        {
            bool toAccountExists = await AccountRepository.ExistsAsync(
                a => a.Id == request.ToAccountId.Value && a.UserId == userId
            );
            
            if (!toAccountExists)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                {
                    Logger.LogWarning(
                        "Destination account '{AccountId}' was not found for user '{UserId}'.",
                        request.ToAccountId,
                        userId
                    );
                }
                
                return Result.Fail(string.Empty);
            }
        }
        
        List<Guid> itemIds = request.Items!.Select(i =>
            i.ItemId!.Value
        ).Distinct().ToList();
        
        IReadOnlyList<Item> items = await ItemRepository.GetAllAsync(
            i => itemIds.Contains(i.Id) && i.UserId == userId
        );
        
        if (items.Count != itemIds.Count)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "One or more items were not found for user '{UserId}'.",
                    userId
                );
            }
            
            return Result.Fail(string.Empty);
        }
        
        Dictionary<Guid, Item> itemsById = items.ToDictionary(i => i.Id);
        foreach (CreateTransactionItemRequest itemRequest in request.Items!)
        {
            Item item = itemsById[itemRequest.ItemId!.Value];
            
            if (itemRequest.Unit!.Value.GetUnitCategory() != item.UnitCategory)
            {
                return Result.Fail(
                    CreateError(
                        ValidationMessages.TransactionItemUnitCategoryMismatch,
                        nameof(itemRequest.Unit)
                    )
                );
            }
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        Transaction transaction = new()
        {
            OccurredAt = request.OccurredAt!.Value,
            CategoryId = request.CategoryId!.Value,
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId
        };
        await TransactionRepository.CreateAsync(transaction);
        
        await TransactionManager.Value.SaveChangesAsync();
        
        foreach (CreateTransactionItemRequest itemRequest in request.Items!)
        {
            TransactionItem transactionItem = new()
            {
                TransactionId = transaction.Id,
                ItemId = itemRequest.ItemId!.Value,
                TotalPrice = itemRequest.TotalPrice!.Value,
                Amount = new(itemRequest.Amount!.Value, itemRequest.Unit!.Value)
            };
            await TransactionItemRepository.CreateAsync(transactionItem);
        }
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        Logger.LogInformation("New transaction created successfully.");
        return Result.Ok();
    }
    
    #endregion
}
