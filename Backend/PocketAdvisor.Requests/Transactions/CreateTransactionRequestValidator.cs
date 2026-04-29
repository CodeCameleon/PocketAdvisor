using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Transactions;

/// <summary>
/// The validator for the <see cref="CreateTransactionRequest" /> model.
/// </summary>
public sealed class CreateTransactionRequestValidator
    : AbstractValidator<CreateTransactionRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTransactionRequestValidator" /> class.
    /// </summary>
    public CreateTransactionRequestValidator()
    {
        RuleFor(r => r.OccurredAt).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.TransactionOccurredAtRequired)
            .LessThanOrEqualTo(_ => DateTime.UtcNow).WithMessage(ValidationMessages.TransactionOccurredAtFuture);
        
        RuleFor(r => r.CategoryId)
            .NotNull().WithMessage(ValidationMessages.TransactionCategoryIdRequired);
        
        RuleFor(r => r).Cascade(CascadeMode.Stop)
            .Must(r =>
                r.FromAccountId.HasValue || r.ToAccountId.HasValue
            ).WithMessage(ValidationMessages.TransactionEitherAccountRequired)
            .Must(r =>
                !(r.FromAccountId.HasValue && r.ToAccountId.HasValue) || r.FromAccountId != r.ToAccountId
            ).WithMessage(ValidationMessages.TransactionAccountsSame);
        
        RuleFor(r => r.Items).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.TransactionItemsRequired)
            .NotEmpty().WithMessage(ValidationMessages.TransactionItemsRequired);
        
        RuleForEach(r => r.Items).SetValidator(new CreateTransactionItemRequestValidator());
    }
    
    #endregion
}
