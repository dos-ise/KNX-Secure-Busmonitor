namespace KnxMonitor.Models;

public enum ConnectionMode { Tunneling, Routing }
public enum ConnectionState { Disconnected, Connecting, Connected, Error }

public class KnxInterface
{
    public string Name       { get; set; } = string.Empty;
    public string Model      { get; set; } = string.Empty;
    public string IpAddress  { get; set; } = string.Empty;
    public int    Port       { get; set; } = 3671;
    public ConnectionMode Mode   { get; set; } = ConnectionMode.Tunneling;
    public int    SignalLevel    { get; set; } = 4;   // 1–4
    public string MacAddress     { get; set; } = string.Empty;
}

public class ConnectionSettings
{
    public string         IpAddress      { get; set; } = string.Empty;
    public int            Port           { get; set; } = 3671;
    public ConnectionMode Mode           { get; set; } = ConnectionMode.Tunneling;
    public bool           AutoReconnect  { get; set; } = true;
    public bool           NatMode        { get; set; } = false;
}

public class ConnectionLogEntry
{
    public DateTime   Timestamp { get; set; } = DateTime.Now;
    public string     Message   { get; set; } = string.Empty;
    public LogLevel   Level     { get; set; } = LogLevel.Info;

    public enum LogLevel { Info, Ok, Warn, Error }
}
