namespace Maui.Health.Enums;

/// <summary>
/// Types of health data that can be read from or written to the health data store.
/// iOS: https://developer.apple.com/documentation/healthkit/hkquantitytypeidentifier
/// Android: https://developer.android.com/health-and-fitness/guides/health-connect/plan/data-types#permissions
/// Flutter: https://pub.dev/packages/health#data-types
/// </summary>
public enum HealthDataType
{
#pragma warning disable CS1591
    ActiveCaloriesBurned,
    BasalBodyTemperature,
    BasalMetabolicRate,
    BloodGlucose,
    BloodPressure, // Note: split to 2 types (diastolic, systolic) on iOS - handled in platform code
    BodyFat,
    BodyTemperature,
    //BodyWaterMass, //not on iOS
    //BoneMass, //not on iOS
    //CervicalMucus,//not on iOS
    //CyclingPedalingCadence,
    //Distance,
    //ElevationGained,
    ExerciseSession,
    //FloorsClimbed,
    HeartRate,
    HeartRateVariabilityRmssd,
    Height,
    Hydration,
    //IntermenstrualBleeding,
    LeanBodyMass,
    //MenstruationFlow,
    //MenstruationPeriod,
    //Nutrition,
    //OvulationTest,
    OxygenSaturation,
    //PlannedExercise, // couldnt find corrent using in AndroidX binding - probably old one
    //Power,
    RespiratoryRate,
    RestingHeartRate,
    //SexualActivity,
    //SkinTemperature, // couldnt find corrent using in AndroidX binding - probably old one
    //SleepSession,
    //Speed,
    //StepsCadence,
    Steps,
    //TotalCaloriesBurned,
    Vo2Max,
    Weight,
    //WheelchairPushes
#pragma warning restore CS1591
}
