using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services.Gps;

public sealed class DualGpsDiagnosticsService : IDualGpsDiagnosticsService
{
    private readonly IGpsService _frontGpsService;
    private readonly IRearGpsReceiverService _rearGpsService;
    private readonly System.Timers.Timer _timer;

    public DualGpsDiagnosticsService(
        IGpsService frontGpsService,
        IRearGpsReceiverService rearGpsService)
    {
        _frontGpsService = frontGpsService;
        _rearGpsService = rearGpsService;

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

        double distanceMeters = DistanceMeters(
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

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000.0;

        double phi1 = DegreesToRadians(lat1);
        double phi2 = DegreesToRadians(lat2);
        double deltaPhi = DegreesToRadians(lat2 - lat1);
        double deltaLambda = DegreesToRadians(lon2 - lon1);

        double a =
            Math.Sin(deltaPhi / 2.0) * Math.Sin(deltaPhi / 2.0) +
            Math.Cos(phi1) * Math.Cos(phi2) *
            Math.Sin(deltaLambda / 2.0) * Math.Sin(deltaLambda / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
