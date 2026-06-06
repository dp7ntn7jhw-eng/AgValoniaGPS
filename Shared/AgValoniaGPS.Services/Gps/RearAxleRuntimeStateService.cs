using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services.Gps;

public sealed class RearAxleRuntimeStateService : IRearAxleRuntimeStateService
{
    private readonly object _lock = new();

    private RearAxleRuntimeState _current = new(
        UpdatedUtc: DateTime.MinValue,
        RearGpsFixOk: false,
        RearGpsRecent: false,
        ActiveTrackAvailable: false,
        ActiveTrackName: "n/a",
        RearCrossTrackErrorMeters: null,
        RearDiagnosticValid: false,
        RearCommandValid: false,
        RearSteerCommandDegrees: 0.0,
        Stm32StatusAvailable: false,
        Stm32Recent: false,
        Stm32Accepted: false,
        Stm32TargetAdc: 0,
        Stm32FeedbackAdc: 0,
        Stm32Pwm: 0,
        Stm32Timeout: false,
        OverallReady: false,
        Reason: "not initialized");

    public RearAxleRuntimeState Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    public event EventHandler<RearAxleRuntimeState>? StateUpdated;

    public void Update(
        RearAxleGuidanceDiagnostic diagnostic,
        RearAxleGuidanceCommand command,
        RearAxleStatus? status,
        bool statusRecent)
    {
        bool statusAvailable = status != null;

        bool overallReady =
            diagnostic.IsValid &&
            command.IsCommandValid &&
            statusAvailable &&
            statusRecent &&
            status!.Accepted &&
            !status.Timeout &&
            status.CrcOk;

        string reason = BuildReason(diagnostic, command, status, statusRecent);

        var next = new RearAxleRuntimeState(
            UpdatedUtc: DateTime.UtcNow,
            RearGpsFixOk: diagnostic.RearGpsFixOk,
            RearGpsRecent: diagnostic.RearGpsRecent,
            ActiveTrackAvailable: diagnostic.ActiveTrackAvailable,
            ActiveTrackName: diagnostic.ActiveTrackName,
            RearCrossTrackErrorMeters: diagnostic.RearCrossTrackErrorMeters,
            RearDiagnosticValid: diagnostic.IsValid,
            RearCommandValid: command.IsCommandValid,
            RearSteerCommandDegrees: command.TargetSteerAngleDegrees,
            Stm32StatusAvailable: statusAvailable,
            Stm32Recent: statusRecent,
            Stm32Accepted: status?.Accepted ?? false,
            Stm32TargetAdc: status?.TargetAdc ?? 0,
            Stm32FeedbackAdc: status?.FeedbackAdc ?? 0,
            Stm32Pwm: status?.Pwm ?? 0,
            Stm32Timeout: status?.Timeout ?? false,
            OverallReady: overallReady,
            Reason: reason);

        lock (_lock)
        {
            _current = next;
        }

        StateUpdated?.Invoke(this, next);
    }

    private static string BuildReason(
        RearAxleGuidanceDiagnostic diagnostic,
        RearAxleGuidanceCommand command,
        RearAxleStatus? status,
        bool statusRecent)
    {
        if (!diagnostic.RearGpsFixOk)
        {
            return "rear GPS fix not OK";
        }

        if (!diagnostic.RearGpsRecent)
        {
            return "rear GPS data not recent";
        }

        if (!diagnostic.ActiveTrackAvailable)
        {
            return "no active track";
        }

        if (!diagnostic.IsValid)
        {
            return "rear diagnostic invalid";
        }

        if (!command.IsCommandValid)
        {
            return command.ReasonIfInvalid;
        }

        if (status == null)
        {
            return "no STM32 status";
        }

        if (!status.CrcOk)
        {
            return "STM32 status CRC invalid";
        }

        if (!statusRecent)
        {
            return "STM32 status not recent";
        }

        if (status.Timeout)
        {
            return "STM32 timeout";
        }

        if (!status.Accepted)
        {
            return "STM32 rejected command";
        }

        return "ready";
    }
}
