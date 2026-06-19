// ============================================================
// HogOS — Hog Operating System
// Author:  RevGamer (Simba "Davy" Jones)
// Version: 2.0
//
// Mining operations management for GroundHog & SpaceHog
//
// CHANGES v2.0:
//   - Reduced screen set: Splash, Loading, Power, OreCargo, Weight
//   - Utility and Drills screens removed
//   - DrillManager removed (dead code - CargoManager already folds
//     drill ore buffers into the combined cargo capacity gauge)
//   - Splash screen auto-switches to a dock status panel when a
//     connector tagged [HogOS-Dock] is connected; reverts on disconnect
//   - Logo changed to MinerIcon_3 (mining faction icon)
//
// SETUP — add to any LCD or Cockpit CustomData:
//   [HogOS]
//   Surface0=Splash
//   Surface1=Power
//   Surface2=OreCargo
//   Surface3=Weight
//
// SETUP — dock status panel (optional):
//   Tag a connector's name with [HogOS-Dock] to enable dock-aware
//   Splash screens. When that connector is locked or connectable,
//   any Splash surface shows connector status and the docked grid's
//   name instead of the boot logo.
//
// AVAILABLE SCREENS:
//   Splash    Boot logo (auto dock-status panel when docked)
//   Loading   Boot sequence (immersive cockpit entry)
//   Power     Battery / Reactor fuel / Net flow
//   OreCargo  Ore inventory
//   Weight    Lift thrust + cargo gauges
//   Blank     Turn off a surface
//
// COMMANDS:
//   toggle_gaa          Toggle gravity align
//   set_gaa_pitch +5    Adjust pitch offset
//   toggle_cruise       Toggle cruise control
//   set_cruise 20       Set cruise speed (m/s)
//   set_cruise +5       Adjust cruise speed
//   dump Iron           Toggle dump sorter for ore type
// ============================================================

private HogOS _os;
private MyIni _storage;

public Program()
{
    _storage = new MyIni();
    _storage.TryParse(Storage);
    _os = new HogOS(this, _storage);
    Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
}

public void Save()
{
    _os.Save();
    Storage = _storage.ToString();
}

public void Main(string argument, UpdateType updateSource)
{
    _os.Update(argument, updateSource, Runtime.TimeSinceLastRun);
}

// ============================================================
// HOG PAINTER
// Works in surface-local space (0,0 = top-left of surface).
// Painter adds _offset internally — never add vp.X/vp.Y outside.
// ============================================================

public class HogPainter
{
    private static IMyTextSurface    _surface;
    private static MySpriteDrawFrame _frame;
    private static Vector2           _offset;

    // Surface-local dimensions (use these for layout)
    public static float   Width;
    public static float   Height;
    public static Vector2 Center;       // surface-local centre
    public static Vector2 AvailableSize;

    public static Color Primary = new Color(255, 184,   0);
    public static Color Accent  = new Color(255, 224,  51);
    public static Color Dim     = new Color(100,  60,   0);
    public static Color Danger  = new Color(255,  60,   0);
    public static Color OK      = new Color( 80, 220, 100);

    // Read from LCD — user sets ScriptForegroundColor on LCD
    public static Color FGColor;
    public static Color BGColor;
    public static Color SecondaryColor;

    public static string LOGO = "Textures\\FactionLogo\\Miners\\MinerIcon_3.dds";

    private static StringBuilder _measSb = new StringBuilder(32);

    public static void Setup(IMyTextSurface surface, MySpriteDrawFrame frame)
    {
        _surface = surface;
        _frame   = frame;
        // Offset to centre the surface inside the texture atlas
        _offset  = (_surface.TextureSize - _surface.SurfaceSize) / 2f;

        Width         = _surface.SurfaceSize.X;
        Height        = _surface.SurfaceSize.Y;
        AvailableSize = new Vector2(Width, Height);
        // Center in surface-local coords (no offset added yet)
        Center        = AvailableSize / 2f;

        // Derive colours from LCD settings so user can tint
        FGColor      = surface.ScriptForegroundColor;
        BGColor      = surface.ScriptBackgroundColor;
        Vector3 hsv  = FGColor.ColorToHSV();
        SecondaryColor = hsv.Z < 0.5f
            ? Color.Lighten(FGColor, 0.3f)
            : Color.Darken(FGColor, 0.3f);

        // Hardcode our amber palette on top (keeps consistency)
        Primary = new Color(255, 184,  0);
        Accent  = new Color(255, 224, 51);
        Dim     = new Color(100,  60,  0);
        Danger  = new Color(255,  60,  0);
        OK      = new Color( 80, 220, 100);

        surface.ContentType = VRage.Game.GUI.TextPanel.ContentType.SCRIPT;
        surface.Script      = "";
    }

    // -- Coordinate helper --
    // All positions passed in are surface-local (0,0 = top-left).
    // TLC: top-left -> sprite centre (sprites need their centre coord + offset)
    static Vector2 TLC(Vector2 pos, Vector2 size) => pos + size / 2f + _offset;

    // -- Primitives (all coords surface-local) --

