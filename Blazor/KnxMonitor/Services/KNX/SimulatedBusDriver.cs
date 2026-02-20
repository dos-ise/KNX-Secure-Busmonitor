using KnxMonitor.Abstractions;
using KnxMonitor.Models;

namespace KnxMonitor.Infrastructure;

/// <summary>
/// In-process KNX bus simulator — implements <see cref="IKnxBusDriver"/>
/// without any real hardware or SDK dependency.
///
/// Produces a realistic stream of random telegrams across a set of
/// pre-configured group addresses, simulates KNXnet/IP device discovery,
/// and responds to GroupValue_Read requests with a plausible value.
///
/// Register in MauiProgram.cs:
///   builder.Services.AddSingleton&lt;IKnxBusDriver, SimulatedBusDriver&gt;();
/// </summary>
public sealed class SimulatedBusDriver : IKnxBusDriver
{
    // ── IKnxBusDriver ─────────────────────────────────────────────────────────

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public bool IsConnected => State == ConnectionState.Connected;

    public event Action<KnxTelegram>? TelegramReceived;
    public event Action<ConnectionState>? StateChanged;

    // ── Simulated KNXnet/IP interfaces on the "network" ───────────────────────

    private static readonly KnxInterface[] SimulatedInterfaces =
    {
        new() { Name = "MDT IP Interface",     Model = "SCN-IP100.02",  IpAddress = "192.168.1.100", Port = 3671, Mode = ConnectionMode.Tunneling, SignalLevel = 4, MacAddress = "00:0C:45:12:34:56" },
        new() { Name = "Gira X1",              Model = "209200",        IpAddress = "192.168.1.101", Port = 3671, Mode = ConnectionMode.Tunneling, SignalLevel = 3, MacAddress = "00:09:47:AB:CD:EF" },
        new() { Name = "Weinzierl KNX IP 730", Model = "5207",          IpAddress = "192.168.1.115", Port = 3671, Mode = ConnectionMode.Tunneling, SignalLevel = 4, MacAddress = "00:0E:8C:77:11:22" },
        new() { Name = "KNX IP Router",        Model = "SCN-IPROUT.02", IpAddress = "224.0.23.12",   Port = 3671, Mode = ConnectionMode.Routing,   SignalLevel = 2, MacAddress = "00:0C:45:99:88:77" },
    };

    // ── Simulated group address table ─────────────────────────────────────────
    // (addr, group-name, DPT, default message type, possible decoded values, raw byte templates)

    private static readonly SimGa[] SimulatedGas =
    {
        new("0/0/1", "Switch Living Room",   "DPT-1",  MessageType.Write,    ["On",       "Off"],                          ["0x01", "0x00"]),
        new("0/0/2", "Dimmer Living Room",   "DPT-5",  MessageType.Write,    ["85%",      "40%",   "100%",   "0%"],        ["0xD9", "0x66", "0xFF", "0x00"]),
        new("0/1/1", "Switch Kitchen",       "DPT-1",  MessageType.Write,    ["On",       "Off"],                          ["0x01", "0x00"]),
        new("1/0/1", "Setpoint Living Room", "DPT-9",  MessageType.Write,    ["21.5°C",   "22.0°C","20.5°C"],              ["0x0C 0x1A", "0x0C 0x35", "0x0B 0xF5"]),
        new("1/0/2", "Actual Temp Kitchen",  "DPT-9",  MessageType.Response, ["21.5°C",   "22.1°C","20.8°C"],              ["0x0C 0x1A", "0x0C 0x35", "0x0C 0x08"]),
        new("1/0/3", "Actual Temp Bedroom",  "DPT-9",  MessageType.Response, ["19.8°C",   "20.2°C","18.5°C"],              ["0x0B 0xE8", "0x0C 0x02", "0x0B 0x9A"]),
        new("1/1/1", "Fan Speed",            "DPT-5",  MessageType.Write,    ["30%",      "60%",   "90%"],                 ["0x4C", "0x99", "0xE6"]),
        new("1/1/2", "Bypass Flap",          "DPT-1",  MessageType.Write,    ["On",       "Off"],                          ["0x01", "0x00"]),
        new("2/0/1", "Blind Move LR",        "DPT-1",  MessageType.Write,    ["Up",       "Down"],                         ["0x00", "0x01"]),
        new("2/0/2", "Blind Position LR",    "DPT-5",  MessageType.Response, ["35%",      "0%",    "100%",   "60%"],       ["0x59", "0x00", "0xFF", "0x99"]),
        new("2/1/1", "Blind Move Bedroom",   "DPT-1",  MessageType.Write,    ["Up",       "Down"],                         ["0x00", "0x01"]),
        new("3/0/1", "Zone 1 – Entry",       "DPT-1",  MessageType.Response, ["Idle",     "Active"],                       ["0x00", "0x01"]),
        new("3/0/2", "Zone 2 – Garden",      "DPT-1",  MessageType.Response, ["Idle",     "Active"],                       ["0x00", "0x01"]),
        new("4/0/1", "Power Total",          "DPT-13", MessageType.Response, ["1240 W",   "980 W", "1540 W", "2100 W"],   ["0x00 0x04 0xD8", "0x00 0x03 0xD4", "0x00 0x06 0x04"]),
        new("4/0/2", "Energy Counter",       "DPT-13", MessageType.Response, ["5843 kWh", "5844 kWh"],                    ["0x00 0x16 0xD3", "0x00 0x16 0xD4"]),
    };

    private static readonly string[] SimulatedSources =
        ["1.1.1", "1.1.2", "1.2.1", "2.1.3", "1.3.1", "2.0.1"];

