
private const string VERSION       = "1.0-status";
private const string PB_TAG        = "{AGM-Status}";
private const string CORE_TAG      = "{AGM-Core}";
private const string LOGISTICS_TAG = "{AGM-Logistics}";
private const string LCD_TAG       = "[AGM-S]";
private const string BRAND_TITLE   = "AGM";
private const string BRAND_NAME    = "AutoGrid Manager";
private const string BRAND_AUTHOR  = "by RevGamer";
private const float BOOT_SECONDS   = 4.0f;
private const int   INDEX_TICKS    = 30;
private const int   RESCAN_TICKS   = 300;
private const int   CRAFT_TICKS    = 300;
private static readonly StringComparison SC = StringComparison.OrdinalIgnoreCase;
private const int   SCREENS_PER_RUN = 1;
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
private enum ScreenMode { Normal, Wide, Vertical }
private enum ScreenKind { Inventory, Power, Autocrafting, Sorter, FuelLifeSupport, Production }
private struct ScreenCommand
{
    public ScreenKind Kind;
    public ScreenMode Mode;
    public string     Category;
    public int        Page;
    public int        RowsPerPage;
    public string     Join;
    public string     PowerProfile;
    public string     CraftCategory;
}
private class ItemTotal
{
    public string     Category;
    public string     Key;
    public string     DisplayName;
    public string     SpriteName;
    public double     Amount;
}
private class CraftQuota
{
    public string Name;
    public string Category;
    public string Key;
    public double Wanted;
    public double Current;
    public double Queued;
    public bool   HasBlueprint;
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
private class LifeSupportConfig
{
    public bool Found, IncludeUngrouped;
    public string HydrogenGroup, OxygenGroup, GeneratorsGroup;
}
private struct LinkInfo
{
    public bool Found;
    public string Group;
    public int Page;
}
private bool     booting       = true;
private double   bootElapsed   = 0.0;
private bool     bootCleared   = false;
private int      tickCounter   = 0;
private int      indexCounter  = 0;
private int      craftCounter  = 0;
private int      nextScreenIndex = 0;
private DateTime lastRun       = DateTime.Now;
private readonly List<IMyTerminalBlock> allBlocks           = new List<IMyTerminalBlock>();
private readonly List<IMyTerminalBlock> dockedSourceBlocks  = new List<IMyTerminalBlock>();
private readonly List<IMyTerminalBlock> lcdBlocks       = new List<IMyTerminalBlock>();
private readonly List<IMyTerminalBlock> cargoBlocks     = new List<IMyTerminalBlock>();
private readonly List<IMyInventory>     inventories     = new List<IMyInventory>();
private readonly List<IMyAssembler>     assemblers      = new List<IMyAssembler>();
private readonly List<IMyGasTank>       gasTanks        = new List<IMyGasTank>();
private readonly List<IMyGasGenerator>  gasGenerators   = new List<IMyGasGenerator>();
private readonly List<IMyAirVent>       airVents        = new List<IMyAirVent>();
private readonly List<IMyBatteryBlock>  batteries       = new List<IMyBatteryBlock>();
private readonly List<IMyPowerProducer> powerProducers  = new List<IMyPowerProducer>();
private readonly List<IMyTerminalBlock> powerGroupBlocks = new List<IMyTerminalBlock>();
private readonly List<MyInventoryItem>  itemBuffer      = new List<MyInventoryItem>();
private readonly List<MyProductionItem> queueBuffer     = new List<MyProductionItem>();
private readonly List<ItemTotal>        itemTotals      = new List<ItemTotal>();
private readonly List<CraftQuota>       craftQuotas     = new List<CraftQuota>();
private readonly Dictionary<string, ItemTotal> totalsByKey = new Dictionary<string, ItemTotal>();
private readonly Dictionary<string, MyDefinitionId> blueprintByKey = new Dictionary<string, MyDefinitionId>();
private readonly HashSet<long>          selectedPowerIds = new HashSet<long>();
private readonly StringBuilder          sb              = new StringBuilder();
private bool includeDockedGrids     = false;
private bool coreFound              = false;
private bool statusEnabledByCore    = true;
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    InitBlueprints();
    ReadProgramConfig();
    RescanBlocks();
    ReadCoreState();
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
        ReadProgramConfig();
        RescanBlocks();
        ReadCoreState();
        IndexInventory();
        StartBoot();
    }
    else if (arg == "reboot")
    {
        StartBoot();
    }
    else if (arg == "sort")
    {
        ReadProgramConfig();
        RescanBlocks();
        ReadCoreState();
    }
    else if (arg == "reset")
    {
        itemTotals.Clear();
        totalsByKey.Clear();
        ReadProgramConfig();
        RescanBlocks();
        ReadCoreState();
        IndexInventory();
        StartBoot();
    }
    double dt = (DateTime.Now - lastRun).TotalSeconds;
    if (dt < 0.0 || dt > 2.0) dt = 0.166;
    lastRun = DateTime.Now;
    if ((updateSource & (UpdateType.Update10 | UpdateType.Update100)) != 0)
    {
        int stepTicks = (updateSource & UpdateType.Update100) != 0 ? 100 : 10;
        tickCounter += stepTicks;
        indexCounter += stepTicks;
        craftCounter += stepTicks;
        if (tickCounter >= RESCAN_TICKS)
        {
            tickCounter = 0;
            RescanBlocks();
            ReadCoreState();
        }
        if (indexCounter >= INDEX_TICKS)
        {
            indexCounter = 0;
            IndexInventory();
            ReadProgramConfig();
            ReadCoreState();
        }
        if (craftCounter >= CRAFT_TICKS)
        {
            craftCounter = 0;
        }
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
    if (statusEnabledByCore)
        DrawNextScreens();
    DrawPbStatus();
    EchoStatus();
}
private void StartBoot()
{
    booting = true;
    bootCleared = false;
    bootElapsed = 0.0;
    nextScreenIndex = 0;
    lastRun = DateTime.Now;
}
private void RescanBlocks()
{
    allBlocks.Clear();
    lcdBlocks.Clear();
    cargoBlocks.Clear();
    inventories.Clear();
    assemblers.Clear();
    gasTanks.Clear();
    gasGenerators.Clear();
    airVents.Clear();
    batteries.Clear();
    powerProducers.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(allBlocks, b => b.IsSameConstructAs(Me));
    dockedSourceBlocks.Clear();
    if (includeDockedGrids)
    {
        GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(dockedSourceBlocks,
            b => !b.IsSameConstructAs(Me) && b.HasInventory);
    }
    foreach (var block in allBlocks)
    {
        if (block.HasInventory)
        {
            for (int i = 0; i < block.InventoryCount; i++)
                inventories.Add(block.GetInventory(i));
        }
        if (block.CustomName.Contains(LCD_TAG) && block is IMyTextSurfaceProvider)
        {
            var provider = block as IMyTextSurfaceProvider;
            if (GetPrimarySurface(provider) != null && !IsCoreDashboardScreen(block) && !IsLogisticsDashboardScreen(block) && !IsProductionModuleScreen(block))
                lcdBlocks.Add(block);
        }
        if (block is IMyCargoContainer)
            cargoBlocks.Add(block);
        var assembler = block as IMyAssembler;
        if (assembler != null && assembler.IsFunctional)
            assemblers.Add(assembler);
        var tank = block as IMyGasTank;
        if (tank != null && tank.IsFunctional)
            gasTanks.Add(tank);
        var generator = block as IMyGasGenerator;
        if (generator != null && generator.IsFunctional)
            gasGenerators.Add(generator);
        var vent = block as IMyAirVent;
        if (vent != null && vent.IsFunctional && IsInteriorVentMonitor(block))
            airVents.Add(vent);
        var battery = block as IMyBatteryBlock;
        if (battery != null)
            batteries.Add(battery);
        var producer = block as IMyPowerProducer;
        if (producer != null && battery == null)
            powerProducers.Add(producer);
    }
}