    public static void FilledRect(Vector2 pos, Vector2 size,
                                   Color color, float rot = 0f)
    {
        _frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
            size: size, color: color)
        {
            Position        = TLC(pos, size),
            RotationOrScale = rot
        });
    }

    public static void Border(Vector2 pos, Vector2 size,
                               Color color, float t = 1f)
    {
        FilledRect(pos,                                     new Vector2(size.X, t), color);
        FilledRect(new Vector2(pos.X, pos.Y + size.Y - t), new Vector2(size.X, t), color);
        FilledRect(pos,                                     new Vector2(t, size.Y), color);
        FilledRect(new Vector2(pos.X + size.X - t, pos.Y), new Vector2(t, size.Y), color);
    }

    // pos = surface-local top-left
    public static void Text(Vector2 pos, string text, float fs = 1f,
                             TextAlignment align = TextAlignment.LEFT,
                             Color? color = null)
    {
        var s = MySprite.CreateText(text, _surface.Font,
            color ?? Primary, fs, align);
        // Text sprites use their position directly + offset
        s.Position = pos + _offset;
        _frame.Add(s);
    }

    // Centred text at a surface-local point
    public static void TextCentered(Vector2 pos, string text, float fs = 1f,
                                     Color? color = null)
    {
        var s = MySprite.CreateText(text, _surface.Font,
            color ?? Primary, fs, TextAlignment.CENTER);
        s.Position = pos + _offset;
        _frame.Add(s);
    }

    // Right-aligned text -- pos is the right edge, surface-local
    public static void TextRight(Vector2 pos, string text, float fs = 1f,
                                  Color? color = null)
    {
        var s = MySprite.CreateText(text, _surface.Font,
            color ?? Primary, fs, TextAlignment.RIGHT);
        s.Position = pos + _offset;
        _frame.Add(s);
    }

    // Sprite with top-left pos, surface-local
    public static void Sprite(Vector2 pos, Vector2 size, string name,
                               Color? color = null, float rot = 0f)
    {
        _frame.Add(new MySprite(SpriteType.TEXTURE, name,
            size: size, color: color ?? Primary)
        {
            Position        = TLC(pos, size),
            RotationOrScale = rot
        });
    }

    // Sprite centred at a surface-local point
    public static void SpriteCentered(Vector2 center, Vector2 size, string name,
                                       Color? color = null, float rot = 0f)
    {
        _frame.Add(new MySprite(SpriteType.TEXTURE, name,
            size: size, color: color ?? Primary)
        {
            Position        = center + _offset,
            RotationOrScale = rot
        });
    }

    public static void ProgressBar(Vector2 pos, Vector2 size, float value,
                                    float border = 1f,
                                    Color? borderColor = null,
                                    Color? fillColor   = null,
                                    string icon        = "")
    {
        value = MathHelper.Clamp(value, 0f, 1f);
        Border(pos, size, borderColor ?? Dim, border);
        Vector2 inner = size - border * 2f;
        FilledRect(pos + border,
            new Vector2(inner.X * value, inner.Y), fillColor ?? Primary);
        if (icon != "")
        {
            Vector2 isz = new Vector2(inner.Y, inner.Y);
            Sprite(pos + border + new Vector2((inner.X - isz.X) / 2f, 0),
                isz, icon, Dim);
        }
    }

    public static void Divider(float y, float margin = 0f, Color? color = null)
    {
        FilledRect(new Vector2(margin, y),
            new Vector2(Width - margin * 2f, 1f), color ?? Dim);
    }

    public static Vector2 Measure(string text, float fs)
    {
        _measSb.Clear(); _measSb.Append(text);
        return _surface.MeasureStringInPixels(_measSb, _surface.Font, fs);
    }

    // -- Header --
    // All coords surface-local. Returns Y after header.
    public static float DrawHeader(MySpriteDrawFrame frame,
                                    float margin, string moduleName,
                                    float spinnerAngle, Color accentColor)
    {
        float y     = margin;
        float right = Width - margin;

        Text(new Vector2(margin, y), "HogOS",
            0.46f, TextAlignment.LEFT, Accent);
        TextRight(new Vector2(right, y), "v2.0",
            0.32f, new Color(Dim.R, Dim.G, Dim.B, 180));

        frame.Add(new MySprite(SpriteType.TEXTURE, "Screen_LoadingBar",
            size: new Vector2(11f, 11f),
            color: new Color(accentColor.R, accentColor.G, accentColor.B, 150))
        {
            Position        = new Vector2(right - 16f, y + 6f) + _offset,
            RotationOrScale = spinnerAngle
        });

        y += 12f;
        Text(new Vector2(margin, y), "  " + moduleName,
            0.34f, TextAlignment.LEFT, accentColor);
        y += 10f;
        Text(new Vector2(margin, y),
            "  RevGamer (Simba \"Davy\" Jones)",
            0.24f, TextAlignment.LEFT,
            new Color(Dim.R, Dim.G, Dim.B, 160));
        y += 9f;
        FilledRect(new Vector2(margin, y), new Vector2(Width - margin * 2f, 1.5f),
            new Color(accentColor.R, accentColor.G, accentColor.B, 90));
        y += 3f;

        return y; // ~34px
    }

    // -- Radial gauge -- ExcavOS Radial style --
    // pos/size in surface-local coords.
    // Arc origin at bottom-centre of bounding box (or top if flip=true).
    // bars sweep from left to right.
    public static void Radial(Vector2 pos, Vector2 size, float value,
                               string subText = "", int bars = 20,
                               bool flip = false)
    {
        value = MathHelper.Clamp(value, 0f, 1f);
        Color secondary = new Color(SecondaryColor.R, SecondaryColor.G,
                                     SecondaryColor.B, 25);

        // bar geometry -- matched to ExcavOS exactly
        Vector2 barSize = new Vector2(size.X / 256f * 20f, size.X / 128f * 4f);
        float radius    = (size.X - barSize.X) / 2f;
        float fontSize  = 0.5f + size.X / 256f;

        // origin: bottom-centre of bounding box (or top if flip)
        Vector2 origin = new Vector2(
            pos.X + radius,
            flip ? pos.Y + barSize.Y : pos.Y + size.Y);

        string text        = string.Format("{0:0.00}%", value * 100f);
        Vector2 mainTxtSz  = Measure(text, fontSize);
        Vector2 subTxtSz   = Measure(subText, fontSize / 2f);

        for (int n = 0; n <= bars; n++)
        {
            float angle    = -(float)Math.PI / 2f
                + (flip ? -n : n) * ((float)Math.PI / bars);
            float v        = (float)n / bars;
            float barScale = 0.2f + v * 0.8f;

            Vector2 barPos = new Vector2(
                (float)(radius * Math.Sin(angle)) + barSize.X / 2f,
                -(float)(radius * Math.Cos(angle)) - barSize.Y / 2f);

            _frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                size: new Vector2(barSize.X, barSize.Y * barScale),
                color: value > v ? Primary : secondary)
            {
                Position        = origin + barPos + _offset,
                RotationOrScale = angle + (float)Math.PI / 2f
            });

            // value text -- centred in bounding box
            Vector2 txtPos = new Vector2(
                pos.X + size.X / 2f,
                flip ? pos.Y : origin.Y - mainTxtSz.Y);
            TextCentered(txtPos, text, fontSize, Primary);

            // sub text
            Vector2 subPos = txtPos;
            subPos.Y += flip ? mainTxtSz.Y : -subTxtSz.Y;
            TextCentered(subPos, subText, fontSize / 2f, SecondaryColor);
        }
    }
}
// ============================================================
// BLOCK FINDER
// ============================================================

public class BlockFinder<T> where T : class
{
    private const double CACHE_SEC = 10.0;
    private readonly Program _p;
    private DateTime _lastFetch = DateTime.MinValue;
    public readonly List<T> blocks = new List<T>();

    public BlockFinder(Program p) { _p = p; }

    public void FindBlocks(bool sameConstruct = true,
                            Func<T, bool> filter = null,
                            string groupName = null)
    {
        if (blocks.Count > 0 &&
            (DateTime.Now - _lastFetch).TotalSeconds < CACHE_SEC) return;

        Func<T, bool> f = b =>
        {
            var tb = b as IMyTerminalBlock;
            if (tb != null && sameConstruct != tb.IsSameConstructAs(_p.Me))
                return false;
            return filter == null || filter(b);
        };

        _lastFetch = DateTime.Now;
        blocks.Clear();

        if (!string.IsNullOrEmpty(groupName))
            _p.GridTerminalSystem.GetBlockGroupWithName(groupName)
                ?.GetBlocksOfType(blocks, f);
        else
            _p.GridTerminalSystem.GetBlocksOfType(blocks, f);
    }

    public void ForEach(Action<T> cb) => blocks.ForEach(cb);
    public bool HasBlocks()           => blocks.Count > 0;
    public int  Count()               => blocks.Count;
}

// ============================================================
// PID
// ============================================================

public class PIDController
{
    public readonly double dt;
    public double min = -1.0, max = 1.0;
    public double Kp, Ki, Kd;
    private double _i, _lastErr;

    public PIDController(double dt) { this.dt = dt; }
    public void Reset() { _i = 0; _lastErr = 0; }

    public double Compute(double err)
    {
        double ni  = _i + err * dt;
        double der = err - _lastErr;
        _lastErr   = err;
        double cv  = Kp * err + Ki * ni + (Kd / dt) * der;
        if (cv > max) { if (ni <= _i) _i = ni; return max; }
        if (cv < min) { if (ni >= _i) _i = ni; return min; }
        _i = ni;
        return cv;
    }
}

// ============================================================
// BLOCK HELPER
// ============================================================

public static class BlockHelper
{
    public static bool IsHydrogenTank(IMyGasTank b)
        => b.BlockDefinition.SubtypeId.Contains("HydrogenTank");

