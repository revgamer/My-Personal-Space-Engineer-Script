// ============================================================
// HogOS2 - Hog Operating System
// Author: RevGamer
// Version: v2.1
//
// Cockpit default, 4 screens:
//   Surface0=OreCargo
//   Surface1=Menu
//   Surface2=Weight
//   Surface3=Utility
// Mother-style aliases:
//   hog/boot
//   hog/stop
//   flight/cruise on
//   flight/level toggle
//   mine/slow
//   mine/fast
//   mine/terrain
//   view/up
//   view/down
//   view/select
//   view/back
//   view/go Menu
//   view/go Power
// ============================================================

const string VERSION = "v2.1";
const string SEC = "HogOS2";

MyIni _ini = new MyIni();
HogConfig _cfg = new HogConfig();
Hud _hud;
Flight _flight;
DrillSys _drills;
PowerSys _power;
CargoSys _cargo;
ScreenSys _screens;
double _time;
int _tick;
bool _wasInCockpit;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
    EnsureCustomData();
    LoadConfig();
    _hud = new Hud();
    _flight = new Flight(this, _cfg);
    _drills = new DrillSys(this, _cfg);
    _power = new PowerSys(this, _cfg);
    _cargo = new CargoSys(this, _cfg);
    _screens = new ScreenSys(this, _cfg, _hud, _flight, _drills, _power, _cargo);
}

public void Save()
{
    Storage = _flight.SaveState(Storage);
}

public void Main(string argument, UpdateType updateSource)
{
    if (!string.IsNullOrWhiteSpace(argument))
        HandleCommand(argument.Trim());

    _time += Runtime.TimeSinceLastRun.TotalSeconds;

    if ((updateSource & UpdateType.Update100) != 0)
    {
        LoadConfig();
        Echo("HogOS2 " + VERSION + "\n" + _cfg.ShipName);
        Echo("Cruise: " + (_flight.CruiseOn ? "ON " : "OFF ") + _flight.CruiseTarget.ToString("0.0") + " m/s");
        Echo("Level: " + (_flight.AlignOn ? "ON " : "OFF ") + _flight.AlignPitch.ToString("0") + " deg");
    }

    if ((updateSource & UpdateType.Update10) != 0)
    {
        _tick++;
        _flight.Update();
        bool inCockpit = _flight.Controller != null && _flight.Controller.IsUnderControl;
        if (inCockpit && !_wasInCockpit) _screens.TriggerBoot(_time);
        _wasInCockpit = inCockpit;
        if (_tick % 3 == 0)
        {
            _power.Update();
            _cargo.Update();
            _screens.Update(_time, _tick);
        }
    }
}

void HandleCommand(string arg)
{
    string[] commands = arg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (string raw in commands)
    {
        string one = raw.Trim();
        if (one.Length == 0) continue;
        HandleOneCommand(one);
    }
}

void HandleOneCommand(string arg)
{
    string[] p = arg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    if (p.Length == 0) return;
    string cmd = p[0].ToLower();

    if (cmd == "reboot" || cmd == "boot" || cmd == "hog/boot")
    {
        _screens?.TriggerBoot(_time);
        return;
    }
    if (cmd == "toggle_gaa" || cmd == "toggle_gyro" || cmd == "flight/level")
    {
        if (cmd == "flight/level" && p.Length > 1)
        {
            string mode = p[1].ToLower();
            if (mode == "on") _flight.AlignOn = true;
            else if (mode == "off") _flight.AlignOn = false;
            else _flight.AlignOn = !_flight.AlignOn;
        }
        else _flight.AlignOn = !_flight.AlignOn;
        if (!_flight.AlignOn) _flight.ReleaseGyros();
        return;
    }
    if (cmd == "set_gyro_pitch_0" || cmd == "flight/level_zero")
    {
        _flight.AlignPitch = 0;
        return;
    }
    if ((cmd == "set_gaa_pitch" || cmd == "set_gyro_pitch" || cmd == "flight/level_pitch") && p.Length > 1)
    {
        float v;
        if (float.TryParse(p[1], out v))
        {
            if (p[1][0] == '+' || p[1][0] == '-') _flight.AlignPitch += v;
            else _flight.AlignPitch = v;
            _flight.AlignPitch = MathHelper.Clamp(_flight.AlignPitch, -90f, 90f);
        }
        return;
    }
    if (cmd == "toggle_cruise" || cmd == "flight/cruise_toggle")
    {
        _flight.SetFlightCruise(!_flight.CruiseOn);
        return;
    }
    if (cmd == "stop" || cmd == "cruise_stop" || cmd == "mine_stop" || cmd == "hog/stop")
    {
        StopAllMotion();
        return;
    }
    if (cmd == "mine_slow" || cmd == "mine/slow")
    {
        SetCruisePreset(_cfg.SlowMiningSpeed, "Slow mining", false);
        return;
    }
    if (cmd == "mine_fast" || cmd == "mine/fast")
    {
        SetCruisePreset(_cfg.FastMiningSpeed, "Fast mining", false);
        return;
    }
    if (cmd == "terrain_clear" || cmd == "terrain_clearing" || cmd == "clear_terrain" || cmd == "clear_terrian" || cmd == "terrian_clear" || cmd == "mine/terrain")
    {
        SetCruisePreset(_cfg.TerrainClearingSpeed, "Terrain clearing", true);
        return;
    }
    if (cmd == "mine" && p.Length > 1)
    {
        string mode = p[1].ToLower();
        if (mode == "stop" || mode == "off") StopAllMotion();
        else if (mode == "slow") SetCruisePreset(_cfg.SlowMiningSpeed, "Slow mining", false);
        else if (mode == "fast") SetCruisePreset(_cfg.FastMiningSpeed, "Fast mining", false);
        else if (mode == "clear" || mode == "clearing" || mode == "terrain") SetCruisePreset(_cfg.TerrainClearingSpeed, "Terrain clearing", true);
        return;
    }
    if (cmd == "cruise_full")
    {
        _flight.SetFlightCruise(true);
        return;
    }
    if (cmd == "cruise_reset")
    {
        _flight.CruiseTarget = _cfg.CruiseSpeed;
        _flight.SetCruise(false);
        return;
    }
    if (cmd == "set_cruise" && p.Length > 1)
    {
        SetFlightCruiseValue(p[1]);
        return;
    }
    if (cmd == "cruise")
    {
        if (p.Length == 1) _flight.SetFlightCruise(!_flight.CruiseOn);
        else if (p[1].ToLower() == "on") _flight.SetFlightCruise(true);
        else if (p[1].ToLower() == "off") _flight.SetCruise(false);
        else SetFlightCruiseValue(p[1]);
    }
    if (cmd == "flight/cruise")
    {
        if (p.Length == 1) _flight.SetFlightCruise(!_flight.CruiseOn);
        else if (p[1].ToLower() == "on") _flight.SetFlightCruise(true);
        else if (p[1].ToLower() == "off") _flight.SetFlightCruise(false);
        else SetFlightCruiseValue(p[1]);
        return;
    }
    if (cmd == "view/up" || cmd == "menu/up")
    {
        _screens?.MenuUp();
        return;
    }
    if (cmd == "view/down" || cmd == "menu/down")
    {
        _screens?.MenuDown();
        return;
    }
    if (cmd == "view/back" || cmd == "menu/back")
    {
        _screens?.MenuBack();
        return;
    }
    if (cmd == "view/select" || cmd == "menu/select")
    {
        string next = _screens == null ? "" : _screens.MenuSelect();
        if (!string.IsNullOrWhiteSpace(next)) HandleCommand(next);
        return;
    }
    if ((cmd == "view/go" || cmd == "screen/go") && p.Length > 1)
    {
        _screens?.Go(p[1]);
        return;
    }
}

