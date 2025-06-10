using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public class UdpProxyServer
{
    private readonly int _port;
    private readonly ConcurrentQueue<DeviceMessage> _messageQueue;
    private readonly MessageStats _stats;
    private UdpClient? _udpClient;
    private CancellationTokenSource _cancellationTokenSource = new();

    public UdpProxyServer(int port, ConcurrentQueue<DeviceMessage> messageQueue, MessageStats stats)
    {
        _port = port;
        _messageQueue = messageQueue;
        _stats = stats;
    }

    public async Task StartAsync()
    {
        try
        {
            // Explicitly bind to IPAddress.Any for all network interfaces
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, _port));
            Console.WriteLine($"UDP Proxy Server listening on 0.0.0.0:{_port} (all interfaces)");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    _ = Task.Run(() => ProcessMessage(result.Buffer, result.RemoteEndPoint));
                }
                catch (ObjectDisposedException)
                {
                    // Server is being shut down
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving UDP message: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting UDP server: {ex.Message}");
        }
    }

    private void ProcessMessage(byte[] buffer, IPEndPoint remoteEndPoint)
    {
        try
        {
            var messageJson = Encoding.UTF8.GetString(buffer);
            var deviceMessage = JsonSerializer.Deserialize<DeviceMessage>(messageJson);
            
            if (deviceMessage != null)
            {
                deviceMessage.SourceEndPoint = remoteEndPoint;
                deviceMessage.Timestamp = DateTime.Now;
                
                _messageQueue.Enqueue(deviceMessage);
                _stats.IncrementMessage();
                
                Console.WriteLine($"Received message from {remoteEndPoint}: Device {deviceMessage.DeviceId}");
            }
            else
            {
                Console.WriteLine($"Failed to deserialize message from {remoteEndPoint}");
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Invalid JSON message from {remoteEndPoint}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message from {remoteEndPoint}: {ex.Message}");
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _udpClient?.Close();
        _udpClient?.Dispose();
    }
}