    public static float GetReactorFuelLevel(IMyReactor r)
    {
        var inv = r.GetInventory(0);
        float mx = (float)inv.MaxVolume;
        return mx > 0 ? (float)inv.CurrentVolume / mx : 0f;
    }
}

// ============================================================
// CARGO MANAGER
// ============================================================

public class CargoEntry { public string TypeId; public double Amount; }

public class CargoManager
{
    private readonly BlockFinder<IMyTerminalBlock> _blocks;
    private readonly List<MyInventoryItem>         _items = new List<MyInventoryItem>();
    private readonly HogConfig                     _cfg;

    public bool   HasOre        = false;
    public bool   HasNonOre     = false;
    public double CurrentVolume = 0;
    public double MaxVolume     = 0;
    public IDictionary<string, CargoEntry> Cargo
        = new Dictionary<string, CargoEntry>();

    public CargoManager(Program p, HogConfig cfg)
    {
        _blocks = new BlockFinder<IMyTerminalBlock>(p);
        _cfg    = cfg;
    }

    public void QueryData()
    {
        _blocks.FindBlocks(true, b =>
        {
            if (string.IsNullOrEmpty(_cfg.CargoTrackGroupName)
                && b is IMyConveyorSorter) return false;
            return b.HasInventory && b.IsFunctional;
        }, _cfg.CargoTrackGroupName);

        CurrentVolume = 0; MaxVolume = 0;
        HasOre = false; HasNonOre = false;
        Cargo.Clear();
        _blocks.ForEach(ProcessBlock);
    }

    private void ProcessBlock(IMyTerminalBlock block)
    {
        for (int i = 0; i < block.InventoryCount; i++)
        {
            _items.Clear();
            var inv = block.GetInventory(i);
            CurrentVolume += (double)inv.CurrentVolume;
            MaxVolume     += (double)inv.MaxVolume;
            inv.GetItems(_items);
            foreach (var item in _items)
            {
                bool ore = item.Type.TypeId == "MyObjectBuilder_Ore";
                if (ore) HasOre = true; else HasNonOre = true;
                string key = item.Type.ToString();
                double amt = (double)item.Amount;
                if (Cargo.ContainsKey(key)) Cargo[key].Amount += amt;
                else Cargo[key] = new CargoEntry
                    { TypeId = item.Type.TypeId, Amount = amt };
            }
        }
    }

    public void IterateDescending(Action<string, CargoEntry> cb)
    {
        foreach (var kv in Cargo.OrderByDescending(x => x.Value.Amount))
            cb(kv.Key, kv.Value);
    }
}

// ============================================================
// WEIGHT ANALYSER
// ============================================================

public class WeightAnalyser
{
    private readonly HogConfig     _cfg;
    private readonly CargoManager  _cargo;
    private readonly SystemManager _sys;

    public string Status          = "";
    public float  LiftNeeded      = 0;
    public float  LiftAvailable   = 0;
    public float  StopDistance    = 0;
    public float  StopTime        = 0;
    public float  CapacityDelta   = 0;
    public bool   StopWarning     = false;
    public bool   NoLiftThrusters = false;

    private struct WPoint { public double Time, Cap; }
    private const int MAX_WP = 20;
    private WPoint[] _wpts     = new WPoint[MAX_WP];
    private int      _wptCount = 0;

    public WeightAnalyser(HogConfig cfg, CargoManager cargo, SystemManager sys)
    { _cfg = cfg; _cargo = cargo; _sys = sys; }

    public void QueryData(TimeSpan time)
    {
        var ctrl = _sys.ActiveController;
        if (ctrl == null) { Status = "No controller"; return; }
        if (ctrl.CalculateShipMass().PhysicalMass == 0)
        {
            Status = "Static grid";
            LiftNeeded = LiftAvailable = StopDistance = StopTime = 0;
            return;
        }
        Status = "";
        NoLiftThrusters = _sys.LiftThrusters.Count == 0;
        CalcLift(ctrl);
        CalcStop(ctrl);
        CalcCapDelta(time);
    }

    private void CalcLift(IMyShipController ctrl)
    {
        float mass    = ctrl.CalculateShipMass().PhysicalMass;
        float gravMS2 = (float)ctrl.GetNaturalGravity().Length();
        LiftNeeded    = mass * gravMS2;

        Vector3 grav = ctrl.GetNaturalGravity();
        if (grav.Length() < 0.01) { LiftAvailable = 0; return; }
        Vector3D gravN = Vector3D.Normalize(grav);

        LiftAvailable = 0;
        _sys.LiftThrusters.ForEach(t =>
        {
            if (!t.IsWorking) return;
            Vector3D td  = t.WorldMatrix.Forward;
            double   dot = Vector3D.Dot(td, Vector3.Normalize(grav));
            LiftAvailable += t.MaxEffectiveThrust * (float)dot;
        });
        if (LiftAvailable < 0) LiftAvailable = 0;
    }

    private void CalcStop(IMyShipController ctrl)
    {
        float  mass   = ctrl.CalculateShipMass().PhysicalMass;
        double thrust = 0; int disabled = 0;
        _sys.StopThrusters.ForEach(t =>
        {
            if (!t.IsWorking) disabled++;
            if (t.IsFunctional) thrust += t.MaxEffectiveThrust;
        });
        StopWarning = disabled > 0;
        if (mass <= 0 || thrust <= 0) { StopDistance = StopTime = 0; return; }
        double decel = thrust / mass;
        double speed = ctrl.GetShipSpeed();
        StopTime     = (float)(speed / decel);
        StopDistance = (float)(speed * StopTime - decel * StopTime * StopTime / 2.0);
        if (StopDistance < 0) StopDistance = 0;
    }

    private void CalcCapDelta(TimeSpan time)
    {
        double cap = _cargo.MaxVolume > 0
            ? _cargo.CurrentVolume / _cargo.MaxVolume : 0;
        var wp = new WPoint { Time = time.TotalSeconds, Cap = cap };
        if (_wptCount < MAX_WP) { _wpts[_wptCount++] = wp; CapacityDelta = 0; return; }
        for (int i = 1; i < MAX_WP; i++) _wpts[i - 1] = _wpts[i];
        _wpts[MAX_WP - 1] = wp;
        double dt = _wpts[MAX_WP - 1].Time - _wpts[0].Time;
        CapacityDelta = dt > 0
            ? (float)((_wpts[MAX_WP - 1].Cap - _wpts[0].Cap) / dt) : 0f;
    }

    public float LiftUsage =>
        LiftAvailable > 0
            ? MathHelper.Clamp(LiftNeeded / LiftAvailable, 0f, 1f)
            : 0f;

    public float LiftThresholdWarning => _cfg.LiftThresholdWarning;
}

// ============================================================
// UTILITY MANAGER (battery and reactor only)
// ============================================================

public class UtilityManager
{
    private readonly Program _p;
    private readonly MyIni   _storage;

    private readonly BlockFinder<IMyBatteryBlock> _batteries;
    private readonly BlockFinder<IMyReactor>      _reactors;

    public double BatteryLevel   = 0;
    public double BatteryDelta   = 0;
    public string BatteryStr     = "N/A";
    public string BatteryETAStr  = "";
    public string BatteryStatus  = "STABLE";
    public Color  BatteryStatCol = new Color(80, 220, 100);

