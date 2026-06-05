// Horizon Sentinel - Phase 3 foundation
// Ship safety, automation, and systems management controller for Space Engineers.
// Screen tags are placed in LCD/control seat CustomData, for example:
// [HS:Pilot]
// Surface=0

const string ScriptName = "Horizon Sentinel";
const string Version = "0.3.0";

const string LcdNameTag = "{HSLCD}";
const string CornerLcdNameTag = "{HSCLCD}";

const string TagSplash = "[HS:Splash]";
const string TagPilot = "[HS:Pilot]";
const string TagJump = "[HS:Jump]";
const string TagCargo = "[HS:Cargo]";
const string TagDescent = "[HS:Descent]";
const string TagCombat = "[HS:Combat]";
const string TagDamage = "[HS:Damage]";
const string TagPressure = "[HS:Pressure]";
const string TagThrusters = "[HS:Thrusters]";
const string TagBattery = "[HS:Battery]";
const string TagHydrogen = "[HS:Hydrogen]";
const string TagOxygen = "[HS:Oxygen]";
const string TagAmmo = "[HS:Ammo]";

const string DefaultGroupBatteries = "HS Batteries";
const string DefaultGroupJump = "HS Jump Drives";
const string DefaultGroupHydrogen = "HS Hydrogen Tanks";
const string DefaultGroupOxygen = "HS Oxygen Tanks";
const string DefaultGroupTurrets = "HS Turrets";
const string DefaultGroupCargo = "HS Cargo";
const string DefaultGroupVents = "HS Air Vents";
const string DefaultGroupThrusters = "HS Thrusters";
const string DefaultGroupLift = "HS Lifting Thrusters";
const string DefaultGroupCruise = "HS Cruising Thrusters";
const string DefaultGroupBrake = "HS Braking Thrusters";
const string DefaultGroupGyros = "HS Gyros";
const string DefaultGroupReactors = "HS Reactors";
const string DefaultGroupSolar = "HS Solar Panels";
const string DefaultGroupHydrogenEngines = "HS Hydrogen Engines";

string GroupBatteries = DefaultGroupBatteries;
string GroupJump = DefaultGroupJump;
string GroupHydrogen = DefaultGroupHydrogen;
string GroupOxygen = DefaultGroupOxygen;
string GroupTurrets = DefaultGroupTurrets;
string GroupCargo = DefaultGroupCargo;
string GroupVents = DefaultGroupVents;
string GroupThrusters = DefaultGroupThrusters;
string GroupLift = DefaultGroupLift;
string GroupCruise = DefaultGroupCruise;
string GroupBrake = DefaultGroupBrake;
string GroupGyros = DefaultGroupGyros;
string GroupReactors = DefaultGroupReactors;
string GroupSolar = DefaultGroupSolar;
string GroupHydrogenEngines = DefaultGroupHydrogenEngines;

Color Bg = new Color(0, 4, 7);
Color Panel = new Color(5, 12, 15);
Color Panel2 = new Color(8, 20, 24);
Color Text = new Color(235, 242, 244);
Color Muted = new Color(118, 160, 174);
Color Accent = new Color(0, 235, 245);
Color Orange = new Color(235, 118, 39);
Color Safe = new Color(75, 210, 126);
Color Caution = new Color(248, 205, 84);
Color Warning = new Color(245, 139, 55);
Color Critical = new Color(255, 56, 47);
Color Offline = new Color(95, 105, 110);

List<ScreenTarget> _screens = new List<ScreenTarget>();
List<IMyTerminalBlock> _tmpBlocks = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> _allBlocks = new List<IMyTerminalBlock>();
List<IMyGasTank> _tmpTanks = new List<IMyGasTank>();
List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();
List<IMyPowerProducer> _reactors = new List<IMyPowerProducer>();
List<IMyPowerProducer> _solar = new List<IMyPowerProducer>();
List<IMyPowerProducer> _hydrogenEngines = new List<IMyPowerProducer>();
List<IMyGasTank> _hydrogenTanks = new List<IMyGasTank>();
List<IMyGasTank> _oxygenTanks = new List<IMyGasTank>();
List<IMyJumpDrive> _jumpDrives = new List<IMyJumpDrive>();
List<IMyTerminalBlock> _cargoBlocks = new List<IMyTerminalBlock>();
List<IMyLargeTurretBase> _turrets = new List<IMyLargeTurretBase>();
List<IMyAirVent> _vents = new List<IMyAirVent>();
List<IMyThrust> _thrusters = new List<IMyThrust>();
List<IMyThrust> _liftThrusters = new List<IMyThrust>();
List<IMyThrust> _cruiseThrusters = new List<IMyThrust>();
List<IMyThrust> _brakeThrusters = new List<IMyThrust>();
List<IMyGyro> _gyros = new List<IMyGyro>();
List<MyInventoryItem> _items = new List<MyInventoryItem>();

Metrics _m = new Metrics();
string _page = "auto";
int _tick;
int _scanTick;
int _screenTick;
string _lastCustomData = "";

bool AutoReactorCharging = false;
bool AutoHydrogenBalancing = false;
bool AutoAmmoBalancing = false;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    LoadConfig();
    RefreshScreens();
    RefreshBlocks();
}

public void Main(string argument, UpdateType updateSource)
{
    _tick++;

    if (Me.CustomData != _lastCustomData)
    {
        LoadConfig();
        RefreshScreens();
        RefreshBlocks();
    }

    if (++_screenTick >= 30 || argument.Equals("refresh", StringComparison.OrdinalIgnoreCase))
    {
        RefreshScreens();
        _screenTick = 0;
    }

    if (++_scanTick >= 10 || argument.Equals("refresh", StringComparison.OrdinalIgnoreCase))
    {
        RefreshBlocks();
        _scanTick = 0;
    }

    HandleCommand(argument);
    ScanMetrics();
    DrawAllScreens();

    Echo(ScriptName + " v" + Version);
    Echo("Screens: " + _screens.Count);
    Echo("Mass: " + FormatKg(_m.TotalMassKg));
    Echo("State: " + _m.MasterState);
}

void LoadConfig()
{
    _lastCustomData = Me.CustomData;
    AutoReactorCharging = GetBool("Auto Reactor Charging", false);
    AutoHydrogenBalancing = GetBool("Auto Hydrogen Balancing", false);
    AutoAmmoBalancing = GetBool("Auto Ammo Balancing", false);

    GroupBatteries = GetString("Group Batteries", DefaultGroupBatteries);
    GroupReactors = GetString("Group Reactors", DefaultGroupReactors);
    GroupSolar = GetString("Group Solar Panels", DefaultGroupSolar);
    GroupHydrogenEngines = GetString("Group Hydrogen Engines", DefaultGroupHydrogenEngines);
    GroupHydrogen = GetString("Group Hydrogen Tanks", DefaultGroupHydrogen);
    GroupOxygen = GetString("Group Oxygen Tanks", DefaultGroupOxygen);
    GroupJump = GetString("Group Jump Drives", DefaultGroupJump);
    GroupTurrets = GetString("Group Turrets", DefaultGroupTurrets);
    GroupCargo = GetString("Group Cargo", DefaultGroupCargo);
    GroupVents = GetString("Group Air Vents", DefaultGroupVents);
    GroupThrusters = GetString("Group Thrusters", DefaultGroupThrusters);
    GroupLift = GetString("Group Lifting Thrusters", DefaultGroupLift);
    GroupCruise = GetString("Group Cruising Thrusters", DefaultGroupCruise);
    GroupBrake = GetString("Group Braking Thrusters", DefaultGroupBrake);
    GroupGyros = GetString("Group Gyros", DefaultGroupGyros);
}

string GetString(string key, string fallback)
{
    string[] lines = Me.CustomData.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string line = lines[i].Trim();
        int p = line.IndexOf('=');
        if (p < 0) continue;
        string k = line.Substring(0, p).Trim();
        if (!k.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
        string v = line.Substring(p + 1).Trim();
        return v == "" ? fallback : v;
    }
    return fallback;
}

bool GetBool(string key, bool fallback)
{
    string[] lines = Me.CustomData.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string line = lines[i].Trim();
        int p = line.IndexOf('=');
        if (p < 0) continue;
        string k = line.Substring(0, p).Trim();
        if (!k.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
        string v = line.Substring(p + 1).Trim();
        return v.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("on", StringComparison.OrdinalIgnoreCase);
    }
    return fallback;
}

void HandleCommand(string argument)
{
    if (string.IsNullOrWhiteSpace(argument)) return;
    string a = argument.Trim().ToLower();
    if (a == "logo" || a == "splash") _page = "splash";
    else if (a == "pilot" || a == "overview") _page = "pilot";
    else if (a == "jump") _page = "jump";
    else if (a == "cargo") _page = "cargo";
    else if (a == "descent" || a == "takeoff") _page = "descent";
    else if (a == "combat" || a == "turrets") _page = "combat";
    else if (a == "damage") _page = "damage";
    else if (a == "pressure") _page = "pressure";
    else if (a == "thrusters") _page = "thrusters";
    else if (a == "battery" || a == "batteries") _page = "battery";
    else if (a == "hydrogen" || a == "h2") _page = "hydrogen";
    else if (a == "oxygen" || a == "o2") _page = "oxygen";
    else if (a == "ammo") _page = "ammo";
    else if (a == "auto") _page = "auto";
    else if (a == "abort") _m.FlightMode = "MANUAL";
}

