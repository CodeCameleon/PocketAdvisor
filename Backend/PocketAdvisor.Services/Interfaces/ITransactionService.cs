using FluentResults;
using PocketAdvisor.Requests.Transactions;

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
    /// <see cref="Result" /> indicating the success or failure of the operation
    /// </returns>
    Task<Result> CreateTransactionAsync(CreateTransactionRequest request, Guid userId);
}
