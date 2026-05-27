private const string VERSION = "1.0-logistics";
private const string PB_TAG = "{AGM-Logistics}";
private const string CORE_TAG = "{AGM-Core}";
private static readonly StringComparison SC = StringComparison.OrdinalIgnoreCase;
private readonly Color COLOR_BG = new Color(1, 8, 13);
private readonly Color COLOR_PANEL = new Color(2, 18, 28);
private readonly Color COLOR_PANEL_2 = new Color(3, 58, 78);
private readonly Color COLOR_ACCENT = new Color(38, 239, 255);
private readonly Color COLOR_ACCENT_2 = new Color(112, 247, 255);
private readonly Color COLOR_TEXT = new Color(126, 246, 255);
private readonly Color COLOR_DIM = new Color(44, 177, 195);
private readonly Color COLOR_ROW_TEXT = new Color(126, 246, 255);
private readonly Color COLOR_OK = new Color(97, 255, 214);
private readonly Color COLOR_BAD = new Color(255, 79, 66);
private readonly Color COLOR_PROGRESS_BG = new Color(18, 48, 32);
private readonly Color COLOR_PROGRESS_FILL = new Color(255, 204, 36);

private class CargoInfo
{
    public IMyTerminalBlock Block;
    public IMyInventory Inv;
    public string Type;
    public int Index;
    public bool Locked;
    public bool Hidden;
    public bool Manual;
}

private class SourceInfo
{
    public IMyTerminalBlock Block;
    public IMyInventory Inv;
    public string Type;
}

private readonly List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
private readonly List<CargoInfo> cargos = new List<CargoInfo>();
private readonly List<SourceInfo> sources = new List<SourceInfo>();
private readonly List<MyInventoryItem> items = new List<MyInventoryItem>();
private readonly StringBuilder sb = new StringBuilder();

private bool coreFound = false;
private bool coreEnabled = false;
private bool logisticsEnabled = false;
private bool globalPause = false;
private bool includeDockedGrids = false;
private bool autoAssign = true;
private int maxMoves = 2;
private int tick = 0;
private bool booting = true;
private double bootElapsed = 0.0;
private DateTime lastRun = DateTime.Now;
private int sourceIndex = 0;
private int lastMoves = 0;
private string lastStatus = "boot";
private string lastItem = "";
private string lastFrom = "";
private string lastTo = "";
private string warning = "";
private string noSortingTag = "[No Sorting]";
private string lockedTag = "{Locked}";
private string manualTag = "{Manual}";
private string hiddenTag = "{Hidden}";

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    EnsureConfig();
    Reload();
}

public void Save()
{
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
    if (arg.Equals("run", SC) || arg.Equals("sort", SC))
    {
        Reload();
        RunLogistics();
    }

    if ((updateSource & (UpdateType.Update10 | UpdateType.Update100)) != 0)
    {
        tick += (updateSource & UpdateType.Update100) != 0 ? 100 : 10;
        if (tick >= 100)
        {
            tick = 0;
            Reload();
            RunLogistics();
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
@"[Logistics]
auto_assign=true
max_moves_per_run=2";
}

private void Reload()
{
    ReadOwnConfig();
    ScanBlocks();
    ReadCore();
    if (includeDockedGrids)
        ScanBlocks();
    BuildCargoAndSources();
}

private void ReadOwnConfig()
{
    autoAssign = true;
    maxMoves = 2;
    string[] lines = Me.CustomData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string key, value;
        if (!TrySplitKeyValue(StripComment(lines[i]).Trim(), '=', out key, out value)) continue;
        if (key.Equals("auto_assign", SC)) autoAssign = ParseBool(value, true);
        else if (key.Equals("max_moves_per_run", SC)) int.TryParse(value, out maxMoves);
    }
    if (maxMoves < 1) maxMoves = 1;
    if (maxMoves > 10) maxMoves = 10;
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
    logisticsEnabled = false;
    globalPause = false;
    includeDockedGrids = false;
    noSortingTag = "[No Sorting]";
    lockedTag = "{Locked}";
    manualTag = "{Manual}";
    hiddenTag = "{Hidden}";
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
            else if (key.Equals("logistics", SC) || key.Equals("logistics_enabled", SC)) logisticsEnabled = ParseBool(value, true);
            else if (key.Equals("global_pause", SC)) globalPause = ParseBool(value, false);
            else if (key.Equals("include_docked_grids", SC)) includeDockedGrids = ParseBool(value, false);
            else if (key.Equals("no_sorting_tag", SC)) noSortingTag = value.Trim();
            else if (key.Equals("locked_tag", SC)) lockedTag = value.Trim();
            else if (key.Equals("manual_tag", SC)) manualTag = value.Trim();
            else if (key.Equals("hidden_tag", SC)) hiddenTag = value.Trim();
        }
        break;
    }
}

