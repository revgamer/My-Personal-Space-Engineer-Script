private const string VERSION = "1.0-core";
private const string CORE_TAG = "{AGM-Core}";
private const string SCREEN_TAG = "[AGM-S]";
private const string STATE_HEADER = "[CoreState]";
private static readonly StringComparison SC = StringComparison.OrdinalIgnoreCase;
private readonly Color COLOR_BG = new Color(1, 8, 13);
private readonly Color COLOR_PANEL = new Color(2, 18, 28);
private readonly Color COLOR_PANEL_2 = new Color(3, 58, 78);
private readonly Color COLOR_ACCENT = new Color(38, 239, 255);
private readonly Color COLOR_ACCENT_2 = new Color(112, 247, 255);
private readonly Color COLOR_TEXT = new Color(126, 246, 255);
private readonly Color COLOR_DIM = new Color(44, 177, 195);
private readonly Color COLOR_ROW_TEXT = new Color(126, 246, 255);
private readonly Color COLOR_ROW_DIM = new Color(63, 207, 222);
private readonly Color COLOR_OK = new Color(97, 255, 214);
private readonly Color COLOR_WARN = new Color(255, 202, 34);
private readonly Color COLOR_BAD = new Color(255, 79, 66);
private readonly Color COLOR_PROGRESS_BG = new Color(18, 48, 32);
private readonly Color COLOR_PROGRESS_FILL = new Color(255, 204, 36);

private class ModuleInfo
{
    public string Key;
    public string Label;
    public string Tag;
    public string ConfigName;
    public bool Enabled;
    public IMyProgrammableBlock Block;
    public string Status;
}

private class StockEntry
{
    public string Category;
    public string Name;
    public string Icon;
    public double Amount;
}

private readonly List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
private readonly List<IMyTerminalBlock> screens = new List<IMyTerminalBlock>();
private readonly List<ModuleInfo> modules = new List<ModuleInfo>();
private readonly List<MyInventoryItem> inventoryItems = new List<MyInventoryItem>();
private readonly List<IMyAirVent> interiorVents = new List<IMyAirVent>();
private readonly List<StockEntry> stockEntries = new List<StockEntry>();
private readonly Dictionary<string, StockEntry> stockByKey = new Dictionary<string, StockEntry>(StringComparer.OrdinalIgnoreCase);
private readonly StringBuilder sb = new StringBuilder();

private bool coreEnabled = true;
private bool globalPause = false;
private bool includeDockedGrids = false;
private string noSortingTag = "[No Sorting]";
private string lockedTag = "{Locked}";
private string manualTag = "{Manual}";
private string hiddenTag = "{Hidden}";
private bool booting = true;
private bool moduleBootTriggered = false;
private double bootElapsed = 0.0;
private DateTime lastRun = DateTime.Now;

// --- tick splitting ---
// Update100 fires every ~100 ticks (~1.6s): handles all heavy work (scan, cache, state)
// Update10 fires every 10 ticks (~0.16s): draws 2 screens per call, staggered via _drawTick
// _drawTick 0  = PB status screen only
// _drawTick 1+ = 2 external screens per tick (covers up to 38 screens across ticks 1-19)
private int _drawTick = 0;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
    InitModules();
    EnsureConfig();
    Reload();
    stockByKey.Clear();
    stockEntries.Clear();
    BuildStockCache();
}

public void Save()
{
    WriteState();
}

public void Main(string argument, UpdateType updateSource)
{
    string arg = argument == null ? "" : argument.Trim();
    if (arg.Length > 0)
        HandleArgument(arg);

    // --- Update100: all heavy work, no drawing ---
    if ((updateSource & UpdateType.Update100) != 0)
    {
        Reload();
        stockByKey.Clear();
        stockEntries.Clear();
        BuildStockCache();
        WriteState();
        EchoStatus();
        return;
    }

    // --- Update10: drawing only ---
    if ((updateSource & UpdateType.Update10) == 0) return;

    if (booting)
    {
        bootElapsed += Math.Max(0.1, (DateTime.Now - lastRun).TotalSeconds);
        lastRun = DateTime.Now;
        double progress = Math.Min(1.0, bootElapsed / 4.0);
        if (!moduleBootTriggered)
        {
            TriggerModuleReboot();
            moduleBootTriggered = true;
        }
        DrawModuleBoot(Me.GetSurface(0), "AGM - Core", progress);
        DrawWallBootScreens(progress);
        if (bootElapsed >= 4.0) booting = false;
        return;
    }
    lastRun = DateTime.Now;

    _drawTick++;
    if (_drawTick > 20) _drawTick = 0;

    // Tick 0: PB status screen only
    if (_drawTick == 0)
    {
        DrawCorePbStatus();
        return;
    }

    // Ticks 1-19: draw 2 external screens per tick
    // Covers up to 38 external screens per full cycle (~3.2s total for full wall refresh)
    int baseIndex = (_drawTick - 1) * 2;
    if (baseIndex < screens.Count)
        DrawSingleScreen(screens[baseIndex]);
    if (baseIndex + 1 < screens.Count)
        DrawSingleScreen(screens[baseIndex + 1]);
}

// Draws one external screen — extracted from old DrawScreens() loop
private void DrawSingleScreen(IMyTerminalBlock block)
{
    var provider = block as IMyTextSurfaceProvider;
    if (provider == null || provider.SurfaceCount <= 0) return;
    IMyTextSurface surface = provider.GetSurface(0);
    if (!HasDashboardCommand(block))
    {
        DrawWaitingForCommand(surface);
        return;
    }
    string stockKind = StockDashboardKind(block);
    if (WantsFuelLifeSupport(block))
        DrawFuelLifeSupportDashboard(surface);
    else if (WantsAutocraftingDashboard(block))
        DrawAutocraftingDashboard(surface, DashboardPage(block, "Autocrafting"));
    else if (stockKind.Length > 0)
        DrawStockDashboard(surface, stockKind, StockDashboardPage(block, stockKind));
    else if (WantsPowerDashboard(block))
        DrawPowerDashboard(surface);
    else if (WantsLogisticsDashboard(block))
        DrawLogisticsDashboard(surface);
    else if (WantsProductionDashboard(block))
        DrawProductionDashboard(surface);
    else
        DrawCoreDashboard(surface);
}

private void InitModules()
{
    modules.Clear();
    AddModule("power", "Power", "{AGM-Power}", true);
    AddModule("logistics", "Logistics", "{AGM-Logistics}", true);
    AddModule("production", "Production", "{AGM-Production}", true);
}

private void AddModule(string key, string label, string tag, bool enabled)
{
    modules.Add(new ModuleInfo { Key = key, Label = label, Tag = tag, ConfigName = "", Enabled = enabled, Status = "missing" });
}

private void HandleArgument(string arg)
{
    if (arg.Equals("reboot", SC) || arg.Equals("boot", SC))
    {
        EnsureConfig();
        Reload();
        booting = true;
        moduleBootTriggered = false;
        bootElapsed = 0.0;
        lastRun = DateTime.Now;
        return;
    }
    if (arg.Equals("reload", SC) || arg.Equals("rescan", SC))
    {
        EnsureConfig();
        Reload();
        return;
    }
    if (arg.Equals("pause", SC))
    {
        globalPause = true;
        WriteCoreValue("global_pause", "true");
        Reload();
        return;
    }
    if (arg.Equals("resume", SC))
    {
        globalPause = false;
        WriteCoreValue("global_pause", "false");
        Reload();
        return;
    }
    string key, value;
    if (TrySplitKeyValue(arg, ' ', out key, out value) || TrySplitKeyValue(arg, '=', out key, out value))
    {
        for (int i = 0; i < modules.Count; i++)
        {
            if (!key.Equals(modules[i].Key, SC))
                continue;
            bool enabled = ParseBool(value, modules[i].Enabled);
            WriteCoreValue(modules[i].Key, enabled ? "true" : "false");
            Reload();
            return;
        }
    }
}

