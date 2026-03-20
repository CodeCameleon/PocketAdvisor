using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PocketAdvisor.Enums;
using PocketAdvisor.Enums.Extensions;

namespace PocketAdvisor.Entities.ValueObjects;

/// <summary>
/// A value object representing a quantity, which consists of a value and a unit.
/// </summary>
public sealed class Quantity
    : IComparable<Quantity>, IEquatable<Quantity>
{
    #region Constants
    
    /// <summary>
    /// When the comparison is not possible because the units belong to different categories.
    /// </summary>
    private const string CategoryMismatchMessage = "Units must belong to the same category.";
    
    /// <summary>
    /// The template used to stringify the object.
    /// </summary>
    private const string FormatTemplate = "{0} {1}";
    
    /// <summary>
    /// When the comparison is not possible because one of the units belongs to the uncategorized category.
    /// </summary>
    private const string UncategorizedMessage = "Uncategorized units cannot be compared.";
    
    /// <summary>
    /// The number of decimal places used when normalizing values for comparison and hashing.
    /// </summary>
    private const int Precision = 6;
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Quantity" /> class,
    /// with the given value and unit.
    /// </summary>
    /// <param name="value">The value of the quantity.</param>
    /// <param name="unit">The unit of the quantity.</param>
    public Quantity(decimal value, EUnit unit)
    {
        Value = value;
        Unit = unit;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The value of the quantity.
    /// </summary>
    public decimal Value { get; }
    
    /// <summary>
    /// The unit of the quantity.
    /// </summary>
    public EUnit Unit { get; }
    
    #endregion
    
    #region Operators
    
    /// <summary>
    /// Checks if the first quantity is less than the second quantity.
    /// </summary>
    /// <param name="left">The first quantity.</param>
    /// <param name="right">The second quantity.</param>
    /// <returns>
    /// <see langword="true" />, if the first quantity is less than
    /// the second quantity, <see langword="false" /> otherwise.
    /// </returns>
    public static bool operator <(Quantity? left, Quantity? right)
    {
        if (left is null)
        {
            return right is not null;
        }
        
        return left.CompareTo(right) < 0;
    }
    
    /// <summary>
    /// Checks if the first quantity is greater than the second quantity.
    /// </summary>
    /// <param name="left">The first quantity.</param>
    /// <param name="right">The second quantity.</param>
    /// <returns>
    /// <see langword="true" />, if the first quantity is greater than
    /// the second quantity, <see langword="false" /> otherwise.
    /// </returns>
    public static bool operator >(Quantity? left, Quantity? right)
    {
        if (left is null)
        {
            return false;
        }
        
        return left.CompareTo(right) > 0;
    }
    
    /// <summary>
    /// Checks if the first quantity is less than or equal to the second quantity.
    /// </summary>
    /// <param name="left">The first quantity.</param>
    /// <param name="right">The second quantity.</param>
    /// <returns>
    /// <see langword="true" />, if the first quantity is less than or
    /// equal to the second quantity, <see langword="false" /> otherwise.
    /// </returns>
    public static bool operator <=(Quantity? left, Quantity? right) => !(left > right);
    
    /// <summary>
    /// Checks if the first quantity is greater than or equal to the second quantity.
    /// </summary>
    /// <param name="left">The first quantity.</param>
    /// <param name="right">The second quantity.</param>
    /// <returns>
    /// <see langword="true" />, if the first quantity is greater than or
    /// equal to the second quantity, <see langword="false" /> otherwise.
    /// </returns>
    public static bool operator >=(Quantity? left, Quantity? right) => !(left < right);
    
    /// <summary>
    /// Checks if two quantities are equal.
    /// </summary>
    /// <param name="left">The first quantity.</param>
    /// <param name="right">The second quantity.</param>
    /// <returns><see langword="true" />, if they are equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(Quantity? left, Quantity? right)
    {
        if (left is null)
        {
            return right is null;
        }
        
        return left.Equals(right);
    }
    
    /// <summary>
    /// Checks if two quantities are not equal.
    /// </summary>
    /// <param name="left">The first quantity.</param>
    /// <param name="right">The second quantity.</param>
    /// <returns><see langword="true" />, if they are not equal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(Quantity? left, Quantity? right) => !(left == right);
    
    #endregion
    
    #region CompareTo
    
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// If the units belong to different categories or<br />
    /// if one of the units belongs to the uncategorized category.
    /// </exception>
    public int CompareTo(Quantity? other)
    {
        if (other is null)
        {
            return 1;
        }
        
        if (Unit == other.Unit)
        {
            return Value.CompareTo(other.Value);
        }
        
        EUnitCategory category = Unit.GetUnitCategory();
        EUnitCategory otherCategory = other.Unit.GetUnitCategory();
        
        if (category == EUnitCategory.Uncategorized || otherCategory == EUnitCategory.Uncategorized)
        {
            throw new InvalidOperationException(UncategorizedMessage);
        }
        
        if (category != otherCategory)
        {
            throw new InvalidOperationException(CategoryMismatchMessage);
        }
        
        decimal factor = Unit.GetUnitFactor(other.Unit);
        return (Value * factor).CompareTo(other.Value);
    }
    
    #endregion
    
    #region Equals
    
    /// <inheritdoc />
    public bool Equals(Quantity? other)
    {
        if (other is null)
        {
            return false;
        }
        
        if (Unit == other.Unit)
        {
            return Value == other.Value;
        }
        
        EUnitCategory category = Unit.GetUnitCategory();
        EUnitCategory otherCategory = other.Unit.GetUnitCategory();
        
        if (category == EUnitCategory.Uncategorized ||
            otherCategory == EUnitCategory.Uncategorized ||
            category != otherCategory)
        {
            return false;
        }
        
        decimal factor = Unit.GetUnitFactor(other.Unit);
        decimal normalized = Math.Round(Value * factor, Precision);
        decimal otherValue = Math.Round(other.Value, Precision);
        
        return normalized == otherValue;
    }
    
    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Quantity other && Equals(other);
    }
    
    #endregion
    
    #region GetHashCode
    
    /// <inheritdoc />
    public override int GetHashCode()
    {
        EUnitCategory category = Unit.GetUnitCategory();
        
        if (category == EUnitCategory.Uncategorized)
        {
            return HashCode.Combine(Value, Unit);
        }
        
        EUnit baseUnit = category.GetBaseUnit();
        
        if (Unit == baseUnit)
        {
            return HashCode.Combine(Value, Unit);
        }
        
        decimal factor = Unit.GetUnitFactor(baseUnit);
        decimal normalized = Math.Round(Value * factor, Precision);
        return HashCode.Combine(normalized, baseUnit);
    }
    
    #endregion
    
    #region ToString
    
    /// <inheritdoc />
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, FormatTemplate, Value, Unit);
    }
    
    #endregion
}
