using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="RefreshRequest" /> model.
/// </summary>
public sealed class RefreshRequestValidator
    : AbstractValidator<RefreshRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshRequestValidator" /> class.
    /// </summary>
    public RefreshRequestValidator()
    {
        RuleFor(rr => rr.RefreshToken).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.RefreshTokenRequired)
            .MinimumLength(44).WithMessage(ValidationMessages.RefreshTokenTooShort)
            .MaximumLength(44).WithMessage(ValidationMessages.RefreshTokenTooLong);
    }
}
