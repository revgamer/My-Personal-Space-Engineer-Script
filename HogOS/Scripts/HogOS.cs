// ============================================================
// HogOS - Hog Operating System
// Author: RevGamer
// Version: 2.0-clean
//
// Profiles:
//   GroundHog = atmospheric miner
//   SpaceHog  = ion miner
//   HydroHog  = hydrogen miner
//
// Cockpit default, 4 screens:
//   Surface0=Power
//   Surface1=CargoOre
//   Surface2=Weight
//   Surface3=Utility
//
// Commands:
//   toggle_gaa
//   set_gaa_pitch 0
//   set_gaa_pitch +5
//   cruise on
//   cruise off
//   cruise 20
//   cruise +5
//   toggle_cruise
//   set_cruise 20
// ============================================================

const string VERSION = "2.0-clean";
const string SEC = "HogOS";

MyIni _ini = new MyIni();
HogConfig _cfg = new HogConfig();
Hud _hud;
Flight _flight;
PowerSys _power;
CargoSys _cargo;
ScreenSys _screens;
double _time;
int _tick;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
    EnsureCustomData();
    LoadConfig();
    _hud = new Hud();
    _flight = new Flight(this, _cfg);
    _power = new PowerSys(this, _cfg);
    _cargo = new CargoSys(this, _cfg);
    _screens = new ScreenSys(this, _cfg, _hud, _flight, _power, _cargo);
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
        Echo("HogOS " + VERSION + "\n" + _cfg.ShipName + " / " + _cfg.Profile);
        Echo("Cruise: " + (_flight.CruiseOn ? "ON " : "OFF ") + _flight.CruiseTarget.ToString("0.0") + " m/s");
        Echo("Level: " + (_flight.AlignOn ? "ON " : "OFF ") + _flight.AlignPitch.ToString("0") + " deg");
    }

    if ((updateSource & UpdateType.Update10) != 0)
    {
        _tick++;
            _flight.Update();
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
    string[] p = arg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    if (p.Length == 0) return;
    string cmd = p[0].ToLower();

    if (cmd == "toggle_gaa")
    {
        _flight.AlignOn = !_flight.AlignOn;
        if (!_flight.AlignOn) _flight.ReleaseGyros();
        return;
    }
    if (cmd == "set_gaa_pitch" && p.Length > 1)
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
    if (cmd == "toggle_cruise")
    {
        _flight.SetCruise(!_flight.CruiseOn);
        return;
    }
    if (cmd == "set_cruise" && p.Length > 1)
    {
        SetCruiseValue(p[1]);
        return;
    }
    if (cmd == "cruise")
    {
        if (p.Length == 1) _flight.SetCruise(!_flight.CruiseOn);
        else if (p[1].ToLower() == "on") _flight.SetCruise(true);
        else if (p[1].ToLower() == "off") _flight.SetCruise(false);
        else SetCruiseValue(p[1]);
    }
}

void SetCruiseValue(string raw)
{
    float v;
    if (!float.TryParse(raw, out v)) return;
    if (raw[0] == '+' || raw[0] == '-') _flight.CruiseTarget += v;
    else _flight.CruiseTarget = v;
    if (_flight.CruiseTarget < 0) _flight.CruiseTarget = 0;
    _flight.SetCruise(_flight.CruiseTarget > 0);
}

void LoadConfig()
{
    MyIniParseResult r;
    _ini.Clear();
    if (_ini.TryParse(Me.CustomData, out r)) _cfg.Read(_ini);
    _flight?.ApplyConfig(_cfg);
    _screens?.LoadSurfaces();
}

void EnsureCustomData()
{
    if (MyIni.HasSection(Me.CustomData, SEC) && MyIni.HasSection(Me.CustomData, "Groups")) return;

    MyIni oldIni = new MyIni();
    MyIniParseResult r;
    if (oldIni.TryParse(Me.CustomData, out r))
    {
        _cfg.ShipName = oldIni.Get(SEC, "ShipName").ToString(_cfg.ShipName);
        _cfg.Profile = oldIni.Get(SEC, "Profile").ToString(_cfg.Profile);
        _cfg.Theme = oldIni.Get(SEC, "Theme").ToString(_cfg.Theme);
    }

    MyIni n = new MyIni();
    _cfg.WriteDefaults(n);
    Me.CustomData = n.ToString();
}