    public double UraniumLevel   = 0;
    public string UraniumStr     = "N/A";
    public string UraniumETAStr  = "";
    public int    ReactorCount   = 0;
    public int    ReactorOnline  = 0;

    public UtilityManager(Program p, MyIni storage)
    {
        _p         = p;
        _storage   = storage;
        _batteries = new BlockFinder<IMyBatteryBlock>(p);
        _reactors  = new BlockFinder<IMyReactor>(p);
    }

    public void Save() { }

    public void Update()
    {
        _batteries.FindBlocks();
        _reactors.FindBlocks();
        CalcBattery();
        CalcReactor();
    }

    private void CalcBattery()
    {
        float stored = 0, max = 0, input = 0, output = 0;
        _batteries.ForEach(b =>
        {
            if (!b.IsFunctional) return;
            stored += b.CurrentStoredPower; max    += b.MaxStoredPower;
            input  += b.CurrentInput;       output += b.CurrentOutput;
        });
        if (max > 0)
        {
            BatteryLevel = stored / max;
            BatteryStr   = string.Format("{0:0.0}%", BatteryLevel * 100);
            BatteryDelta = input - output;
            if (BatteryDelta < -0.001f)
            {
                BatteryStatus  = "DRAINING";
                BatteryStatCol = HogPainter.Danger;
                float h = stored / (float)(-BatteryDelta);
                BatteryETAStr = h * 60f < 60f
                    ? string.Format("{0:0}m remaining", h * 60f)
                    : string.Format("{0:0.0}h remaining", h);
            }
            else if (BatteryDelta > 0.001f)
            {
                BatteryStatus  = "CHARGING";
                BatteryStatCol = HogPainter.OK;
                float h = (max - stored) / (float)BatteryDelta;
                BatteryETAStr = h * 60f < 60f
                    ? string.Format("{0:0}m to full", h * 60f)
                    : string.Format("{0:0.0}h to full", h);
            }
            else
            {
                BatteryStatus  = "STABLE";
                BatteryStatCol = HogPainter.Primary;
                BatteryETAStr  = "";
            }
        }
        else
        {
            BatteryLevel   = 0; BatteryStr = "N/A";
            BatteryETAStr  = ""; BatteryDelta = 0;
            BatteryStatus  = "NO BATTERIES";
            BatteryStatCol = HogPainter.Danger;
        }
    }

    private void CalcReactor()
    {
        ReactorCount = _reactors.Count(); ReactorOnline = 0;
        if (ReactorCount == 0)
        { UraniumLevel = 0; UraniumStr = "N/A"; UraniumETAStr = ""; return; }

        double fuel = 0, inp = 0, maxOut = 0;
        _reactors.ForEach(r =>
        {
            if (r.IsWorking) ReactorOnline++;
            fuel   += BlockHelper.GetReactorFuelLevel(r);
            inp    += r.CurrentOutput;
            maxOut += r.MaxOutput;
        });
        UraniumLevel = fuel / ReactorCount;
        UraniumStr   = string.Format("{0:0.0}%", UraniumLevel * 100);
        if (inp > 0.001 && UraniumLevel > 0 && maxOut > 0)
        {
            double drain = (inp / maxOut) * 0.01;
            double h     = drain > 0 ? UraniumLevel / drain : 0;
            UraniumETAStr = h <= 0 ? "" : h < 1.0
                ? string.Format("{0:0} minutes", h * 60)
                : h < 48.0
                    ? string.Format("{0:0.0} hours", h)
                    : string.Format("{0:0} days", h / 24.0);
        }
        else UraniumETAStr = "";
    }
}

// ============================================================
// CONFIG
// ============================================================

public class HogConfig
{
    private readonly MyIni  _ini;
    private readonly string _sec;

    public string CargoTrackGroupName    = "";
    public string LiftThrustersGroupName = "";
    public string StopThrustersGroupName = "";
    public float  LiftThresholdWarning   = 0.9f;

    public HogConfig(MyIni ini, string sec) { _ini = ini; _sec = sec; }

    public void SetupDefaults()
    {
        Set("CargoTrackGroupName",    CargoTrackGroupName);
        Set("LiftThrustersGroupName", LiftThrustersGroupName);
        Set("StopThrustersGroupName", StopThrustersGroupName);
        Set("LiftThresholdWarning",   LiftThresholdWarning);
    }

    public void ReadConfig(string blob)
    {
        MyIniParseResult r;
        if (!_ini.TryParse(blob, _sec, out r)) return;
        if (!_ini.ContainsSection(_sec)) return;
        CargoTrackGroupName    = Get("CargoTrackGroupName",    CargoTrackGroupName);
        LiftThrustersGroupName = Get("LiftThrustersGroupName", LiftThrustersGroupName);
        StopThrustersGroupName = Get("StopThrustersGroupName", StopThrustersGroupName);
        LiftThresholdWarning   = _ini.Get(_sec, "LiftThresholdWarning")
                                     .ToSingle(LiftThresholdWarning);
    }

    private string Get(string k, string def) => _ini.Get(_sec, k).ToString(def);
    private void Set(string k, string v)     => _ini.Set(_sec, k, v);
    private void Set(string k, float  v)     => _ini.Set(_sec, k, v);
    public bool HasSection(string blob)      => MyIni.HasSection(blob, _sec);
}

// ============================================================
// CONTEXT
// ============================================================

public class HogContext
{
    public readonly Program        Program;
    public readonly HogConfig      Config;
    public readonly MyIni          Storage;
    public readonly CargoManager   Cargo;
    public readonly SystemManager  System;
    public readonly WeightAnalyser Weight;
    public readonly UtilityManager Utility;

    public TimeSpan TimeAccum;
    public Random   Rng  = new Random();
    public int      Tick = 0;

    public HogContext(Program p, HogConfig cfg, MyIni storage)
    {
        Program = p; Config = cfg; Storage = storage;
        Cargo   = new CargoManager(p, cfg);
        System  = new SystemManager(p, cfg);
        Weight  = new WeightAnalyser(cfg, Cargo, System);
        Utility = new UtilityManager(p, storage);
    }

    public void Save() => Utility.Save();

    public void Update(TimeSpan time)
    {
        TimeAccum = time;
        Cargo.QueryData();
        System.Update();
        Weight.QueryData(time);
        Utility.Update();
        Tick++;
    }

    public void HandleCommand(string arg)
    {
        // No commands in v2.0
    }
}

// ============================================================
// SYSTEM MANAGER (controller and lift/stop thrusters only)
// ============================================================

public class SystemManager
{
    private readonly Program   _p;
    private readonly HogConfig _cfg;

    private readonly BlockFinder<IMyShipController> _controllers;
    private readonly BlockFinder<IMyThrust>         _liftThrusters;
    private readonly BlockFinder<IMyThrust>         _stopThrusters;

    private IMyShipController _ctrl;

    public IMyShipController ActiveController => _ctrl;
    public List<IMyThrust>   LiftThrusters    => _liftThrusters.blocks;
    public List<IMyThrust>   StopThrusters    => _stopThrusters.blocks;
    public string            Status           = "";

    public SystemManager(Program p, HogConfig cfg)
    {
        _p             = p;
        _cfg           = cfg;
        _controllers   = new BlockFinder<IMyShipController>(p);
        _liftThrusters = new BlockFinder<IMyThrust>(p);
        _stopThrusters = new BlockFinder<IMyThrust>(p);
    }

    public void Update()
    {
        UpdateController();
        if (_ctrl != null) UpdateThrusters();
    }

