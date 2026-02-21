// NuGet packages required:
//   <PackageReference Include="Knx.Falcon.Sdk"             Version="6.*" />
//   <PackageReference Include="Knx.Falcon.ApplicationData" Version="6.*" />
//
// ⚠  MAUI/Android note:
//    The Falcon SDK is NOT supported on MAUI/Android (and potentially iOS).
//    Use SimulatedBusDriver on mobile targets. FalconBusDriver works on
//    Windows, macOS and Linux only.
//
// ── GroupValue API (Falcon 6.x) ────────────────────────────────────────────
//
//  GroupValue has NO .AsBool() / .AsUInt8() / .As2ByteFloat() etc.
//  The correct way to read values is:
//
//  1. Raw bytes (always available):
//       byte[] raw = groupValue.Value;           // raw APDU payload
//
//  2. Explicit cast operators (only when you know the exact type):
//       bool   b  = (bool)   groupValue;         // 1-bit  DPT-1
//       byte   u8 = (byte)   groupValue;         // 8-bit  DPT-5
//       ushort u16= (ushort) groupValue;         // 16-bit DPT-7
//       uint   u32= (uint)   groupValue;         // 32-bit DPT-12
//       ulong  u64= (ulong)  groupValue;         // 64-bit
//       float  f  = (float)  groupValue;         // 32-bit IEEE 754 DPT-14
//       double d  = (double) groupValue;         // 64-bit IEEE 754
//
//  3. DptFactory (recommended – DPT-aware, handles all sub-types correctly):
//       using Knx.Falcon.ApplicationData.DatapointTypes;
//       var converter = DptFactory.Default.Get(mainNumber, subNumber);
//       object typedVal = converter?.ToTypedValue(groupValue); // typed .NET value
//       string display  = converter?.ToValue(groupValue);      // formatted string
//
//  The DptFactory approach is used here because it:
//    - Correctly handles DPT-9 (16-bit KNX float, NOT IEEE 754)
//    - Respects sub-type scaling (DPT-5.001 = 0–100%, DPT-5.004 = 0–255)
//    - Returns a ready-to-display string via ToValue()
//    - Requires knx_master.xml in %ProgramData%/KNX/XML/project-23 or app dir

using Knx.Falcon;
using Knx.Falcon.ApplicationData.DatapointTypes;
using Knx.Falcon.Configuration;
using Knx.Falcon.KnxnetIp;
using Knx.Falcon.Sdk;
using KnxMonitor.Abstractions;
using KnxMonitor.Models;
using KnxMonitor.Services;
using ConnectionState = KnxMonitor.Models.ConnectionState;
using FalconGroupAddress = Knx.Falcon.GroupAddress;

namespace KnxMonitor.Infrastructure;

public sealed class FalconBusDriver(GroupAddressService addressService) : IKnxBusDriver
{
    // ── IKnxBusDriver ─────────────────────────────────────────────────────────

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public bool IsConnected => State == ConnectionState.Connected;

    public event Action<KnxTelegram>? TelegramReceived;
    public event Action<ConnectionState>? StateChanged;

    private KnxBus? _bus;

    // ── Discovery ─────────────────────────────────────────────────────────────

    public async Task DiscoverAsync(Action<KnxInterface> onFound, CancellationToken ct = default)
    {
        await foreach (var ip in KnxBus.DiscoverIpDevicesAsync(ct))
        {
            var mode = ip.Supports(ServiceFamily.Tunneling, 1)
                ? ConnectionMode.Tunneling
                : ConnectionMode.Routing;

            onFound(new KnxInterface
            {
                Name = ip.FriendlyName,
                Model = ip.SerialNumber,
                IpAddress = ip.LocalIPAddress.ToString(),
                Port = 3671,
                Mode = mode,
                MacAddress = ip.MacAddress.ToString(),
                SignalLevel = 4,
            });
        }
    }

    // ── Connection ────────────────────────────────────────────────────────────

    public async Task ConnectAsync(ConnectionSettings settings, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(settings.IpAddress))
        {
            return;
        }

        if (_bus is not null)
            await CleanupBusAsync();

        SetState(ConnectionState.Connecting);

