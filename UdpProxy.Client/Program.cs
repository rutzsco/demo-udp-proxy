using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

// Configuration
//const string SERVER_HOST = "20.237.5.127";
const string SERVER_HOST = "localhost"; // Change to your server's IP or hostname
const int SERVER_PORT = 8080;

Console.WriteLine("UDP Proxy Client - Device Simulator");
Console.WriteLine("=====================================");

// Generate a unique device ID for this client instance
var deviceId = $"Device_{Environment.MachineName}_{DateTime.Now:yyyyMMdd_HHmmss}";
Console.WriteLine($"Device ID: {deviceId}");

var client = new UdpDeviceClient(SERVER_HOST, SERVER_PORT, deviceId);

Console.WriteLine("\nOptions:");
Console.WriteLine("1. Send single message");
Console.WriteLine("2. Start continuous simulation");
Console.WriteLine("3. Send test batch");
Console.WriteLine("q. Quit");

while (true)
{
    Console.Write("\nEnter your choice: ");
    var input = Console.ReadLine();

    switch (input?.ToLower())
    {
        case "1":
            await SendSingleMessage(client);
            break;
        case "2":
            await StartContinuousSimulation(client);
            break;
        case "3":
            await SendTestBatch(client);
            break;
        case "q":
            Console.WriteLine("Exiting...");
            client.Dispose();
            return;
        default:
            Console.WriteLine("Invalid option. Please try again.");
            break;
    }
}

static async Task SendSingleMessage(UdpDeviceClient client)
{
    Console.Write("Enter message data: ");
    var data = Console.ReadLine() ?? "Test message";
    
    await client.SendMessageAsync(data);
    Console.WriteLine("Message sent!");
}

static async Task StartContinuousSimulation(UdpDeviceClient client)
{
    Console.WriteLine("Starting continuous simulation... Press any key to stop.");
    
    var random = new Random();
    var sensorTypes = new[] { "Temperature", "Humidity", "Pressure", "Motion" };
    
    var simulationTask = Task.Run(async () =>
    {
        while (!Console.KeyAvailable)
        {
            var sensorType = sensorTypes[random.Next(sensorTypes.Length)];
            var value = random.Next(1, 100);
            var data = $"{sensorType}: {value}";
            
            await client.SendMessageAsync(data);
            Console.WriteLine($"Sent: {data}");
            
            await Task.Delay(random.Next(1000, 5000)); // Random delay between 1-5 seconds
        }
    });
    
    await simulationTask;
    Console.ReadKey(); // Consume the key that stopped the simulation
    Console.WriteLine("\nSimulation stopped.");
}

static async Task SendTestBatch(UdpDeviceClient client)
{
    Console.WriteLine("Sending test batch of 10 messages...");
    
    for (int i = 1; i <= 10; i++)
    {
        var data = $"Test batch message {i} - {DateTime.Now:HH:mm:ss.fff}";
        await client.SendMessageAsync(data);
        Console.WriteLine($"Sent message {i}/10");
        await Task.Delay(500); // Small delay between messages
    }
    
    Console.WriteLine("Test batch completed!");
}

// Device message model (must match server-side model)
public class DeviceMessage
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Data { get; set; } = string.Empty;
}

// UDP Device Client
public class UdpDeviceClient : IDisposable
{
    private readonly string _serverHost;
    private readonly int _serverPort;
    private readonly string _deviceId;
    private readonly UdpClient _udpClient;

    public UdpDeviceClient(string serverHost, int serverPort, string deviceId)
    {
        _serverHost = serverHost;
        _serverPort = serverPort;
        _deviceId = deviceId;
        _udpClient = new UdpClient();
    }

    public async Task SendMessageAsync(string data)
    {
        try
        {
            var message = new DeviceMessage
            {
                DeviceId = _deviceId,
                Timestamp = DateTime.Now,
                Data = data
            };

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _udpClient.SendAsync(bytes, bytes.Length, _serverHost, _serverPort);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _udpClient?.Dispose();
    }
}
