using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="VerifyEmailRequest" /> model.
/// </summary>
public sealed class VerifyEmailRequestValidator
    : AbstractValidator<VerifyEmailRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyEmailRequestValidator" /> class.
    /// </summary>
    public VerifyEmailRequestValidator()
    {
        RuleFor(ver => ver.Token).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.EmailVerificationTokenRequired)
            .MinimumLength(44).WithMessage(ValidationMessages.EmailVerificationTokenTooShort)
            .MaximumLength(44).WithMessage(ValidationMessages.EmailVerificationTokenTooLong);
    }
}