private void TriggerModuleReboot()
{
    ResolveModules();
    for (int i = 0; i < modules.Count; i++)
    {
        ModuleInfo m = modules[i];
        if (!m.Enabled || m.Block == null || m.Block == Me) continue;
        m.Block.TryRun("reboot");
    }
}

private void EnsureConfig()
{
    if (!string.IsNullOrWhiteSpace(Me.CustomData))
        return;
    Me.CustomData =
@"[Core]
enabled=true
power=true
logistics=true
production=true
global_pause=false
include_docked_grids=false
no_sorting_tag=[No Sorting]
locked_tag={Locked}
manual_tag={Manual}
hidden_tag={Hidden}

[Modules]
power=PB AutoGrid Manager Power {AGM-Power}
logistics=PB AutoGrid Manager Logistics {AGM-Logistics}
production=PB AutoGrid Manager Production {AGM-Production}";
}

private void Reload()
{
    ReadConfig();
    ScanBlocks();
    ResolveModules();
    WriteState();
}

private void ReadConfig()
{
    coreEnabled = true;
    globalPause = false;
    includeDockedGrids = false;
    noSortingTag = "[No Sorting]";
    lockedTag = "{Locked}";
    manualTag = "{Manual}";
    hiddenTag = "{Hidden}";
    for (int i = 0; i < modules.Count; i++)
    {
        modules[i].Enabled = true;
        modules[i].ConfigName = "";
    }

    string[] lines = Me.CustomData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    string section = "";
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.Length == 0) continue;
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            section = line.Substring(1, line.Length - 2).Trim();
            continue;
        }
        string key, value;
        if (!TrySplitKeyValue(line, '=', out key, out value)) continue;
        if (section.Equals("Core", SC))
            ReadCoreValue(key, value);
        else if (section.Equals("Modules", SC))
            ReadModuleValue(key, value);
    }
}

private void ReadCoreValue(string key, string value)
{
    if (key.Equals("enabled", SC)) coreEnabled = ParseBool(value, true);
    else if (key.Equals("global_pause", SC)) globalPause = ParseBool(value, false);
    else if (key.Equals("include_docked_grids", SC)) includeDockedGrids = ParseBool(value, false);
    else if (key.Equals("no_sorting_tag", SC)) noSortingTag = value.Trim();
    else if (key.Equals("locked_tag", SC)) lockedTag = value.Trim();
    else if (key.Equals("manual_tag", SC)) manualTag = value.Trim();
    else if (key.Equals("hidden_tag", SC)) hiddenTag = value.Trim();
    else
    {
        for (int i = 0; i < modules.Count; i++)
            if (key.Equals(modules[i].Key, SC))
                modules[i].Enabled = ParseBool(value, modules[i].Enabled);
    }
}

private void ReadModuleValue(string key, string value)
{
    for (int i = 0; i < modules.Count; i++)
        if (key.Equals(modules[i].Key, SC))
            modules[i].ConfigName = value.Trim();
}

private void ScanBlocks()
{
    blocks.Clear();
    screens.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, b => b.IsSameConstructAs(Me));
    for (int i = 0; i < blocks.Count; i++)
    {
        IMyTerminalBlock block = blocks[i];
        if (block == null) continue;
        if (block == Me) continue;
        if ((block.CustomName.IndexOf(SCREEN_TAG, SC) >= 0 || HasDashboardCommand(block)) && block is IMyTextSurfaceProvider)
        {
            var provider = block as IMyTextSurfaceProvider;
            if (provider.SurfaceCount > 0)
                screens.Add(block);
        }
    }
}

private void ResolveModules()
{
    for (int i = 0; i < modules.Count; i++)
    {
        ModuleInfo m = modules[i];
        m.Block = null;
        for (int b = 0; b < blocks.Count; b++)
        {
            IMyProgrammableBlock pb = blocks[b] as IMyProgrammableBlock;
            if (pb == null || pb == Me) continue;
            if (m.ConfigName.Length > 0 && pb.CustomName.Equals(m.ConfigName, SC))
            {
                m.Block = pb;
                break;
            }
            if (pb.CustomName.IndexOf(m.Tag, SC) >= 0)
                m.Block = pb;
        }
        m.Status = ModuleStatus(m);
    }
}

private string ModuleStatus(ModuleInfo m)
{
    if (!m.Enabled) return "disabled";
    if (globalPause && (m.Key.Equals("power", SC) || m.Key.Equals("logistics", SC) || m.Key.Equals("production", SC))) return "paused";
    if (m.Block == null) return "missing";
    if (!m.Block.IsFunctional) return "damaged";
    if (!m.Block.Enabled) return "off";
    if (!m.Block.IsWorking) return "not working";
    return "online";
}

private void WriteState()
{
    sb.Clear();
    sb.AppendLine(STATE_HEADER);
    sb.AppendLine("version=" + VERSION);
    sb.AppendLine("core_enabled=" + BoolText(coreEnabled));
    sb.AppendLine("global_pause=" + BoolText(globalPause));
    sb.AppendLine("include_docked_grids=" + BoolText(includeDockedGrids));
    sb.AppendLine("no_sorting_tag=" + noSortingTag);
    sb.AppendLine("locked_tag=" + lockedTag);
    sb.AppendLine("manual_tag=" + manualTag);
    sb.AppendLine("hidden_tag=" + hiddenTag);
    for (int i = 0; i < modules.Count; i++)
    {
        ModuleInfo m = modules[i];
        sb.AppendLine(m.Key + "_enabled=" + BoolText(m.Enabled));
        sb.AppendLine(m.Key + "_status=" + m.Status);
        sb.AppendLine(m.Key + "_found=" + BoolText(m.Block != null));
    }
    Storage = sb.ToString();
}

private void DrawWallBootScreens(double progress)
{
    for (int i = 0; i < screens.Count; i++)
    {
        IMyTerminalBlock block = screens[i];
        var provider = block as IMyTextSurfaceProvider;
        if (provider == null || provider.SurfaceCount <= 0) continue;
        DrawModuleBoot(provider.GetSurface(0), "AGM SYSTEM", progress);
    }
}

private bool HasDashboardCommand(IMyTerminalBlock block)
{
    return WantsCoreDashboard(block) || WantsPowerDashboard(block) || WantsLogisticsDashboard(block) || WantsProductionDashboard(block) || WantsStockDashboard(block) || WantsAutocraftingDashboard(block) || WantsFuelLifeSupport(block);
}

private bool WantsCoreDashboard(IMyTerminalBlock block)
{
    string data = block.CustomData == null ? "" : block.CustomData;
    return data.IndexOf("CoreDashboard", SC) >= 0 || data.IndexOf("AGM-Core", SC) >= 0;
}

private bool WantsPowerDashboard(IMyTerminalBlock block)
{
    string data = block.CustomData == null ? "" : block.CustomData;
    return data.IndexOf("PowerDashboard", SC) >= 0 || data.IndexOf("AGM-Power", SC) >= 0;
}

private bool WantsLogisticsDashboard(IMyTerminalBlock block)
{
    string data = block.CustomData == null ? "" : block.CustomData;
    return data.IndexOf("LogisticsDashboard", SC) >= 0 || data.IndexOf("SorterDashboard", SC) >= 0;
}

private bool WantsProductionDashboard(IMyTerminalBlock block)
{
    string data = block.CustomData == null ? "" : block.CustomData;
    return data.IndexOf("ProductionDashboard", SC) >= 0;
}

private bool WantsStockDashboard(IMyTerminalBlock block)
{
    return StockDashboardKind(block).Length > 0;
}

