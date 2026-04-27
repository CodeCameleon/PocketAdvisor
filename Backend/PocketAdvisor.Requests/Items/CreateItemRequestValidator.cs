using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Items;

/// <summary>
/// The validator for the <see cref="CreateItemRequest" /> model.
/// </summary>
public sealed class CreateItemRequestValidator
    : AbstractValidator<CreateItemRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateItemRequestValidator" /> class.
    /// </summary>
    public CreateItemRequestValidator()
    {
        RuleFor(cir => cir.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.ItemNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.ItemNameTooLong);
        
        RuleFor(cir => cir.UnitCategory).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ValidationMessages.ItemUnitCategoryRequired)
            .IsInEnum().WithMessage(ValidationMessages.ItemUnitCategoryInvalid);
    }
    
    #endregion
}
