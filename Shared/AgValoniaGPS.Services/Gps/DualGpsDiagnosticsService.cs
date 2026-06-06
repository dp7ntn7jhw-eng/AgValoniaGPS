using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services.Gps;

public sealed class DualGpsDiagnosticsService : IDualGpsDiagnosticsService
{
    private readonly IGpsService _frontGpsService;
    private readonly IRearGpsReceiverService _rearGpsService;
    private readonly IGuidanceGeometryService _geometryService;
    private readonly System.Timers.Timer _timer;

    public DualGpsDiagnosticsService(
        IGpsService frontGpsService,
        IRearGpsReceiverService rearGpsService,
        IGuidanceGeometryService geometryService)
    {
        _frontGpsService = frontGpsService;
        _rearGpsService = rearGpsService;
        _geometryService = geometryService;

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    public void Start()
    {
        _timer.Start();
        Console.WriteLine("Dual GPS diagnostics service started.");
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var front = _frontGpsService.CurrentData;
        var rear = _rearGpsService.LastState;

        bool frontFix = _frontGpsService.IsConnected && _frontGpsService.IsGpsDataOk();
        bool rearFix = _rearGpsService.HasFix;
        bool rearRecent = _rearGpsService.IsRecent;

        double distanceMeters = _geometryService.DistanceMetersLatLon(
            front.CurrentPosition.Latitude,
            front.CurrentPosition.Longitude,
            rear.Latitude,
            rear.Longitude);

        Console.WriteLine(
            $"Dual GPS: frontFix={frontFix}, rearFix={rearFix}, rearRecent={rearRecent}, " +
            $"dist={distanceMeters:F2}m, " +
            $"front=({front.CurrentPosition.Latitude:F8},{front.CurrentPosition.Longitude:F8}), " +
            $"rear=({rear.Latitude:F8},{rear.Longitude:F8})");
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
