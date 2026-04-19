using FluentResults;
using PocketAdvisor.Requests.Users;
using PocketAdvisor.Responses.Users;

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
    /// <see cref="Result{TValue}" /> indicating the success or failure of the operation.
    /// If successful, the result contains the email verification token of the new user.
    /// </returns>
    Task<Result<string>> CreateUserAsync(CreateUserRequest request);
    
    /// <summary>
    /// Authenticates a user and issues a JSON Web Token and refresh token asynchronously.
    /// </summary>
    /// <param name="request">The credentials of the user to authenticate.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result{TValue}" /> indicating the success or failure of the operation.
    /// If successful, the result contains a <see cref="LoginResponse" /> with the issued tokens.
    /// </returns>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
}
