public class MessageStats
{
    private long _totalMessages;
    public long TotalMessages => _totalMessages;
    public DateTime LastMessageTime { get; set; }
    
    public void IncrementMessage()
    {
        Interlocked.Increment(ref _totalMessages);
        LastMessageTime = DateTime.Now;
    }
}
