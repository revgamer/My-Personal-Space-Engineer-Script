// AutoGrid Manager v2.0
// Author: RevGamer
// Stage 1 - Scaffold + Category Sorting + Basic Dashboards
// Update1 frequency, instruction-gated pipeline, no hard move cap

// ============================================================
// REGION: Constants & Theme
// ============================================================
private const string VERSION   = "2.0-S1";
private const string STOCK_TAG = "[Stock]";
private const string NOSORT_TAG= "[AGM:NoSort]";
private const string MANUAL_TAG= "[AGM:Manual]";
private const string AGM_SEC   = "[AGM]";
private const string STOCK_BEGIN = "=== AGM Stock BEGIN ===";
private const string STOCK_END   = "=== AGM Stock END ===";
private int          RESCAN_TICKS = 180;    // updated by config (default: 30s at Update10)
private int          _updateFreq  = 10;
private const int    INST_GUARD   = 35000;

private readonly Color COL_BG        = new Color(1,8,13);
private readonly Color COL_PANEL     = new Color(2,18,28);
private readonly Color COL_PANEL2    = new Color(3,58,78);
private readonly Color COL_ACCENT    = new Color(38,239,255);
private readonly Color COL_ACCENT2   = new Color(112,247,255);
private readonly Color COL_TEXT      = new Color(126,246,255);
private readonly Color COL_DIM       = new Color(44,177,195);
private readonly Color COL_ROW_TEXT  = new Color(126,246,255);
private readonly Color COL_ROW_DIM   = new Color(63,207,222);
private readonly Color COL_OK        = new Color(97,255,214);
private readonly Color COL_WARN      = new Color(255,202,34);
private readonly Color COL_BAD       = new Color(255,79,66);
private readonly Color COL_PROG_BG   = new Color(18,48,32);
private readonly Color COL_PROG_FILL = new Color(255,204,36);

// ============================================================
// REGION: Data Classes
// ============================================================
private class LcdEntry
{
    public IMyTextSurface Surface;
    public string Command;
    public int Page;
}

private class CategoryContainer
{
    public IMyTerminalBlock Block;
    public IMyInventory Inv;
    public string Category;
    public bool Locked;
}

private class StockItem
{
    public string SubtypeId;
    public string Mode;   // "Target","Min","Max","All","Disabled"
    public double Amount;
    public bool Pinned;
}

private class StockContainer
{
    public IMyTerminalBlock Block;
    public IMyInventory Inv;
    public int Priority;
    public string CategoryFilter;
    public List<StockItem> Items = new List<StockItem>();
}

// ============================================================
// REGION: Fields - Scheduler
// ============================================================
private int    _stage     = 0;
private int    _tick      = 0;
private int    _srcIdx    = 0;   // resume index for sorter
private int    _stockIdx   = 0;   // resume index for stock manager
private int    _lcdIdx    = 0;   // resume index for LCD draw
private bool   _paused    = false;
private string _status    = "boot";
private string _lastErr   = "";
private int    _movesThisCycle = 0;
private double _animPulse  = 0.0;
private bool   _booting    = true;
private float  _bootPct    = 0.0f;
private int    _bootTick   = 0;
private const int BOOT_TICKS = 180;  // 3 seconds at Update1

// ============================================================
// REGION: Fields - Block Lists
// ============================================================
private readonly List<IMyTerminalBlock>  _allBlocks    = new List<IMyTerminalBlock>();
private readonly List<IMyShipConnector>  _connectors   = new List<IMyShipConnector>();
private readonly List<IMyCargoContainer> _cargo        = new List<IMyCargoContainer>();
private readonly List<IMyAssembler>      _assemblers   = new List<IMyAssembler>();
private readonly List<IMyAssembler>      _agmAssemblers      = new List<IMyAssembler>();
private readonly List<IMyAssembler>      _agmBasicAssemblers = new List<IMyAssembler>();
private readonly List<IMyRefinery>       _refineries   = new List<IMyRefinery>();
private readonly List<CategoryContainer> _catContainers= new List<CategoryContainer>();
private readonly List<StockContainer>    _stockContainers = new List<StockContainer>();
private readonly List<LcdEntry>          _lcds         = new List<LcdEntry>();
private readonly HashSet<IMyCubeGrid>    _excludedGrids= new HashSet<IMyCubeGrid>();

// ============================================================
// REGION: Fields - Inventory Data
// ============================================================
private readonly Dictionary<MyItemType,double> _totals = new Dictionary<MyItemType,double>();
private readonly Dictionary<string,double>     _catTotals = new Dictionary<string,double>();
private readonly List<MyInventoryItem>          _itemBuf = new List<MyInventoryItem>();
private readonly StringBuilder                  _sb = new StringBuilder();

// ============================================================
// REGION: Fields - Config / Autocrafting
// ============================================================
private readonly Dictionary<string,double> _acQuotas  = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
private bool   _acEnabled        = true;
private double _acAssembleMargin = 0.0;
private double _acDisassembleMargin = 10.0;
private bool   _acAutoDisassemble= true;
private bool   _acSortQueue      = true;
private readonly Dictionary<string,MyDefinitionId> _bpCache = new Dictionary<string,MyDefinitionId>(StringComparer.OrdinalIgnoreCase);

// ============================================================
// REGION: Constructor
// ============================================================
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    _status = "boot";
    if (string.IsNullOrEmpty(Me.CustomData))
        WritePbTemplate();
}

public void Save() { }

// ============================================================
// REGION: Main
// ============================================================
public void Main(string argument, UpdateType updateSource)
{
    _tick++;
    _animPulse += 0.08;
    if (_animPulse > 1000.0) _animPulse -= 1000.0;

    // Handle run arguments
    if ((updateSource & UpdateType.Trigger) != 0 || (updateSource & UpdateType.Terminal) != 0)
    {
        HandleArgument(argument.Trim().ToLowerInvariant());
        return;
    }

    if (_paused) { DrawPbScreen(); return; }

    // Rescan on interval or first boot
    if (_tick == 1 || _tick % RESCAN_TICKS == 0)
    {
        ParseConfig();
        ScanBlocks();
        RebuildDockedFilter();
        ParseAllStockContainers();
        ParseAllLcds();
        _srcIdx = 0;
        _lcdIdx = 0;
        _stage  = 0;
    }

    // Boot sequence
    if (_booting)
    {
        _bootTick++;
        _bootPct = Math.Min(1.0f, (float)_bootTick / BOOT_TICKS);
        DrawBootScreen();
        if (_bootTick >= BOOT_TICKS) _booting = false;
        return;
    }

    // Pipeline - one stage per tick, instruction-gated
    try
    {
        switch (_stage)
        {
            case 0: StageCount();       break;
            case 1: StageStock();       break;
            case 2: StageSort();        break;
            case 3: StageAutocraft();   break;
            case 4: StageLcd();         break;
            default: _stage = 0;        break;
        }
    }
    catch (Exception ex)
    {
        _lastErr = ex.Message;
        _stage = 0;
    }

    DrawPbScreen();
    Echo("AGM v" + VERSION + "  tick=" + _tick + "  stage=" + _stage);
    Echo("status: " + _status);
    if (_lastErr.Length > 0) Echo("err: " + _lastErr);
}

// ============================================================
// REGION: Arguments
// ============================================================
private void HandleArgument(string arg)
{
    if (arg == "pause")   { _paused = true;  _status = "paused"; }
    else if (arg == "resume") { _paused = false; _status = "running"; }
    else if (arg == "scan")
    {
        ParseConfig(); ScanBlocks(); RebuildDockedFilter();
        ParseAllStockContainers(); ParseAllLcds();
        _srcIdx = 0; _lcdIdx = 0; _stage = 0;
        _status = "rescan";
    }
    else if (arg == "reset")
    {
        _bpCache.Clear(); _totals.Clear(); _catTotals.Clear();
        ParseConfig(); ScanBlocks(); RebuildDockedFilter();
        ParseAllStockContainers(); ParseAllLcds();
        _srcIdx = 0; _lcdIdx = 0; _stage = 0;
        _status = "reset";
    }
    else if (arg == "debug")
    {
        Echo("AGM v" + VERSION + " DEBUG");
        Echo("blocks=" + _allBlocks.Count);
        Echo("cargo=" + _cargo.Count);
        Echo("catContainers=" + _catContainers.Count);
        Echo("stockContainers=" + _stockContainers.Count);
        Echo("lcds=" + _lcds.Count);
        Echo("excludedGrids=" + _excludedGrids.Count);
        Echo("totals=" + _totals.Count);
        Echo("acQuotas=" + _acQuotas.Count);
        Echo("stage=" + _stage);
        Echo("--- [AGM] blocks ---");
        for (int di = 0; di < _allBlocks.Count; di++)
        {
            IMyTerminalBlock db = _allBlocks[di];
            if (!db.HasInventory) continue;
            string dcd = db.CustomData ?? "";
            if (dcd.Length == 0) continue;
            string dcat = ReadAgmKey(dcd, "type");
            if (dcat.Length > 0)
                Echo("  CAT: " + db.CustomName + " type=" + dcat);
        }
        if (_catContainers.Count == 0)
            Echo("  no category containers found");
    }
}

