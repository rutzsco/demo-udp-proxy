using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus; // Added for Service Bus

public class UdpProxyServer
{
    private readonly int _port;
    // private readonly ConcurrentQueue<DeviceMessage> _messageQueue; // Removed ConcurrentQueue
    private readonly ServiceBusSender _serviceBusSender; // Added ServiceBusSender
    private readonly MessageStats _stats;
    private UdpClient? _udpClient;
    private CancellationTokenSource _cancellationTokenSource = new();

    // Updated constructor to accept ServiceBusSender instead of ConcurrentQueue
    public UdpProxyServer(int port, ServiceBusSender serviceBusSender, MessageStats stats)
    {
        _port = port;
        _serviceBusSender = serviceBusSender;
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
                    // Updated to call ProcessMessageAsync as it's now async due to Service Bus
                    _ = Task.Run(async () => await ProcessMessageAsync(result.Buffer, result.RemoteEndPoint));
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

    // Renamed to ProcessMessageAsync and made async
    private async Task ProcessMessageAsync(byte[] buffer, IPEndPoint remoteEndPoint)
    {
        try
        {
            var messageJson = Encoding.UTF8.GetString(buffer);
            var deviceMessage = JsonSerializer.Deserialize<DeviceMessage>(messageJson);
            
            if (deviceMessage != null)
            {
                deviceMessage.SourceEndPoint = remoteEndPoint;
                deviceMessage.Timestamp = DateTime.Now;
                
                // Send message to Azure Service Bus
                var serviceBusMessage = new ServiceBusMessage(messageJson);
                await _serviceBusSender.SendMessageAsync(serviceBusMessage);
                
                _stats.IncrementMessage();
                
                Console.WriteLine($"Received and sent to Service Bus: message from {remoteEndPoint}: Device {deviceMessage.DeviceId}");
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
