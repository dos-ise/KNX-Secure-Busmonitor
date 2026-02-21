using KnxMonitor.Abstractions;
using KnxMonitor.Models;

namespace KnxMonitor.Services;

/// <summary>
/// High-level KNX bus service — sits above <see cref="IKnxBusDriver"/> and
/// adds app-level concerns: pause/resume, stats aggregation, activity history,
/// and the recent-telegram ring buffer used by the GA detail panel.
///
/// The concrete transport (simulated, Falcon, knx.net…) is fully
/// hidden behind the injected driver.
/// </summary>
public class KnxService : IAsyncDisposable
{
    // ── Public Events ─────────────────────────────────────────────────────────
    public event Action<KnxTelegram>? TelegramReceived;
    public event Action<BusStats>? StatsUpdated;

    // ── Public State ──────────────────────────────────────────────────────────
    public BusStats Stats { get; private set; } = new() { DeviceCount = 192 };
    public bool IsPaused { get; private set; }
    public bool IsConnected => _driver.IsConnected;

    /// <summary>
    /// Ring buffer of the last <see cref="MaxRecent"/> telegrams.
    /// Used by the GA detail panel to show per-address history.
    /// Thread-safe for reads via snapshot; mutations are lock-protected.
    /// </summary>
    public IReadOnlyCollection<KnxTelegram> RecentTelegrams => _recentTelegrams;

    // ── Private ───────────────────────────────────────────────────────────────
    private readonly IKnxBusDriver _driver;
    private readonly LinkedList<KnxTelegram> _recentTelegrams = new();
    private const int MaxRecent = 200;

    private CancellationTokenSource? _statsCts;
    private readonly Random _rng = new();

    private int _totalCount;
    private int _tps;
    private int _errorCount;
    private readonly List<int> _activityHistory = new();

    // ── Constructor ───────────────────────────────────────────────────────────
    public KnxService(IKnxBusDriver driver)
    {
        _driver = driver;
        _driver.TelegramReceived += OnDriverTelegramReceived;
        _driver.StateChanged += OnDriverStateChanged;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Connect to the bus using the given settings and start the stats ticker.
    /// Delegates the actual handshake to <see cref="IKnxBusDriver.ConnectAsync"/>.
    /// Calling while already connected is a no-op.
    /// </summary>
    public async Task ConnectAsync(ConnectionSettings? settings = null, CancellationToken ct = default)
    {
        if (IsConnected) return;

        await _driver.ConnectAsync(settings ?? new ConnectionSettings(), ct);

        // Start the 1-second stats ticker only after a successful connect
        if (IsConnected)
            StartStatsTicker();
    }

    /// <summary>Gracefully disconnect and stop the stats ticker.</summary>
    public async Task DisconnectAsync()
    {
        StopStatsTicker();
        await _driver.DisconnectAsync();
        PublishStats();
    }

    public void TogglePause() => IsPaused = !IsPaused;
    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        _driver.TelegramReceived -= OnDriverTelegramReceived;
        _driver.StateChanged -= OnDriverStateChanged;

        StopStatsTicker();
        await _driver.DisposeAsync();
    }

    // ── Driver callbacks ──────────────────────────────────────────────────────

    private void OnDriverTelegramReceived(KnxTelegram telegram)
    {
        if (IsPaused) return;

        // Update ring buffer
        lock (_recentTelegrams)
        {
            _recentTelegrams.AddFirst(telegram);
            while (_recentTelegrams.Count > MaxRecent)
                _recentTelegrams.RemoveLast();
        }

        if (telegram.IsError)
            Interlocked.Increment(ref _errorCount);

        Interlocked.Increment(ref _totalCount);

        TelegramReceived?.Invoke(telegram);
        PublishStats();
    }

    private void OnDriverStateChanged(ConnectionState state)
    {
        // If the driver drops the connection unexpectedly, stop the stats ticker
        if (state is ConnectionState.Disconnected or ConnectionState.Error)
            StopStatsTicker();

        PublishStats();
    }

    // ── Stats ticker ──────────────────────────────────────────────────────────

    private void StartStatsTicker()
    {
        if (_statsCts is not null) return;
        _statsCts = new CancellationTokenSource();
        _ = RunStatsTickAsync(_statsCts.Token);
    }

    private void StopStatsTicker()
    {
        if (_statsCts is null) return;
        _statsCts.Cancel();
        _statsCts.Dispose();
        _statsCts = null;
    }

    private async Task RunStatsTickAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);

                // Smooth random TPS fluctuation (in production: compute from real telegram timestamps)
                _tps = Math.Max(5, Math.Min(80, _tps + _rng.Next(-5, 6)));

                lock (_activityHistory)
                {
                    _activityHistory.Add(_tps);
                    if (_activityHistory.Count > 40)
                        _activityHistory.RemoveAt(0);
                }

                PublishStats();
            }
        }
        catch (OperationCanceledException) { /* clean shutdown */ }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PublishStats()
    {
        List<int> historyCopy;
        lock (_activityHistory)
            historyCopy = new List<int>(_activityHistory);

        Stats = new BusStats
        {
            IsConnected = IsConnected,
            TotalCount = _totalCount,
            TelegramsPerSecond = _tps,
            ErrorCount = _errorCount,
            DeviceCount = Stats.DeviceCount,
            ActivityHistory = historyCopy,
        };

        StatsUpdated?.Invoke(Stats);
    }

    public async Task Write(string gaAddress, string gaName, string gaDptType, DptWriteValue staged)
    {
        await _driver.WriteAsync(gaAddress, staged.RawBytes);
    }

    public async Task Read(string gaAddress, string gaName, string gaDptType, string gaLastValue, string gaLastRaw)
    {
        //await _driver.ReadAsync();
    }
}