    private void UpdateController()
    {
        _controllers.FindBlocks();
        IMyShipController first = null;
        _ctrl = null;
        foreach (var c in _controllers.blocks)
        {
            if (!c.IsWorking) continue;
            if (first == null) first = c;
            if (_ctrl == null && c.IsUnderControl && c.CanControlShip) _ctrl = c;
            if (c.IsMainCockpit) _ctrl = c;
        }
        if (_ctrl == null) _ctrl = first;
        Status = _ctrl == null ? "No controller" : "";
    }

    private void UpdateThrusters()
    {
        Vector3D grav  = _ctrl.GetNaturalGravity();
        Vector3D gravN = grav.Length() > 0.01
            ? Vector3D.Normalize(grav)
            : _ctrl.WorldMatrix.Down;

        if (!string.IsNullOrEmpty(_cfg.LiftThrustersGroupName))
            _liftThrusters.FindBlocks(true, null, _cfg.LiftThrustersGroupName);
        else
            _liftThrusters.FindBlocks(true, t =>
            {
                Vector3D td = -t.WorldMatrix.Forward;
                return Vector3D.Dot(td, -gravN) >= 0.2;
            });

        if (!string.IsNullOrEmpty(_cfg.StopThrustersGroupName))
            _stopThrusters.FindBlocks(true, null, _cfg.StopThrustersGroupName);
        else
            _stopThrusters.FindBlocks(true, t =>
                Vector3D.Dot(-t.WorldMatrix.Forward,
                    _ctrl.GetShipVelocities().LinearVelocity) <= -0.7);
    }
}

// ============================================================
// SCREEN BASE
// ============================================================

public abstract class HogScreen
{
    public const string NAME = "Blank";
    protected readonly HogContext _ctx;
    protected float _spinnerAngle = 0f;

    public HogScreen(HogContext ctx) { _ctx = ctx; }
    public abstract void Draw(IMyTextSurface surface);
    public virtual  bool ShouldDispose() => false;

    public void TickSpinner()
    {
        _spinnerAngle += 0.4f;
        if (_spinnerAngle > (float)(Math.PI * 2)) _spinnerAngle = 0f;
    }
}

// ============================================================
// SCREEN: SPLASH
// Boot logo normally; auto-switches to dock status panel when
// a connector tagged [HogOS-Dock] is connected or connectable.
// Reverts to logo when connector is unconnected.
// ============================================================

public class SplashScreen : HogScreen
{
    public new const string NAME = "Splash";

    private readonly BlockFinder<IMyShipConnector> _connectors;

    public SplashScreen(HogContext ctx) : base(ctx)
    {
        _connectors = new BlockFinder<IMyShipConnector>(ctx.Program);
    }

    private bool IsDocked(out IMyShipConnector docked)
    {
        docked = null;
        _connectors.FindBlocks(true, c =>
            c.CustomName.Contains("[HogOS-Dock]"));
        foreach (var c in _connectors.blocks)
        {
            if (c.Status == MyShipConnectorStatus.Connected ||
                c.Status == MyShipConnectorStatus.Connectable)
            {
                docked = c;
                return true;
            }
        }
        return false;
    }

    public override void Draw(IMyTextSurface surface)
    {
        TickSpinner();
        IMyShipConnector dockedConn;
        if (IsDocked(out dockedConn))
        {
            DrawDockPanel(surface, dockedConn);
            return;
        }
        DrawLogo(surface);
    }

    private void DrawLogo(IMyTextSurface surface)
    {
        using (var frame = surface.DrawFrame())
        {
            HogPainter.Setup(surface, frame);
            float w = HogPainter.Width, h = HogPainter.Height;
            float cx = w / 2f, cy = h / 2f;

            float iconSize = Math.Min(w, h) * 0.38f;
            HogPainter.SpriteCentered(
                new Vector2(cx, cy - iconSize * 0.15f),
                new Vector2(iconSize, iconSize),
                HogPainter.LOGO,
                new Color(HogPainter.Dim.R, HogPainter.Dim.G,
                           HogPainter.Dim.B, 180));

            float fsT = w >= 512f ? 1.6f : 1.1f;
            float fsS = w >= 512f ? 0.65f : 0.5f;

            HogPainter.TextCentered(
                new Vector2(cx, cy - fsT * 28f),
                "HogOS", fsT, HogPainter.Accent);
            HogPainter.TextCentered(
                new Vector2(cx, cy + fsT * 10f),
                "Hog Operating System", fsS, HogPainter.Primary);
            HogPainter.TextCentered(
                new Vector2(cx, h - 42f),
                "RevGamer (Simba \"Davy\" Jones)",
                fsS * 0.7f, HogPainter.Dim);

            HogPainter.Divider(h - 36f, 20f, HogPainter.Dim);
            HogPainter.TextRight(new Vector2(w - 6f, h - 24f),
                "v2.0", fsS * 0.8f, HogPainter.Dim);

            frame.Add(new MySprite(SpriteType.TEXTURE, "Screen_LoadingBar",
                size: new Vector2(14f, 14f),
                color: new Color(HogPainter.Primary.R,
                                  HogPainter.Primary.G,
                                  HogPainter.Primary.B, 120))
            {
                Position        = new Vector2(cx, h - 24f) + HogPainter.AvailableSize / 2f,
                RotationOrScale = _spinnerAngle
            });
        }
    }

    private void DrawDockPanel(IMyTextSurface surface, IMyShipConnector conn)
    {
        using (var frame = surface.DrawFrame())
        {
            HogPainter.Setup(surface, frame);
            float w      = HogPainter.Width;
            float margin = 5f;
            float gap    = 5f;
            float fs     = 0.62f * surface.FontSize;
            float fsS    = fs * 0.80f;
            float right  = w - margin;

            float y = HogPainter.DrawHeader(frame, margin,
                "DOCK STATUS", _spinnerAngle, HogPainter.Accent);

            // Connector status
            string statusStr;
            Color  statusCol;
            if (conn.Status == MyShipConnectorStatus.Connected)
            {
                statusStr = "LOCKED";
                statusCol = HogPainter.OK;
            }
            else
            {
                statusStr = "CONNECTABLE";
                statusCol = HogPainter.Primary;
            }

            HogPainter.Text(new Vector2(margin, y),
                "STATUS", fs, TextAlignment.LEFT, HogPainter.Accent);
            HogPainter.TextRight(new Vector2(right, y),
                statusStr, fs, statusCol);
            y += fs * 15f + gap;

            HogPainter.Divider(y, margin, HogPainter.Dim);
            y += gap * 2f;

            // Docked grid name
            string gridName = "";
            if (conn.Status == MyShipConnectorStatus.Connected &&
                conn.OtherConnector != null)
            {
                gridName = conn.OtherConnector.CubeGrid.CustomName;
            }

            HogPainter.Text(new Vector2(margin, y),
                "DOCKED TO", fsS, TextAlignment.LEFT, HogPainter.Dim);
            y += fsS * 15f + gap;

            HogPainter.Text(new Vector2(margin, y),
                gridName != "" ? gridName : "---",
                fs, TextAlignment.LEFT,
                gridName != "" ? HogPainter.Accent : HogPainter.Dim);
            y += fs * 15f + gap;

            HogPainter.Divider(y, margin, HogPainter.Dim);
            y += gap * 2f;

            // Connector name
            HogPainter.Text(new Vector2(margin, y),
                "CONNECTOR", fsS, TextAlignment.LEFT, HogPainter.Dim);
            y += fsS * 15f + gap;
            HogPainter.Text(new Vector2(margin, y),
                conn.CustomName, fsS, TextAlignment.LEFT, HogPainter.Primary);
        }
    }
}

