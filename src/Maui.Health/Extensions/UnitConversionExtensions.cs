using Maui.Health.Enums;
using Maui.Health.Models;
using Maui.Health.Models.Metrics;
using UnitsNet;

namespace Maui.Health.Extensions;

/// <summary>
/// Unit conversion extensions for read DTOs and aggregated results.
/// Read values are always in metric units (kg, cm, kcal).
/// Use these methods to convert to other supported units.
/// </summary>
public static class UnitConversionExtensions
{
    /// <summary>
    /// Converts the weight value to the specified unit.
    /// The <see cref="WeightDto.Value"/> is always in kilograms.
    /// </summary>
    public static double In(this WeightDto dto, MassUnit unit)
    {
        return unit switch
        {
            MassUnit.Kilogram => dto.Value,
            MassUnit.Pound => Mass.FromKilograms(dto.Value).Pounds,
            MassUnit.Gram => Mass.FromKilograms(dto.Value).Grams,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    /// <summary>
    /// Converts the height value to the specified unit.
    /// The <see cref="HeightDto.Value"/> is always in centimeters.
    /// </summary>
    public static double In(this HeightDto dto, LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Centimeter => dto.Value,
            LengthUnit.Meter => Length.FromCentimeters(dto.Value).Meters,
            LengthUnit.Inch => Length.FromCentimeters(dto.Value).Inches,
            LengthUnit.Foot => Length.FromCentimeters(dto.Value).Feet,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    /// <summary>
    /// Converts the energy value to the specified unit.
    /// The <see cref="ActiveCaloriesBurnedDto.Energy"/> is always in kilocalories.
    /// </summary>
    public static double In(this ActiveCaloriesBurnedDto dto, EnergyUnit unit)
    {
        return unit switch
        {
            EnergyUnit.Kilocalorie => dto.Energy,
            EnergyUnit.Kilojoule => Energy.FromKilocalories(dto.Energy).Kilojoules,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    /// <summary>
    /// Converts the aggregated weight value to the specified unit.
    /// Weight aggregations are always in kilograms.
    /// </summary>
    public static double In(this AggregatedResult result, MassUnit unit)
    {
        return unit switch
        {
            MassUnit.Kilogram => result.Value,
            MassUnit.Pound => Mass.FromKilograms(result.Value).Pounds,
            MassUnit.Gram => Mass.FromKilograms(result.Value).Grams,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    /// <summary>
    /// Converts the aggregated height value to the specified unit.
    /// Height aggregations are always in centimeters.
    /// </summary>
    public static double In(this AggregatedResult result, LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Centimeter => result.Value,
            LengthUnit.Meter => Length.FromCentimeters(result.Value).Meters,
            LengthUnit.Inch => Length.FromCentimeters(result.Value).Inches,
            LengthUnit.Foot => Length.FromCentimeters(result.Value).Feet,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    /// <summary>
    /// Converts the aggregated energy value to the specified unit.
    /// Energy aggregations are always in kilocalories.
    /// </summary>
    public static double In(this AggregatedResult result, EnergyUnit unit)
    {
        return unit switch
        {
            EnergyUnit.Kilocalorie => result.Value,
            EnergyUnit.Kilojoule => Energy.FromKilocalories(result.Value).Kilojoules,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }
}
