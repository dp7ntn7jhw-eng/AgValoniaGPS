using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services.Gps;

public sealed class RearAxleStatusReceiverService : IRearAxleStatusReceiverService
{
    private const int StatusPort = 12001;
    private const double RecentThresholdSeconds = 2.0;

    private readonly byte[] _receiveBuffer = new byte[2048];

    private Socket? _socket;
    private EndPoint _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
    private bool _isRunning;
    private DateTime _lastConsoleLogUtc = DateTime.MinValue;

    public RearAxleStatus? LastStatus { get; private set; }
    public DateTime LastUpdateUtc { get; private set; }

    public bool IsRecent
    {
        get
        {
            if (LastUpdateUtc == default)
            {
                return false;
            }

            return (DateTime.UtcNow - LastUpdateUtc).TotalSeconds <= RecentThresholdSeconds;
        }
    }

    public event EventHandler<RearAxleStatus>? StatusReceived;

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _socket.Bind(new IPEndPoint(IPAddress.Any, StatusPort));

        _isRunning = true;

        Console.WriteLine($"Rear axle status receiver listening on UDP {StatusPort}");

        BeginReceive();
    }

    public void Stop()
    {
        _isRunning = false;

        try
        {
            _socket?.Close();
            _socket?.Dispose();
        }
        catch
        {
            // Ignore shutdown errors.
        }

        _socket = null;
    }

    private void BeginReceive()
    {
        if (!_isRunning || _socket == null)
        {
            return;
        }

        try
        {
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            _socket.BeginReceiveFrom(
                _receiveBuffer,
                0,
                _receiveBuffer.Length,
                SocketFlags.None,
                ref _remoteEndPoint,
                ReceiveCallback,
                null);
        }
        catch (ObjectDisposedException)
        {
            // Socket closed during shutdown.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Rear status BeginReceive error: {ex.Message}");
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        if (!_isRunning || _socket == null)
        {
            return;
        }

        int bytesReceived;

        try
        {
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            bytesReceived = _socket.EndReceiveFrom(ar, ref remote);
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Rear status receive error: {ex.Message}");
            BeginReceive();
            return;
        }

        try
        {
            ProcessPacket(bytesReceived);
        }
        finally
        {
            BeginReceive();
        }
    }

    private void ProcessPacket(int bytesReceived)
    {
        if (bytesReceived <= 0)
        {
            return;
        }

        string message = Encoding.ASCII.GetString(_receiveBuffer, 0, bytesReceived).Trim();

        var status = ParseStatus(message);
        if (status == null)
        {
            Console.WriteLine($"Rear status rejected: {message}");
            return;
        }

        LastStatus = status;
        LastUpdateUtc = DateTime.UtcNow;

        StatusReceived?.Invoke(this, status);

        LogStatusThrottled(status);
    }

    private RearAxleStatus? ParseStatus(string message)
    {
        const string crcMarker = ",crc=";

        int crcIndex = message.LastIndexOf(crcMarker, StringComparison.Ordinal);
        if (crcIndex < 0)
        {
            return null;
        }

        string payload = message[..crcIndex];
        string crcText = message[(crcIndex + crcMarker.Length)..].Trim();

        if (!ushort.TryParse(crcText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort expectedCrc))
        {
            return null;
        }

        ushort actualCrc = ComputeChecksum(payload);
        bool crcOk = actualCrc == expectedCrc;

        if (!crcOk)
        {
            return new RearAxleStatus(
                Seq: 0,
                CommandValid: false,
                Accepted: false,
                XteMm: 0,
                SteerCdeg: 0,
                TargetAdc: 0,
                FeedbackAdc: 0,
                Pwm: 0,
                Enabled: false,
                Timeout: false,
                CrcOk: false,
                RawMessage: message,
                ReceivedUtc: DateTime.UtcNow);
        }

        string[] parts = payload.Split(',');
        if (parts.Length == 0 || parts[0] != "REAR_STATUS")
        {
            return null;
        }

        var fields = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string part in parts.Skip(1))
        {
            int eq = part.IndexOf('=');
            if (eq <= 0)
            {
                return null;
            }

            fields[part[..eq]] = part[(eq + 1)..];
        }

        if (!TryGetUShort(fields, "seq", out ushort seq) ||
            !TryGetBool01(fields, "cmd_valid", out bool cmdValid) ||
            !TryGetBool01(fields, "accepted", out bool accepted) ||
            !TryGetInt(fields, "xte_mm", out int xteMm) ||
            !TryGetInt(fields, "steer_cdeg", out int steerCdeg) ||
            !TryGetInt(fields, "target_adc", out int targetAdc) ||
            !TryGetInt(fields, "feedback_adc", out int feedbackAdc) ||
            !TryGetInt(fields, "pwm", out int pwm) ||
            !TryGetBool01(fields, "enabled", out bool enabled) ||
            !TryGetBool01(fields, "timeout", out bool timeout))
        {
            return null;
        }

        return new RearAxleStatus(
            seq,
            cmdValid,
            accepted,
            xteMm,
            steerCdeg,
            targetAdc,
            feedbackAdc,
            pwm,
            enabled,
            timeout,
            true,
            message,
            DateTime.UtcNow);
    }

    private static bool TryGetInt(Dictionary<string, string> fields, string key, out int value)
    {
        value = 0;
        return fields.TryGetValue(key, out string? text) &&
               int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetUShort(Dictionary<string, string> fields, string key, out ushort value)
    {
        value = 0;
        return fields.TryGetValue(key, out string? text) &&
               ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetBool01(Dictionary<string, string> fields, string key, out bool value)
    {
        value = false;

        if (!fields.TryGetValue(key, out string? text))
        {
            return false;
        }

        if (text == "1")
        {
            value = true;
            return true;
        }

        if (text == "0")
        {
            value = false;
            return true;
        }

        return false;
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

    private void LogStatusThrottled(RearAxleStatus status)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastConsoleLogUtc).TotalSeconds < 1.0)
        {
            return;
        }

        _lastConsoleLogUtc = now;

        Console.WriteLine(
            $"Rear STATUS: crc={status.CrcOk}, cmdValid={status.CommandValid}, " +
            $"accepted={status.Accepted}, xte={status.XteMm}mm, " +
            $"steer={status.SteerCdeg / 100.0:F2}deg, " +
            $"targetAdc={status.TargetAdc}, feedbackAdc={status.FeedbackAdc}, " +
            $"pwm={status.Pwm}, enabled={status.Enabled}, timeout={status.Timeout}");
    }

    public void Dispose()
    {
        Stop();
    }
}