// ============================================================
// SCREEN: LOADING
// ============================================================

public class LoadingScreen : HogScreen
{
    public new const string NAME = "Loading";
    private readonly double _start;
    private readonly double _duration;
    private readonly int    _quoteCount;
    private int    _lastIdx = -1;
    private string _quote   = "Initialising";

    private string[] Quotes =
    {
        "Calibrating drill telemetry",
        "Scanning ore signatures",
        "Aligning gyroscopic arrays",
        "Loading ore manifests",
        "Negotiating with Clang",
        "Pressurising cargo bays",
        "Running subsystem checks",
        "Syncing conveyor network",
        "Warming up thrusters",
        "Mapping subsurface deposits",
        "Charging capacitor banks",
        "Establishing ground lock",
        "Checking hog integrity",
        "Sniffing for ore deposits"
    };

    public LoadingScreen(HogContext ctx) : base(ctx)
    {
        _start      = ctx.TimeAccum.TotalSeconds;
        _duration   = 1.5 + ctx.Rng.Next(800, 2000) / 1000.0;
        _quoteCount = ctx.Rng.Next(3, 7);
    }

    private string GetQuote(double progress)
    {
        int idx = (int)(progress * _quoteCount);
        if (idx == _lastIdx) return _quote;
        _lastIdx = idx;
        _quote   = Quotes[_ctx.Rng.Next(Quotes.Length)];
        return _quote;
    }

    public override void Draw(IMyTextSurface surface)
    {
        TickSpinner();
        using (var frame = surface.DrawFrame())
        {
            HogPainter.Setup(surface, frame);
            float w      = HogPainter.Width, h = HogPainter.Height;
            float margin = 14f;
            float elapsed  = (float)(_ctx.TimeAccum.TotalSeconds - _start);
            float progress = MathHelper.Clamp((float)(elapsed / _duration), 0f, 1f);

            float iconSize = Math.Min(w, h) * 0.28f;
            HogPainter.SpriteCentered(
                new Vector2(w / 2f, h / 2f - iconSize * 0.2f),
                new Vector2(iconSize, iconSize),
                HogPainter.LOGO, HogPainter.Dim);

            HogPainter.TextCentered(new Vector2(w / 2f, margin),
                "HogOS", 0.9f, HogPainter.Accent);

            HogPainter.Text(new Vector2(margin, h - margin * 2.6f),
                GetQuote(progress) + "...", 0.42f,
                TextAlignment.LEFT, HogPainter.Dim);

            float barH = 7f;
            HogPainter.ProgressBar(
                new Vector2(margin, h - margin - barH),
                new Vector2(w - margin * 2f, barH),
                progress, 1.5f, HogPainter.Dim, HogPainter.Primary);

            HogPainter.TextRight(
                new Vector2(w - margin, h - margin - barH - 2f),
                (int)(progress * 100f) + "%", 0.28f, HogPainter.Dim);
        }
    }

    public override bool ShouldDispose()
        => _ctx.TimeAccum.TotalSeconds - _start > _duration;
}

// ============================================================
// SCREEN: POWER
// ============================================================

public class PowerScreen : HogScreen
{
    public new const string NAME = "Power";
    public PowerScreen(HogContext ctx) : base(ctx) { }

    public override void Draw(IMyTextSurface surface)
    {
        TickSpinner();
        using (var frame = surface.DrawFrame())
        {
            HogPainter.Setup(surface, frame);
            float w      = HogPainter.Width;
            float margin = 5f;
            float gap    = 5f;
            float fs     = 0.62f * surface.FontSize;
            float fsS    = fs * 0.80f;
            float right  = w - margin;

            float y = HogPainter.DrawHeader(frame, margin,
                "POWER SYSTEMS", _spinnerAngle, HogPainter.Accent);

            var   um   = _ctx.Utility;
            float barW = w - margin * 2f;
            float barH = 13f;

            // Battery
            Color batCol = um.BatteryLevel < 0.15 ? HogPainter.Danger
                : HogPainter.Primary;
            HogPainter.Text(new Vector2(margin, y),
                "BATTERY", fs, TextAlignment.LEFT, HogPainter.Accent);
            HogPainter.TextRight(new Vector2(right, y),
                um.BatteryStr, fs, batCol);
            y += fs * 15f + gap;

            HogPainter.ProgressBar(new Vector2(margin, y),
                new Vector2(barW, barH), (float)um.BatteryLevel,
                1.5f, HogPainter.Dim, batCol, "IconEnergy");
            y += barH + gap;

            HogPainter.Text(new Vector2(margin, y),
                um.BatteryStatus, fsS, TextAlignment.LEFT, um.BatteryStatCol);
            if (um.BatteryETAStr != "")
                HogPainter.TextRight(new Vector2(right, y),
                    um.BatteryETAStr, fsS,
                    new Color(HogPainter.Dim.R, HogPainter.Dim.G,
                               HogPainter.Dim.B, 220));
            y += fsS * 15f + gap * 2f;

            HogPainter.Divider(y, margin, HogPainter.Dim);
            y += gap * 2f;

            // Reactor
            Color reactCol = um.UraniumLevel < 0.10 ? HogPainter.Danger
                : HogPainter.Primary;
            HogPainter.Text(new Vector2(margin, y),
                "REACTOR FUEL", fs, TextAlignment.LEFT, HogPainter.Accent);
            HogPainter.TextRight(new Vector2(right, y),
                um.UraniumStr, fs, reactCol);
            y += fs * 15f + gap;

            HogPainter.ProgressBar(new Vector2(margin, y),
                new Vector2(barW, barH), (float)um.UraniumLevel,
                1.5f, HogPainter.Dim, reactCol,
                "MyObjectBuilder_Ingot/Uranium");
            y += barH + gap;

            string rStatus = um.ReactorCount == 0 ? "NO REACTORS"
                : string.Format("{0} of {1} online",
                    um.ReactorOnline, um.ReactorCount);
            Color rCol = um.ReactorOnline == 0 ? HogPainter.Danger
                : um.ReactorOnline < um.ReactorCount ? HogPainter.Primary
                : HogPainter.OK;
            HogPainter.Text(new Vector2(margin, y),
                rStatus, fsS, TextAlignment.LEFT, rCol);
            if (um.UraniumETAStr != "")
                HogPainter.TextRight(new Vector2(right, y),
                    um.UraniumETAStr, fsS,
                    new Color(HogPainter.Dim.R, HogPainter.Dim.G,
                               HogPainter.Dim.B, 220));
            y += fsS * 15f + gap * 2f;

            HogPainter.Divider(y, margin, HogPainter.Dim);
            y += gap * 2f;

            string flowStr = um.BatteryDelta >= 0
                ? string.Format("NET  +{0:0.00} MW", um.BatteryDelta)
                : string.Format("NET  {0:0.00} MW",  um.BatteryDelta);
            HogPainter.TextCentered(new Vector2(w / 2f, y), flowStr, fsS,
                um.BatteryDelta >= 0 ? HogPainter.OK : HogPainter.Danger);
        }
    }
}

// ============================================================
// SCREEN: CARGO ORE
// ============================================================

public class CargoOreScreen : HogScreen
{
    public new const string NAME = "OreCargo";
    private readonly StringBuilder _sb = new StringBuilder();

