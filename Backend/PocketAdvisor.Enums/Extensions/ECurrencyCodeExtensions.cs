namespace PocketAdvisor.Enums.Extensions;

/// <summary>
/// The extensions methods for the <see cref="ECurrencyCode" /> enum.
/// </summary>
public static class ECurrencyCodeExtensions
{
    /// <summary>
    /// Gets the standard ISO 4217 code of the currency code.
    /// </summary>
    /// <param name="currencyCode">The currency code.</param>
    /// <returns>The ISO 4217 code of the currency code.</returns>
    public static string GetIsoCode(this ECurrencyCode currencyCode)
    {
        return currencyCode.ToString().ToUpper();
    }
}