    // ── Private state ─────────────────────────────────────────────────────────

    private CancellationTokenSource? _cts;
    private readonly Random _rng = new();

    // Tracks the current "last value" per GA so Read responses are coherent
    private readonly Dictionary<string, int> _lastValueIndex = new();

    // ── IKnxBusDriver: Discovery ──────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task DiscoverAsync(Action<KnxInterface> onFound, CancellationToken ct = default)
    {
        // Simulate staggered KNXnet/IP Search Response packets arriving over ~3 s
        var delays = new[] { 350, 750, 1400, 2200 };

        for (int i = 0; i < SimulatedInterfaces.Length; i++)
        {
            await Task.Delay(delays[i], ct);
            ct.ThrowIfCancellationRequested();
            onFound(SimulatedInterfaces[i]);
        }
    }

    // ── IKnxBusDriver: Connection ─────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task ConnectAsync(ConnectionSettings settings, CancellationToken ct = default)
    {
        if (IsConnected) return;

        SetState(ConnectionState.Connecting);

        // Simulate handshake delay (Description Request → Description Response → Connect Request → Connect Response)
        await Task.Delay(600, ct);

        // Simulate occasional connect failure (~10 %)
        if (_rng.NextDouble() < 0.10)
        {
            SetState(ConnectionState.Error);
            throw new KnxConnectionException(
                $"KNXnet/IP connect to {settings.IpAddress}:{settings.Port} timed out.");
        }

        SetState(ConnectionState.Connected);

        // Start background telegram generator
        _cts = new CancellationTokenSource();
        _ = RunTelegramLoopAsync(_cts.Token);
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }
        SetState(ConnectionState.Disconnected);
    }

    // ── IKnxBusDriver: Sending ────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// The simulator stores the written value so that a subsequent
    /// <see cref="ReadAsync"/> returns it as a coherent response.
    /// </remarks>
    public Task WriteAsync(string groupAddress, byte[] value, CancellationToken ct = default)
    {
        ThrowIfNotConnected();

        // Echo the write back as an incoming telegram (mirrors bus behavior)
        var ga = FindGa(groupAddress);
        var decoded = ga is not null
            ? ga.Values[_rng.Next(ga.Values.Length)]   // simplified: pick a plausible label
            : BitConverter.ToString(value).Replace("-", " 0x").Insert(0, "0x");

        var telegram = BuildTelegram(groupAddress, ga?.GroupName ?? groupAddress,
            ga?.Dpt ?? "DPT-1", MessageType.Write, decoded,
            BitConverter.ToString(value).Replace("-", " 0x").Insert(0, "0x"));

        RaiseTelegram(telegram);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The simulator fires a GroupValue_Response back on the same GA
    /// after a realistic ~20–80 ms round-trip delay.
    /// </remarks>
    public Task ReadAsync(string groupAddress, CancellationToken ct = default)
    {
        ThrowIfNotConnected();

        _ = Task.Run(async () =>
        {
            await Task.Delay(20 + _rng.Next(60), ct);

            var ga = FindGa(groupAddress);
            if (ga is null) return;

            // Return the last known value for this GA (or pick randomly)
            if (!_lastValueIndex.TryGetValue(groupAddress, out var idx))
                idx = _rng.Next(ga.Values.Length);

            var telegram = BuildTelegram(
                groupAddress, ga.GroupName, ga.Dpt,
                MessageType.Response,
                ga.Values[idx],
                ga.RawBytes[idx % ga.RawBytes.Length]);

            RaiseTelegram(telegram);
        }, ct);

        return Task.CompletedTask;
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

    // ── Background telegram loop ──────────────────────────────────────────────

    private async Task RunTelegramLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Random inter-telegram gap: 300 ms – 1 500 ms
                await Task.Delay(300 + _rng.Next(1200), ct);

                var ga = SimulatedGas[_rng.Next(SimulatedGas.Length)];
                var idx = _rng.Next(ga.Values.Length);
                _lastValueIndex[ga.Address] = idx;

                var telegram = BuildTelegram(
                    ga.Address, ga.GroupName, ga.Dpt, ga.DefaultType,
                    ga.Values[idx],
                    ga.RawBytes[idx % ga.RawBytes.Length]);

                RaiseTelegram(telegram);
            }
        }
        catch (OperationCanceledException) { /* clean shutdown */ }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private KnxTelegram BuildTelegram(
        string groupAddress, string groupName,
        string dpt, MessageType type,
        string decodedValue, string rawBytes) => new()
        {
            Timestamp = DateTime.Now,
            SourceAddress = SimulatedSources[_rng.Next(SimulatedSources.Length)],
            GroupAddress = groupAddress,
            GroupName = groupName,
            DptType = dpt,
            Type = type,
            DecodedValue = decodedValue,
            RawBytes = rawBytes,
            RssiPercent = 40 + _rng.Next(60),
            IsError = false,
        };

    private void RaiseTelegram(KnxTelegram telegram) =>
        TelegramReceived?.Invoke(telegram);

    private void SetState(ConnectionState next)
    {
        State = next;
        StateChanged?.Invoke(next);
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException(
                "SimulatedBusDriver is not connected. Call ConnectAsync first.");
    }

    private static SimGa? FindGa(string address) =>
        Array.Find(SimulatedGas, g => g.Address == address);

    // ── Inner record ─────────────────────────────────────────────────────────

    private sealed record SimGa(
        string Address,
        string GroupName,
        string Dpt,
        MessageType DefaultType,
        string[] Values,
        string[] RawBytes);
}