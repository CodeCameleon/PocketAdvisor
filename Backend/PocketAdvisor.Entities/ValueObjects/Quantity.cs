using System.Diagnostics.CodeAnalysis;
using PocketAdvisor.Enums;
using PocketAdvisor.Enums.Extensions;

namespace PocketAdvisor.Entities.ValueObjects;

/// <summary>
/// A value object representing a quantity, which consists of a value and a unit.
/// </summary>
public readonly struct Quantity
    : IEquatable<Quantity>
{
    #region Constants
    
    /// <summary>
    /// The template used to stringify the object.
    /// </summary>
    private const string FormatTemplate = "{0} {1}";
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Quantity" /> struct,
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
    /// Checks if two quantities are equal.
    /// </summary>
    /// <param name="left">The first quantity.</param>
    /// <param name="right">The second quantity.</param>
    /// <returns><see langword="true" />, if they are equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(Quantity left, Quantity right) => left.Equals(right);
    
    /// <summary>
    /// Checks if two quantities are not equal.
    /// </summary>
    /// <param name="left">The first quantity.</param>
    /// <param name="right">The second quantity.</param>
    /// <returns><see langword="true" />, if they are not equal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(Quantity left, Quantity right) => !left.Equals(right);
    
    #endregion
    
    #region Equals
    
    /// <inheritdoc />
    public bool Equals(Quantity other)
    {
        if (Unit == other.Unit)
        {
            return Value == other.Value;
        }
        
        EUnitCategory category = Unit.GetUnitCategory();
        EUnitCategory otherCategory = other.Unit.GetUnitCategory();
        
        if (category == EUnitCategory.Uncategorized || otherCategory == EUnitCategory.Uncategorized)
        {
            return false;
        }
        
        if (category != otherCategory)
        {
            return false;
        }
        
        decimal factor = Unit.GetUnitFactor(other.Unit);
        
        return Value * factor == other.Value;
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
        return HashCode.Combine(Value, Unit);
    }
    
    #endregion
    
    #region ToString
    
    /// <inheritdoc />
    public override string ToString()
    {
        return string.Format(FormatTemplate, Value, Unit);
    }
    
    #endregion
}
