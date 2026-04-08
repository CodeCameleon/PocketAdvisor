using FluentResults;
using FluentValidation.Results;
using PocketAdvisor.Services.Constants;

namespace PocketAdvisor.Services.Extensions;

/// <summary>
/// The extension methods for the <see cref="ValidationFailure" /> class.
/// </summary>
internal static class ValidationFailureExtensions
{
    /// <summary>
    /// Converts the validation failures to returnable errors.
    /// </summary>
    /// <param name="validationFailures">The validation failure list to convert.</param>
    /// <returns>The ready to use error list.</returns>
    internal static IReadOnlyList<Error> ToErrorList(this IEnumerable<ValidationFailure> validationFailures)
    {
        return validationFailures.Select(failure => new Error(failure.ErrorMessage)
        {
            Metadata =
            {
                [ErrorMetadataKeys.PropertyName] = failure.PropertyName
            }
        }).ToList();
    }
}
