using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services.Gps;

public sealed class RearAxleCommandPublisherService : IRearAxleCommandPublisherService, IDisposable
{
    private const string TargetIp = "127.0.0.1";
    private const int TargetPort = 12000;

    private readonly UdpClient _udpClient = new();

    public void Publish(RearAxleGuidanceDiagnostic diagnostic, RearAxleGuidanceCommand command)
    {
        string message =
            "REAR_CMD," +
            $"valid={(command.IsCommandValid ? 1 : 0)}," +
            $"xte={command.RearCrossTrackErrorMeters.ToString("F3", CultureInfo.InvariantCulture)}," +
            $"steer={command.TargetSteerAngleDegrees.ToString("F3", CultureInfo.InvariantCulture)}," +
            $"rearFix={(diagnostic.RearGpsFixOk ? 1 : 0)}," +
            $"rearRecent={(diagnostic.RearGpsRecent ? 1 : 0)}," +
            $"track={Sanitize(diagnostic.ActiveTrackName)}," +
            $"reason={Sanitize(command.ReasonIfInvalid)}";

        byte[] bytes = Encoding.ASCII.GetBytes(message);
        _udpClient.Send(bytes, bytes.Length, TargetIp, TargetPort);
    }

    private static string Sanitize(string value)
    {
        return value
            .Replace(",", "_", StringComparison.Ordinal)
            .Replace("\r", "", StringComparison.Ordinal)
            .Replace("\n", "", StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _udpClient.Dispose();
    }
}
