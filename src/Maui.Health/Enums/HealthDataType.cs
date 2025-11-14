namespace Maui.Health.Enums;

//iOS: https://developer.apple.com/documentation/healthkit/hkquantitytypeidentifier
//Android: https://developer.android.com/health-and-fitness/guides/health-connect/plan/data-types#permissions
//Flutter: https://pub.dev/packages/health#data-types
public enum HealthDataType
{
    //extracted from Android 1.0.0-alpha10 and higher
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
}