private bool IsCoreDashboardScreen(IMyTerminalBlock block)
{
    if (block == null || block.CustomData == null)
        return false;
    return block.CustomData.IndexOf("CoreDashboard", SC) >= 0
        || block.CustomData.IndexOf("AGM-Core", SC) >= 0;
}
private bool IsLogisticsDashboardScreen(IMyTerminalBlock block)
{
    if (block == null || block.CustomData == null)
        return false;
    return HasScreenCommand(block.CustomData, "LogisticsDashboard")
        || HasScreenCommand(block.CustomData, "AGM-Logistics");
}
private bool IsProductionModuleScreen(IMyTerminalBlock block)
{
    if (block == null || block.CustomData == null)
        return false;
    return HasScreenCommand(block.CustomData, "AGM-Production");
}
private bool HasScreenCommand(string raw, string command)
{
    if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(command))
        return false;
    string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (string.Equals(line, command, SC))
            return true;
        string key, value;
        if (TrySplitKeyValue(line, '=', out key, out value) && string.Equals(key, command, SC))
            return true;
    }
    return false;
}
private bool IsMainPb()
{
    return Me.CustomName.Contains(PB_TAG);
}
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
        int c = string.Compare(a.Category, b.Category, SC);
        if (c != 0) return c;
        return string.Compare(a.DisplayName, b.DisplayName, SC);
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
private void ReadProgramConfig()
{
    includeDockedGrids = false;
    string raw = Me.CustomData;
    if (string.IsNullOrEmpty(raw))
        return;
    string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.Length == 0)
            continue;
        string key, value;
        if (!TrySplitKeyValue(line, '=', out key, out value))
            continue;
        if (string.Equals(key, "include_docked_grids", SC)
            || string.Equals(key, "docked_grids", SC))
            includeDockedGrids = ParseBool(value, false);

    }
}

private void ReadCoreState()
{
    coreFound = false;
    statusEnabledByCore = true;
    IMyProgrammableBlock core = null;
    for (int i = 0; i < allBlocks.Count; i++)
    {
        core = allBlocks[i] as IMyProgrammableBlock;
        if (core != null && core != Me && core.CustomName.IndexOf(CORE_TAG, SC) >= 0)
            break;
        core = null;
    }
    if (core == null)
        return;
    coreFound = true;
    string raw = core.CustomData;
    string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string key, value;
        if (!TrySplitKeyValue(StripComment(lines[i]).Trim(), '=', out key, out value))
            continue;
        if (key.Equals("core_enabled", SC) || key.Equals("enabled", SC))
            statusEnabledByCore = ParseBool(value, statusEnabledByCore);
        else if (key.Equals("status_enabled", SC) || key.Equals("status", SC))
            statusEnabledByCore = ParseBool(value, statusEnabledByCore);
    }
}

