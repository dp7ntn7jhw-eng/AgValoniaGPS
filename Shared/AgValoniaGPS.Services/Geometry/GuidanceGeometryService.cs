using AgValoniaGPS.Models.Base;
using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services.Geometry;

public sealed class GuidanceGeometryService : IGuidanceGeometryService
{
    public double DistanceMeters(Vec2 a, Vec2 b)
    {
        double dx = b.Easting - a.Easting;
        double dz = b.Northing - a.Northing;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    public double DistanceMetersLatLon(double lat1, double lon1, double lat2, double lon2)
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

    public double BearingRadians(Vec2 from, Vec2 to)
    {
        double dx = to.Easting - from.Easting;
        double dz = to.Northing - from.Northing;
        return Math.Atan2(dx, dz);
    }

    private static Vec2 ProjectLatLonToLocalMeters(
        double lat,
        double lon,
        double originLat,
        double originLon)
    {
        const double earthRadiusMeters = 6371000.0;

        double originLatRad = DegreesToRadians(originLat);
        double east = DegreesToRadians(lon - originLon) * earthRadiusMeters * Math.Cos(originLatRad);
        double north = DegreesToRadians(lat - originLat) * earthRadiusMeters;

        return new Vec2(east, north);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    public double CrossTrackErrorMeters(Vec2 point, Vec2 lineA, Vec2 lineB)
    {
        double dx = lineB.Easting - lineA.Easting;
        double dz = lineB.Northing - lineA.Northing;

        double length = Math.Sqrt(dx * dx + dz * dz);
        if (length < 0.000001)
        {
            return 0.0;
        }

        // Same sign convention as TrackGuidanceService:
        // positive and negative indicate opposite sides of the guidance line.
        return ((point.Easting - lineA.Easting) * dz - (point.Northing - lineA.Northing) * dx) / length;
    }

    public double CrossTrackErrorMetersLatLon(
        double pointLat,
        double pointLon,
        double lineALat,
        double lineALon,
        double lineBLat,
        double lineBLon)
    {
        var point = ProjectLatLonToLocalMeters(pointLat, pointLon, lineALat, lineALon);
        var lineA = new Vec2(0.0, 0.0);
        var lineB = ProjectLatLonToLocalMeters(lineBLat, lineBLon, lineALat, lineALon);

        return CrossTrackErrorMeters(point, lineA, lineB);
    }

    public double AlongTrackDistanceMeters(Vec2 point, Vec2 lineA, Vec2 lineB)
    {
        double dx = lineB.Easting - lineA.Easting;
        double dz = lineB.Northing - lineA.Northing;

        double length = Math.Sqrt(dx * dx + dz * dz);
        if (length < 0.000001)
        {
            return 0.0;
        }

        return ((point.Easting - lineA.Easting) * dx + (point.Northing - lineA.Northing) * dz) / length;
    }

    public Vec2 ClosestPointOnLine(Vec2 point, Vec2 lineA, Vec2 lineB)
    {
        double dx = lineB.Easting - lineA.Easting;
        double dz = lineB.Northing - lineA.Northing;

        double lengthSquared = dx * dx + dz * dz;
        if (lengthSquared < 0.000001)
        {
            return lineA;
        }

        double t = ((point.Easting - lineA.Easting) * dx + (point.Northing - lineA.Northing) * dz) / lengthSquared;

        return new Vec2(
            lineA.Easting + t * dx,
            lineA.Northing + t * dz);
    }
}