void StopAllMotion()
{
    _flight.StopMotion();
    _drills.SetMode(false, false);
}

void SetCruisePreset(float speed, string mode, bool terrain)
{
    _flight.CruiseTarget = Math.Max(0, speed);
    _flight.MiningMode = mode;
    if (_cfg.MiningAutoLevel) _flight.AlignOn = true;
    _drills.SetMode(true, terrain);
    _flight.SetCruise(_flight.CruiseTarget > 0);
}

void SetFlightCruiseValue(string raw)
{
    float v;
    if (!float.TryParse(raw, out v)) return;
    if (raw[0] == '+' || raw[0] == '-') _flight.CruiseTarget += v;
    else _flight.CruiseTarget = v;
    if (_flight.CruiseTarget < 0) _flight.CruiseTarget = 0;
    _flight.MiningMode = "";
    _flight.SetCruise(_flight.CruiseTarget > 0);
}

void LoadConfig()
{
    LoadConfigFromCustomData();
    _flight?.ApplyConfig(_cfg);
    _drills?.ApplyConfig(_cfg);
    _screens?.LoadSurfaces();
}

void LoadConfigFromCustomData()
{
    MyIniParseResult r;
    _ini.Clear();
    if (_ini.TryParse(Me.CustomData, out r)) _cfg.Read(_ini);
}

void EnsureCustomData()
{
    MyIni oldIni = new MyIni();
    MyIniParseResult r;
    if (oldIni.TryParse(Me.CustomData, out r))
    {
        _cfg.Read(oldIni);
    }

    if (!IsConfigCurrent(oldIni)) WriteConfig();
}

bool IsConfigCurrent(MyIni ini)
{
    return ini.ContainsKey(SEC, "ShipName")
        && ini.ContainsKey("Menu", "0")
        && !ini.ContainsSection("Boot")
        && !ini.ContainsSection("Screens")
        && !ini.ContainsSection("Blocks")
        && ini.ContainsKey("Groups", "LiftThrusters")
        && ini.ContainsKey("Groups", "StopThrusters")
        && ini.ContainsKey("Groups", "ForwardThrusters")
        && ini.ContainsKey("Groups", "ReverseThrusters")
        && ini.ContainsKey("Groups", "AlignGyros")
        && ini.ContainsKey("Groups", "Drills")
        && ini.ContainsKey("Safety", "CargoReturn")
        && ini.ContainsKey("Safety", "BatteryReturn")
        && ini.ContainsKey("Safety", "HydrogenReturn")
        && ini.ContainsKey("Cruise", "TargetSpeed")
        && ini.ContainsKey("Cruise", "UseReverseThrusters")
        && ini.ContainsKey("Mining", "SlowSpeed")
        && ini.ContainsKey("Mining", "FastSpeed")
        && ini.ContainsKey("Mining", "TerrainClearingSpeed")
        && ini.ContainsKey("Mining", "AutoLevel");
}

void WriteConfig()
{
    MyIni n = new MyIni();
    _cfg.WriteDefaults(n);
    Me.CustomData = n.ToString();
}

class HogConfig
{
    public string ShipName = "Rev Spacehog 01";
    public const float BootSeconds = 6f;
    public string LiftThrusters = "[RSH] Lift Thrusters";
    public string StopThrusters = "[RSH] Brake Thrusters";
    public string ForwardThrusters = "[RSH] Cruising Thrusters";
    public string ReverseThrusters = "[RSH] Brake Thrusters";
    public string CargoTrack = "";
    public string AlignGyros = "[RSH] Gyros";
    public string Drills = "[RSH] Drills";
    public float LiftWarning = 0.90f;
    public float LiftCutoff = 0.98f;
    public float CargoReturn = 0.90f;
    public float BatteryReturn = 0.20f;
    public float HydrogenReturn = 0.25f;
    public float CruiseSpeed = 20f;
    public bool UseReverseThrusters = false;
    public float SlowMiningSpeed = 0.03f;
    public float FastMiningSpeed = 1.0f;
    public float TerrainClearingSpeed = 2.5f;
    public bool MiningAutoLevel = true;
    public string[] Surface = new string[] { "OreCargo", "Menu", "Weight", "Utility" };
    public string[] Menu = new string[]
    {
        "1 - Slow Mining=mine/slow",
        "2 - Fast Mining=mine/fast",
        "3 - Clear Terrain=mine/terrain",
        "4 - Cancel=hog/stop",
        "5 - Flight Cruising On=flight/cruise on",
        "6 - Flight Level On/Off=flight/level toggle",
        "7 - Power Dashboard screen=view/go Power"
    };

    public void WriteDefaults(MyIni ini)
    {
        ini.Set(SEC, "ShipName", ShipName);
        for (int i = 0; i < Menu.Length; i++) ini.Set("Menu", i.ToString(), Menu[i]);
        ini.Set("Groups", "LiftThrusters", LiftThrusters);
        ini.Set("Groups", "StopThrusters", StopThrusters);
        ini.Set("Groups", "ForwardThrusters", ForwardThrusters);
        ini.Set("Groups", "ReverseThrusters", ReverseThrusters);
        ini.Set("Groups", "AlignGyros", AlignGyros);
        ini.Set("Groups", "Drills", Drills);
        ini.Set("Safety", "CargoReturn", CargoReturn);
        ini.Set("Safety", "BatteryReturn", BatteryReturn);
        ini.Set("Safety", "HydrogenReturn", HydrogenReturn);
        ini.Set("Cruise", "TargetSpeed", CruiseSpeed);
        ini.Set("Cruise", "UseReverseThrusters", UseReverseThrusters);
        ini.Set("Mining", "SlowSpeed", SlowMiningSpeed);
        ini.Set("Mining", "FastSpeed", FastMiningSpeed);
        ini.Set("Mining", "TerrainClearingSpeed", TerrainClearingSpeed);
        ini.Set("Mining", "AutoLevel", MiningAutoLevel);
    }

    public void Read(MyIni ini)
    {
        ShipName = ini.Get(SEC, "ShipName").ToString(ShipName);
        for (int i = 0; i < Menu.Length; i++) Menu[i] = ini.Get("Menu", i.ToString()).ToString(Menu[i]);
        LiftThrusters = ini.Get("Groups", "LiftThrusters").ToString(LiftThrusters);
        StopThrusters = ini.Get("Groups", "StopThrusters").ToString(StopThrusters);
        ForwardThrusters = ini.Get("Groups", "ForwardThrusters").ToString(ForwardThrusters);
        ReverseThrusters = ini.Get("Groups", "ReverseThrusters").ToString(ReverseThrusters);
        CargoTrack = ini.Get("Groups", "CargoTrack").ToString(CargoTrack);
        AlignGyros = ini.Get("Groups", "AlignGyros").ToString(AlignGyros);
        Drills = ini.Get("Groups", "Drills").ToString(Drills);
        LiftWarning = ini.Get("Safety", "LiftWarning").ToSingle(LiftWarning);
        LiftCutoff = ini.Get("Safety", "LiftCutoff").ToSingle(LiftCutoff);
        CargoReturn = ini.Get("Safety", "CargoReturn").ToSingle(CargoReturn);
        BatteryReturn = ini.Get("Safety", "BatteryReturn").ToSingle(BatteryReturn);
        HydrogenReturn = ini.Get("Safety", "HydrogenReturn").ToSingle(HydrogenReturn);
        CruiseSpeed = ini.Get("Cruise", "TargetSpeed").ToSingle(CruiseSpeed);
        UseReverseThrusters = ini.Get("Cruise", "UseReverseThrusters").ToBoolean(UseReverseThrusters);
        SlowMiningSpeed = ini.Get("Mining", "SlowSpeed").ToSingle(SlowMiningSpeed);
        FastMiningSpeed = ini.Get("Mining", "FastSpeed").ToSingle(FastMiningSpeed);
        TerrainClearingSpeed = ini.Get("Mining", "TerrainClearingSpeed").ToSingle(TerrainClearingSpeed);
        MiningAutoLevel = ini.Get("Mining", "AutoLevel").ToBoolean(MiningAutoLevel);
    }
}

