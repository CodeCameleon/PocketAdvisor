using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts.Interfaces;
using PocketAdvisor.Entities;
using PocketAdvisor.Enums.Extensions;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Requests.Transactions;
using PocketAdvisor.Responses.Transactions;
using PocketAdvisor.Services.Extensions;
using PocketAdvisor.Services.Interfaces;
using PocketAdvisor.Services.Resources;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to transactions.
/// </summary>
public sealed partial class TransactionService
    : BaseDatabaseService<TransactionService>, ITransactionService
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="transactionManager">The transaction manager of the database.</param>
    /// <param name="accountRepository">The account repository instance.</param>
    /// <param name="categoryRepository">The category repository instance.</param>
    /// <param name="itemRepository">The item repository instance.</param>
    /// <param name="transactionItemRepository">The transaction item repository instance.</param>
    /// <param name="transactionRepository">The transaction repository instance.</param>
    /// <param name="createTransactionRequestValidator">
    /// The validator for the <see cref="CreateTransactionRequest" /> model.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public TransactionService(ILogger<TransactionService> logger, ITransactionManager transactionManager,
        IAccountRepository accountRepository, ICategoryRepository categoryRepository,
        IItemRepository itemRepository, ITransactionItemRepository transactionItemRepository,
        ITransactionRepository transactionRepository,
        IValidator<CreateTransactionRequest> createTransactionRequestValidator)
        : base(logger, transactionManager)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(itemRepository);
        ArgumentNullException.ThrowIfNull(transactionItemRepository);
        ArgumentNullException.ThrowIfNull(transactionRepository);
        ArgumentNullException.ThrowIfNull(createTransactionRequestValidator);

        AccountRepository = accountRepository;
        CategoryRepository = categoryRepository;
        ItemRepository = itemRepository;
        TransactionItemRepository = transactionItemRepository;
        TransactionRepository = transactionRepository;
        CreateTransactionRequestValidator = createTransactionRequestValidator;
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

    /// <summary>
    /// The validator for the <see cref="CreateTransactionRequest" /> model.
    /// </summary>
    private IValidator<CreateTransactionRequest> CreateTransactionRequestValidator { get; }

    #endregion

    #region LogMessages

    /// <summary>
    /// Logs that a new transaction is being created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Creating new transaction...")]
    private static partial void LogCreatingTransaction(ILogger logger);

    /// <summary>
    /// Logs that the create transaction request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Validation failed for CreateTransactionRequest: {Errors}")]
    private static partial void LogCreateTransactionRequestInvalid(ILogger logger,
        IEnumerable<ValidationFailure> errors);

    /// <summary>
    /// Logs that the category of the transaction was not found for the user.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "Category '{CategoryId}' was not found for user '{UserId}'.")]
    private static partial void LogCategoryNotFound(ILogger logger, Guid? categoryId, Guid userId);

    /// <summary>
    /// Logs that the source account of the transaction was not found for the user.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountId">The ID of the account.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
        Message = "Source account '{AccountId}' was not found for user '{UserId}'.")]
    private static partial void LogSourceAccountNotFound(ILogger logger, Guid? accountId, Guid userId);

    /// <summary>
    /// Logs that the destination account of the transaction was not found for the user.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountId">The ID of the account.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
        Message = "Destination account '{AccountId}' was not found for user '{UserId}'.")]
    private static partial void LogDestinationAccountNotFound(ILogger logger, Guid? accountId, Guid userId);

    /// <summary>
    /// Logs that one or more items of the transaction were not found for the user.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 6, Level = LogLevel.Warning,
        Message = "One or more items were not found for user '{UserId}'.")]
    private static partial void LogItemsNotFound(ILogger logger, Guid userId);

    /// <summary>
    /// Logs that the new transaction was created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "New transaction created successfully.")]
    private static partial void LogTransactionCreated(ILogger logger);

    /// <summary>
    /// Logs that a transaction is being deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Deleting transaction '{TransactionId}'...")]
    private static partial void LogDeletingTransaction(ILogger logger, Guid transactionId);

    /// <summary>
    /// Logs that the transaction was not found for the user.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 9, Level = LogLevel.Warning,
        Message = "Transaction '{TransactionId}' was not found for user '{UserId}'.")]
    private static partial void LogTransactionNotFound(ILogger logger, Guid transactionId, Guid userId);

    /// <summary>
    /// Logs that the transaction was deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 10, Level = LogLevel.Information,
        Message = "Transaction '{TransactionId}' deleted successfully.")]
    private static partial void LogTransactionDeleted(ILogger logger, Guid transactionId);

    /// <summary>
    /// Logs that an item is being deleted from a transaction.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 11, Level = LogLevel.Information,
        Message = "Deleting item '{ItemId}' from transaction '{TransactionId}'...")]
    private static partial void LogDeletingTransactionItem(ILogger logger, Guid itemId, Guid transactionId);

    /// <summary>
    /// Logs that the item was not found on the transaction.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 12, Level = LogLevel.Warning,
        Message = "Item '{ItemId}' was not found on transaction '{TransactionId}'.")]
    private static partial void LogTransactionItemNotFound(ILogger logger, Guid itemId, Guid transactionId);

    /// <summary>
    /// Logs that the item cannot be deleted because it is the last item of the transaction.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 13, Level = LogLevel.Warning,
        Message = "Cannot delete item '{ItemId}' from transaction '{TransactionId}' as it is the last item.")]
    private static partial void LogLastTransactionItemNotDeletable(ILogger logger, Guid itemId, Guid transactionId);

    /// <summary>
    /// Logs that the item was deleted from the transaction.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 14, Level = LogLevel.Information,
        Message = "Item '{ItemId}' deleted from transaction '{TransactionId}' successfully.")]
    private static partial void LogTransactionItemDeleted(ILogger logger, Guid itemId, Guid transactionId);

    /// <summary>
    /// Logs that the transactions of an account are being retrieved.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountId">The ID of the account.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 15, Level = LogLevel.Information,
        Message = "Retrieving transactions for account '{AccountId}' and user '{UserId}'...")]
    private static partial void LogRetrievingAccountTransactions(ILogger logger, Guid accountId, Guid userId);

    /// <summary>
    /// Logs the number of retrieved transactions of the account.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of retrieved transactions.</param>
    /// <param name="accountId">The ID of the account.</param>
    [LoggerMessage(EventId = 16, Level = LogLevel.Information,
        Message = "Retrieved {Count} transactions for account '{AccountId}'.")]
    private static partial void LogAccountTransactionsRetrieved(ILogger logger, int count, Guid accountId);

    /// <summary>
    /// Logs that the transactions of an item are being retrieved.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 17, Level = LogLevel.Information,
        Message = "Retrieving transactions for item '{ItemId}' and user '{UserId}'...")]
    private static partial void LogRetrievingItemTransactions(ILogger logger, Guid itemId, Guid userId);

    /// <summary>
    /// Logs the number of retrieved transactions of the item.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of retrieved transactions.</param>
    /// <param name="itemId">The ID of the item.</param>
    [LoggerMessage(EventId = 18, Level = LogLevel.Information,
        Message = "Retrieved {Count} transactions for item '{ItemId}'.")]
    private static partial void LogItemTransactionsRetrieved(ILogger logger, int count, Guid itemId);

    #endregion

    #region CreateTransactionAsync

    /// <inheritdoc />
    public async Task<Result> CreateTransactionAsync(CreateTransactionRequest request, Guid userId)
    {
        LogCreatingTransaction(Logger);

        ValidationResult validationResult = await CreateTransactionRequestValidator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            LogCreateTransactionRequestInvalid(Logger, validationResult.Errors);

            return Result.Fail(validationResult.Errors.ToErrorList());
        }

        bool categoryExists = await CategoryRepository.ExistsAsync(
            c => c.Id == request.CategoryId!.Value && (c.UserId == null || c.UserId == userId)
        );

        if (!categoryExists)
        {
            LogCategoryNotFound(Logger, request.CategoryId, userId);

            return Result.Fail(CreateNotFoundError());
        }

        if (request.FromAccountId.HasValue)
        {
            bool fromAccountExists = await AccountRepository.ExistsAsync(
                a => a.Id == request.FromAccountId.Value && a.UserId == userId
            );

            if (!fromAccountExists)
            {
                LogSourceAccountNotFound(Logger, request.FromAccountId, userId);

                return Result.Fail(CreateNotFoundError());
            }
        }

        if (request.ToAccountId.HasValue)
        {
            bool toAccountExists = await AccountRepository.ExistsAsync(
                a => a.Id == request.ToAccountId.Value && a.UserId == userId
            );

            if (!toAccountExists)
            {
                LogDestinationAccountNotFound(Logger, request.ToAccountId, userId);

                return Result.Fail(CreateNotFoundError());
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
            LogItemsNotFound(Logger, userId);

            return Result.Fail(CreateNotFoundError());
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

        await TransactionManager.BeginTransactionAsync();

        Transaction transaction = new()
        {
            OccurredAt = request.OccurredAt!.Value,
            CategoryId = request.CategoryId!.Value,
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId
        };
        await TransactionRepository.CreateAsync(transaction);

        await TransactionManager.SaveChangesAsync();

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

        await TransactionManager.CommitTransactionAsync();

        LogTransactionCreated(Logger);
        return Result.Ok();
    }

    #endregion

    #region DeleteTransactionAsync

    /// <inheritdoc />
    public async Task<Result> DeleteTransactionAsync(Guid transactionId, Guid userId)
    {
        LogDeletingTransaction(Logger, transactionId);

        Transaction? transaction = await TransactionRepository.GetSingleOrDefaultAsync(t =>
            t.Id == transactionId && (
                (t.FromAccountId.HasValue && t.FromAccount!.UserId == userId) ||
                (t.ToAccountId.HasValue && t.ToAccount!.UserId == userId)
            )
        );

        if (transaction is null)
        {
            LogTransactionNotFound(Logger, transactionId, userId);

            return Result.Fail(CreateNotFoundError());
        }

        await TransactionManager.BeginTransactionAsync();

        TransactionRepository.Delete(transaction);

        await TransactionManager.CommitTransactionAsync();

        LogTransactionDeleted(Logger, transactionId);

        return Result.Ok();
    }

    #endregion

    #region DeleteTransactionItemAsync

    /// <inheritdoc />
    public async Task<Result> DeleteTransactionItemAsync(Guid transactionId, Guid itemId, Guid userId)
    {
        LogDeletingTransactionItem(Logger, itemId, transactionId);

        Transaction? transaction = await TransactionRepository.GetSingleOrDefaultAsync(t =>
            t.Id == transactionId && (
                (t.FromAccountId.HasValue && t.FromAccount!.UserId == userId) ||
                (t.ToAccountId.HasValue && t.ToAccount!.UserId == userId)
            )
        );

        if (transaction is null)
        {
            LogTransactionNotFound(Logger, transactionId, userId);

            return Result.Fail(CreateNotFoundError());
        }

        TransactionItem? transactionItem = await TransactionItemRepository.GetSingleOrDefaultAsync(
            ti => ti.TransactionId == transactionId && ti.ItemId == itemId
        );

        if (transactionItem is null)
        {
            LogTransactionItemNotFound(Logger, itemId, transactionId);

            return Result.Fail(CreateNotFoundError());
        }

        bool hasOtherItems = await TransactionItemRepository.ExistsAsync(
            ti => ti.TransactionId == transactionId && ti.ItemId != itemId
        );

        if (!hasOtherItems)
        {
            LogLastTransactionItemNotDeletable(Logger, itemId, transactionId);

            return Result.Fail(CreateConflictError());
        }

        await TransactionManager.BeginTransactionAsync();

        TransactionItemRepository.Delete(transactionItem);

        await TransactionManager.CommitTransactionAsync();

        LogTransactionItemDeleted(Logger, itemId, transactionId);

        return Result.Ok();
    }

    #endregion

    #region GetTransactionsAsync

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsAsync(Guid accountId, Guid userId)
    {
        LogRetrievingAccountTransactions(Logger, accountId, userId);

        IReadOnlyList<Transaction> transactions = await TransactionRepository.GetAllAsync(
            t => (t.FromAccountId == accountId || t.ToAccountId == accountId) && (
                (t.FromAccountId.HasValue && t.FromAccount!.UserId == userId) ||
                (t.ToAccountId.HasValue && t.ToAccount!.UserId == userId)
            ),
            includes: q => q.Include(t => t.TransactionItems!)
        );

        List<TransactionResponse> response = transactions.Select(t => new TransactionResponse
        {
            Id = t.Id,
            OccurredAt = t.OccurredAt,
            CategoryId = t.CategoryId,
            FromAccountId = t.FromAccountId,
            ToAccountId = t.ToAccountId,
            Items = t.TransactionItems!.Select(ti => new TransactionItemResponse
            {
                ItemId = ti.ItemId,
                TotalPrice = ti.TotalPrice,
                AmountValue = ti.Amount.Value,
                AmountUnit = ti.Amount.Unit
            }).ToList()
        }).ToList();

        LogAccountTransactionsRetrieved(Logger, response.Count, accountId);

        return response;
    }

    #endregion

    #region GetTransactionsByItemAsync

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsByItemAsync(Guid itemId, Guid userId)
    {
        LogRetrievingItemTransactions(Logger, itemId, userId);

        IReadOnlyList<Transaction> transactions = await TransactionRepository.GetAllAsync(
            t => t.TransactionItems!.Any(ti => ti.ItemId == itemId) && (
                (t.FromAccountId.HasValue && t.FromAccount!.UserId == userId) ||
                (t.ToAccountId.HasValue && t.ToAccount!.UserId == userId)
            ),
            includes: q => q.Include(t => t.TransactionItems!)
        );

        List<TransactionResponse> response = transactions.Select(t => new TransactionResponse
        {
            Id = t.Id,
            OccurredAt = t.OccurredAt,
            CategoryId = t.CategoryId,
            FromAccountId = t.FromAccountId,
            ToAccountId = t.ToAccountId,
            Items = t.TransactionItems!.Select(ti => new TransactionItemResponse
            {
                ItemId = ti.ItemId,
                TotalPrice = ti.TotalPrice,
                AmountValue = ti.Amount.Value,
                AmountUnit = ti.Amount.Unit
            }).ToList()
        }).ToList();

        LogItemTransactionsRetrieved(Logger, response.Count, itemId);

        return response;
    }

    #endregion
}