private void BuildCargoAndSources()
{
    cargos.Clear();
    sources.Clear();
    for (int i = 0; i < blocks.Count; i++)
    {
        IMyTerminalBlock b = blocks[i];
        if (b == null || !b.HasInventory) continue;
        if (IsNoSortingBlock(b)) continue;
        if (b is IMyCargoContainer)
        {
            CargoInfo c = new CargoInfo();
            c.Block = b;
            c.Inv = b.GetInventory(0);
            c.Type = CargoTypeFromBlock(b);
            c.Index = CargoNumber(b, c.Type);
            c.Locked = HasAnyToken(b, lockedTag);
            c.Manual = HasAnyToken(b, manualTag);
            c.Hidden = HasAnyToken(b, hiddenTag);
            cargos.Add(c);
            if (!c.Locked && !c.Manual && !c.Hidden)
                AddSource(b, c.Inv, c.Type);
            continue;
        }
        if (b is IMyReactor || b is IMyGasGenerator || b is IMyGasTank) continue;
        if (HasAnyToken(b, lockedTag) || HasAnyToken(b, manualTag) || HasAnyToken(b, hiddenTag)) continue;
        if (b.InventoryCount >= 2 && (b is IMyAssembler || b is IMyRefinery))
            AddSource(b, b.GetInventory(1), "");
        else if (b.InventoryCount == 1)
            AddSource(b, b.GetInventory(0), "");
    }
    cargos.Sort((a, b) => ((float)b.Inv.MaxVolume).CompareTo((float)a.Inv.MaxVolume));
}

private void AddSource(IMyTerminalBlock b, IMyInventory inv, string type)
{
    if (inv == null) return;
    SourceInfo s = new SourceInfo();
    s.Block = b;
    s.Inv = inv;
    s.Type = type;
    sources.Add(s);
}

private void RunLogistics()
{
    lastMoves = 0;
    warning = "";
    if (!coreFound) { lastStatus = "no core"; return; }
    if (!coreEnabled) { lastStatus = "core off"; return; }
    if (!logisticsEnabled) { lastStatus = "disabled"; return; }
    if (globalPause) { lastStatus = "paused"; return; }
    if (autoAssign) EnsureBaselineDestinations();
    if (sources.Count == 0) { lastStatus = "no sources"; return; }

    int moves = 0;
    if (sourceIndex < 0 || sourceIndex >= sources.Count) sourceIndex = 0;
    int checkedCount = 0;
    while (checkedCount < sources.Count && moves < maxMoves)
    {
        SourceInfo src = sources[sourceIndex];
        sourceIndex++;
        if (sourceIndex >= sources.Count) sourceIndex = 0;
        checkedCount++;
        items.Clear();
        src.Inv.GetItems(items);
        for (int i = items.Count - 1; i >= 0 && moves < maxMoves; i--)
        {
            MyInventoryItem item = items[i];
            string type = ItemCategory(item.Type);
            if (type.Length == 0) continue;
            if (src.Type.Equals(type, SC)) continue;
            CargoInfo dest = FindDestination(type);
            if (dest == null && autoAssign)
                dest = AssignCargo(type);
            if (dest == null)
            {
                warning = type + " storage full";
                continue;
            }
            if (TryMove(src.Inv, dest.Inv, i, item.Amount))
            {
                moves++;
                lastItem = DisplayItem(item.Type.SubtypeId);
                lastFrom = ShortName(src.Block);
                lastTo = ShortName(dest.Block);
            }
        }
    }
    lastMoves = moves;
    lastStatus = moves > 0 ? "moved " + moves : "idle";
}

private bool TryMove(IMyInventory src, IMyInventory dst, int index, MyFixedPoint amount)
{
    if (src.TransferItemTo(dst, index, null, true)) return true;
    double a = (double)amount;
    double batch = Math.Min(a, 1000.0);
    if (batch > 0 && src.TransferItemTo(dst, index, null, true, (MyFixedPoint)batch)) return true;
    batch = Math.Min(a, 100.0);
    if (batch > 0 && src.TransferItemTo(dst, index, null, true, (MyFixedPoint)batch)) return true;
    return src.TransferItemTo(dst, index, null, true, (MyFixedPoint)1.0);
}

