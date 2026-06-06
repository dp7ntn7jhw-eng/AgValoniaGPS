using AgValoniaGPS.Services.Gps;

namespace AgValoniaGPS.Services.Interfaces;

public interface IRearAxleGuidanceCommandService
{
    RearAxleGuidanceCommand ComputeCommand(RearAxleGuidanceDiagnostic diagnostic);
}
