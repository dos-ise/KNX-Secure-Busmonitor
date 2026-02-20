using KnxMonitor.Models;

namespace KnxMonitor.Abstractions;

/// <summary>
/// Abstraction over a physical KNX bus driver.
/// Implement this interface to swap between Knx.Falcon, knx.net,
/// or any other KNX IP stack without touching higher-level services.
///
/// Register in MauiProgram.cs, e.g.:
///   builder.Services.AddSingleton&lt;IKnxBusDriver, FalconBusDriver&gt;();
///   builder.Services.AddSingleton&lt;IKnxBusDriver, KnxNetBusDriver&gt;();
/// </summary>
public interface IKnxBusDriver : IAsyncDisposable
{
    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>Current connection state of the driver.</summary>
    ConnectionState State { get; }

    /// <summary>True when the driver is connected and ready to send/receive.</summary>
    bool IsConnected { get; }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired on every incoming KNX telegram from the bus.
    /// Always raised on a background thread — marshal to UI thread as needed.
    /// </summary>
    event Action<KnxTelegram> TelegramReceived;

    /// <summary>
    /// Fired when the connection state changes (e.g. connected, dropped, reconnecting).
    /// </summary>
    event Action<ConnectionState> StateChanged;

    // ── Discovery ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Broadcast a KNXnet/IP Search Request on the local network (UDP 3671)
    /// and stream discovered interfaces via <paramref name="onFound"/>.
    /// Completes when the scan timeout expires or <paramref name="ct"/> is cancelled.
    /// </summary>
    Task DiscoverAsync(Action<KnxInterface> onFound, CancellationToken ct = default);

    // ── Connection ────────────────────────────────────────────────────────────

    /// <summary>
    /// Connect to a KNX IP interface using the supplied settings.
    /// Throws <see cref="KnxConnectionException"/> on failure.
    /// </summary>
    Task ConnectAsync(ConnectionSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Gracefully disconnect from the bus and release all resources.
    /// Safe to call even when already disconnected.
    /// </summary>
    Task DisconnectAsync();

    // ── Sending ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Send a GroupValue_Write to the given group address.
    /// </summary>
    /// <param name="groupAddress">KNX group address, e.g. "0/0/1"</param>
    /// <param name="value">DPT-encoded payload bytes</param>
    Task WriteAsync(string groupAddress, byte[] value, CancellationToken ct = default);

    /// <summary>
    /// Send a GroupValue_Read to the given group address.
    /// The response will arrive asynchronously via <see cref="TelegramReceived"/>.
    /// </summary>
    Task ReadAsync(string groupAddress, CancellationToken ct = default);
}

/// <summary>
/// Thrown by <see cref="IKnxBusDriver.ConnectAsync"/> when the connection attempt fails.
/// </summary>
public class KnxConnectionException(string message, Exception? inner = null)
    : Exception(message, inner);