private string StockDashboardKind(IMyTerminalBlock block)
{
    string data = block.CustomData == null ? "" : block.CustomData;
    if (data.IndexOf("InventoryStock", SC) >= 0 || data.IndexOf("Inventory Stock", SC) >= 0) return "Inventory";
    if (data.IndexOf("OreStock", SC) >= 0 || data.IndexOf("Ore Stock", SC) >= 0) return "Ore";
    if (data.IndexOf("IngotStock", SC) >= 0 || data.IndexOf("Ingot Stock", SC) >= 0) return "Ingot";
    if (data.IndexOf("ComponentStock", SC) >= 0 || data.IndexOf("Component Stock", SC) >= 0) return "Component";
    if (data.IndexOf("AmmoStock", SC) >= 0 || data.IndexOf("Ammo Stock", SC) >= 0) return "Ammo";
    if (data.IndexOf("ToolStock", SC) >= 0 || data.IndexOf("Tool Stock", SC) >= 0) return "Tool";
    if (data.IndexOf("BottleStock", SC) >= 0 || data.IndexOf("Bottle Stock", SC) >= 0) return "Bottle";
    return "";
}

private int StockDashboardPage(IMyTerminalBlock block, string kind)
{
    string data = block.CustomData == null ? "" : block.CustomData;
    string[] lines = data.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    string compact = kind.Equals("Inventory", SC) ? "InventoryStock" : kind + "Stock";
    string spaced = kind.Equals("Inventory", SC) ? "Inventory Stock" : kind + " Stock";
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.IndexOf(compact, SC) < 0 && line.IndexOf(spaced, SC) < 0) continue;
        int page = FirstNumber(line);
        if (page > 0) return page;
    }
    return 1;
}

private bool WantsAutocraftingDashboard(IMyTerminalBlock block)
{
    string data = block.CustomData == null ? "" : block.CustomData;
    return data.IndexOf("Autocrafting", SC) >= 0 || data.IndexOf("AutoCrafting", SC) >= 0 || data.IndexOf("AutocraftingDashboard", SC) >= 0;
}

private bool WantsFuelLifeSupport(IMyTerminalBlock block)
{
    string data = block.CustomData == null ? "" : block.CustomData;
    return data.IndexOf("FuelLifeSupport", SC) >= 0 || data.IndexOf("LifeSupport", SC) >= 0 || data.IndexOf("Fuel & Life Support", SC) >= 0;
}

private int DashboardPage(IMyTerminalBlock block, string command)
{
    string data = block.CustomData == null ? "" : block.CustomData;
    string[] lines = data.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.IndexOf(command, SC) < 0) continue;
        int page = FirstNumber(line);
        if (page > 0) return page;
    }
    return 1;
}

private void DrawPowerDashboard(IMyTextSurface surface)
{
    if (surface == null) return;
    Dictionary<string, string> state = ReadModuleState("{AGM-Power}", "[PowerState]");
    string status = GetState(state, "state", "MISSING");
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
        DrawText(frame, "POWER DASHBOARD", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 0.78f, TextAlignment.LEFT);
        DrawText(frame, state.Count > 0 ? "LINK ONLINE" : "NO POWER", panel.Position + new Vector2(panel.Width - 24f, 28f), state.Count > 0 ? COLOR_OK : COLOR_BAD, 0.44f, TextAlignment.RIGHT);
        float y = panel.Y + 72f;
        DrawCoreInfoRow(frame, panel, y, "State", status, ModuleColor(status)); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Profile", TrimText(GetState(state, "profile", "-"), 24), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Batteries", GetState(state, "batteries", "0") + " blocks | " + GetState(state, "battery_percent", "0") + "%", COLOR_ACCENT_2); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Stored", GetState(state, "stored_mwh", "0") + " MWh", COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Capacity", GetState(state, "capacity_mwh", "0") + " MWh", COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Input", GetState(state, "input_mw", "0") + " MW", COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Output", GetState(state, "output_mw", "0") + " / " + GetState(state, "max_output_mw", "0") + " MW", COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Load", GetState(state, "output_percent", "0") + "%", COLOR_ACCENT_2); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Reactors", GetState(state, "reactors", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Solar/Wind", GetState(state, "solar", "0") + " / " + GetState(state, "wind", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Hydrogen", GetState(state, "hydrogen", "0"), COLOR_TEXT);
        DrawText(frame, "Data from AGM Power", new Vector2(panel.X + 24f, panel.Bottom - 24f), COLOR_DIM, 0.44f, TextAlignment.LEFT);
    }
}

private void DrawLogisticsDashboard(IMyTextSurface surface)
{
    if (surface == null) return;
    Dictionary<string, string> state = ReadModuleState("{AGM-Logistics}", "[LogisticsState]");
    string status = GetState(state, "state", "MISSING");
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
        DrawText(frame, "LOGISTICS DASHBOARD", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 0.72f, TextAlignment.LEFT);
        DrawText(frame, state.Count > 0 ? "LINK ONLINE" : "NO LOGISTICS", panel.Position + new Vector2(panel.Width - 24f, 28f), state.Count > 0 ? COLOR_OK : COLOR_BAD, 0.40f, TextAlignment.RIGHT);
        float y = panel.Y + 72f;
        DrawCoreInfoRow(frame, panel, y, "State", status, ModuleColor(status)); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Cargo", GetState(state, "cargo", "0") + " containers", COLOR_ACCENT_2); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Sources", GetState(state, "sources", "0") + " inventories", COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Ore/Ingot", GetState(state, "ore", "0") + " / " + GetState(state, "ingot", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Component", GetState(state, "component", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Ammo/Tool", GetState(state, "ammo", "0") + " / " + GetState(state, "tool", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Bottle", GetState(state, "bottle", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Moved/run", GetState(state, "moves", "0"), COLOR_ACCENT_2); y += 32f;
        DrawFullInfoRow(frame, panel, y, "Last item", GetState(state, "last_item", "-"), COLOR_TEXT); y += 44f;
        DrawFullInfoRow(frame, panel, y, "From", GetState(state, "last_from", "-"), COLOR_TEXT); y += 44f;
        DrawFullInfoRow(frame, panel, y, "To", GetState(state, "last_to", "-"), COLOR_TEXT);
        DrawText(frame, "Data from AGM Logistics", new Vector2(panel.X + 24f, panel.Bottom - 24f), COLOR_DIM, 0.44f, TextAlignment.LEFT);
    }
}

private void DrawProductionDashboard(IMyTextSurface surface)
{
    if (surface == null) return;
    Dictionary<string, string> state = ReadModuleState("{AGM-Production}", "[ProductionState]");
    string status = GetState(state, "state", "MISSING");
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
        DrawText(frame, "PRODUCTION DASHBOARD", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 0.68f, TextAlignment.LEFT);
        DrawText(frame, state.Count > 0 ? "LINK ONLINE" : "NO PRODUCTION", panel.Position + new Vector2(panel.Width - 24f, 28f), state.Count > 0 ? COLOR_OK : COLOR_BAD, 0.40f, TextAlignment.RIGHT);
        float y = panel.Y + 72f;
        DrawCoreInfoRow(frame, panel, y, "State", status, ModuleColor(status)); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Mode", GetState(state, "mode", "monitor"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Assemblers", GetState(state, "assemblers", "0") + " total", COLOR_ACCENT_2); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Asm online", GetState(state, "assemblers_online", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Asm producing", GetState(state, "assemblers_producing", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Queued mach", GetState(state, "queued_machines", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Queued items", GetState(state, "queued_items", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Refineries", GetState(state, "refineries", "0") + " total", COLOR_ACCENT_2); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Ref online", GetState(state, "refineries_online", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Ref producing", GetState(state, "refineries_producing", "0"), COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Ref input", GetState(state, "refinery_input_percent", "0") + "%", COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Autocraft", GetState(state, "queued_last_run", "0") + " queued", COLOR_TEXT); y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Queue moves", GetState(state, "assembler_moves", "0") + " asm / " + GetState(state, "refinery_moves", "0") + " ref", COLOR_TEXT);
        DrawText(frame, "Data from AGM Production", new Vector2(panel.X + 24f, panel.Bottom - 24f), COLOR_DIM, 0.44f, TextAlignment.LEFT);
    }
}

private void DrawStockDashboard(IMyTextSurface surface, string category, int page)
{
    if (surface == null) return;
    BuildStockCache();
    List<StockEntry> filtered = new List<StockEntry>();
    for (int i = 0; i < stockEntries.Count; i++)
        if (category.Equals("Inventory", SC) || stockEntries[i].Category.Equals(category, SC))
            filtered.Add(stockEntries[i]);
    filtered.Sort((a, b) =>
    {
        int c = a.Category.CompareTo(b.Category);
        return c != 0 ? c : a.Name.CompareTo(b.Name);
    });

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

        int rows = Math.Max(1, (int)((panel.Height - 116f) / 34f));
        int pages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)rows));
        if (page < 1) page = 1;
        if (page > pages) page = pages;
        int start = (page - 1) * rows;
        int end = Math.Min(filtered.Count, start + rows);

        DrawText(frame, category.ToUpperInvariant() + " STOCK", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, category.Equals("Inventory", SC) ? 0.82f : 0.95f, TextAlignment.LEFT);
        DrawText(frame, "PAGE " + page + "/" + pages + "  ITEMS " + filtered.Count, panel.Position + new Vector2(panel.Width - 24f, 28f), COLOR_DIM, 0.46f, TextAlignment.RIGHT);

        if (filtered.Count == 0)
        {
            DrawText(frame, "NO " + category.ToUpperInvariant() + " ITEMS FOUND", panel.Position + panel.Size * 0.5f + new Vector2(0f, -10f), COLOR_WARN, 0.68f, TextAlignment.CENTER);
            DrawText(frame, "Check Logistics/Hidden/No Sorting tags", new Vector2(panel.X + panel.Width * 0.5f, panel.Bottom - 24f), COLOR_DIM, 0.38f, TextAlignment.CENTER);
            return;
        }

        double maxOnPage = 1;
        for (int i = start; i < end; i++)
        {
            double quota = StockQuota(filtered[i]);
            if (quota > maxOnPage) maxOnPage = quota;
        }

        float y = panel.Y + 70f;
        for (int i = start; i < end; i++)
        {
            DrawStockRow(frame, panel, y, filtered[i], maxOnPage);
            y += 34f;
        }

        DrawText(frame, "Data from grid inventories", new Vector2(panel.X + 24f, panel.Bottom - 24f), COLOR_DIM, 0.38f, TextAlignment.LEFT);
    }
}

