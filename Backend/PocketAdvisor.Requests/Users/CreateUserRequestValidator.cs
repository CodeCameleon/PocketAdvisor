using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="CreateUserRequest" /> model.
/// </summary>
public sealed class CreateUserRequestValidator
    : AbstractValidator<CreateUserRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserRequestValidator" /> class.
    /// </summary>
    public CreateUserRequestValidator()
    {
        RuleFor(cur => cur.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.EmailTooLong)
            .EmailAddress().WithMessage(ValidationMessages.EmailInvalid);
        
        RuleFor(cur => cur.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.PasswordRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.PasswordTooLong)
            .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.PasswordTooWeak);
        
        RuleFor(cur => cur.ConfirmPassword).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.ConfirmPasswordRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.ConfirmPasswordTooLong)
            .Equal(cur => cur.Password).WithMessage(ValidationMessages.ConfirmPasswordMismatch)
            .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.ConfirmPasswordTooWeak);
    }
    
    #endregion
}