class BlockGroup<T> where T : class
{
    Program _p;
    public List<T> Blocks = new List<T>();
    public BlockGroup(Program p) { _p = p; }
    public void Find(string groupOrName, Func<T, bool> filter = null)
    {
        Blocks.Clear();
        Func<T, bool> f = b =>
        {
            IMyTerminalBlock tb = b as IMyTerminalBlock;
            if (tb != null && !tb.IsSameConstructAs(_p.Me)) return false;
            if (filter != null && !filter(b)) return false;
            return true;
        };
        if (!string.IsNullOrWhiteSpace(groupOrName))
        {
            IMyBlockGroup g = _p.GridTerminalSystem.GetBlockGroupWithName(groupOrName);
            if (g != null) g.GetBlocksOfType(Blocks, f);
            if (Blocks.Count == 0)
                _p.GridTerminalSystem.GetBlocksOfType(Blocks, b =>
                {
                    IMyTerminalBlock tb = b as IMyTerminalBlock;
                    return tb != null && tb.CustomName.Contains(groupOrName) && f(b);
                });
            return;
        }
        _p.GridTerminalSystem.GetBlocksOfType(Blocks, f);
    }
}

class Flight
{
    Program _p;
    HogConfig _cfg;
    BlockGroup<IMyShipController> _controllers;
    BlockGroup<IMyGyro> _gyros;
    BlockGroup<IMyThrust> _forward;
    BlockGroup<IMyThrust> _reverse;
    public IMyShipController Controller;
    public bool AlignOn;
    public float AlignPitch;
    public bool CruiseOn;
    public float CruiseTarget;
    public string MiningMode = "";
    public string Status = "";

    public Flight(Program p, HogConfig cfg)
    {
        _p = p;
        _cfg = cfg;
        _controllers = new BlockGroup<IMyShipController>(p);
        _gyros = new BlockGroup<IMyGyro>(p);
        _forward = new BlockGroup<IMyThrust>(p);
        _reverse = new BlockGroup<IMyThrust>(p);
        ApplyConfig(cfg);
        LoadState(p.Storage);
    }

    public void ApplyConfig(HogConfig cfg)
    {
        _cfg = cfg;
        if (CruiseTarget <= 0) CruiseTarget = cfg.CruiseSpeed;
    }

    public void LoadState(string storage)
    {
        MyIni s = new MyIni();
        MyIniParseResult r;
        if (!s.TryParse(storage, out r)) return;
        AlignPitch = s.Get("Flight", "AlignPitch").ToSingle(AlignPitch);
        CruiseTarget = s.Get("Flight", "CruiseTarget").ToSingle(CruiseTarget);
    }

    public string SaveState(string storage)
    {
        MyIni s = new MyIni();
        MyIniParseResult r;
        s.TryParse(storage, out r);
        s.Set("Flight", "AlignPitch", AlignPitch);
        s.Set("Flight", "CruiseTarget", CruiseTarget);
        return s.ToString();
    }

    public void SetCruise(bool on)
    {
        CruiseOn = on;
        if (!on) MiningMode = "";
        if (!on)
        {
            ReleaseThrusters();
        }
    }

    public void SetFlightCruise(bool on)
    {
        MiningMode = "";
        if (on) CruiseTarget = _cfg.CruiseSpeed;
        SetCruise(on);
    }

    public void StopMotion()
    {
        CruiseOn = false;
        MiningMode = "";
        AlignOn = false;
        ReleaseThrusters();
        ReleaseGyros();
    }

    public void Update()
    {
        FindController();
        if (Controller == null) { Status = "No controller"; return; }
        _gyros.Find(_cfg.AlignGyros);
        FindCruiseThrusters();
        Status = "";
        if (AlignOn) AlignToGravity();
        if (CruiseOn) Cruise();
    }

    void FindController()
    {
        _controllers.Find("");
        Controller = null;
        IMyShipController first = null;
        foreach (var c in _controllers.Blocks)
        {
            if (!c.IsWorking) continue;
            if (first == null) first = c;
            if (Controller == null && c.IsUnderControl && c.CanControlShip) Controller = c;
            if (c.IsMainCockpit) Controller = c;
        }
        if (Controller == null) Controller = first;
    }

    void FindCruiseThrusters()
    {
        if (!string.IsNullOrWhiteSpace(_cfg.ForwardThrusters)) _forward.Find(_cfg.ForwardThrusters);
        else _forward.Find("", t => Vector3D.Dot(-t.WorldMatrix.Forward, Controller.WorldMatrix.Forward) > 0.7);

        if (!string.IsNullOrWhiteSpace(_cfg.ReverseThrusters)) _reverse.Find(_cfg.ReverseThrusters);
        else _reverse.Find("", t => Vector3D.Dot(-t.WorldMatrix.Forward, -Controller.WorldMatrix.Forward) > 0.7);
    }

    void ReleaseThrusters()
    {
        if (Controller == null) FindController();
        if (Controller != null) FindCruiseThrusters();
        else _forward.Find(_cfg.ForwardThrusters);
        foreach (var t in _forward.Blocks) t.ThrustOverridePercentage = 0;
        if (Controller == null) _reverse.Find(_cfg.ReverseThrusters);
        foreach (var t in _reverse.Blocks) { t.ThrustOverridePercentage = 0; t.Enabled = true; }
    }

    public void ReleaseGyros()
    {
        foreach (var g in _gyros.Blocks)
        {
            g.SetValueFloat("Pitch", 0);
            g.SetValueFloat("Yaw", 0);
            g.SetValueFloat("Roll", 0);
            g.SetValueFloat("Power", 1);
            g.GyroOverride = false;
        }
    }

    void AlignToGravity()
    {
        Vector3D grav = Controller.GetNaturalGravity();
        if (grav.Length() < 0.01) { Status = "No natural gravity"; ReleaseGyros(); return; }
        grav.Normalize();

        Matrix orientation;
        Controller.Orientation.GetMatrix(out orientation);
        Vector3D desiredDown = orientation.Down;
        if (AlignPitch < 0) desiredDown = Vector3D.Lerp(orientation.Down, orientation.Forward, -AlignPitch / 90f);
        else if (AlignPitch > 0) desiredDown = Vector3D.Lerp(orientation.Down, -orientation.Forward, AlignPitch / 90f);

        foreach (var gyro in _gyros.Blocks)
        {
            if (!gyro.IsWorking) continue;
            gyro.Orientation.GetMatrix(out orientation);
            Vector3D localDown = Vector3D.Transform(desiredDown, MatrixD.Transpose(orientation));
            Vector3D localGrav = Vector3D.Transform(grav, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));
            Vector3D rotation = Vector3D.Cross(localDown, localGrav);
            double angle = rotation.Length();
            if (angle < 0.01)
            {
                gyro.SetValueFloat("Pitch", 0f);
                gyro.SetValueFloat("Yaw", 0f);
                gyro.SetValueFloat("Roll", 0f);
                gyro.GyroOverride = false;
                continue;
            }
            angle = Math.Atan2(angle, Math.Sqrt(Math.Max(0.0, 1.0 - angle * angle)));
            double control = gyro.GetMaximum<float>("Yaw") * (angle / Math.PI) * 0.9;
            control = Math.Min(gyro.GetMaximum<float>("Yaw"), Math.Max(0.01, control));
            rotation.Normalize();
            rotation *= control;
            gyro.SetValueFloat("Pitch", (float)rotation.GetDim(0));
            gyro.SetValueFloat("Yaw", -(float)rotation.GetDim(1));
            gyro.SetValueFloat("Roll", -(float)rotation.GetDim(2));
            gyro.SetValueFloat("Power", 1f);
            gyro.GyroOverride = true;
        }
    }

    double gyroMaxSpeed()
    {
        double max = 1;
        foreach (var g in _gyros.Blocks)
        {
            if (!g.IsWorking) continue;
            max = Math.Max(max, g.GetMaximum<float>("Yaw"));
        }
        return max;
    }

    void Cruise()
    {
        if (CruiseTarget <= 0) return;
        double speed = Vector3D.Dot(Controller.GetShipVelocities().LinearVelocity, Controller.WorldMatrix.Forward);
        double err = CruiseTarget - speed;
        double deadband = Math.Max(0.005, CruiseTarget * 0.10);
        float amount = (float)MathHelper.Clamp(err / Math.Max(CruiseTarget, 0.03), 0, 1);
        foreach (var t in _forward.Blocks)
        {
            if (err > deadband) { t.Enabled = true; t.ThrustOverridePercentage = amount; }
            else t.ThrustOverridePercentage = 0;
        }
        foreach (var t in _reverse.Blocks)
        {
            float brake = (float)MathHelper.Clamp(-err / Math.Max(CruiseTarget, 0.03), 0, 1);
            if (MiningMode == "" && _cfg.UseReverseThrusters && err < -deadband) { t.Enabled = true; t.ThrustOverridePercentage = brake; }
            else t.ThrustOverridePercentage = 0;
        }
    }
}

