using System.Text;
using System.Xml.Linq;
using KnxMonitor.Models;

namespace KnxMonitor.Services;

/// <summary>
/// Manages the group address database.
/// Supports:
///   - ETS5 XML export (.esf / .xml)
///   - Simple CSV  (MainAddr;MainName;MidAddr;MidName;GA;Name;DPT;Flags;Description)
///   - Manual entry
///
/// In production: parse real .knxproj (ZIP) with System.IO.Compression.
/// </summary>
public class GroupAddressService
{
    // ── Events ───────────────────────────────────────────────────────────────
    public event Action? DataChanged;

    // ── State ────────────────────────────────────────────────────────────────
    public List<MainGroup> MainGroups { get; private set; } = new();
    public bool HasData => MainGroups.Count > 0;

    public int TotalGaCount  => MainGroups.Sum(g => g.TotalGaCount);
    public int MainGroupCount => MainGroups.Count;

    // ── DPT Descriptions ─────────────────────────────────────────────────────
    private static readonly Dictionary<string, string> DptDescriptions = new()
    {
        ["DPT-1"]     = "Binary (1 bit)",
        ["DPT-5"]     = "Percent (8 bit)",
        ["DPT-5.001"] = "Percentage 0–100%",
        ["DPT-9"]     = "Float (2 byte)",
        ["DPT-9.001"] = "Temperature °C",
        ["DPT-9.004"] = "Illuminance lx",
        ["DPT-13"]    = "Int (4 byte)",
        ["DPT-17"]    = "Scene number",
    };

    public static string DptDescription(string dpt) =>
        DptDescriptions.TryGetValue(dpt, out var d) ? d : string.Empty;

    // ── Import ────────────────────────────────────────────────────────────────

    /// <summary>Parse CSV content and replace the current dataset.</summary>
    public void ImportCsv(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) throw new FormatException("CSV has no data rows.");

        var mainMap = new Dictionary<string, MainGroup>();

        foreach (var raw in lines.Skip(1))
        {
            var cols = raw.Split(';').Select(c => c.Trim().Trim('"')).ToArray();
            if (cols.Length < 5) continue;

            var mainAddr  = cols.ElementAtOrDefault(0) ?? "";
            var mainName  = cols.ElementAtOrDefault(1) ?? $"Main {mainAddr}";
            var midAddr   = cols.ElementAtOrDefault(2) ?? "";
            var midName   = cols.ElementAtOrDefault(3) ?? $"Middle {midAddr}";
            var gaAddr    = cols.ElementAtOrDefault(4) ?? "";
            var gaName    = cols.ElementAtOrDefault(5) ?? "Unknown";
            var dpt       = cols.ElementAtOrDefault(6) ?? "DPT-1";
            var flags     = cols.ElementAtOrDefault(7) ?? "CWT";
            var desc      = cols.ElementAtOrDefault(8) ?? "";

            if (string.IsNullOrWhiteSpace(gaAddr)) continue;

            if (!mainMap.TryGetValue(mainAddr, out var mg))
            {
                mg = new MainGroup { Address = mainAddr, Name = mainName };
                mainMap[mainAddr] = mg;
            }

            var mid = mg.MiddleGroups.FirstOrDefault(m => m.Address == midAddr);
            if (mid is null)
            {
                mid = new MiddleGroup { Address = midAddr, Name = midName };
                mg.MiddleGroups.Add(mid);
            }

            mid.GroupAddresses.Add(new GroupAddress
            {
                Address           = gaAddr,
                Name              = gaName,
                DptType           = dpt,
                Flags             = flags,
                Description       = desc,
                MainGroupName     = mainName,
                MiddleGroupName   = midName,
            });
        }

