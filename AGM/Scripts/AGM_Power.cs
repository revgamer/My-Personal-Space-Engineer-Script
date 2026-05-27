private const string VERSION = "1.0-power";
private const string PB_TAG = "{AGM-Power}";
private const string CORE_TAG = "{AGM-Core}";
private static readonly StringComparison SC = StringComparison.OrdinalIgnoreCase;
private readonly Color COLOR_BG = new Color(5, 16, 28);
private readonly Color COLOR_PANEL = new Color(9, 24, 40);
private readonly Color COLOR_ACCENT = new Color(255, 231, 38);
private readonly Color COLOR_ACCENT_2 = new Color(255, 225, 94);
private readonly Color COLOR_TEXT = new Color(244, 227, 184);
private readonly Color COLOR_DIM = new Color(191, 160, 100);
private readonly Color COLOR_OK = new Color(91, 242, 159);
private readonly Color COLOR_WARN = new Color(255, 205, 89);
private readonly Color COLOR_BAD = new Color(255, 100, 78);

private class PowerProfile
{
    public string Name = "Base";
    public string Batteries = "";
    public string Reactors = "";
    public string Solar = "";
    public string Wind = "";
    public string Hydrogen = "";
    public bool IncludeUngrouped = false;
}

private class PowerStats
{
    public int Batteries, Reactors, Solar, Wind, Hydrogen, Producers;
    public double Stored, Capacity, Input, Output, MaxOutput;
}

private readonly List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
private readonly List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
private readonly List<PowerProfile> profiles = new List<PowerProfile>();
private readonly HashSet<long> selectedIds = new HashSet<long>();
private readonly StringBuilder sb = new StringBuilder();

private bool coreFound = false;
private bool coreEnabled = false;
private bool powerEnabled = false;
private bool globalPause = false;
private bool includeDockedGrids = false;
private int tick = 0;
private bool booting = true;
private double bootElapsed = 0.0;
private DateTime lastRun = DateTime.Now;
private string lastStatus = "boot";
private string warning = "";

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    EnsureConfig();
    Reload();
}

public void Save()
{
    WriteState();
}

public void Main(string argument, UpdateType updateSource)
{
    string arg = argument == null ? "" : argument.Trim();
    if (arg.Equals("reload", SC) || arg.Equals("rescan", SC) || arg.Equals("reboot", SC) || arg.Equals("boot", SC))
    {
        Reload();
        if (arg.Equals("reboot", SC) || arg.Equals("boot", SC))
            RestartBoot();
    }

    if ((updateSource & (UpdateType.Update10 | UpdateType.Update100)) != 0)
    {
        tick += (updateSource & UpdateType.Update100) != 0 ? 100 : 10;
        if (tick >= 100)
        {
            tick = 0;
            Reload();
        }
    }

    if (booting)
    {
        bootElapsed += Math.Max(0.1, (DateTime.Now - lastRun).TotalSeconds);
        lastRun = DateTime.Now;
        DrawModuleBoot(Me.GetSurface(0), Math.Min(1.0, bootElapsed / 4.0));
        if (bootElapsed >= 4.0) booting = false;
        return;
    }
    lastRun = DateTime.Now;

    DrawPbStatus();
    WriteState();
    EchoStatus();
}

private void RestartBoot()
{
    booting = true;
    bootElapsed = 0.0;
    lastRun = DateTime.Now;
}

private void EnsureConfig()
{
    if (!string.IsNullOrWhiteSpace(Me.CustomData)) return;
    Me.CustomData =
@"[Power:Base]
batteries=G:Base Batteries
reactors=G:Base Reactors
solar=G:Base Solar
wind=G:Base Wind
hydrogen=G:Base Hydrogen Engines
include_ungrouped=false";
}

private void Reload()
{
    ReadOwnConfig();
    ScanBlocks();
    ReadCore();
    if (includeDockedGrids)
        ScanBlocks();
    lastStatus = CurrentState();
}

