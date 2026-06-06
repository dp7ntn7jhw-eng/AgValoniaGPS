namespace AgValoniaGPS.Services.Interfaces;

public interface IRearGpsReceiverService : IDisposable
{
    void Start();
    void Stop();

    double Latitude { get; }
    double Longitude { get; }
    double Speed { get; }
    double Heading { get; }
    int FixQuality { get; }
    int Satellites { get; }
    DateTime LastUpdateUtc { get; }
}
