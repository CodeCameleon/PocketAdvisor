using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Categories;

/// <summary>
/// The validator for the <see cref="UpdateCategoryNameRequest" /> model.
/// </summary>
public sealed class UpdateCategoryNameRequestValidator
    : AbstractValidator<UpdateCategoryNameRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryNameRequestValidator" /> class.
    /// </summary>
    public UpdateCategoryNameRequestValidator()
    {
        RuleFor(r => r.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.CategoryNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.CategoryNameTooLong);
    }
    
    #endregion
}