private void DrawAutocraftingDashboard(IMyTextSurface surface, int page)
{
    if (surface == null) return;
    BuildStockCache();
    Dictionary<string, string> state = ReadModuleState("{AGM-Production}", "[ProductionState]");
    Dictionary<string, double> quotas = ReadProductionQuotas();
    List<string> names = new List<string>(quotas.Keys);
    names.Sort();

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

        int rows = Math.Max(1, (int)((panel.Height - 128f) / 34f));
        int pages = Math.Max(1, (int)Math.Ceiling(names.Count / (double)rows));
        if (page < 1) page = 1;
        if (page > pages) page = pages;
        int start = (page - 1) * rows;
        int end = Math.Min(names.Count, start + rows);

        DrawText(frame, "AUTOCRAFTING", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 0.95f, TextAlignment.LEFT);
        DrawText(frame, "COMPONENT", panel.Position + new Vector2(panel.Width - 24f, 28f), COLOR_DIM, 0.50f, TextAlignment.RIGHT);
        DrawText(frame, GetState(state, "state", "MISSING") + " | PAGE " + page + "/" + pages, panel.Position + new Vector2(24f, 56f), ModuleColor(GetState(state, "state", "missing")), 0.44f, TextAlignment.LEFT);

        if (names.Count == 0)
        {
            DrawText(frame, "NO COMPONENT QUOTAS FOUND", panel.Position + panel.Size * 0.5f + new Vector2(0f, -10f), COLOR_WARN, 0.62f, TextAlignment.CENTER);
            DrawText(frame, "Add [ComponentQuotas] in AGM Production", new Vector2(panel.X + panel.Width * 0.5f, panel.Bottom - 24f), COLOR_DIM, 0.38f, TextAlignment.CENTER);
            return;
        }

        float y = panel.Y + 86f;
        for (int i = start; i < end; i++)
        {
            string item = names[i];
            double quota = quotas[item];
            double stock = StateDouble(state, "stock_" + item, StockAmount("Component", item));
            DrawQuotaRow(frame, panel, y, item, SplitName(item), stock, quota);
            y += 34f;
        }
        DrawText(frame, GetState(state, "queued_last_run", "0") + " queued | " + GetState(state, "assemblers_online", "0") + " assemblers", new Vector2(panel.X + 24f, panel.Bottom - 24f), COLOR_DIM, 0.38f, TextAlignment.LEFT);
    }
}

private Dictionary<string, double> ReadProductionQuotas()
{
    Dictionary<string, double> result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
    IMyProgrammableBlock pb = null;
    for (int i = 0; i < blocks.Count; i++)
    {
        pb = blocks[i] as IMyProgrammableBlock;
        if (pb != null && pb.CustomName.IndexOf("{AGM-Production}", SC) >= 0) break;
        pb = null;
    }
    if (pb == null) return result;
    Dictionary<string, string> state = ReadModuleState("{AGM-Production}", "[ProductionState]");
    foreach (var entry in state)
    {
        if (!entry.Key.StartsWith("quota_", SC)) continue;
        double amount;
        if (double.TryParse(entry.Value, out amount) && amount > 0)
            result[entry.Key.Substring(6)] = amount;
    }
    if (result.Count > 0) return result;
    string[] lines = (pb.CustomData ?? "").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    bool inQuotas = false;
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            inQuotas = line.Equals("[ComponentQuotas]", SC);
            continue;
        }
        string autoKey, autoValue;
        if (TrySplitKeyValue(line, '=', out autoKey, out autoValue) && autoKey.Equals("AutoCrafting", SC))
        {
            inQuotas = autoValue.Trim().Equals("Component", SC);
            continue;
        }
        if (!inQuotas) continue;
        string key, value;
        double amount;
        if (TrySplitKeyValue(line, '=', out key, out value) && double.TryParse(value, out amount) && amount > 0)
            result[key.Trim()] = amount;
    }
    return result;
}

private double StateDouble(Dictionary<string, string> state, string key, double fallback)
{
    string value;
    double parsed;
    if (state.TryGetValue(key, out value) && double.TryParse(value, out parsed))
        return parsed;
    return fallback;
}