private void ReadOwnConfig()
{
    profiles.Clear();
    PowerProfile active = null;
    string[] lines = Me.CustomData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.Length == 0) continue;
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            string name = line.Substring(1, line.Length - 2).Trim();
            active = null;
            if (name.StartsWith("Power:", SC))
            {
                active = new PowerProfile();
                active.Name = name.Substring(6).Trim();
                if (active.Name.Length == 0) active.Name = "Base";
                profiles.Add(active);
            }
            continue;
        }
        if (active == null) continue;
        string key, value;
        if (!TrySplitKeyValue(line, '=', out key, out value)) continue;
        if (key.Equals("batteries", SC)) active.Batteries = value.Trim();
        else if (key.Equals("reactors", SC)) active.Reactors = value.Trim();
        else if (key.Equals("solar", SC)) active.Solar = value.Trim();
        else if (key.Equals("wind", SC)) active.Wind = value.Trim();
        else if (key.Equals("hydrogen", SC)) active.Hydrogen = value.Trim();
        else if (key.Equals("include_ungrouped", SC)) active.IncludeUngrouped = ParseBool(value, false);
    }
    if (profiles.Count == 0)
    {
        PowerProfile p = new PowerProfile();
        p.IncludeUngrouped = true;
        profiles.Add(p);
    }
}

private void ScanBlocks()
{
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, b => includeDockedGrids || b.IsSameConstructAs(Me));
}

private void ReadCore()
{
    coreFound = false;
    coreEnabled = false;
    powerEnabled = false;
    globalPause = false;
    includeDockedGrids = false;
    for (int i = 0; i < blocks.Count; i++)
    {
        IMyProgrammableBlock pb = blocks[i] as IMyProgrammableBlock;
        if (pb == null || pb == Me || pb.CustomName.IndexOf(CORE_TAG, SC) < 0) continue;
        coreFound = true;
        string[] lines = pb.CustomData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int l = 0; l < lines.Length; l++)
        {
            string key, value;
            if (!TrySplitKeyValue(StripComment(lines[l]).Trim(), '=', out key, out value)) continue;
            if (key.Equals("enabled", SC) || key.Equals("core_enabled", SC)) coreEnabled = ParseBool(value, true);
            else if (key.Equals("power", SC) || key.Equals("power_enabled", SC)) powerEnabled = ParseBool(value, true);
            else if (key.Equals("global_pause", SC)) globalPause = ParseBool(value, false);
            else if (key.Equals("include_docked_grids", SC)) includeDockedGrids = ParseBool(value, false);
        }
        break;
    }
}

private PowerStats BuildStats(PowerProfile profile)
{
    PowerStats stats = new PowerStats();
    selectedIds.Clear();
    AddBatteries(stats, profile.Batteries, profile.IncludeUngrouped);
    AddProducers(stats, profile.Reactors, "reactor", profile.IncludeUngrouped);
    AddProducers(stats, profile.Solar, "solar", profile.IncludeUngrouped);
    AddProducers(stats, profile.Wind, "wind", profile.IncludeUngrouped);
    AddProducers(stats, profile.Hydrogen, "hydrogen", profile.IncludeUngrouped);
    return stats;
}

private void AddBatteries(PowerStats stats, string spec, bool includeUngrouped)
{
    for (int i = 0; i < blocks.Count; i++)
    {
        IMyBatteryBlock b = blocks[i] as IMyBatteryBlock;
        if (b == null || !MatchesSpec(b, spec, includeUngrouped)) continue;
        if (!selectedIds.Add(b.EntityId)) continue;
        stats.Batteries++;
        stats.Stored += b.CurrentStoredPower;
        stats.Capacity += b.MaxStoredPower;
        stats.Input += b.CurrentInput;
        stats.Output += b.CurrentOutput;
    }
}

private void AddProducers(PowerStats stats, string spec, string kind, bool includeUngrouped)
{
    for (int i = 0; i < blocks.Count; i++)
    {
        IMyPowerProducer p = blocks[i] as IMyPowerProducer;
        IMyTerminalBlock b = blocks[i];
        if (p == null || p is IMyBatteryBlock || !IsProducerKind(b, kind)) continue;
        if (!MatchesSpec(b, spec, includeUngrouped)) continue;
        if (!selectedIds.Add(b.EntityId)) continue;
        stats.Producers++;
        stats.Output += p.CurrentOutput;
        stats.MaxOutput += p.MaxOutput;
        if (kind == "reactor") stats.Reactors++;
        else if (kind == "solar") stats.Solar++;
        else if (kind == "wind") stats.Wind++;
        else if (kind == "hydrogen") stats.Hydrogen++;
    }
}

