using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using UdpProxy.Portal.Hubs;
using UdpProxy.Portal.Models;

namespace UdpProxy.Portal.Services;

public class ServiceBusListenerService : BackgroundService
{
    private readonly ILogger<ServiceBusListenerService> _logger;
    private readonly IHubContext<DeviceMessageHub> _hubContext;
    private readonly ServiceBusProcessor _processor;
    private readonly ServiceBusClient _client;

    public ServiceBusListenerService(
        ILogger<ServiceBusListenerService> logger,
        IHubContext<DeviceMessageHub> hubContext,
        IConfiguration configuration)
    {
        _logger = logger;
        _hubContext = hubContext;

        var connectionString = configuration["ServiceBusConnectionString"] 
            ?? throw new InvalidOperationException("ServiceBusConnectionString not found in configuration");
        var queueName = configuration["ServiceBusQueueName"] 
            ?? throw new InvalidOperationException("ServiceBusQueueName not found in configuration");

        _client = new ServiceBusClient(connectionString);
        _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service Bus Listener Service starting...");
        
        try
        {
            await _processor.StartProcessingAsync(stoppingToken);
            _logger.LogInformation("Service Bus Listener Service started successfully");
            
            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service Bus Listener Service is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service Bus Listener Service encountered an error");
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageBody = args.Message.Body.ToString();
            _logger.LogInformation("Received message: {MessageBody}", messageBody);

            // Deserialize the device message
            var deviceMessage = JsonSerializer.Deserialize<DeviceMessage>(messageBody);
            
            if (deviceMessage != null)
            {
                // Send to all connected SignalR clients
                await _hubContext.Clients.All.SendAsync("ReceiveDeviceMessage", deviceMessage);
                _logger.LogInformation("Sent message to SignalR clients for device: {DeviceId}", deviceMessage.DeviceId);
            }

            // Complete the message so it's removed from the queue
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Service Bus message");
            // Abandon the message so it can be retried
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processor error: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service Bus Listener Service is stopping...");
        
        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
        
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Service Bus Listener Service stopped");
    }
}
