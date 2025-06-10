# UDP Proxy - Device to Cloud Messaging

This project implements a UDP proxy server that receives messages from IoT devices and queues them in memory for processing. It consists of a server component that listens for UDP messages and a client component that simulates device messages.

## Architecture

- **UdpProxy.Server**: Receives UDP messages from devices and queues them in memory
- **UdpProxy.Client**: Simulates IoT devices sending messages to the proxy server

## Features

### Server Features
- Listens on UDP port 8080 (configurable)
- Receives JSON-formatted device messages
- Queues messages in memory using thread-safe collections
- Displays real-time statistics and recent messages
- Supports graceful shutdown

### Client Features
- Simulates IoT device behavior
- Supports single message sending
- Continuous simulation mode with random sensor data
- Batch message testing
- Unique device ID generation

## Message Format

Messages are sent as JSON with the following structure:

```json
{
  "DeviceId": "Device_MachineName_20240610_143022",
  "Timestamp": "2024-06-10T14:30:22.123Z",
  "Data": "Temperature: 25.6°C"
}
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio Code or Visual Studio

### Building the Project

```bash
# Build the entire solution
dotnet build UdpProxy.Server.sln

# Or build individual projects
dotnet build UdpProxy.Server/UdpProxy.Server.csproj
dotnet build UdpProxy.Client/UdpProxy.Client.csproj
```

### Running the Server

```bash
cd UdpProxy.Server
dotnet run
```

The server will start listening on UDP port 8080 and display:
- Total messages received
- Current queue size
- Recent messages
- Real-time statistics every 5 seconds

Press 'q' to gracefully shutdown the server.

### Running the Client

```bash
cd UdpProxy.Client
dotnet run
```

The client provides several options:
1. **Send single message**: Enter custom message data
2. **Start continuous simulation**: Automatically sends random sensor data
3. **Send test batch**: Sends 10 test messages quickly
4. **Quit**: Exit the client

## Docker Support

The server includes Docker support for containerized deployment:

```bash
cd UdpProxy.Server
docker build -t udp-proxy-server .
docker run -p 8080:8080/udp udp-proxy-server
```

## Configuration

### Server Configuration
- **UDP_PORT**: Port number for UDP listener (default: 8080)
- **MAX_BUFFER_SIZE**: Maximum UDP packet size (default: 1024 bytes)

### Client Configuration
- **SERVER_HOST**: Target server hostname (default: localhost)
- **SERVER_PORT**: Target server port (default: 8080)

## Message Processing

The server implements an in-memory queue using `ConcurrentQueue<DeviceMessage>` for thread-safe operations. In a production environment, you would typically:

1. Process messages from the queue in a background service
2. Store messages in a persistent database
3. Forward messages to cloud services (Azure IoT Hub, AWS IoT Core, etc.)
4. Implement message routing based on device type or location

## Monitoring

The server provides real-time monitoring including:
- Message throughput statistics
- Queue depth monitoring
- Device activity tracking
- Error logging and diagnostics

## Development Notes

- Messages are processed asynchronously to maintain high throughput
- The queue is thread-safe and supports concurrent producers/consumers
- Error handling includes JSON deserialization failures and network errors
- Device IDs are auto-generated but can be customized

## Future Enhancements

- Persistent message storage (database integration)
- Message acknowledgment system
- Device authentication and authorization
- Message filtering and routing rules
- REST API for queue management
- Metrics export (Prometheus, etc.)
- Clustering support for high availability