// ============================================================================
// AGM - AutoGrid Manager
// Author: RevGamer
// Build: 0.3.0 test
//
// Space Engineers Programmable Block script.
// Paste this file directly into the in-game PB editor. Do not add using
// statements, namespaces, or a Program class wrapper.
// ============================================================================

// -------------------------------------------------------------------------
// Tags / defaults
// -------------------------------------------------------------------------

private const string VERSION       = "0.3.0-test";
private const string PB_TAG        = "{AGM-Main}";
private const string LCD_TAG       = "[AGM-S]";
private const string BRAND_TITLE   = "AGM";
private const string BRAND_NAME    = "AutoGrid Manager";
private const string BRAND_AUTHOR  = "by RevGamer";

private const float BOOT_SECONDS   = 4.0f;
private const int   RESCAN_TICKS   = 100;

// -------------------------------------------------------------------------
// Colours
// -------------------------------------------------------------------------

private readonly Color COLOR_BG        = new Color(13, 9, 5);
private readonly Color COLOR_PANEL     = new Color(28, 19, 10);
private readonly Color COLOR_PANEL_2   = new Color(42, 29, 13);
private readonly Color COLOR_ACCENT    = new Color(255, 174, 48);
private readonly Color COLOR_ACCENT_2  = new Color(255, 213, 91);
private readonly Color COLOR_TEXT      = new Color(236, 218, 177);
private readonly Color COLOR_DIM       = new Color(120, 94, 58);
private readonly Color COLOR_OK        = new Color(75, 210, 120);
private readonly Color COLOR_WARN      = new Color(255, 142, 45);
private readonly Color COLOR_LOW       = new Color(226, 64, 45);

// -------------------------------------------------------------------------
// Data
// -------------------------------------------------------------------------

private enum ScreenMode { Normal, Wide, Vertical }
private enum ScreenKind { Inventory, Power }

private struct ScreenCommand
{
    public ScreenKind Kind;
    public ScreenMode Mode;
    public string     Category;
    public int        Page;
    public int        RowsPerPage;
    public string     Join;
    public string     PowerProfile;
}

private class ItemTotal
{
    public string     Category;
    public string     Key;
    public string     DisplayName;
    public string     SpriteName;
    public double     Amount;
}

private class PowerConfig
{
    public string Name;
    public bool   Found;
    public bool   IncludeUngrouped;
    public string BatteriesGroup;
    public string ReactorsGroup;
    public string SolarGroup;
    public string WindGroup;
    public string HydrogenGroup;
    public string OtherGroup;
}

// -------------------------------------------------------------------------
// Runtime state
// -------------------------------------------------------------------------

private bool     booting       = true;
private double   bootElapsed   = 0.0;
private int      tickCounter   = 0;
private DateTime lastRun       = DateTime.Now;

private readonly List<IMyTerminalBlock> allBlocks       = new List<IMyTerminalBlock>();
private readonly List<IMyTerminalBlock> lcdBlocks       = new List<IMyTerminalBlock>();
private readonly List<IMyInventory>     inventories     = new List<IMyInventory>();
private readonly List<IMyBatteryBlock>  batteries       = new List<IMyBatteryBlock>();
private readonly List<IMyPowerProducer> powerProducers  = new List<IMyPowerProducer>();
private readonly List<IMyTerminalBlock> powerGroupBlocks = new List<IMyTerminalBlock>();
private readonly List<MyInventoryItem>  itemBuffer      = new List<MyInventoryItem>();
private readonly List<ItemTotal>        itemTotals      = new List<ItemTotal>();
private readonly Dictionary<string, ItemTotal> totalsByKey = new Dictionary<string, ItemTotal>();
private readonly HashSet<long>          selectedPowerIds = new HashSet<long>();
private readonly StringBuilder          sb              = new StringBuilder();

// -------------------------------------------------------------------------
// Constructor / entry points
// -------------------------------------------------------------------------

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
    RescanBlocks();
    StartBoot();
}

public void Save()
{
}

public void Main(string argument, UpdateType updateSource)
{
    string arg = argument == null ? "" : argument.Trim().ToLowerInvariant();

    if (arg == "reload" || arg == "rescan")
    {
        RescanBlocks();
        StartBoot();
    }
    else if (arg == "reboot")
    {
        StartBoot();
    }
    else if (arg == "reset")
    {
        itemTotals.Clear();
        totalsByKey.Clear();
        RescanBlocks();
        StartBoot();
    }

    double dt = (DateTime.Now - lastRun).TotalSeconds;
    if (dt < 0.0 || dt > 2.0) dt = 0.166;
    lastRun = DateTime.Now;

    if ((updateSource & UpdateType.Update100) != 0)
    {
        tickCounter += 100;
        RescanBlocks();
        IndexInventory();
    }
    else
    {
        tickCounter += 10;
    }

    if (booting)
    {
        bootElapsed += dt;
        double progress = Math.Min(1.0, bootElapsed / BOOT_SECONDS);
        DrawBootAll(progress);
        EchoBoot(progress);
        if (progress >= 1.0)
            booting = false;
        return;
    }

    IndexInventory();
    DrawAllScreens();
    DrawPbStatus();
    EchoStatus();
}

// -------------------------------------------------------------------------
// Boot / scan
// -------------------------------------------------------------------------

private void StartBoot()
{
    booting = true;
    bootElapsed = 0.0;
    lastRun = DateTime.Now;
}