class DrillSys
{
    Program _p;
    HogConfig _cfg;
    List<IMyShipDrill> _drills = new List<IMyShipDrill>();
    List<IMyTerminalBlock> _tmpBlocks = new List<IMyTerminalBlock>();
    List<IMyBlockGroup> _groups = new List<IMyBlockGroup>();
    public bool DrillsOn;
    public bool TerrainMode;
    public int DrillCount;

    public DrillSys(Program p, HogConfig cfg)
    {
        _p = p;
        _cfg = cfg;
    }

    public void ApplyConfig(HogConfig cfg)
    {
        _cfg = cfg;
    }

    public void SetMode(bool on, bool terrain)
    {
        DrillsOn = on;
        TerrainMode = on && terrain;
        FindDrills();
        foreach (var d in _drills)
        {
            SetTerrainClearing(d, on && terrain);
            d.Enabled = on;
        }
    }

    void SetTerrainClearing(IMyShipDrill drill, bool on)
    {
        var prop = drill.GetProperty("TerrainClearingMode");
        if (prop != null)
        {
            drill.SetValueBool("TerrainClearingMode", on);
            return;
        }

        string[] actions = on
            ? new string[] { "TerrainClearingMode_On", "TerrainClearing_On", "ClearTerrain_On", "Drill_TerrainClearingMode_On" }
            : new string[] { "TerrainClearingMode_Off", "TerrainClearing_Off", "ClearTerrain_Off", "Drill_TerrainClearingMode_Off" };

        foreach (var actionName in actions)
        {
            var action = drill.GetActionWithName(actionName);
            if (action == null) continue;
            action.Apply(drill);
            return;
        }
    }

