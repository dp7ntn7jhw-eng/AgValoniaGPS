using AgValoniaGPS.Services.Interfaces;
using AgValoniaGPS.Models.Base;
using AgValoniaGPS.Models.State;

namespace AgValoniaGPS.Services.Gps;

public sealed class DualGpsDiagnosticsService : IDualGpsDiagnosticsService
{
    private readonly IGpsService _frontGpsService;
    private readonly IRearGpsReceiverService _rearGpsService;
    private readonly IGuidanceGeometryService _geometryService;
    private readonly IGpsPipelineService _gpsPipelineService;
    private readonly ApplicationState _appState;
    private readonly System.Timers.Timer _timer;

    public DualGpsDiagnosticsService(
        IGpsService frontGpsService,
        IRearGpsReceiverService rearGpsService,
        IGuidanceGeometryService geometryService,
        IGpsPipelineService gpsPipelineService,
        ApplicationState appState)
    {
        _frontGpsService = frontGpsService;
        _rearGpsService = rearGpsService;
        _geometryService = geometryService;
        _gpsPipelineService = gpsPipelineService;
        _appState = appState;

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

        double frontLat = front.CurrentPosition.Latitude;
        double frontLon = front.CurrentPosition.Longitude;
        double frontHeading = front.CurrentPosition.Heading;

        double distanceMeters = _geometryService.DistanceMetersLatLon(
            frontLat,
            frontLon,
            rear.Latitude,
            rear.Longitude);

        var lineB = OffsetLatLon(frontLat, frontLon, frontHeading, 100.0);

        double rearCrossTrackErrorMeters = _geometryService.CrossTrackErrorMetersLatLon(
            rear.Latitude,
            rear.Longitude,
            frontLat,
            frontLon,
            lineB.Latitude,
            lineB.Longitude);

        string rearTrackDiagnostic = BuildRearTrackDiagnostic(rear);

        Console.WriteLine(
            $"Dual GPS: frontFix={frontFix}, rearFix={rearFix}, rearRecent={rearRecent}, " +
            $"dist={distanceMeters:F2}m, rearXteTemp={rearCrossTrackErrorMeters:F2}m, " +
            $"{rearTrackDiagnostic}, " +
            $"front=({frontLat:F8},{frontLon:F8}), " +
            $"rear=({rear.Latitude:F8},{rear.Longitude:F8})");
    }

    private string BuildRearTrackDiagnostic(AgValoniaGPS.Models.VehicleState rear)
    {
        var track = _gpsPipelineService.CurrentActiveTrack;
        var localPlane = _appState.Field.LocalPlane;

        if (track == null)
        {
            return "rearXteTrack=n/a(track=null)";
        }

        if (track.Points.Count < 2)
        {
            return $"rearXteTrack=n/a(points={track.Points.Count})";
        }

        if (localPlane == null)
        {
            return "rearXteTrack=n/a(localPlane=null)";
        }

        var rearGeo = localPlane.ConvertWgs84ToGeoCoord(
            new AgValoniaGPS.Models.Wgs84(rear.Latitude, rear.Longitude));

        var rearPoint = new Vec2(rearGeo.Easting, rearGeo.Northing);
        var pointA = new Vec2(track.Points[0].Easting, track.Points[0].Northing);
        var pointB = new Vec2(track.Points[1].Easting, track.Points[1].Northing);

        double rearXteTrack = _geometryService.CrossTrackErrorMeters(
            rearPoint,
            pointA,
            pointB);

        return $"rearXteTrack={rearXteTrack:F2}m";
    }

    private static (double Latitude, double Longitude) OffsetLatLon(
        double latitude,
        double longitude,
        double headingDegrees,
        double distanceMeters)
    {
        const double earthRadiusMeters = 6371000.0;

        double headingRad = headingDegrees * Math.PI / 180.0;
        double latRad = latitude * Math.PI / 180.0;

        double northMeters = Math.Cos(headingRad) * distanceMeters;
        double eastMeters = Math.Sin(headingRad) * distanceMeters;

        double newLat = latitude + (northMeters / earthRadiusMeters) * 180.0 / Math.PI;
        double newLon = longitude + (eastMeters / (earthRadiusMeters * Math.Cos(latRad))) * 180.0 / Math.PI;

        return (newLat, newLon);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