void RefreshScreens()
{
    _screens.Clear();

    IMyTextSurfaceProvider meProvider = Me as IMyTextSurfaceProvider;
    if (meProvider != null && meProvider.SurfaceCount > 0)
        _screens.Add(new ScreenTarget("PB", "splash", meProvider.GetSurface(0), true));

    List<IMyTextSurfaceProvider> providers = new List<IMyTextSurfaceProvider>();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(providers);

    for (int i = 0; i < providers.Count; i++)
    {
        IMyTerminalBlock block = providers[i] as IMyTerminalBlock;
        if (block == null || block.CubeGrid != Me.CubeGrid) continue;
        if (!IsHorizonScreen(block)) continue;
        ParseScreenBlock(block, providers[i]);
    }

    List<IMyTextPanel> panels = new List<IMyTextPanel>();
    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, p => p.CubeGrid == Me.CubeGrid);
    for (int i = 0; i < panels.Count; i++)
    {
        if (!IsHorizonScreen(panels[i])) continue;
        if (panels[i] is IMyTextSurfaceProvider) continue;
        ParseTextPanel(panels[i]);
    }
}

bool IsHorizonScreen(IMyTerminalBlock block)
{
    return block.CustomName.Contains(LcdNameTag) || block.CustomName.Contains(CornerLcdNameTag);
}

bool IsCornerScreen(IMyTerminalBlock block)
{
    return block.CustomName.Contains(CornerLcdNameTag);
}

void ParseScreenBlock(IMyTerminalBlock block, IMyTextSurfaceProvider provider)
{
    string[] lines = block.CustomData.Split('\n');
    string page = "";
    int surface = 0;
    bool hasPage = false;

    for (int i = 0; i < lines.Length; i++)
    {
        string line = lines[i].Trim();
        if (line.StartsWith("[HS:", StringComparison.OrdinalIgnoreCase))
        {
            if (hasPage) AddScreen(block, provider, page, surface);
            page = PageFromTag(line);
            surface = 0;
            hasPage = page != "";
        }
        else if (hasPage && line.StartsWith("Surface=", StringComparison.OrdinalIgnoreCase))
        {
            int.TryParse(line.Substring(8).Trim(), out surface);
        }
    }

    if (hasPage) AddScreen(block, provider, page, surface);
}

void ParseTextPanel(IMyTextPanel panel)
{
    string[] lines = panel.CustomData.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        string page = PageFromTag(lines[i].Trim());
        if (page != "")
        {
            _screens.Add(new ScreenTarget(panel.CustomName, page, panel as IMyTextSurface, IsCornerScreen(panel)));
            return;
        }
    }
}

string PageFromTag(string tag)
{
    if (tag.Equals(TagSplash, StringComparison.OrdinalIgnoreCase)) return "splash";
    if (tag.Equals(TagPilot, StringComparison.OrdinalIgnoreCase)) return "pilot";
    if (tag.Equals(TagJump, StringComparison.OrdinalIgnoreCase)) return "jump";
    if (tag.Equals(TagCargo, StringComparison.OrdinalIgnoreCase)) return "cargo";
    if (tag.Equals(TagDescent, StringComparison.OrdinalIgnoreCase)) return "descent";
    if (tag.Equals(TagCombat, StringComparison.OrdinalIgnoreCase)) return "combat";
    if (tag.Equals(TagDamage, StringComparison.OrdinalIgnoreCase)) return "damage";
    if (tag.Equals(TagPressure, StringComparison.OrdinalIgnoreCase)) return "pressure";
    if (tag.Equals(TagThrusters, StringComparison.OrdinalIgnoreCase)) return "thrusters";
    if (tag.Equals(TagBattery, StringComparison.OrdinalIgnoreCase)) return "battery";
    if (tag.Equals(TagHydrogen, StringComparison.OrdinalIgnoreCase)) return "hydrogen";
    if (tag.Equals(TagOxygen, StringComparison.OrdinalIgnoreCase)) return "oxygen";
    if (tag.Equals(TagAmmo, StringComparison.OrdinalIgnoreCase)) return "ammo";
    return "";
}

void AddScreen(IMyTerminalBlock block, IMyTextSurfaceProvider provider, string page, int index)
{
    if (provider.SurfaceCount == 0) return;
    if (index < 0 || index >= provider.SurfaceCount) index = 0;
    _screens.Add(new ScreenTarget(block.CustomName, page, provider.GetSurface(index), IsCornerScreen(block)));
}