private void RescanBlocks()
{
    allBlocks.Clear();
    lcdBlocks.Clear();
    inventories.Clear();
    batteries.Clear();
    powerProducers.Clear();

    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(allBlocks, b => b.IsSameConstructAs(Me));

    foreach (var block in allBlocks)
    {
        if (block.HasInventory)
        {
            for (int i = 0; i < block.InventoryCount; i++)
                inventories.Add(block.GetInventory(i));
        }

        if (block.CustomName.Contains(LCD_TAG) && block is IMyTextSurfaceProvider)
            lcdBlocks.Add(block);

        var battery = block as IMyBatteryBlock;
        if (battery != null)
            batteries.Add(battery);

        var producer = block as IMyPowerProducer;
        if (producer != null && battery == null)
            powerProducers.Add(producer);
    }
}

private bool IsMainPb()
{
    return Me.CustomName.Contains(PB_TAG);
}

// -------------------------------------------------------------------------
// Inventory indexing
// -------------------------------------------------------------------------

private void IndexInventory()
{
    totalsByKey.Clear();
    itemTotals.Clear();

    foreach (var inv in inventories)
    {
        itemBuffer.Clear();
        inv.GetItems(itemBuffer);

        for (int i = 0; i < itemBuffer.Count; i++)
        {
            MyInventoryItem item = itemBuffer[i];
            string category = GetCategory(item.Type);
            if (category.Length == 0)
                continue;

            string key = category + "/" + item.Type.SubtypeId;
            ItemTotal total;
            if (!totalsByKey.TryGetValue(key, out total))
            {
                total = new ItemTotal();
                total.Category    = category;
                total.Key         = key;
                total.DisplayName = MakeDisplayName(item.Type.SubtypeId);
                total.SpriteName  = item.Type.TypeId + "/" + item.Type.SubtypeId;
                total.Amount      = 0.0;
                totalsByKey[key] = total;
                itemTotals.Add(total);
            }

            total.Amount += (double)item.Amount;
        }
    }

    itemTotals.Sort((a, b) =>
    {
        int c = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
        if (c != 0) return c;
        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
    });
}

private string GetCategory(MyItemType type)
{
    string typeId = type.TypeId.ToString();

    if (typeId.EndsWith("_Ore")) return "Ore";
    if (typeId.EndsWith("_Ingot")) return "Ingot";
    if (typeId.EndsWith("_Component")) return "Component";
    if (typeId.EndsWith("_AmmoMagazine")) return "Ammo";
    if (typeId.EndsWith("_PhysicalGunObject")) return "Tool";
    if (typeId.EndsWith("_GasContainerObject")) return "Bottle";
    if (typeId.EndsWith("_OxygenContainerObject")) return "Bottle";

    return "";
}

private string MakeDisplayName(string subtype)
{
    if (string.IsNullOrEmpty(subtype))
        return "Unknown";

    string result = subtype;
    result = result.Replace("SteelPlate", "Steel Plate");
    result = result.Replace("InteriorPlate", "Interior Plate");
    result = result.Replace("MetalGrid", "Metal Grid");
    result = result.Replace("SmallTube", "Small Tube");
    result = result.Replace("LargeTube", "Large Tube");
    result = result.Replace("BulletproofGlass", "Bulletproof Glass");
    result = result.Replace("PowerCell", "Power Cell");
    result = result.Replace("SolarCell", "Solar Cell");
    result = result.Replace("RadioCommunication", "Radio Components");
    result = result.Replace("GravityGenerator", "Gravity Components");
    result = result.Replace("NATO_25x184mm", "Gatling Ammo");
    result = result.Replace("NATO_5p56x45mm", "S-20A Magazine");
    result = result.Replace("Missile200mm", "Rocket");
    result = result.Replace("AutocannonClip", "Autocannon Mag");
    result = result.Replace("MediumCalibreAmmo", "Assault Shell");
    result = result.Replace("LargeCalibreAmmo", "Artillery Shell");
    result = result.Replace("HydrogenBottle", "Hydrogen Bottle");
    result = result.Replace("OxygenBottle", "Oxygen Bottle");
    return result;
}

// -------------------------------------------------------------------------
// Commands
// -------------------------------------------------------------------------

private bool TryParseCommand(string raw, out ScreenCommand cmd)
{
    cmd = default(ScreenCommand);
    cmd.Kind = ScreenKind.Inventory;
    cmd.Mode = ScreenMode.Normal;
    cmd.Category = "Component";
    cmd.Page = 1;
    cmd.RowsPerPage = 0;
    cmd.Join = "";
    cmd.PowerProfile = "";

    if (string.IsNullOrEmpty(raw))
        return false;

    string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.Length == 0)
            continue;

        string key, value;
        if (!TrySplitKeyValue(line, '=', out key, out value))
        {
            if (string.Equals(line, "PowerDashboard", StringComparison.OrdinalIgnoreCase)
                || string.Equals(line, "Power", StringComparison.OrdinalIgnoreCase)
                || string.Equals(line, "IndustrialPower", StringComparison.OrdinalIgnoreCase))
            {
                cmd.Kind = ScreenKind.Power;
                cmd.PowerProfile = "";
                return true;
            }
            continue;
        }

        if (string.Equals(key, "PowerDashboard", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Power", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "IndustrialPower", StringComparison.OrdinalIgnoreCase))
        {
            cmd.Kind = ScreenKind.Power;
            cmd.PowerProfile = value.Trim();
            return true;
        }

        if (string.Equals(key, "IndustrialInventory", StringComparison.OrdinalIgnoreCase))
        {
            cmd.Mode = ScreenMode.Normal;
            ParseCommandValue(value, ref cmd);
            return true;
        }

        if (string.Equals(key, "IndustrialInventoryWide", StringComparison.OrdinalIgnoreCase))
        {
            cmd.Mode = ScreenMode.Wide;
            ParseCommandValue(value, ref cmd);
            return true;
        }

        if (string.Equals(key, "IndustrialInventoryVertical", StringComparison.OrdinalIgnoreCase))
        {
            cmd.Mode = ScreenMode.Vertical;
            ParseCommandValue(value, ref cmd);
            return true;
        }
    }

    return false;
}