class HogConfig
{
    public string ShipName = "GroundHog 1";
    public string Profile = "GroundHog";
    public string Theme = "AGM";
    public string LiftThrusters = "[RGH] Lift Thrusters";
    public string StopThrusters = "[RGH] Brake Thrusters";
    public string ForwardThrusters = "[RGH] Cruising Thrusters";
    public string ReverseThrusters = "[RGH] Brake Thrusters";
    public string CargoTrack = "";
    public string AlignGyros = "[RGH] Gyros";
    public string Controller = "";
    public float LiftWarning = 0.90f;
    public float LiftCutoff = 0.98f;
    public float CargoReturn = 0.90f;
    public float BatteryReturn = 0.20f;
    public float HydrogenReturn = 0.25f;
    public bool CruiseEnabled = false;
    public float CruiseSpeed = 20f;
    public bool UseReverseThrusters = true;
    public string[] Surface = new string[] { "Power", "CargoOre", "Weight", "Utility" };

    public void WriteDefaults(MyIni ini)
    {
        ini.Set(SEC, "ShipName", ShipName);
        ini.Set(SEC, "Profile", Profile);
        ini.Set(SEC, "Theme", Theme);
        for (int i = 0; i < Surface.Length; i++) ini.Set(SEC, "Surface" + i, Surface[i]);
        ini.Set("Groups", "LiftThrusters", LiftThrusters);
        ini.Set("Groups", "StopThrusters", StopThrusters);
        ini.Set("Groups", "ForwardThrusters", ForwardThrusters);
        ini.Set("Groups", "ReverseThrusters", ReverseThrusters);
        ini.Set("Groups", "CargoTrack", CargoTrack);
        ini.Set("Groups", "AlignGyros", AlignGyros);
        ini.Set("Blocks", "Controller", Controller);
        ini.Set("Safety", "LiftWarning", LiftWarning);
        ini.Set("Safety", "LiftCutoff", LiftCutoff);
        ini.Set("Safety", "CargoReturn", CargoReturn);
        ini.Set("Safety", "BatteryReturn", BatteryReturn);
        ini.Set("Safety", "HydrogenReturn", HydrogenReturn);
        ini.Set("Cruise", "Enabled", CruiseEnabled);
        ini.Set("Cruise", "TargetSpeed", CruiseSpeed);
        ini.Set("Cruise", "UseReverseThrusters", UseReverseThrusters);
    }