// ============================================================
// REGION: PB Custom Data Template Writer
// ============================================================
private void WritePbTemplate()
{
    Me.CustomData =
        "[AGM]
" +
        "; AGM performance settings
" +
        "update_frequency = 10       ; 1, 10, or 100 ticks
" +
        "rescan_interval = 30        ; seconds between full block rescans
" +
        "
" +
        "[Autocrafting]
" +
        "enabled = true
" +
        "assemble_margin = 5         ; craft when stock < quota * (1 - margin%)
" +
        "disassemble_margin = 10     ; disassemble when stock > quota * (1 + margin%)
" +
        "auto_disassemble = true
" +
        "
" +
        "; === Basic Components (BasicAssembler) ===
" +
        "SteelPlate = 5000
" +
        "InteriorPlate = 2000
" +
        "Construction = 1000
" +
        "SmallTube = 500
" +
        "LargeTube = 200
" +
        "Motor = 500
" +
        "Display = 50
" +
        "BulletproofGlass = 100
" +
        "Girder = 200
" +
        "
" +
        "; === Advanced Components (Assembler) ===
" +
        "Computer = 500
" +
        "MetalGrid = 200
" +
        "PowerCell = 50
" +
        "RadioCommunication = 20
" +
        "Reactor = 10
" +
        "SolarCell = 50
" +
        "Superconductor = 20
" +
        "Thrust = 50
" +
        "Medical = 10
" +
        "Detector = 20
" +
        "Explosives = 10
" +
        "Canvas = 10
" +
        "
" +
        "; === Ammo (Assembler) ===
" +
        "NATO_5p56x45mm = 2000
" +
        "NATO_25x184mm = 500
" +
        "Missile200mm = 500
" +
        "LargeCalibreAmmo = 200
" +
        "MediumCalibreAmmo = 500
" +
        "AutocannonClip = 500
" +
        "SmallRailgunAmmo = 100
" +
        "LargeRailgunAmmo = 50
" +
        "
" +
        "; === Tools (Assembler) ===
" +
        "AngleGrinderItem = 5
" +
        "HandDrillItem = 5
" +
        "WelderItem = 5
" +
        "AngleGrinder2Item = 3
" +
        "HandDrill2Item = 3
" +
        "Welder2Item = 3
" +
        "AngleGrinder3Item = 2
" +
        "HandDrill3Item = 2
" +
        "Welder3Item = 2
" +
        "AngleGrinder4Item = 1
" +
        "HandDrill4Item = 1
" +
        "Welder4Item = 1
" +
        "
" +
        "; === Medkits / Powerkits (Assembler) ===
" +
        "Medkit = 10
" +
        "Powerkit = 10
" +
        "
" +
        "; === Food (Assembler) ===
" +
        "; Set to 0 to disable a food item
" +
        "MealPack_Burrito = 10
" +
        "MealPack_Chili = 10
" +
        "MealPack_Curry = 10
" +
        "MealPack_Dumplings = 10
" +
        "MealPack_Flatbread = 10
" +
        "MealPack_FrontierStew = 10
" +
        "MealPack_FruitBar = 10
" +
        "MealPack_FruitPastry = 10
" +
        "MealPack_GardenSlaw = 10
" +
        "MealPack_GreenPellets = 10
" +
        "MealPack_KelpCrisp = 10
" +
        "MealPack_Lasagna = 10
" +
        "MealPack_Ramen = 10
" +
        "MealPack_RedPellets = 10
" +
        "MealPack_SearedSabiroid = 10
" +
        "MealPack_Spaghetti = 10
" +
        "MealPack_SteakDinner = 10
" +
        "MealPack_VeggieBurger = 10
" +
        "ClangCola = 5
" +
        "CosmicCoffee = 5
" +
        "InsectMeatCooked = 5
" +
        "MammalMeatCooked = 5
";
}

// ============================================================
// REGION: Config Parser
// ============================================================
private void ParseConfig()
{
    _acQuotas.Clear();
    _acEnabled = true;
    _acAssembleMargin = 5.0;
    _acDisassembleMargin = 10.0;
    _acAutoDisassemble = true;
    _acSortQueue = true;
    int rescanSec = 30;
    _updateFreq = 10;

    string cd = Me.CustomData ?? "";
    if (string.IsNullOrEmpty(cd)) { WritePbTemplate(); cd = Me.CustomData ?? ""; }

    bool inAgm = false;
    bool inAc  = false;
    string[] lines = cd.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string raw = lines[i];
        int ci = raw.IndexOf(';');
        string line = (ci >= 0 ? raw.Substring(0, ci) : raw).Trim();
        if (line.Length == 0) continue;

        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            string sec = line.Substring(1, line.Length - 2).Trim();
            inAgm = sec.Equals("AGM", StringComparison.OrdinalIgnoreCase);
            inAc  = sec.Equals("Autocrafting", StringComparison.OrdinalIgnoreCase);
            continue;
        }

        string key, val;
        if (!TrySplit(line, '=', out key, out val)) continue;

        if (inAgm)
        {
            if (key.Equals("update_frequency", StringComparison.OrdinalIgnoreCase))
            {
                int f; if (int.TryParse(val, out f) && (f==1||f==10||f==100)) _updateFreq = f;
                continue;
            }
            if (key.Equals("rescan_interval", StringComparison.OrdinalIgnoreCase))
            {
                int r; if (int.TryParse(val, out r) && r > 0) rescanSec = r;
                continue;
            }
        }

        if (!inAc) continue;

        if (key.Equals("enabled", StringComparison.OrdinalIgnoreCase))
            { _acEnabled = ParseBool(val, true); continue; }
        if (key.Equals("assemble_margin", StringComparison.OrdinalIgnoreCase))
            { double v; if (double.TryParse(val, out v)) _acAssembleMargin = v; continue; }
        if (key.Equals("disassemble_margin", StringComparison.OrdinalIgnoreCase))
            { double v; if (double.TryParse(val, out v)) _acDisassembleMargin = v; continue; }
        if (key.Equals("auto_disassemble", StringComparison.OrdinalIgnoreCase))
            { _acAutoDisassemble = ParseBool(val, true); continue; }
        if (key.Equals("sort_queue", StringComparison.OrdinalIgnoreCase))
            { _acSortQueue = ParseBool(val, true); continue; }

        double quota;
        if (double.TryParse(val, out quota) && quota >= 0)
            _acQuotas[key] = quota;
        else
            Echo("AGM config: bad quota line: " + line);
    }

    // Apply update frequency and rescan interval
    RESCAN_TICKS = rescanSec * (_updateFreq == 1 ? 60 : _updateFreq == 10 ? 6 : 1);
    UpdateFrequency uf = UpdateFrequency.Update1;
    if (_updateFreq == 10)  uf = UpdateFrequency.Update10;
    if (_updateFreq == 100) uf = UpdateFrequency.Update100;
    Runtime.UpdateFrequency = uf;
}

// ============================================================
// REGION: Block Scanner
// ============================================================
private void ScanBlocks()
{
    _allBlocks.Clear(); _connectors.Clear(); _cargo.Clear();
    _assemblers.Clear(); _refineries.Clear();
    _catContainers.Clear();

    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(_allBlocks,
        b => b.IsSameConstructAs(Me));

    for (int i = 0; i < _allBlocks.Count; i++)
    {
        IMyTerminalBlock b = _allBlocks[i];
        IMyShipConnector conn = b as IMyShipConnector;
        if (conn != null) { _connectors.Add(conn); continue; }
        IMyAssembler asm = b as IMyAssembler;
        if (asm != null) { _assemblers.Add(asm); continue; }
        IMyRefinery ref2 = b as IMyRefinery;
        if (ref2 != null) { _refineries.Add(ref2); continue; }
        IMyCargoContainer cargo = b as IMyCargoContainer;
        if (cargo != null) _cargo.Add(cargo);
    }

    // Build AGM assembler lists from Custom Data tags
    _agmAssemblers.Clear();
    _agmBasicAssemblers.Clear();
    for (int i = 0; i < _assemblers.Count; i++)
    {
        IMyAssembler a = _assemblers[i];
        string acd = a.CustomData ?? "";
        if (acd.Contains("[AGM] BasicAssembler") || acd.Contains("[AGM]BasicAssembler"))
            _agmBasicAssemblers.Add(a);
        else if (acd.Contains("[AGM] Assembler") || acd.Contains("[AGM]Assembler"))
            _agmAssemblers.Add(a);
    }

    // Build category container list from all blocks with [AGM] Custom Data
    for (int i = 0; i < _allBlocks.Count; i++)
    {
        IMyTerminalBlock b = _allBlocks[i];
        if (!b.HasInventory) continue;
        string cd = b.CustomData ?? "";
        if (!cd.Contains(AGM_SEC)) continue;
        string cat = ReadAgmKey(cd, "type");
        if (cat.Length == 0) continue;
        bool locked = ParseBool(ReadAgmKey(cd, "locked"), false);
        var cc = new CategoryContainer();
        cc.Block    = b;
        cc.Inv      = b.GetInventory(0);
        cc.Category = cat;
        cc.Locked   = locked;
        _catContainers.Add(cc);
    }
}

// ============================================================
// REGION: Docked Grid Filter
// ============================================================
private void RebuildDockedFilter()
{
    _excludedGrids.Clear();
    for (int i = 0; i < _connectors.Count; i++)
    {
        IMyShipConnector c = _connectors[i];
        if (!c.CustomName.Contains(NOSORT_TAG)) continue;
        if (c.Status != MyShipConnectorStatus.Connected) continue;
        IMyShipConnector other = c.OtherConnector;
        if (other != null) _excludedGrids.Add(other.CubeGrid);
    }
}

