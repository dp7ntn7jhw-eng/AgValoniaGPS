using AgValoniaGPS.Services.Gps;

namespace AgValoniaGPS.Services.Interfaces;

public interface IRearAxleCommandPublisherService
{
    void Publish(RearAxleGuidanceDiagnostic diagnostic, RearAxleGuidanceCommand command);
}
