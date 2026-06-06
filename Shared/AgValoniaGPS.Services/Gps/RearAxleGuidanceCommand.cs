namespace AgValoniaGPS.Services.Gps;

public sealed record RearAxleGuidanceCommand(
    bool IsCommandValid,
    double RearCrossTrackErrorMeters,
    double TargetSteerAngleDegrees,
    string ReasonIfInvalid);
