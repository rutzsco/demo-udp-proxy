# UDP Proxy Portal Setup Guide

This portal provides real-time monitoring of device messages by reading from Azure Service Bus and displaying them through Azure SignalR.

## Prerequisites

1. **Azure Service Bus**: A Service Bus namespace with a queue configured
2. **Azure SignalR Service**: A SignalR service instance
3. **.NET 8.0 SDK**

## Configuration

### 1. Update Connection Strings

Update the following files with your Azure connection strings:

#### `appsettings.json` and `appsettings.Development.json`:
```json
{
  "ServiceBusConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=your-policy;SharedAccessKey=your-key",
  "ServiceBusQueueName": "your-queue-name",
  "ConnectionStrings": {
    "AzureSignalR": "Endpoint=https://your-signalr-service.service.signalr.net;AccessKey=your-access-key;Version=1.0;"
  }
}
```

### 2. Azure Service Bus Setup

1. Create a Service Bus namespace in Azure
2. Create a queue (e.g., "device-messages")
3. Create a Shared Access Policy with Send and Listen permissions
4. Copy the connection string

### 3. Azure SignalR Setup

1. Create a SignalR Service in Azure
2. Choose "Default" service mode
3. Copy the connection string from the "Keys" section

## Features

### Real-time Dashboard
- **Total Messages**: Count of all received messages
- **Active Devices**: Number of unique devices that sent messages in the last 5 minutes
- **Connection Status**: SignalR connection status
- **Last Message Time**: Timestamp of the most recent message

### Message Table
- Real-time display of incoming device messages
- Shows timestamp, device ID, data, and source endpoint
- Automatically scrolls to show latest messages
- Keeps last 1000 messages in memory to prevent memory issues
- Clear button to reset message history

## Architecture

```
UDP Client → UDP Server → Azure Service Bus → Portal Background Service → SignalR Hub → Web UI
```

1. **UDP Server** receives messages and sends them to Azure Service Bus
2. **Portal Background Service** (`ServiceBusListenerService`) reads from Service Bus
3. **SignalR Hub** broadcasts messages to connected web clients
4. **Web UI** displays real-time updates

## Running the Portal

### Development
```bash
cd UdpProxy.Portal/UdpProxy.Portal
dotnet run
```

The portal will be available at:
- HTTP: http://localhost:5245
- HTTPS: https://localhost:7060

### Production
```bash
dotnet publish -c Release
# Deploy the published files to your web server
```

## Background Service

The `ServiceBusListenerService` runs as a hosted background service that:
- Connects to Azure Service Bus
- Processes messages from the configured queue
- Deserializes device messages
- Broadcasts to all connected SignalR clients
- Handles errors and message acknowledgments

## SignalR Hub

The `DeviceMessageHub` provides:
- Real-time message broadcasting
- Group management for targeted messages
- Connection management

## Client-Side Features

The Blazor WebAssembly client:
- Establishes SignalR connection on startup
- Listens for `ReceiveDeviceMessage` events
- Updates UI in real-time
- Manages message history and statistics
- Provides responsive Bootstrap UI

## Troubleshooting

### Common Issues

1. **SignalR Connection Fails**
   - Check Azure SignalR connection string
   - Ensure CORS is configured if hosting on different domains

2. **No Messages Appearing**
   - Verify Service Bus connection string and queue name
   - Check if UDP Server is sending messages to Service Bus
   - Review application logs for Service Bus processing errors

3. **Build Errors**
   - Ensure all NuGet packages are restored: `dotnet restore`
   - Check .NET 8.0 SDK is installed

### Logs

The application logs Service Bus message processing:
- Message received from Service Bus
- SignalR broadcast success
- Any processing errors

## Security Considerations

- Store connection strings in Azure Key Vault for production
- Use Managed Identity where possible
- Configure CORS appropriately for your domain
- Consider authentication for the web portal in production environments

## Performance

- Message history is limited to 1000 messages in browser memory
- Service Bus processor uses single concurrent call by default
- SignalR broadcasts to all connected clients (consider groups for scaling)