private string PluralCategoryTag(string category)
{
    if (string.Equals(category, "Ore", SC)) return "Ores";
    if (string.Equals(category, "Ingot", SC)) return "Ingots";
    if (string.Equals(category, "Component", SC)) return "Components";
    if (string.Equals(category, "Tool", SC)) return "Tools";
    if (string.Equals(category, "Bottle", SC)) return "Bottles";
    return category;
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
private void InitBlueprints()
{
    blueprintByKey.Clear();
    AddBlueprint("Component", "SteelPlate", "SteelPlate");
    AddBlueprint("Component", "InteriorPlate", "InteriorPlate");
    AddBlueprint("Component", "Construction", "ConstructionComponent");
    AddBlueprint("Component", "Computer", "ComputerComponent");
    AddBlueprint("Component", "Display", "Display");
    AddBlueprint("Component", "Motor", "MotorComponent");
    AddBlueprint("Component", "SmallTube", "SmallTube");
    AddBlueprint("Component", "LargeTube", "LargeTube");
    AddBlueprint("Component", "MetalGrid", "MetalGrid");
    AddBlueprint("Component", "Girder", "GirderComponent");
    AddBlueprint("Component", "BulletproofGlass", "BulletproofGlass");
    AddBlueprint("Component", "PowerCell", "PowerCell");
    AddBlueprint("Component", "SolarCell", "SolarCell");
    AddBlueprint("Component", "Detector", "DetectorComponent");
    AddBlueprint("Component", "RadioCommunication", "RadioCommunicationComponent");
    AddBlueprint("Component", "Medical", "MedicalComponent");
    AddBlueprint("Component", "Reactor", "ReactorComponent");
    AddBlueprint("Component", "Thrust", "ThrustComponent");
    AddBlueprint("Component", "GravityGenerator", "GravityGeneratorComponent");
    AddBlueprint("Component", "Superconductor", "Superconductor");
    AddBlueprint("Component", "Explosives", "ExplosivesComponent");
    AddBlueprint("Component", "Canvas", "Canvas");
}
private void AddBlueprint(string category, string subtype, string blueprintSubtype)
{
    MyDefinitionId id;
    if (MyDefinitionId.TryParse("MyObjectBuilder_BlueprintDefinition/" + blueprintSubtype, out id))
        blueprintByKey[category + "/" + subtype] = id;
}
private void ProcessAutocrafting()
{
}
private void CollectCraftQuotas(string raw, string category, List<CraftQuota> output)
{
    output.Clear();
    if (string.IsNullOrEmpty(raw))
        return;
    string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.Length == 0)
            continue;
        string key, value;
        if (!TrySplitKeyValue(line, '=', out key, out value))
            continue;
        if (string.Equals(key, "AutoCrafting", SC)
            || string.Equals(key, "Autocrafting", SC)
            || string.Equals(key, "Crafting", SC))
            continue;
        double wanted;
        if (!TryParseAmount(value, out wanted) || wanted <= 0.0)
            continue;
        CraftQuota quota;
        if (TryResolveCraftQuota(key, category, wanted, out quota))
            output.Add(quota);
    }
}
private bool TryResolveCraftQuota(string itemName, string category, double wanted, out CraftQuota quota)
{
    quota = new CraftQuota();
    string clean = CleanToken(itemName);
    string resolvedKey = "";
    for (int i = 0; i < itemTotals.Count; i++)
    {
        ItemTotal total = itemTotals[i];
        if (!string.Equals(total.Category, category, SC))
            continue;
        string subtype = total.Key.Substring(total.Key.IndexOf('/') + 1);
        if (CleanToken(subtype) == clean || CleanToken(total.DisplayName) == clean)
        {
            resolvedKey = total.Key;
            quota.Current = total.Amount;
            quota.Name = total.DisplayName;
            break;
        }
    }
    if (resolvedKey.Length == 0)
    {
        foreach (var bp in blueprintByKey)
        {
            if (!bp.Key.StartsWith(category + "/", SC))
                continue;
            string subtype = bp.Key.Substring(bp.Key.IndexOf('/') + 1);
            if (CleanToken(subtype) == clean || CleanToken(MakeDisplayName(subtype)) == clean)
            {
                resolvedKey = bp.Key;
                quota.Current = 0.0;
                quota.Name = MakeDisplayName(subtype);
                break;
            }
        }
    }
    if (resolvedKey.Length == 0)
        return false;
    MyDefinitionId blueprint;
    quota.Category = category;
    quota.Key = resolvedKey;
    quota.Wanted = wanted;
    quota.Queued = GetQueuedAmount(resolvedKey);
    quota.HasBlueprint = blueprintByKey.TryGetValue(resolvedKey, out blueprint);
    if (quota.Name == null || quota.Name.Length == 0)
        quota.Name = MakeDisplayName(resolvedKey.Substring(resolvedKey.IndexOf('/') + 1));
    return true;
}
private double GetQueuedAmount(string key)
{
    MyDefinitionId blueprint;
    if (!blueprintByKey.TryGetValue(key, out blueprint))
        return 0.0;
    double queued = 0.0;
    foreach (var assembler in assemblers)
    {
        queueBuffer.Clear();
        assembler.GetQueue(queueBuffer);
        for (int i = 0; i < queueBuffer.Count; i++)
        {
            if (queueBuffer[i].BlueprintId == blueprint)
                queued += (double)queueBuffer[i].Amount;
        }
    }
    return queued;
}
private void QueueCraftQuota(CraftQuota quota)
{
}
private bool TryParseAmount(string text, out double amount)
{
    amount = 0.0;
    string t = (text == null ? "" : text.Trim()).ToLowerInvariant().Replace(",", "");
    double multiplier = 1.0;
    if (t.EndsWith("k"))
    {
        multiplier = 1000.0;
        t = t.Substring(0, t.Length - 1);
    }
    else if (t.EndsWith("m"))
    {
        multiplier = 1000000.0;
        t = t.Substring(0, t.Length - 1);
    }
    double parsed;
    if (!double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
        return false;
    amount = parsed * multiplier;
    return true;
}
private string CleanToken(string value)
{
    if (value == null)
        return "";
    value = value.ToLowerInvariant();
    sb.Clear();
    for (int i = 0; i < value.Length; i++)
    {
        char c = value[i];
        if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            sb.Append(c);
    }
    return sb.ToString();
}
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
    cmd.CraftCategory = "Component";
    if (string.IsNullOrEmpty(raw))
        return false;
    string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.Length == 0)
            continue;
        if (line.StartsWith("AutoCrafting-", SC)
            || line.StartsWith("Autocrafting-", SC)
            || line.StartsWith("Crafting-", SC))
        {
            int dash = line.IndexOf('-');
            cmd.Kind = ScreenKind.Autocrafting;
            string v = dash >= 0 && dash + 1 < line.Length ? line.Substring(dash + 1).Trim() : "Component";
            ParseCraftCommandValue(v, ref cmd);
            return true;
        }
        string key, value;
        if (!TrySplitKeyValue(line, '=', out key, out value))
        {
            if (string.Equals(line, "PowerDashboard", SC)
                || string.Equals(line, "Power", SC)
                || string.Equals(line, "IndustrialPower", SC))
            {
                cmd.Kind = ScreenKind.Power;
                cmd.PowerProfile = "";
                return true;
            }
            if (string.Equals(line, "AutoCrafting", SC)
                || string.Equals(line, "Autocrafting", SC)
                || string.Equals(line, "Crafting", SC))
            {
                cmd.Kind = ScreenKind.Autocrafting;
                cmd.CraftCategory = "Component";
                return true;
            }
            if (string.Equals(line, "SorterDashboard", SC)
                || string.Equals(line, "Sorter", SC)
                || string.Equals(line, "AutoSorter", SC))
            {
                cmd.Kind = ScreenKind.Sorter;
                return true;
            }
            if (string.Equals(line, "FuelLifeSupport", SC)
                || string.Equals(line, "LifeSupport", SC))
            {
                cmd.Kind = ScreenKind.FuelLifeSupport;
                return true;
            }
            if (string.Equals(line, "ProductionDashboard", SC))
            {
                cmd.Kind = ScreenKind.Production;
                return true;
            }
            continue;
        }
        if (string.Equals(key, "PowerDashboard", SC)
            || string.Equals(key, "Power", SC)
            || string.Equals(key, "IndustrialPower", SC))
        {
            cmd.Kind = ScreenKind.Power;
            cmd.PowerProfile = value.Trim();
            return true;
        }
        if (string.Equals(key, "IndustrialInventory", SC))
        {
            cmd.Mode = ScreenMode.Normal;
            ParseCommandValue(value, ref cmd);
            return true;
        }
        if (string.Equals(key, "AutoCrafting", SC)
            || string.Equals(key, "Autocrafting", SC)
            || string.Equals(key, "Crafting", SC))
        {
            cmd.Kind = ScreenKind.Autocrafting;
            ParseCraftCommandValue(value, ref cmd);
            return true;
        }
        if (string.Equals(key, "SorterDashboard", SC)
            || string.Equals(key, "Sorter", SC)
            || string.Equals(key, "AutoSorter", SC))
        {
            cmd.Kind = ScreenKind.Sorter;
            return true;
        }
        if (string.Equals(key, "FuelLifeSupport", SC)
            || string.Equals(key, "LifeSupport", SC))
        {
            cmd.Kind = ScreenKind.FuelLifeSupport;
            cmd.PowerProfile = value.Trim();
            return true;
        }
        if (string.Equals(key, "ProductionDashboard", SC))
        {
            cmd.Kind = ScreenKind.Production;
            return true;
        }
        if (string.Equals(key, "IndustrialInventoryWide", SC))
        {
            cmd.Mode = ScreenMode.Wide;
            ParseCommandValue(value, ref cmd);
            return true;
        }
        if (string.Equals(key, "IndustrialInventoryVertical", SC))
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
private void ParseCraftCommandValue(string value, ref ScreenCommand cmd)
{
    string[] parts = value.Split(':');
    cmd.CraftCategory = parts.Length > 0 && parts[0].Trim().Length > 0 ? NormaliseCategory(parts[0].Trim()) : "Component";
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
}
private string NormaliseCategory(string value)
{
    if (string.Equals(value, "Ores", SC)) return "Ore";
    if (string.Equals(value, "Ingots", SC)) return "Ingot";
    if (string.Equals(value, "Components", SC)) return "Component";
    if (string.Equals(value, "AmmoMagazine", SC)) return "Ammo";
    if (string.Equals(value, "Ammunition", SC)) return "Ammo";
    if (string.Equals(value, "Bottles", SC)) return "Bottle";
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
private void DrawNextScreens()
{
    if (lcdBlocks.Count == 0)
        return;
    int count = Math.Min(SCREENS_PER_RUN, lcdBlocks.Count);
    for (int i = 0; i < count; i++)
    {
        if (nextScreenIndex < 0 || nextScreenIndex >= lcdBlocks.Count)
            nextScreenIndex = 0;
        DrawScreen(lcdBlocks[nextScreenIndex]);
        nextScreenIndex++;
        if (nextScreenIndex >= lcdBlocks.Count)
            nextScreenIndex = 0;
    }
}
private void DrawScreen(IMyTerminalBlock block)
{
    var provider = block as IMyTextSurfaceProvider;
    if (provider == null)
        return;
    IMyTextSurface surface = GetPrimarySurface(provider);
    if (surface == null)
        return;
    ScreenCommand cmd;
    string raw;
    if (!TryGetScreenCommand(block, out raw, out cmd))
    {
        DrawNoCommand(surface, block.CustomName);
        return;
    }
    if (cmd.Kind == ScreenKind.Power)
        DrawPowerDashboard(surface, cmd.PowerProfile);
    else if (cmd.Kind == ScreenKind.Autocrafting)
        DrawAutocraftingScreen(surface, raw, cmd);
    else if (cmd.Kind == ScreenKind.Sorter)
        DrawSorterDashboard(surface);
    else if (cmd.Kind == ScreenKind.FuelLifeSupport)
        DrawFuelLifeSupportDashboard(surface, cmd.PowerProfile);
    else if (cmd.Kind == ScreenKind.Production)
        DrawProductionDashboard(surface);
    else
        DrawInventoryScreen(surface, cmd);
}
private IMyTextSurface GetPrimarySurface(IMyTextSurfaceProvider provider)
{
    if (provider == null || provider.SurfaceCount <= 0)
        return null;
    return provider.GetSurface(0);
}
private bool TryGetScreenCommand(IMyTerminalBlock block, out string raw, out ScreenCommand cmd)
{
    raw = block.CustomData;
    LinkInfo link = ParseLink(block.CustomName);
    if (link.Found && (raw == null || raw.Trim().Length == 0 || IsCommandOnlyAutocraftingData(raw)))
    {
        string linkedRaw = FindLinkedCustomData(link.Group);
        if (linkedRaw.Trim().Length > 0)
            raw = linkedRaw;
    }
    if (!TryParseCommand(raw, out cmd))
        return false;
    if (link.Found && link.Page > 0)
        cmd.Page = link.Page;
    return true;
}
private string FindLinkedCustomData(string group)
{
    string fallback = "";
    for (int i = 0; i < lcdBlocks.Count; i++)
    {
        IMyTerminalBlock block = lcdBlocks[i];
        if (block.CustomData == null || block.CustomData.Trim().Length == 0)
            continue;
        if (IsCommandOnlyAutocraftingData(block.CustomData))
            continue;
        LinkInfo other = ParseLink(block.CustomName);
        if (!other.Found)
            continue;
        if (string.Equals(other.Group, group, SC))
        {
            if (other.Page == 1)
                return block.CustomData;
            if (fallback.Length == 0)
                fallback = block.CustomData;
        }
    }
    return fallback;
}
private bool IsCommandOnlyAutocraftingData(string raw)
{
    if (string.IsNullOrEmpty(raw))
        return false;
    bool foundCommand = false;
    string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.Length == 0)
            continue;
        string key, value;
        if (TrySplitKeyValue(line, '=', out key, out value))
        {
            if (string.Equals(key, "AutoCrafting", SC)
                || string.Equals(key, "Autocrafting", SC)
                || string.Equals(key, "Crafting", SC))
            {
                foundCommand = true;
                continue;
            }
            return false;
        }
        if (line.StartsWith("AutoCrafting-", SC)
            || line.StartsWith("Autocrafting-", SC)
            || line.StartsWith("Crafting-", SC)
            || string.Equals(line, "AutoCrafting", SC)
            || string.Equals(line, "Autocrafting", SC)
            || string.Equals(line, "Crafting", SC))
        {
            foundCommand = true;
            continue;
        }
        return false;
    }
    return foundCommand;
}
private LinkInfo ParseLink(string name)
{
    LinkInfo info = new LinkInfo();
    info.Found = false;
    info.Group = "";
    info.Page = 0;
    if (string.IsNullOrEmpty(name))
        return info;
    int idx = name.IndexOf("!LINK", SC);
    if (idx < 0)
        return info;
    int p = idx + 5;
    if (p < name.Length && name[p] == ':')
        p++;
    while (p < name.Length && (name[p] == ' ' || name[p] == '-' || name[p] == '_'))
        p++;
    sb.Clear();
    while (p < name.Length && char.IsLetter(name[p]))
    {
        sb.Append(char.ToUpperInvariant(name[p]));
        p++;
    }
    if (p < name.Length && name[p] == ':')
        p++;
    while (p < name.Length && (name[p] == ' ' || name[p] == '-' || name[p] == '_'))
        p++;
    string digits = "";
    while (p < name.Length && char.IsDigit(name[p]))
    {
        digits += name[p];
        p++;
    }
    int page;
    int.TryParse(digits, out page);
    if (sb.Length == 0)
        sb.Append("A");
    info.Found = true;
    info.Group = sb.ToString();
    info.Page = page;
    return info;
}
private void DrawBootAll(double progress)
{
    if (IsMainPb())
        DrawBootSurface(Me.GetSurface(0), progress);
    if (!bootCleared)
    {
        ClearBootLcds();
        bootCleared = true;
    }
    if (lcdBlocks.Count == 0)
        return;
    if (nextScreenIndex < 0 || nextScreenIndex >= lcdBlocks.Count)
        nextScreenIndex = 0;
    var provider = lcdBlocks[nextScreenIndex] as IMyTextSurfaceProvider;
    if (provider != null)
    {
        IMyTextSurface surface = GetPrimarySurface(provider);
        if (surface != null)
            DrawBootSurface(surface, progress);
    }
    nextScreenIndex++;
    if (nextScreenIndex >= lcdBlocks.Count)
        nextScreenIndex = 0;
}
private void ClearBootLcds()
{
    foreach (var block in lcdBlocks)
    {
        var provider = block as IMyTextSurfaceProvider;
        if (provider == null)
            continue;
        IMyTextSurface surface = GetPrimarySurface(provider);
        if (surface == null)
            continue;
        PrepareSurface(surface, 0.75f);
        RectangleF vp = Viewport(surface);
        using (var frame = surface.DrawFrame())
        {
            Fill(frame, vp, COLOR_BG);
            DrawBorder(frame, Inset(vp, 6f), COLOR_ACCENT, 3f, true, true, true, true);
        }
    }
}
private void DrawBootSurface(IMyTextSurface surface, double progress)
{
    if (surface == null)
        return;
    PrepareSurface(surface, 0.75f);
    RectangleF vp = Viewport(surface);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        DrawBorder(frame, Inset(vp, 6f), COLOR_ACCENT, 3f, true, true, true, true);
        Vector2 center = vp.Position + vp.Size * 0.5f;
        bool compact = vp.Width < 300f || vp.Height < 300f;
        float titleScale = compact ? 1.12f : 3.1f;
        float nameScale = compact ? 0.34f : 1.0f;
        float authorScale = compact ? 0.34f : 0.78f;
        float yTop = compact ? center.Y - 68f : center.Y - 82f;
        DrawText(frame, BRAND_TITLE, new Vector2(center.X, yTop), COLOR_ACCENT_2, titleScale, TextAlignment.CENTER);
        DrawText(frame, BRAND_NAME, new Vector2(center.X, yTop + (compact ? 38f : 60f)), COLOR_TEXT, nameScale, TextAlignment.CENTER);
        DrawText(frame, BRAND_AUTHOR, new Vector2(center.X, yTop + (compact ? 58f : 100f)), COLOR_DIM, authorScale, TextAlignment.CENTER);
        float barW = Math.Min(vp.Width * 0.52f, compact ? 122f : 360f);
        RectangleF bar = new RectangleF(center.X - barW * 0.5f, center.Y + (compact ? 26f : 66f), barW, compact ? 10f : 20f);
        Fill(frame, bar, COLOR_PANEL_2);
        Fill(frame, new RectangleF(bar.X, bar.Y, (float)(bar.Width * progress), bar.Height), COLOR_ACCENT);
        DrawBorder(frame, bar, COLOR_ACCENT_2, 2f, true, true, true, true);
        DrawText(frame, "REBOOT " + ((int)(progress * 100.0)).ToString() + "%", center + new Vector2(0, compact ? 48f : 112f), COLOR_TEXT, compact ? 0.40f : 0.8f, TextAlignment.CENTER);
    }
}
private void DrawPbStatus()
{
    if (!IsMainPb())
        return;
    IMyTextSurface surface = Me.GetSurface(0);
    PrepareSurface(surface, 1.0f);
    RectangleF vp = Viewport(surface);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF inner = Inset(vp, 10f);
        Fill(frame, inner, COLOR_PANEL);
        DrawBorder(frame, inner, COLOR_ACCENT, 3f, true, true, true, true);
        Vector2 top = inner.Position + new Vector2(inner.Width * 0.5f, 22f);
        DrawText(frame, "AGM - Status", top, COLOR_ACCENT_2, 0.92f, TextAlignment.CENTER);
        DrawText(frame, "AutoGrid Manager", top + new Vector2(0, 30f), COLOR_TEXT, 0.44f, TextAlignment.CENTER);
        DrawText(frame, coreFound ? "AGM CORE CONNECTED" : "AGM CORE STANDALONE", top + new Vector2(0, 58f), coreFound ? COLOR_OK : COLOR_WARN, 0.44f, TextAlignment.CENTER);
        DrawText(frame, statusEnabledByCore ? "ONLINE" : "DISABLED", top + new Vector2(0, 86f), statusEnabledByCore ? COLOR_OK : COLOR_LOW, 0.58f, TextAlignment.CENTER);
        float statX = inner.X + 24f;
        float statY = top.Y + 124f;
        DrawText(frame, "LIVE " + DateTime.Now.ToString("HH:mm:ss"), new Vector2(statX, statY), COLOR_OK, 0.46f, TextAlignment.LEFT);
        DrawText(frame, "LCDs  " + lcdBlocks.Count, new Vector2(statX, statY + 28f), COLOR_TEXT, 0.44f, TextAlignment.LEFT);
        DrawText(frame, "Cargo " + inventories.Count, new Vector2(statX, statY + 54f), COLOR_TEXT, 0.44f, TextAlignment.LEFT);
        DrawText(frame, "Items " + itemTotals.Count, new Vector2(statX, statY + 80f), COLOR_TEXT, 0.44f, TextAlignment.LEFT);
        DrawText(frame, "v" + VERSION, new Vector2(inner.X + inner.Width * 0.5f, inner.Bottom - 18f), COLOR_DIM, 0.34f, TextAlignment.CENTER);
    }
}
private void DrawNoCommand(IMyTextSurface surface, string name)
{
    if (surface == null)
        return;
    PrepareSurface(surface, 0.9f);
    RectangleF vp = Viewport(surface);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        DrawBorder(frame, vp, COLOR_ACCENT, 3f, true, true, true, true);
        Vector2 center = vp.Position + vp.Size * 0.5f;
        DrawText(frame, "AGM SCREEN READY", center + new Vector2(0, -32), COLOR_ACCENT_2, 1.1f, TextAlignment.CENTER);
        DrawText(frame, "Add AutoCrafting=Component", center + new Vector2(0, 12), COLOR_TEXT, 0.7f, TextAlignment.CENTER);
        DrawText(frame, "Power, Sorter, or FuelLifeSupport", center + new Vector2(0, 42), COLOR_DIM, 0.65f, TextAlignment.CENTER);
    }
}
private void DrawInventoryScreen(IMyTextSurface surface, ScreenCommand cmd)
{
    if (surface == null)
        return;
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
        DrawText(frame, "PAGE " + page + "/" + pageCount + "  ITEMS " + rows.Count, panel.Position + new Vector2(panel.Width - 24, 26), COLOR_DIM, 0.58f, TextAlignment.RIGHT);
        float top = panel.Y + (title.Length > 0 ? 70f : 22f);
        float rowH = Math.Max(34f, (panel.Bottom - top - 26f) / Math.Max(1, rowsPerPage));
        if (rows.Count == 0)
        {
            Fill(frame, new RectangleF(panel.X + 14f, top - 4f, panel.Width - 28f, panel.Bottom - top - 30f), COLOR_PANEL);
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
        if (string.Equals(itemTotals[i].Category, category, SC))
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
private void DrawProductionDashboard(IMyTextSurface surface)
{
    if (surface == null)
        return;
    Dictionary<string, string> state = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    bool linked = TryGetProductionState(state);
    PrepareSurface(surface, 0.78f);
    RectangleF vp = Viewport(surface);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f, true, true, true, true);
        DrawText(frame, "PRODUCTION DASHBOARD", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 0.86f, TextAlignment.LEFT);
        DrawText(frame, linked ? "PRODUCTION LINK" : "NO PRODUCTION", panel.Position + new Vector2(panel.Width - 24f, 26f), linked ? COLOR_OK : COLOR_WARN, 0.58f, TextAlignment.RIGHT);
        float y = panel.Y + 72f;
        string status = GetStateValue(state, "state", linked ? "UNKNOWN" : "missing");
        string mode = GetStateValue(state, "monitor_only", "true") == "true" ? "MONITOR" : "ACTIVE";
        DrawProductionRow(frame, panel, y, "State", status + " | " + mode, linked ? COLOR_OK : COLOR_WARN); y += 34f;
        DrawProductionRow(frame, panel, y, "Assemblers", GetStateValue(state, "assemblers_producing", "0") + " producing / " + GetStateValue(state, "assemblers_working", "0") + " online / " + GetStateValue(state, "assemblers", "0") + " total", COLOR_OK); y += 34f;
        DrawProductionRow(frame, panel, y, "Queued", GetStateValue(state, "assemblers_queued", "0") + " machines / " + GetStateValue(state, "queue_amount", "0") + " items", COLOR_ACCENT_2); y += 34f;
        DrawProductionRow(frame, panel, y, "Last action", GetStateValue(state, "last_action", "-"), COLOR_TEXT); y += 42f;
        DrawProductionRow(frame, panel, y, "Refineries", GetStateValue(state, "refineries_producing", "0") + " producing / " + GetStateValue(state, "refineries_working", "0") + " online / " + GetStateValue(state, "refineries", "0") + " total", COLOR_OK); y += 34f;
        DrawProductionRow(frame, panel, y, "Ref input", GetStateValue(state, "refinery_input_pct", "0") + "%", COLOR_OK); y += 34f;
        DrawProductionRow(frame, panel, y, "Ref output", GetStateValue(state, "refinery_output_pct", "0") + "%", COLOR_OK); y += 42f;
        DrawProductionRow(frame, panel, y, "Autocraft", GetStateValue(state, "queued_last_run", "0") + " queued / " + GetStateValue(state, "assembler_moves", "0") + " queue moves", COLOR_TEXT); y += 34f;
        DrawProductionRow(frame, panel, y, "Refinery sort", GetStateValue(state, "refinery_moves", "0") + " moves", COLOR_TEXT);
        string warning = GetStateValue(state, "warning", "");
        DrawText(frame, warning.Length > 0 ? warning : "Production priorities and quotas read from AGM Production PB", new Vector2(panel.X + 24f, panel.Bottom - 24f), warning.Length > 0 ? COLOR_LOW : COLOR_DIM, 0.45f, TextAlignment.LEFT);
    }
}

