using FluentValidation;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="CreateUserRequest" /> model.
/// </summary>
public sealed class CreateUserRequestValidator
    : AbstractValidator<CreateUserRequest>
{
    #region Fields
    
    /// <summary>
    /// The user repository instance used for validation purposes.
    /// </summary>
    private readonly IUserRepository _userRepository;
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserRequestValidator" /> class.
    /// </summary>
    /// <param name="userRepository">The user repository instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If the user repository instance is <see langword="null" />.
    /// </exception>
    public CreateUserRequestValidator(IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        _userRepository = userRepository;
        
        RuleFor(cur => cur.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.EmailTooLong)
            .EmailAddress().WithMessage(ValidationMessages.EmailInvalid)
            .MustAsync(BeUniqueEmail).WithMessage(ValidationMessages.EmailAlreadyExists);
        
        RuleFor(cur => cur.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.PasswordRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.PasswordTooLong)
            .Must(BeStrongPassword).WithMessage(ValidationMessages.PasswordTooWeak);
        
        RuleFor(cur => cur.ConfirmPassword).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.ConfirmPasswordRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.ConfirmPasswordTooLong)
            .Equal(cur => cur.Password).WithMessage(ValidationMessages.ConfirmPasswordMismatch)
            .Must(BeStrongPassword).WithMessage(ValidationMessages.ConfirmPasswordTooWeak);
    }
    
    #endregion
    
    #region BeStrongPassword
    
    /// <summary>
    /// Validates whether the given password satisfies the required strength policy.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>
    /// <see langword="true" />, if the password is strong enough, <see langword="false" /> otherwise.
    /// </returns>
    private static bool BeStrongPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }
        
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));
        
        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
    
    #endregion
    
    #region BeUniqueEmail
    
    /// <summary>
    /// Validates the uniqueness of the given email address asynchronously.
    /// </summary>
    /// <param name="email">The email address to validate for uniqueness.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains <see langword="true" />,
    /// if the email address is not found, <see langword="false" /> otherwise.
    /// </returns>
    private async Task<bool> BeUniqueEmail(string? email, CancellationToken cancellationToken)
    {
        string normalizedEmail = (email ?? string.Empty).Trim().ToUpperInvariant();
        
        return !await _userRepository.ExistsAsync(
            u => u.Email.ToUpper() == normalizedEmail,
            cancellationToken
        );
    }
    
    #endregion
}