private void ParseCommandValue(string value, ref ScreenCommand cmd)
{
    string[] parts = value.Split(':');
    if (parts.Length > 0 && parts[0].Trim().Length > 0)
        cmd.Category = NormaliseCategory(parts[0].Trim());

    if (parts.Length > 1)
    {
        int page;
        if (int.TryParse(parts[1].Trim(), out page) && page > 0)
            cmd.Page = page;
    }

    if (parts.Length > 2)
    {
        int rows;
        if (int.TryParse(parts[2].Trim(), out rows) && rows > 0)
            cmd.RowsPerPage = rows;
    }

    if (parts.Length > 3)
        cmd.Join = parts[3].Trim().ToLowerInvariant();
}

private string NormaliseCategory(string value)
{
    if (string.Equals(value, "Ores", StringComparison.OrdinalIgnoreCase)) return "Ore";
    if (string.Equals(value, "Ingots", StringComparison.OrdinalIgnoreCase)) return "Ingot";
    if (string.Equals(value, "Components", StringComparison.OrdinalIgnoreCase)) return "Component";
    if (string.Equals(value, "AmmoMagazine", StringComparison.OrdinalIgnoreCase)) return "Ammo";
    if (string.Equals(value, "Ammunition", StringComparison.OrdinalIgnoreCase)) return "Ammo";
    if (string.Equals(value, "Bottles", StringComparison.OrdinalIgnoreCase)) return "Bottle";
    return value;
}

private bool TrySplitKeyValue(string line, char sep, out string key, out string value)
{
    key = "";
    value = "";
    int idx = line.IndexOf(sep);
    if (idx < 1)
        return false;

    key = line.Substring(0, idx).Trim();
    value = line.Substring(idx + 1).Trim();
    return key.Length > 0;
}

private string StripComment(string line)
{
    int idx = line.IndexOf(';');
    if (idx >= 0)
        return line.Substring(0, idx);
    return line;
}

// -------------------------------------------------------------------------
// Screen rendering
// -------------------------------------------------------------------------

private void DrawAllScreens()
{
    foreach (var block in lcdBlocks)
    {
        var provider = block as IMyTextSurfaceProvider;
        if (provider == null)
            continue;

        ScreenCommand cmd;
        if (!TryParseCommand(block.CustomData, out cmd))
        {
            DrawNoCommand(provider.GetSurface(0), block.CustomName);
            continue;
        }

        if (cmd.Kind == ScreenKind.Power)
            DrawPowerDashboard(provider.GetSurface(0), cmd.PowerProfile);
        else
            DrawInventoryScreen(provider.GetSurface(0), cmd);
    }
}

private void DrawBootAll(double progress)
{
    if (IsMainPb())
        DrawBootSurface(Me.GetSurface(0), progress);

    foreach (var block in lcdBlocks)
    {
        var provider = block as IMyTextSurfaceProvider;
        if (provider != null)
            DrawBootSurface(provider.GetSurface(0), progress);
    }
}

private void DrawBootSurface(IMyTextSurface surface, double progress)
{
    PrepareSurface(surface, 0.95f);
    RectangleF vp = Viewport(surface);

    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        DrawBorder(frame, vp, COLOR_ACCENT, 4f, true, true, true, true);

        Vector2 center = vp.Position + vp.Size * 0.5f;
        DrawText(frame, BRAND_TITLE, center + new Vector2(0, -82), COLOR_ACCENT_2, 3.4f, TextAlignment.CENTER);
        DrawText(frame, BRAND_NAME, center + new Vector2(0, -22), COLOR_TEXT, 1.1f, TextAlignment.CENTER);
        DrawText(frame, BRAND_AUTHOR, center + new Vector2(0, 18), COLOR_DIM, 0.85f, TextAlignment.CENTER);

        float barW = Math.Min(vp.Width * 0.70f, 360f);
        RectangleF bar = new RectangleF(center.X - barW * 0.5f, center.Y + 66f, barW, 20f);
        Fill(frame, bar, COLOR_PANEL_2);
        Fill(frame, new RectangleF(bar.X, bar.Y, (float)(bar.Width * progress), bar.Height), COLOR_ACCENT);
        DrawBorder(frame, bar, COLOR_ACCENT_2, 2f, true, true, true, true);

        DrawText(frame, "REBOOT " + ((int)(progress * 100.0)).ToString() + "%", center + new Vector2(0, 112), COLOR_TEXT, 0.8f, TextAlignment.CENTER);
    }
}