private void DrawFuelLifeSupportDashboard(IMyTextSurface surface)
{
    if (surface == null) return;
    BuildStockCache();
    double h2Now = 0, h2Max = 0, o2Now = 0, o2Max = 0, genIce = 0;
    int h2Count = 0, o2Count = 0, genCount = 0, genOnline = 0, genWorking = 0;
    int ventOk = 0, ventLeak = 0;
    string leaks = "";
    ScanLifeSupport(ref h2Now, ref h2Max, ref h2Count, ref o2Now, ref o2Max, ref o2Count, ref genIce, ref genCount, ref genOnline, ref genWorking, ref ventOk, ref ventLeak, ref leaks);

    double iceStock = StockAmount("Ore", "Ice");
    double o2Bottles = StockAmount("Bottle", "OxygenBottle");
    double h2Bottles = StockAmount("Bottle", "HydrogenBottle");
    bool pressurized = interiorVents.Count > 0 && ventLeak == 0;

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
        DrawText(frame, "FUEL & LIFE SUPPORT", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 0.82f, TextAlignment.LEFT);
        DrawText(frame, pressurized ? "BASE OK" : "BASE X", panel.Position + new Vector2(panel.Width - 24f, 28f), pressurized ? COLOR_OK : COLOR_BAD, 0.48f, TextAlignment.RIGHT);
        float y = panel.Y + 64f;
        DrawTankRow(frame, panel, y, "H2 Hydrogen", h2Now, h2Max, h2Count); y += 58f;
        DrawTankRow(frame, panel, y, "O2 Oxygen", o2Now, o2Max, o2Count); y += 62f;
        DrawFuelInfoRow(frame, panel, y, "O2/H2 Generators", genWorking + " working | " + genOnline + " online | " + genCount + " total", COLOR_TEXT); y += 32f;
        DrawFuelInfoRow(frame, panel, y, "Ice in generators", FormatAmount(genIce), COLOR_ACCENT_2); y += 32f;
        DrawFuelInfoRow(frame, panel, y, "Ice stock", FormatAmount(iceStock), COLOR_ACCENT_2); y += 32f;
        DrawFuelInfoRow(frame, panel, y, "Bottles", "O2 " + FormatAmount(o2Bottles) + " | H2 " + FormatAmount(h2Bottles), COLOR_TEXT); y += 42f;
        DrawText(frame, "PRESSURIZATION", new Vector2(panel.X + 24f, y), COLOR_ACCENT_2, 0.54f, TextAlignment.LEFT);
        DrawText(frame, pressurized ? "OK Base Pressurized" : "X Base Not Pressurized", new Vector2(panel.Right - 24f, y), pressurized ? COLOR_OK : COLOR_BAD, 0.42f, TextAlignment.RIGHT);
        y += 30f;
        DrawFuelInfoRow(frame, panel, y, "Air vents", ventOk + " OK | " + ventLeak + " leaking", ventLeak > 0 ? COLOR_BAD : COLOR_OK); y += 32f;
        if (ventLeak > 0)
            DrawFullInfoRow(frame, panel, y, "Leak", leaks, COLOR_BAD);
        DrawText(frame, interiorVents.Count + " vents monitored", new Vector2(panel.X + 24f, panel.Bottom - 24f), COLOR_DIM, 0.38f, TextAlignment.LEFT);
    }
}

private void BuildStockCache()
{
    if (stockEntries.Count > 0 || stockByKey.Count > 0) return;
    for (int b = 0; b < blocks.Count; b++)
    {
        IMyTerminalBlock block = blocks[b];
        if (block == null || !block.HasInventory) continue;
        if (HasToken(block.CustomName, hiddenTag) || HasToken(block.CustomData, hiddenTag)) continue;
        if (HasToken(block.CustomName, noSortingTag) || HasToken(block.CustomData, noSortingTag)) continue;
        for (int inv = 0; inv < block.InventoryCount; inv++)
        {
            IMyInventory inventory = block.GetInventory(inv);
            if (inventory == null) continue;
            inventoryItems.Clear();
            inventory.GetItems(inventoryItems);
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                MyItemType type = inventoryItems[i].Type;
                string category = ItemCategory(type);
                if (category.Length == 0) continue;
                string name = DisplayItemName(type);
                string key = category + "/" + name;
                StockEntry entry;
                if (!stockByKey.TryGetValue(key, out entry))
                {
                    entry = new StockEntry();
                    entry.Category = category;
                    entry.Name = name;
                    entry.Icon = ItemIcon(type);
                    stockByKey[key] = entry;
                    stockEntries.Add(entry);
                }
                entry.Amount += (double)inventoryItems[i].Amount;
            }
        }
    }
}

private string ItemCategory(MyItemType type)
{
    string t = type.TypeId.ToString();
    if (t.EndsWith("_Ore")) return "Ore";
    if (t.EndsWith("_Ingot")) return "Ingot";
    if (t.EndsWith("_Component")) return "Component";
    if (t.EndsWith("_AmmoMagazine")) return "Ammo";
    if (t.EndsWith("_PhysicalGunObject")) return "Tool";
    if (t.EndsWith("_GasContainerObject") || t.EndsWith("_OxygenContainerObject")) return "Bottle";
    return "";
}

private string DisplayItemName(MyItemType type)
{
    string name = type.SubtypeId.ToString();
    if (name == "Stone" && type.TypeId.ToString().EndsWith("_Ingot")) return "Gravel";
    return SplitName(name);
}

private string ItemIcon(MyItemType type)
{
    string t = type.TypeId.ToString();
    string sub = type.SubtypeId.ToString();
    if (t.EndsWith("_Ore")) return "MyObjectBuilder_Ore/" + sub;
    if (t.EndsWith("_Ingot")) return "MyObjectBuilder_Ingot/" + sub;
    if (t.EndsWith("_Component")) return "MyObjectBuilder_Component/" + sub;
    if (t.EndsWith("_AmmoMagazine")) return "MyObjectBuilder_AmmoMagazine/" + sub;
    if (t.EndsWith("_PhysicalGunObject")) return "MyObjectBuilder_PhysicalGunObject/" + sub;
    if (t.EndsWith("_GasContainerObject")) return "MyObjectBuilder_GasContainerObject/" + sub;
    if (t.EndsWith("_OxygenContainerObject")) return "MyObjectBuilder_OxygenContainerObject/" + sub;
    return "IconInventory";
}

private string SplitName(string name)
{
    if (string.IsNullOrEmpty(name)) return "";
    sb.Clear();
    for (int i = 0; i < name.Length; i++)
    {
        char c = name[i];
        if (i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1])) sb.Append(' ');
        sb.Append(c);
    }
    return sb.ToString();
}

private void DrawStockRow(MySpriteDrawFrame frame, RectangleF panel, float y, StockEntry entry, double maxOnPage)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 28f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawBorder(frame, row, COLOR_DIM, 1f);
    DrawIcon(frame, entry.Icon, new Vector2(row.X + 18f, row.Y + 14f), new Vector2(20f, 20f), COLOR_ROW_TEXT);
    string name = TrimText(entry.Name, 20);
    string amount = FormatAmount(entry.Amount);
    double quota = StockQuota(entry);
    double pct = quota > 0 ? Math.Min(1.0, entry.Amount / quota) : 0.0;
    Color barColor = pct >= 0.35 ? COLOR_PROGRESS_FILL : COLOR_WARN;

    DrawText(frame, name, new Vector2(row.X + 34f, row.Y + 5f), COLOR_ROW_TEXT, 0.43f, TextAlignment.LEFT);
    DrawText(frame, amount, new Vector2(row.Right - 108f, row.Y + 5f), COLOR_ROW_TEXT, 0.43f, TextAlignment.RIGHT);
    RectangleF bar = new RectangleF(row.Right - 96f, row.Y + 8f, 82f, 10f);
    Fill(frame, bar, COLOR_PROGRESS_BG);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)pct, bar.Height), barColor);
    DrawBorder(frame, bar, COLOR_DIM, 1f);
}

private void DrawQuotaRow(MySpriteDrawFrame frame, RectangleF panel, float y, string subtype, string name, double stock, double quota)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 28f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawBorder(frame, row, COLOR_DIM, 1f);
    double pct = quota > 0 ? Math.Min(1.0, stock / quota) : 0.0;
    Color barColor = pct >= 0.50 ? COLOR_PROGRESS_FILL : COLOR_WARN;
    DrawIcon(frame, "MyObjectBuilder_Component/" + subtype, new Vector2(row.X + 18f, row.Y + 14f), new Vector2(20f, 20f), COLOR_ROW_TEXT);
    DrawText(frame, TrimText(name, 18), new Vector2(row.X + 34f, row.Y + 5f), COLOR_ROW_TEXT, 0.43f, TextAlignment.LEFT);
    DrawText(frame, FormatAmount(stock) + " / " + FormatAmount(quota), new Vector2(row.Right - 10f, row.Y + 5f), COLOR_ROW_TEXT, 0.43f, TextAlignment.RIGHT);
    RectangleF bar = new RectangleF(row.X + 10f, row.Bottom - 6f, row.Width - 20f, 4f);
    Fill(frame, bar, COLOR_PROGRESS_BG);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)pct, bar.Height), barColor);
}

