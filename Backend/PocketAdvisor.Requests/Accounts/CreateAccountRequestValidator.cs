using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Accounts;

/// <summary>
/// The validator for the <see cref="CreateAccountRequest" /> model.
/// </summary>
public sealed class CreateAccountRequestValidator
    : AbstractValidator<CreateAccountRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAccountRequestValidator" /> class.
    /// </summary>
    public CreateAccountRequestValidator()
    {
        RuleFor(car => car.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.AccountNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.AccountNameTooLong);
        
        RuleFor(car => car.Balance).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.AccountBalanceRequired);
        
        RuleFor(car => car.CurrencyCode).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.AccountCurrencyCodeRequired)
            .IsInEnum().WithMessage(ValidationMessages.AccountCurrencyCodeInvalid);
    }
    
    #endregion
}
