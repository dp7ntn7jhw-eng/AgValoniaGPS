using AgValoniaGPS.Services.Gps;

namespace AgValoniaGPS.Services.Interfaces;

public interface IRearAxleRuntimeStateService
{
    RearAxleRuntimeState Current { get; }

    void Update(
        RearAxleGuidanceDiagnostic diagnostic,
        RearAxleGuidanceCommand command,
        RearAxleStatus? status,
        bool statusRecent);

    event EventHandler<RearAxleRuntimeState>? StateUpdated;
}