private void DrawProductionRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string value, Color color)
{
    RectangleF row = new RectangleF(panel.X + 16f, y - 4f, panel.Width - 32f, 28f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawText(frame, label, new Vector2(row.X + 10f, y), COLOR_TEXT, 0.50f, TextAlignment.LEFT);
    DrawText(frame, value, new Vector2(row.Right - 10f, y), color, 0.48f, TextAlignment.RIGHT);
}

private bool TryGetProductionState(Dictionary<string, string> state)
{
    IMyProgrammableBlock pb = null;
    for (int i = 0; i < allBlocks.Count; i++)
    {
        pb = allBlocks[i] as IMyProgrammableBlock;
        if (pb != null && pb.CustomName.IndexOf("{AGM-Production}", SC) >= 0)
            break;
        pb = null;
    }
    if (pb == null)
        return false;
    string[] lines = (pb.CustomData ?? "").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    bool inState = false;
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            inState = line.Equals("[ProductionState]", SC);
            continue;
        }
        if (!inState) continue;
        string key, value;
        if (TrySplitKeyValue(line, '=', out key, out value))
            state[key] = value;
    }
    return state.Count > 0;
}

private string GetStateValue(Dictionary<string, string> state, string key, string fallback)
{
    string value;
    return state.TryGetValue(key, out value) ? value : fallback;
}