private CargoInfo FindDestination(string type)
{
    CargoInfo best = null;
    int bestNum = int.MaxValue;
    for (int i = 0; i < cargos.Count; i++)
    {
        CargoInfo c = cargos[i];
        if (c.Locked || c.Manual || c.Hidden) continue;
        if (!c.Type.Equals(type, SC)) continue;
        if (!HasSpace(c.Inv)) continue;
        int n = c.Index > 0 ? c.Index : CargoNumber(c.Block, type);
        if (n < bestNum)
        {
            best = c;
            bestNum = n;
        }
    }
    return best;
}

private void EnsureBaselineDestinations()
{
    EnsureDestination("Ore");
    EnsureDestination("Ingot");
    EnsureDestination("Component");
    EnsureDestination("Ammo");
    EnsureDestination("Tool");
    EnsureDestination("Bottle");
}

private void EnsureDestination(string type)
{
    if (FindDestination(type) != null) return;
    AssignCargo(type);
}

private CargoInfo AssignCargo(string type)
{
    for (int i = 0; i < cargos.Count; i++)
    {
        CargoInfo c = cargos[i];
        if (c.Locked || c.Manual || c.Hidden || c.Type.Length > 0) continue;
        if (c.Inv.ItemCount > 0) continue;
        if (!CargoSizeMatches(c.Block, type)) continue;
        int n = NextCargoNumber(type);
        c.Block.CustomName = CleanCargoName(c.Block.CustomName) + " {" + TagType(type) + " " + n + "}";
        WriteCargoMetadata(c.Block, type, n);
        c.Type = type;
        c.Index = n;
        lastStatus = "assigned " + TagType(type) + " " + n;
        return c;
    }
    return null;
}

private int NextCargoNumber(string type)
{
    int max = 0;
    for (int i = 0; i < cargos.Count; i++)
    {
        if (!cargos[i].Type.Equals(type, SC)) continue;
        int n = cargos[i].Index > 0 ? cargos[i].Index : CargoNumber(cargos[i].Block, type);
        if (n > max) max = n;
    }
    return max + 1;
}

private int CargoNumber(IMyTerminalBlock block, string type)
{
    int fromData = CargoIndexFromData(block.CustomData, type);
    if (fromData > 0) return fromData;
    return CargoNumberFromName(block.CustomName, type);
}

private int CargoNumberFromName(string name, string type)
{
    string tag = TagType(type);
    int idx = name.IndexOf("{" + tag, SC);
    if (idx < 0) return 999;
    int start = idx + tag.Length + 1;
    while (start < name.Length && name[start] == ' ') start++;
    int end = start;
    while (end < name.Length && char.IsDigit(name[end])) end++;
    int n;
    if (int.TryParse(name.Substring(start, end - start), out n)) return n;
    return 999;
}

private string CargoTypeFromBlock(IMyTerminalBlock block)
{
    string fromData = CargoTypeFromData(block.CustomData);
    if (fromData.Length > 0) return fromData;
    return CargoTypeFromName(block.CustomName);
}

private string CargoTypeFromName(string name)
{
    if (HasTagType(name, "Ore")) return "Ore";
    if (HasTagType(name, "Ingot")) return "Ingot";
    if (HasTagType(name, "Component")) return "Component";
    if (HasTagType(name, "Ammo")) return "Ammo";
    if (HasTagType(name, "Tools") || HasTagType(name, "Tool")) return "Tool";
    if (HasTagType(name, "Bottle")) return "Bottle";
    return "";
}

private string CargoTypeFromData(string data)
{
    string value = GetSectionValue(data, "[AGM-Logistics]", "type");
    return NormaliseCargoType(value);
}

private int CargoIndexFromData(string data, string type)
{
    string dataType = CargoTypeFromData(data);
    if (dataType.Length > 0 && !dataType.Equals(type, SC)) return 0;
    string value = GetSectionValue(data, "[AGM-Logistics]", "index");
    int n;
    return int.TryParse(value, out n) ? n : 0;
}