private void DrawTankRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, double filled, double capacity, int count)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 54f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawBorder(frame, row, COLOR_DIM, 1f);
    double pct = capacity > 0 ? Math.Min(1.0, filled / capacity) : 0.0;
    Color barColor = pct >= 0.20 ? COLOR_PROGRESS_FILL : COLOR_WARN;
    string amount = Percent(pct) + "  " + FormatGas(filled) + " / " + FormatGas(capacity);
    DrawText(frame, label, new Vector2(row.X + 10f, row.Y + 6f), COLOR_ROW_TEXT, FitMonospace(label, 0.48f, 0.34f, 150f), TextAlignment.LEFT);
    DrawText(frame, amount, new Vector2(row.Right - 10f, row.Y + 6f), COLOR_ROW_TEXT, FitMonospace(amount, 0.42f, 0.28f, row.Width - 160f), TextAlignment.RIGHT);
    DrawText(frame, count + " tanks", new Vector2(row.Right - 10f, row.Y + 23f), COLOR_ROW_DIM, 0.30f, TextAlignment.RIGHT);
    RectangleF bar = new RectangleF(row.X + 10f, row.Y + 40f, row.Width - 20f, 8f);
    Fill(frame, bar, COLOR_PROGRESS_BG);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)pct, bar.Height), barColor);
    DrawBorder(frame, bar, COLOR_ROW_DIM, 1f);
}

private void DrawFuelInfoRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string value, Color valueColor)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 26f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawBorder(frame, row, COLOR_DIM, 1f);
    string safeValue = value ?? "-";
    float labelWidth = row.Width * 0.48f;
    float valueWidth = row.Width - labelWidth - 20f;
    DrawText(frame, label, new Vector2(row.X + 10f, row.Y + 4f), COLOR_ROW_TEXT, FitMonospace(label, 0.40f, 0.28f, labelWidth), TextAlignment.LEFT);
    DrawText(frame, safeValue, new Vector2(row.Right - 10f, row.Y + 4f), RowValueColor(valueColor), FitMonospace(safeValue, 0.40f, 0.26f, valueWidth), TextAlignment.RIGHT);
}

private float FitMonospace(string text, float normal, float minimum, float width)
{
    if (string.IsNullOrEmpty(text) || width <= 0f) return normal;
    float estimated = text.Length * 19.4f * normal;
    if (estimated <= width) return normal;
    float scale = normal * width / estimated;
    return scale < minimum ? minimum : scale;
}

private string Percent(double ratio)
{
    return (ratio * 100.0).ToString("0.0") + "%";
}

private string FormatGas(double liters)
{
    if (liters >= 1000000000) return (liters / 1000000000d).ToString("0.##") + " GL";
    if (liters >= 1000000) return (liters / 1000000d).ToString("0.##") + " ML";
    if (liters >= 1000) return (liters / 1000d).ToString("0.##") + " KL";
    return liters.ToString("0.##") + " L";
}

private double StockAmount(string category, string item)
{
    BuildStockCache();
    string wanted = NormalizeKey(item);
    for (int i = 0; i < stockEntries.Count; i++)
        if (stockEntries[i].Category.Equals(category, SC) && NormalizeKey(stockEntries[i].Name).Equals(wanted, SC))
            return stockEntries[i].Amount;
    return 0;
}

private string NormalizeKey(string value)
{
    if (value == null) return "";
    sb.Clear();
    for (int i = 0; i < value.Length; i++)
        if (char.IsLetterOrDigit(value[i]))
            sb.Append(char.ToLowerInvariant(value[i]));
    return sb.ToString();
}

private bool HasToken(string text, string token)
{
    if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token)) return false;
    return text.IndexOf(token, SC) >= 0;
}

private void ScanLifeSupport(ref double h2Now, ref double h2Max, ref int h2Count, ref double o2Now, ref double o2Max, ref int o2Count, ref double genIce, ref int genCount, ref int genOnline, ref int genWorking, ref int ventOk, ref int ventLeak, ref string leaks)
{
    interiorVents.Clear();
    MyItemType ice = MyItemType.MakeOre("Ice");
    for (int i = 0; i < blocks.Count; i++)
    {
        IMyTerminalBlock block = blocks[i];
        if (block == null) continue;
        if (block == Me) continue;
        IMyGasTank tank = block as IMyGasTank;
        if (tank != null)
        {
            double capacity = tank.Capacity;
            double filled = capacity * tank.FilledRatio;
            if (IsHydrogenTank(tank))
            {
                h2Count++;
                h2Max += capacity;
                h2Now += filled;
            }
            else
            {
                o2Count++;
                o2Max += capacity;
                o2Now += filled;
            }
            continue;
        }
        IMyGasGenerator generator = block as IMyGasGenerator;
        if (generator != null)
        {
            genCount++;
            if (generator.Enabled) genOnline++;
            if (generator.IsWorking) genWorking++;
            if (generator.InventoryCount > 0)
                genIce += (double)generator.GetInventory(0).GetItemAmount(ice);
            continue;
        }
        IMyAirVent vent = block as IMyAirVent;
        if (vent != null && IsInteriorVentMonitor(block))
        {
            interiorVents.Add(vent);
            bool ok = vent.IsWorking && vent.CanPressurize && vent.GetOxygenLevel() >= 0.95f;
            SetVentStatusTag(vent, ok);
            if (ok) ventOk++;
            else
            {
                ventLeak++;
                if (leaks.Length < 80)
                {
                    if (leaks.Length > 0) leaks += ", ";
                    leaks += CleanVentName(vent.CustomName);
                }
            }
        }
    }
}

private bool IsHydrogenTank(IMyGasTank tank)
{
    string text = tank.BlockDefinition.TypeIdString + "/" + tank.BlockDefinition.SubtypeId + "/" + tank.CustomName;
    return text.IndexOf("Hydrogen", SC) >= 0;
}

private bool IsInteriorVentMonitor(IMyTerminalBlock block)
{
    if (block == null) return false;
    if (block.CustomName.IndexOf("[AGM-S]", SC) < 0) return false;
    return block.CustomData.IndexOf("InteriorVent", SC) >= 0;
}

private void SetVentStatusTag(IMyAirVent vent, bool ok)
{
    string clean = CleanVentName(vent.CustomName);
    string tag = ok ? "[Pressurized]" : "[Leaking]";
    string next = clean + " " + tag;
    if (!vent.CustomName.Equals(next, SC))
        vent.CustomName = next;
}

private string CleanVentName(string name)
{
    if (string.IsNullOrWhiteSpace(name)) return "Air Vent";
    return name.Replace("[Pressurized]", "").Replace("[Leaking]", "").Replace("  ", " ").Trim();
}

private double StockQuota(StockEntry entry)
{
    if (entry.Category.Equals("Component", SC))
    {
        if (entry.Name.IndexOf("Steel Plate", SC) >= 0) return 50000;
        if (entry.Name.IndexOf("Interior Plate", SC) >= 0) return 50000;
        if (entry.Name.IndexOf("Construction", SC) >= 0) return 50000;
        if (entry.Name.IndexOf("Computer", SC) >= 0) return 5000;
        if (entry.Name.IndexOf("Motor", SC) >= 0) return 10000;
        if (entry.Name.IndexOf("Display", SC) >= 0) return 1000;
        if (entry.Name.IndexOf("Metal Grid", SC) >= 0) return 5000;
        if (entry.Name.IndexOf("Small Tube", SC) >= 0) return 5000;
        if (entry.Name.IndexOf("Large Tube", SC) >= 0) return 5000;
        if (entry.Name.IndexOf("Gravity", SC) >= 0) return 100;
        if (entry.Name.IndexOf("Superconductor", SC) >= 0) return 3000;
    }
    if (entry.Category.Equals("Ammo", SC)) return Math.Max(1000, entry.Amount);
    if (entry.Category.Equals("Ore", SC)) return Math.Max(1000000, entry.Amount);
    if (entry.Category.Equals("Ingot", SC)) return Math.Max(100000, entry.Amount);
    return Math.Max(1, entry.Amount);
}

