using System.Collections.Frozen;

namespace PocketAdvisor.Enums.Extensions;

/// <summary>
/// The extensions methods for the <see cref="EUnitCategory" /> enum.
/// </summary>
public static class EUnitCategoryExtensions
{
    #region Constants
    
    /// <summary>
    /// When the conversion is not possible because the units belong to different categories.
    /// </summary>
    private const string CategoryMissMatchMessage = "Units must belong to the same category.";
    
    /// <summary>
    /// When the conversion is not possible because one of the units belongs to the uncategorized category.
    /// </summary>
    private const string UncategorizedMessage = "Uncategorized units cannot be converted.";
    
    /// <summary>
    /// The dictionary containing the units of each category.
    /// </summary>
    private static readonly FrozenDictionary<EUnitCategory, FrozenSet<EUnit>> Categories;
    
    /// <summary>
    /// The dictionary containing the conversion factors of each unit.
    /// </summary>
    private static readonly FrozenDictionary<EUnit, decimal> Factors;
    
    /// <summary>
    /// The dictionary containing the category of each unit.
    /// </summary>
    private static readonly FrozenDictionary<EUnit, EUnitCategory> UnitCategories;
    
    #endregion
    
    #region Constuctor
    
    /// <summary>
    /// Initializes the only instance of the <see cref="EUnitCategoryExtensions" /> class.
    /// </summary>
    static EUnitCategoryExtensions()
    {
        const int range = 100;
        Dictionary<EUnitCategory, FrozenSet<EUnit>> categories = [];
        
        foreach (EUnitCategory unitCategory in Enum.GetValues<EUnitCategory>())
        {
            int min = ((int)unitCategory - 1) * range;
            
            IEnumerable<EUnit> units = Enum.GetValues<EUnit>().Where(u =>
                (int)u >= min &&
                (int)u < min + range
            );
            
            categories.Add(unitCategory, units.ToFrozenSet());
        }
        
        Categories = categories.ToFrozenDictionary();
        
        UnitCategories = Categories.SelectMany(pair =>
            pair.Value.Select(unit => (unit, pair.Key))
        ).ToFrozenDictionary(x => x.unit, x => x.Key);
        
        Dictionary<EUnit, decimal> factors = new()
        {
            { EUnit.Millimeter, 0.001m },
            { EUnit.Centimeter, 0.01m },
            { EUnit.Meter, 1m },
            { EUnit.Kilometer, 1000m },
            
            { EUnit.Milligram, 1e-6m },
            { EUnit.Gram, 0.001m },
            { EUnit.Kilogram, 1m },
            { EUnit.Tonne, 1000m },
            
            { EUnit.SquareMeter, 1m },
            { EUnit.SquareKilometer, 1e6m },
            { EUnit.Acre, 4046.85642m },
            { EUnit.Hectare, 1e4m },
            
            { EUnit.Milliliter, 0.001m },
            { EUnit.Liter, 1m },
            { EUnit.CubicMeter, 1000m },
            
            { EUnit.Second, 1m },
            { EUnit.Minute, 60m },
            { EUnit.Hour, 3600m },
            { EUnit.Day, 86400m },
            { EUnit.Month, 2630016m }, // Average month (30.44 days)
            { EUnit.Year, 31557600m }, // Average year (365.25 days)
            
            { EUnit.Joule, 1m },
            { EUnit.Kilojoule, 1000m },
            { EUnit.KilowattHour, 3600000m },
            
            { EUnit.Byte, 9.536743164E-7m },
            { EUnit.Kilobyte, 9.765625e-4m },
            { EUnit.Megabyte, 1m },
            { EUnit.Gigabyte, 1024m },
            { EUnit.Terabyte, 1048576m },
        };
        
        Factors = factors.ToFrozenDictionary();
    }
    
    #endregion
    
    #region GetCategoryUnitList
    
    /// <summary>
    /// Gets the list of units that belong to the category.
    /// </summary>
    /// <param name="unitCategory">The unit category.</param>
    /// <returns>The list of units that belong to the category.</returns>
    public static List<EUnit> GetCategoryUnitList(this EUnitCategory unitCategory)
    {
        return Categories[unitCategory].ToList();
    }
    
    #endregion
    
    #region GetUnitCategory
    
    /// <summary>
    /// Gets the category of the unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The category of the unit.</returns>
    public static EUnitCategory GetUnitCategory(this EUnit unit)
    {
        return UnitCategories[unit];
    }
    
    #endregion
    
    #region GetUnitFactor
    
    /// <summary>
    /// Gets the factor needed to convert the base unit to the target unit.
    /// </summary>
    /// <param name="baseUnit">The base unit to convert from.</param>
    /// <param name="targetUnit">The target unit to convert to.</param>
    /// <returns>The factor of the unit conversion.</returns>
    /// <exception cref="ArgumentException">
    /// If the units belong to different categories or<br />
    /// if one of the units belongs to the uncategorized category.
    /// </exception>
    public static decimal GetUnitFactor(this EUnit baseUnit, EUnit targetUnit)
    {
        EUnitCategory baseCategory = baseUnit.GetUnitCategory();
        EUnitCategory targetCategory = targetUnit.GetUnitCategory();
        
        if (baseCategory == EUnitCategory.Uncategorized || targetCategory == EUnitCategory.Uncategorized)
        {
            throw new ArgumentException(UncategorizedMessage);
        }
        
        if (baseCategory != targetCategory)
        {
            throw new ArgumentException(CategoryMissMatchMessage);
        }
        
        return Factors[baseUnit] / Factors[targetUnit];
    }
    
    #endregion
}
