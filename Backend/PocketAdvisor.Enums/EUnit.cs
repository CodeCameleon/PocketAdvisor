namespace PocketAdvisor.Enums;

/// <summary>
/// The enumeration containing the possible units in the system.
/// </summary>
public enum EUnit
{
    #region Uncategorized
    
    /// <summary>
    /// The piece unit.
    /// </summary>
    Piece = 1,
    
    #endregion
    
    #region Length
    
    /// <summary>
    /// The millimeter length unit.
    /// </summary>
    Millimeter = 101,
    
    /// <summary>
    /// The centimeter length unit.
    /// </summary>
    Centimeter = 102,
    
    /// <summary>
    /// The meter length unit.
    /// </summary>
    Meter = 103,
    
    /// <summary>
    /// The kilometer length unit.
    /// </summary>
    Kilometer = 104,
    
    #endregion
    
    #region Mass
    
    /// <summary>
    /// The milligram mass unit.
    /// </summary>
    Milligram = 201,
    
    /// <summary>
    /// The gram mass unit.
    /// </summary>
    Gram = 202,
    
    /// <summary>
    /// The kilogram mass unit.
    /// </summary>
    Kilogram = 203,
    
    /// <summary>
    /// The tonne mass unit.
    /// </summary>
    Tonne = 204,
    
    #endregion
    
    #region Area
    
    /// <summary>
    /// The square meter area unit.
    /// </summary>
    SquareMeter = 301,
    
    /// <summary>
    /// The square kilometer area unit.
    /// </summary>
    SquareKilometer = 302,
    
    /// <summary>
    /// The acre area unit.
    /// </summary>
    Acre = 306,
    
    /// <summary>
    /// The hectare area unit.
    /// </summary>
    Hectare = 307,
    
    #endregion
    
    #region Volume
    
    /// <summary>
    /// The milliliter volume unit.
    /// </summary>
    Milliliter = 401,
    
    /// <summary>
    /// The liter volume unit.
    /// </summary>
    Liter = 402,
    
    /// <summary>
    /// The cubic meter volume unit.
    /// </summary>
    CubicMeter = 403,
    
    #endregion
    
    #region Time
    
    /// <summary>
    /// The second time unit.
    /// </summary>
    Second = 501,
    
    /// <summary>
    /// The minute time unit.
    /// </summary>
    Minute = 502,
    
    /// <summary>
    /// The hour time unit.
    /// </summary>
    Hour = 503,
    
    /// <summary>
    /// The day time unit.
    /// </summary>
    Day = 504,
    
    /// <summary>
    /// The month time unit.
    /// </summary>
    Month = 505,
    
    /// <summary>
    /// The year time unit.
    /// </summary>
    Year = 506,
    
    #endregion
    
    #region Energy
    
    /// <summary>
    /// The joule energy unit.
    /// </summary>
    Joule = 601,
    
    /// <summary>
    /// The kilojoule energy unit.
    /// </summary>
    Kilojoule = 602,
    
    /// <summary>
    /// The kilowatt-hour energy unit.
    /// </summary>
    KilowattHour = 603,
    
    #endregion
    
    #region DataSize
    
    /// <summary>
    /// The byte data size unit.
    /// </summary>
    Byte = 701,
    
    /// <summary>
    /// The kilobyte data size unit.
    /// </summary>
    Kilobyte = 702,
    
    /// <summary>
    /// The megabyte data size unit.
    /// </summary>
    Megabyte = 703,
    
    /// <summary>
    /// The gigabyte data size unit.
    /// </summary>
    Gigabyte = 704,
    
    /// <summary>
    /// The terabyte data size unit.
    /// </summary>
    Terabyte = 705
    
    #endregion
}