private void DrawPbStatus()
{
    if (!IsMainPb())
        return;

    IMyTextSurface surface = Me.GetSurface(0);
    PrepareSurface(surface, 0.85f);
    RectangleF vp = Viewport(surface);

    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        DrawBorder(frame, vp, COLOR_ACCENT, 3f, true, true, true, true);

        Vector2 center = vp.Position + vp.Size * 0.5f;
        DrawText(frame, BRAND_TITLE, center + new Vector2(0, -90), COLOR_ACCENT_2, 2.9f, TextAlignment.CENTER);
        DrawText(frame, BRAND_NAME, center + new Vector2(0, -42), COLOR_TEXT, 0.9f, TextAlignment.CENTER);
        DrawText(frame, "ONLINE", center + new Vector2(0, 0), COLOR_OK, 1.15f, TextAlignment.CENTER);

        DrawText(frame, "LCDs  : " + lcdBlocks.Count, center + new Vector2(-110, 48), COLOR_TEXT, 0.72f, TextAlignment.LEFT);
        DrawText(frame, "Cargo : " + inventories.Count, center + new Vector2(-110, 76), COLOR_TEXT, 0.72f, TextAlignment.LEFT);
        DrawText(frame, "Items : " + itemTotals.Count, center + new Vector2(-110, 104), COLOR_TEXT, 0.72f, TextAlignment.LEFT);
        DrawText(frame, "v" + VERSION, center + new Vector2(0, 138), COLOR_DIM, 0.58f, TextAlignment.CENTER);
    }
}

private void DrawNoCommand(IMyTextSurface surface, string name)
{
    PrepareSurface(surface, 0.9f);
    RectangleF vp = Viewport(surface);

    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        DrawBorder(frame, vp, COLOR_ACCENT, 3f, true, true, true, true);
        Vector2 center = vp.Position + vp.Size * 0.5f;
        DrawText(frame, "AGM SCREEN READY", center + new Vector2(0, -32), COLOR_ACCENT_2, 1.1f, TextAlignment.CENTER);
        DrawText(frame, "Add IndustrialInventory=Component", center + new Vector2(0, 16), COLOR_TEXT, 0.7f, TextAlignment.CENTER);
        DrawText(frame, "or PowerDashboard to Custom Data", center + new Vector2(0, 44), COLOR_DIM, 0.65f, TextAlignment.CENTER);
    }
}

private void DrawInventoryScreen(IMyTextSurface surface, ScreenCommand cmd)
{
    PrepareSurface(surface, 0.78f);
    RectangleF vp = Viewport(surface);

    List<ItemTotal> rows = GetCategoryRows(cmd.Category);
    int rowsPerPage = cmd.RowsPerPage > 0 ? cmd.RowsPerPage : AutoRows(vp);
    int pageCount = Math.Max(1, (rows.Count + rowsPerPage - 1) / rowsPerPage);
    int page = Math.Max(1, Math.Min(cmd.Page, pageCount));
    int start = (page - 1) * rowsPerPage;
    int end = Math.Min(rows.Count, start + rowsPerPage);

    bool leftBorder = true;
    bool rightBorder = true;
    bool topBorder = true;
    bool bottomBorder = true;

    if (cmd.Mode == ScreenMode.Wide)
    {
        leftBorder = cmd.Join != "right" && cmd.Join != "middle";
        rightBorder = cmd.Join != "left" && cmd.Join != "middle";
    }
    else if (cmd.Mode == ScreenMode.Vertical)
    {
        topBorder = cmd.Join != "bottom" && cmd.Join != "middle";
        bottomBorder = cmd.Join != "top" && cmd.Join != "middle";
    }

    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f, leftBorder, rightBorder, topBorder, bottomBorder);

        string title = cmd.Category.ToUpperInvariant() + " STOCK";
        if (cmd.Mode == ScreenMode.Vertical && (cmd.Join == "bottom" || cmd.Join == "middle"))
            title = "";

        if (title.Length > 0)
            DrawText(frame, title, panel.Position + new Vector2(24, 24), COLOR_ACCENT_2, 1.0f, TextAlignment.LEFT);

        DrawText(frame, "PAGE " + page + "/" + pageCount, panel.Position + new Vector2(panel.Width - 24, 26), COLOR_DIM, 0.62f, TextAlignment.RIGHT);

        float top = panel.Y + (title.Length > 0 ? 70f : 22f);
        float rowH = Math.Max(34f, (panel.Bottom - top - 26f) / Math.Max(1, rowsPerPage));

        if (rows.Count == 0)
        {
            DrawText(frame, "NO " + cmd.Category.ToUpperInvariant() + " ITEMS FOUND", panel.Position + panel.Size * 0.5f, COLOR_WARN, 0.9f, TextAlignment.CENTER);
        }
        else
        {
            int drawIndex = 0;
            for (int i = start; i < end; i++)
            {
                DrawItemRow(frame, panel, top + drawIndex * rowH, rowH - 5f, rows[i]);
                drawIndex++;
            }
        }

        if (end < rows.Count)
            DrawText(frame, "+ " + (rows.Count - end) + " MORE", new Vector2(panel.X + panel.Width - 24, panel.Bottom - 22), COLOR_ACCENT, 0.62f, TextAlignment.RIGHT);
    }
}

private List<ItemTotal> GetCategoryRows(string category)
{
    List<ItemTotal> rows = new List<ItemTotal>();
    for (int i = 0; i < itemTotals.Count; i++)
    {
        if (string.Equals(itemTotals[i].Category, category, StringComparison.OrdinalIgnoreCase))
            rows.Add(itemTotals[i]);
    }
    return rows;
}

private int AutoRows(RectangleF vp)
{
    int rows = (int)((vp.Height - 110f) / 42f);
    if (rows < 4) rows = 4;
    if (rows > 18) rows = 18;
    return rows;
}

