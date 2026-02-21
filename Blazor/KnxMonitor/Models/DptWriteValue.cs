namespace KnxMonitor.Models;

/// <summary>Widget type derived from DPT string.</summary>
public enum DptWidgetType { Binary, Percent, Float, Integer, Scene, Raw }

/// <summary>
/// Encapsulates a staged value ready to be written to the KNX bus.
/// Holds both the human-readable display value and the raw byte payload.
/// </summary>
public class DptWriteValue
{
    public string        DisplayValue { get; set; } = string.Empty;
    public byte[]        RawBytes     { get; set; } = Array.Empty<byte>();
    public string        RawHex       => RawBytes.Length == 0 ? "—"
                                         : string.Join(" ", RawBytes.Select(b => $"0x{b:X2}"));

    // ── Factory helpers ───────────────────────────────────────────────────────

    public static DptWriteValue FromBool(bool on)
        => new() { DisplayValue = on ? "On" : "Off", RawBytes = [on ? (byte)1 : (byte)0] };

    public static DptWriteValue FromPercent(int percent)
    {
        var raw = (byte)Math.Round(Math.Clamp(percent, 0, 100) * 2.55);
        return new() { DisplayValue = $"{percent}%", RawBytes = [raw] };
    }

    public static DptWriteValue FromFloat(double value, string unit = "")
    {
        // KNX DPT-9 16-bit float (EIS 5): M × 0.01 × 2^E
        // Simplified encoding — replace with a full Falcon/Calimero encoder in production
        var encoded = EncodeDpt9(value);
        return new()
        {
            DisplayValue = $"{value:0.##}{unit}",
            RawBytes     = [(byte)(encoded >> 8), (byte)(encoded & 0xFF)]
        };
    }

    public static DptWriteValue FromInt32(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes); // KNX is big-endian
        return new() { DisplayValue = value.ToString(), RawBytes = bytes };
    }

    public static DptWriteValue FromScene(int sceneNumber)
    {
        // DPT-17: scene number stored as (n-1) in bits 5..0, bit 7 = 0 (activate)
        var raw = (byte)(Math.Clamp(sceneNumber - 1, 0, 63) & 0x3F);
        return new() { DisplayValue = $"Scene {sceneNumber}", RawBytes = [raw] };
    }

    public static DptWriteValue FromHex(string hex)
    {
        var clean = hex.Replace("0x", "").Replace(" ", "").Trim();
        try
        {
            var bytes = Enumerable.Range(0, clean.Length / 2)
                                  .Select(i => Convert.ToByte(clean.Substring(i * 2, 2), 16))
                                  .ToArray();
            return new() { DisplayValue = hex, RawBytes = bytes };
        }
        catch { return new() { DisplayValue = hex, RawBytes = [] }; }
    }

    // ── DPT-9 encoder (simplified — accurate for most HVAC/sensor values) ────
    private static ushort EncodeDpt9(double v)
    {
        if (v == 0) return 0;
        var sign = v < 0;
        var abs  = Math.Abs(v);
        int exp  = 0;
        var mant = abs * 100.0;
        while (mant > 2047) { mant /= 2; exp++; }
        while (mant < 1 && exp > 0) { mant *= 2; exp--; }
        var m = (int)Math.Round(mant) & 0x7FF;
        if (sign) m = (~m + 1) & 0x7FF;
        return (ushort)(((sign ? 1 : 0) << 15) | ((exp & 0x0F) << 11) | (m & 0x7FF));
    }
}

/// <summary>Helper to classify a DPT string into a widget type.</summary>
public static class DptClassifier
{
    public static DptWidgetType Classify(string dpt)
    {
        if (string.IsNullOrEmpty(dpt)) return DptWidgetType.Raw;
        var d = dpt.ToUpperInvariant();
        if (d.StartsWith("DPT-1"))  return DptWidgetType.Binary;
        if (d.StartsWith("DPT-5"))  return DptWidgetType.Percent;
        if (d.StartsWith("DPT-9"))  return DptWidgetType.Float;
        if (d.StartsWith("DPT-13")) return DptWidgetType.Integer;
        if (d.StartsWith("DPT-17")) return DptWidgetType.Scene;
        return DptWidgetType.Raw;
    }

    public static string FloatUnit(string dpt) => dpt switch
    {
        "DPT-9.001" => "°C",
        "DPT-9.004" => " lx",
        "DPT-9.005" => " m/s",
        "DPT-9.007" => " %",
        _ => ""
    };

    public static (string Off, string On) BinaryLabels(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("blind") || n.Contains("shutter")) return ("Up",   "Down");
        if (n.Contains("alarm") || n.Contains("zone"))    return ("Idle", "Active");
        if (n.Contains("bypass") || n.Contains("flap"))   return ("Open", "Closed");
        return ("Off", "On");
    }
}