void RefreshBlocks()
{
    _batteries.Clear();
    _reactors.Clear();
    _solar.Clear();
    _hydrogenEngines.Clear();
    _hydrogenTanks.Clear();
    _oxygenTanks.Clear();
    _jumpDrives.Clear();
    _cargoBlocks.Clear();
    _turrets.Clear();
    _vents.Clear();
    _thrusters.Clear();
    _liftThrusters.Clear();
    _cruiseThrusters.Clear();
    _brakeThrusters.Clear();
    _gyros.Clear();
    _allBlocks.Clear();

    _tmpBlocks.Clear();
    GridTerminalSystem.GetBlocks(_tmpBlocks);
    for (int i = 0; i < _tmpBlocks.Count; i++)
        if (_tmpBlocks[i].CubeGrid == Me.CubeGrid) _allBlocks.Add(_tmpBlocks[i]);
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(_batteries, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(_jumpDrives, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(_turrets, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType<IMyAirVent>(_vents, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(_thrusters, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType<IMyGyro>(_gyros, b => b.CubeGrid == Me.CubeGrid);

    GetGroup(GroupBatteries, _batteries);
    GetGroup(GroupJump, _jumpDrives);
    GetGroup(GroupTurrets, _turrets);
    GetGroup(GroupVents, _vents);
    GetGroup(GroupThrusters, _thrusters);
    GetGroup(GroupLift, _liftThrusters);
    GetGroup(GroupCruise, _cruiseThrusters);
    GetGroup(GroupBrake, _brakeThrusters);
    GetGroup(GroupGyros, _gyros);
    GetPowerGroup(GroupReactors, _reactors);
    GetPowerGroup(GroupSolar, _solar);
    GetPowerGroup(GroupHydrogenEngines, _hydrogenEngines);

    _tmpTanks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyGasTank>(_tmpTanks, b => b.CubeGrid == Me.CubeGrid);
    for (int i = 0; i < _tmpTanks.Count; i++)
    {
        IMyGasTank tank = _tmpTanks[i];
        if (tank == null) continue;
        string subtype = tank.BlockDefinition.SubtypeName.ToLower();
        string name = tank.CustomName.ToLower();
        if (subtype.Contains("hydrogen") || name.Contains("hydrogen") || name.Contains(" h2"))
            _hydrogenTanks.Add(tank);
        else if (subtype.Contains("oxygen") || name.Contains("oxygen") || name.Contains(" o2"))
            _oxygenTanks.Add(tank);
    }
    GetGroup(GroupHydrogen, _hydrogenTanks);
    GetGroup(GroupOxygen, _oxygenTanks);

    IMyBlockGroup cargoGroup = GridTerminalSystem.GetBlockGroupWithName(GroupCargo);
    if (cargoGroup != null)
    {
        _tmpBlocks.Clear();
        cargoGroup.GetBlocks(_tmpBlocks);
        for (int i = 0; i < _tmpBlocks.Count; i++)
            if (_tmpBlocks[i].CubeGrid == Me.CubeGrid && _tmpBlocks[i].InventoryCount > 0)
                _cargoBlocks.Add(_tmpBlocks[i]);
    }
    else
    {
        _tmpBlocks.Clear();
        GridTerminalSystem.GetBlocks(_tmpBlocks);
        for (int i = 0; i < _tmpBlocks.Count; i++)
            if (_tmpBlocks[i].CubeGrid == Me.CubeGrid && _tmpBlocks[i].InventoryCount > 0)
                _cargoBlocks.Add(_tmpBlocks[i]);
    }
}

void GetGroup<T>(string name, List<T> list) where T : class, IMyTerminalBlock
{
    IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(name);
    if (group == null) return;
    list.Clear();
    _tmpBlocks.Clear();
    group.GetBlocks(_tmpBlocks);
    for (int i = 0; i < _tmpBlocks.Count; i++)
        if (_tmpBlocks[i].CubeGrid == Me.CubeGrid && _tmpBlocks[i] is T)
            list.Add(_tmpBlocks[i] as T);
}

void GetPowerGroup(string name, List<IMyPowerProducer> list)
{
    list.Clear();
    IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(name);
    if (group == null)
    {
        _tmpBlocks.Clear();
        string key = name.Replace("HS ", "");
        GridTerminalSystem.GetBlocks(_tmpBlocks);
        for (int i = 0; i < _tmpBlocks.Count; i++)
            if (_tmpBlocks[i].CubeGrid == Me.CubeGrid && _tmpBlocks[i].CustomName.Contains(key) && _tmpBlocks[i] is IMyPowerProducer)
                list.Add(_tmpBlocks[i] as IMyPowerProducer);
        return;
    }
    _tmpBlocks.Clear();
    group.GetBlocks(_tmpBlocks);
    for (int i = 0; i < _tmpBlocks.Count; i++)
        if (_tmpBlocks[i].CubeGrid == Me.CubeGrid && _tmpBlocks[i] is IMyPowerProducer)
            list.Add(_tmpBlocks[i] as IMyPowerProducer);
}

void ScanMetrics()
{
    _m = new Metrics();
    _m.FlightMode = "MANUAL";

    IMyShipController controller = FindController();
    if (controller != null)
    {
        MyShipMass mass = controller.CalculateShipMass();
        _m.BaseMassKg = mass.BaseMass;
        _m.TotalMassKg = mass.TotalMass;
        Vector3D gravity = controller.GetNaturalGravity();
        _m.NaturalGravity = (float)gravity.Length();
        _m.GravityPercent = _m.NaturalGravity / 9.81f * 100f;
        _m.VerticalSpeed = _m.NaturalGravity > 0.01f ?
            Vector3D.Dot(controller.GetShipVelocities().LinearVelocity, -Vector3D.Normalize(gravity)) : 0;
    }

    ScanPower();
    ScanTanks();
    ScanJump();
    ScanCargo();
    ScanTurrets();
    ScanPressure();
    ScanThrusters();
    ScanDamage();
    CalculateMasterState();
}

IMyShipController FindController()
{
    List<IMyShipController> controllers = new List<IMyShipController>();
    GridTerminalSystem.GetBlocksOfType<IMyShipController>(controllers, c => c.CubeGrid == Me.CubeGrid);
    for (int i = 0; i < controllers.Count; i++)
        if (controllers[i].IsMainCockpit) return controllers[i];
    return controllers.Count > 0 ? controllers[0] : null;
}

void ScanPower()
{
    double stored = 0;
    double max = 0;
    double input = 0;
    double output = 0;
    int online = 0;

    for (int i = 0; i < _batteries.Count; i++)
    {
        IMyBatteryBlock b = _batteries[i];
        if (b.IsWorking) online++;
        stored += b.CurrentStoredPower;
        max += b.MaxStoredPower;
        input += b.CurrentInput;
        output += b.CurrentOutput;
    }

    _m.BatteryCount = _batteries.Count;
    _m.BatteryOnline = online;
    _m.BatteryPercent = Percent(stored, max);
    _m.PowerInputMw = input;
    _m.PowerOutputMw = output;
    _m.ReactorCount = _reactors.Count;
    _m.SolarCount = _solar.Count;
    _m.HydrogenEngineCount = _hydrogenEngines.Count;
}

void ScanTanks()
{
    _m.FuelPercent = AverageTank(_hydrogenTanks, out _m.HydrogenOnline);
    _m.HydrogenCount = _hydrogenTanks.Count;
    _m.OxygenPercent = AverageTank(_oxygenTanks, out _m.OxygenOnline);
    _m.OxygenCount = _oxygenTanks.Count;
    _m.HydrogenImbalance = TankImbalance(_hydrogenTanks);
}

float AverageTank(List<IMyGasTank> tanks, out int online)
{
    online = 0;
    double total = 0;
    if (tanks.Count == 0) return 0;
    for (int i = 0; i < tanks.Count; i++)
    {
        if (tanks[i].IsWorking) online++;
        total += tanks[i].FilledRatio;
    }
    return (float)(total / tanks.Count * 100.0);
}

float TankImbalance(List<IMyGasTank> tanks)
{
    if (tanks.Count < 2) return 0;
    double min = 1;
    double max = 0;
    for (int i = 0; i < tanks.Count; i++)
    {
        min = Math.Min(min, tanks[i].FilledRatio);
        max = Math.Max(max, tanks[i].FilledRatio);
    }
    return (float)((max - min) * 100.0);
}

void ScanJump()
{
    double stored = 0;
    double max = 0;
    int online = 0;
    int damaged = 0;
    for (int i = 0; i < _jumpDrives.Count; i++)
    {
        IMyJumpDrive j = _jumpDrives[i];
        if (j.IsWorking) online++;
        if (!j.IsFunctional) damaged++;
        stored += j.CurrentStoredPower;
        max += j.MaxStoredPower;
    }
    _m.JumpCount = _jumpDrives.Count;
    _m.JumpOnline = online;
    _m.JumpDamaged = damaged;
    _m.JumpPercent = Percent(stored, max);
    if (_m.JumpCount == 0) _m.JumpState = "NO DRIVES";
    else if (damaged > 0) _m.JumpState = "DAMAGED";
    else if (online < _m.JumpCount) _m.JumpState = "OFFLINE";
    else if (_m.JumpPercent >= 99.5f) _m.JumpState = "READY";
    else _m.JumpState = "CHARGING";
}

void ScanCargo()
{
    double used = 0;
    double max = 0;
    double mass = 0;

    for (int b = 0; b < _cargoBlocks.Count; b++)
    {
        for (int i = 0; i < _cargoBlocks[b].InventoryCount; i++)
        {
            IMyInventory inv = _cargoBlocks[b].GetInventory(i);
            used += (double)inv.CurrentVolume;
            max += (double)inv.MaxVolume;
            mass += (double)inv.CurrentMass;
        }
    }

    _m.CargoBlocks = _cargoBlocks.Count;
    _m.CargoPercent = Percent(used, max);
    _m.CargoMassKg = mass;
    _m.CargoDensityKgPerL = used > 0 ? (float)(mass / (used * 1000.0)) : 0;
}

void ScanTurrets()
{
    int online = 0;
    int damaged = 0;
    double ammoMass = 0;

    for (int i = 0; i < _turrets.Count; i++)
    {
        IMyLargeTurretBase t = _turrets[i];
        if (t.IsWorking) online++;
        if (!t.IsFunctional) damaged++;
        for (int inv = 0; inv < t.InventoryCount; inv++)
            ammoMass += (double)t.GetInventory(inv).CurrentMass;
    }

    _m.TurretCount = _turrets.Count;
    _m.TurretOnline = online;
    _m.TurretDamaged = damaged;
    _m.AmmoMassKg = ammoMass;
}

void ScanPressure()
{
    int online = 0;
    int pressurized = 0;
    double pressure = 0;
    int depressurize = 0;

    for (int i = 0; i < _vents.Count; i++)
    {
        IMyAirVent v = _vents[i];
        if (v.IsWorking) online++;
        if (v.Status == VentStatus.Pressurized) pressurized++;
        if (v.Depressurize) depressurize++;
        pressure += v.GetOxygenLevel();
    }

    _m.VentCount = _vents.Count;
    _m.VentOnline = online;
    _m.PressurizedVents = pressurized;
    _m.DepressurizeVents = depressurize;
    _m.PressurePercent = _vents.Count > 0 ? (float)(pressure / _vents.Count * 100.0) : 0;
}

void ScanThrusters()
{
    _m.ThrusterCount = _thrusters.Count;
    _m.ThrusterOnline = CountWorking(_thrusters);
    _m.LiftCount = _liftThrusters.Count;
    _m.LiftOnline = CountWorking(_liftThrusters);
    _m.CruiseCount = _cruiseThrusters.Count;
    _m.CruiseOnline = CountWorking(_cruiseThrusters);
    _m.BrakeCount = _brakeThrusters.Count;
    _m.BrakeOnline = CountWorking(_brakeThrusters);
    _m.LiftLoadPercent = ThrustLoad(_liftThrusters);
    _m.LiftCapacityPercent = LiftCapacity(_liftThrusters);
    _m.CruiseLoadPercent = ThrustLoad(_cruiseThrusters);
    _m.BrakeLoadPercent = ThrustLoad(_brakeThrusters);
}

int CountWorking<T>(List<T> blocks) where T : class, IMyFunctionalBlock
{
    int count = 0;
    for (int i = 0; i < blocks.Count; i++)
        if (blocks[i].IsWorking) count++;
    return count;
}

float ThrustLoad(List<IMyThrust> thrusters)
{
    double current = 0;
    double max = 0;
    for (int i = 0; i < thrusters.Count; i++)
    {
        current += thrusters[i].CurrentThrust;
        max += thrusters[i].MaxEffectiveThrust;
    }
    return Percent(current, max);
}

float LiftCapacity(List<IMyThrust> thrusters)
{
    if (_m.TotalMassKg <= 0 || _m.NaturalGravity <= 0.01f) return 100;
    double lift = 0;
    for (int i = 0; i < thrusters.Count; i++)
    {
        if (!thrusters[i].IsWorking) continue;
        lift += thrusters[i].MaxEffectiveThrust;
    }

    double weight = _m.TotalMassKg * _m.NaturalGravity;
    return Percent(lift, weight);
}

void ScanDamage()
{
    int nonfunctional = 0;
    for (int i = 0; i < _allBlocks.Count; i++)
    {
        IMyFunctionalBlock f = _allBlocks[i] as IMyFunctionalBlock;
        if (f != null && !f.IsFunctional) nonfunctional++;
    }
    _m.BlockCount = _allBlocks.Count;
    _m.DamagedCount = nonfunctional;
}

void CalculateMasterState()
{
    _m.MasterState = "READY";
    _m.MasterColor = Safe;

    if (_m.DamagedCount > 0 || _m.FuelPercent < 10 || _m.BatteryPercent < 10)
    {
        _m.MasterState = "CRITICAL";
        _m.MasterColor = Critical;
        return;
    }

    if (_m.FuelPercent < 25 || _m.BatteryPercent < 25 || _m.CargoPercent > 85 ||
        _m.JumpState == "DAMAGED" || _m.PressurePercent < 80)
    {
        _m.MasterState = "CAUTION";
        _m.MasterColor = Caution;
    }
}

void DrawAllScreens()
{
    for (int i = 0; i < _screens.Count; i++)
    {
        ScreenTarget s = _screens[i];
        string page = _page == "auto" ? s.Page : _page;
        DrawPage(s.Surface, page, s.Compact);
    }
}

void DrawPage(IMyTextSurface surface, string page, bool compact)
{
    Prepare(surface);
    if (compact || page == "battery" || page == "hydrogen" || page == "oxygen" || page == "ammo")
    {
        DrawCompactPage(surface, page);
        return;
    }

    if (page == "splash") DrawSplash(surface);
    else if (page == "pilot") DrawPilot(surface, compact);
    else if (page == "jump") DrawJump(surface);
    else if (page == "cargo") DrawCargo(surface);
    else if (page == "descent") DrawDescent(surface);
    else if (page == "combat") DrawCombat(surface, compact);
    else if (page == "damage") DrawDamage(surface);
    else if (page == "pressure") DrawPressure(surface);
    else if (page == "thrusters") DrawThrusters(surface);
    else DrawPilot(surface, compact);
}

void Prepare(IMyTextSurface surface)
{
    surface.ContentType = ContentType.SCRIPT;
    surface.Script = "";
    surface.ScriptBackgroundColor = Bg;
}

void DrawCompactPage(IMyTextSurface surface, string page)
{
    if (page == "splash")
    {
        DrawSplash(surface);
        return;
    }

    View v = GetView(surface);
    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBackground(frame, v);

    string title = "STATUS";
    string label = "SYS";
    string value = "--";
    string footer = "";
    float percent = 0;
    Color color = Muted;

    if (page == "jump")
    {
        title = "JUMP";
        label = "JUMP";
        value = FormatPct(_m.JumpPercent);
        footer = _m.JumpState + "  " + _m.JumpOnline + "/" + _m.JumpCount;
        percent = _m.JumpPercent;
        color = StateColor(_m.JumpState);
    }
    else if (page == "combat" || page == "ammo")
    {
        title = "AMMO";
        label = "AMMO";
        value = FormatKg(_m.AmmoMassKg);
        footer = _m.TurretOnline + "/" + _m.TurretCount + " turrets";
        percent = Math.Min((float)_m.AmmoMassKg / 1000f * 100f, 100f);
        color = _m.TurretDamaged > 0 ? Critical : Caution;
    }
    else if (page == "battery" || page == "pilot")
    {
        title = "BATTERY";
        label = "PWR";
        value = FormatPct(_m.BatteryPercent);
        footer = _batteries.Count + " batteries";
        if (_m.ReactorCount + _m.SolarCount + _m.HydrogenEngineCount > 0)
            footer = "R " + _m.ReactorCount + "  S " + _m.SolarCount + "  H2E " + _m.HydrogenEngineCount;
        percent = _m.BatteryPercent;
        color = StatusColor(_m.BatteryPercent, false);
    }
    else if (page == "hydrogen")
    {
        title = "HYDROGEN";
        label = "H2";
        value = FormatPct(_m.FuelPercent);
        footer = _hydrogenTanks.Count + " tanks  Imb " + _m.HydrogenImbalance.ToString("0") + "%";
        percent = _m.FuelPercent;
        color = StatusColor(_m.FuelPercent, false);
    }
    else if (page == "oxygen")
    {
        title = "OXYGEN";
        label = "O2";
        value = FormatPct(_m.OxygenPercent);
        footer = _oxygenTanks.Count + " tanks";
        percent = _m.OxygenPercent;
        color = StatusColor(_m.OxygenPercent, false);
    }
    else if (page == "pressure")
    {
        title = "AIR";
        label = "AIR";
        value = FormatPct(_m.PressurePercent);
        footer = PressureState();
        percent = _m.PressurePercent;
        color = StatusColor(_m.PressurePercent, false);
    }
    else if (page == "cargo")
    {
        title = "CARGO";
        label = "LOAD";
        value = FormatPct(_m.CargoPercent);
        footer = FormatKg(_m.CargoMassKg);
        percent = _m.CargoPercent;
        color = StatusColor(100 - _m.CargoPercent, false);
    }
    else if (page == "thrusters")
    {
        title = "THRUST";
        label = "LIFT";
        value = _m.LiftOnline + "/" + _m.LiftCount;
        footer = _m.ThrusterOnline + "/" + _m.ThrusterCount + " all";
        percent = Percent(_m.LiftOnline, Math.Max(_m.LiftCount, 1));
        color = _m.LiftCount == 0 ? Warning : Safe;
    }
    else if (page == "damage")
    {
        title = "DAMAGE";
        label = "DMG";
        value = _m.DamagedCount.ToString();
        footer = _m.DamagedCount == 0 ? "NO DAMAGE" : "REPAIR";
        percent = _m.DamagedCount == 0 ? 100 : 10;
        color = _m.DamagedCount == 0 ? Safe : Critical;
    }

    DrawCompactStatusBar(frame, v, title, label, value, footer, percent, color);
    frame.Dispose();
}

void DrawPilot(IMyTextSurface surface, bool compact)
{
    View v = GetView(surface);
    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBackground(frame, v);
    DrawHeader(frame, v, "PILOT STATUS", _m.MasterState, _m.MasterColor);
    DrawTile(frame, v, 0, 0, 3, 2, "PWR", FormatPct(_m.BatteryPercent), _m.BatteryPercent, StatusColor(_m.BatteryPercent, false), "IconEnergy");
    DrawTile(frame, v, 1, 0, 3, 2, "H2", FormatPct(_m.FuelPercent), _m.FuelPercent, StatusColor(_m.FuelPercent, false), "IconHydrogen");
    DrawTile(frame, v, 2, 0, 3, 2, "O2", FormatPct(_m.OxygenPercent), _m.OxygenPercent, StatusColor(_m.OxygenPercent, false), "IconOxygen");
    DrawTile(frame, v, 0, 1, 3, 2, "GRAV", _m.GravityPercent.ToString("0") + "%", Math.Min(_m.GravityPercent, 100), _m.GravityPercent > 10 ? Caution : Safe, "AH_BoreSight");
    DrawTile(frame, v, 1, 1, 3, 2, "MASS", FormatKg(_m.TotalMassKg), Math.Min((float)(_m.TotalMassKg / 1000000.0 * 100.0), 100f), Muted, "SquareSimple");
    DrawTile(frame, v, 2, 1, 3, 2, "JUMP", FormatPct(_m.JumpPercent), _m.JumpPercent, StateColor(_m.JumpState), "Arrow");
    frame.Dispose();
}

void DrawJump(IMyTextSurface surface)
{
    View v = GetView(surface);
    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBackground(frame, v);
    DrawHeader(frame, v, "JUMP DRIVES", _m.JumpState, StateColor(_m.JumpState));
    DrawHalfGauge(frame, v.Center + new Vector2(0, v.Unit * 0.10f), v.Unit * 0.64f, _m.JumpPercent, StateColor(_m.JumpState));
    DrawText(frame, FormatPct(_m.JumpPercent), v.Center + new Vector2(0, v.Unit * 0.10f),
        v.Unit * 0.0021f, Text, TextAlignment.CENTER);
    DrawSegmentBar(frame, new Vector2(v.Center.X, v.Top + v.H * 0.69f), v.W * 0.70f, v.Unit * 0.045f, _m.JumpPercent, StateColor(_m.JumpState), 16);
    DrawText(frame, "Online " + _m.JumpOnline + "/" + _m.JumpCount + "  Damaged " + _m.JumpDamaged,
        new Vector2(v.Center.X, v.Bottom - v.Unit * 0.105f), v.Unit * 0.00115f, Muted, TextAlignment.CENTER);
    frame.Dispose();
}

void DrawCargo(IMyTextSurface surface)
{
    View v = GetView(surface);
    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBackground(frame, v);
    DrawHeader(frame, v, "CARGO STATUS", CargoState(), StatusColor(100 - _m.CargoPercent, false));
    DrawTile(frame, v, 0, 0, 2, 1, "FILL", FormatPct(_m.CargoPercent), _m.CargoPercent, StatusColor(100 - _m.CargoPercent, false), "SquareHollow");
    DrawTile(frame, v, 1, 0, 2, 1, "DENS", _m.CargoDensityKgPerL.ToString("0.0"), Math.Min(_m.CargoDensityKgPerL / 21.28f * 100f, 100f), Warning, "SquareSimple");
    DrawText(frame, "Mass " + FormatKg(_m.CargoMassKg) + "  Blocks " + _m.CargoBlocks,
        new Vector2(v.Center.X, v.Bottom - v.Unit * 0.070f), v.Unit * 0.0012f, Muted, TextAlignment.CENTER);
    frame.Dispose();
}

void DrawDescent(IMyTextSurface surface)
{
    View v = GetView(surface);
    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBackground(frame, v);
    string state = PlanetEntryState();
    DrawHeader(frame, v, "PLANET ENTRY", state, StateColor(state));
    DrawTile(frame, v, 0, 0, 4, 1, "GRAV", _m.GravityPercent.ToString("0") + "%", Math.Min(_m.GravityPercent, 100), Caution, "AH_BoreSight");
    DrawTile(frame, v, 1, 0, 4, 1, "FUEL", FormatPct(_m.FuelPercent), _m.FuelPercent, StatusColor(_m.FuelPercent, false), "IconHydrogen");
    DrawTile(frame, v, 2, 0, 4, 1, "LIFT", FormatPct(_m.LiftCapacityPercent), Math.Min(_m.LiftCapacityPercent, 100), LiftStatusColor(), "Triangle");
    DrawTile(frame, v, 3, 0, 4, 1, "LOAD", FormatPct(_m.CargoPercent), _m.CargoPercent, StatusColor(100 - _m.CargoPercent, false), "SquareHollow");
    DrawText(frame, "Mass " + FormatKg(_m.TotalMassKg) + "  Vertical " + _m.VerticalSpeed.ToString("0.0") + " m/s  Lift " + _m.LiftOnline + "/" + _m.LiftCount,
        new Vector2(v.Center.X, v.Bottom - v.Unit * 0.12f), v.Unit * 0.00115f, Muted, TextAlignment.CENTER);
    frame.Dispose();
}

void DrawCombat(IMyTextSurface surface, bool compact)
{
    View v = GetView(surface);
    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBackground(frame, v);
    DrawHeader(frame, v, "COMBAT", TurretState(), _m.TurretDamaged > 0 ? Critical : Safe);
    DrawTile(frame, v, 0, 0, 2, 1, "TURRET", _m.TurretOnline + "/" + _m.TurretCount, Percent(_m.TurretOnline, Math.Max(_m.TurretCount, 1)), _m.TurretDamaged > 0 ? Critical : Safe, "AH_BoreSight");
    DrawTile(frame, v, 1, 0, 2, 1, "AMMO", FormatKg(_m.AmmoMassKg), Math.Min((float)_m.AmmoMassKg / 1000f * 100f, 100f), Caution, "Triangle");
    DrawText(frame, "Ammo balancing: " + (AutoAmmoBalancing ? "ON" : "OFF"),
        new Vector2(v.Center.X, v.Bottom - v.Unit * 0.13f), v.Unit * 0.00115f, Muted, TextAlignment.CENTER);
    frame.Dispose();
}

void DrawDamage(IMyTextSurface surface)
{
    View v = GetView(surface);
    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBackground(frame, v);
    DrawHeader(frame, v, "DAMAGE / LEAK", _m.DamagedCount == 0 ? "NO DAMAGE" : "REPAIR", _m.DamagedCount == 0 ? Safe : Critical);
    float y = v.Top + v.H * 0.28f;
    DrawStatusLine(frame, v, "Damaged blocks", _m.DamagedCount + "/" + _m.BlockCount, y, _m.DamagedCount == 0 ? Safe : Critical);
    DrawStatusLine(frame, v, "Pressure", FormatPct(_m.PressurePercent), y + v.Unit * 0.12f, StatusColor(_m.PressurePercent, false));
    DrawStatusLine(frame, v, "Depressurize vents", _m.DepressurizeVents.ToString(), y + v.Unit * 0.24f, _m.DepressurizeVents == 0 ? Safe : Warning);
    frame.Dispose();
}

void DrawPressure(IMyTextSurface surface)
{
    View v = GetView(surface);
    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBackground(frame, v);
    DrawHeader(frame, v, "PRESSURE", PressureState(), StatusColor(_m.PressurePercent, false));
    DrawTile(frame, v, 0, 0, 1, 1, "AIR", FormatPct(_m.PressurePercent), _m.PressurePercent, StatusColor(_m.PressurePercent, false), "IconOxygen");
    DrawText(frame, "Vents " + _m.PressurizedVents + "/" + _m.VentCount + " pressurized",
        new Vector2(v.Center.X, v.Bottom - v.Unit * 0.16f), v.Unit * 0.00115f, Muted, TextAlignment.CENTER);
    frame.Dispose();
}

void DrawThrusters(IMyTextSurface surface)
{
    View v = GetView(surface);
    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBackground(frame, v);
    DrawHeader(frame, v, "THRUSTERS", "GROUPS", Safe);
    DrawTile(frame, v, 0, 0, 3, 1, "LIFT", _m.LiftOnline + "/" + _m.LiftCount, Percent(_m.LiftOnline, Math.Max(_m.LiftCount, 1)), _m.LiftCount == 0 ? Warning : Safe, "Triangle");
    DrawTile(frame, v, 1, 0, 3, 1, "CRUISE", _m.CruiseOnline + "/" + _m.CruiseCount, Percent(_m.CruiseOnline, Math.Max(_m.CruiseCount, 1)), _m.CruiseCount == 0 ? Warning : Safe, "Arrow");
    DrawTile(frame, v, 2, 0, 3, 1, "BRAKE", _m.BrakeOnline + "/" + _m.BrakeCount, Percent(_m.BrakeOnline, Math.Max(_m.BrakeCount, 1)), _m.BrakeCount == 0 ? Warning : Safe, "No Entry");
    DrawText(frame, "All thrusters " + _m.ThrusterOnline + "/" + _m.ThrusterCount,
        new Vector2(v.Center.X, v.Bottom - v.Unit * 0.12f), v.Unit * 0.00115f, Muted, TextAlignment.CENTER);
    frame.Dispose();
}

void DrawSplash(IMyTextSurface surface)
{
    View v = GetView(surface);
    float unit = v.Unit;
    float skyBottom = v.Top + v.H * 0.38f;
    float dawnBottom = v.Top + v.H * 0.50f;
    float oceanBottom = v.Top + v.H * 0.74f;
    float horizonY = dawnBottom;
    float pulse = 0.35f + 0.65f * (float)((Math.Sin(_tick * 0.22) + 1.0) * 0.5);
    Color red = new Color(255, 36, 28, (byte)(70 + 185 * pulse));

    MySpriteDrawFrame frame = surface.DrawFrame();
    DrawBox(frame, v.Center, v.Size, new Color(3, 13, 31));
    DrawBox(frame, new Vector2(v.Center.X, v.Top + v.H * 0.24f), new Vector2(v.W, v.H * 0.34f), new Color(20, 32, 55));
    DrawBox(frame, new Vector2(v.Center.X, skyBottom - v.H * 0.035f), new Vector2(v.W, v.H * 0.17f), new Color(88, 48, 50));
    DrawBox(frame, new Vector2(v.Center.X, skyBottom + (dawnBottom - skyBottom) * 0.5f), new Vector2(v.W, dawnBottom - skyBottom), new Color(224, 98, 37));
    DrawCircle(frame, new Vector2(v.Center.X, dawnBottom), unit * 0.25f, new Color(255, 177, 48));
    DrawCircle(frame, new Vector2(v.Center.X, dawnBottom + unit * 0.01f), unit * 0.15f, new Color(255, 226, 125));
    DrawBox(frame, new Vector2(v.Center.X, dawnBottom + (oceanBottom - dawnBottom) * 0.5f), new Vector2(v.W, oceanBottom - dawnBottom), new Color(9, 32, 60));
    DrawBox(frame, new Vector2(v.Center.X, oceanBottom + (v.Bottom - oceanBottom) * 0.5f), new Vector2(v.W, v.Bottom - oceanBottom), new Color(3, 17, 36));
    DrawLine(frame, new Vector2(v.Left, horizonY), new Vector2(v.Right, horizonY), unit * 0.007f, new Color(255, 172, 51));
    Vector2 logoCenter = new Vector2(v.Center.X, v.Top + v.H * 0.39f);
    DrawShield(frame, logoCenter + new Vector2(0, unit * 0.005f), unit * 0.56f, unit * 0.030f, new Color(48, 83, 96));
    DrawShield(frame, logoCenter, unit * 0.56f, unit * 0.020f, Text);
    DrawCrosshair(frame, logoCenter + new Vector2(0, unit * 0.005f), unit * 0.20f, unit * 0.012f, Text, red, pulse);
    DrawText(frame, ScriptName, new Vector2(v.Center.X, v.Bottom - unit * 0.255f), unit * 0.00215f, Text, TextAlignment.CENTER);
    DrawTitleOrnament(frame, v.Center.X, v.Bottom - unit * 0.185f, unit * 0.72f, unit * 0.010f, Orange);
    frame.Dispose();
}

void DrawBackground(MySpriteDrawFrame frame, View v)
{
    DrawBox(frame, v.Center, v.Size, Bg);
    DrawBox(frame, v.Center + new Vector2(0, v.H * 0.18f), new Vector2(v.W, v.H * 0.64f), new Color(2, 13, 18));
    DrawBox(frame, v.Center + new Vector2(0, v.H * 0.18f), new Vector2(v.W, v.H * 0.16f), Panel2);
    DrawLine(frame, new Vector2(v.Left + v.Unit * 0.08f, v.Top + v.Unit * 0.13f),
        new Vector2(v.Right - v.Unit * 0.08f, v.Top + v.Unit * 0.13f), v.Unit * 0.006f, Orange);
    DrawRectOutline(frame, v.Center, v.Size - new Vector2(v.Unit * 0.03f, v.Unit * 0.03f), v.Unit * 0.004f, new Color(0, 95, 105));
}

void DrawHeader(MySpriteDrawFrame frame, View v, string title, string state, Color stateColor)
{
    DrawText(frame, title, new Vector2(v.Left + v.Unit * 0.08f, v.Top + v.Unit * 0.078f), v.Unit * 0.00110f, Text, TextAlignment.LEFT);
    DrawText(frame, state, new Vector2(v.Right - v.Unit * 0.08f, v.Top + v.Unit * 0.078f), v.Unit * 0.00110f, stateColor, TextAlignment.RIGHT);
}

void DrawTile(MySpriteDrawFrame frame, View v, int col, int row, int cols, int rows, string label, string value, float percent, Color color, string icon)
{
    float margin = v.Unit * 0.075f;
    float gap = v.Unit * 0.035f;
    float areaTop = v.Top + v.H * 0.205f;
    float areaH = v.H * 0.62f;
    float cellW = (v.W - margin * 2f - gap * (cols - 1)) / cols;
    float cellH = (areaH - gap * (rows - 1)) / rows;
    float maxH = rows == 1 ? v.Unit * 0.34f : v.Unit * 0.30f;
    cellH = Math.Min(cellH, maxH);
    Vector2 size = new Vector2(cellW, cellH);
    Vector2 center = new Vector2(v.Left + margin + col * (cellW + gap) + cellW * 0.5f,
        areaTop + row * (cellH + gap) + cellH * 0.5f);

    percent = Clamp(percent, 0, 100);
    DrawBox(frame, center, size, new Color(0, 6, 8));
    DrawRectOutline(frame, center, size, v.Unit * 0.009f, Accent);
    DrawCornerTicks(frame, center, size, v.Unit * 0.020f, v.Unit * 0.006f, Text);

    float iconSize = Math.Min(size.X, size.Y) * 0.38f;
    DrawFsdIcon(frame, center + new Vector2(0, -size.Y * 0.18f), iconSize, label, icon, color);

    DrawText(frame, value, center + new Vector2(0, size.Y * 0.10f), v.Unit * 0.00135f, color, TextAlignment.CENTER);
    DrawText(frame, label, center + new Vector2(0, size.Y * 0.31f), v.Unit * 0.00082f, Text, TextAlignment.CENTER);
    DrawSegmentBar(frame, center + new Vector2(0, size.Y * 0.43f), size.X * 0.72f, size.Y * 0.060f, percent, color, 8);
}

void DrawCompactStatusBar(MySpriteDrawFrame frame, View v, string title, string label, string value, string footer, float percent, Color color)
{
    percent = Clamp(percent, 0, 100);
    float pad = v.H * 0.14f;
    float titleScale = Math.Min(v.H * 0.00036f, v.W * 0.00016f);
    float valueScale = Math.Min(v.H * 0.00044f, v.W * 0.00018f);
    float footerScale = Math.Min(v.H * 0.00028f, v.W * 0.00013f);

    DrawBox(frame, v.Center, v.Size, Bg);
    DrawBox(frame, v.Center, new Vector2(v.W - pad * 0.75f, v.H - pad * 0.75f), new Color(0, 8, 11));
    DrawRectOutline(frame, v.Center, new Vector2(v.W - pad * 0.65f, v.H - pad * 0.65f), Math.Max(2f, v.H * 0.025f), Accent);

    float left = v.Left + pad;
    float right = v.Right - pad;
    float topY = v.Top + v.H * 0.27f;
    DrawText(frame, title, new Vector2(left, topY), titleScale, Text, TextAlignment.LEFT);
    DrawText(frame, value, new Vector2(right, topY), valueScale, color, TextAlignment.RIGHT);
    DrawLine(frame, new Vector2(left, v.Top + v.H * 0.39f), new Vector2(right, v.Top + v.H * 0.39f), Math.Max(2f, v.H * 0.018f), Orange);

    float barY = v.Top + v.H * 0.58f;
    float barW = v.W - pad * 2f;
    float barH = Math.Max(5f, v.H * 0.16f);
    DrawBox(frame, new Vector2(v.Center.X, barY), new Vector2(barW, barH), new Color(14, 43, 49));
    DrawBox(frame, new Vector2(left + barW * percent / 100f * 0.5f, barY), new Vector2(barW * percent / 100f, barH), color);
    DrawRectOutline(frame, new Vector2(v.Center.X, barY), new Vector2(barW, barH), Math.Max(1.5f, v.H * 0.010f), Muted);
    DrawSegmentTicks(frame, new Vector2(v.Center.X, barY), barW, barH, 12, new Color(0, 175, 190));

    DrawText(frame, label, new Vector2(left, v.Bottom - v.H * 0.20f), footerScale, Muted, TextAlignment.LEFT);
    DrawText(frame, footer, new Vector2(right, v.Bottom - v.H * 0.20f), footerScale, Muted, TextAlignment.RIGHT);
}

void DrawSegmentTicks(MySpriteDrawFrame frame, Vector2 center, float width, float height, int segments, Color color)
{
    float left = center.X - width * 0.5f;
    for (int i = 1; i < segments; i++)
    {
        float x = left + width * i / segments;
        DrawLine(frame, new Vector2(x, center.Y - height * 0.48f), new Vector2(x, center.Y + height * 0.48f), Math.Max(1f, height * 0.06f), color);
    }
}

void DrawSegmentBar(MySpriteDrawFrame frame, Vector2 center, float width, float height, float percent, Color color, int segments)
{
    percent = Clamp(percent, 0, 100);
    float gap = Math.Max(1f, height * 0.28f);
    float segW = (width - gap * (segments - 1)) / segments;
    float left = center.X - width * 0.5f;
    int lit = (int)Math.Ceiling(percent / 100f * segments);
    for (int i = 0; i < segments; i++)
    {
        Color c = i < lit ? color : new Color(18, 48, 54);
        DrawBox(frame, new Vector2(left + i * (segW + gap) + segW * 0.5f, center.Y), new Vector2(segW, height), c);
    }
}

void DrawFsdIcon(MySpriteDrawFrame frame, Vector2 center, float size, string label, string icon, Color color)
{
    float t = size * 0.075f;
    DrawRectOutline(frame, center, new Vector2(size, size), t, color);
    string l = label.ToUpper();
    if (l == "PWR")
    {
        DrawLine(frame, center + new Vector2(size * 0.08f, -size * 0.34f), center + new Vector2(-size * 0.12f, -size * 0.02f), t * 1.4f, color);
        DrawLine(frame, center + new Vector2(-size * 0.12f, -size * 0.02f), center + new Vector2(size * 0.12f, -size * 0.02f), t * 1.4f, color);
        DrawLine(frame, center + new Vector2(size * 0.12f, -size * 0.02f), center + new Vector2(-size * 0.06f, size * 0.34f), t * 1.4f, color);
    }
    else if (l == "GRAV" || l == "TURRET")
    {
        DrawCircleOutline(frame, center, size * 0.52f, t, color);
        DrawLine(frame, center + new Vector2(-size * 0.32f, 0), center + new Vector2(size * 0.32f, 0), t, color);
        DrawLine(frame, center + new Vector2(0, -size * 0.32f), center + new Vector2(0, size * 0.32f), t, color);
    }
    else if (l == "LOAD" || l == "FILL" || l == "MASS")
    {
        DrawBox(frame, center + new Vector2(0, size * 0.10f), new Vector2(size * 0.44f, size * 0.36f), color);
        DrawBox(frame, center + new Vector2(0, size * -0.18f), new Vector2(size * 0.30f, size * 0.09f), color);
    }
    else if (l == "LIFT" || l == "AMMO")
    {
        DrawTriangle(frame, center, new Vector2(size * 0.48f, size * 0.55f), 0f, color);
    }
    else if (l == "CRUISE" || l == "JUMP")
    {
        DrawLine(frame, center + new Vector2(-size * 0.30f, 0), center + new Vector2(size * 0.25f, 0), t * 1.5f, color);
        DrawTriangle(frame, center + new Vector2(size * 0.23f, 0), new Vector2(size * 0.24f, size * 0.24f), (float)Math.PI * 0.5f, color);
    }
    else if (l == "BRAKE")
    {
        DrawLine(frame, center + new Vector2(-size * 0.25f, -size * 0.25f), center + new Vector2(size * 0.25f, size * 0.25f), t * 1.4f, color);
        DrawLine(frame, center + new Vector2(size * 0.25f, -size * 0.25f), center + new Vector2(-size * 0.25f, size * 0.25f), t * 1.4f, color);
    }
    else
    {
        DrawText(frame, l.Length > 2 ? l.Substring(0, 2) : l, center + new Vector2(0, -size * 0.11f), size * 0.0040f, color, TextAlignment.CENTER);
    }
}

void DrawBar(MySpriteDrawFrame frame, View v, string label, float percent, string value, float y, Color color)
{
    float x = v.Center.X;
    float w = v.W * 0.78f;
    float h = v.Unit * 0.050f;
    percent = Clamp(percent, 0, 100);
    DrawText(frame, label, new Vector2(x - w * 0.5f, y - h * 1.35f), v.Unit * 0.00105f, Text, TextAlignment.LEFT);
    DrawText(frame, value, new Vector2(x + w * 0.5f, y - h * 1.35f), v.Unit * 0.00105f, Text, TextAlignment.RIGHT);
    DrawBox(frame, new Vector2(x, y), new Vector2(w, h), new Color(22, 42, 54));
    DrawBox(frame, new Vector2(x - w * 0.5f + w * percent / 100f * 0.5f, y), new Vector2(w * percent / 100f, h), color);
    DrawLine(frame, new Vector2(x - w * 0.5f, y - h * 0.52f), new Vector2(x + w * 0.5f, y - h * 0.52f), v.Unit * 0.003f, Muted);
    DrawLine(frame, new Vector2(x - w * 0.5f, y + h * 0.52f), new Vector2(x + w * 0.5f, y + h * 0.52f), v.Unit * 0.003f, Muted);
}

void DrawGauge(MySpriteDrawFrame frame, Vector2 center, float size, float percent, Color color)
{
    percent = Clamp(percent, 0, 100);
    DrawCircle(frame, center, size, new Color(18, 35, 49));
    DrawCircleOutline(frame, center, size, size * 0.055f, Muted);
    float angle = (float)(-Math.PI * 0.75 + Math.PI * 1.5 * percent / 100f);
    Vector2 end = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * size * 0.38f;
    DrawLine(frame, center, end, size * 0.035f, color);
    DrawCircle(frame, center, size * 0.11f, color);
}

void DrawHalfGauge(MySpriteDrawFrame frame, Vector2 center, float size, float percent, Color color)
{
    percent = Clamp(percent, 0, 100);
    int segments = 28;
    float radius = size * 0.43f;
    float thickness = size * 0.045f;
    Color empty = new Color(92, 130, 143);
    for (int i = 0; i < segments; i++)
    {
        float t0 = (float)i / segments;
        float t1 = (float)(i + 1) / segments;
        float angle0 = (float)Math.PI * (1f + t0);
        float angle1 = (float)Math.PI * (1f + t1);
        Vector2 a = center + new Vector2((float)Math.Cos(angle0), (float)Math.Sin(angle0)) * radius;
        Vector2 b = center + new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)) * radius;
        DrawLine(frame, a, b, thickness, t1 * 100f <= percent ? color : empty);
    }

    float needleAngle = (float)Math.PI * (1f + percent / 100f);
    Vector2 end = center + new Vector2((float)Math.Cos(needleAngle), (float)Math.Sin(needleAngle)) * radius * 0.82f;
    DrawLine(frame, center, end, thickness * 0.65f, color);
    DrawCircle(frame, center, size * 0.075f, color);
}

void DrawStatusLine(MySpriteDrawFrame frame, View v, string label, string value, float y, Color color)
{
    Vector2 center = new Vector2(v.Center.X, y);
    Vector2 size = new Vector2(v.W * 0.76f, v.Unit * 0.065f);
    DrawBox(frame, center, size, new Color(0, 6, 8));
    DrawRectOutline(frame, center, size, v.Unit * 0.004f, Accent);
    DrawText(frame, label, new Vector2(center.X - size.X * 0.44f, y - v.Unit * 0.010f), v.Unit * 0.00098f, Text, TextAlignment.LEFT);
    DrawText(frame, value, new Vector2(center.X + size.X * 0.44f, y - v.Unit * 0.010f), v.Unit * 0.00098f, color, TextAlignment.RIGHT);
}

View GetView(IMyTextSurface surface)
{
    Vector2 size = surface.SurfaceSize;
    Vector2 offset = (surface.TextureSize - size) * 0.5f;
    return new View(offset, size);
}

float Percent(double value, double max)
{
    return max <= 0 ? 0 : (float)(value / max * 100.0);
}

float Clamp(float v, float min, float max)
{
    return Math.Max(min, Math.Min(max, v));
}

Color StatusColor(float percent, bool inverse)
{
    float p = inverse ? 100 - percent : percent;
    if (p >= 50) return Safe;
    if (p >= 25) return Caution;
    if (p >= 10) return Warning;
    return Critical;
}

Color StateColor(string state)
{
    string s = state.ToUpper();
    if (s.Contains("READY") || s.Contains("SAFE") || s.Contains("NO DAMAGE")) return Safe;
    if (s.Contains("CAUTION") || s.Contains("CHARGING") || s.Contains("HEAVY")) return Caution;
    if (s.Contains("DO NOT") || s.Contains("CRITICAL") || s.Contains("DAMAGED") || s.Contains("REPAIR")) return Critical;
    return Warning;
}

string CargoState()
{
    if (_m.CargoPercent > 95) return "FULL";
    if (_m.CargoPercent > 85) return "HEAVY";
    return "OK";
}

string PlanetEntryState()
{
    if (_m.GravityPercent < 1) return "SPACE";
    if (_m.VerticalSpeed < -8) return "FALLING";
    if (_m.LiftCount == 0) return "NO LIFT";
    if (_m.LiftOnline == 0) return "LIFT OFF";
    if (_m.LiftOnline < _m.LiftCount) return "LIFT PARTIAL";
    if (_m.LiftCapacityPercent < 100) return "NO HOVER";
    if (_m.LiftCapacityPercent < 110) return "LOW LIFT";
    if (_m.FuelPercent < 25 || _m.CargoPercent > 90) return "DO NOT ENTER";
    if (_m.FuelPercent < 40 || _m.CargoPercent > 80) return "CAUTION";
    return "SAFE";
}

Color LiftStatusColor()
{
    if (_m.GravityPercent < 1) return Muted;
    if (_m.LiftCount == 0 || _m.LiftOnline == 0 || _m.LiftCapacityPercent < 100) return Critical;
    if (_m.LiftOnline < _m.LiftCount || _m.LiftCapacityPercent < 110) return Caution;
    return Safe;
}

string TurretState()
{
    if (_m.TurretCount == 0) return "NO TURRETS";
    if (_m.TurretDamaged > 0) return "DAMAGED";
    if (_m.TurretOnline < _m.TurretCount) return "PARTIAL";
    return "READY";
}

string PressureState()
{
    if (_m.VentCount == 0) return "NO VENTS";
    if (_m.PressurePercent >= 95) return "PRESSURIZED";
    if (_m.PressurePercent >= 50) return "PARTIAL";
    return "LEAK / NO AIR";
}

string FormatPct(float p)
{
    return p.ToString("0") + "%";
}

string FormatKg(double kg)
{
    if (kg >= 1000000) return (kg / 1000000.0).ToString("0.00") + " Mkg";
    if (kg >= 1000) return (kg / 1000.0).ToString("0.0") + " t";
    return kg.ToString("0") + " kg";
}

void DrawShield(MySpriteDrawFrame frame, Vector2 center, float size, float thickness, Color color)
{
    float w = size * 0.58f;
    float h = size * 0.70f;
    Vector2 top = center + new Vector2(0, -h * 0.48f);
    Vector2 upperLeft = center + new Vector2(-w * 0.42f, -h * 0.31f);
    Vector2 leftMid = center + new Vector2(-w * 0.42f, h * 0.10f);
    Vector2 lowerLeft = center + new Vector2(-w * 0.28f, h * 0.34f);
    Vector2 bottom = center + new Vector2(0, h * 0.50f);
    Vector2 lowerRight = center + new Vector2(w * 0.28f, h * 0.34f);
    Vector2 rightMid = center + new Vector2(w * 0.42f, h * 0.10f);
    Vector2 upperRight = center + new Vector2(w * 0.42f, -h * 0.31f);
    DrawLine(frame, top, upperLeft, thickness, color);
    DrawLine(frame, upperLeft, leftMid, thickness, color);
    DrawLine(frame, leftMid, lowerLeft, thickness, color);
    DrawLine(frame, lowerLeft, bottom, thickness, color);
    DrawLine(frame, bottom, lowerRight, thickness, color);
    DrawLine(frame, lowerRight, rightMid, thickness, color);
    DrawLine(frame, rightMid, upperRight, thickness, color);
    DrawLine(frame, upperRight, top, thickness, color);
}

void DrawCrosshair(MySpriteDrawFrame frame, Vector2 center, float size, float thickness, Color white, Color red, float pulse)
{
    float half = size * 0.50f;
    float gap = size * 0.23f;
    float ring = size * 0.58f;
    float spike = size * 0.18f;
    DrawCircleOutline(frame, center, ring, thickness, white);
    DrawLine(frame, center + new Vector2(-half, 0), center + new Vector2(-gap, 0), thickness, white);
    DrawLine(frame, center + new Vector2(gap, 0), center + new Vector2(half, 0), thickness, white);
    DrawLine(frame, center + new Vector2(0, -half), center + new Vector2(0, -gap), thickness, white);
    DrawLine(frame, center + new Vector2(0, gap), center + new Vector2(0, half), thickness, white);
    DrawTriangle(frame, center + new Vector2(0, -ring * 0.50f - spike * 0.35f), new Vector2(spike, spike * 1.8f), 0f, white);
    DrawTriangle(frame, center + new Vector2(0, ring * 0.50f + spike * 0.35f), new Vector2(spike, spike * 1.8f), (float)Math.PI, white);
    DrawCircle(frame, center, size * (0.16f + 0.08f * pulse), red);
}

void DrawBox(MySpriteDrawFrame frame, Vector2 center, Vector2 size, Color color)
{
    MySprite sprite = MySprite.CreateSprite("SquareSimple", center, size);
    sprite.Color = color;
    frame.Add(sprite);
}

void DrawRectOutline(MySpriteDrawFrame frame, Vector2 center, Vector2 size, float thickness, Color color)
{
    float left = center.X - size.X * 0.5f;
    float right = center.X + size.X * 0.5f;
    float top = center.Y - size.Y * 0.5f;
    float bottom = center.Y + size.Y * 0.5f;
    DrawLine(frame, new Vector2(left, top), new Vector2(right, top), thickness, color);
    DrawLine(frame, new Vector2(right, top), new Vector2(right, bottom), thickness, color);
    DrawLine(frame, new Vector2(right, bottom), new Vector2(left, bottom), thickness, color);
    DrawLine(frame, new Vector2(left, bottom), new Vector2(left, top), thickness, color);
}

void DrawCornerTicks(MySpriteDrawFrame frame, Vector2 center, Vector2 size, float length, float thickness, Color color)
{
    float left = center.X - size.X * 0.5f;
    float right = center.X + size.X * 0.5f;
    float top = center.Y - size.Y * 0.5f;
    float bottom = center.Y + size.Y * 0.5f;
    DrawLine(frame, new Vector2(left, top), new Vector2(left + length, top), thickness, color);
    DrawLine(frame, new Vector2(left, top), new Vector2(left, top + length), thickness, color);
    DrawLine(frame, new Vector2(right, top), new Vector2(right - length, top), thickness, color);
    DrawLine(frame, new Vector2(right, top), new Vector2(right, top + length), thickness, color);
    DrawLine(frame, new Vector2(left, bottom), new Vector2(left + length, bottom), thickness, color);
    DrawLine(frame, new Vector2(left, bottom), new Vector2(left, bottom - length), thickness, color);
    DrawLine(frame, new Vector2(right, bottom), new Vector2(right - length, bottom), thickness, color);
    DrawLine(frame, new Vector2(right, bottom), new Vector2(right, bottom - length), thickness, color);
}

void DrawCircle(MySpriteDrawFrame frame, Vector2 center, float diameter, Color color)
{
    MySprite sprite = MySprite.CreateSprite("Circle", center, new Vector2(diameter, diameter));
    sprite.Color = color;
    frame.Add(sprite);
}

void DrawCircleOutline(MySpriteDrawFrame frame, Vector2 center, float diameter, float thickness, Color color)
{
    MySprite outer = MySprite.CreateSprite("Circle", center, new Vector2(diameter, diameter));
    outer.Color = color;
    frame.Add(outer);
    MySprite inner = MySprite.CreateSprite("Circle", center, new Vector2(diameter - thickness * 2.2f, diameter - thickness * 2.2f));
    inner.Color = Bg;
    frame.Add(inner);
}

void DrawLine(MySpriteDrawFrame frame, Vector2 a, Vector2 b, float thickness, Color color)
{
    Vector2 delta = b - a;
    float length = delta.Length();
    Vector2 center = (a + b) * 0.5f;
    float angle = (float)Math.Atan2(delta.Y, delta.X);
    MySprite sprite = MySprite.CreateSprite("SquareSimple", center, new Vector2(length, thickness));
    sprite.Color = color;
    sprite.RotationOrScale = angle;
    frame.Add(sprite);
}

void DrawTriangle(MySpriteDrawFrame frame, Vector2 center, Vector2 size, float rotation, Color color)
{
    MySprite sprite = MySprite.CreateSprite("Triangle", center, size);
    sprite.Color = color;
    sprite.RotationOrScale = rotation;
    frame.Add(sprite);
}

void DrawTitleOrnament(MySpriteDrawFrame frame, float x, float y, float width, float thickness, Color color)
{
    float gap = width * 0.055f;
    float notch = width * 0.055f;
    DrawLine(frame, new Vector2(x - width * 0.50f, y), new Vector2(x - gap, y), thickness, color);
    DrawLine(frame, new Vector2(x + gap, y), new Vector2(x + width * 0.50f, y), thickness, color);
    DrawLine(frame, new Vector2(x - gap, y), new Vector2(x, y + notch), thickness, color);
    DrawLine(frame, new Vector2(x, y + notch), new Vector2(x + gap, y), thickness, color);
}

void DrawText(MySpriteDrawFrame frame, string value, Vector2 position, float scale, Color color, TextAlignment align)
{
    MySprite sprite = MySprite.CreateText(value, "White", color, scale, align);
    sprite.Position = position;
    frame.Add(sprite);
}

class ScreenTarget
{
    public string Name;
    public string Page;
    public IMyTextSurface Surface;
    public bool Compact;
    public ScreenTarget(string name, string page, IMyTextSurface surface, bool compact)
    {
        Name = name;
        Page = page;
        Surface = surface;
        Compact = compact;
    }
}

class View
{
    public Vector2 Offset;
    public Vector2 Size;
    public Vector2 Center;
    public float Left;
    public float Right;
    public float Top;
    public float Bottom;
    public float W;
    public float H;
    public float Unit;
    public View(Vector2 offset, Vector2 size)
    {
        Offset = offset;
        Size = size;
        Center = offset + size * 0.5f;
        Left = offset.X;
        Top = offset.Y;
        W = size.X;
        H = size.Y;
        Right = Left + W;
        Bottom = Top + H;
        Unit = Math.Min(W, H);
    }
}

class Metrics
{
    public string MasterState = "READY";
    public Color MasterColor;
    public string FlightMode = "MANUAL";
    public double BaseMassKg;
    public double TotalMassKg;
    public double CargoMassKg;
    public float CargoPercent;
    public float CargoDensityKgPerL;
    public int CargoBlocks;
    public float BatteryPercent;
    public int BatteryCount;
    public int BatteryOnline;
    public double PowerInputMw;
    public double PowerOutputMw;
    public int ReactorCount;
    public int SolarCount;
    public int HydrogenEngineCount;
    public float FuelPercent;
    public int HydrogenCount;
    public int HydrogenOnline;
    public float HydrogenImbalance;
    public float OxygenPercent;
    public int OxygenCount;
    public int OxygenOnline;
    public float JumpPercent;
    public int JumpCount;
    public int JumpOnline;
    public int JumpDamaged;
    public string JumpState = "NO DRIVES";
    public int TurretCount;
    public int TurretOnline;
    public int TurretDamaged;
    public double AmmoMassKg;
    public int VentCount;
    public int VentOnline;
    public int PressurizedVents;
    public int DepressurizeVents;
    public float PressurePercent;
    public int ThrusterCount;
    public int ThrusterOnline;
    public int LiftCount;
    public int LiftOnline;
    public float LiftLoadPercent;
    public float LiftCapacityPercent;
    public int CruiseCount;
    public int CruiseOnline;
    public float CruiseLoadPercent;
    public int BrakeCount;
    public int BrakeOnline;
    public float BrakeLoadPercent;
    public int BlockCount;
    public int DamagedCount;
    public float NaturalGravity;
    public float GravityPercent;
    public double VerticalSpeed;
}
