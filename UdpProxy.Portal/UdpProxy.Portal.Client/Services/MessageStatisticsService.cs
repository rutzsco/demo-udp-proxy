using UdpProxy.Portal.Client.Models;

namespace UdpProxy.Portal.Client.Services;

public class MessageStatisticsService
{
    private readonly List<DeviceMessage> _messages = new();

    public void AddMessage(DeviceMessage message)
    {
        _messages.Add(message);
        
        // Keep only the last 1000 messages to prevent memory issues
        if (_messages.Count > 1000)
        {
            _messages.RemoveRange(0, _messages.Count - 1000);
        }
    }

    public void ClearMessages()
    {
        _messages.Clear();
    }

    public int TotalMessages => _messages.Count;

    public int GetActiveDevicesCount(TimeSpan timespan)
    {
        var cutoffTime = DateTime.Now - timespan;
        return _messages
            .Where(m => m.Timestamp > cutoffTime)
            .Select(m => m.DeviceId)
            .Distinct()
            .Count();
    }

    public DateTime? GetLastMessageTime()
    {
        return _messages.LastOrDefault()?.Timestamp;
    }

    public IEnumerable<DeviceMessage> GetRecentMessages(int count = 100)
    {
        return _messages.TakeLast(count).Reverse();
    }

    public Dictionary<string, int> GetDeviceMessageCounts()
    {
        return _messages
            .GroupBy(m => m.DeviceId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public Dictionary<string, DateTime> GetDeviceLastSeen()
    {
        return _messages
            .GroupBy(m => m.DeviceId)
            .ToDictionary(g => g.Key, g => g.Max(m => m.Timestamp));
    }

    public double GetMessagesPerMinute(TimeSpan timespan)
    {
        var cutoffTime = DateTime.Now - timespan;
        var recentMessages = _messages.Where(m => m.Timestamp > cutoffTime).Count();
        return recentMessages / timespan.TotalMinutes;
    }
}
