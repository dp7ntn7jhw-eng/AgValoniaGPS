namespace AgValoniaGPS.Services.Interfaces;

public interface IDualGpsDiagnosticsService : IDisposable
{
    void Start();
    void Stop();
}