private string FormatAmount(double value)
{
    if (value >= 1000000000) return (value / 1000000000d).ToString("0.##") + "B";
    if (value >= 1000000) return (value / 1000000d).ToString("0.##") + "M";
    if (value >= 1000) return (value / 1000d).ToString("0.##") + "K";
    return value.ToString("0.##");
}

private string TrimText(string text, int max)
{
    if (text == null) return "";
    if (text.Length <= max) return text;
    if (max <= 2) return text.Substring(0, max);
    return text.Substring(0, max - 2) + "..";
}

private int FirstNumber(string text)
{
    int value = 0;
    bool found = false;
    for (int i = 0; i < text.Length; i++)
    {
        if (char.IsDigit(text[i]))
        {
            found = true;
            value = value * 10 + (text[i] - '0');
        }
        else if (found) break;
    }
    return value;
}

private Dictionary<string, string> ReadModuleState(string tag, string header)
{
    Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    IMyProgrammableBlock pb = null;
    for (int i = 0; i < blocks.Count; i++)
    {
        pb = blocks[i] as IMyProgrammableBlock;
        if (pb != null && pb.CustomName.IndexOf(tag, SC) >= 0)
            break;
        pb = null;
    }
    if (pb == null) return result;
    string[] lines = (pb.CustomData ?? "").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    bool inState = false;
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            inState = line.Equals(header, SC);
            continue;
        }
        if (!inState) continue;
        string key, value;
        if (TrySplitKeyValue(line, '=', out key, out value))
            result[key] = value;
    }
    return result;
}

private string GetState(Dictionary<string, string> state, string key, string fallback)
{
    string value;
    return state.TryGetValue(key, out value) ? value : fallback;
}

private string BuildDashboardText()
{
    sb.Clear();
    sb.AppendLine("AUTO GRID MANAGER");
    sb.AppendLine("CORE " + VERSION);
    sb.AppendLine("====================");
    sb.AppendLine();
    sb.AppendLine("Core: " + (coreEnabled ? "ONLINE" : "OFF"));
    sb.AppendLine("Global pause: " + (globalPause ? "ON" : "OFF"));
    sb.AppendLine("Docked grids: " + (includeDockedGrids ? "ON" : "OFF"));
    sb.AppendLine();
    for (int i = 0; i < modules.Count; i++)
    {
        ModuleInfo m = modules[i];
        sb.AppendLine(PadRight(m.Label, 11) + StatusText(m.Status));
    }
    sb.AppendLine();
    sb.AppendLine("Tags:");
    sb.AppendLine("No sort " + noSortingTag);
    sb.AppendLine("Locked  " + lockedTag);
    return sb.ToString();
}

private void DrawCoreDashboard(IMyTextSurface surface)
{
    if (surface == null) return;
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
        DrawText(frame, "AUTO GRID MANAGER", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 0.95f, TextAlignment.LEFT);
        DrawText(frame, coreEnabled ? "CORE OK" : "CORE X", panel.Position + new Vector2(panel.Width - 24f, 26f), coreEnabled ? COLOR_OK : COLOR_BAD, 0.62f, TextAlignment.RIGHT);
        DrawText(frame, "CORE " + VERSION, panel.Position + new Vector2(24f, 56f), COLOR_DIM, 0.46f, TextAlignment.LEFT);
        DrawText(frame, "LIVE " + DateTime.Now.ToString("HH:mm:ss"), panel.Position + new Vector2(panel.Width - 24f, 56f), COLOR_OK, 0.46f, TextAlignment.RIGHT);
        float y = panel.Y + 92f;
        DrawCoreInfoRow(frame, panel, y, "Global pause", globalPause ? "ON" : "OFF", globalPause ? COLOR_WARN : COLOR_OK);
        y += 32f;
        DrawCoreInfoRow(frame, panel, y, "Docked grids", includeDockedGrids ? "ON" : "OFF", COLOR_TEXT);
        y += 44f;
        for (int i = 0; i < modules.Count; i++)
        {
            ModuleInfo m = modules[i];
            DrawCoreInfoRow(frame, panel, y, "AGM " + m.Label, StatusText(m.Status), ModuleColor(m.Status));
            y += 32f;
        }
        DrawCoreInfoRow(frame, panel, panel.Bottom - 64f, "No sort", noSortingTag, COLOR_DIM);
        DrawCoreInfoRow(frame, panel, panel.Bottom - 32f, "Locked", lockedTag, COLOR_DIM);
    }
}

private void DrawCoreInfoRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string value, Color valueColor)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 26f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawBorder(frame, row, COLOR_DIM, 1f);
    DrawText(frame, TrimText(label, 15), new Vector2(row.X + 10f, row.Y + 4f), COLOR_ROW_TEXT, 0.46f, TextAlignment.LEFT);
    DrawText(frame, TrimText(value, 30), new Vector2(row.Right - 10f, row.Y + 4f), RowValueColor(valueColor), 0.46f, TextAlignment.RIGHT);
}

private void DrawFullInfoRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string value, Color valueColor)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 38f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawBorder(frame, row, COLOR_DIM, 1f);
    string safeValue = value ?? "-";
    DrawText(frame, label, new Vector2(row.X + 10f, row.Y + 3f), COLOR_ROW_DIM, 0.34f, TextAlignment.LEFT);
    DrawText(frame, safeValue, new Vector2(row.X + 10f, row.Y + 18f), RowValueColor(valueColor), FitMonospace(safeValue, 0.36f, 0.18f, row.Width - 20f), TextAlignment.LEFT);
}

private Color RowValueColor(Color valueColor)
{
    if (valueColor.PackedValue == COLOR_OK.PackedValue || valueColor.PackedValue == COLOR_WARN.PackedValue || valueColor.PackedValue == COLOR_BAD.PackedValue)
        return valueColor;
    return COLOR_ROW_TEXT;
}

private float FitScale(string value, float normal, float minimum)
{
    if (string.IsNullOrEmpty(value)) return normal;
    if (value.Length <= 34) return normal;
    float scale = normal * 34f / value.Length;
    return scale < minimum ? minimum : scale;
}

private void DrawCorePbStatus()
{
    IMyTextSurface surface = Me.GetSurface(0);
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
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f);
        Vector2 top = panel.Position + new Vector2(panel.Width * 0.5f, 24f);
        DrawText(frame, "AGM - Core", top, COLOR_ACCENT_2, 0.92f, TextAlignment.CENTER);
        DrawText(frame, "AutoGrid Manager", top + new Vector2(0, 30f), COLOR_TEXT, 0.44f, TextAlignment.CENTER);
        DrawText(frame, coreEnabled ? "CORE ONLINE" : "CORE OFF", top + new Vector2(0, 60f), coreEnabled ? COLOR_OK : COLOR_BAD, 0.52f, TextAlignment.CENTER);
        float y = top.Y + 100f;
        DrawText(frame, "LIVE " + DateTime.Now.ToString("HH:mm:ss"), new Vector2(panel.X + 24f, y), COLOR_OK, 0.46f, TextAlignment.LEFT);
        y += 30f;
        for (int i = 0; i < modules.Count; i++)
        {
            ModuleInfo m = modules[i];
            Color c = ModuleColor(m.Status);
            DrawText(frame, m.Label, new Vector2(panel.X + 24f, y), COLOR_TEXT, 0.44f, TextAlignment.LEFT);
            DrawText(frame, StatusText(m.Status), new Vector2(panel.Right - 24f, y), c, 0.44f, TextAlignment.RIGHT);
            y += 26f;
        }
        DrawText(frame, "Pause " + (globalPause ? "ON" : "OFF"), new Vector2(panel.X + 24f, panel.Bottom - 42f), globalPause ? COLOR_WARN : COLOR_DIM, 0.38f, TextAlignment.LEFT);
        DrawText(frame, "v" + VERSION, new Vector2(panel.X + panel.Width * 0.5f, panel.Bottom - 18f), COLOR_DIM, 0.34f, TextAlignment.CENTER);
    }
}

