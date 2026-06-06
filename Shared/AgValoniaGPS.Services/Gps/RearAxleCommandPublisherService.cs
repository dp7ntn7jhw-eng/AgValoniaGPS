using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services.Gps;

public sealed class RearAxleCommandPublisherService : IRearAxleCommandPublisherService, IDisposable
{
    private const string TargetIp = "192.168.1.62";
    private const int TargetPort = 12000;

    private readonly UdpClient _udpClient = new();
    private ushort _sequence;

    public void Publish(RearAxleGuidanceDiagnostic diagnostic, RearAxleGuidanceCommand command)
    {
        ushort sequence = _sequence++;
        long timestampMs = Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;

        int valid = command.IsCommandValid ? 1 : 0;
        int xteMillimeters = (int)Math.Round(command.RearCrossTrackErrorMeters * 1000.0);
        int steerCentiDegrees = (int)Math.Round(command.TargetSteerAngleDegrees * 100.0);
        int flags = BuildFlags(diagnostic, command);

        string payloadWithoutChecksum =
            "REARV1," +
            $"seq={sequence}," +
            $"ms={timestampMs}," +
            $"valid={valid}," +
            $"xte_mm={xteMillimeters}," +
            $"steer_cdeg={steerCentiDegrees}," +
            $"flags={flags}," +
            $"track={Sanitize(diagnostic.ActiveTrackName)}," +
            $"reason={Sanitize(command.ReasonIfInvalid)}";

        ushort checksum = ComputeChecksum(payloadWithoutChecksum);

        string message =
            payloadWithoutChecksum +
            $",crc={checksum:X4}";

        byte[] bytes = Encoding.ASCII.GetBytes(message);
        _udpClient.Send(bytes, bytes.Length, TargetIp, TargetPort);
    }

    private static int BuildFlags(RearAxleGuidanceDiagnostic diagnostic, RearAxleGuidanceCommand command)
    {
        int flags = 0;

        if (diagnostic.RearGpsFixOk)
        {
            flags |= 1 << 0;
        }

        if (diagnostic.RearGpsRecent)
        {
            flags |= 1 << 1;
        }

        if (diagnostic.ActiveTrackAvailable)
        {
            flags |= 1 << 2;
        }

        if (diagnostic.IsValid)
        {
            flags |= 1 << 3;
        }

        if (command.IsCommandValid)
        {
            flags |= 1 << 4;
        }

        return flags;
    }

    private static ushort ComputeChecksum(string payload)
    {
        ushort checksum = 0;

        foreach (char c in payload)
        {
            checksum = unchecked((ushort)((checksum + (byte)c) & 0xFFFF));
        }

        return checksum;
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