        try
        {
            var connector = new IpTunnelingConnectorParameters
            {
                HostAddress = settings.IpAddress,
            };

            _bus = new KnxBus(connector);
            _bus.GroupMessageReceived += OnGroupMessageReceived;
            _bus.ConnectionStateChanged += OnFalconStateChanged;

            await _bus.ConnectAsync(ct);
            SetState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            await CleanupBusAsync();
            SetState(ConnectionState.Error);
            throw new KnxConnectionException(
                $"Falcon connect to {settings.IpAddress} failed: {ex.Message}", ex);
        }
    }

    public async Task DisconnectAsync()
    {
        await CleanupBusAsync();
        SetState(ConnectionState.Disconnected);
    }

    // ── Sending ───────────────────────────────────────────────────────────────

    public async Task WriteAsync(string groupAddress, byte[] value, CancellationToken ct = default)
    {
        ThrowIfNotConnected();
        var ga = FalconGroupAddress.Parse(groupAddress);
        var payload = BuildGroupValue(groupAddress, value);
        await _bus!.WriteGroupValueAsync(ga, payload, MessagePriority.Low, ct);
    }

    public async Task ReadAsync(string groupAddress, CancellationToken ct = default)
    {
        ThrowIfNotConnected();
        var ga = FalconGroupAddress.Parse(groupAddress);
        await _bus!.RequestGroupValueAsync(ga, MessagePriority.Low, ct);
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        await CleanupBusAsync();
    }

    // ── Falcon event handlers ─────────────────────────────────────────────────

    private void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        var type = e.EventType switch
        {
            GroupEventType.ValueWrite => MessageType.Write,
            GroupEventType.ValueRead => MessageType.Read,
            GroupEventType.ValueResponse => MessageType.Response,
            _ => MessageType.Write,
        };

        // DestinationAddress = the KNX group address being written/read (e.g. "0/0/1")
        // SourceAddress      = individual address of the sending device  (e.g. "1.1.5")
        var gaAddress = e.DestinationAddress.ToString();
        var srcAddress = e.SourceAddress.ToString();

        // Resolve GA metadata (name + DPT) from the ETS group address table
        var gaInfo = addressService.AllGroupAddresses
                            .FirstOrDefault(g => g.Address == gaAddress);
        var groupName = gaInfo?.Name ?? string.Empty;
        var dptString = gaInfo?.DptType ?? string.Empty;   // e.g. "DPT-9.001"

        // Decode value using the DPT-aware DptFactory
        var (decodedValue, rawHex) = DecodeGroupValue(e.Value, dptString);

        TelegramReceived?.Invoke(new KnxTelegram
        {
            Timestamp = DateTime.Now,
            SourceAddress = srcAddress,
            GroupAddress = gaAddress,
            GroupName = groupName,
            DptType = dptString,
            Type = type,
            DecodedValue = decodedValue,
            RawBytes = rawHex,
            RssiPercent = 100,
            IsError = false,
        });
    }

    private void OnFalconStateChanged(object? sender, EventArgs e)
    {
        var next = _bus?.ConnectionState == BusConnectionState.Connected
            ? ConnectionState.Connected
            : ConnectionState.Disconnected;
        SetState(next);
    }

    // ── Value decoding ────────────────────────────────────────────────────────
    //
    // Strategy (in order of preference):
    //
    // 1. DptFactory.Default.Get(main, sub) — returns a DptBase converter.
    //    Call .ToValue(groupValue) for a formatted display string.
    //    This handles DPT-9 (KNX 16-bit float), sub-type scaling, etc.
    //
    // 2. If DPT is unknown or DptFactory returns null, fall back to the
    //    GroupValue explicit cast operators based on raw byte count.
    //
    // 3. Raw hex is always computed from groupValue.Value (byte[]).

    private static (string decoded, string rawHex) DecodeGroupValue(
        GroupValue? value, string dptString)
    {
        if (value is null)
            return ("—", "—");

        // ── Raw hex (always available via .Value property) ─────────────────
        var rawBytes = value.Value;   // byte[]  — the APDU payload
        var rawHex = rawBytes is { Length: > 0 }
                       ? string.Join(" ", rawBytes.Select(b => $"0x{b:X2}"))
                       : "—";

        // ── Try DptFactory first (covers all standard DPTs correctly) ──────
        if (!string.IsNullOrEmpty(dptString))
        {
            var (main, sub) = ParseDptString(dptString);  // "DPT-9.001" → (9, 1)
            if (main > 0)
            {
                try
                {
                    var converter = DptFactory.Default.Get(main, sub);
                    if (converter is not null)
                    {
                        // ToValue(GroupValue) → object  (canonical .NET type, e.g. float for DPT-9)
                        // Format(GroupValue, string, IFormatProvider) → string with unit ("21.5 °C")
                        var typedObj = converter.ToValue(value);
                        if (typedObj is not null)
                        {
                            var display = converter.Format(
                                value,
                                null,   // format string – null = default
                                System.Globalization.CultureInfo.InvariantCulture);
                            return (display ?? typedObj.ToString() ?? rawHex, rawHex);
                        }
                    }
                }
                catch
                {
                    // DPT mismatch between ETS table and actual telegram — fall through
                }
            }
        }

        // ── Fallback: explicit cast operators based on byte count ──────────
        // Only use when DptFactory cannot help (unknown DPT or missing master data).
        try
        {
            return rawBytes.Length switch
            {
                // 1-bit: SizeInBit == 1 means it's a boolean
                _ when value.SizeInBit == 1
                    => ((bool)value ? "On" : "Off", rawHex),

                // 8-bit unsigned
                1 => ($"{(byte)value}", rawHex),

                // 16-bit unsigned (also used for KNX 2-byte float — but
                // without DPT we can't decode it properly, so show raw uint)
                2 => ($"{(ushort)value}", rawHex),

                // 32-bit unsigned
                4 => ($"{(uint)value}", rawHex),

                // 8-byte double
                8 => ($"{(double)value:0.##}", rawHex),

                // Anything else: raw hex only
                _ => (rawHex, rawHex),
            };
        }
        catch
        {
            // Cast failed (wrong size) — return raw hex
            return (rawHex, rawHex);
        }
    }

    // ── Value encoding (Write) ────────────────────────────────────────────────
    //
    // DptWriteValue.RawBytes contains the correctly encoded APDU payload.
    // We wrap it in GroupValue for Falcon to send.
    //
    // Special case: DPT-1 MUST use GroupValue(bool) constructor so Falcon
    // serialises it as a 1-bit APDU (value in the 6 LSBs of the first byte),
    // NOT as a full byte payload.  All other DPTs use GroupValue(byte[]).

    private GroupValue BuildGroupValue(string gaAddress, byte[] value)
    {
        var gaInfo = addressService.AllGroupAddresses
                           .FirstOrDefault(g => g.Address == gaAddress);
        var dptStr = gaInfo?.DptType ?? string.Empty;
        var (main, _) = ParseDptString(dptStr);

        // DPT-1: 1-bit boolean — must use the bool constructor
        if (main == 1 && value.Length == 1)
            return new GroupValue(value[0] != 0);   // GroupValue(bool)

        // All other DPTs: pass raw bytes directly
        return new GroupValue(value);               // GroupValue(byte[])
    }

    // ── DPT string parser ─────────────────────────────────────────────────────
    //
    // Parses ETS-style DPT strings into (mainNumber, subNumber):
    //   "DPT-9"       → (9,  -1)
    //   "DPT-9.001"   → (9,   1)
    //   "9.001"       → (9,   1)
    //   "DPST-9-1"    → (9,   1)   ← ETS 5 format
    //   invalid/empty → (0,  -1)

    private static (int main, int sub) ParseDptString(string dpt)
    {
        if (string.IsNullOrWhiteSpace(dpt))
            return (0, -1);

        // Normalise: strip "DPT-", "DPST-", spaces, uppercase
        var s = dpt.Trim()
                   .Replace("DPST-", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("DPT-", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("DPT", "", StringComparison.OrdinalIgnoreCase);

        // Replace hyphens used as separator (ETS 5: "9-1") with dot
        // but not the minus in negative numbers — safe because DPT numbers are positive
        s = System.Text.RegularExpressions.Regex.Replace(s, @"^(\d+)-(\d+)$", "$1.$2");

        var parts = s.Split('.');
        if (parts.Length == 0 || !int.TryParse(parts[0], out var main) || main <= 0)
            return (0, -1);

        var sub = -1;
        if (parts.Length >= 2 && int.TryParse(parts[1], out var parsedSub))
            sub = parsedSub;

        return (main, sub);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task CleanupBusAsync()
    {
        if (_bus is null) return;
        _bus.GroupMessageReceived -= OnGroupMessageReceived;
        _bus.ConnectionStateChanged -= OnFalconStateChanged;
        try { await _bus.DisposeAsync(); } catch { /* ignore */ }
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
}