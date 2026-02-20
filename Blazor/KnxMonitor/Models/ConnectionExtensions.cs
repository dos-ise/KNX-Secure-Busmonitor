namespace KnxMonitor.Models;

public static class ConnectionExtensions
{
    public static string ToBadgeClass(this ConnectionState state) => state switch
    {
        ConnectionState.Connected    => "conn-badge live",
        ConnectionState.Connecting   => "conn-badge connecting",
        ConnectionState.Error        => "conn-badge offline",
        _                            => "conn-badge offline",
    };

    public static string ToLabel(this ConnectionState state) => state switch
    {
        ConnectionState.Connected    => "LIVE",
        ConnectionState.Connecting   => "CONNECTING",
        ConnectionState.Error        => "ERROR",
        _                            => "OFFLINE",
    };

    public static string ToCssClass(this ConnectionLogEntry.LogLevel level) => level switch
    {
        ConnectionLogEntry.LogLevel.Ok    => "ok",
        ConnectionLogEntry.LogLevel.Warn  => "warn",
        ConnectionLogEntry.LogLevel.Error => "err",
        _                                 => "info",
    };
}