private void DrawModuleBoot(IMyTextSurface surface, string title, double progress)
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
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f);
        Vector2 center = panel.Position + panel.Size * 0.5f;
        DrawText(frame, title, new Vector2(center.X, panel.Y + 56f), COLOR_ACCENT_2, 0.82f, TextAlignment.CENTER);
        DrawText(frame, "AutoGrid Manager", new Vector2(center.X, panel.Y + 88f), COLOR_TEXT, 0.42f, TextAlignment.CENTER);
        DrawText(frame, "BOOTING", new Vector2(center.X, panel.Y + 124f), COLOR_OK, 0.54f, TextAlignment.CENTER);
        RectangleF bar = new RectangleF(panel.X + 34f, center.Y + 36f, panel.Width - 68f, 12f);
        Fill(frame, bar, COLOR_PROGRESS_BG);
        Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)progress, bar.Height), COLOR_PROGRESS_FILL);
        DrawBorder(frame, bar, COLOR_ACCENT_2, 1f);
        DrawText(frame, ((int)(progress * 100.0)).ToString() + "%", new Vector2(center.X, bar.Y + 28f), COLOR_DIM, 0.38f, TextAlignment.CENTER);
        DrawText(frame, "v" + VERSION, new Vector2(center.X, panel.Bottom - 18f), COLOR_DIM, 0.34f, TextAlignment.CENTER);
    }
}

private void DrawWaitingForCommand(IMyTextSurface surface)
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
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f);
        Vector2 center = panel.Position + panel.Size * 0.5f;
        DrawText(frame, "AGM SCREEN", new Vector2(center.X, panel.Y + 56f), COLOR_ACCENT_2, 0.82f, TextAlignment.CENTER);
        DrawText(frame, "AutoGrid Manager", new Vector2(center.X, panel.Y + 88f), COLOR_TEXT, 0.42f, TextAlignment.CENTER);
        DrawText(frame, "WAITING FOR COMMAND", new Vector2(center.X, center.Y - 6f), COLOR_WARN, 0.48f, TextAlignment.CENTER);
        DrawText(frame, "Add one command in Custom Data", new Vector2(center.X, center.Y + 28f), COLOR_DIM, 0.34f, TextAlignment.CENTER);
        DrawText(frame, "CoreDashboard | ComponentStock | FuelLifeSupport", new Vector2(center.X, center.Y + 56f), COLOR_DIM, 0.28f, TextAlignment.CENTER);
        DrawText(frame, "v" + VERSION, new Vector2(center.X, panel.Bottom - 18f), COLOR_DIM, 0.34f, TextAlignment.CENTER);
    }
}

private RectangleF Inset(RectangleF rect, float amount)
{
    return new RectangleF(rect.X + amount, rect.Y + amount, rect.Width - amount * 2f, rect.Height - amount * 2f);
}

private void Fill(MySpriteDrawFrame frame, RectangleF rect, Color color)
{
    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", rect.Position + rect.Size * 0.5f, rect.Size, color));
}

private void DrawBorder(MySpriteDrawFrame frame, RectangleF r, Color color, float t)
{
    Fill(frame, new RectangleF(r.X, r.Y, r.Width, t), color);
    Fill(frame, new RectangleF(r.X, r.Bottom - t, r.Width, t), color);
    Fill(frame, new RectangleF(r.X, r.Y, t, r.Height), color);
    Fill(frame, new RectangleF(r.Right - t, r.Y, t, r.Height), color);
}

private void DrawText(MySpriteDrawFrame frame, string text, Vector2 pos, Color color, float scale, TextAlignment align)
{
    frame.Add(new MySprite(SpriteType.TEXT, text ?? "", pos, null, color, "Monospace", align, scale));
}

private void DrawIcon(MySpriteDrawFrame frame, string spriteName, Vector2 center, Vector2 size, Color color)
{
    if (string.IsNullOrEmpty(spriteName)) spriteName = "IconInventory";
    frame.Add(new MySprite(SpriteType.TEXTURE, spriteName, center, size, color));
}

private string StatusText(string status)
{
    if (status.Equals("online", SC)) return "ONLINE";
    if (status.Equals("paused", SC)) return "PAUSED";
    if (status.Equals("disabled", SC)) return "DISABLED";
    if (status.Equals("missing", SC)) return "MISSING";
    return status.ToUpperInvariant();
}

private Color ModuleColor(string status)
{
    if (status.Equals("online", SC)) return COLOR_OK;
    if (status.Equals("missing", SC)) return COLOR_BAD;
    if (status.Equals("paused", SC)) return COLOR_WARN;
    return COLOR_DIM;
}

private void EchoStatus()
{
    Echo("AutoGrid Manager Core");
    Echo("Version: " + VERSION);
    Echo("PB tag : " + CORE_TAG);
    Echo("Pause  : " + (globalPause ? "ON" : "OFF"));
    Echo("");
    for (int i = 0; i < modules.Count; i++)
        Echo(modules[i].Label + ": " + modules[i].Status);
    Echo("");
    Echo("Screens: " + screens.Count);
}

private string StripComment(string line)
{
    int idx = line.IndexOf("//");
    if (idx >= 0) return line.Substring(0, idx);
    idx = line.IndexOf(';');
    if (idx >= 0) return line.Substring(0, idx);
    return line;
}

private bool TrySplitKeyValue(string line, char separator, out string key, out string value)
{
    key = "";
    value = "";
    int idx = line.IndexOf(separator);
    if (idx < 0) return false;
    key = line.Substring(0, idx).Trim();
    value = line.Substring(idx + 1).Trim();
    return key.Length > 0;
}

private bool ParseBool(string value, bool fallback)
{
    if (value == null) return fallback;
    string v = value.Trim().ToLowerInvariant();
    if (v == "true" || v == "yes" || v == "on" || v == "1") return true;
    if (v == "false" || v == "no" || v == "off" || v == "0") return false;
    return fallback;
}

private string BoolText(bool value)
{
    return value ? "true" : "false";
}

private string PadRight(string value, int width)
{
    if (value == null) value = "";
    if (value.Length >= width) return value + " ";
    return value + new string(' ', width - value.Length);
}

private void WriteCoreValue(string key, string value)
{
    string[] lines = Me.CustomData.Split(new char[] { '\r', '\n' }, StringSplitOptions.None);
    sb.Clear();
    bool inCore = false;
    bool wrote = false;
    for (int i = 0; i < lines.Length; i++)
    {
        string trimmed = lines[i].Trim();
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            if (inCore && !wrote)
            {
                sb.AppendLine(key + "=" + value);
                wrote = true;
            }
            inCore = trimmed.Equals("[Core]", SC);
        }
        if (inCore)
        {
            string k, v;
            if (TrySplitKeyValue(trimmed, '=', out k, out v) && k.Equals(key, SC))
            {
                sb.AppendLine(key + "=" + value);
                wrote = true;
                continue;
            }
        }
        sb.AppendLine(lines[i]);
    }
    if (!wrote)
        sb.AppendLine(key + "=" + value);
    Me.CustomData = sb.ToString().TrimEnd();
}