private bool MatchesSpec(IMyTerminalBlock block, string spec, bool includeUngrouped)
{
    if (block == null) return false;
    if (string.IsNullOrWhiteSpace(spec)) return includeUngrouped;
    spec = spec.Trim();
    if (spec.StartsWith("G:", SC))
    {
        string groupName = spec.Substring(2).Trim();
        IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(groupName);
        if (group == null) return false;
        groupBlocks.Clear();
        group.GetBlocks(groupBlocks);
        return groupBlocks.Contains(block);
    }
    return block.CustomName.IndexOf(spec, SC) >= 0;
}

private bool IsProducerKind(IMyTerminalBlock block, string kind)
{
    if (block == null) return false;
    if (kind == "reactor") return block is IMyReactor;
    if (kind == "solar") return block is IMySolarPanel;
    string def = block.BlockDefinition.TypeIdString + "/" + block.BlockDefinition.SubtypeId;
    if (kind == "wind") return def.IndexOf("WindTurbine", SC) >= 0;
    if (kind == "hydrogen") return def.IndexOf("HydrogenEngine", SC) >= 0;
    return false;
}

private string CurrentState()
{
    if (!coreFound) return "NO CORE";
    if (!coreEnabled || !powerEnabled) return "DISABLED";
    if (globalPause) return "PAUSED";
    return "ONLINE";
}

private void DrawPbStatus()
{
    DrawPower(Me.GetSurface(0));
}

private void DrawPower(IMyTextSurface surface)
{
    if (surface == null) return;
    PowerProfile profile = profiles.Count > 0 ? profiles[0] : new PowerProfile();
    PowerStats stats = BuildStats(profile);
    double batteryPct = stats.Capacity > 0 ? stats.Stored / stats.Capacity : 0.0;
    double outputPct = stats.MaxOutput > 0 ? stats.Output / stats.MaxOutput : 0.0;
    surface.ContentType = ContentType.SCRIPT;
    surface.Script = "";
    surface.Font = "Monospace";
    surface.FontSize = 0.78f;
    surface.TextPadding = 1f;
    RectangleF vp = new RectangleF((surface.TextureSize - surface.SurfaceSize) * 0.5f, surface.SurfaceSize);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f);
        DrawText(frame, "AGM POWER", panel.Position + new Vector2(panel.Width * 0.5f, 24f), COLOR_ACCENT_2, 0.92f, TextAlignment.CENTER);
        DrawText(frame, lastStatus, panel.Position + new Vector2(panel.Width * 0.5f, 54f), StateColor(), 0.55f, TextAlignment.CENTER);
        float y = panel.Y + 92f;
        DrawRow(frame, panel, y, "Profile", profile.Name, COLOR_TEXT); y += 32f;
        DrawRow(frame, panel, y, "Batteries", stats.Batteries + " | " + Percent(batteryPct), BatteryColor(batteryPct)); y += 32f;
        DrawRow(frame, panel, y, "Stored", FormatPower(stats.Stored) + "Wh / " + FormatPower(stats.Capacity) + "Wh", COLOR_ACCENT_2); y += 32f;
        DrawRow(frame, panel, y, "Output", FormatPower(stats.Output) + "W / " + FormatPower(stats.MaxOutput) + "W", OutputColor(outputPct)); y += 32f;
        DrawRow(frame, panel, y, "Sources", "R " + stats.Reactors + " S " + stats.Solar + " W " + stats.Wind + " H " + stats.Hydrogen, COLOR_TEXT); y += 32f;
        DrawRow(frame, panel, y, "Input", FormatPower(stats.Input) + "W", COLOR_TEXT);
        DrawText(frame, "v" + VERSION, new Vector2(panel.X + panel.Width * 0.5f, panel.Bottom - 18f), COLOR_DIM, 0.34f, TextAlignment.CENTER);
    }
}

