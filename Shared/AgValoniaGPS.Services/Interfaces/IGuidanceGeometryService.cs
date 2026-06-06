using AgValoniaGPS.Models.Base;

namespace AgValoniaGPS.Services.Interfaces;

public interface IGuidanceGeometryService
{
    double DistanceMeters(Vec2 a, Vec2 b);

    double DistanceMetersLatLon(double lat1, double lon1, double lat2, double lon2);

    double BearingRadians(Vec2 from, Vec2 to);

    double CrossTrackErrorMeters(Vec2 point, Vec2 lineA, Vec2 lineB);

    double CrossTrackErrorMetersLatLon(
        double pointLat,
        double pointLon,
        double lineALat,
        double lineALon,
        double lineBLat,
        double lineBLon);

    double AlongTrackDistanceMeters(Vec2 point, Vec2 lineA, Vec2 lineB);

    Vec2 ClosestPointOnLine(Vec2 point, Vec2 lineA, Vec2 lineB);
}