        MainGroups = mainMap.Values.ToList();
        DataChanged?.Invoke();
    }

    /// <summary>Parse ETS5 XML / ESF export.</summary>
    public void ImportXml(string content)
    {
        var doc = XDocument.Parse(content);
        var ns  = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        var mainMap = new Dictionary<string, MainGroup>();

        foreach (var mr in doc.Descendants(ns + "GroupRange").Where(e => e.Parent?.Name.LocalName != "GroupRange"))
        {
            var mainAddr = mr.Attribute("RangeStart")?.Value ?? "0";
            var mainName = mr.Attribute("Name")?.Value ?? $"Main {mainAddr}";

            if (!mainMap.TryGetValue(mainAddr, out var mg))
            {
                mg = new MainGroup { Address = mainAddr, Name = mainName };
                mainMap[mainAddr] = mg;
            }

            foreach (var midr in mr.Elements(ns + "GroupRange"))
            {
                var midAddr = midr.Attribute("RangeStart")?.Value ?? "0/0";
                var midName = midr.Attribute("Name")?.Value ?? $"Middle {midAddr}";
                var mid     = new MiddleGroup { Address = midAddr, Name = midName };
                mg.MiddleGroups.Add(mid);

                foreach (var ga in midr.Elements(ns + "GroupAddress"))
                {
                    mid.GroupAddresses.Add(new GroupAddress
                    {
                        Address         = ga.Attribute("Address")?.Value     ?? "0/0/0",
                        Name            = ga.Attribute("Name")?.Value        ?? "GA",
                        DptType         = ga.Attribute("DPTs")?.Value        ?? "DPT-1",
                        Flags           = "CWT",
                        Description     = ga.Attribute("Description")?.Value ?? "",
                        MainGroupName   = mainName,
                        MiddleGroupName = midName,
                    });
                }
            }
        }

        if (mainMap.Count == 0)
            throw new FormatException("No GroupRange elements found in XML.");

        MainGroups = mainMap.Values.ToList();
        DataChanged?.Invoke();
    }

    /// <summary>Update the last-seen value for a group address (called by KnxService).</summary>
    public void UpdateLastValue(string address, string decodedValue, string rawBytes)
    {
        foreach (var mg in MainGroups)
        foreach (var mid in mg.MiddleGroups)
        {
            var ga = mid.GroupAddresses.FirstOrDefault(g => g.Address == address);
            if (ga is not null)
            {
                ga.LastValue = decodedValue;
                ga.LastRaw   = rawBytes;
                ga.LastSeen  = DateTime.Now;
                DataChanged?.Invoke();
                return;
            }
        }
    }

    /// <summary>Load built-in demo dataset.</summary>
    public void LoadDemoData()
    {
        MainGroups = BuildDemoData();
        // Open first main + first middle group by default
        if (MainGroups.Count > 0)
        {
            MainGroups[0].IsOpen = true;
            if (MainGroups[0].MiddleGroups.Count > 0)
                MainGroups[0].MiddleGroups[0].IsOpen = true;
        }
        DataChanged?.Invoke();
    }

    public void Clear()
    {
        MainGroups.Clear();
        DataChanged?.Invoke();
    }

    // ── Search / Filter ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns a filtered copy of the group tree.
    /// Empty query → returns all groups (not a copy, be careful with IsOpen mutation).
    /// </summary>
    public List<MainGroup> Filter(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return MainGroups;

        var q = query.ToLowerInvariant();
        var result = new List<MainGroup>();

        foreach (var mg in MainGroups)
        {
            var mids = new List<MiddleGroup>();
            foreach (var mid in mg.MiddleGroups)
            {
                var gas = mid.GroupAddresses.Where(ga =>
                    ga.Address.Contains(q) ||
                    ga.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    ga.DptType.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    ga.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                if (gas.Count > 0)
                    mids.Add(new MiddleGroup { Address = mid.Address, Name = mid.Name, IsOpen = true, GroupAddresses = gas });
            }
            if (mids.Count > 0)
                result.Add(new MainGroup { Address = mg.Address, Name = mg.Name, IsOpen = true, MiddleGroups = mids });
        }
        return result;
    }

    /// <summary>All GAs as flat list (for flat view).</summary>
    public IEnumerable<GroupAddress> AllGroupAddresses =>
        MainGroups.SelectMany(mg => mg.MiddleGroups.SelectMany(m => m.GroupAddresses));

    // ── Demo Data ─────────────────────────────────────────────────────────────
    private static List<MainGroup> BuildDemoData() => new()
    {
        new MainGroup { Address="0", Name="Lighting", MiddleGroups = new()
        {
            new MiddleGroup { Address="0/0", Name="Living Room", GroupAddresses = new()
            {
                new() { Address="0/0/1", Name="Switch Main Light",  DptType="DPT-1",     Flags="CWT",  Description="Main ceiling light on/off",  LastValue="On",     LastRaw="0x01",      LastSeen=DateTime.Now.AddMinutes(-2) },
                new() { Address="0/0/2", Name="Dimmer Main Light",  DptType="DPT-5.001", Flags="CRWT", Description="Brightness 0–100%",           LastValue="75%",    LastRaw="0xBF",      LastSeen=DateTime.Now.AddMinutes(-5) },
                new() { Address="0/0/3", Name="Switch Floor Lamp",  DptType="DPT-1",     Flags="CWT",  Description="",                            LastValue="Off",    LastRaw="0x00",      LastSeen=DateTime.Now.AddMinutes(-28) },
            }},
            new MiddleGroup { Address="0/1", Name="Kitchen", GroupAddresses = new()
            {
                new() { Address="0/1/1", Name="Switch Kitchen",     DptType="DPT-1",     Flags="CWT",  Description="",                            LastValue="On",     LastRaw="0x01",      LastSeen=DateTime.Now.AddMinutes(-3) },
                new() { Address="0/1/2", Name="Switch Counter",     DptType="DPT-1",     Flags="CWT",  Description="Under-cabinet lighting",      LastValue="Off",    LastRaw="0x00",      LastSeen=DateTime.Now.AddMinutes(-47) },
            }},
            new MiddleGroup { Address="0/2", Name="Hallway", GroupAddresses = new()
            {
                new() { Address="0/2/1", Name="Switch Hallway",     DptType="DPT-1",     Flags="CWT",  Description="Motion-linked",               LastValue="Off",    LastRaw="0x00",      LastSeen=DateTime.Now.AddMinutes(-13) },
            }},
        }},
        new MainGroup { Address="1", Name="HVAC & Climate", MiddleGroups = new()
        {
            new MiddleGroup { Address="1/0", Name="Temperature", GroupAddresses = new()
            {
                new() { Address="1/0/1", Name="Setpoint Living Room", DptType="DPT-9.001", Flags="CRW",  Description="Target temperature",      LastValue="21.5°C", LastRaw="0x0C 0x1A", LastSeen=DateTime.Now.AddSeconds(-40) },
                new() { Address="1/0/2", Name="Actual Temp Kitchen",  DptType="DPT-9.001", Flags="CR",   Description="Sensor readback",         LastValue="22.1°C", LastRaw="0x0C 0x35", LastSeen=DateTime.Now.AddSeconds(-65) },
                new() { Address="1/0/3", Name="Actual Temp Bedroom",  DptType="DPT-9.001", Flags="CR",   Description="",                        LastValue="19.8°C", LastRaw="0x0B 0xE8", LastSeen=DateTime.Now.AddMinutes(-8) },
            }},
            new MiddleGroup { Address="1/1", Name="Fan & Ventilation", GroupAddresses = new()
            {
                new() { Address="1/1/1", Name="Fan Speed",            DptType="DPT-5.001", Flags="CRW",  Description="0–100% fan speed",        LastValue="40%",    LastRaw="0x66",      LastSeen=DateTime.Now.AddMinutes(-22) },
                new() { Address="1/1/2", Name="Bypass Flap",          DptType="DPT-1",     Flags="CWT",  Description="Heat recovery bypass",    LastValue="Off",    LastRaw="0x00",      LastSeen=DateTime.Now.AddHours(-2) },
            }},
        }},
        new MainGroup { Address="2", Name="Blinds & Shading", MiddleGroups = new()
        {
            new MiddleGroup { Address="2/0", Name="Living Room", GroupAddresses = new()
            {
                new() { Address="2/0/1", Name="Blind Move",           DptType="DPT-1",     Flags="CWT",  Description="Up/Down command",         LastValue="Up",     LastRaw="0x00",      LastSeen=DateTime.Now.AddHours(-1) },
                new() { Address="2/0/2", Name="Blind Position",       DptType="DPT-5.001", Flags="CRWT", Description="0=open, 100=closed",      LastValue="35%",    LastRaw="0x59",      LastSeen=DateTime.Now.AddHours(-1) },
                new() { Address="2/0/3", Name="Slat Position",        DptType="DPT-5.001", Flags="CRWT", Description="Slat angle 0–100%",       LastValue="0%",     LastRaw="0x00",      LastSeen=DateTime.Now.AddHours(-1) },
            }},
            new MiddleGroup { Address="2/1", Name="Bedroom", GroupAddresses = new()
            {
                new() { Address="2/1/1", Name="Blind Move",           DptType="DPT-1",     Flags="CWT",  Description="",                        LastValue="Down",   LastRaw="0x01",      LastSeen=DateTime.Now.AddHours(-10) },
                new() { Address="2/1/2", Name="Blind Position",       DptType="DPT-5.001", Flags="CRWT", Description="",                        LastValue="100%",   LastRaw="0xFF",      LastSeen=DateTime.Now.AddHours(-10) },
            }},
        }},
        new MainGroup { Address="3", Name="Security & Alarm", MiddleGroups = new()
        {
            new MiddleGroup { Address="3/0", Name="Zones", GroupAddresses = new()
            {
                new() { Address="3/0/1", Name="Zone 1 – Entry",       DptType="DPT-1",     Flags="CR",   Description="Sensor state",            LastValue="Idle",   LastRaw="0x00",      LastSeen=DateTime.Now.AddHours(-1) },
                new() { Address="3/0/2", Name="Zone 2 – Garden",      DptType="DPT-1",     Flags="CR",   Description="PIR sensor",              LastValue="Idle",   LastRaw="0x00",      LastSeen=DateTime.Now.AddHours(-1) },
            }},
            new MiddleGroup { Address="3/1", Name="Alarm Output", GroupAddresses = new()
            {
                new() { Address="3/1/1", Name="Siren",                DptType="DPT-1",     Flags="CWT",  Description="",                        LastValue="Off",    LastRaw="0x00" },
            }},
        }},
        new MainGroup { Address="4", Name="Energy", MiddleGroups = new()
        {
            new MiddleGroup { Address="4/0", Name="Metering", GroupAddresses = new()
            {
                new() { Address="4/0/1", Name="Power Total",          DptType="DPT-13",    Flags="CR",   Description="Active power W",          LastValue="1240 W", LastRaw="0x00 0x04 0xD8", LastSeen=DateTime.Now.AddSeconds(-5) },
                new() { Address="4/0/2", Name="Energy Counter",       DptType="DPT-13",    Flags="CR",   Description="kWh total",               LastValue="5843 kWh",LastRaw="0x00 0x16 0xD3",LastSeen=DateTime.Now.AddSeconds(-5) },
            }},
        }},
    };
}
