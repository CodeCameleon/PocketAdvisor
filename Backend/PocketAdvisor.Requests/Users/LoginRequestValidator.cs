using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="LoginRequest" /> model.
/// </summary>
public sealed class LoginRequestValidator
    : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginRequestValidator" /> class.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(lr => lr.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.EmailTooLong)
            .EmailAddress().WithMessage(ValidationMessages.EmailInvalid);
        
        RuleFor(lr => lr.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.PasswordRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.PasswordTooLong);
    }
}