private void DrawItemRow(MySpriteDrawFrame frame, RectangleF panel, float y, float h, ItemTotal item)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, h);
    Fill(frame, row, COLOR_PANEL_2);

    float iconSize = Math.Min(h - 8f, 34f);
    Vector2 iconCenter = new Vector2(row.X + 26f, row.Y + row.Height * 0.5f);
    TryDrawSprite(frame, item.SpriteName, iconCenter, new Vector2(iconSize, iconSize), Color.White);

    DrawText(frame, item.DisplayName, new Vector2(row.X + 54f, row.Y + row.Height * 0.5f - 10f), COLOR_TEXT, 0.58f, TextAlignment.LEFT);
    DrawText(frame, FormatAmount(item.Amount), new Vector2(row.X + row.Width - 170f, row.Y + row.Height * 0.5f - 10f), COLOR_ACCENT_2, 0.58f, TextAlignment.RIGHT);

    float barW = 120f;
    RectangleF bar = new RectangleF(row.Right - barW - 12f, row.Y + row.Height * 0.5f - 6f, barW, 12f);
    Fill(frame, bar, COLOR_BG);

    float fill = EstimateBarFill(item.Amount);
    Color fillColor = fill < 0.25f ? COLOR_LOW : (fill < 0.60f ? COLOR_WARN : COLOR_OK);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * fill, bar.Height), fillColor);
    DrawBorder(frame, bar, COLOR_DIM, 1f, true, true, true, true);
}

private float EstimateBarFill(double amount)
{
    if (amount <= 0.0) return 0.0f;
    double scale = 1000.0;
    if (amount > 10000.0) scale = 100000.0;
    else if (amount > 1000.0) scale = 10000.0;
    return (float)Math.Min(1.0, amount / scale);
}

private string FormatAmount(double amount)
{
    if (amount >= 1000000.0)
        return (amount / 1000000.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "M";
    if (amount >= 1000.0)
        return (amount / 1000.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "K";
    return amount.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
}

// -------------------------------------------------------------------------
// Power dashboard
// -------------------------------------------------------------------------

private void DrawPowerDashboard(IMyTextSurface surface, string profileName)
{
    PrepareSurface(surface, 0.78f);
    RectangleF vp = Viewport(surface);
    PowerConfig cfg = GetPowerConfig(profileName);
    selectedPowerIds.Clear();

    double stored = 0.0;
    double capacity = 0.0;
    double batteryIn = 0.0;
    double batteryOut = 0.0;

    double reactorNow = 0.0;
    double reactorMax = 0.0;
    double h2Now = 0.0;
    double h2Max = 0.0;
    double solarNow = 0.0;
    double solarMax = 0.0;
    double windNow = 0.0;
    double windMax = 0.0;
    double otherNow = 0.0;
    double otherMax = 0.0;
    int batteryCount = 0;
    int producerCount = 0;

    if (cfg.Found)
    {
        AddConfiguredBatteries(cfg.BatteriesGroup, ref stored, ref capacity, ref batteryIn, ref batteryOut, ref batteryCount);
        AddConfiguredProducers(cfg.ReactorsGroup, "reactor", ref reactorNow, ref reactorMax, ref producerCount);
        AddConfiguredProducers(cfg.HydrogenGroup, "hydrogen", ref h2Now, ref h2Max, ref producerCount);
        AddConfiguredProducers(cfg.SolarGroup, "solar", ref solarNow, ref solarMax, ref producerCount);
        AddConfiguredProducers(cfg.WindGroup, "wind", ref windNow, ref windMax, ref producerCount);
        AddConfiguredProducers(cfg.OtherGroup, "other", ref otherNow, ref otherMax, ref producerCount);

        if (cfg.IncludeUngrouped)
        {
            AddUngroupedBatteries(ref stored, ref capacity, ref batteryIn, ref batteryOut, ref batteryCount);
            AddUngroupedProducers(ref reactorNow, ref reactorMax, ref h2Now, ref h2Max, ref solarNow, ref solarMax, ref windNow, ref windMax, ref otherNow, ref otherMax, ref producerCount);
        }
    }
    else
    {
        foreach (var battery in batteries)
        {
            stored += battery.CurrentStoredPower;
            capacity += battery.MaxStoredPower;
            batteryIn += battery.CurrentInput;
            batteryOut += battery.CurrentOutput;
            batteryCount++;
        }

        AddUngroupedProducers(ref reactorNow, ref reactorMax, ref h2Now, ref h2Max, ref solarNow, ref solarMax, ref windNow, ref windMax, ref otherNow, ref otherMax, ref producerCount);
    }

    double production = reactorNow + h2Now + solarNow + windNow + otherNow + batteryOut;
    double maxProduction = reactorMax + h2Max + solarMax + windMax + otherMax + batteryOut + batteryIn;
    double storageFill = capacity > 0.0 ? stored / capacity : 0.0;
    double net = production - batteryIn;

    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f, true, true, true, true);

        DrawText(frame, "POWER DASHBOARD", panel.Position + new Vector2(24, 24), COLOR_ACCENT_2, 1.0f, TextAlignment.LEFT);
        DrawText(frame, cfg.Found ? cfg.Name : "AGM", panel.Position + new Vector2(panel.Width - 24, 26), COLOR_DIM, 0.62f, TextAlignment.RIGHT);

        float y = panel.Y + 72f;
        DrawMetricRow(frame, panel, y, "Battery", stored, capacity, "MWh", storageFill, "IconEnergy");
        y += 46f;

        DrawMetricRow(frame, panel, y, "Output", production, maxProduction, "MW", maxProduction > 0.0 ? production / maxProduction : 0.0, "IconEnergy");
        y += 46f;

        Color netColor = net >= 0.0 ? COLOR_OK : COLOR_LOW;
        DrawText(frame, "NET " + FormatPower(net, "MW"), new Vector2(panel.X + 24f, y + 8f), netColor, 0.75f, TextAlignment.LEFT);
        DrawText(frame, "BAT IN " + FormatPower(batteryIn, "MW") + "   OUT " + FormatPower(batteryOut, "MW"),
            new Vector2(panel.Right - 24f, y + 8f), COLOR_DIM, 0.58f, TextAlignment.RIGHT);
        y += 48f;

        DrawText(frame, "SOURCES", new Vector2(panel.X + 24f, y), COLOR_ACCENT, 0.72f, TextAlignment.LEFT);
        y += 30f;

        if (!cfg.Found || !string.IsNullOrEmpty(cfg.ReactorsGroup) || cfg.IncludeUngrouped)
        {
            DrawPowerSourceRow(frame, panel, y, "Reactors", reactorNow, reactorMax, batteryCount == 0 && producerCount == 0 ? COLOR_DIM : COLOR_TEXT);
            y += 34f;
        }
        if (!cfg.Found || !string.IsNullOrEmpty(cfg.HydrogenGroup) || cfg.IncludeUngrouped)
        {
            DrawPowerSourceRow(frame, panel, y, "Hydrogen", h2Now, h2Max, COLOR_TEXT);
            y += 34f;
        }
        if (!cfg.Found || !string.IsNullOrEmpty(cfg.SolarGroup) || cfg.IncludeUngrouped)
        {
            DrawPowerSourceRow(frame, panel, y, "Solar", solarNow, solarMax, COLOR_TEXT);
            y += 34f;
        }
        if (!cfg.Found || !string.IsNullOrEmpty(cfg.WindGroup) || cfg.IncludeUngrouped)
        {
            DrawPowerSourceRow(frame, panel, y, "Wind", windNow, windMax, COLOR_TEXT);
            y += 34f;
        }

        if ((!cfg.Found || !string.IsNullOrEmpty(cfg.OtherGroup) || cfg.IncludeUngrouped) && (otherMax > 0.0 || otherNow > 0.0))
            DrawPowerSourceRow(frame, panel, y, "Other", otherNow, otherMax, COLOR_TEXT);

        DrawText(frame, batteryCount + " batteries  |  " + producerCount + " producers",
            new Vector2(panel.X + 24f, panel.Bottom - 24f), COLOR_DIM, 0.55f, TextAlignment.LEFT);
    }
}