// ============================================================
// REGION: Stock Container Parser
// ============================================================
private void ParseAllStockContainers()
{
    _stockContainers.Clear();
    for (int i = 0; i < _cargo.Count; i++)
    {
        IMyCargoContainer b = _cargo[i];
        if (!b.CustomName.Contains(STOCK_TAG)) continue;
        var sc = new StockContainer();
        sc.Block = b;
        sc.Inv   = b.GetInventory(0);

        string cd = b.CustomData ?? "";
        bool hasMarker = cd.Contains(STOCK_BEGIN);

        if (!hasMarker)
        {
            WriteStockTemplate(b);
            cd = b.CustomData ?? "";
        }

        string agmSec = ReadAgmKey(cd, "priority");
        int pri;
        if (!int.TryParse(agmSec, out pri)) pri = 5;
        sc.Priority = pri;
        sc.CategoryFilter = ReadAgmKey(cd, "category");

        ParseStockSection(cd, sc.Items);
        _stockContainers.Add(sc);
    }
    _stockContainers.Sort((a, b2) => a.Priority.CompareTo(b2.Priority));
}

private void ParseStockSection(string cd, List<StockItem> items)
{
    int start = cd.IndexOf(STOCK_BEGIN, StringComparison.OrdinalIgnoreCase);
    int end   = cd.IndexOf(STOCK_END,   StringComparison.OrdinalIgnoreCase);
    if (start < 0 || end < 0 || end <= start) return;

    string section = cd.Substring(start + STOCK_BEGIN.Length, end - start - STOCK_BEGIN.Length);
    string[] lines = section.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string raw = lines[i];
        int ci = raw.IndexOf(';');
        string line = (ci >= 0 ? raw.Substring(0, ci) : raw).Trim();
        if (line.Length == 0 || line.StartsWith(";")) continue;

        string key, val;
        if (!TrySplit(line, '=', out key, out val)) continue;
        if (key.Length == 0 || val.Length == 0) continue;
        // Skip [AGM] section header keys
        if (key.Equals("category", StringComparison.OrdinalIgnoreCase)) continue;
        if (key.Equals("priority", StringComparison.OrdinalIgnoreCase)) continue;

        var si = new StockItem();
        si.SubtypeId = key;
        val = val.Trim();

        bool pinned = false;
        if (val.EndsWith("P", StringComparison.OrdinalIgnoreCase) ||
            val.EndsWith("MP", StringComparison.OrdinalIgnoreCase) ||
            val.EndsWith("LP", StringComparison.OrdinalIgnoreCase))
        {
            pinned = true;
            val = val.TrimEnd('P','p');
        }
        si.Pinned = pinned;

        if (val.Equals("All", StringComparison.OrdinalIgnoreCase))
            { si.Mode = "All"; si.Amount = 0; }
        else if (val.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            { si.Mode = "Disabled"; si.Amount = 0; }
        else if (val.EndsWith("M", StringComparison.OrdinalIgnoreCase))
        {
            double n;
            if (double.TryParse(val.TrimEnd('M','m'), out n))
                { si.Mode = "Min"; si.Amount = n; }
            else continue;
        }
        else if (val.EndsWith("L", StringComparison.OrdinalIgnoreCase))
        {
            double n;
            if (double.TryParse(val.TrimEnd('L','l'), out n))
                { si.Mode = "Max"; si.Amount = n; }
            else continue;
        }
        else
        {
            double n;
            if (double.TryParse(val, out n))
                { si.Mode = "Target"; si.Amount = n; }
            else { Echo("AGM stock: bad line: " + line); continue; }
        }
        items.Add(si);
    }
}

private void WriteStockTemplate(IMyCargoContainer b)
{
    string existing = b.CustomData ?? "";
    string template =
        STOCK_BEGIN + "\n" +
        "; AutoGrid Manager v2.0 - Stock Container Template\n" +
        "; Edit lines below. AGM will never rewrite this section.\n" +
        ";\n" +
        "; MODES:\n" +
        ";   ItemName = 5000        Target: fill to 5000, remove excess\n" +
        ";   ItemName = 5000M       Minimum: fill to 5000, keep excess\n" +
        ";   ItemName = 5000L       Limiter: remove above 5000, do not add\n" +
        ";   ItemName = All         Accept all until full\n" +
        ";   ItemName = Disabled    AGM ignores this item here\n" +
        ";   Append P to pin: SteelPlate = 5000P\n" +
        ";\n" +
        "; PRIORITY (lower = higher priority, default 5):\n" +
        ";   priority = 5\n" +
        ";\n" +
        "; CATEGORY FILTER (optional, limits what is sorted in):\n" +
        ";   category = Component\n" +
        ";\n" +
        "; --- Uncomment and edit items below ---\n" +
        ";\n" +
        "; === Components ===\n" +
        "; SteelPlate = 5000\n" +
        "; Motor = 500\n" +
        "; Computer = 500\n" +
        "; Construction = 1000\n" +
        "; MetalGrid = 200\n" +
        "; LargeTube = 200\n" +
        "; SmallTube = 500\n" +
        "; InteriorPlate = 2000\n" +
        "; BulletproofGlass = 100\n" +
        "; Display = 50\n" +
        "; Detector = 20\n" +
        "; Girder = 200\n" +
        "; Medical = 10\n" +
        "; PowerCell = 50\n" +
        "; RadioCommunication = 20\n" +
        "; Reactor = 10\n" +
        "; SolarCell = 50\n" +
        "; Superconductor = 20\n" +
        "; Thrust = 50\n" +
        ";\n" +
        "; === Ores ===\n" +
        "; Iron = 10000\n" +
        "; Nickel = 5000\n" +
        "; Cobalt = 5000\n" +
        "; Silicon = 3000\n" +
        "; Gold = 1000\n" +
        "; Silver = 1000\n" +
        "; Platinum = 500\n" +
        "; Uranium = 200\n" +
        "; Magnesium = 500\n" +
        "; Stone = 5000\n" +
        "; Ice = 5000\n" +
        ";\n" +
        "; === Ingots ===\n" +
        "; Iron = 5000\n" +
        "; Nickel = 2000\n" +
        "; Cobalt = 1000\n" +
        "; Silicon = 1000\n" +
        "; Gold = 200\n" +
        "; Silver = 500\n" +
        "; Platinum = 100\n" +
        "; Uranium = 50\n" +
        "; Magnesium = 100\n" +
        ";\n" +
        "; === Ammo ===\n" +
        "; NATO_5p56x45mm = 2000\n" +
        "; NATO_25x184mm = 500\n" +
        "; Missile200mm = 500\n" +
        ";\n" +
        "; === Tools ===\n" +
        "; AngleGrinderItem = 5\n" +
        "; HandDrillItem = 5\n" +
        "; WelderItem = 5\n" +
        STOCK_END + "\n";

    if (existing.Length == 0)
        b.CustomData = template;
    else
        b.CustomData = existing + "\n" + template;
}

// ============================================================
// REGION: LCD Parser
// ============================================================
private void ParseAllLcds()
{
    _lcds.Clear();
    for (int i = 0; i < _allBlocks.Count; i++)
    {
        IMyTerminalBlock b = _allBlocks[i];
        IMyTextSurfaceProvider prov = b as IMyTextSurfaceProvider;
        if (prov == null) continue;
        string cd = b.CustomData ?? "";
        if (!cd.Contains(AGM_SEC)) continue;

        bool inAgm = false;
        string[] lines = cd.Split('\n');
        for (int j = 0; j < lines.Length; j++)
        {
            string raw = lines[j];
            int ci2 = raw.IndexOf(';');
            string line = (ci2 >= 0 ? raw.Substring(0, ci2) : raw).Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                string sec = line.Substring(1, line.Length - 2).Trim();
                inAgm = sec.Equals("AGM", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inAgm) continue;

            string key, val;
            if (!TrySplit(line, '=', out key, out val)) continue;

            int surfaceIdx = 0;
            int page = 1;

            // Parse page from value e.g. "page1", "page 2", "1"
            string vl = val.Trim().ToLowerInvariant();
            if (vl.StartsWith("page")) vl = vl.Substring(4).Trim();
            int pg;
            if (int.TryParse(vl, out pg) && pg >= 1) page = pg;

            if (prov.SurfaceCount > surfaceIdx)
            {
                // Skip PB surface 0 -- reserved for the AGM status screen
                IMyTextSurface candidate = prov.GetSurface(surfaceIdx);
                if (b == Me && surfaceIdx == 0) continue;
                var entry = new LcdEntry();
                entry.Surface = candidate;
                entry.Command = key.Trim();
                entry.Page    = page;
                _lcds.Add(entry);
            }
        }
    }
}

