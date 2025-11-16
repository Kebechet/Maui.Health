using HealthKit;
using Maui.Health.Enums;

namespace Maui.Health.Platforms.iOS.Extensions;

internal static class HealthDataTypeExtensions
{
    internal static HKQuantityTypeIdentifier ToHKQuantityTypeIdentifier(this HealthDataType healthDataType)
    {
        var typeIdentifier = healthDataType switch
        {
            HealthDataType.ActiveCaloriesBurned => HKQuantityTypeIdentifier.ActiveEnergyBurned,
            HealthDataType.BasalBodyTemperature => HKQuantityTypeIdentifier.BasalBodyTemperature,
            HealthDataType.BasalMetabolicRate => HKQuantityTypeIdentifier.BasalEnergyBurned,
            HealthDataType.BloodGlucose => HKQuantityTypeIdentifier.BloodGlucose,
            //HealthDataType.BloodPressure => HKQuantityTypeIdentifier.BloodPressureSystolic, // Also need Diastolic - handled separately
            HealthDataType.BodyFat => HKQuantityTypeIdentifier.BodyFatPercentage,
            HealthDataType.BodyTemperature => HKQuantityTypeIdentifier.BodyTemperature,
            //HealthDataType.BodyWaterMass => HKQuantityTypeIdentifier.wate,//not on iOS
            //HealthDataType.BoneMass => HKQuantityTypeIdentifier.bone,//not on iOS
            //HealthDataType.CervicalMucus => HKQuantityTypeIdentifier.cerica,
            //HealthDataType.CyclingPedalingCadence => HKQuantityTypeIdentifier.CyclingCadence,//only iOS17+
            //HealthDataType.Distance => HKQuantityTypeIdentifier.distance,//iOS has it split to several types
            //HealthDataType.ElevationGained => HKQuantityTypeIdentifier.ele,//not on iOS
            HealthDataType.ExerciseSession => HKQuantityTypeIdentifier.AppleExerciseTime,
            //HealthDataType.FloorsClimbed => HKQuantityTypeIdentifier.flo,//not on iOS
            HealthDataType.HeartRate => HKQuantityTypeIdentifier.HeartRate,
            HealthDataType.HeartRateVariabilityRmssd => HKQuantityTypeIdentifier.HeartRateVariabilitySdnn,
            HealthDataType.Height => HKQuantityTypeIdentifier.Height,
            HealthDataType.Hydration => HKQuantityTypeIdentifier.DietaryWater,
            //HealthDataType.IntermenstrualBleeding => HKQuantityTypeIdentifier.mens,//not on iOS
            HealthDataType.LeanBodyMass => HKQuantityTypeIdentifier.LeanBodyMass,
            //HealthDataType.MenstruationFlow => HKQuantityTypeIdentifier.mens,//not on iOS
            //HealthDataType.MenstruationPeriod => HKQuantityTypeIdentifier.,//not on iOS
            //HealthDataType.Nutrition => HKQuantityTypeIdentifier.nutri,//not on iOS
            //HealthDataType.OvulationTest => HKQuantityTypeIdentifier.ovu,//not on iOS
            HealthDataType.OxygenSaturation => HKQuantityTypeIdentifier.OxygenSaturation,
            //HealthDataType.PlannedExercise => HKQuantityTypeIdentifier.,
            //HealthDataType.Power => HKQuantityTypeIdentifier.powe,//ios has it split
            HealthDataType.RespiratoryRate => HKQuantityTypeIdentifier.RespiratoryRate,
            HealthDataType.RestingHeartRate => HKQuantityTypeIdentifier.RestingHeartRate,
            //HealthDataType.SexualActivity => HKQuantityTypeIdentifier.sex,//not on iOS
            //HealthDataType.SkinTemperature => HKQuantityTypeIdentifier.,//not on iOS
            //HealthDataType.SleepSession => HKQuantityTypeIdentifier.slee,//not on iOS
            //HealthDataType.Speed => HKQuantityTypeIdentifier.speed,//ios has it split
            //HealthDataType.StepsCadence => HKQuantityTypeIdentifier.cade,//not on iOS
            HealthDataType.Steps => HKQuantityTypeIdentifier.StepCount,
            //HealthDataType.TotalCaloriesBurned => HKQuantityTypeIdentifier.burn,//not on iOS
            HealthDataType.Vo2Max => HKQuantityTypeIdentifier.VO2Max,
            HealthDataType.Weight => HKQuantityTypeIdentifier.BodyMass,
            //HealthDataType.WheelchairPushes => HKQuantityTypeIdentifier.Dis,//not on iOS
            _ => throw new ArgumentOutOfRangeException(nameof(healthDataType), healthDataType, null)
        };

        return typeIdentifier;
    }
}
