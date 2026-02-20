using KnxMonitor.Abstractions;
using KnxMonitor.Models;
using static KnxMonitor.Models.ConnectionLogEntry;

namespace KnxMonitor.Services;

/// <summary>
/// Manages KNX IP interface discovery and connection lifecycle.
/// The concrete bus protocol is fully delegated to <see cref="IKnxBusDriver"/>,
/// which is injected via IoC — swap between SimulatedBusDriver, FalconBusDriver,
/// or any other implementation without touching this class.
/// </summary>
public class ConnectionService : IAsyncDisposable
{
    // ── Events ────────────────────────────────────────────────────────────────
    public event Action? StateChanged;

    // ── Public State ──────────────────────────────────────────────────────────
    public ConnectionState State => _driver.State;
    public ConnectionSettings Settings { get; private set; } = new();
    public KnxInterface? ActiveInterface { get; private set; }
    public TimeSpan ConnectedFor => _connectedSince.HasValue
                                                  ? DateTime.Now - _connectedSince.Value
                                                  : TimeSpan.Zero;

    public IReadOnlyList<ConnectionLogEntry> Log => _log;
    public IReadOnlyList<KnxInterface> DiscoveredInterfaces => _discovered;

    public bool IsConnected => _driver.IsConnected;
    public bool IsScanning => _scanning;
    public bool IsConnecting => _driver.State == ConnectionState.Connecting;

    // ── Private ───────────────────────────────────────────────────────────────
    private readonly IKnxBusDriver _driver;
    private readonly List<ConnectionLogEntry> _log = new();
    private readonly List<KnxInterface> _discovered = new();
    private DateTime? _connectedSince;
    private bool _scanning;

    // ── Constructor ───────────────────────────────────────────────────────────
    public ConnectionService(IKnxBusDriver driver)
    {
        _driver = driver;
        _driver.StateChanged += OnDriverStateChanged;
    }

    // ── Discovery ─────────────────────────────────────────────────────────────

    public async Task ScanNetworkAsync(Action<KnxInterface> onFound, CancellationToken ct = default)
    {
        if (_scanning) return;
        _scanning = true;
        _discovered.Clear();
        AddLog(LogLevel.Info, "KNXnet/IP Search Request broadcast sent (UDP 3671)");
        Notify();

        try
        {
            await _driver.DiscoverAsync(
                iface =>
                {
                    _discovered.Add(iface);
                    AddLog(LogLevel.Ok, $"Found: {iface.Name} @ {iface.IpAddress}");
                    onFound(iface);
                    Notify();
                },
                ct);

            AddLog(LogLevel.Ok, $"Scan complete — {_discovered.Count} interface(s) found");
        }
        catch (OperationCanceledException)
        {
            AddLog(LogLevel.Warn, "Scan cancelled");
        }
        finally
        {
            _scanning = false;
            Notify();
        }
    }

    // ── Connection ────────────────────────────────────────────────────────────

    public async Task ConnectAsync(ConnectionSettings settings)
    {
        if (IsConnecting) return;

        Settings = settings;
        AddLog(LogLevel.Info, $"Connecting to {settings.IpAddress}:{settings.Port} via {settings.Mode}…");
        Notify();

        try
        {
            await _driver.ConnectAsync(settings);

            _connectedSince = DateTime.Now;
            ActiveInterface = _discovered.FirstOrDefault(d => d.IpAddress == settings.IpAddress)
                              ?? new KnxInterface
                              {
                                  Name = "KNX Interface",
                                  IpAddress = settings.IpAddress,
                                  Port = settings.Port,
                                  Mode = settings.Mode,
                              };
            AddLog(LogLevel.Info, "KNXnet/IP handshake OK");
            AddLog(LogLevel.Ok, $"Connected to {settings.IpAddress}:{settings.Port}");
        }
        catch (KnxConnectionException ex)
        {
            ActiveInterface = null;
            _connectedSince = null;
            AddLog(LogLevel.Error, ex.Message);
        }
        catch (Exception ex)
        {
            ActiveInterface = null;
            _connectedSince = null;
            AddLog(LogLevel.Error, $"Unexpected error: {ex.Message}");
        }

        Notify();
    }

    public async Task DisconnectAsync()
    {
        await _driver.DisconnectAsync();
        ActiveInterface = null;
        _connectedSince = null;
        AddLog(LogLevel.Warn, "Disconnected by user");
        Notify();
    }

    /// <summary>Convenience sync wrapper — fires and forgets DisconnectAsync.</summary>
    public void Disconnect() => _ = DisconnectAsync();

    public void SelectInterface(KnxInterface iface)
    {
        Settings.IpAddress = iface.IpAddress;
        Settings.Port = iface.Port;
        Settings.Mode = iface.Mode;
        Notify();
    }

    // ── Driver state passthrough ──────────────────────────────────────────────

    private void OnDriverStateChanged(ConnectionState next)
    {
        // Connection dropped unexpectedly (e.g. network loss)
        if (next == ConnectionState.Disconnected && _connectedSince.HasValue)
        {
            ActiveInterface = null;
            _connectedSince = null;
            AddLog(LogLevel.Warn, "Connection lost — driver reported disconnect");
        }
        else if (next == ConnectionState.Error)
        {
            ActiveInterface = null;
            _connectedSince = null;
            AddLog(LogLevel.Error, "Driver reported a connection error");
        }

        Notify();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddLog(LogLevel level, string message)
    {
        _log.Insert(0, new ConnectionLogEntry { Level = level, Message = message });
        if (_log.Count > 20) _log.RemoveAt(_log.Count - 1);
    }

    private void Notify() => StateChanged?.Invoke();

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        _driver.StateChanged -= OnDriverStateChanged;
        await _driver.DisposeAsync();
    }
}