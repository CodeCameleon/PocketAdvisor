using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="ResetPasswordRequest" /> model.
/// </summary>
public sealed class ResetPasswordRequestValidator
    : AbstractValidator<ResetPasswordRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordRequestValidator" /> class.
    /// </summary>
    public ResetPasswordRequestValidator()
    {
        RuleFor(rpr => rpr.Token).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.PasswordResetTokenRequired)
            .MinimumLength(44).WithMessage(ValidationMessages.PasswordResetTokenTooShort)
            .MaximumLength(44).WithMessage(ValidationMessages.PasswordResetTokenTooLong);
        
        RuleFor(rpr => rpr.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.PasswordRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.PasswordTooLong)
            .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.PasswordTooWeak);
        
        RuleFor(rpr => rpr.ConfirmPassword).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.ConfirmPasswordRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.ConfirmPasswordTooLong)
            .Equal(rpr => rpr.Password).WithMessage(ValidationMessages.ConfirmPasswordMismatch)
            .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.ConfirmPasswordTooWeak);
    }
    
    #endregion
}
