using AgValoniaGPS.Services.Gps;

namespace AgValoniaGPS.Services.Interfaces;

public interface IRearAxleStatusReceiverService : IDisposable
{
    void Start();
    void Stop();

    RearAxleStatus? LastStatus { get; }
    DateTime LastUpdateUtc { get; }
    bool IsRecent { get; }

    event EventHandler<RearAxleStatus>? StatusReceived;
}