    public CargoOreScreen(HogContext ctx) : base(ctx) { }

    private string ShortName(string t) => t.Split('/').Last();

    private string FormatMass(double kg)
    {
        if (kg >= 1000000) return string.Format("{0:0.00}Mt", kg / 1000000);
        if (kg >= 1000)    return string.Format("{0:0.00}t",  kg / 1000);
        return string.Format("{0:0.00}kg", kg);
    }

    public override void Draw(IMyTextSurface surface)
    {
        TickSpinner();
        using (var frame = surface.DrawFrame())
        {
            HogPainter.Setup(surface, frame);
            float w      = HogPainter.Width;
            float margin = 5f;
            float gap    = 5f;
            float fs     = 0.65f * surface.FontSize;
            float right  = w - margin;

            double fillPct = _ctx.Cargo.MaxVolume > 0
                ? _ctx.Cargo.CurrentVolume / _ctx.Cargo.MaxVolume * 100 : 0;

            float y = HogPainter.DrawHeader(frame, margin,
                string.Format("ORE CARGO  {0:0.0}%", fillPct),
                _spinnerAngle, HogPainter.Accent);

            if (!_ctx.Cargo.HasOre)
            {
                HogPainter.SpriteCentered(HogPainter.Center,
                    new Vector2(60f, 60f),
                    "MyObjectBuilder_Ore/Stone", HogPainter.Dim);
                HogPainter.TextCentered(
                    HogPainter.Center + new Vector2(0, 40f),
                    "No ores detected", 0.7f, HogPainter.Dim);
                return;
            }

            _sb.Clear(); _sb.Append("Xy");
            Vector2 th   = surface.MeasureStringInPixels(_sb, surface.Font, fs);
            float   rowH = th.Y + gap;

            _ctx.Cargo.IterateDescending((name, entry) =>
            {
                if (entry.TypeId != "MyObjectBuilder_Ore") return;
                if (y + rowH > HogPainter.Height - margin) return;

                _sb.Clear(); _sb.Append(ShortName(name));
                Vector2 ts = surface.MeasureStringInPixels(_sb, surface.Font, fs);

                HogPainter.Sprite(new Vector2(margin, y),
                    new Vector2(ts.Y, ts.Y), name, HogPainter.Primary);
                HogPainter.Text(new Vector2(margin + ts.Y + gap, y),
                    ShortName(name), fs, TextAlignment.LEFT, HogPainter.Accent);
                HogPainter.TextRight(new Vector2(right, y),
                    FormatMass(entry.Amount), fs, HogPainter.Primary);

                y += rowH;
                HogPainter.Divider(y, margin, HogPainter.Dim);
                y += gap;
            });
        }
    }
}

// ============================================================
// SCREEN: WEIGHT
// Uses ExcavOS Radial approach — two half-height gauges
// ============================================================

public class WeightScreen : HogScreen
{
    public new const string NAME = "Weight";
    public WeightScreen(HogContext ctx) : base(ctx) { }

    public override void Draw(IMyTextSurface surface)
    {
        TickSpinner();
        using (var frame = surface.DrawFrame())
        {
            HogPainter.Setup(surface, frame);
            float w = HogPainter.Width, h = HogPainter.Height;

            if (_ctx.Weight.Status != "")
            {
                HogPainter.TextCentered(HogPainter.Center,
                    _ctx.Weight.Status, 0.8f, HogPainter.Danger);
                return;
            }

            float cargo     = _ctx.Cargo.MaxVolume > 0
                ? (float)(_ctx.Cargo.CurrentVolume / _ctx.Cargo.MaxVolume) : 0f;
            float liftUsage = _ctx.Weight.LiftUsage;

            float margin  = 5f;
            float maxDim  = Math.Min(w, h);
            bool  shortMode = maxDim < w;

            if (_ctx.Weight.NoLiftThrusters)
            {
                // No lift thrusters -- show cargo only
                Vector2 pos  = new Vector2((w - maxDim) / 2f + margin, margin);
                Vector2 size = new Vector2(maxDim - margin * 2f, maxDim / 2f - margin);
                string subText = shortMode ? "Cargo" : "Cargo capacity";
                if (_ctx.Weight.CapacityDelta > 0.0001f)
                {
                    float tl = (1f - cargo) / _ctx.Weight.CapacityDelta;
                    subText = string.Format("+{0:0.00}%  full in {1:0}s",
                        _ctx.Weight.CapacityDelta * 100, tl);
                }
                HogPainter.Radial(pos, size, cargo, subText, 60);
            }
            else
            {
                // Two gauges -- ExcavOS style, each takes half height
                Vector2 pos  = new Vector2(
                    (w - maxDim) / 2f + margin, margin / 2f);
                Vector2 size = new Vector2(
                    maxDim - margin * 2f, maxDim / 2f - margin);

                // Top: lift thrust (sweeps left to right)
                string liftLabel = shortMode ? "Lift" : "Lift thrust";
                HogPainter.Radial(pos, size, liftUsage, liftLabel, 30);

                // Separator line
                float sepY = pos.Y + h / 2f;
                HogPainter.FilledRect(
                    new Vector2(pos.X, sepY - 1f - margin / 2f),
                    new Vector2(maxDim - margin * 2f, 2f),
                    HogPainter.Dim);

                // Bottom: cargo (sweep right to left with flip=true)
                pos.Y += h / 2f;
                string cargoLabel = shortMode ? "Cargo" : "Cargo capacity";
                if (_ctx.Weight.CapacityDelta > 0.0001f)
                {
                    float tl = (1f - cargo) / _ctx.Weight.CapacityDelta;
                    cargoLabel = string.Format("+{0:0.00}%  {1:0}s",
                        _ctx.Weight.CapacityDelta * 100, tl);
                }
                HogPainter.Radial(pos, size, cargo, cargoLabel, 30, true);

                // Warning flash on lift gauge
                if (liftUsage > _ctx.Weight.LiftThresholdWarning
                    && _ctx.Tick % 2 == 0)
                {
                    float fsz = maxDim * 0.12f;
                    HogPainter.SpriteCentered(
                        new Vector2(w / 2f,
                            margin / 2f + (maxDim / 2f - margin) * 0.85f),
                        new Vector2(fsz, fsz), "Danger", HogPainter.Danger);
                }
            }
        }
    }
}

// ============================================================
// SCREEN: BLANK (safety default for unknown screen names)
// ============================================================

public class BlankScreen : HogScreen
{
    public new const string NAME = "Blank";
    public BlankScreen(HogContext ctx) : base(ctx) { }
    public override void Draw(IMyTextSurface surface)
    {
        using (var frame = surface.DrawFrame())
            HogPainter.Setup(surface, frame);
    }
}

// ============================================================
// SCREEN FACTORY
// ============================================================

public static class ScreenFactory
{
    private static Dictionary<string, HogScreen> _cache
        = new Dictionary<string, HogScreen>();

    public static HogScreen Get(string name, HogContext ctx)
    {
        if (name == LoadingScreen.NAME)
            return new LoadingScreen(ctx);

        HogScreen cached;
        if (_cache.TryGetValue(name, out cached)) return cached;

        HogScreen s;
        switch (name)
        {
            case SplashScreen.NAME:   s = new SplashScreen(ctx);   break;
            case PowerScreen.NAME:    s = new PowerScreen(ctx);    break;
            case CargoOreScreen.NAME: s = new CargoOreScreen(ctx); break;
            case WeightScreen.NAME:   s = new WeightScreen(ctx);   break;
            default:                  s = new BlankScreen(ctx);    break;
        }
        _cache[name] = s;
        return s;
    }

