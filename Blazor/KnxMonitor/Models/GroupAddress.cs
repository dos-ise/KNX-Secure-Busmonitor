namespace KnxMonitor.Models;

/// <summary>KNX Group Address with DPT metadata and flags.</summary>
public class GroupAddress
{
    public string Address     { get; set; } = string.Empty;   // e.g. "0/0/1"
    public string Name        { get; set; } = string.Empty;
    public string DptType     { get; set; } = "DPT-1";
    public string Flags       { get; set; } = "CWT";          // C R W T U
    public string Description { get; set; } = string.Empty;
    public string LastValue   { get; set; } = "—";
    public string LastRaw     { get; set; } = "—";
    public DateTime? LastSeen { get; set; }
    public string MainGroupName   { get; set; } = string.Empty;
    public string MiddleGroupName { get; set; } = string.Empty;
}

/// <summary>Middle group (level 2, e.g. 0/1)</summary>
public class MiddleGroup
{
    public string Address  { get; set; } = string.Empty;   // e.g. "0/1"
    public string Name     { get; set; } = string.Empty;
    public bool   IsOpen   { get; set; }
    public List<GroupAddress> GroupAddresses { get; set; } = new();
}

/// <summary>Main group (level 1, e.g. 0)</summary>
public class MainGroup
{
    public string Address  { get; set; } = string.Empty;   // e.g. "0"
    public string Name     { get; set; } = string.Empty;
    public bool   IsOpen   { get; set; }
    public List<MiddleGroup> MiddleGroups { get; set; } = new();

    public int TotalGaCount => MiddleGroups.Sum(m => m.GroupAddresses.Count);
}

/// <summary>Color palette for main groups.</summary>
public static class GroupColors
{
    private static readonly string[] Palette =
    {
        "#00d4ff", "#ff6b35", "#39ff14", "#ffcc00",
        "#ff3366", "#a855f7", "#06b6d4", "#f97316"
    };

    public static string ForIndex(int i) => Palette[i % Palette.Length];
}