private PowerConfig GetPowerConfig(string wantedName)
{
    PowerConfig cfg = new PowerConfig();
    cfg.Name = "";
    cfg.Found = false;
    cfg.IncludeUngrouped = true;
    cfg.BatteriesGroup = "";
    cfg.ReactorsGroup = "";
    cfg.SolarGroup = "";
    cfg.WindGroup = "";
    cfg.HydrogenGroup = "";
    cfg.OtherGroup = "";

    string raw = Me.CustomData;
    if (string.IsNullOrEmpty(raw))
        return cfg;

    string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    bool inSection = false;
    bool capturedAny = false;
    string wanted = wantedName == null ? "" : wantedName.Trim();

    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.Length == 0)
            continue;

        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            string inner = line.Substring(1, line.Length - 2).Trim();
            inSection = false;

            if (inner.StartsWith("power:", StringComparison.OrdinalIgnoreCase))
            {
                string name = inner.Substring(6).Trim();
                if (wanted.Length == 0 || string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    cfg.Name = name;
                    cfg.Found = true;
                    cfg.IncludeUngrouped = false;
                    inSection = true;
                    capturedAny = true;
                }
                else if (capturedAny)
                {
                    break;
                }
            }
            continue;
        }

        if (!inSection)
            continue;

        string key, value;
        if (!TrySplitKeyValue(line, '=', out key, out value))
            continue;

        value = CleanGroupName(value);
        if (string.Equals(key, "batteries", StringComparison.OrdinalIgnoreCase)) cfg.BatteriesGroup = value;
        else if (string.Equals(key, "reactors", StringComparison.OrdinalIgnoreCase)) cfg.ReactorsGroup = value;
        else if (string.Equals(key, "solar", StringComparison.OrdinalIgnoreCase)) cfg.SolarGroup = value;
        else if (string.Equals(key, "wind", StringComparison.OrdinalIgnoreCase)) cfg.WindGroup = value;
        else if (string.Equals(key, "hydrogen", StringComparison.OrdinalIgnoreCase)) cfg.HydrogenGroup = value;
        else if (string.Equals(key, "other", StringComparison.OrdinalIgnoreCase)) cfg.OtherGroup = value;
        else if (string.Equals(key, "include_ungrouped", StringComparison.OrdinalIgnoreCase)) cfg.IncludeUngrouped = ParseBool(value, false);
    }

    if (cfg.Found && string.IsNullOrEmpty(cfg.Name))
        cfg.Name = "Power";

    return cfg;
}