private void WriteState()
{
    PowerProfile profile = profiles.Count > 0 ? profiles[0] : new PowerProfile();
    PowerStats stats = BuildStats(profile);
    double batteryPct = stats.Capacity > 0 ? stats.Stored / stats.Capacity * 100.0 : 0.0;
    double outputPct = stats.MaxOutput > 0 ? stats.Output / stats.MaxOutput * 100.0 : 0.0;
    sb.Clear();
    sb.AppendLine("[PowerState]");
    sb.AppendLine("version=" + VERSION);
    sb.AppendLine("core_found=" + BoolText(coreFound));
    sb.AppendLine("core_enabled=" + BoolText(coreEnabled));
    sb.AppendLine("power_enabled=" + BoolText(powerEnabled));
    sb.AppendLine("global_pause=" + BoolText(globalPause));
    sb.AppendLine("state=" + CurrentState());
    sb.AppendLine("profile=" + profile.Name);
    sb.AppendLine("profiles=" + profiles.Count);
    sb.AppendLine("batteries=" + stats.Batteries);
    sb.AppendLine("battery_percent=" + batteryPct.ToString("0.0"));
    sb.AppendLine("stored_mwh=" + stats.Stored.ToString("0.000"));
    sb.AppendLine("capacity_mwh=" + stats.Capacity.ToString("0.000"));
    sb.AppendLine("input_mw=" + stats.Input.ToString("0.000"));
    sb.AppendLine("output_mw=" + stats.Output.ToString("0.000"));
    sb.AppendLine("max_output_mw=" + stats.MaxOutput.ToString("0.000"));
    sb.AppendLine("output_percent=" + outputPct.ToString("0.0"));
    sb.AppendLine("producers=" + stats.Producers);
    sb.AppendLine("reactors=" + stats.Reactors);
    sb.AppendLine("solar=" + stats.Solar);
    sb.AppendLine("wind=" + stats.Wind);
    sb.AppendLine("hydrogen=" + stats.Hydrogen);
    sb.AppendLine("warning=" + warning);
    Me.CustomData = UpsertSectionText(Me.CustomData, "[PowerState]", sb.ToString());
}

private string UpsertSectionText(string raw, string header, string content)
{
    raw = raw ?? "";
    int start = raw.IndexOf(header, SC);
    if (start < 0)
    {
        if (raw.Trim().Length == 0) return content.TrimEnd();
        return raw.TrimEnd() + "\n\n" + content.TrimEnd();
    }
    int next = raw.IndexOf("\n[", start + header.Length, SC);
    if (next < 0) return raw.Substring(0, start).TrimEnd() + "\n\n" + content.TrimEnd();
    return raw.Substring(0, start).TrimEnd() + "\n\n" + content.TrimEnd() + "\n\n" + raw.Substring(next + 1).TrimStart();
}

private void DrawModuleBoot(IMyTextSurface surface, double progress)
{
    if (surface == null) return;
    surface.ContentType = ContentType.SCRIPT;
    surface.Script = "";
    surface.Font = "Monospace";
    surface.FontSize = 1.0f;
    surface.TextPadding = 1f;
    RectangleF vp = new RectangleF((surface.TextureSize - surface.SurfaceSize) * 0.5f, surface.SurfaceSize);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 12f);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f);
        DrawText(frame, "AGM - Power", panel.Position + new Vector2(panel.Width * 0.5f, panel.Height * 0.35f), COLOR_ACCENT_2, 0.82f, TextAlignment.CENTER);
        DrawText(frame, "LOADING", panel.Position + new Vector2(panel.Width * 0.5f, panel.Height * 0.48f), COLOR_OK, 0.44f, TextAlignment.CENTER);
        RectangleF bar = new RectangleF(panel.X + 32f, panel.Y + panel.Height * 0.62f, panel.Width - 64f, 10f);
        Fill(frame, bar, COLOR_PANEL);
        Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)progress, bar.Height), COLOR_ACCENT);
        DrawBorder(frame, bar, COLOR_DIM, 1f);
    }
}