private string NormaliseCargoType(string value)
{
    if (value == null) return "";
    value = value.Trim();
    if (value.Equals("Ore", SC)) return "Ore";
    if (value.Equals("Ingot", SC)) return "Ingot";
    if (value.Equals("Component", SC)) return "Component";
    if (value.Equals("Ammo", SC)) return "Ammo";
    if (value.Equals("Tool", SC) || value.Equals("Tools", SC)) return "Tool";
    if (value.Equals("Bottle", SC) || value.Equals("Bottles", SC)) return "Bottle";
    return "";
}

private bool HasTagType(string name, string tag)
{
    return name.IndexOf("{" + tag, SC) >= 0 || name.IndexOf("[" + tag + "]", SC) >= 0;
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

private bool CargoSizeMatches(IMyTerminalBlock b, string type)
{
    if (!IsLargeGridBlock(b)) return false;
    if (type.Equals("Ammo", SC) || type.Equals("Tool", SC) || type.Equals("Bottle", SC))
        return IsSmallCargo(b);
    return IsLargeCargo(b);
}

private bool IsLargeGridBlock(IMyTerminalBlock b)
{
    return b.CubeGrid != null && b.CubeGrid.GridSize > 1.0f;
}

private bool IsLargeCargo(IMyTerminalBlock b)
{
    string s = b.BlockDefinition.SubtypeId + " " + b.DefinitionDisplayNameText + " " + b.CustomName;
    return s.IndexOf("Large", SC) >= 0 || s.IndexOf("Bulk", SC) >= 0;
}

private bool IsSmallCargo(IMyTerminalBlock b)
{
    string s = b.BlockDefinition.SubtypeId + " " + b.DefinitionDisplayNameText + " " + b.CustomName;
    return s.IndexOf("Small", SC) >= 0;
}

private string TagType(string type)
{
    if (type.Equals("Tool", SC)) return "Tools";
    return type;
}

private string CleanCargoName(string name)
{
    int idx = name.IndexOf("{");
    if (idx >= 0) name = name.Substring(0, idx);
    return name.Trim();
}

private bool HasSpace(IMyInventory inv)
{
    if (inv == null) return false;
    return (float)inv.CurrentVolume < (float)inv.MaxVolume * 0.98f;
}

private bool IsNoSortingBlock(IMyTerminalBlock b)
{
    return HasToken(b.CustomName, noSortingTag) || b.CubeGrid.CustomName.IndexOf(noSortingTag, SC) >= 0;
}

private bool HasToken(string name, string token)
{
    return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(token) && name.IndexOf(token, SC) >= 0;
}

private bool HasAnyToken(IMyTerminalBlock block, string token)
{
    if (block == null || string.IsNullOrEmpty(token)) return false;
    return HasToken(block.CustomName, token) || HasToken(block.CustomData, token);
}

private void WriteCargoMetadata(IMyTerminalBlock block, string type, int index)
{
    sb.Clear();
    sb.AppendLine("[AGM-Logistics]");
    sb.AppendLine("managed=true");
    sb.AppendLine("type=" + TagType(type));
    sb.AppendLine("index=" + index);
    sb.AppendLine("size=" + (IsSmallCargo(block) ? "Small" : "Large"));
    block.CustomData = UpsertSectionText(block.CustomData, "[AGM-Logistics]", sb.ToString());
}

private string GetSectionValue(string data, string header, string key)
{
    if (string.IsNullOrEmpty(data)) return "";
    int start = data.IndexOf(header, SC);
    if (start < 0) return "";
    int next = data.IndexOf("\n[", start + header.Length, SC);
    string section = next < 0 ? data.Substring(start) : data.Substring(start, next - start);
    string[] lines = section.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string k, v;
        if (TrySplitKeyValue(StripComment(lines[i]).Trim(), '=', out k, out v) && k.Equals(key, SC))
            return v.Trim();
    }
    return "";
}

private void WriteState()
{
    sb.Clear();
    sb.AppendLine("[LogisticsState]");
    sb.AppendLine("version=" + VERSION);
    sb.AppendLine("core_found=" + BoolText(coreFound));
    sb.AppendLine("core_enabled=" + BoolText(coreEnabled));
    sb.AppendLine("logistics_enabled=" + BoolText(logisticsEnabled));
    sb.AppendLine("global_pause=" + BoolText(globalPause));
    sb.AppendLine("state=" + StateText());
    sb.AppendLine("cargo=" + cargos.Count);
    sb.AppendLine("sources=" + sources.Count);
    sb.AppendLine("moves=" + lastMoves);
    sb.AppendLine("last_item=" + lastItem);
    sb.AppendLine("last_from=" + lastFrom);
    sb.AppendLine("last_to=" + lastTo);
    sb.AppendLine("warning=" + warning);
    sb.AppendLine("ore=" + CountType("Ore"));
    sb.AppendLine("ingot=" + CountType("Ingot"));
    sb.AppendLine("component=" + CountType("Component"));
    sb.AppendLine("ammo=" + CountType("Ammo"));
    sb.AppendLine("tool=" + CountType("Tool"));
    sb.AppendLine("bottle=" + CountType("Bottle"));
    UpsertSection("[LogisticsState]", sb.ToString());
}

