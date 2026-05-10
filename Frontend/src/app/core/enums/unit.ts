export enum Unit {
  // Uncategorized
  Piece = 1,

  // Length
  Millimeter = 101,
  Centimeter = 102,
  Meter = 103,
  Kilometer = 104,

  // Mass
  Milligram = 201,
  Gram = 202,
  Kilogram = 203,
  Tonne = 204,

  // Area
  SquareMeter = 301,
  SquareKilometer = 302,
  Acre = 306,
  Hectare = 307,

  // Volume
  Milliliter = 401,
  Liter = 402,
  CubicMeter = 403,

  // Time
  Second = 501,
  Minute = 502,
  Hour = 503,
  Day = 504,
  Month = 505,
  Year = 506,

  // Energy
  Joule = 601,
  Kilojoule = 602,
  KilowattHour = 603,

  // DataSize
  Byte = 701,
  Kilobyte = 702,
  Megabyte = 703,
  Gigabyte = 704,
  Terabyte = 705,
}

export const UNIT_LABELS: Record<Unit, string> = {
  [Unit.Piece]: 'Piece',
  [Unit.Millimeter]: 'Millimeter',
  [Unit.Centimeter]: 'Centimeter',
  [Unit.Meter]: 'Meter',
  [Unit.Kilometer]: 'Kilometer',
  [Unit.Milligram]: 'Milligram',
  [Unit.Gram]: 'Gram',
  [Unit.Kilogram]: 'Kilogram',
  [Unit.Tonne]: 'Tonne',
  [Unit.SquareMeter]: 'Square Meter',
  [Unit.SquareKilometer]: 'Square Kilometer',
  [Unit.Acre]: 'Acre',
  [Unit.Hectare]: 'Hectare',
  [Unit.Milliliter]: 'Milliliter',
  [Unit.Liter]: 'Liter',
  [Unit.CubicMeter]: 'Cubic Meter',
  [Unit.Second]: 'Second',
  [Unit.Minute]: 'Minute',
  [Unit.Hour]: 'Hour',
  [Unit.Day]: 'Day',
  [Unit.Month]: 'Month',
  [Unit.Year]: 'Year',
  [Unit.Joule]: 'Joule',
  [Unit.Kilojoule]: 'Kilojoule',
  [Unit.KilowattHour]: 'Kilowatt-hour',
  [Unit.Byte]: 'Byte',
  [Unit.Kilobyte]: 'Kilobyte',
  [Unit.Megabyte]: 'Megabyte',
  [Unit.Gigabyte]: 'Gigabyte',
  [Unit.Terabyte]: 'Terabyte',
};
