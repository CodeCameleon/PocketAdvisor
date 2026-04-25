using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="ForgotPasswordRequest" /> model.
/// </summary>
public sealed class ForgotPasswordRequestValidator
    : AbstractValidator<ForgotPasswordRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordRequestValidator" /> class.
    /// </summary>
    public ForgotPasswordRequestValidator()
    {
        RuleFor(fpr => fpr.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.EmailTooLong)
            .EmailAddress().WithMessage(ValidationMessages.EmailInvalid);
    }
}
