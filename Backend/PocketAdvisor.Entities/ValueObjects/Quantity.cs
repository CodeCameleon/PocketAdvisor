using PocketAdvisor.Enums;

namespace PocketAdvisor.Entities.ValueObjects;

/// <summary>
/// A value object representing a quantity, which consists of a value and a unit.
/// </summary>
public readonly struct Quantity
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
