using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

// Configuration
const int UDP_PORT = 8080;

// In-memory message queue
var messageQueue = new ConcurrentQueue<DeviceMessage>();
var messageStats = new MessageStats();

Console.WriteLine($"UDP Proxy Server starting on port {UDP_PORT}...");

// Start the UDP server
var udpServer = new UdpProxyServer(UDP_PORT, messageQueue, messageStats);
var serverTask = Task.Run(() => udpServer.StartAsync());

// Start the monitoring task
var monitorTask = Task.Run(() => MonitorMessages(messageQueue, messageStats));

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
static async Task MonitorMessages(ConcurrentQueue<DeviceMessage> queue, MessageStats stats)
{
    while (true)
    {
        await Task.Delay(5000); // Update every 5 seconds
        
        Console.WriteLine($"\n--- Message Statistics ---");
        Console.WriteLine($"Total Messages Received: {stats.TotalMessages}");
        Console.WriteLine($"Messages in Queue: {queue.Count}");
        Console.WriteLine($"Last Message Time: {stats.LastMessageTime:yyyy-MM-dd HH:mm:ss}");
        
        // Display recent messages
        if (queue.Count > 0)
        {
            Console.WriteLine("\n--- Recent Messages ---");
            var tempQueue = new List<DeviceMessage>();
            var displayCount = Math.Min(5, queue.Count);
            
            for (int i = 0; i < displayCount; i++)
            {
                if (queue.TryDequeue(out var message))
                {
                    tempQueue.Add(message);
                    Console.WriteLine($"Device: {message.DeviceId}, Timestamp: {message.Timestamp:HH:mm:ss}, Data: {message.Data}");
                }
            }
            
            // Re-queue the messages (in a real scenario, you'd process them)
            foreach (var msg in tempQueue)
            {
                queue.Enqueue(msg);
            }
        }
    }
}