    void FindDrills()
    {
        _drills.Clear();
        DrillCount = 0;
        string name = _cfg.Drills ?? "";

        if (!string.IsNullOrWhiteSpace(name))
        {
            IMyBlockGroup exact = _p.GridTerminalSystem.GetBlockGroupWithName(name);
            if (exact != null) AddGroupDrills(exact);

            if (_drills.Count == 0)
            {
                _groups.Clear();
                _p.GridTerminalSystem.GetBlockGroups(_groups);
                foreach (var g in _groups)
                {
                    if (g.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        AddGroupDrills(g);
                }
            }

            if (_drills.Count == 0)
            {
                _p.GridTerminalSystem.GetBlocksOfType(_drills, d => d.IsSameConstructAs(_p.Me) && d.CustomName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        if (_drills.Count == 0)
            _p.GridTerminalSystem.GetBlocksOfType(_drills, d => d.IsSameConstructAs(_p.Me));

        DrillCount = _drills.Count;
    }

    void AddGroupDrills(IMyBlockGroup group)
    {
        _tmpBlocks.Clear();
        group.GetBlocks(_tmpBlocks, b => b.IsSameConstructAs(_p.Me) && b is IMyShipDrill);
        foreach (var b in _tmpBlocks)
        {
            var d = b as IMyShipDrill;
            if (d != null && !_drills.Contains(d)) _drills.Add(d);
        }
    }
}

class PowerSys
{
    Program _p;
    HogConfig _cfg;
    BlockGroup<IMyBatteryBlock> _bats;
    BlockGroup<IMyReactor> _reactors;
    BlockGroup<IMyGasTank> _tanks;
    BlockGroup<IMyPowerProducer> _producers;
    public double Battery, BatteryDelta, Uranium, Hydrogen;
    public string BatteryText = "N/A", UraniumText = "N/A", HydrogenText = "N/A";
    public string BatteryState = "NO BATTERIES", HydrogenState = "NO TANKS";
    public int BatteryCount, ReactorCount, ReactorOnline, TankCount, StockpileCount, EngineCount, EngineOnline;

    public PowerSys(Program p, HogConfig cfg)
    {
        _p = p; _cfg = cfg;
        _bats = new BlockGroup<IMyBatteryBlock>(p);
        _reactors = new BlockGroup<IMyReactor>(p);
        _tanks = new BlockGroup<IMyGasTank>(p);
        _producers = new BlockGroup<IMyPowerProducer>(p);
    }

    public void Update()
    {
        _bats.Find("");
        _reactors.Find("");
        _tanks.Find("", t => t.BlockDefinition.SubtypeId.Contains("Hydrogen"));
        _producers.Find("", p => p.BlockDefinition.ToString().Contains("HydrogenEngine") || p.BlockDefinition.SubtypeId.Contains("HydrogenEngine"));
        CalcBattery();
        CalcReactors();
        CalcHydrogen();
    }

    void CalcBattery()
    {
        BatteryCount = _bats.Blocks.Count;
        double cur = 0, max = 0, input = 0, output = 0;
        foreach (var b in _bats.Blocks)
        {
            if (!b.IsFunctional) continue;
            cur += b.CurrentStoredPower;
            max += b.MaxStoredPower;
            input += b.CurrentInput;
            output += b.CurrentOutput;
        }
        BatteryDelta = input - output;
        if (max <= 0) { Battery = 0; BatteryText = "N/A"; BatteryState = "NO BATTERIES"; return; }
        Battery = cur / max;
        BatteryText = (Battery * 100).ToString("0.0") + "%";
        if (BatteryDelta > 0.001) BatteryState = "CHARGING";
        else if (BatteryDelta < -0.001) BatteryState = "DRAINING";
        else BatteryState = "STABLE";
    }

    void CalcReactors()
    {
        ReactorCount = _reactors.Blocks.Count;
        ReactorOnline = 0;
        double fuel = 0;
        foreach (var r in _reactors.Blocks)
        {
            if (r.IsWorking) ReactorOnline++;
            var inv = r.GetInventory(0);
            double max = (double)inv.MaxVolume;
            fuel += max > 0 ? (double)inv.CurrentVolume / max : 0;
        }
        Uranium = ReactorCount > 0 ? fuel / ReactorCount : 0;
        UraniumText = ReactorCount > 0 ? (Uranium * 100).ToString("0.0") + "%" : "N/A";
    }

    void CalcHydrogen()
    {
        TankCount = _tanks.Blocks.Count;
        StockpileCount = 0;
        EngineCount = _producers.Blocks.Count;
        EngineOnline = 0;
        foreach (var e in _producers.Blocks) if (e.IsWorking) EngineOnline++;
        double cur = 0, max = 0;
        foreach (var t in _tanks.Blocks)
        {
            if (!t.IsFunctional) continue;
            cur += t.FilledRatio * t.Capacity;
            max += t.Capacity;
            if (t.Stockpile) StockpileCount++;
        }
        Hydrogen = max > 0 ? cur / max : 0;
        HydrogenText = TankCount > 0 ? (Hydrogen * 100).ToString("0.0") + "%" : "N/A";
        if (TankCount == 0) HydrogenState = "NO TANKS";
        else if (StockpileCount > 0) HydrogenState = "STOCKPILE " + StockpileCount + "/" + TankCount;
        else if (Hydrogen < _cfg.HydrogenReturn) HydrogenState = "LOW";
        else HydrogenState = "READY";
    }
}

class CargoSys
{
    Program _p;
    HogConfig _cfg;
    BlockGroup<IMyTerminalBlock> _blocks;
    List<MyInventoryItem> _items = new List<MyInventoryItem>();
    public double CurrentVolume, MaxVolume;
    public Dictionary<string, double> Ores = new Dictionary<string, double>();
    public bool HasOre;

    public CargoSys(Program p, HogConfig cfg)
    {
        _p = p; _cfg = cfg; _blocks = new BlockGroup<IMyTerminalBlock>(p);
    }

    public void Update()
    {
        CurrentVolume = 0;
        MaxVolume = 0;
        HasOre = false;
        Ores.Clear();
        _blocks.Find(_cfg.CargoTrack, b => b.HasInventory && b.IsFunctional);
        foreach (var b in _blocks.Blocks)
        {
            for (int i = 0; i < b.InventoryCount; i++)
            {
                var inv = b.GetInventory(i);
                CurrentVolume += (double)inv.CurrentVolume;
                MaxVolume += (double)inv.MaxVolume;
                _items.Clear();
                inv.GetItems(_items);
                foreach (var it in _items)
                {
                    if (it.Type.TypeId != "MyObjectBuilder_Ore") continue;
                    HasOre = true;
                    string name = it.Type.SubtypeId;
                    if (!Ores.ContainsKey(name)) Ores[name] = 0;
                    Ores[name] += (double)it.Amount;
                }
            }
        }
    }

    public double FillRatio { get { return MaxVolume > 0 ? CurrentVolume / MaxVolume : 0; } }
}

class ScreenSys
{
    Program _p;
    HogConfig _cfg;
    Hud _hud;
    Flight _flight;
    DrillSys _drills;
    PowerSys _power;
    CargoSys _cargo;
    List<IMyTerminalBlock> _providers = new List<IMyTerminalBlock>();
    Dictionary<long, string[]> _screens = new Dictionary<long, string[]>();
    BlockGroup<IMyThrust> _liftThrusters;
    double[] _liftHistory = new double[32];
    int _liftHistoryAt;
    double _liftUse, _shipMass, _liftNeed, _liftHave;
    int _liftCount, _liftWorking;
    double _bootStart, _bootUntil;
    int _menuIndex;
    string _menuPage = "Menu";
    MyIni _ini = new MyIni();

    public ScreenSys(Program p, HogConfig cfg, Hud hud, Flight flight, DrillSys drills, PowerSys power, CargoSys cargo)
    {
        _p = p; _cfg = cfg; _hud = hud; _flight = flight; _drills = drills; _power = power; _cargo = cargo;
        _liftThrusters = new BlockGroup<IMyThrust>(p);
        TriggerBoot(0);
        LoadSurfaces();
    }

    public void TriggerBoot(double time)
    {
        _bootStart = time;
        _bootUntil = time + HogConfig.BootSeconds;
    }

    public void MenuUp()
    {
        int count = MenuCount();
        if (count == 0) return;
        _menuIndex--;
        if (_menuIndex < 0) _menuIndex = count - 1;
        _menuPage = "Menu";
    }

    public void MenuDown()
    {
        int count = MenuCount();
        if (count == 0) return;
        _menuIndex++;
        if (_menuIndex >= count) _menuIndex = 0;
        _menuPage = "Menu";
    }

    public void MenuBack()
    {
        _menuPage = "Menu";
    }

    public void Go(string page)
    {
        _menuPage = string.IsNullOrWhiteSpace(page) ? "Menu" : page;
    }

    public string MenuSelect()
    {
        int count = MenuCount();
        if (count == 0) return "";
        if (_menuIndex >= count) _menuIndex = count - 1;
        string entry = MenuEntryAt(_menuIndex);
        int eq = entry.IndexOf('=');
        return eq >= 0 ? entry.Substring(eq + 1).Trim() : "";
    }

    public void LoadSurfaces()
    {
        _providers.Clear();
        _screens.Clear();
        _p.GridTerminalSystem.GetBlocksOfType(_providers, b => b.IsSameConstructAs(_p.Me) && b is IMyTextSurfaceProvider && MyIni.HasSection(b.CustomData, SEC));
        foreach (var b in _providers)
        {
            var sp = b as IMyTextSurfaceProvider;
            if (sp == null) continue;
            string[] names = new string[sp.SurfaceCount];
            MyIniParseResult r;
            _ini.Clear();
            _ini.TryParse(b.CustomData, SEC, out r);
            for (int i = 0; i < sp.SurfaceCount; i++)
                names[i] = _ini.Get(SEC, "Surface" + i).ToString(i < _cfg.Surface.Length ? _cfg.Surface[i] : "Blank");
            _screens[b.EntityId] = names;
        }
        if (!_screens.ContainsKey(_p.Me.EntityId))
            _screens[_p.Me.EntityId] = _cfg.Surface;
    }

    public void Update(double time, int tick)
    {
        foreach (var b in _providers)
        {
            var sp = b as IMyTextSurfaceProvider;
            if (sp == null || !b.IsWorking) continue;
            string[] names = _screens.ContainsKey(b.EntityId) ? _screens[b.EntityId] : _cfg.Surface;
            for (int i = 0; i < sp.SurfaceCount && i < names.Length; i++)
                Draw(sp.GetSurface(i), names[i], time, tick);
        }

        var pb = _p.Me as IMyTextSurfaceProvider;
        if (pb != null)
        {
            string[] names = _screens.ContainsKey(_p.Me.EntityId) ? _screens[_p.Me.EntityId] : _cfg.Surface;
            Draw(pb.GetSurface(0), names[0], time, tick);
        }
    }

    void Draw(IMyTextSurface s, string name, double time, int tick)
    {
        s.ContentType = ContentType.SCRIPT;
        s.Script = "";
        using (var frame = s.DrawFrame())
        {
            _hud.Begin(s, frame);
            if (time < _bootUntil)
            {
                DrawBoot(time);
                return;
            }
            string page = name;
            if ((name ?? "").Equals("Menu", StringComparison.OrdinalIgnoreCase) || (name ?? "").Equals("MenuView", StringComparison.OrdinalIgnoreCase))
                page = _menuPage;
            string n = (page ?? "Blank").ToLower();
            if (n == "power") DrawPower();
            else if (n == "weight") DrawWeight();
            else if (n == "cargoore" || n == "orecargo") DrawCargo();
            else if (n == "menu" || n == "menuview") DrawMenu();
            else if (n == "utility") DrawUtility();
            else if (n == "boot" || n == "splash" || n == "hogos") DrawBoot(time);
        }
    }

    void Header(string title)
    {
        _hud.Text(_hud.Margin, _hud.Margin, "HogOS2", _hud.TitleScale, _hud.Primary);
        _hud.TextRight(_hud.W - _hud.Margin, _hud.Margin + 2, title, _hud.SmallScale, _hud.Secondary);
        _hud.Text(_hud.Margin, _hud.Margin + _hud.LineH, ShortText(_cfg.ShipName, 22), _hud.SmallScale, _hud.Secondary);
        _hud.Line(_hud.Margin, _hud.Margin + _hud.LineH * 2, _hud.W - _hud.Margin * 2, _hud.Secondary);
        _hud.Y = _hud.Margin + _hud.LineH * 2 + _hud.Gap;
    }

    void Row(string a, string b, Color c)
    {
        if (_hud.Y > _hud.H - _hud.LineH) return;
        _hud.Text(_hud.Margin, _hud.Y, a, _hud.RowScale, _hud.Secondary);
        _hud.TextRight(_hud.W - _hud.Margin, _hud.Y, ShortText(b, _hud.ValueChars), _hud.RowScale, c);
        _hud.Y += _hud.LineH;
        _hud.Line(_hud.Margin, _hud.Y - 2, _hud.W - _hud.Margin * 2, _hud.Faint);
        _hud.Y += _hud.Gap;
    }

    void Bar(string label, double val, Color c)
    {
        Row(label, (val * 100).ToString("0.0") + "%", c);
        _hud.Bar(_hud.Margin, _hud.Y, _hud.W - _hud.Margin * 2, _hud.BarH, val, c);
        _hud.Y += _hud.BarH + _hud.Gap;
    }

    int MenuCount()
    {
        int count = 0;
        for (int i = 0; i < _cfg.Menu.Length; i++)
            if (!string.IsNullOrWhiteSpace(_cfg.Menu[i])) count++;
        return count;
    }

    string MenuEntryAt(int index)
    {
        int visible = 0;
        for (int i = 0; i < _cfg.Menu.Length; i++)
        {
            string entry = _cfg.Menu[i];
            if (string.IsNullOrWhiteSpace(entry)) continue;
            if (visible == index) return entry;
            visible++;
        }
        return "";
    }

    string MenuLabel(string entry)
    {
        int eq = entry.IndexOf('=');
        return eq >= 0 ? entry.Substring(0, eq).Trim() : entry.Trim();
    }

    string MenuCommand(string entry)
    {
        int eq = entry.IndexOf('=');
        return eq >= 0 ? entry.Substring(eq + 1).Trim() : "";
    }

    void DrawMenu()
    {
        Header("MENU");
        int count = MenuCount();
        if (count == 0)
        {
            Row("Menu", "No entries", _hud.Dim);
            return;
        }
        if (_menuIndex >= count) _menuIndex = count - 1;
        int maxRows = _hud.W >= 512 ? 7 : 6;
        int first = _menuIndex - maxRows / 2;
        if (first < 0) first = 0;
        if (first + maxRows > count) first = Math.Max(0, count - maxRows);
        int visible = 0;
        for (int i = 0; i < _cfg.Menu.Length; i++)
        {
            string entry = _cfg.Menu[i];
            if (string.IsNullOrWhiteSpace(entry)) continue;
            if (visible >= first && visible < first + maxRows)
            {
                bool selected = visible == _menuIndex;
                float y = _hud.Y;
                float rowH = _hud.MenuLineH;
                if (selected) _hud.Rect(_hud.Margin, y - 2, _hud.W - _hud.Margin * 2, rowH, _hud.Faint);
                string label = (selected ? "> " : "  ") + MenuLabel(entry);
                _hud.Text(_hud.Margin, y, ShortText(label, _hud.MenuChars), _hud.MenuScale, selected ? _hud.Primary : _hud.Secondary);
                _hud.Y += rowH;
                _hud.Line(_hud.Margin, _hud.Y - 2, _hud.W - _hud.Margin * 2, selected ? _hud.Primary : _hud.Faint);
                _hud.Y += Math.Max(1f, _hud.Gap * 0.35f);
            }
            visible++;
        }
    }

    void UpdateLiftStats()
    {
        _shipMass = 0;
        _liftNeed = 0;
        _liftHave = 0;
        _liftCount = 0;
        _liftWorking = 0;
        _liftUse = 0;

        if (_flight.Controller != null)
        {
            _shipMass = _flight.Controller.CalculateShipMass().PhysicalMass;
            _liftNeed = _shipMass * _flight.Controller.GetNaturalGravity().Length();
        }

        _liftThrusters.Find(_cfg.LiftThrusters);
        _liftCount = _liftThrusters.Blocks.Count;
        foreach (var t in _liftThrusters.Blocks)
        {
            if (!t.IsWorking) continue;
            _liftWorking++;
            _liftHave += t.MaxEffectiveThrust;
        }
        _liftUse = _liftHave > 0 ? _liftNeed / _liftHave : 0;
        _liftHistory[_liftHistoryAt] = _liftUse;
        _liftHistoryAt = (_liftHistoryAt + 1) % _liftHistory.Length;
    }

    void LiftGraph()
    {
        float x = _hud.Margin;
        float y = _hud.Y;
        float w = _hud.W - _hud.Margin * 2;
        float h = _hud.W >= 512 ? 42 : 28;
        _hud.Text(x, y, "Lift Graph", _hud.SmallScale, _hud.Secondary);
        y += _hud.LineH;
        _hud.Rect(x, y, w, h, new Color(_hud.Secondary.R, _hud.Secondary.G, _hud.Secondary.B, 35));
        _hud.Line(x, y + h * (1f - _cfg.LiftWarning), w, _hud.Fill);
        float gap = 1;
        float bw = (w - gap * (_liftHistory.Length - 1)) / _liftHistory.Length;
        for (int i = 0; i < _liftHistory.Length; i++)
        {
            int idx = (_liftHistoryAt + i) % _liftHistory.Length;
            double v = MathHelper.Clamp((float)_liftHistory[idx], 0f, 1.2f);
            float bh = (float)Math.Min(h, h * v);
            Color c = v > _cfg.LiftWarning ? _hud.Danger : _hud.Primary;
            _hud.Rect(x + i * (bw + gap), y + h - bh, bw, bh, c);
        }
        _hud.Y = y + h + 6;
    }

    string LiftStatus()
    {
        if (_flight.Controller == null) return "NO CONTROLLER";
        if (_liftCount == 0) return "NO LIFT THRUSTERS";
        if (_liftWorking == 0) return "LIFT OFFLINE";
        if (_liftUse >= _cfg.LiftCutoff) return "OVERLOAD";
        if (_liftUse >= _cfg.LiftWarning) return "HEAVY";
        return "READY";
    }

    void DrawPower()
    {
        Header("POWER");
        Bar("Battery", _power.Battery, _power.Battery < _cfg.BatteryReturn ? _hud.Danger : _hud.Primary);
        Row("State", _power.BatteryState, _power.BatteryDelta < 0 ? _hud.Danger : _hud.Ok);
        Row("Net", _power.BatteryDelta.ToString("+0.00;-0.00;0.00") + " MW", _power.BatteryDelta < 0 ? _hud.Danger : _hud.Ok);
        Bar("Uranium", _power.Uranium, _power.Uranium < 0.1 ? _hud.Danger : _hud.Primary);
        Row("Reactors", _power.ReactorOnline + "/" + _power.ReactorCount, _hud.Dim);
    }

    void DrawWeight()
    {
        UpdateLiftStats();
        double cargo = _cargo.FillRatio;
        float margin = _hud.W >= 512 ? 24f : 12f;
        float max = Math.Min(_hud.W, _hud.H);
        bool roverMode = _liftCount == 0;
        string cargoText = cargo >= _cfg.CargoReturn ? "Cargo return" : "Cargo";
        if (roverMode)
        {
            float gauge = Math.Min(_hud.W, _hud.H) * 0.62f;
            Vector2 pos = new Vector2((_hud.W - gauge) / 2f, (_hud.H - gauge) / 2f - _hud.LineH * 0.4f);
            _hud.FullRadial(new Vector2(pos.X, pos.Y), new Vector2(gauge, gauge), cargo, "", 48);
            _hud.TextCenter(_hud.W / 2f, pos.Y + gauge + _hud.LineH * 0.35f, cargoText, _hud.RowScale * 1.25f, _hud.Secondary);
        }
        else
        {
            bool wide = _hud.W >= _hud.H * 1.25f;
            float gauge = wide ? Math.Min(_hud.H * 0.50f, (_hud.W - margin * 3f) / 2f) : Math.Min(_hud.W * 0.48f, (_hud.H - margin * 3f) / 2f);
            if (wide)
            {
                float y = (_hud.H - gauge) / 2f - _hud.LineH * 0.35f;
                float leftX = _hud.W * 0.25f - gauge / 2f;
                float rightX = _hud.W * 0.75f - gauge / 2f;
                _hud.FullRadial(new Vector2(leftX, y), new Vector2(gauge, gauge), _liftUse, "", 42);
                _hud.FullRadial(new Vector2(rightX, y), new Vector2(gauge, gauge), cargo, "", 42);
                _hud.TextCenter(leftX + gauge / 2f, y + gauge + _hud.LineH * 0.35f, "Lift", _hud.RowScale * 1.2f, _hud.Secondary);
                _hud.TextCenter(rightX + gauge / 2f, y + gauge + _hud.LineH * 0.35f, cargoText, _hud.RowScale * 1.2f, _hud.Secondary);
            }
            else
            {
                float x = (_hud.W - gauge) / 2f;
                float topY = margin;
                float bottomY = _hud.H - margin - gauge - _hud.LineH * 1.2f;
                _hud.FullRadial(new Vector2(x, topY), new Vector2(gauge, gauge), _liftUse, "", 36);
                _hud.TextCenter(_hud.W / 2f, topY + gauge + _hud.LineH * 0.25f, "Lift", _hud.RowScale * 1.15f, _hud.Secondary);
                _hud.Line(margin, _hud.H / 2f, _hud.W - margin * 2, _hud.Secondary);
                _hud.FullRadial(new Vector2(x, bottomY), new Vector2(gauge, gauge), cargo, "", 36);
                _hud.TextCenter(_hud.W / 2f, bottomY + gauge + _hud.LineH * 0.25f, cargoText, _hud.RowScale * 1.15f, _hud.Secondary);
            }
            if (_liftUse > _cfg.LiftWarning && _tickFlash()) _hud.SpriteCentered(new Vector2(_hud.W - margin - 24f, margin + 24f), new Vector2(48f, 48f), "Danger", _hud.Danger);
        }
    }

    bool _tickFlash()
    {
        return (_p.Runtime.UpdateFrequency & UpdateFrequency.Update10) != 0;
    }

    void DrawCargo()
    {
        Header("ORE");
        Bar("Cargo", _cargo.FillRatio, _cargo.FillRatio > _cfg.CargoReturn ? _hud.Danger : _hud.Primary);
        int count = 0;
        foreach (var kv in _cargo.Ores.OrderByDescending(x => x.Value))
        {
            if (count++ >= 5) break;
            Row(kv.Key, FormatMass(kv.Value), _hud.Primary);
        }
        if (!_cargo.HasOre) Row("Ore", "None", _hud.Dim);
    }

    void DrawUtility()
    {
        Header("UTILITY");
        UpdateLiftStats();
        Row("Controller", _flight.Controller == null ? "Missing" : _flight.Controller.CustomName, _flight.Controller == null ? _hud.Danger : _hud.Dim);
        Row("Lift Thrusters", _liftWorking + "/" + _liftCount + " " + LiftStatus(), _liftWorking == _liftCount && _liftCount > 0 ? _hud.Ok : _hud.Danger);
        Row("Gravity Align", (_flight.AlignOn ? "ON " : "OFF ") + _flight.AlignPitch.ToString("0"), _flight.AlignOn ? _hud.Ok : _hud.Dim);
        Row("Cruise", (_flight.CruiseOn ? "ON " : "OFF ") + _flight.CruiseTarget.ToString("0.0") + " m/s", _flight.CruiseOn ? _hud.Ok : _hud.Dim);
        Row("Mining Mode", _flight.MiningMode == "" ? "Manual" : _flight.MiningMode, _flight.MiningMode == "" ? _hud.Dim : _hud.Ok);
        Row("Drills", _drills.DrillCount + " " + (_drills.DrillsOn ? "ON" : "OFF") + (_drills.TerrainMode ? " TERRAIN" : ""), _drills.DrillsOn ? _hud.Ok : _hud.Dim);
        Row("Status", _flight.Status == "" ? "OK" : _flight.Status, _flight.Status == "" ? _hud.Ok : _hud.Danger);
    }

    void DrawBoot(double time)
    {
        double len = Math.Max(1, _bootUntil - _bootStart);
        double progress = MathHelper.Clamp((float)((time - _bootStart) / len), 0f, 1f);
        string stage = BootPhrase(progress);
        float cx = _hud.W / 2;
        float margin = _hud.W >= 512 ? 24f : 10f;
        float bottom = _hud.H - margin;
        float titleScale = _hud.W >= 512 ? 1.25f : 0.86f;
        float versionScale = _hud.W >= 512 ? 0.42f : 0.30f;
        _hud.Line(margin, margin, _hud.W * 0.18f, _hud.Primary);
        _hud.Line(_hud.W - margin - _hud.W * 0.18f, margin, _hud.W * 0.18f, _hud.Primary);
        _hud.TextCenter(cx, margin + (_hud.W >= 512 ? 10f : 7f), "HogOS2", titleScale, _hud.Primary);
        _hud.TextCenter(cx, margin + (_hud.W >= 512 ? 48f : 35f), VERSION, versionScale, _hud.Secondary);

        float logo = Math.Min(_hud.H * 0.46f, _hud.W * 0.44f);
        Vector2 center = new Vector2(cx, _hud.H * 0.48f);
        _hud.SpriteCentered(center, new Vector2(logo, logo), "Textures\\FactionLogo\\Miners\\MinerIcon_3.dds", _hud.Primary);
        float textBase = center.Y + logo * 0.52f;
        _hud.TextCenter(cx, textBase, "MINING OPERATING SYSTEM", _hud.W >= 512 ? 0.44f : 0.30f, _hud.Secondary);
        _hud.TextCenter(cx, textBase + _hud.LineH * 0.9f, ShortText(_cfg.ShipName, 24), _hud.W >= 512 ? 0.40f : 0.28f, _hud.Secondary);

        float barH = _hud.W >= 512 ? 14f : 9f;
        float barY = bottom - barH;
        _hud.Text(margin, barY - _hud.LineH * 1.3f, stage + "...", _hud.W >= 512 ? 0.46f : 0.30f, _hud.Secondary);
        _hud.TextRight(_hud.W - margin, barY - _hud.LineH * 1.3f, (progress * 100).ToString("0") + "%", _hud.W >= 512 ? 0.46f : 0.30f, _hud.Primary);
        _hud.Bar(margin, barY, _hud.W - margin * 2, barH, progress, _hud.Primary);
    }

    void BootStatus(float x, float y, string label, string value, Color color)
    {
        float w = (_hud.W - _hud.Margin * 2 - _hud.Gap) / 2;
        _hud.Text(x, y, label, _hud.SmallScale, _hud.Secondary);
        _hud.TextRight(x + w, y, value, _hud.SmallScale, color);
    }

    string BootPhrase(double progress)
    {
        if (progress > 0.85) return "Systems online";
        if (progress > 0.68) return "Loading displays";
        if (progress > 0.50) return "Checking flight systems";
        if (progress > 0.32) return "Checking cargo";
        if (progress > 0.15) return "Checking power";
        return "Booting";
    }

    string ShortText(string text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max) return text;
        if (max <= 1) return text.Substring(0, max);
        return text.Substring(0, max - 1) + ".";
    }

    string FormatMass(double kg)
    {
        if (kg >= 1000000) return (kg / 1000000).ToString("0.00") + "Mt";
        if (kg >= 1000) return (kg / 1000).ToString("0.00") + "t";
        return kg.ToString("0") + "kg";
    }

    string FormatForce(double n)
    {
        if (n >= 1000000) return (n / 1000000).ToString("0.00") + "MN";
        if (n >= 1000) return (n / 1000).ToString("0.00") + "kN";
        return n.ToString("0") + "N";
    }
}

class Hud
{
    IMyTextSurface _s;
    MySpriteDrawFrame _f;
    Vector2 _off;
    public float W, H, Y;
    public float Margin, Gap, LineH, BarH, RowScale, SmallScale, TitleScale, BootTitleScale, MenuScale, MenuLineH;
    public int ValueChars, MenuChars;
    public Color Bg = new Color(1, 8, 13);
    public Color Panel = new Color(2, 18, 28);
    public Color Primary = new Color(126, 246, 255);
    public Color Accent = new Color(38, 239, 255);
    public Color Secondary = new Color(44, 177, 195);
    public Color Faint = new Color(44, 177, 195, 55);
    public Color Dim = new Color(44, 177, 195);
    public Color Fill = new Color(255, 204, 36);
    public Color Danger = new Color(255, 79, 66);
    public Color Ok = new Color(97, 255, 214);

    public void Begin(IMyTextSurface s, MySpriteDrawFrame f)
    {
        _s = s;
        _f = f;
        _off = (s.TextureSize - s.SurfaceSize) / 2f;
        W = s.SurfaceSize.X;
        H = s.SurfaceSize.Y;
        s.Script = "";
        s.ContentType = ContentType.SCRIPT;
        Bg = Mix(s.ScriptBackgroundColor, s.ScriptForegroundColor, 0.035f, 255);
        Primary = s.ScriptForegroundColor;
        Accent = Primary;
        Panel = Mix(Bg, Primary, 0.08f, 255);
        Secondary = Mix(Bg, Primary, 0.78f, 255);
        Dim = Secondary;
        Faint = Mix(Bg, Primary, 0.48f, 100);
        Ok = Mix(Primary, new Color(80, 255, 180), 0.55f, 255);
        Fill = new Color(255, 204, 36);
        Danger = new Color(255, 79, 66);
        Margin = W >= 512 ? 25f : 7f;
        Gap = W >= 512 ? 8f : 2f;
        LineH = W >= 512 ? 24f : 17f;
        BarH = W >= 512 ? 10f : 7f;
        RowScale = W >= 512 ? 0.56f : 0.40f;
        SmallScale = W >= 512 ? 0.40f : 0.28f;
        TitleScale = W >= 512 ? 0.66f : 0.48f;
        BootTitleScale = W >= 512 ? 0.88f : 0.62f;
        ValueChars = W >= 512 ? 26 : 15;
        MenuScale = W >= 512 ? 0.62f : 0.43f;
        MenuLineH = W >= 512 ? 28f : 20f;
        MenuChars = W >= 512 ? 34 : 24;
        Rect(0, 0, W, H, Bg);
    }

    Color Mix(Color a, Color b, float t, int alpha)
    {
        t = MathHelper.Clamp(t, 0f, 1f);
        return new Color(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t),
            alpha);
    }

    public void Text(float x, float y, string text, float scale, Color color)
    {
        var sp = MySprite.CreateText(text, _s.Font, color, scale, TextAlignment.LEFT);
        sp.Position = new Vector2(x, y) + _off;
        _f.Add(sp);
    }

    public void TextRight(float x, float y, string text, float scale, Color color)
    {
        var sp = MySprite.CreateText(text, _s.Font, color, scale, TextAlignment.RIGHT);
        sp.Position = new Vector2(x, y) + _off;
        _f.Add(sp);
    }

    public void TextCenter(float x, float y, string text, float scale, Color color)
    {
        var sp = MySprite.CreateText(text, _s.Font, color, scale, TextAlignment.CENTER);
        sp.Position = new Vector2(x, y) + _off;
        _f.Add(sp);
    }

    public void Rect(float x, float y, float w, float h, Color color, float rotation = 0f)
    {
        _f.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
            position: new Vector2(x + w / 2, y + h / 2) + _off,
            size: new Vector2(w, h), color: color, rotation: rotation));
    }