// ============================================================
// REGION: Stage 0 - Inventory Count
// ============================================================
private void StageCount()
{
    _totals.Clear();
    _catTotals.Clear();

    for (int i = 0; i < _allBlocks.Count; i++)
    {
        IMyTerminalBlock b = _allBlocks[i];
        if (!b.HasInventory) continue;
        if (_excludedGrids.Contains(b.CubeGrid)) continue;
        if (b is IMyReactor || b is IMyGasGenerator || b is IMyGasTank) continue;
        if (IsLocked(b)) continue;

        // Check instruction budget
        if (Runtime.CurrentInstructionCount > INST_GUARD) return;

        int invCount = b is IMyProductionBlock ? 2 : 1;
        for (int k = 0; k < invCount; k++)
        {
            IMyInventory inv = b.GetInventory(k);
            if (inv == null) continue;
            _itemBuf.Clear();
            inv.GetItems(_itemBuf);
            for (int m = 0; m < _itemBuf.Count; m++)
            {
                MyInventoryItem item = _itemBuf[m];
                MyItemType t = item.Type;
                double amt = (double)item.Amount;
                double existing;
                if (_totals.TryGetValue(t, out existing))
                    _totals[t] = existing + amt;
                else
                    _totals[t] = amt;

                string cat = GetCategory(t);
                if (cat.Length > 0)
                {
                    double cex;
                    if (_catTotals.TryGetValue(cat, out cex))
                        _catTotals[cat] = cex + amt;
                    else
                        _catTotals[cat] = amt;
                }
            }
        }
    }
    _stage++;
    _status = "counted " + _totals.Count + " types";
}


// ============================================================
// REGION: Stage 1 - Stock Manager (fill/drain to quotas)
// ============================================================
private void StageStock()
{
    for (int si = _stockIdx; si < _stockContainers.Count; si++)
    {
        if (Runtime.CurrentInstructionCount > INST_GUARD) { _stockIdx = si; return; }

        StockContainer sc = _stockContainers[si];
        if (sc.Inv == null) continue;

        for (int ii = 0; ii < sc.Items.Count; ii++)
        {
            if (Runtime.CurrentInstructionCount > INST_GUARD) { _stockIdx = si; return; }

            StockItem si2 = sc.Items[ii];
            if (si2.Mode == "Disabled") continue;

            // Find item type from catalog
            MyItemType itemType = FindItemType(si2.SubtypeId);
            if (itemType == default(MyItemType)) continue;

            double current = (double)sc.Inv.GetItemAmount(itemType);

            if (si2.Mode == "Target" || si2.Mode == "Min")
            {
                // Need to fill up to quota
                if (current < si2.Amount)
                {
                    double needed = si2.Amount - current;
                    PullItemInto(sc.Inv, itemType, needed);
                }
                // Need to push excess out (Target only)
                if (si2.Mode == "Target" && current > si2.Amount * 1.01)
                {
                    double excess = current - si2.Amount;
                    PushItemOut(sc, itemType, excess);
                }
            }
            else if (si2.Mode == "Max")
            {
                // Limiter: push excess out, never pull in
                if (current > si2.Amount * 1.01)
                {
                    double excess = current - si2.Amount;
                    PushItemOut(sc, itemType, excess);
                }
            }
            else if (si2.Mode == "All")
            {
                // Accept everything - pull all available from anywhere
                PullItemInto(sc.Inv, itemType, double.MaxValue);
            }
        }
    }
    _stockIdx = 0;
    _stage++;
}

// Pull up to 'needed' amount of itemType into dstInv from any source
private void PullItemInto(IMyInventory dstInv, MyItemType itemType, double needed)
{
    if (!IsInventoryAvailable(dstInv)) return;
    double remaining = needed;

    for (int i = 0; i < _allBlocks.Count; i++)
    {
        if (Runtime.CurrentInstructionCount > INST_GUARD) return;
        if (remaining <= 0) return;

        IMyTerminalBlock src = _allBlocks[i];
        if (!src.HasInventory) continue;
        if (_excludedGrids.Contains(src.CubeGrid)) continue;
        if (src is IMyReactor || src is IMyGasGenerator || src is IMyGasTank) continue;
        if (IsLocked(src)) continue;

        IMyInventory srcInv = src.GetInventory(0);
        if (srcInv == null || srcInv == dstInv) continue;

        double available = (double)srcInv.GetItemAmount(itemType);
        if (available <= 0) continue;

        double toMove = Math.Min(available, remaining);
        MyFixedPoint fp = (MyFixedPoint)toMove;
        if (fp <= 0) continue;

        try
        {
            // Find slot index
            _itemBuf.Clear();
            srcInv.GetItems(_itemBuf);
            for (int m = _itemBuf.Count - 1; m >= 0; m--)
            {
                if (_itemBuf[m].Type != itemType) continue;
                if (srcInv.TransferItemTo(dstInv, m, null, true, fp))
                {
                    remaining -= toMove;
                    _movesThisCycle++;
                }
                break;
            }
        }
        catch { }
    }
}

// Push 'excess' amount of itemType out of srcContainer to overflow destinations
private void PushItemOut(StockContainer sc, MyItemType itemType, double excess)
{
    double remaining = excess;

    // Try other stock containers that list this item
    for (int i = 0; i < _stockContainers.Count; i++)
    {
        if (remaining <= 0) return;
        StockContainer dst = _stockContainers[i];
        if (dst.Inv == sc.Inv) continue;
        if (!IsInventoryAvailable(dst.Inv)) continue;

        bool listed = false;
        for (int j = 0; j < dst.Items.Count; j++)
        {
            if (dst.Items[j].SubtypeId.Equals(itemType.SubtypeId, StringComparison.OrdinalIgnoreCase)
                && dst.Items[j].Mode != "Disabled" && dst.Items[j].Mode != "Max")
            {
                listed = true; break;
            }
        }
        if (!listed) continue;
        remaining -= TransferOut(sc.Inv, dst.Inv, itemType, remaining);
    }

    // Try matching category container
    string cat = GetCategory(itemType);
    for (int i = 0; i < _catContainers.Count; i++)
    {
        if (remaining <= 0) return;
        CategoryContainer cc = _catContainers[i];
        if (cc.Inv == sc.Inv) continue;
        if (cc.Locked) continue;
        if (!cc.Category.Equals(cat, StringComparison.OrdinalIgnoreCase)) continue;
        if (!IsInventoryAvailable(cc.Inv)) continue;
        remaining -= TransferOut(sc.Inv, cc.Inv, itemType, remaining);
    }

    // Try any untagged cargo
    for (int i = 0; i < _cargo.Count; i++)
    {
        if (remaining <= 0) return;
        IMyInventory dstInv = _cargo[i].GetInventory(0);
        if (dstInv == null || dstInv == sc.Inv) continue;
        if (_cargo[i].CustomName.Contains(STOCK_TAG)) continue;
        if (!IsInventoryAvailable(dstInv)) continue;
        remaining -= TransferOut(sc.Inv, dstInv, itemType, remaining);
    }
}

private double TransferOut(IMyInventory srcInv, IMyInventory dstInv, MyItemType itemType, double maxAmount)
{
    _itemBuf.Clear();
    srcInv.GetItems(_itemBuf);
    for (int m = _itemBuf.Count - 1; m >= 0; m--)
    {
        if (_itemBuf[m].Type != itemType) continue;
        double available = (double)_itemBuf[m].Amount;
        double toMove = Math.Min(available, maxAmount);
        MyFixedPoint fp = (MyFixedPoint)toMove;
        if (fp <= 0) continue;
        try
        {
            if (srcInv.TransferItemTo(dstInv, m, null, true, fp))
            {
                _movesThisCycle++;
                return toMove;
            }
        }
        catch { }
        break;
    }
    return 0;
}

// Find a MyItemType from a SubtypeId by checking _totals keys
private MyItemType FindItemType(string subtypeId)
{
    foreach (var kv in _totals)
    {
        if (kv.Key.SubtypeId.Equals(subtypeId, StringComparison.OrdinalIgnoreCase))
            return kv.Key;
    }
    // Fallback: try common type prefixes
    string[] prefixes = new string[]
    {
        "MyObjectBuilder_Component",
        "MyObjectBuilder_Ore",
        "MyObjectBuilder_Ingot",
        "MyObjectBuilder_AmmoMagazine",
        "MyObjectBuilder_PhysicalGunObject"
    };
    for (int i = 0; i < prefixes.Length; i++)
    {
        try
        {
            var t = new MyItemType(prefixes[i], subtypeId);
            return t;
        }
        catch { }
    }
    return default(MyItemType);
}

// ============================================================
// REGION: Stage 2 - Category Sorter
// ============================================================
private void StageSort()
{
    _movesThisCycle = 0;

    for (int i = _srcIdx; i < _allBlocks.Count; i++)
    {
        if (Runtime.CurrentInstructionCount > INST_GUARD) { _srcIdx = i; return; }

        IMyTerminalBlock src = _allBlocks[i];
        if (!src.HasInventory) continue;
        if (_excludedGrids.Contains(src.CubeGrid)) continue;
        if (src is IMyReactor || src is IMyGasGenerator || src is IMyGasTank) continue;
        if (IsLocked(src)) continue;

        IMyInventory srcInv = src.GetInventory(0);
        if (srcInv == null) continue;

        _itemBuf.Clear();
        srcInv.GetItems(_itemBuf);

        for (int m = _itemBuf.Count - 1; m >= 0; m--)
        {
            if (Runtime.CurrentInstructionCount > INST_GUARD) { _srcIdx = i; return; }

            MyInventoryItem item = _itemBuf[m];
            MyItemType t = item.Type;
            string cat = GetCategory(t);
            if (cat.Length == 0) continue;

            // Find destination: [Stock] first, then category container
            IMyInventory dst = FindStockDest(t, cat, srcInv);
            if (dst == null) dst = FindCategoryDest(cat, srcInv);
            if (dst == null) continue;
            if (dst == srcInv) continue;

            try
            {
                if (srcInv.TransferItemTo(dst, m, null, true))
                    _movesThisCycle++;
            }
            catch { }
        }
    }

    _srcIdx = 0;
    _stage++;
    _status = "sorted " + _movesThisCycle + " moves";
}

