using AgValoniaGPS.Models;

namespace AgValoniaGPS.Services.Interfaces;

public interface IRearGpsReceiverService : IDisposable
{
    void Start();
    void Stop();

    VehicleState LastState { get; }

    double Latitude { get; }
    double Longitude { get; }
    double Speed { get; }
    double Heading { get; }
    int FixQuality { get; }
    int Satellites { get; }

    DateTime LastUpdateUtc { get; }
    double AgeSeconds { get; }
    bool HasFix { get; }
    bool IsRecent { get; }

    event EventHandler<VehicleState>? RearGpsUpdated;
}
