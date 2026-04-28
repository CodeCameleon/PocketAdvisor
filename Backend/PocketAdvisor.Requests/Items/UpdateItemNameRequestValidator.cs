using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Items;

/// <summary>
/// The validator for the <see cref="UpdateItemNameRequest" /> model.
/// </summary>
public sealed class UpdateItemNameRequestValidator
    : AbstractValidator<UpdateItemNameRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateItemNameRequestValidator" /> class.
    /// </summary>
    public UpdateItemNameRequestValidator()
    {
        RuleFor(r => r.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.ItemNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.ItemNameTooLong);
    }
    
    #endregion
}