private IMyInventory FindStockDest(MyItemType t, string cat, IMyInventory srcInv)
{
    for (int i = 0; i < _stockContainers.Count; i++)
    {
        StockContainer sc = _stockContainers[i];
        if (sc.Inv == srcInv) continue;
        if (!IsInventoryAvailable(sc.Inv)) continue;
        if (sc.CategoryFilter.Length > 0 &&
            !sc.CategoryFilter.Equals(cat, StringComparison.OrdinalIgnoreCase)) continue;

        for (int j = 0; j < sc.Items.Count; j++)
        {
            StockItem si = sc.Items[j];
            if (!si.SubtypeId.Equals(t.SubtypeId, StringComparison.OrdinalIgnoreCase)) continue;
            if (si.Mode == "Disabled") return null;
            if (si.Mode == "Max") return null;  // limiter: don't pull items in
            // Check quota not already met
            if (si.Mode == "Target" || si.Mode == "Min")
            {
                double current = (double)sc.Inv.GetItemAmount(t);
                if (current >= si.Amount && si.Mode == "Target") continue;
            }
            return sc.Inv;
        }
    }
    return null;
}

private IMyInventory FindCategoryDest(string cat, IMyInventory srcInv)
{
    for (int i = 0; i < _catContainers.Count; i++)
    {
        CategoryContainer cc = _catContainers[i];
        if (cc.Inv == srcInv) continue;
        if (cc.Locked) continue;
        if (!cc.Category.Equals(cat, StringComparison.OrdinalIgnoreCase)) continue;
        if (!IsInventoryAvailable(cc.Inv)) continue;
        return cc.Inv;
    }
    return null;
}


// ============================================================
// REGION: Basic Component Set (for BasicAssembler routing)
// ============================================================
private static readonly string[] BASIC_COMPONENTS = new string[]
{
    "SteelPlate","InteriorPlate","Construction","SmallTube",
    "LargeTube","Motor","Display","BulletproofGlass","Girder"
};
private static bool IsBasicComponent(string subtypeId)
{
    for (int i = 0; i < BASIC_COMPONENTS.Length; i++)
        if (BASIC_COMPONENTS[i].Equals(subtypeId, StringComparison.OrdinalIgnoreCase))
            return true;
    return false;
}

// ============================================================
// REGION: Stage 3 - Autocraft Manager
// ============================================================
private void StageAutocraft()
{
    if (!_acEnabled) { _stage++; return; }

    // Rebuild target assembler lists if empty (first run)
    List<IMyAssembler> fullAsms  = _agmAssemblers;
    List<IMyAssembler> basicAsms = _agmBasicAssemblers;

    // Fallback: if no tagged assemblers, use all assemblers as full
    if (fullAsms.Count == 0 && basicAsms.Count == 0)
    {
        fullAsms = _assemblers;
    }

    // Clear all queues first
    for (int i = 0; i < fullAsms.Count; i++)
    {
        try { fullAsms[i].ClearQueue(); } catch { }
    }
    for (int i = 0; i < basicAsms.Count; i++)
    {
        try { basicAsms[i].ClearQueue(); } catch { }
    }

    // Process each quota item
    var keys = new List<string>(_acQuotas.Keys);
    for (int qi = 0; qi < keys.Count; qi++)
    {
        if (Runtime.CurrentInstructionCount > INST_GUARD) { _stage++; return; }

        string subtypeId = keys[qi];
        double quota = _acQuotas[subtypeId];
        double stock = GetStockForSubtype(subtypeId);

        // Assemble: only if stock < quota * (1 - margin/100)
        double assembleThreshold = quota * (1.0 - _acAssembleMargin / 100.0);
        if (stock < assembleThreshold)
        {
            double needed = quota - stock;
            MyDefinitionId bp;
            if (!FindBlueprint(subtypeId, fullAsms, basicAsms, out bp)) continue;

            // Route to correct assembler list
            List<IMyAssembler> targets = (IsBasicComponent(subtypeId) && basicAsms.Count > 0)
                ? basicAsms : fullAsms;
            if (targets.Count == 0) continue;

            // Split evenly across targets
            double perAsm = Math.Ceiling(needed / targets.Count);
            MyFixedPoint fp = (MyFixedPoint)perAsm;
            if (fp < 1) fp = 1;

            for (int i = 0; i < targets.Count; i++)
            {
                try { targets[i].AddQueueItem(bp, fp); }
                catch { }
            }
        }

        // Disassemble: only if stock > quota * 1.10
        if (_acAutoDisassemble && stock > quota * (1.0 + _acDisassembleMargin / 100.0))
        {
            double excess = stock - quota;
            MyDefinitionId bp;
            if (!FindBlueprint(subtypeId, fullAsms, basicAsms, out bp)) continue;

            // Only disassemble on full assemblers
            if (fullAsms.Count == 0) continue;
            MyFixedPoint fp = (MyFixedPoint)Math.Ceiling(excess / fullAsms.Count);
            if (fp < 1) fp = 1;

            for (int i = 0; i < fullAsms.Count; i++)
            {
                try
                {
                    fullAsms[i].Mode = MyAssemblerMode.Disassembly;
                    fullAsms[i].AddQueueItem(bp, fp);
                    fullAsms[i].Mode = MyAssemblerMode.Assembly;
                }
                catch { }
            }
        }
    }
    _stage++;
    _status = "autocraft ok";
}

// Find blueprint ID for a SubtypeId, checking both assembler lists
private bool FindBlueprint(string subtypeId, List<IMyAssembler> full,
    List<IMyAssembler> basic, out MyDefinitionId bp)
{
    bp = default(MyDefinitionId);
    if (_bpCache.TryGetValue(subtypeId, out bp)) return true;

    // Try standard patterns
    string[] patterns = new string[]
    {
        subtypeId,
        subtypeId + "Component",
        subtypeId + "Magazine",
        subtypeId + "Item",
        "MyObjectBuilder_BlueprintDefinition/" + subtypeId,
        "MyObjectBuilder_BlueprintDefinition/" + subtypeId + "Component"
    };

    // Check against all available assemblers
    var allAsms = new List<IMyAssembler>();
    allAsms.AddRange(full);
    allAsms.AddRange(basic);
    if (allAsms.Count == 0) allAsms.AddRange(_assemblers);

    for (int pi = 0; pi < patterns.Length; pi++)
    {
        MyDefinitionId candidate;
        if (!MyDefinitionId.TryParse("MyObjectBuilder_BlueprintDefinition", patterns[pi], out candidate))
            continue;

        for (int ai = 0; ai < allAsms.Count; ai++)
        {
            try
            {
                if (allAsms[ai].CanUseBlueprint(candidate))
                {
                    _bpCache[subtypeId] = candidate;
                    bp = candidate;
                    return true;
                }
            }
            catch { }
        }
    }

    Echo("AGM autocraft: no blueprint for " + subtypeId);
    return false;
}

// ============================================================
// REGION: Stage 4 - LCD Draw
// ============================================================
private void StageLcd()
{
    for (int i = _lcdIdx; i < _lcds.Count; i++)
    {
        if (Runtime.CurrentInstructionCount > INST_GUARD) { _lcdIdx = i; return; }
        LcdEntry e = _lcds[i];
        if (e.Surface == null) continue;
        try { DispatchDash(e.Surface, e.Command, e.Page); }
        catch { }
    }
    _lcdIdx = 0;
    _stage = 0;   // full cycle complete, restart
    _status = "running";
}

private void DispatchDash(IMyTextSurface s, string cmd, int page)
{
    string c = NormCmd(cmd);
    if      (c == "coredashboard")        DrawCoreDash(s);
    else if (c == "logisticsdashboard")   DrawLogisticsDash(s);
    else if (c == "oredashboard")         DrawCategoryDash(s,"Ore",page);
    else if (c == "ingotdashboard")       DrawCategoryDash(s,"Ingot",page);
    else if (c == "componentdashboard")   DrawCategoryDash(s,"Component",page);
    else if (c == "ammodashboard")      DrawCategoryDash(s,"Ammo",page);
    else if (c == "tooldashboard")        DrawCategoryDash(s,"Tool",page);
    else if (c == "bottledashboard")      DrawCategoryDash(s,"Bottle",page);
    else if (c == "inventorydashboard")   DrawCategoryDash(s,"",page);
    else if (c == "autocraftdashboard")   DrawAutocraftDash(s,page);
    else if (c == "productiondashboard")  DrawProductionDash(s,page);
}

private string NormCmd(string s)
{
    if (s == null) return "";
    _sb.Clear();
    for (int i = 0; i < s.Length; i++)
    {
        char ch = s[i];
        if (ch == ' ' || ch == '_' || ch == '-') continue;
        _sb.Append(char.ToLowerInvariant(ch));
    }
    return _sb.ToString();
}