private void DrawSorterDashboard(IMyTextSurface surface)
{
    if (surface == null)
        return;
    string state, moves, item, from, to, warning, cargoCount, ore, ingot, component, ammo, tool, bottle;
    bool linked = TryGetLogisticsState(out state, out moves, out item, out from, out to, out warning, out cargoCount,
        out ore, out ingot, out component, out ammo, out tool, out bottle);
    if (!linked)
    {
        state = "missing";
        moves = "0";
        item = from = to = warning = "";
        cargoCount = ore = ingot = component = ammo = tool = bottle = "?";
    }
    PrepareSurface(surface, 0.78f);
    RectangleF vp = Viewport(surface);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f, true, true, true, true);
        DrawText(frame, "SORTER DASHBOARD", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 1.0f, TextAlignment.LEFT);
        DrawText(frame, linked ? "LOGISTICS LINK" : "NO LOGISTICS", panel.Position + new Vector2(panel.Width - 24f, 26f), linked ? COLOR_OK : COLOR_WARN, 0.62f, TextAlignment.RIGHT);
        float y = panel.Y + 72f;
        DrawSorterSimpleRow(frame, panel, y, "State", linked ? state : "missing", linked ? COLOR_OK : COLOR_WARN); y += 36f;
        DrawSorterSimpleRow(frame, panel, y, "Cargo", cargoCount + " containers", COLOR_ACCENT_2); y += 36f;
        DrawSorterSimpleRow(frame, panel, y, "Types", "O " + ore + "  I " + ingot + "  C " + component, COLOR_TEXT); y += 36f;
        DrawSorterSimpleRow(frame, panel, y, "More", "A " + ammo + "  T " + tool + "  B " + bottle, COLOR_TEXT); y += 44f;

        DrawText(frame, "LAST MOVEMENT", new Vector2(panel.X + 24f, y), COLOR_ACCENT, 0.68f, TextAlignment.LEFT);
        y += 32f;
        DrawSorterSimpleRow(frame, panel, y, "Moved", moves, COLOR_ACCENT_2); y += 36f;
        DrawSorterSimpleRow(frame, panel, y, "Item", item.Length > 0 ? item : "-", COLOR_TEXT); y += 36f;
        string route = from.Length > 0 ? from + " > " + to : "-";
        DrawSorterSimpleRow(frame, panel, y, "Route", route, COLOR_TEXT);

        DrawText(frame, linked ? ("Warn " + (warning.Length > 0 ? warning : "none")) : "waiting for AGM Logistics",
            new Vector2(panel.X + 24f, panel.Bottom - 24f), linked ? COLOR_DIM : COLOR_WARN, 0.48f, TextAlignment.LEFT);
    }
}

private bool TryGetLogisticsState(out string state, out string moves, out string item, out string from, out string to, out string warning, out string cargo,
    out string ore, out string ingot, out string component, out string ammo, out string tool, out string bottle)
{
    state = moves = item = from = to = warning = cargo = ore = ingot = component = ammo = tool = bottle = "";
    IMyProgrammableBlock pb = null;
    for (int i = 0; i < allBlocks.Count; i++)
    {
        pb = allBlocks[i] as IMyProgrammableBlock;
        if (pb != null && pb.CustomName.IndexOf(LOGISTICS_TAG, SC) >= 0)
            break;
        pb = null;
    }
    if (pb == null)
        return false;
    string[] lines = (pb.CustomData ?? "").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    bool inState = false;
    for (int i = 0; i < lines.Length; i++)
    {
        string line = StripComment(lines[i]).Trim();
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            inState = line.Equals("[LogisticsState]", SC);
            continue;
        }
        if (!inState) continue;
        string key, value;
        if (!TrySplitKeyValue(line, '=', out key, out value)) continue;
        if (key.Equals("state", SC)) state = value;
        else if (key.Equals("moves", SC)) moves = value;
        else if (key.Equals("last_item", SC)) item = value;
        else if (key.Equals("last_from", SC)) from = value;
        else if (key.Equals("last_to", SC)) to = value;
        else if (key.Equals("warning", SC)) warning = value;
        else if (key.Equals("cargo", SC)) cargo = value;
        else if (key.Equals("ore", SC)) ore = value;
        else if (key.Equals("ingot", SC)) ingot = value;
        else if (key.Equals("component", SC)) component = value;
        else if (key.Equals("ammo", SC)) ammo = value;
        else if (key.Equals("tool", SC)) tool = value;
        else if (key.Equals("bottle", SC)) bottle = value;
    }
    if (state.Length == 0) state = "connected";
    if (moves.Length == 0) moves = "0";
    if (cargo.Length == 0) cargo = "?";
    if (ore.Length == 0) ore = "0";
    if (ingot.Length == 0) ingot = "0";
    if (component.Length == 0) component = "0";
    if (ammo.Length == 0) ammo = "0";
    if (tool.Length == 0) tool = "0";
    if (bottle.Length == 0) bottle = "0";
    return true;
}