private string CleanGroupName(string value)
{
    if (value == null)
        return "";
    value = value.Trim();
    if (value.StartsWith("G:", StringComparison.OrdinalIgnoreCase))
        value = value.Substring(2).Trim();
    if (value.Length >= 2 && value[0] == '{' && value[value.Length - 1] == '}')
        value = value.Substring(1, value.Length - 2).Trim();
    return value;
}

private bool ParseBool(string value, bool fallback)
{
    if (value == null)
        return fallback;
    string v = value.Trim().ToLowerInvariant();
    if (v == "true" || v == "yes" || v == "1" || v == "on")
        return true;
    if (v == "false" || v == "no" || v == "0" || v == "off")
        return false;
    return fallback;
}

private void AddConfiguredBatteries(string groupName, ref double stored, ref double capacity, ref double input, ref double output, ref int count)
{
    if (string.IsNullOrEmpty(groupName))
        return;

    powerGroupBlocks.Clear();
    var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
    if (group == null)
        return;

    group.GetBlocks(powerGroupBlocks, b => b.IsSameConstructAs(Me));
    foreach (var block in powerGroupBlocks)
    {
        var battery = block as IMyBatteryBlock;
        if (battery == null)
            continue;
        if (selectedPowerIds.Contains(battery.EntityId))
            continue;

        selectedPowerIds.Add(battery.EntityId);
        stored += battery.CurrentStoredPower;
        capacity += battery.MaxStoredPower;
        input += battery.CurrentInput;
        output += battery.CurrentOutput;
        count++;
    }
}

private void AddConfiguredProducers(string groupName, string kind, ref double now, ref double max, ref int count)
{
    if (string.IsNullOrEmpty(groupName))
        return;

    powerGroupBlocks.Clear();
    var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
    if (group == null)
        return;

    group.GetBlocks(powerGroupBlocks, b => b.IsSameConstructAs(Me));
    foreach (var block in powerGroupBlocks)
    {
        var producer = block as IMyPowerProducer;
        if (producer == null || producer is IMyBatteryBlock)
            continue;
        if (selectedPowerIds.Contains(block.EntityId))
            continue;
        if (!PowerProducerMatches(producer, kind))
            continue;

        selectedPowerIds.Add(block.EntityId);
        now += producer.CurrentOutput;
        max += producer.MaxOutput;
        count++;
    }
}

private void AddUngroupedBatteries(ref double stored, ref double capacity, ref double input, ref double output, ref int count)
{
    foreach (var battery in batteries)
    {
        if (selectedPowerIds.Contains(battery.EntityId))
            continue;

        selectedPowerIds.Add(battery.EntityId);
        stored += battery.CurrentStoredPower;
        capacity += battery.MaxStoredPower;
        input += battery.CurrentInput;
        output += battery.CurrentOutput;
        count++;
    }
}

private void AddUngroupedProducers(ref double reactorNow, ref double reactorMax, ref double h2Now, ref double h2Max, ref double solarNow, ref double solarMax, ref double windNow, ref double windMax, ref double otherNow, ref double otherMax, ref int count)
{
    foreach (var producer in powerProducers)
    {
        var block = producer as IMyTerminalBlock;
        if (block == null)
            continue;
        if (selectedPowerIds.Contains(block.EntityId))
            continue;

        selectedPowerIds.Add(block.EntityId);
        count++;

        if (PowerProducerMatches(producer, "reactor"))
        {
            reactorNow += producer.CurrentOutput;
            reactorMax += producer.MaxOutput;
        }
        else if (PowerProducerMatches(producer, "hydrogen"))
        {
            h2Now += producer.CurrentOutput;
            h2Max += producer.MaxOutput;
        }
        else if (PowerProducerMatches(producer, "solar"))
        {
            solarNow += producer.CurrentOutput;
            solarMax += producer.MaxOutput;
        }
        else if (PowerProducerMatches(producer, "wind"))
        {
            windNow += producer.CurrentOutput;
            windMax += producer.MaxOutput;
        }
        else
        {
            otherNow += producer.CurrentOutput;
            otherMax += producer.MaxOutput;
        }
    }
}

private bool PowerProducerMatches(IMyPowerProducer producer, string kind)
{
    var block = producer as IMyTerminalBlock;
    if (block == null)
        return false;

    string type = block.BlockDefinition.TypeIdString + "/" + block.BlockDefinition.SubtypeId;
    if (kind == "reactor")
        return producer is IMyReactor || type.IndexOf("Reactor", StringComparison.OrdinalIgnoreCase) >= 0;
    if (kind == "hydrogen")
        return type.IndexOf("HydrogenEngine", StringComparison.OrdinalIgnoreCase) >= 0;
    if (kind == "solar")
        return type.IndexOf("Solar", StringComparison.OrdinalIgnoreCase) >= 0;
    if (kind == "wind")
        return type.IndexOf("Wind", StringComparison.OrdinalIgnoreCase) >= 0;

    return true;
}

private void DrawMetricRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, double value, double max, string unit, double ratio, string icon)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 38f);
    Fill(frame, row, COLOR_PANEL_2);
    TryDrawSprite(frame, icon, new Vector2(row.X + 22f, row.Y + row.Height * 0.5f), new Vector2(28f, 28f), Color.White);
    DrawText(frame, label, new Vector2(row.X + 48f, row.Y + 10f), COLOR_TEXT, 0.62f, TextAlignment.LEFT);
    DrawText(frame, FormatPower(value, unit) + " / " + FormatPower(max, unit), new Vector2(row.Right - 150f, row.Y + 10f), COLOR_ACCENT_2, 0.58f, TextAlignment.RIGHT);

    float barW = 120f;
    RectangleF bar = new RectangleF(row.Right - barW - 12f, row.Y + row.Height * 0.5f - 6f, barW, 12f);
    Fill(frame, bar, COLOR_BG);
    float fill = (float)Math.Max(0.0, Math.Min(1.0, ratio));
    Color fillColor = fill < 0.25f ? COLOR_LOW : (fill < 0.60f ? COLOR_WARN : COLOR_OK);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * fill, bar.Height), fillColor);
    DrawBorder(frame, bar, COLOR_DIM, 1f, true, true, true, true);
}