    public static void ClearCache() => _cache.Clear();
}

// ============================================================
// REGISTERED PROVIDER
// ============================================================

public class RegisteredProvider
{
    private readonly IMyTerminalBlock _block;
    private readonly HogContext       _ctx;
    private readonly MyIni            _ini;
    private readonly string           _sec;

    private readonly Dictionary<int, HogScreen> _screens
        = new Dictionary<int, HogScreen>();
    private readonly Dictionary<int, HogScreen> _immersive
        = new Dictionary<int, HogScreen>();
    private bool _wasUnderControl = false;
    private bool _enableImmersion = false;

    public RegisteredProvider(HogContext ctx, IMyTerminalBlock block,
                               MyIni ini, string sec)
    { _ctx = ctx; _block = block; _ini = ini; _sec = sec; }

    public void LoadConfig(string blob)
    {
        MyIniParseResult r;
        if (!_ini.TryParse(blob, _sec, out r)) return;
        if (!_ini.ContainsSection(_sec)) return;
        _enableImmersion = _ini.Get(_sec, "EnableImmersion").ToBoolean(false);
        var sp = _block as IMyTextSurfaceProvider;
        if (sp == null) return;
        for (int n = 0; n < sp.SurfaceCount; n++)
        {
            string key = "Surface" + n;
            if (_ini.ContainsKey(_sec, key))
                SetScreen(_ini.Get(_sec, key).ToString(), n);
            else if (_screens.ContainsKey(n))
                ClearScreen(n);
        }
    }

    public void SetScreen(string name, int idx)
        => _screens[idx] = ScreenFactory.Get(name, _ctx);

    private void ClearScreen(int idx)
    {
        _screens.Remove(idx);
        var sp = _block as IMyTextSurfaceProvider;
        if (sp == null) return;
        var surf = sp.GetSurface(idx);
        surf.Script = ""; surf.ContentType = ContentType.NONE;
    }

    public bool HasSurfaces() => _screens.Count > 0;

    public void Update(bool booting, HogContext ctx)
    {
        if (!_block.IsWorking) return;
        var sp = _block as IMyTextSurfaceProvider;
        if (sp == null) return;

        if (booting)
        {
            for (int n = 0; n < sp.SurfaceCount; n++)
            {
                if (!_screens.ContainsKey(n)) continue;
                var surf = sp.GetSurface(n);
                surf.Script = ""; surf.ContentType = ContentType.SCRIPT;
                if (!_immersive.ContainsKey(n))
                    _immersive[n] = new LoadingScreen(ctx);
                _immersive[n].Draw(surf);
            }
            return;
        }

        IMyCockpit cockpit = _block as IMyCockpit;
        if (_enableImmersion && cockpit != null)
        {
            if (!_wasUnderControl && cockpit.IsUnderControl)
            {
                _wasUnderControl = true;
                foreach (int n in _screens.Keys)
                    _immersive[n] = new LoadingScreen(ctx);
            }
            else if (_wasUnderControl && !cockpit.IsUnderControl)
            {
                _wasUnderControl = false;
                _immersive.Clear();
            }
        }

        for (int n = 0; n < sp.SurfaceCount; n++)
        {
            if (!_screens.ContainsKey(n)) continue;
            var surf = sp.GetSurface(n);
            surf.Script = ""; surf.ContentType = ContentType.SCRIPT;

            if (_immersive.ContainsKey(n))
            {
                _immersive[n].Draw(surf);
                if (_immersive[n].ShouldDispose()) _immersive.Remove(n);
            }
            else
            {
                _screens[n].Draw(surf);
            }
        }
    }
}

// ============================================================
// HOG OS
// ============================================================

public class HogOS
{
    private const string SCRIPT_NAME    = "HogOS";
    private const string SCRIPT_VERSION = "2.0";
    private const string SPINNER        = "|/-\\";
    private const float  BOOT_TIME      = 3.0f;

    private readonly Program    _p;
    private readonly MyIni      _ini     = new MyIni();
    private readonly MyIni      _storage;
    private readonly HogConfig  _config;
    private readonly HogContext _context;

    private readonly BlockFinder<IMyTerminalBlock>        _surfaceProviders;
    private readonly Dictionary<long, RegisteredProvider> _providers
        = new Dictionary<long, RegisteredProvider>();

    private int      _tick100   = 0;
    private int      _tick10    = 0;
    private TimeSpan _timeAccum = new TimeSpan();

    private bool  _booting   = true;
    private float _bootTimer = 0f;

    public HogOS(Program p, MyIni storage)
    {
        _p                = p;
        _storage          = storage;
        _config           = new HogConfig(_ini, SCRIPT_NAME);
        _context          = new HogContext(p, _config, storage);
        _surfaceProviders = new BlockFinder<IMyTerminalBlock>(p);
        Initialize();
        _p.Echo("HogOS " + SCRIPT_VERSION + " -- Booting...");
    }

    public void Save() => _context.Save();

    public void Update(string arg, UpdateType src, TimeSpan dt)
    {
        _timeAccum += dt;

        if (src == UpdateType.Update100)
        {
            _tick100++;
            _p.Echo(string.Format(
                "HogOS  v{0}  {1}\nRuntime: {2:0.00}ms",
                SCRIPT_VERSION,
                SPINNER[_tick100 % SPINNER.Length],
                _p.Runtime.LastRunTimeMs));

            if (_tick100 % 5 == 0) Initialize();
        }
        else if (src == UpdateType.Update10)
        {
            _tick10++;
            if (_tick10 % 3 == 0)
            {
                _context.Update(_timeAccum);

                if (_booting)
                {
                    _bootTimer += (float)dt.TotalSeconds * 3f;
                    if (_bootTimer >= BOOT_TIME)
                    {
                        _booting = false;
                        _p.Echo("HogOS " + SCRIPT_VERSION + " -- Online");
                    }
                }

                foreach (var prov in _providers.Values)
                    prov.Update(_booting, _context);
            }
        }
        else if (!string.IsNullOrEmpty(arg))
        {
            _context.HandleCommand(arg);
        }
    }

    private void Initialize()
    {
        CreateConfigIfMissing();
        _config.ReadConfig(_p.Me.CustomData);
        FetchSurfaces();
    }

    private void CreateConfigIfMissing()
    {
        if (_config.HasSection(_p.Me.CustomData)) return;
        _ini.Clear();
        _config.SetupDefaults();
        _p.Me.CustomData = _ini.ToString();
    }

    private void FetchSurfaces()
    {
        _surfaceProviders.FindBlocks(true, block =>
        {
            if (!(block is IMyTextSurfaceProvider)) return false;
            if (!MyIni.HasSection(block.CustomData, SCRIPT_NAME)) return false;

            RegisteredProvider rp;
            if (!_providers.TryGetValue(block.EntityId, out rp))
            {
                rp = new RegisteredProvider(_context, block, _ini, SCRIPT_NAME);
                _providers[block.EntityId] = rp;
            }

            if (block == _p.Me)
                rp.SetScreen(SplashScreen.NAME, 0);
            else
                rp.LoadConfig(block.CustomData);

            if (!rp.HasSurfaces())
                _providers.Remove(block.EntityId);

            return true;
        });
    }
}
