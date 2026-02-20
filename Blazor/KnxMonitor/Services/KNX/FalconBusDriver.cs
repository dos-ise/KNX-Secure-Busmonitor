// NuGet: <PackageReference Include="Knx.Falcon.Sdk" Version="6.*" />
//
// ⚠  MAUI/Android note (from KNX Association docs):
//    "We are unable to provide support for the Falcon SDK on MAUI for Android
//    (and potentially for iOS as well)."
//    On Windows/macOS desktops and on Linux this driver works fine.
//    For a mobile target, consider keeping SimulatedBusDriver and tunnelling
//    via a local proxy that exposes a REST/WebSocket API instead.


using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Discovery;
using Knx.Falcon.KnxnetIp;
using Knx.Falcon.Sdk;
using KnxMonitor.Abstractions;
using KnxMonitor.Models;
using KnxMonitor.Services;
using ConnectionState = KnxMonitor.Models.ConnectionState;
using GroupAddress = Knx.Falcon.GroupAddress;

namespace KnxMonitor.Infrastructure;


public sealed class FalconBusDriver(GroupAddressService addressService) : IKnxBusDriver
{
    private readonly GroupAddressService _addressService = addressService;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public bool IsConnected => State == ConnectionState.Connected;

    public event Action<KnxTelegram>? TelegramReceived;
    public event Action<ConnectionState>? StateChanged;

    private KnxBus? _bus;

    public async Task DiscoverAsync(Action<KnxInterface> onFound, CancellationToken ct = default)
    {
        var discovery = new IpDeviceDiscovery();
        await foreach (var ip in KnxBus.DiscoverIpDevicesAsync(ct))
        {
            var connectionString = ip.ToConnectionString();

            // or directly to a IPTunnelingConnectorParameters object
            if (ip.Supports(ServiceFamily.Tunneling, 1))
            {
                var connectorParameter = IpTunnelingConnectorParameters.FromDiscovery(ip);
                var type = connectorParameter.Type;
            }

            var iface = new KnxInterface
            {
                Name = ip.FriendlyName,
                Model = ip.SerialNumber,
                IpAddress = ip.LocalIPAddress.ToString(),
                Port = 8080,
                Mode = ConnectionMode.Tunneling,
                MacAddress = ip.MacAddress.ToString(),
                SignalLevel = 4, // Falcon discovery doesn't expose RSSI; default to max
            };

            onFound(iface); 
        }
    }

    public async Task ConnectAsync(ConnectionSettings settings, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(settings.IpAddress))
        {
            return; 
        }

        if (_bus is not null)
            await DisposeAsync();
        SetState(ConnectionState.Connecting);

        var connector = new IpTunnelingConnectorParameters
        {
            HostAddress = settings.IpAddress
        };
        _bus = new KnxBus(connector);

        // Wire up events before connecting so no telegrams are missed
        _bus.GroupMessageReceived += OnGroupValueReceived;
        _bus.ConnectionStateChanged += OnFalconStateChanged;

        await _bus.ConnectAsync(ct);

        SetState(ConnectionState.Connected);
    }

    private void OnFalconStateChanged(object? sender, EventArgs e)
    {
        SetState(_bus is { ConnectionState: BusConnectionState.Connected }
            ? ConnectionState.Connected
            : ConnectionState.Disconnected);
    }

    private void OnGroupValueReceived(object? sender, GroupEventArgs e)
    {
        var type = e.EventType switch
        {
            GroupEventType.ValueWrite => MessageType.Write,
            GroupEventType.ValueRead => MessageType.Read,
            GroupEventType.ValueResponse => MessageType.Response,
            _ => MessageType.Write,
        };
        var source = e.DestinationAddress.ToString();
        var foundaddress = _addressService.AllGroupAddresses.Where(g => g.Address == source).ToList();
        string groupName = string.Empty;
        string dptType = string.Empty;
        if (foundaddress.Any())
        {
            groupName = foundaddress.First().Name;
            dptType = foundaddress.First().DptType;
        }
    
        string rawBytes = "—";
        var telegram = new KnxTelegram
        {
            Timestamp = DateTime.Now,
            SourceAddress = e.SourceAddress.ToString() ?? "—",
            GroupAddress = e.DestinationAddress.ToString(),
            GroupName = groupName,   // Populate from GroupAddressService by address lookup
            DptType = dptType,   // Populate from GroupAddressService
            Type = type,
            DecodedValue = e.Value?.ToString() ?? "—",
            RawBytes = rawBytes,
            RssiPercent = 100,
            IsError = false,
        };
        TelegramReceived?.Invoke(telegram);
    }

    public async Task DisconnectAsync()
    {
        await DisposeAsync();
    }

    public async Task WriteAsync(string groupAddress, byte[] value, CancellationToken ct = default)
    {
        ThrowIfNotConnected();

        var ga = GroupAddress.Parse(groupAddress);
        var payload = GroupValue.Parse(ConvertBytesForGroupValue(value));

        await _bus!.WriteGroupValueAsync(ga, payload, MessagePriority.Low, ct);
    }

    public async Task ReadAsync(string groupAddress, CancellationToken ct = default)
    {
        ThrowIfNotConnected();

        var ga = GroupAddress.Parse(groupAddress);
        await _bus!.RequestGroupValueAsync(ga, MessagePriority.Low, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupBusAsync();
    }

    private async Task CleanupBusAsync()
    {
        if (_bus is null) return;

        try { await _bus.DisposeAsync(); }
        catch { /* ignore errors during cleanup */ }

        _bus = null;
    }

    private void SetState(ConnectionState next)
    {
        if (State == next) return;
        State = next;
        StateChanged?.Invoke(next);
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException(
                "FalconBusDriver is not connected. Call ConnectAsync first.");
    }

    private string ConvertBytesForGroupValue(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentException("Empty byte array");

        // 1-bit boolean
        if (bytes.Length == 1 && (bytes[0] == 0 || bytes[0] == 1))
            return bytes[0] == 1 ? "true" : "false";

        // 8-bit integer
        if (bytes.Length == 1)
            return bytes[0].ToString();

        // Hexadecimal bytes
        return string.Join(" ", bytes.Select(b => $"0x{b:X2}"));
    }

}