private void DrawPowerSourceRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, double value, double max, Color textColor)
{
    double ratio = max > 0.0 ? value / max : 0.0;
    float barW = panel.Width * 0.34f;
    RectangleF bar = new RectangleF(panel.Right - barW - 24f, y + 4f, barW, 10f);

    DrawText(frame, label, new Vector2(panel.X + 24f, y), textColor, 0.58f, TextAlignment.LEFT);
    DrawText(frame, FormatPower(value, "MW") + " / " + FormatPower(max, "MW"), new Vector2(bar.X - 16f, y), COLOR_DIM, 0.52f, TextAlignment.RIGHT);
    Fill(frame, bar, COLOR_BG);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)Math.Max(0.0, Math.Min(1.0, ratio)), bar.Height), COLOR_ACCENT);
}

private string FormatPower(double value, string unit)
{
    string sign = value < 0.0 ? "-" : "";
    value = Math.Abs(value);

    if (value >= 1000.0)
        return sign + (value / 1000.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " G" + unit;
    if (value >= 10.0)
        return sign + value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " " + unit;
    return sign + value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " " + unit;
}

// -------------------------------------------------------------------------
// Sprite helpers
// -------------------------------------------------------------------------

private void PrepareSurface(IMyTextSurface surface, float fontSize)
{
    surface.ContentType = ContentType.SCRIPT;
    surface.Script = "";
    surface.Font = "Monospace";
    surface.FontSize = fontSize;
    surface.BackgroundColor = COLOR_BG;
    surface.FontColor = COLOR_TEXT;
}

private RectangleF Viewport(IMyTextSurface surface)
{
    return new RectangleF((surface.TextureSize - surface.SurfaceSize) * 0.5f, surface.SurfaceSize);
}

private RectangleF Inset(RectangleF r, float amount)
{
    return new RectangleF(r.X + amount, r.Y + amount, r.Width - amount * 2f, r.Height - amount * 2f);
}

private void Fill(MySpriteDrawFrame frame, RectangleF rect, Color color)
{
    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", rect.Position + rect.Size * 0.5f, rect.Size, color));
}

private void DrawBorder(MySpriteDrawFrame frame, RectangleF r, Color color, float t, bool left, bool right, bool top, bool bottom)
{
    if (top)    Fill(frame, new RectangleF(r.X, r.Y, r.Width, t), color);
    if (bottom) Fill(frame, new RectangleF(r.X, r.Bottom - t, r.Width, t), color);
    if (left)   Fill(frame, new RectangleF(r.X, r.Y, t, r.Height), color);
    if (right)  Fill(frame, new RectangleF(r.Right - t, r.Y, t, r.Height), color);
}

private void DrawText(MySpriteDrawFrame frame, string text, Vector2 pos, Color color, float scale, TextAlignment align)
{
    frame.Add(new MySprite(SpriteType.TEXT, text, pos, null, color, "Monospace", align, scale));
}

private void TryDrawSprite(MySpriteDrawFrame frame, string spriteName, Vector2 center, Vector2 size, Color color)
{
    try
    {
        frame.Add(new MySprite(SpriteType.TEXTURE, spriteName, center, size, color));
    }
    catch
    {
        frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, COLOR_DIM));
    }
}

// -------------------------------------------------------------------------
// Echo
// -------------------------------------------------------------------------

private void EchoBoot(double progress)
{
    Echo("");
    Echo("AGM");
    Echo("AutoGrid Manager");
    Echo("by RevGamer");
    Echo("");
    Echo("REBOOT / BOOT SEQUENCE");
    Echo(MakeTextBar(progress, 24) + " " + ((int)(progress * 100.0)).ToString() + "%");
    Echo("Main PB: " + (IsMainPb() ? "ON " + PB_TAG : "OFF"));
}

private void EchoStatus()
{
    Echo("");
    Echo("AGM");
    Echo("AutoGrid Manager");
    Echo("by RevGamer");
    Echo("");
    Echo("Version: " + VERSION);
    Echo("State  : RUNNING");
    Echo("Main PB: " + (IsMainPb() ? "ON " + PB_TAG : "OFF"));
    Echo("LCDs   : " + lcdBlocks.Count);
    Echo("Cargo  : " + inventories.Count);
    Echo("Items  : " + itemTotals.Count);
    Echo("Power  : " + batteries.Count + " batteries, " + powerProducers.Count + " producers");
    Echo("Sort   : OFF");
    Echo("");
    Echo("Args: reload | rescan | reboot | reset");
}

private string MakeTextBar(double progress, int width)
{
    int filled = (int)Math.Round(progress * width);
    if (filled < 0) filled = 0;
    if (filled > width) filled = width;

    sb.Clear();
    sb.Append("[");
    for (int i = 0; i < width; i++)
        sb.Append(i < filled ? "|" : ".");
    sb.Append("]");
    return sb.ToString();
}