private void DrawRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string value, Color valueColor)
{
    RectangleF row = new RectangleF(panel.X + 16f, y - 4f, panel.Width - 32f, 26f);
    Fill(frame, row, new Color(105, 73, 29));
    DrawText(frame, label, new Vector2(row.X + 10f, row.Y + 4f), COLOR_TEXT, 0.46f, TextAlignment.LEFT);
    DrawText(frame, value, new Vector2(row.Right - 10f, row.Y + 4f), valueColor, 0.46f, TextAlignment.RIGHT);
}

private Color StateColor()
{
    if (lastStatus == "ONLINE") return COLOR_OK;
    if (lastStatus == "PAUSED") return COLOR_WARN;
    return COLOR_BAD;
}

private Color BatteryColor(double pct)
{
    if (pct < 0.25) return COLOR_BAD;
    if (pct < 0.50) return COLOR_WARN;
    return COLOR_OK;
}

private Color OutputColor(double pct)
{
    if (pct > 0.90) return COLOR_BAD;
    if (pct > 0.75) return COLOR_WARN;
    return COLOR_OK;
}

private string Percent(double ratio)
{
    return (ratio * 100.0).ToString("0.0") + "%";
}

private string FormatPower(double mw)
{
    double watts = mw * 1000000.0;
    if (watts >= 1000000000.0) return (watts / 1000000000.0).ToString("0.##") + "G";
    if (watts >= 1000000.0) return (watts / 1000000.0).ToString("0.##") + "M";
    if (watts >= 1000.0) return (watts / 1000.0).ToString("0.##") + "k";
    return watts.ToString("0");
}

private void EchoStatus()
{
    Echo("AGM Power");
    Echo("Version: " + VERSION);
    Echo("Core   : " + (coreFound ? "FOUND" : "missing"));
    Echo("State  : " + CurrentState());
    Echo("Profiles: " + profiles.Count);
}

private string StripComment(string line)
{
    int idx = line.IndexOf("//");
    if (idx >= 0) return line.Substring(0, idx);
    return line;
}

private bool TrySplitKeyValue(string line, char split, out string key, out string value)
{
    key = "";
    value = "";
    int idx = line.IndexOf(split);
    if (idx < 0) return false;
    key = line.Substring(0, idx).Trim();
    value = line.Substring(idx + 1).Trim();
    return key.Length > 0;
}

private bool ParseBool(string text, bool fallback)
{
    if (string.IsNullOrEmpty(text)) return fallback;
    text = text.Trim();
    if (text.Equals("true", SC) || text.Equals("on", SC) || text.Equals("yes", SC) || text.Equals("1")) return true;
    if (text.Equals("false", SC) || text.Equals("off", SC) || text.Equals("no", SC) || text.Equals("0")) return false;
    return fallback;
}

private string BoolText(bool value)
{
    return value ? "true" : "false";
}

private RectangleF Inset(RectangleF rect, float amount)
{
    return new RectangleF(rect.X + amount, rect.Y + amount, rect.Width - amount * 2f, rect.Height - amount * 2f);
}

private void Fill(MySpriteDrawFrame frame, RectangleF rect, Color color)
{
    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", rect.Position + rect.Size * 0.5f, rect.Size, color));
}

private void DrawBorder(MySpriteDrawFrame frame, RectangleF rect, Color color, float width)
{
    Fill(frame, new RectangleF(rect.X, rect.Y, rect.Width, width), color);
    Fill(frame, new RectangleF(rect.X, rect.Bottom - width, rect.Width, width), color);
    Fill(frame, new RectangleF(rect.X, rect.Y, width, rect.Height), color);
    Fill(frame, new RectangleF(rect.Right - width, rect.Y, width, rect.Height), color);
}

private void DrawText(MySpriteDrawFrame frame, string text, Vector2 position, Color color, float scale, TextAlignment align)
{
    frame.Add(new MySprite(SpriteType.TEXT, text, position, null, color, "Monospace", align, scale));
}
