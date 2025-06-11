using System.Net;

namespace UdpProxy.Portal.Client.Models;

public class DeviceMessage
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Data { get; set; } = string.Empty;
    public IPEndPoint? SourceEndPoint { get; set; }
}
