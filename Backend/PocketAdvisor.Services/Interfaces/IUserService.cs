using FluentResults;
using PocketAdvisor.Requests.Users;

namespace PocketAdvisor.Services.Interfaces;

/// <summary>
/// Defines the service interface for performing operations related to users.
/// </summary>
public interface IUserService
    : IBaseService
{
    /// <summary>
    /// Creates a new user in the system asynchronously.
    /// </summary>
    /// <param name="request">The data of the user to create.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// If successful, the result contains the email verification token of the new user.
    /// </returns>
    Task<Result<string>> CreateUserAsync(CreateUserRequest request);
}
