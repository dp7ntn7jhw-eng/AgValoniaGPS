namespace AgValoniaGPS.Services.Gps;

public sealed record RearAxleGuidanceDiagnostic(
    bool RearGpsFixOk,
    bool RearGpsRecent,
    bool ActiveTrackAvailable,
    string ActiveTrackName,
    int ActiveTrackPointCount,
    double RearDistanceToFrontMeters,
    double? RearTemporaryCrossTrackErrorMeters,
    double? RearCrossTrackErrorMeters,
    bool IsValid);