private int CountType(string type)
{
    int count = 0;
    for (int i = 0; i < cargos.Count; i++)
        if (cargos[i].Type.Equals(type, SC))
            count++;
    return count;
}

private void UpsertSection(string header, string content)
{
    Me.CustomData = UpsertSectionText(Me.CustomData, header, content);
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
    if (next < 0)
        return raw.Substring(0, start).TrimEnd() + "\n\n" + content.TrimEnd();
    return raw.Substring(0, start).TrimEnd() + "\n\n" + content.TrimEnd() + "\n\n" + raw.Substring(next + 1).TrimStart();
}

private void DrawPbStatus()
{
    DrawLogistics(Me.GetSurface(0));
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
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f);
        Vector2 center = panel.Position + panel.Size * 0.5f;
        DrawText(frame, "AGM - Logistics", new Vector2(center.X, panel.Y + 56f), COLOR_ACCENT_2, 0.78f, TextAlignment.CENTER);
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

private void DrawLogistics(IMyTextSurface s)
{
    if (s == null) return;
    s.ContentType = ContentType.SCRIPT;
    s.Script = "";
    s.Font = "Monospace";
    s.FontSize = 0.72f;
    s.TextPadding = 1f;
    RectangleF vp = new RectangleF((s.TextureSize - s.SurfaceSize) * 0.5f, s.SurfaceSize);
    using (var frame = s.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 12f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f);

        float x = panel.X + 26f;
        float right = panel.Right - 26f;
        float y = panel.Y + 30f;
        DrawText(frame, "AGM LOGISTICS", new Vector2(x, y), COLOR_ACCENT_2, 0.62f, TextAlignment.LEFT);
        DrawText(frame, "LIVE " + DateTime.Now.ToString("HH:mm:ss"), new Vector2(right, y + 2f), COLOR_OK, 0.38f, TextAlignment.RIGHT);
        y += 34f;
        DrawText(frame, "AutoGrid Manager", new Vector2(x, y), COLOR_TEXT, 0.34f, TextAlignment.LEFT);
        DrawText(frame, "v" + VERSION, new Vector2(right, y), COLOR_DIM, 0.32f, TextAlignment.RIGHT);

        y += 34f;
        DrawLogisticsRow(frame, panel, y, "Core", coreFound ? "CONNECTED" : "MISSING", coreFound ? COLOR_OK : COLOR_BAD);
        y += 28f;
        DrawLogisticsRow(frame, panel, y, "State", StateText(), StateText().IndexOf("NO", SC) >= 0 || StateText().IndexOf("OFF", SC) >= 0 ? COLOR_BAD : COLOR_OK);
        y += 34f;

        DrawTypeRow(frame, panel, y, "Ore", "Ore"); y += 26f;
        DrawTypeRow(frame, panel, y, "Ingot", "Ingot"); y += 26f;
        DrawTypeRow(frame, panel, y, "Component", "Component"); y += 26f;
        DrawTypeRow(frame, panel, y, "Ammo", "Ammo"); y += 26f;
        DrawTypeRow(frame, panel, y, "Tools", "Tool"); y += 26f;
        DrawTypeRow(frame, panel, y, "Bottle", "Bottle"); y += 34f;

        DrawLogisticsRow(frame, panel, y, "Last run", "moved " + lastMoves + " / " + maxMoves, lastMoves > 0 ? COLOR_OK : COLOR_DIM);
        y += 28f;
        DrawLogisticsRow(frame, panel, y, "Last item", lastItem.Length > 0 ? lastItem : "-", COLOR_TEXT);
        y += 28f;
        string route = lastFrom.Length > 0 ? ShortText(lastFrom, 18) + " > " + ShortText(lastTo, 18) : "-";
        DrawLogisticsRow(frame, panel, y, "Route", route, COLOR_TEXT);
        if (warning.Length > 0)
        {
            y += 28f;
            DrawLogisticsRow(frame, panel, y, "Warning", ShortText(warning, 30), COLOR_BAD);
        }

        DrawText(frame, "Cargo " + cargos.Count + "  Sources " + sources.Count, new Vector2(x, panel.Bottom - 20f), COLOR_DIM, 0.32f, TextAlignment.LEFT);
    }
}