    public void SpriteCentered(Vector2 position, Vector2 size, string sprite, Color color, float rotation = 0f)
    {
        _f.Add(new MySprite(SpriteType.TEXTURE, sprite,
            position: position + _off, size: size, color: color, rotation: rotation));
    }

    public void Line(float x, float y, float w, Color color)
    {
        Rect(x, y, w, W >= 512 ? 2.4f : 1.8f, color);
    }

    public void Bar(float x, float y, float w, float h, double val, Color color)
    {
        val = MathHelper.Clamp((float)val, 0f, 1f);
        Rect(x, y, w, h, Faint);
        Rect(x + 1, y + 1, (float)((w - 2) * val), h - 2, color);
    }

    public void Radial(Vector2 position, Vector2 size, double raw, string subText, int bars, bool flip)
    {
        float value = MathHelper.Clamp((float)raw, 0f, 1f);
        Color secondary = new Color(Secondary.R, Secondary.G, Secondary.B, 42);
        Vector2 barSize = new Vector2(size.X / 12.8f, size.X / 64f);
        float radius = (size.X - barSize.X) / 2f;
        float fontSize = 0.35f + size.X / 360f;
        Vector2 origin = new Vector2(position.X + radius, flip ? position.Y + barSize.Y : position.Y + size.Y);
        float dir = flip ? -1f : 1f;
        for (int i = 0; i <= bars; i++)
        {
            float angle = MathHelper.ToRadians((180f / bars) * i);
            Vector2 barPos = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle) * dir) * radius + origin;
            Color color = ((float)i / bars) < value ? Primary : secondary;
            _f.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                position: barPos + _off, size: barSize, color: color, rotation: angle * dir));
        }
        string pct = (value * 100f).ToString("0") + "%";
        float centerX = position.X + size.X / 2f;
        float labelY = flip ? position.Y + size.Y - size.Y * 0.46f : position.Y + size.Y * 0.34f;
        TextCenter(centerX, labelY, pct, fontSize * 1.25f, value >= 0.9f ? Danger : Primary);
        TextCenter(centerX, flip ? labelY + LineH * 0.75f : labelY - LineH * 0.75f, subText, fontSize * 0.68f, Secondary);
    }

    public void FullRadial(Vector2 position, Vector2 size, double raw, string subText, int bars)
    {
        float value = MathHelper.Clamp((float)raw, 0f, 1f);
        Color secondary = new Color(Secondary.R, Secondary.G, Secondary.B, 42);
        Vector2 barSize = new Vector2(size.X / 12.8f, size.X / 64f);
        float radius = (Math.Min(size.X, size.Y) - barSize.X) / 2f;
        Vector2 origin = position + size / 2f;
        for (int i = 0; i <= bars; i++)
        {
            float angle = MathHelper.ToRadians((360f / bars) * i);
            Vector2 barPos = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius + origin;
            Color color = ((float)i / bars) < value ? Primary : secondary;
            _f.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                position: barPos + _off, size: barSize, color: color, rotation: angle));
        }
        TextCenter(origin.X, origin.Y - LineH * 0.55f, (value * 100f).ToString("0") + "%", RowScale * 1.8f, value >= 0.9f ? Danger : Primary);
        if (!string.IsNullOrWhiteSpace(subText))
            TextCenter(origin.X, origin.Y + LineH * 0.85f, subText, RowScale, Secondary);
    }
}
