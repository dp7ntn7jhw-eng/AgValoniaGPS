namespace AgValoniaGPS.Services.Gps;

public sealed record RearAxleRuntimeState(
    DateTime UpdatedUtc,
    bool RearGpsFixOk,
    bool RearGpsRecent,
    bool ActiveTrackAvailable,
    string ActiveTrackName,
    double? RearCrossTrackErrorMeters,
    bool RearDiagnosticValid,
    bool RearCommandValid,
    double RearSteerCommandDegrees,
    bool Stm32StatusAvailable,
    bool Stm32Recent,
    bool Stm32Accepted,
    int Stm32TargetAdc,
    int Stm32FeedbackAdc,
    int Stm32Pwm,
    bool Stm32Timeout,
    bool OverallReady,
    string Reason);
