using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration; // Added for Configuration
using Microsoft.Extensions.Configuration.Json; // Added for AddJsonFile

// Configuration
const int UDP_PORT = 8080;

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // Override with local settings if present
    .Build();

string serviceBusConnectionString = configuration["ServiceBusConnectionString"] ?? throw new InvalidOperationException("ServiceBusConnectionString not found in configuration.");
string serviceBusQueueName = configuration["ServiceBusQueueName"] ?? throw new InvalidOperationException("ServiceBusQueueName not found in configuration.");

var messageStats = new MessageStats();

Console.WriteLine($"UDP Proxy Server starting on port {UDP_PORT}...");

// Initialize Azure Service Bus
await using var client = new ServiceBusClient(serviceBusConnectionString);
await using var sender = client.CreateSender(serviceBusQueueName);

// Start the UDP server
var udpServer = new UdpProxyServer(UDP_PORT, sender, messageStats); // Updated constructor
var serverTask = Task.Run(() => udpServer.StartAsync());

// Start the monitoring task
var monitorTask = Task.Run(() => MonitorMessages(messageStats)); // Updated to pass only stats

// Wait for shutdown signal
Console.WriteLine("Press 'q' to quit the server...");

if (Console.IsInputRedirected || !Console.IsOutputRedirected && !Console.IsErrorRedirected)
{
    // No console available, use Ctrl+C (SIGINT) for shutdown
    var exitEvent = new ManualResetEventSlim(false);
    Console.CancelKeyPress += (sender, e) =>
    {
        Console.WriteLine("Shutting down server (Ctrl+C)...");
        udpServer.Stop();
        exitEvent.Set();
        e.Cancel = true;
    };
    exitEvent.Wait();
}
else
{
    // Console available, use 'q' to quit
    while (true)
    {
        var key = Console.ReadKey(true);
        if (key.KeyChar == 'q' || key.KeyChar == 'Q')
        {
            Console.WriteLine("Shutting down server...");
            udpServer.Stop();
            break;
        }
    }
}

await Task.WhenAny(serverTask, monitorTask);
Console.WriteLine("Server stopped.");

// Monitor and display message statistics
static async Task MonitorMessages(MessageStats stats) // Removed queue parameter
{
    while (true)
    {
        await Task.Delay(5000); // Update every 5 seconds
        
        Console.WriteLine($"\\n--- Message Statistics ---");
        Console.WriteLine($"Total Messages Received: {stats.TotalMessages}");
        Console.WriteLine($"Last Message Time: {stats.LastMessageTime:yyyy-MM-dd HH:mm:ss}");
    }
}
