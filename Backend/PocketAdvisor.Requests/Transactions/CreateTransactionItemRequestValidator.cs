using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Transactions;

/// <summary>
/// The validator for the <see cref="CreateTransactionItemRequest" /> model.
/// </summary>
public sealed class CreateTransactionItemRequestValidator
    : AbstractValidator<CreateTransactionItemRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTransactionItemRequestValidator" /> class.
    /// </summary>
    public CreateTransactionItemRequestValidator()
    {
        RuleFor(r => r.ItemId)
            .NotNull().WithMessage(ValidationMessages.TransactionItemIdRequired);
        
        RuleFor(r => r.TotalPrice).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.TransactionItemTotalPriceRequired)
            .GreaterThanOrEqualTo(0m).WithMessage(ValidationMessages.TransactionItemTotalPriceNegative);
        
        RuleFor(r => r.Amount).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.TransactionItemAmountRequired)
            .GreaterThan(0m).WithMessage(ValidationMessages.TransactionItemAmountNegative);
        
        RuleFor(r => r.Unit).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.TransactionItemUnitRequired)
            .IsInEnum().WithMessage(ValidationMessages.TransactionItemUnitInvalid);
    }
    
    #endregion
}
