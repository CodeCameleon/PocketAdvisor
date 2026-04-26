using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Categories;

/// <summary>
/// The validator for the <see cref="CreateCategoryRequest" /> model.
/// </summary>
public sealed class CreateCategoryRequestValidator
    : AbstractValidator<CreateCategoryRequest>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCategoryRequestValidator" /> class.
    /// </summary>
    public CreateCategoryRequestValidator()
    {
        RuleFor(ccr => ccr.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.CategoryNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.CategoryNameTooLong);
    }
    
    #endregion
}
