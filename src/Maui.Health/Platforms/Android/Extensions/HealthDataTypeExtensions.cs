using AndroidX.Health.Connect.Client.Records;
using Kotlin.Jvm;
using Maui.Health.Enums;

namespace Maui.Health.Platforms.Android.Extensions;

internal static class HealthDataTypeExtensions
{
    internal static Kotlin.Reflect.IKClass ToKotlinClass(this HealthDataType healthDataType)
    {
        var type = healthDataType switch
        {
            HealthDataType.ActiveCaloriesBurned => typeof(ActiveCaloriesBurnedRecord),
            HealthDataType.BasalBodyTemperature => typeof(BasalBodyTemperatureRecord),
            HealthDataType.BasalMetabolicRate => typeof(BasalMetabolicRateRecord),
            HealthDataType.BloodGlucose => typeof(BloodGlucoseRecord),
            //HealthDataType.BloodPressure => typeof(BloodPressureRecord),
            HealthDataType.BodyFat => typeof(BodyFatRecord),
            HealthDataType.BodyTemperature => typeof(BodyTemperatureRecord),
            //HealthDataType.BodyWaterMass => typeof(BodyWaterMassRecord),
            //HealthDataType.BoneMass => typeof(BoneMassRecord),
            //HealthDataType.CervicalMucus => typeof(CervicalMucusRecord),
            //HealthDataType.CyclingPedalingCadence => typeof(CyclingPedalingCadenceRecord),
            //HealthDataType.Distance => typeof(DistanceRecord),
            //HealthDataType.ElevationGained => typeof(ElevationGainedRecord),
            HealthDataType.ExerciseSession => typeof(ExerciseSessionRecord),
            //HealthDataType.FloorsClimbed => typeof(FloorsClimbedRecord),
            HealthDataType.HeartRate => typeof(HeartRateRecord),
            HealthDataType.HeartRateVariabilityRmssd => typeof(HeartRateVariabilityRmssdRecord),
            HealthDataType.Height => typeof(HeightRecord),
            HealthDataType.Hydration => typeof(HydrationRecord),
            //HealthDataType.IntermenstrualBleeding => typeof(IntermenstrualBleedingRecord),
            HealthDataType.LeanBodyMass => typeof(LeanBodyMassRecord),
            //HealthDataType.MenstruationFlow => typeof(MenstruationFlowRecord),
            //HealthDataType.MenstruationPeriod => typeof(MenstruationPeriodRecord),
            //HealthDataType.Nutrition => typeof(NutritionRecord),
            //HealthDataType.OvulationTest => typeof(OvulationTestRecord),
            HealthDataType.OxygenSaturation => typeof(OxygenSaturationRecord),
            //HealthDataType.PlannedExercise => typeof(PlannedExerciseSessionRecord),
            //HealthDataType.Power => typeof(PowerRecord),
            HealthDataType.RespiratoryRate => typeof(RespiratoryRateRecord),
            HealthDataType.RestingHeartRate => typeof(RestingHeartRateRecord),
            //HealthDataType.SexualActivity => typeof(SexualActivityRecord),
            //HealthDataType.SkinTemperature => typeof(SkinTemperatureRecord),
            //HealthDataType.SleepSession => typeof(SleepSessionRecord),
            //HealthDataType.Speed => typeof(SpeedRecord),
            //HealthDataType.StepsCadence => typeof(StepsCadenceRecord),
            HealthDataType.Steps => typeof(StepsRecord),
            //HealthDataType.TotalCaloriesBurned => typeof(TotalCaloriesBurnedRecord),
            HealthDataType.Vo2Max => typeof(Vo2MaxRecord),
            HealthDataType.Weight => typeof(WeightRecord),
            //HealthDataType.WheelchairPushes => typeof(WheelchairPushesRecord),
            _ => throw new ArgumentOutOfRangeException(nameof(healthDataType), healthDataType, null)
        };

        return JvmClassMappingKt.GetKotlinClass(Java.Lang.Class.FromType(type));
    }
}