private void DrawSorterSimpleRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string value, Color valueColor)
{
    RectangleF row = new RectangleF(panel.X + 16f, y - 4f, panel.Width - 32f, 28f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawText(frame, label, new Vector2(row.X + 8f, row.Y + 4f), COLOR_DIM, 0.50f, TextAlignment.LEFT);
    DrawText(frame, TrimTo(value, 34), new Vector2(row.Right - 8f, row.Y + 4f), valueColor, 0.50f, TextAlignment.RIGHT);
}
private string FormatVolume(double volume)
{
    return FormatAmount(volume * 1000.0) + "L";
}
private string TrimTo(string text, int max)
{
    if (string.IsNullOrEmpty(text) || text.Length <= max)
        return text;
    if (max <= 2)
        return text.Substring(0, max);
    return text.Substring(0, max - 2) + "..";
}
private void DrawFuelLifeSupportDashboard(IMyTextSurface surface, string profile)
{
    if (surface == null)
        return;
    PrepareSurface(surface, 0.78f);
    RectangleF vp = Viewport(surface);
    LifeSupportConfig cfg = GetLifeSupportConfig(profile);
    selectedPowerIds.Clear();
    double h2Fill = 0.0, h2Capacity = 0.0;
    double o2Fill = 0.0, o2Capacity = 0.0;
    int h2Count = 0, o2Count = 0;
    AddConfiguredGasTanks(cfg.HydrogenGroup, true, ref h2Fill, ref h2Capacity, ref h2Count);
    AddConfiguredGasTanks(cfg.OxygenGroup, false, ref o2Fill, ref o2Capacity, ref o2Count);
    if (!cfg.Found || cfg.IncludeUngrouped)
    {
        AddUngroupedGasTanks(ref h2Fill, ref h2Capacity, ref h2Count, ref o2Fill, ref o2Capacity, ref o2Count);
    }
    int generatorOnline = 0;
    int generatorWorking = 0;
    int generatorTotal = 0;
    double generatorIce = 0.0;
    MyItemType iceType = MyItemType.MakeOre("Ice");
    AddConfiguredGasGenerators(cfg.GeneratorsGroup, iceType, ref generatorOnline, ref generatorWorking, ref generatorTotal, ref generatorIce);
    if (!cfg.Found || cfg.IncludeUngrouped)
        AddUngroupedGasGenerators(iceType, ref generatorOnline, ref generatorWorking, ref generatorTotal, ref generatorIce);
    double iceStock = GetItemAmount("Ore", "Ice");
    double oxygenBottles = GetItemAmount("Bottle", "OxygenBottle");
    double hydrogenBottles = GetItemAmount("Bottle", "HydrogenBottle");
    int ventOk, ventLeak;
    string leakingVents;
    bool pressurized = UpdateVentStatus(out ventOk, out ventLeak, out leakingVents);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f, true, true, true, true);
        DrawText(frame, "FUEL & LIFE SUPPORT", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 0.95f, TextAlignment.LEFT);
        DrawText(frame, pressurized ? "BASE OK" : "BASE X", panel.Position + new Vector2(panel.Width - 24f, 26f), pressurized ? COLOR_OK : COLOR_LOW, 0.62f, TextAlignment.RIGHT);
        float y = panel.Y + 72f;
        DrawTankMetricRow(frame, panel, y, "Hydrogen", h2Count, h2Fill, h2Capacity, "IconHydrogen");
        y += 58f;
        DrawTankMetricRow(frame, panel, y, "Oxygen", o2Count, o2Fill, o2Capacity, "IconOxygen");
        y += 64f;
        DrawText(frame, "O2/H2 GEN", new Vector2(panel.X + 24f, y), COLOR_ACCENT, 0.62f, TextAlignment.LEFT);
        DrawText(frame, generatorWorking + " working / " + generatorOnline + " online / " + generatorTotal + " total",
            new Vector2(panel.Right - 24f, y + 2f), COLOR_TEXT, 0.43f, TextAlignment.RIGHT);
        y += 34f;
        DrawFuelInfoRow(frame, panel, y, "Ice in generators", FormatAmount(generatorIce));
        y += 32f;
        DrawFuelInfoRow(frame, panel, y, "Ice stock", FormatAmount(iceStock));
        y += 32f;
        DrawFuelInfoRow(frame, panel, y, "Bottles", "O2 " + FormatAmount(oxygenBottles) + " | H2 " + FormatAmount(hydrogenBottles));
        y += 40f;
        DrawText(frame, "PRESSURIZATION", new Vector2(panel.X + 24f, y), COLOR_ACCENT, 0.68f, TextAlignment.LEFT);
        DrawText(frame, pressurized ? "OK Base Pressurized" : "X Base Not Pressurized",
            new Vector2(panel.Right - 24f, y), pressurized ? COLOR_OK : COLOR_LOW, 0.56f, TextAlignment.RIGHT);
        y += 32f;
        DrawFuelInfoRow(frame, panel, y, "Air vents", ventOk + " OK | " + ventLeak + " leaking");
        if (ventLeak > 0)
        {
            y += 32f;
            DrawFuelInfoRow(frame, panel, y, "Leak", TrimTo(leakingVents, 42));
        }
        DrawText(frame, airVents.Count + " vents monitored", new Vector2(panel.X + 24f, panel.Bottom - 24f), COLOR_DIM, 0.48f, TextAlignment.LEFT);
        DrawText(frame, "farms later", new Vector2(panel.Right - 24f, panel.Bottom - 24f), COLOR_DIM, 0.48f, TextAlignment.RIGHT);
    }
}
private bool IsHydrogenTank(IMyGasTank tank)
{
    if (tank == null)
        return false;
    string text = tank.BlockDefinition.TypeIdString + "/" + tank.BlockDefinition.SubtypeId + "/" + tank.DefinitionDisplayNameText + "/" + tank.CustomName;
    return text.IndexOf("Hydrogen", SC) >= 0;
}
private void AddGasTank(IMyGasTank tank, bool hydrogen, ref double fill, ref double capacity, ref int count)
{
    if (tank == null || selectedPowerIds.Contains(tank.EntityId) || IsHydrogenTank(tank) != hydrogen)
        return;
    selectedPowerIds.Add(tank.EntityId);
    double max = tank.Capacity;
    fill += max * tank.FilledRatio;
    capacity += max;
    count++;
}
private void AddConfiguredGasTanks(string groupName, bool hydrogen, ref double fill, ref double capacity, ref int count)
{
    if (string.IsNullOrEmpty(groupName))
        return;
    powerGroupBlocks.Clear();
    var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
    if (group == null)
        return;
    group.GetBlocks(powerGroupBlocks, b => b.IsSameConstructAs(Me));
    foreach (var block in powerGroupBlocks)
        AddGasTank(block as IMyGasTank, hydrogen, ref fill, ref capacity, ref count);
}
private void AddUngroupedGasTanks(ref double h2Fill, ref double h2Capacity, ref int h2Count, ref double o2Fill, ref double o2Capacity, ref int o2Count)
{
    foreach (var tank in gasTanks)
    {
        if (IsHydrogenTank(tank))
            AddGasTank(tank, true, ref h2Fill, ref h2Capacity, ref h2Count);
        else
            AddGasTank(tank, false, ref o2Fill, ref o2Capacity, ref o2Count);
    }
}
private void AddGasGenerator(IMyGasGenerator generator, MyItemType iceType, ref int online, ref int working, ref int total, ref double ice)
{
    if (generator == null || selectedPowerIds.Contains(generator.EntityId))
        return;
    selectedPowerIds.Add(generator.EntityId);
    total++;
    if (generator.Enabled) online++;
    if (generator.IsWorking) working++;
    ice += (double)generator.GetInventory(0).GetItemAmount(iceType);
}
private void AddConfiguredGasGenerators(string groupName, MyItemType iceType, ref int online, ref int working, ref int total, ref double ice)
{
    if (string.IsNullOrEmpty(groupName))
        return;
    powerGroupBlocks.Clear();
    var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
    if (group == null)
        return;
    group.GetBlocks(powerGroupBlocks, b => b.IsSameConstructAs(Me));
    foreach (var block in powerGroupBlocks)
        AddGasGenerator(block as IMyGasGenerator, iceType, ref online, ref working, ref total, ref ice);
}
private void AddUngroupedGasGenerators(MyItemType iceType, ref int online, ref int working, ref int total, ref double ice)
{
    foreach (var generator in gasGenerators)
        AddGasGenerator(generator, iceType, ref online, ref working, ref total, ref ice);
}
private bool IsInteriorVentMonitor(IMyTerminalBlock block)
{
    if (block == null)
        return false;
    if (!block.CustomName.Contains(LCD_TAG))
        return false;
    return block.CustomData.IndexOf("InteriorVent", SC) >= 0;
}
private bool UpdateVentStatus(out int okCount, out int leakCount, out string leakingVents)
{
    okCount = 0;
    leakCount = 0;
    leakingVents = "";
    for (int i = 0; i < airVents.Count; i++)
    {
        IMyAirVent vent = airVents[i];
        if (vent == null)
            continue;
        bool ok = vent.IsWorking && vent.CanPressurize && vent.GetOxygenLevel() >= 0.95f;
        SetVentStatusTag(vent, ok);
        if (ok)
            okCount++;
        else
        {
            leakCount++;
            if (leakingVents.Length < 80)
            {
                if (leakingVents.Length > 0) leakingVents += ", ";
                leakingVents += CleanVentName(vent.CustomName);
            }
        }
    }
    return okCount > 0 && leakCount == 0;
}
private void SetVentStatusTag(IMyAirVent vent, bool ok)
{
    string clean = CleanVentName(vent.CustomName);
    string tag = ok ? "[Pressurized]" : "[Leaking]";
    string wanted = clean + " " + tag;
    if (vent.CustomName != wanted)
        vent.CustomName = wanted;
}
private string CleanVentName(string name)
{
    if (string.IsNullOrEmpty(name))
        return "Air Vent";
    return name.Replace("[Pressurized]", "").Replace("[Leaking]", "").Replace("  ", " ").Trim();
}
private double GetItemAmount(string category, string subtype)
{
    ItemTotal total;
    if (totalsByKey.TryGetValue(category + "/" + subtype, out total))
        return total.Amount;
    return 0.0;
}
private void DrawTankMetricRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, int tankCount, double value, double max, string icon)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 50f);
    Fill(frame, row, COLOR_PANEL_2);
    TryDrawSprite(frame, icon, new Vector2(row.X + 22f, row.Y + 18f), new Vector2(26f, 26f), Color.White);
    double ratio = max > 0.0 ? value / max : 0.0;
    if (ratio < 0.0) ratio = 0.0;
    if (ratio > 1.0) ratio = 1.0;
    DrawText(frame, label, new Vector2(row.X + 48f, row.Y + 4f), COLOR_TEXT, 0.54f, TextAlignment.LEFT);
    DrawText(frame, tankCount + " tanks", new Vector2(row.X + 48f, row.Y + 22f), COLOR_DIM, 0.34f, TextAlignment.LEFT);
    DrawText(frame, (ratio * 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%",
        new Vector2(row.Right - 18f, row.Y + 4f), COLOR_ACCENT_2, 0.54f, TextAlignment.RIGHT);
    DrawText(frame, FormatVolume(value) + " / " + FormatVolume(max),
        new Vector2(row.Right - 18f, row.Y + 22f), COLOR_DIM, 0.34f, TextAlignment.RIGHT);
    float barX = row.X + 48f;
    RectangleF bar = new RectangleF(barX, row.Y + 38f, row.Right - barX - 18f, 7f);
    Fill(frame, bar, COLOR_BG);
    Color fillColor = ratio < 0.25 ? COLOR_LOW : (ratio < 0.60 ? COLOR_WARN : COLOR_OK);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)ratio, bar.Height), fillColor);
    DrawBorder(frame, bar, COLOR_DIM, 1f, true, true, true, true);
}
private void DrawFuelInfoRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, string value)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 26f);
    Fill(frame, row, COLOR_PANEL_2);
    DrawText(frame, label, new Vector2(row.X + 10f, row.Y + 4f), COLOR_TEXT, 0.48f, TextAlignment.LEFT);
    DrawText(frame, value, new Vector2(row.Right - 10f, row.Y + 4f), COLOR_ACCENT_2, 0.48f, TextAlignment.RIGHT);
}
private void DrawAutocraftingScreen(IMyTextSurface surface, string raw, ScreenCommand cmd)
{
    if (surface == null)
        return;
    PrepareSurface(surface, 0.78f);
    RectangleF vp = Viewport(surface);
    string category = cmd.CraftCategory;
    CollectCraftQuotas(raw, category, craftQuotas);
    using (var frame = surface.DrawFrame())
    {
        Fill(frame, vp, COLOR_BG);
        RectangleF panel = Inset(vp, 10f);
        Fill(frame, panel, COLOR_PANEL);
        DrawBorder(frame, panel, COLOR_ACCENT, 3f, true, true, true, true);
        DrawText(frame, "AUTOCRAFTING", panel.Position + new Vector2(24f, 24f), COLOR_ACCENT_2, 1.0f, TextAlignment.LEFT);
        DrawText(frame, category.ToUpperInvariant(), panel.Position + new Vector2(panel.Width - 24f, 26f), COLOR_DIM, 0.62f, TextAlignment.RIGHT);
        if (craftQuotas.Count == 0)
        {
            DrawText(frame, "ADD QUOTAS IN LCD CUSTOM DATA", panel.Position + new Vector2(24f, 92f), COLOR_TEXT, 0.66f, TextAlignment.LEFT);
            DrawText(frame, "SteelPlate=20000", panel.Position + new Vector2(24f, 130f), COLOR_DIM, 0.58f, TextAlignment.LEFT);
            DrawText(frame, "InteriorPlate=5000", panel.Position + new Vector2(24f, 158f), COLOR_DIM, 0.58f, TextAlignment.LEFT);
            return;
        }
        float y = panel.Y + 76f;
        float rowH = 58f;
        int autoRows = Math.Max(3, (int)((panel.Bottom - y - 36f) / rowH));
        int rowsPerPage = cmd.RowsPerPage > 0 ? cmd.RowsPerPage : autoRows;
        int pageCount = Math.Max(1, (craftQuotas.Count + rowsPerPage - 1) / rowsPerPage);
        int page = Math.Max(1, Math.Min(cmd.Page, pageCount));
        int start = (page - 1) * rowsPerPage;
        int end = Math.Min(craftQuotas.Count, start + rowsPerPage);
        int drawn = 0;
        for (int i = start; i < end && drawn < autoRows; i++)
        {
            DrawCraftQuotaRow(frame, panel, y, rowH, craftQuotas[i]);
            y += rowH;
            drawn++;
        }
        DrawText(frame, assemblers.Count + " assemblers  |  " + craftQuotas.Count + " quotas  |  page " + page + "/" + pageCount,
            new Vector2(panel.X + 24f, panel.Bottom - 24f), COLOR_DIM, 0.55f, TextAlignment.LEFT);
    }
}
private void DrawCraftQuotaRow(MySpriteDrawFrame frame, RectangleF panel, float y, float h, CraftQuota quota)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, h - 6f);
    Fill(frame, row, COLOR_PANEL_2);
    string subtype = quota.Key.Substring(quota.Key.IndexOf('/') + 1);
    string sprite = "MyObjectBuilder_" + quota.Category + "/" + subtype;
    TryDrawSprite(frame, sprite, new Vector2(row.X + 24f, row.Y + row.Height * 0.5f), new Vector2(28f, 28f), Color.White);
    double effective = quota.Current + quota.Queued;
    double ratio = quota.Wanted > 0.0 ? effective / quota.Wanted : 0.0;
    if (ratio > 1.0) ratio = 1.0;
    Color statusColor = !quota.HasBlueprint ? COLOR_LOW : (effective >= quota.Wanted ? COLOR_OK : COLOR_WARN);
    DrawText(frame, quota.Name, new Vector2(row.X + 48f, row.Y + 5f), COLOR_TEXT, 0.54f, TextAlignment.LEFT);
    DrawText(frame, FormatAmount(quota.Current) + " / " + FormatAmount(quota.Wanted),
        new Vector2(row.Right - 18f, row.Y + 5f), statusColor, 0.50f, TextAlignment.RIGHT);
    float barX = row.X + 48f;
    if (!quota.HasBlueprint)
        DrawText(frame, "NO BP", new Vector2(row.Right - 18f, row.Y + 25f), COLOR_LOW, 0.40f, TextAlignment.RIGHT);
    else if (quota.Queued > 0.0)
        DrawText(frame, "+" + FormatAmount(quota.Queued) + " queued", new Vector2(row.Right - 18f, row.Y + 25f), COLOR_DIM, 0.40f, TextAlignment.RIGHT);
    RectangleF bar = new RectangleF(barX, row.Y + 42f, row.Right - barX - 18f, 7f);
    Fill(frame, bar, COLOR_BG);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * (float)ratio, bar.Height), statusColor);
}
private void DrawPowerDashboard(IMyTextSurface surface, string profileName)
{
    if (surface == null)
        return;
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
        y += 58f;
        DrawMetricRow(frame, panel, y, "Output", production, maxProduction, "MW", maxProduction > 0.0 ? production / maxProduction : 0.0, "IconEnergy");
        y += 58f;
        Color netColor = net >= 0.0 ? COLOR_OK : COLOR_LOW;
        DrawText(frame, "NET", new Vector2(panel.X + 24f, y + 5f), COLOR_DIM, 0.52f, TextAlignment.LEFT);
        DrawText(frame, FormatPower(net, "MW"), new Vector2(panel.X + 88f, y), netColor, 0.74f, TextAlignment.LEFT);
        DrawText(frame, "BAT IN  " + FormatPower(batteryIn, "MW"), new Vector2(panel.Right - 24f, y - 2f), COLOR_DIM, 0.48f, TextAlignment.RIGHT);
        DrawText(frame, "BAT OUT " + FormatPower(batteryOut, "MW"), new Vector2(panel.Right - 24f, y + 18f), COLOR_DIM, 0.48f, TextAlignment.RIGHT);
        y += 56f;
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
            if (inner.StartsWith("power:", SC))
            {
                string name = inner.Substring(6).Trim();
                if (wanted.Length == 0 || string.Equals(name, wanted, SC))
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
        if (string.Equals(key, "batteries", SC)) cfg.BatteriesGroup = value;
        else if (string.Equals(key, "reactors", SC)) cfg.ReactorsGroup = value;
        else if (string.Equals(key, "solar", SC)) cfg.SolarGroup = value;
        else if (string.Equals(key, "wind", SC)) cfg.WindGroup = value;
        else if (string.Equals(key, "hydrogen", SC)) cfg.HydrogenGroup = value;
        else if (string.Equals(key, "other", SC)) cfg.OtherGroup = value;
        else if (string.Equals(key, "include_ungrouped", SC)) cfg.IncludeUngrouped = ParseBool(value, false);
    }
    if (cfg.Found && string.IsNullOrEmpty(cfg.Name))
        cfg.Name = "Power";
    return cfg;
}
private LifeSupportConfig GetLifeSupportConfig(string wantedName)
{
    LifeSupportConfig cfg = new LifeSupportConfig();
    cfg.Found = false;
    cfg.IncludeUngrouped = true;
    cfg.HydrogenGroup = "";
    cfg.OxygenGroup = "";
    cfg.GeneratorsGroup = "";
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
            string name = "";
            if (inner.StartsWith("lifesupport:", SC))
                name = inner.Substring(12).Trim();
            else if (string.Equals(inner, "lifesupport", SC))
                name = "";
            else
                continue;
            if (wanted.Length == 0 || string.Equals(name, wanted, SC))
            {
                cfg.Found = true;
                cfg.IncludeUngrouped = false;
                inSection = true;
                capturedAny = true;
            }
            else if (capturedAny)
                break;
            continue;
        }
        if (!inSection)
            continue;
        string key, value;
        if (!TrySplitKeyValue(line, '=', out key, out value))
            continue;
        value = CleanGroupName(value);
        if (string.Equals(key, "hydrogen", SC) || string.Equals(key, "h2", SC)) cfg.HydrogenGroup = value;
        else if (string.Equals(key, "oxygen", SC) || string.Equals(key, "o2", SC)) cfg.OxygenGroup = value;
        else if (string.Equals(key, "generators", SC) || string.Equals(key, "o2_generators", SC)) cfg.GeneratorsGroup = value;
        else if (string.Equals(key, "include_ungrouped", SC)) cfg.IncludeUngrouped = ParseBool(value, false);
    }
    return cfg;
}
private string CleanGroupName(string value)
{
    if (value == null)
        return "";
    value = value.Trim();
    if (value.StartsWith("G:", SC))
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
        return producer is IMyReactor || type.IndexOf("Reactor", SC) >= 0;
    if (kind == "hydrogen")
        return type.IndexOf("HydrogenEngine", SC) >= 0;
    if (kind == "solar")
        return type.IndexOf("Solar", SC) >= 0;
    if (kind == "wind")
        return type.IndexOf("Wind", SC) >= 0;
    return true;
}
private void DrawMetricRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, double value, double max, string unit, double ratio, string icon)
{
    RectangleF row = new RectangleF(panel.X + 16f, y, panel.Width - 32f, 50f);
    Fill(frame, row, COLOR_PANEL_2);
    TryDrawSprite(frame, icon, new Vector2(row.X + 22f, row.Y + 18f), new Vector2(26f, 26f), Color.White);
    DrawText(frame, label, new Vector2(row.X + 48f, row.Y + 7f), COLOR_TEXT, 0.58f, TextAlignment.LEFT);
    DrawText(frame, FormatPower(value, unit) + " / " + FormatPower(max, unit), new Vector2(row.Right - 18f, row.Y + 7f), COLOR_ACCENT_2, 0.54f, TextAlignment.RIGHT);
    float barX = row.X + 48f;
    RectangleF bar = new RectangleF(barX, row.Y + 34f, row.Right - barX - 18f, 9f);
    Fill(frame, bar, COLOR_BG);
    float fill = (float)Math.Max(0.0, Math.Min(1.0, ratio));
    Color fillColor = fill < 0.25f ? COLOR_LOW : (fill < 0.60f ? COLOR_WARN : COLOR_OK);
    Fill(frame, new RectangleF(bar.X, bar.Y, bar.Width * fill, bar.Height), fillColor);
    DrawBorder(frame, bar, COLOR_DIM, 1f, true, true, true, true);
}
private void DrawPowerSourceRow(MySpriteDrawFrame frame, RectangleF panel, float y, string label, double value, double max, Color textColor)
{
    double ratio = max > 0.0 ? value / max : 0.0;
    float barW = panel.Width * 0.30f;
    RectangleF bar = new RectangleF(panel.Right - barW - 24f, y + 5f, barW, 9f);
    DrawText(frame, label, new Vector2(panel.X + 24f, y), textColor, 0.58f, TextAlignment.LEFT);
    DrawText(frame, FormatPower(value, "MW") + " / " + FormatPower(max, "MW"), new Vector2(bar.X - 18f, y), COLOR_DIM, 0.48f, TextAlignment.RIGHT);
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
private void PrepareSurface(IMyTextSurface surface, float fontSize)
{
    if (surface == null)
        return;
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
    Echo("State  : " + (statusEnabledByCore ? "RUNNING" : "DISABLED BY CORE"));
    Echo("Core   : " + (coreFound ? "FOUND " + CORE_TAG : "standalone"));
    Echo("Main PB: " + (IsMainPb() ? "ON " + PB_TAG : "OFF"));
    Echo("LCDs   : " + lcdBlocks.Count);
    Echo("Cargo  : " + inventories.Count);
    Echo("Items  : " + itemTotals.Count);
    Echo("Power  : " + batteries.Count + " batteries, " + powerProducers.Count + " producers");
    Echo("Craft  : " + assemblers.Count + " assemblers");
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
