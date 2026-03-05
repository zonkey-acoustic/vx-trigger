using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ShotTrigger.Services;

public class NetworkTriggerService : IDisposable
{
    private UdpClient? _udpClient;
    private bool _disposed;

    public bool IsEnabled { get; set; }
    public int Port { get; set; } = 0;
    public string? TargetHost { get; set; } = "127.0.0.1";

    public void SendTriggerPacket()
    {
        if (!IsEnabled || Port <= 0)
            return;

        SendPacket();
    }

    public void TestPacket(int port, string? host = null)
    {
        SendPacketToPort(port, host ?? "127.0.0.1");
    }

    private void SendPacket()
    {
        SendPacketToPort(Port, TargetHost ?? "127.0.0.1");
    }

    private void SendPacketToPort(int port, string host)
    {
        try
        {
            _udpClient?.Dispose();
            _udpClient = new UdpClient();

            var message = $"SHOTTRIGGER:{DateTime.UtcNow:O}";
            var data = Encoding.UTF8.GetBytes(message);

            var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            _udpClient.Send(data, data.Length, endpoint);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending network trigger: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _udpClient?.Dispose();
        _udpClient = null;
    }
}