    public void Read(MyIni ini)
    {
        ShipName = ini.Get(SEC, "ShipName").ToString(ShipName);
        Profile = ini.Get(SEC, "Profile").ToString(Profile);
        Theme = ini.Get(SEC, "Theme").ToString(Theme);
        for (int i = 0; i < Surface.Length; i++)
            Surface[i] = ini.Get(SEC, "Surface" + i).ToString(Surface[i]);
        LiftThrusters = ini.Get("Groups", "LiftThrusters").ToString(LiftThrusters);
        StopThrusters = ini.Get("Groups", "StopThrusters").ToString(StopThrusters);
        ForwardThrusters = ini.Get("Groups", "ForwardThrusters").ToString(ForwardThrusters);
        ReverseThrusters = ini.Get("Groups", "ReverseThrusters").ToString(ReverseThrusters);
        CargoTrack = ini.Get("Groups", "CargoTrack").ToString(CargoTrack);
        AlignGyros = ini.Get("Groups", "AlignGyros").ToString(AlignGyros);
        Controller = ini.Get("Blocks", "Controller").ToString(Controller);
        LiftWarning = ini.Get("Safety", "LiftWarning").ToSingle(LiftWarning);
        LiftCutoff = ini.Get("Safety", "LiftCutoff").ToSingle(LiftCutoff);
        CargoReturn = ini.Get("Safety", "CargoReturn").ToSingle(CargoReturn);
        BatteryReturn = ini.Get("Safety", "BatteryReturn").ToSingle(BatteryReturn);
        HydrogenReturn = ini.Get("Safety", "HydrogenReturn").ToSingle(HydrogenReturn);
        CruiseEnabled = ini.Get("Cruise", "Enabled").ToBoolean(CruiseEnabled);
        CruiseSpeed = ini.Get("Cruise", "TargetSpeed").ToSingle(CruiseSpeed);
        UseReverseThrusters = ini.Get("Cruise", "UseReverseThrusters").ToBoolean(UseReverseThrusters);
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
        if (!CruiseOn) CruiseOn = cfg.CruiseEnabled;
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
        if (!on)
        {
            foreach (var t in _forward.Blocks) t.ThrustOverridePercentage = 0;
            foreach (var t in _reverse.Blocks) { t.ThrustOverridePercentage = 0; t.Enabled = true; }
        }
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
            if (!string.IsNullOrEmpty(_cfg.Controller) && c.CustomName == _cfg.Controller)
            {
                Controller = c;
                break;
            }
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
        if (grav.Length() < 0.01) return;
        grav.Normalize();

        Matrix ctrlOri;
        Controller.Orientation.GetMatrix(out ctrlOri);
        Vector3D down = ctrlOri.Down;
        if (AlignPitch < 0) down = Vector3D.Lerp(ctrlOri.Down, ctrlOri.Forward, -AlignPitch / 90f);
        if (AlignPitch > 0) down = Vector3D.Lerp(ctrlOri.Down, -ctrlOri.Forward, AlignPitch / 90f);

        foreach (var gyro in _gyros.Blocks)
        {
            Matrix gyroOri;
            gyro.Orientation.GetMatrix(out gyroOri);
            Vector3D localDown = Vector3D.Transform(down, MatrixD.Transpose(gyroOri));
            Vector3D localGrav = Vector3D.Transform(grav, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));
            Vector3D rot = Vector3D.Cross(localDown, localGrav);
            double angle = Math.Atan2(rot.Length(), Math.Sqrt(Math.Max(0, 1 - rot.LengthSquared())));
            if (angle < 0.01) { gyro.GyroOverride = false; continue; }
            double maxYaw = gyro.GetMaximum<float>("Yaw");
            double speed = Math.Max(0.01, Math.Min(maxYaw, maxYaw * (angle / Math.PI) * 0.9));
            rot.Normalize();
            rot *= speed;
            gyro.SetValueFloat("Pitch", (float)rot.GetDim(0));
            gyro.SetValueFloat("Yaw", -(float)rot.GetDim(1));
            gyro.SetValueFloat("Roll", -(float)rot.GetDim(2));
            gyro.SetValueFloat("Power", 1f);
            gyro.GyroOverride = true;
        }
    }

    void Cruise()
    {
        if (CruiseTarget <= 0) return;
        double speed = Vector3D.Dot(Controller.GetShipVelocities().LinearVelocity, Controller.WorldMatrix.Forward);
        double err = CruiseTarget - speed;
        float amount = (float)MathHelper.Clamp(Math.Abs(err) / Math.Max(CruiseTarget, 1), 0, 1);
        foreach (var t in _forward.Blocks)
        {
            if (err > 0.25) { t.Enabled = true; t.ThrustOverridePercentage = amount; }
            else t.ThrustOverridePercentage = 0;
        }
        foreach (var t in _reverse.Blocks)
        {
            if (_cfg.UseReverseThrusters && err < -0.25) { t.Enabled = true; t.ThrustOverridePercentage = amount; }
            else t.ThrustOverridePercentage = 0;
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
    PowerSys _power;
    CargoSys _cargo;
    List<IMyTerminalBlock> _providers = new List<IMyTerminalBlock>();
    Dictionary<long, string[]> _screens = new Dictionary<long, string[]>();
    MyIni _ini = new MyIni();

    public ScreenSys(Program p, HogConfig cfg, Hud hud, Flight flight, PowerSys power, CargoSys cargo)
    {
        _p = p; _cfg = cfg; _hud = hud; _flight = flight; _power = power; _cargo = cargo;
        LoadSurfaces();
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
            string n = (name ?? "Blank").ToLower();
            if (n == "power") DrawPower();
            else if (n == "weight") DrawWeight();
            else if (n == "cargoore") DrawCargo();
            else if (n == "utility") DrawUtility();
        }
    }

    void Header(string title)
    {
        _hud.Text(5, 3, "HogOS", 0.42f, _hud.Accent);
        _hud.Text(5, 17, title, 0.34f, _hud.Primary);
        _hud.Text(5, 30, _cfg.ShipName, 0.24f, _hud.Dim);
        _hud.Line(5, 42, _hud.W - 10, _hud.Dim);
        _hud.Y = 48;
    }

    void Row(string a, string b, Color c)
    {
        if (_hud.Y > _hud.H - 15) return;
        _hud.Text(5, _hud.Y, a, 0.42f, _hud.Dim);
        _hud.TextRight(_hud.W - 5, _hud.Y, b, 0.42f, c);
        _hud.Y += 17;
    }

    void Bar(string label, double val, Color c)
    {
        Row(label, (val * 100).ToString("0.0") + "%", c);
        _hud.Bar(5, _hud.Y, _hud.W - 10, 9, val, c);
        _hud.Y += 15;
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
        Header("WEIGHT");
        double cargo = _cargo.FillRatio;
        double lift = 0;
        if (_flight.Controller != null)
        {
            double mass = _flight.Controller.CalculateShipMass().PhysicalMass;
            double need = mass * _flight.Controller.GetNaturalGravity().Length();
            double have = 0;
            BlockGroup<IMyThrust> liftThrusters = new BlockGroup<IMyThrust>(_p);
            liftThrusters.Find(_cfg.LiftThrusters);
            foreach (var t in liftThrusters.Blocks) if (t.IsWorking) have += t.MaxEffectiveThrust;
            lift = have > 0 ? need / have : 0;
        }
        Bar("Cargo", cargo, cargo > _cfg.CargoReturn ? _hud.Danger : _hud.Primary);
        Bar("Lift Use", lift, lift > _cfg.LiftWarning ? _hud.Danger : _hud.Primary);
        Row("Cruise", (_flight.CruiseOn ? "ON " : "OFF ") + _flight.CruiseTarget.ToString("0"), _flight.CruiseOn ? _hud.Ok : _hud.Dim);
        Row("Level", (_flight.AlignOn ? "ON " : "OFF ") + _flight.AlignPitch.ToString("0"), _flight.AlignOn ? _hud.Ok : _hud.Dim);
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
        Row("Controller", _flight.Controller == null ? "Missing" : _flight.Controller.CustomName, _flight.Controller == null ? _hud.Danger : _hud.Dim);
        Row("Gravity Align", (_flight.AlignOn ? "ON " : "OFF ") + _flight.AlignPitch.ToString("0"), _flight.AlignOn ? _hud.Ok : _hud.Dim);
        Row("Cruise", (_flight.CruiseOn ? "ON " : "OFF ") + _flight.CruiseTarget.ToString("0.0") + " m/s", _flight.CruiseOn ? _hud.Ok : _hud.Dim);
        Row("Status", _flight.Status == "" ? "OK" : _flight.Status, _flight.Status == "" ? _hud.Ok : _hud.Danger);
    }

    string FormatMass(double kg)
    {
        if (kg >= 1000000) return (kg / 1000000).ToString("0.00") + "Mt";
        if (kg >= 1000) return (kg / 1000).ToString("0.00") + "t";
        return kg.ToString("0") + "kg";
    }
}

class Hud
{
    IMyTextSurface _s;
    MySpriteDrawFrame _f;
    Vector2 _off;
    public float W, H, Y;
    public Color Bg = new Color(1, 8, 13);
    public Color Panel = new Color(2, 18, 28);
    public Color Primary = new Color(126, 246, 255);
    public Color Accent = new Color(38, 239, 255);
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
        Rect(0, 0, W, H, Bg);
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

    public void Rect(float x, float y, float w, float h, Color color)
    {
        _f.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
            position: new Vector2(x + w / 2, y + h / 2) + _off,
            size: new Vector2(w, h), color: color));
    }

    public void Line(float x, float y, float w, Color color)
    {
        Rect(x, y, w, 1.5f, color);
    }

    public void Bar(float x, float y, float w, float h, double val, Color color)
    {
        val = MathHelper.Clamp((float)val, 0f, 1f);
        Rect(x, y, w, h, new Color(Dim.R, Dim.G, Dim.B, 90));
        Rect(x + 1, y + 1, (float)((w - 2) * val), h - 2, color);
    }
}
