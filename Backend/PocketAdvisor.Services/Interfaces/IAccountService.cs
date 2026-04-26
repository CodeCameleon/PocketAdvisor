using FluentResults;
using PocketAdvisor.Requests.Accounts;
using PocketAdvisor.Responses.Accounts;

namespace PocketAdvisor.Services.Interfaces;

/// <summary>
/// Defines the service interface for performing operations related to accounts.
/// </summary>
public interface IAccountService
    : IBaseService
{
    /// <summary>
    /// Creates a new account for the specified user asynchronously.
    /// </summary>
    /// <param name="request">The data of the account to create.</param>
    /// <param name="userId">The identifier of the user who owns the account.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> CreateAccountAsync(CreateAccountRequest request, Guid userId);
    
    /// <summary>
    /// Retrieves all accounts that belong to the specified user asynchronously.
    /// </summary>
    /// <param name="userId">The identifier of the user whose accounts to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// read-only list of <see cref="AccountResponse" /> representing the user's accounts.
    /// </returns>
    Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(Guid userId);
}
