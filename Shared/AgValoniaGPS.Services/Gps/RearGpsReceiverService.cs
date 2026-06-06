using System.Net;
using System.Net.Sockets;
using AgValoniaGPS.Models;
using AgValoniaGPS.Services.Interfaces;

namespace AgValoniaGPS.Services.Gps;

public sealed class RearGpsReceiverService : IRearGpsReceiverService
{
    private const int RearGpsPort = 10000;
    private const double RecentThresholdSeconds = 2.0;

    private readonly byte[] _receiveBuffer = new byte[4096];

    private Socket? _socket;
    private EndPoint _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
    private bool _isRunning;

    private VehicleState _lastState;
    private DateTime _lastConsoleLogUtc = DateTime.MinValue;

    public event EventHandler<VehicleState>? RearGpsUpdated;

    public VehicleState LastState => _lastState;

    public double Latitude => _lastState.Latitude;
    public double Longitude => _lastState.Longitude;
    public double Speed => _lastState.Speed;
    public double Heading => _lastState.Heading;
    public int FixQuality => _lastState.FixQuality;
    public int Satellites => _lastState.Satellites;

    public DateTime LastUpdateUtc { get; private set; }

    public double AgeSeconds
    {
        get
        {
            if (LastUpdateUtc == default)
            {
                return double.PositiveInfinity;
            }

            return (DateTime.UtcNow - LastUpdateUtc).TotalSeconds;
        }
    }

    public bool HasFix => FixQuality > 0 && Satellites > 0;
    public bool IsRecent => AgeSeconds <= RecentThresholdSeconds;

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _socket.Bind(new IPEndPoint(IPAddress.Any, RearGpsPort));

        _isRunning = true;

        Console.WriteLine($"Rear GPS receiver listening on UDP {RearGpsPort}");

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
            Console.WriteLine($"Rear GPS BeginReceive error: {ex.Message}");
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
            Console.WriteLine($"Rear GPS receive error: {ex.Message}");
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

        if (_receiveBuffer[0] != (byte)'$')
        {
            return;
        }

        var packet = new byte[bytesReceived];
        Buffer.BlockCopy(_receiveBuffer, 0, packet, 0, bytesReceived);

        var state = _lastState;
        NmeaParserServiceFast.ParseIntoState(packet, ref state);

        _lastState = state;
        LastUpdateUtc = DateTime.UtcNow;

        RearGpsUpdated?.Invoke(this, _lastState);

        LogRearGpsThrottled();
    }

    private void LogRearGpsThrottled()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastConsoleLogUtc).TotalSeconds < 5.0)
        {
            return;
        }

        _lastConsoleLogUtc = now;

        Console.WriteLine(
            $"Rear GPS: lat={Latitude:F8}, lon={Longitude:F8}, " +
            $"speed={Speed * 3.6:F1} km/h, heading={Heading:F1}, " +
            $"fix={FixQuality}, sats={Satellites}, " +
            $"recent={IsRecent}, age={AgeSeconds:F1}s");
    }

    public void Dispose()
    {
        Stop();
    }
}