// ============================================================
// REGION: DrawKit - Sprite Helpers
// ============================================================
private void PrepSurf(IMyTextSurface s)
{
    if (s == null) return;
    try
    {
        s.ContentType = VRage.Game.GUI.TextPanel.ContentType.SCRIPT;
        s.Script = "";
        s.ScriptBackgroundColor = COL_BG;
        s.BackgroundColor       = COL_BG;
    }
    catch { }
}

private RectangleF VP(IMyTextSurface s)
{
    if (s == null) return new RectangleF(0,0,512,512);
    Vector2 ss = s.SurfaceSize;
    if (ss.X < 1f || ss.Y < 1f) ss = new Vector2(512f,512f);
    return new RectangleF((s.TextureSize - ss) * 0.5f, ss);
}

private RectangleF Inset(RectangleF r, float a)
    => new RectangleF(r.X+a, r.Y+a, r.Width-a*2f, r.Height-a*2f);

private void Fill(MySpriteDrawFrame fr, RectangleF r, Color c)
    => fr.Add(new MySprite(SpriteType.TEXTURE,"SquareSimple",r.Position+r.Size*0.5f,r.Size,c));

private void Border(MySpriteDrawFrame fr, RectangleF r, Color c, float t)
{
    Fill(fr, new RectangleF(r.X,       r.Y,        r.Width, t),     c);
    Fill(fr, new RectangleF(r.X,       r.Bottom-t, r.Width, t),     c);
    Fill(fr, new RectangleF(r.X,       r.Y,        t,       r.Height), c);
    Fill(fr, new RectangleF(r.Right-t, r.Y,        t,       r.Height), c);
}

private void Txt(MySpriteDrawFrame fr, string text, float x, float y,
                 Color c, float sc, TextAlignment al)
    => fr.Add(new MySprite(SpriteType.TEXT, text ?? "", new Vector2(x,y),
               null, c, "Monospace", al, sc));

private void FitTxt(MySpriteDrawFrame fr, string text, float x, float y,
                    Color c, float sc, TextAlignment al, float maxW)
{
    if (text == null) text = "";
    float s2 = sc;
    if (text.Length > 0)
    {
        float need = text.Length * 19f * s2;
        if (need > maxW) s2 = Math.Max(0.24f, s2 * maxW / need);
    }
    Txt(fr, text, x, y, c, s2, al);
}

private void Row(MySpriteDrawFrame fr, RectangleF panel, float y,
                 string label, string value, Color vc)
{
    var row = new RectangleF(panel.X+16f, y, panel.Width-32f, 26f);
    Fill(fr, row, COL_PANEL2);
    Border(fr, row, COL_DIM, 1f);
    Txt(fr, label, row.X+10f,    row.Y+4f, COL_ROW_TEXT, 0.46f, TextAlignment.LEFT);
    FitTxt(fr, value, row.Right-10f, row.Y+4f, vc,        0.46f, TextAlignment.RIGHT, row.Width-150f);
}

private void ProgBar(MySpriteDrawFrame fr, RectangleF r, double pct, Color fill)
{
    Fill(fr, r, COL_PROG_BG);
    float fw = r.Width * (float)Math.Min(1.0, Math.Max(0.0, pct));
    if (fw > 0f) Fill(fr, new RectangleF(r.X, r.Y, fw, r.Height), fill);
    Border(fr, r, COL_DIM, 1f);
}

private void Icon(MySpriteDrawFrame fr, string spriteId, float x, float y, float size, Color c)
{
    string id = string.IsNullOrEmpty(spriteId) ? "IconInventory" : spriteId;
    try { fr.Add(new MySprite(SpriteType.TEXTURE, id, new Vector2(x,y), new Vector2(size,size), c)); }
    catch { fr.Add(new MySprite(SpriteType.TEXTURE, "IconInventory", new Vector2(x,y), new Vector2(size,size), c)); }
}

// ============================================================
// REGION: PB Front Screen (always drawn)
// ============================================================
private void DrawBootScreen()
{
    // Draw on PB surface 0
    IMyTextSurfaceProvider prov = Me as IMyTextSurfaceProvider;
    if (prov != null && prov.SurfaceCount > 0)
    {
        IMyTextSurface s = prov.GetSurface(0);
        if (s != null) DrawBootSurface(s);
    }

    // Draw on all registered LCD surfaces
    for (int i = 0; i < _lcds.Count; i++)
    {
        if (_lcds[i].Surface != null)
            try { DrawBootSurface(_lcds[i].Surface); } catch { }
    }
}

private void DrawBootSurface(IMyTextSurface s)
{
    PrepSurf(s);
    RectangleF vp = VP(s);
    RectangleF panel = Inset(vp, 12f);
    using (MySpriteDrawFrame fr = s.DrawFrame())
    {
        Fill(fr, vp, COL_BG);
        Border(fr, vp, COL_ACCENT, 4f);
        float cx = panel.X + panel.Width * 0.5f;

        // Logo -- hex circuit icon
        float logoSize = Math.Min(panel.Width, panel.Height) * 0.28f;
        float logoY = panel.Y + panel.Height * 0.22f;
        Icon(fr, "MyObjectBuilder_Component/Computer", cx, logoY, logoSize, COL_ACCENT);

        // Title
        Txt(fr, "AUTOGRID MANAGER", cx, panel.Y + panel.Height * 0.42f,
            COL_ACCENT2, 0.72f, TextAlignment.CENTER);

        // Loading bar
        float bw = panel.Width * 0.65f;
        float bh = 10f;
        float bx = cx - bw * 0.5f;
        float by = panel.Y + panel.Height * 0.58f;
        var bar = new RectangleF(bx, by, bw, bh);
        Fill(fr, bar, COL_PROG_BG);
        Fill(fr, new RectangleF(bx, by, bw * _bootPct, bh),
            _bootPct >= 1.0f ? COL_OK : COL_PROG_FILL);
        Border(fr, bar, COL_ACCENT, 1f);

        // Percentage
        string pctStr = ((int)(_bootPct * 100f)).ToString() + "%";
        Txt(fr, pctStr, cx, by + bh + 8f, COL_DIM, 0.36f, TextAlignment.CENTER);

        // Author + version
        Txt(fr, "RevGamer", cx, panel.Bottom - 36f, COL_TEXT, 0.40f, TextAlignment.CENTER);
        Txt(fr, "v" + VERSION, cx, panel.Bottom - 18f, COL_DIM, 0.32f, TextAlignment.CENTER);
    }
}

private void DrawPbScreen()
{
    IMyTextSurfaceProvider prov = Me as IMyTextSurfaceProvider;
    if (prov == null || prov.SurfaceCount == 0) return;
    IMyTextSurface s = prov.GetSurface(0);
    if (s == null) return;
    PrepSurf(s);
    RectangleF vp = VP(s);
    RectangleF panel = Inset(vp, 8f);
    using (MySpriteDrawFrame fr = s.DrawFrame())
    {
        Fill(fr, vp, COL_BG);
        Fill(fr, panel, COL_PANEL);
        Border(fr, vp, COL_ACCENT, 4f);
        float cx = panel.X + panel.Width * 0.5f;

        // Logo small
        float logoSize = Math.Min(panel.Width, panel.Height) * 0.18f;
        Icon(fr, "MyObjectBuilder_Component/Computer", cx, panel.Y+panel.Height*0.20f, logoSize, COL_ACCENT);

        Txt(fr,"AUTOGRID MANAGER",cx,panel.Y+panel.Height*0.34f,COL_ACCENT2,0.52f,TextAlignment.CENTER);
        Txt(fr,_status.ToUpperInvariant(),cx,panel.Y+panel.Height*0.48f,
            _paused ? COL_WARN : COL_OK, 0.40f, TextAlignment.CENTER);
        Txt(fr,"Stage "+_stage+"  Tick "+_tick,cx,panel.Y+panel.Height*0.59f,COL_DIM,0.30f,TextAlignment.CENTER);
        Txt(fr,"Moves: "+_movesThisCycle,cx,panel.Y+panel.Height*0.67f,COL_TEXT,0.30f,TextAlignment.CENTER);
        if (_lastErr.Length > 0)
            FitTxt(fr,"ERR: "+_lastErr,cx,panel.Y+panel.Height*0.76f,COL_BAD,0.28f,TextAlignment.CENTER,panel.Width-20f);
        Txt(fr,"RevGamer",cx,panel.Bottom-32f,COL_DIM,0.32f,TextAlignment.CENTER);
        Txt(fr,"v"+VERSION,cx,panel.Bottom-14f,COL_DIM,0.28f,TextAlignment.CENTER);
    }
}

