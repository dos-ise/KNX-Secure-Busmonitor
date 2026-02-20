namespace KnxMonitor.Models;

public record BusStats
{
    public int TotalCount { get; init; }
    public int TelegramsPerSecond { get; init; }
    public int ErrorCount { get; init; }
    public int DeviceCount { get; init; }
    public bool IsConnected { get; init; }
    public List<int> ActivityHistory { get; init; } = new();
}
