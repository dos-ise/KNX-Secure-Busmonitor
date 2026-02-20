namespace KnxMonitor.Models;

public enum MessageType
{
    Write,
    Read,
    Response,
    Ack
}

public class KnxTelegram
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string SourceAddress { get; set; } = string.Empty;   // e.g. "1.1.1"
    public string GroupAddress { get; set; } = string.Empty;    // e.g. "0/0/1"
    public string GroupName { get; set; } = string.Empty;       // e.g. "Switch Living Room"
    public MessageType Type { get; set; }
    public string DptType { get; set; } = string.Empty;         // e.g. "DPT-1"
    public string DecodedValue { get; set; } = string.Empty;    // e.g. "On"
    public string RawBytes { get; set; } = string.Empty;        // e.g. "0x01"
    public int RssiPercent { get; set; } = 100;
    public bool IsError { get; set; }
}
