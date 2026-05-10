using FluentResults;
using PocketAdvisor.Requests.Transactions;
using PocketAdvisor.Responses.Transactions;

namespace PocketAdvisor.Services.Interfaces;

/// <summary>
/// Defines the service interface for performing operations related to transactions.
/// </summary>
public interface ITransactionService
    : IBaseService
{
    /// <summary>
    /// Creates a new transaction together with its items asynchronously.
    /// </summary>
    /// <param name="request">The data of the transaction to create.</param>
    /// <param name="userId">The identifier of the currently authenticated user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> CreateTransactionAsync(CreateTransactionRequest request, Guid userId);
    
    /// <summary>
    /// Deletes the specified transaction and all of its items asynchronously.
    /// </summary>
    /// <param name="transactionId">The identifier of the transaction to delete.</param>
    /// <param name="userId">The identifier of the currently authenticated user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> DeleteTransactionAsync(Guid transactionId, Guid userId);
    
    /// <summary>
    /// Deletes a single item from the specified transaction asynchronously.
    /// </summary>
    /// <param name="transactionId">The identifier of the transaction.</param>
    /// <param name="itemId">The identifier of the item to remove from the transaction.</param>
    /// <param name="userId">The identifier of the currently authenticated user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> DeleteTransactionItemAsync(Guid transactionId, Guid itemId, Guid userId);
    
    /// <summary>
    /// Retrieves all transactions associated with the specified account asynchronously.
    /// </summary>
    /// <param name="accountId">The identifier of the account to filter transactions by.</param>
    /// <param name="userId">The identifier of the currently authenticated user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// read-only list of <see cref="TransactionResponse" /> objects.
    /// </returns>
    Task<IReadOnlyList<TransactionResponse>> GetTransactionsAsync(Guid accountId, Guid userId);
    
    /// <summary>
    /// Retrieves all transactions that contain the specified item across all accounts.
    /// </summary>
    /// <param name="itemId">The identifier of the item to filter transactions by.</param>
    /// <param name="userId">The identifier of the currently authenticated user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// read-only list of <see cref="TransactionResponse" /> objects.
    /// </returns>
    Task<IReadOnlyList<TransactionResponse>> GetTransactionsByItemAsync(Guid itemId, Guid userId);
}