// ============================================================
// REGION: Dashboard - Core
// ============================================================
private void DrawCoreDash(IMyTextSurface s)
{
    PrepSurf(s); RectangleF vp=VP(s); RectangleF panel=Inset(vp,10f);
    using (MySpriteDrawFrame fr = s.DrawFrame())
    {
        Fill(fr,vp,COL_BG); Fill(fr,panel,COL_PANEL); Border(fr,vp,COL_ACCENT,6f);
        Txt(fr,"AUTOGRID MANAGER",panel.X+24f,panel.Y+20f,COL_ACCENT2,0.80f,TextAlignment.LEFT);
        Txt(fr,"v"+VERSION,panel.Right-24f,panel.Y+24f,COL_DIM,0.38f,TextAlignment.RIGHT);
        float y = panel.Y+64f;
        int nextScan = RESCAN_TICKS - (_tick % RESCAN_TICKS);
        string scanStr = (nextScan / 60).ToString() + "s";
        Row(fr,panel,y,"Grid",Me.CubeGrid.CustomName,COL_TEXT);            y+=32f;
        Row(fr,panel,y,"Status",_status.ToUpperInvariant(),COL_OK);        y+=32f;
        Row(fr,panel,y,"Moves",""+_movesThisCycle,COL_TEXT);               y+=32f;
        Row(fr,panel,y,"Item types",""+_totals.Count,COL_TEXT);            y+=32f;
        Row(fr,panel,y,"Cat containers",""+_catContainers.Count,COL_TEXT); y+=32f;
        Row(fr,panel,y,"Stock containers",""+_stockContainers.Count,COL_TEXT); y+=32f;
        int asmTagged = _agmAssemblers.Count + _agmBasicAssemblers.Count;
        string asmStr = _assemblers.Count + " (" + asmTagged + " tagged)";
        Row(fr,panel,y,"Assemblers",asmStr,asmTagged>0?COL_OK:COL_WARN);   y+=32f;
        Row(fr,panel,y,"Next scan",scanStr,COL_DIM);
        Txt(fr,"AutoGrid Manager v"+VERSION,panel.X+24f,panel.Bottom-22f,COL_DIM,0.32f,TextAlignment.LEFT);
    }
}

// ============================================================
// REGION: Dashboard - Logistics
// ============================================================
private void DrawLogisticsDash(IMyTextSurface s)
{
    PrepSurf(s); RectangleF vp=VP(s); RectangleF panel=Inset(vp,10f);
    using (MySpriteDrawFrame fr = s.DrawFrame())
    {
        Fill(fr,vp,COL_BG); Fill(fr,panel,COL_PANEL); Border(fr,vp,COL_ACCENT,6f);
        Txt(fr,"LOGISTICS",panel.X+24f,panel.Y+20f,COL_ACCENT2,0.80f,TextAlignment.LEFT);
        float y = panel.Y+64f;
        string[] cats = new string[]{"Ore","Ingot","Component","Ammo","Tool","Bottle","Food","Seed","Ingredient"};
        for (int i = 0; i < cats.Length; i++)
        {
            string cat = cats[i];
            double cur=0, max2=0;
            for (int j=0; j<_catContainers.Count; j++)
            {
                if (!_catContainers[j].Category.Equals(cat,StringComparison.OrdinalIgnoreCase)) continue;
                cur  += (double)_catContainers[j].Inv.CurrentVolume;
                max2 += (double)_catContainers[j].Inv.MaxVolume;
            }
            if (max2 <= 0) continue;
            double pct = cur / max2;
            Color vc = pct > 0.97 ? COL_BAD : pct > 0.85 ? COL_WARN : COL_OK;
            var row = new RectangleF(panel.X+16f, y, panel.Width-32f, 26f);
            Fill(fr,row,COL_PANEL2); Border(fr,row,COL_DIM,1f);
            Txt(fr,cat,row.X+10f,row.Y+4f,COL_ROW_TEXT,0.46f,TextAlignment.LEFT);
            Txt(fr,Pct(pct),row.Right-10f,row.Y+4f,vc,0.46f,TextAlignment.RIGHT);
            var bar = new RectangleF(row.Right-90f,row.Y+14f,80f,6f);
            ProgBar(fr,bar,pct,pct>0.85?COL_WARN:COL_PROG_FILL);
            y += 30f;
        }
        Txt(fr,"AutoGrid Manager v"+VERSION,panel.X+24f,panel.Bottom-22f,COL_DIM,0.32f,TextAlignment.LEFT);
    }
}

// ============================================================
// REGION: Dashboard - Category Stock
// ============================================================
private void DrawCategoryDash(IMyTextSurface s, string cat, int page)
{
    PrepSurf(s); RectangleF vp=VP(s); RectangleF panel=Inset(vp,10f);

    // Build filtered list
    var list = new List<KeyValuePair<MyItemType,double>>();
    foreach (var kv in _totals)
    {
        string c = GetCategory(kv.Key);
        if (cat.Length == 0 || c.Equals(cat, StringComparison.OrdinalIgnoreCase))
            list.Add(kv);
    }
    list.Sort((a,b2) => GetDisplayName(a.Key).CompareTo(GetDisplayName(b2.Key)));

    using (MySpriteDrawFrame fr = s.DrawFrame())
    {
        Fill(fr,vp,COL_BG); Fill(fr,panel,COL_PANEL); Border(fr,vp,COL_ACCENT,6f);
        string title = (cat.Length==0?"INVENTORY":cat.ToUpperInvariant()) + " STOCK";
        int rowH = 32;
        int rows = Math.Max(1,(int)((panel.Height-110f)/(float)rowH));
        int pages = Math.Max(1,(int)Math.Ceiling(list.Count/(double)rows));
        if (page<1) page=1; if (page>pages) page=pages;
        int start=(page-1)*rows, end=Math.Min(list.Count,start+rows);

        Txt(fr,title,panel.X+24f,panel.Y+20f,COL_ACCENT2,0.78f,TextAlignment.LEFT);
        Txt(fr,"P"+page+"/"+pages,panel.Right-24f,panel.Y+24f,COL_DIM,0.38f,TextAlignment.RIGHT);

        float y = panel.Y + 64f;
        for (int i = start; i < end; i++)
        {
            MyItemType t = list[i].Key;
            double amt   = list[i].Value;
            var row = new RectangleF(panel.X+16f, y, panel.Width-32f, 26f);
            Fill(fr,row,COL_PANEL2); Border(fr,row,COL_DIM,1f);
            Icon(fr, GetIcon(t), row.X+13f, row.Y+13f, 20f, COL_ROW_TEXT);
            FitTxt(fr,GetDisplayName(t),row.X+28f,row.Y+4f,COL_ROW_TEXT,0.42f,TextAlignment.LEFT,row.Width-140f);
            Txt(fr,FmtAmt(amt),row.Right-10f,row.Y+4f,COL_ROW_TEXT,0.42f,TextAlignment.RIGHT);
            y += (float)rowH;
        }
        Txt(fr,"AutoGrid Manager v"+VERSION,panel.X+24f,panel.Bottom-22f,COL_DIM,0.32f,TextAlignment.LEFT);
    }
}

// ============================================================
// REGION: Dashboard - Autocrafting
// ============================================================
private void DrawAutocraftDash(IMyTextSurface s, int page)
{
    PrepSurf(s); RectangleF vp=VP(s); RectangleF panel=Inset(vp,10f);
    var keys = new List<string>(_acQuotas.Keys);
    keys.Sort();

    using (MySpriteDrawFrame fr = s.DrawFrame())
    {
        Fill(fr,vp,COL_BG); Fill(fr,panel,COL_PANEL); Border(fr,vp,COL_ACCENT,6f);
        int rowH = 32;
        int rows = Math.Max(1,(int)((panel.Height-110f)/(float)rowH));
        int pages = Math.Max(1,(int)Math.Ceiling(keys.Count/(double)rows));
        if (page<1) page=1; if (page>pages) page=pages;
        int start=(page-1)*rows, end=Math.Min(keys.Count,start+rows);

        Txt(fr,"AUTOCRAFTING",panel.X+24f,panel.Y+20f,COL_ACCENT2,0.80f,TextAlignment.LEFT);
        Txt(fr,"P"+page+"/"+pages,panel.Right-24f,panel.Y+24f,COL_DIM,0.38f,TextAlignment.RIGHT);
        Txt(fr,_acEnabled?"ONLINE":"OFFLINE",panel.Right-24f,panel.Y+40f,
            _acEnabled?COL_OK:COL_DIM,0.36f,TextAlignment.RIGHT);

        float y = panel.Y + 64f;
        for (int i = start; i < end; i++)
        {
            string name = keys[i];
            double quota = _acQuotas[name];
            // Look up stock via totals - try Component type first
            double stock = GetStockForSubtype(name);
            double pct = quota > 0 ? Math.Min(1.0, stock/quota) : 0.0;
            Color bc = pct >= 0.5 ? COL_PROG_FILL : COL_WARN;

            var row = new RectangleF(panel.X+16f, y, panel.Width-32f, 26f);
            Fill(fr,row,COL_PANEL2); Border(fr,row,COL_DIM,1f);
            FitTxt(fr,SplitCamel(name),row.X+10f,row.Y+4f,COL_ROW_TEXT,0.42f,TextAlignment.LEFT,row.Width-160f);
            Txt(fr,FmtAmt(stock)+"/"+FmtAmt(quota),row.Right-10f,row.Y+4f,COL_ROW_TEXT,0.38f,TextAlignment.RIGHT);
            var bar = new RectangleF(row.X+8f, row.Bottom-5f, row.Width-16f, 3f);
            ProgBar(fr, bar, pct, bc);
            y += (float)rowH;
        }
        Txt(fr,"AutoGrid Manager v"+VERSION,panel.X+24f,panel.Bottom-22f,COL_DIM,0.32f,TextAlignment.LEFT);
    }
}