private void AppendType(string type)
{
    int count = 0;
    double cur = 0, max = 0;
    for (int i = 0; i < cargos.Count; i++)
    {
        if (!cargos[i].Type.Equals(type, SC)) continue;
        count++;
        cur += (double)cargos[i].Inv.CurrentVolume;
        max += (double)cargos[i].Inv.MaxVolume;
    }
    double pct = max > 0 ? cur / max * 100.0 : 0.0;
    sb.AppendLine(TagType(type).PadRight(10) + count + "x " + pct.ToString("0.0") + "%");
}

private void DrawLogisticsRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string value, Color valueColor)
{
    RectangleF row = new RectangleF(panel.X + 18f, y - 4f, panel.Width - 36f, 24f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawBorder(frame, row, COLOR_DIM, 1f);
    DrawText(frame, label, new Vector2(row.X + 8f, y), COLOR_ROW_TEXT, 0.34f, TextAlignment.LEFT);
    DrawText(frame, value, new Vector2(row.Right - 8f, y), RowValueColor(valueColor), 0.34f, TextAlignment.RIGHT);
}

private void DrawTypeRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string type)
{
    int count = 0;
    double cur = 0, max = 0;
    for (int i = 0; i < cargos.Count; i++)
    {
        if (!cargos[i].Type.Equals(type, SC)) continue;
        count++;
        cur += (double)cargos[i].Inv.CurrentVolume;
        max += (double)cargos[i].Inv.MaxVolume;
    }
    double pct = max > 0 ? cur / max * 100.0 : 0.0;
    RectangleF row = new RectangleF(panel.X + 18f, y - 4f, panel.Width - 36f, 24f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawBorder(frame, row, COLOR_DIM, 1f);
    DrawText(frame, label, new Vector2(row.X + 8f, y), COLOR_ROW_TEXT, 0.32f, TextAlignment.LEFT);
    DrawText(frame, count + " cargo  " + pct.ToString("0.0") + "%", new Vector2(row.Right - 8f, y), pct > 97 ? COLOR_BAD : COLOR_ROW_TEXT, 0.32f, TextAlignment.RIGHT);
}

private Color RowValueColor(Color valueColor)
{
    if (valueColor.PackedValue == COLOR_OK.PackedValue || valueColor.PackedValue == COLOR_BAD.PackedValue)
        return valueColor;
    return COLOR_ROW_TEXT;
}

private string StateText()
{
    if (!coreFound) return "NO CORE";
    if (!coreEnabled) return "CORE OFF";
    if (!logisticsEnabled) return "DISABLED";
    if (globalPause) return "PAUSED";
    return lastStatus.ToUpperInvariant();
}

private string DisplayItem(string subtype)
{
    if (string.IsNullOrEmpty(subtype)) return "";
    string r = subtype.Replace("SteelPlate", "Steel Plate").Replace("InteriorPlate", "Interior Plate");
    return r;
}

private string ShortName(IMyTerminalBlock b)
{
    if (b == null) return "";
    return b.CustomName;
}

private string ShortText(string text, int max)
{
    if (string.IsNullOrEmpty(text) || text.Length <= max) return text;
    return text.Substring(0, Math.Max(1, max - 2)) + "..";
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

private void EchoStatus()
{
    Echo("AutoGrid Manager Logistics");
    Echo("Version: " + VERSION);
    Echo("Core   : " + (coreFound ? "CONNECTED" : "MISSING"));
    Echo("State  : " + StateText());
    Echo("Cargo  : " + cargos.Count);
    Echo("Sources: " + sources.Count);
    Echo("Moved  : " + lastMoves);
    if (warning.Length > 0) Echo("Warn   : " + warning);
}

private string StripComment(string line)
{
    int idx = line.IndexOf("//");
    if (idx >= 0) return line.Substring(0, idx);
    idx = line.IndexOf(';');
    if (idx >= 0) return line.Substring(0, idx);
    return line;
}

private bool TrySplitKeyValue(string line, char sep, out string key, out string value)
{
    key = "";
    value = "";
    int idx = line.IndexOf(sep);
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
