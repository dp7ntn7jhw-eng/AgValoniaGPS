using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services.Gps;

public sealed class RearAxleGuidanceCommandService : IRearAxleGuidanceCommandService
{
    private const double ProportionalGainDegreesPerMeter = 8.0;
    private const double MaxSteerAngleDegrees = 25.0;

    public RearAxleGuidanceCommand ComputeCommand(RearAxleGuidanceDiagnostic diagnostic)
    {
        if (!diagnostic.RearGpsFixOk)
        {
            return Invalid("rear GPS fix not OK");
        }

        if (!diagnostic.RearGpsRecent)
        {
            return Invalid("rear GPS data not recent");
        }

        if (!diagnostic.ActiveTrackAvailable)
        {
            return Invalid("no active track");
        }

        if (!diagnostic.RearCrossTrackErrorMeters.HasValue)
        {
            return Invalid("rear cross-track error unavailable");
        }

        double rearXte = diagnostic.RearCrossTrackErrorMeters.Value;

        // Proportional-only diagnostic command.
        // Negative sign: steer back toward the guidance line.
        double targetSteerAngle = -ProportionalGainDegreesPerMeter * rearXte;
        targetSteerAngle = Clamp(targetSteerAngle, -MaxSteerAngleDegrees, MaxSteerAngleDegrees);

        return new RearAxleGuidanceCommand(
            IsCommandValid: true,
            RearCrossTrackErrorMeters: rearXte,
            TargetSteerAngleDegrees: targetSteerAngle,
            ReasonIfInvalid: string.Empty);
    }

    private static RearAxleGuidanceCommand Invalid(string reason)
    {
        return new RearAxleGuidanceCommand(
            IsCommandValid: false,
            RearCrossTrackErrorMeters: 0.0,
            TargetSteerAngleDegrees: 0.0,
            ReasonIfInvalid: reason);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