// ============================================================
// REGION: Dashboard - Production
// ============================================================
private void DrawProductionDash(IMyTextSurface s, int page)
{
    PrepSurf(s); RectangleF vp=VP(s); RectangleF panel=Inset(vp,10f);
    using (MySpriteDrawFrame fr = s.DrawFrame())
    {
        Fill(fr,vp,COL_BG); Fill(fr,panel,COL_PANEL); Border(fr,vp,COL_ACCENT,6f);
        Txt(fr,"PRODUCTION",panel.X+24f,panel.Y+20f,COL_ACCENT2,0.80f,TextAlignment.LEFT);
        float y = panel.Y+64f;

        if (page == 2)
        {
            // Assembler details
            int rows=Math.Max(1,(int)((panel.Height-90f)/30f));
            for (int i=0; i<_assemblers.Count && i<rows; i++)
            {
                IMyAssembler a = _assemblers[i];
                string job = a.IsProducing ? AsmCurrentJob(a) : "IDLE";
                Row(fr,panel,y,TrimName(a.CustomName,18),job,a.IsProducing?COL_OK:COL_DIM);
                y+=30f;
            }
            if (_assemblers.Count==0) Row(fr,panel,y,"Assemblers","NONE",COL_DIM);
        }
        else if (page == 3)
        {
            // Refinery details
            int rows=Math.Max(1,(int)((panel.Height-90f)/30f));
            for (int i=0; i<_refineries.Count && i<rows; i++)
            {
                IMyRefinery r = _refineries[i];
                string ore = r.IsProducing ? RefCurrentOre(r) : "IDLE";
                Row(fr,panel,y,TrimName(r.CustomName,18),ore,r.IsProducing?COL_OK:COL_DIM);
                y+=30f;
            }
            if (_refineries.Count==0) Row(fr,panel,y,"Refineries","NONE",COL_DIM);
        }
        else
        {
            // Overview page 1
            int asmProd=0; for(int i=0;i<_assemblers.Count;i++) if(_assemblers[i].IsProducing) asmProd++;
            int refProd=0; for(int i=0;i<_refineries.Count;i++) if(_refineries[i].IsProducing) refProd++;
            Row(fr,panel,y,"Assemblers",asmProd+"/"+_assemblers.Count+" producing",COL_ACCENT2);y+=32f;
            Row(fr,panel,y,"Refineries",refProd+"/"+_refineries.Count+" producing",COL_ACCENT2);y+=32f;
            Row(fr,panel,y,"Autocrafting",_acEnabled?"ONLINE":"OFFLINE",_acEnabled?COL_OK:COL_DIM);y+=32f;
            Row(fr,panel,y,"Quotas",""+_acQuotas.Count+" items",COL_TEXT);
        }
        Txt(fr,"AutoGrid Manager v"+VERSION,panel.X+24f,panel.Bottom-22f,COL_DIM,0.32f,TextAlignment.LEFT);
    }
}

// ============================================================
// REGION: ItemCatalog Helpers
// ============================================================
private string GetCategory(MyItemType t)
{
    string id = t.TypeId.ToString();
    if (id.EndsWith("_Ore"))               return "Ore";
    if (id.EndsWith("_Ingot"))             return "Ingot";
    if (id.EndsWith("_Component"))         return "Component";
    if (id.EndsWith("_AmmoMagazine"))      return "Ammo";
    if (id.EndsWith("_PhysicalGunObject")) return "Tool";
    if (id.EndsWith("_GasContainerObject") ||
        id.EndsWith("_OxygenContainerObject")) return "Bottle";
    if (id.EndsWith("_SeedItem"))          return "Seed";
    if (id.EndsWith("_ConsumableItem") ||
        id.EndsWith("_Consumable"))        return "Food";
    return "";
}

private string GetIcon(MyItemType t)
{
    string id  = t.TypeId.ToString();
    string sub = t.SubtypeId.ToString();
    if (id.EndsWith("_Ore"))               return "MyObjectBuilder_Ore/"+sub;
    if (id.EndsWith("_Ingot"))             return "MyObjectBuilder_Ingot/"+sub;
    if (id.EndsWith("_Component"))         return "MyObjectBuilder_Component/"+sub;
    if (id.EndsWith("_AmmoMagazine"))      return "MyObjectBuilder_AmmoMagazine/"+sub;
    if (id.EndsWith("_PhysicalGunObject")) return "MyObjectBuilder_PhysicalGunObject/"+sub;
    if (id.EndsWith("_GasContainerObject"))return "MyObjectBuilder_GasContainerObject/"+sub;
    if (id.EndsWith("_OxygenContainerObject")) return "MyObjectBuilder_OxygenContainerObject/"+sub;
    if (id.EndsWith("_SeedItem"))          return "MyObjectBuilder_SeedItem/"+sub;
    if (id.EndsWith("_ConsumableItem") ||
        id.EndsWith("_Consumable"))        return "MyObjectBuilder_ConsumableItem/"+sub;
    return "IconInventory";
}

private string GetDisplayName(MyItemType t)
{
    string id  = t.TypeId.ToString();
    string sub = t.SubtypeId.ToString();
    if (sub == "Stone" && id.EndsWith("_Ingot")) return "Gravel";
    if (id.EndsWith("_Ore"))
    {
        if (sub=="Stone") return "Stone";
        if (sub=="Scrap") return "Scrap";
        if (sub=="Ice")   return "Ice";
        return SplitCamel(sub) + " Ore";
    }
    if (id.EndsWith("_SeedItem")) return SplitCamel(sub) + " Seeds";
    return SplitCamel(sub);
}

private string SplitCamel(string n)
{
    if (string.IsNullOrEmpty(n)) return "";
    if (n.StartsWith("MealPack_")) n = n.Substring(9);
    if (n.EndsWith("Item")) n = n.Substring(0, n.Length-4);
    _sb.Clear();
    for (int i=0; i<n.Length; i++)
    {
        char c = n[i];
        bool nd = char.IsDigit(c);
        bool pd = i > 0 && char.IsDigit(n[i-1]);
        if (i > 0 && ((char.IsUpper(c) && char.IsLower(n[i-1])) || (nd && !pd) || (!nd && pd)))
            _sb.Append(' ');
        _sb.Append(c);
    }
    return _sb.ToString().Trim();
}

private double GetStockForSubtype(string subtypeId)
{
    double total = 0;
    foreach (var kv in _totals)
    {
        if (kv.Key.SubtypeId.Equals(subtypeId, StringComparison.OrdinalIgnoreCase))
            total += kv.Value;
    }
    return total;
}

// ============================================================
// REGION: Production Helpers
// ============================================================
private string AsmCurrentJob(IMyAssembler a)
{
    _queue.Clear();
    a.GetQueue(_queue);
    if (_queue.Count == 0) return "IDLE";
    return SplitCamel(_queue[0].BlueprintId.SubtypeName);
}
private readonly List<MyProductionItem> _queue = new List<MyProductionItem>();

private string RefCurrentOre(IMyRefinery r)
{
    _itemBuf.Clear();
    r.GetInventory(0).GetItems(_itemBuf);
    if (_itemBuf.Count == 0) return "IDLE";
    return GetDisplayName(_itemBuf[0].Type);
}

// ============================================================
// REGION: Utility Helpers
// ============================================================
private bool IsLocked(IMyTerminalBlock b)
{
    string cd = b.CustomData ?? "";
    if (!cd.Contains(AGM_SEC)) return false;
    return ParseBool(ReadAgmKey(cd, "locked"), false);
}

private bool IsInventoryAvailable(IMyInventory inv)
{
    if (inv == null) return false;
    return (double)inv.CurrentVolume < (double)inv.MaxVolume * 0.97;
}

private string ReadAgmKey(string cd, string keyName)
{
    bool inAgm = false;
    string[] lines = cd.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string raw = lines[i];
        int ci = raw.IndexOf(';');
        string line = (ci >= 0 ? raw.Substring(0, ci) : raw).Trim();
        if (line.Length == 0) continue;
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            string sec = line.Substring(1, line.Length-2).Trim();
            inAgm = sec.Equals("AGM", StringComparison.OrdinalIgnoreCase);
            continue;
        }
        if (!inAgm) continue;
        string key, val;
        if (!TrySplit(line, '=', out key, out val)) continue;
        if (key.Equals(keyName, StringComparison.OrdinalIgnoreCase)) return val.Trim();
    }
    return "";
}

private bool TrySplit(string line, char sep, out string key, out string val)
{
    key = ""; val = "";
    int i = line.IndexOf(sep);
    if (i < 0) return false;
    key = line.Substring(0, i).Trim();
    val = line.Substring(i+1).Trim();
    return key.Length > 0;
}

private bool ParseBool(string v, bool fb)
{
    if (v == null) return fb;
    v = v.Trim().ToLowerInvariant();
    if (v=="true"||v=="yes"||v=="on"||v=="1") return true;
    if (v=="false"||v=="no"||v=="off"||v=="0") return false;
    return fb;
}

private string Pct(double r)
    => (r*100.0).ToString("0.0")+"%";

private string FmtAmt(double v)
{
    if (v >= 1e9) return (v/1e9).ToString("0.##")+"B";
    if (v >= 1e6) return (v/1e6).ToString("0.##")+"M";
    if (v >= 1e3) return (v/1e3).ToString("0.##")+"K";
    return v.ToString("0.##");
}

private string TrimName(string s, int max)
{
    if (s == null) return "";
    return s.Length <= max ? s : s.Substring(0, max-1)+"~";
}