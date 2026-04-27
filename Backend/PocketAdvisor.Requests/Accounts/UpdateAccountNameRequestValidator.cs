using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Accounts;

/// <summary>
/// The validator for the <see cref="UpdateAccountNameRequest" /> model.
/// </summary>
public sealed class UpdateAccountNameRequestValidator
    : AbstractValidator<UpdateAccountNameRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAccountNameRequestValidator" /> class.
    /// </summary>
    public UpdateAccountNameRequestValidator()
    {
        RuleFor(r => r.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.AccountNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.AccountNameTooLong);
    }
    
    #endregion
}
