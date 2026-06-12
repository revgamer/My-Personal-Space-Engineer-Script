// =============================================================================
//  RTC - Rev Turret Controller  v2.0
//  Author: RevGamer
//
//  Compatibility:
//    - Vanilla turrets        (IMyLargeTurretBase)
//    - Warfare 2 DLC turrets  (IMyLargeTurretBase + IMyTurretControlBlock)
//    - WeaponCore mod turrets (detected via WcPbApi)
//    - Vanilla + Framework weapons mod (framework turrets are IMyLargeTurretBase)
//
//  Features:
//    - Park position when idle  (azimuth + elevation)
//    - Synchronized aim: all turrets track same computed world point
//    - Ballistic lead compensation using target velocity + own ship velocity
//    - WC: sets AI focus to shared top threat, per-weapon target assignment
//    - TurretControlBlock: direct aim via SetTarget(Vector3D)
//    - IMyLargeTurretBase: Azimuth/Elevation + SyncAzimuth/SyncElevation park;
//                          TrackTarget for entity priority sync
//    - Custom Data config, no name tags required
//    - Status LCD (text mode)
//
//  CUSTOM DATA (written on first run):
//    [RTC]
//    TurretTag=         ; name filter, blank = all turrets same construct
//    LcdTag=[RTC]       ; status LCD name filter, blank = disabled
//    ParkAzimuth=0      ; degrees, 0 = ship forward
//    ParkElevation=0    ; degrees, 0 = horizontal, negative = dip down
//    IdleSeconds=5      ; idle time before parking
//    SyncTurrets=true   ; share aim point across all turrets
//    LeadCompensation=true
//    ProjectileSpeed=380 ; m/s - gatling ~380, rocket ~100, adjust per weapon type
//    UseWeaponCore=true  ; attempt WcPbApi activation
//
//  COMMANDS (run PB with argument):
//    park    - immediately return all turrets to park position (use with Timer Block)
//              AI targeting stays ON - turrets will leave park the moment enemy detected
//    reload  - re-read Custom Data config
// =============================================================================

// ---- TURRET WRAPPER ---------------------------------------------------------
// Abstracts vanilla / Warfare2 / WeaponCore turrets behind one interface
class TurretEntry
{
    public enum TurretKind { Vanilla, TurretControl, WeaponCore }

    public IMyTerminalBlock Block;
    public TurretKind Kind;
    public IMyLargeTurretBase Vanilla;       // IMyLargeTurretBase (vanilla + framework)
    public IMyTurretControlBlock TCtrl;      // Warfare 2 turret controller

    public bool IsWorking { get { return Block.IsWorking; } }

    public bool HasTarget()
    {
        if (Vanilla  != null) return Vanilla.HasTarget;
        if (TCtrl    != null) return TCtrl.HasTarget;
        return false;
    }

    public MyDetectedEntityInfo GetTarget()
    {
        if (Vanilla  != null) return Vanilla.GetTargetedEntity();
        if (TCtrl    != null) return TCtrl.GetTargetedEntity();
        return new MyDetectedEntityInfo();
    }

    // Park this turret to azimuth/elevation (radians)
    // EnableIdleRotation=false stops random spin; targeting AI stays active.
    // The AI will override these angles the moment an enemy enters range.
    public void Park(float azRad, float elRad)
    {
        if (Vanilla != null)
        {
            Vanilla.EnableIdleRotation = false;
            Vanilla.Azimuth            = azRad;
            Vanilla.Elevation          = elRad;
            Vanilla.SyncAzimuth();
            Vanilla.SyncElevation();
            // DO NOT call ResetTargetingToDefault or disable AI - base defense needs it
        }
        // TurretControl and WeaponCore-only: no park API available through PB
    }

    // RestoreAI is no longer used - AI stays on always
    public void RestoreAI() { }

    public long TargetEntityId()
    {
        if (!HasTarget()) return 0L;
        return GetTarget().EntityId;
    }
}

// ---- CONFIG -----------------------------------------------------------------
const string SECTION         = "RTC";
const string DEF_TAG         = "";
const string DEF_LCD_TAG     = "[RTC]";
const float  DEF_PARK_AZ     = 0f;
const float  DEF_PARK_EL     = 0f;
const float  DEF_IDLE_SEC    = 5f;
const bool   DEF_SYNC        = true;
const bool   DEF_LEAD        = true;
const float  DEF_PROJ_SPD    = 380f;
const bool   DEF_USE_WC      = true;

string _turretTag;
string _lcdTag;
float  _parkAzimuth;
float  _parkElevation;
float  _idleSeconds;
bool   _syncTurrets;
bool   _leadComp;
float  _projSpeed;
bool   _useWC;

// ---- STATE ------------------------------------------------------------------
List<TurretEntry>    _turrets  = new List<TurretEntry>();
List<IMyTextSurface> _surfaces = new List<IMyTextSurface>();

double _idleTimer    = 0;
int    _rebuildTick  = 0;
const int REBUILD_INTERVAL = 300; // ~30s at Update10

string _statusLine   = "INIT";
int    _wcTurretCount   = 0;
int    _vanillaTurretCount = 0;
int    _tcTurretCount   = 0;

// ---- WEAPONCORE -------------------------------------------------------------
WcPbApi _wc     = new WcPbApi();
bool    _wcReady = false;

// ---- CONTROLLER CACHE -------------------------------------------------------
IMyShipController _controller     = null;
int               _controllerTick = 0;

// ---- ENTRY ------------------------------------------------------------------
public Program()
{
    LoadConfig();
    Rebuild();
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    Echo("RTC v2.0 ready.");
}

public void Save() { }

public void Main(string argument, UpdateType updateSource)
{
    _rebuildTick++;
    if (_rebuildTick >= REBUILD_INTERVAL)
    {
        _rebuildTick = 0;
        Rebuild();
    }

    if (!string.IsNullOrEmpty(argument))
    {
        HandleCommand(argument.Trim().ToLower());
        return;
    }

    double dt = Runtime.TimeSinceLastRun.TotalSeconds;
    if (dt <= 0) dt = 1.0 / 6.0; // fallback ~Update10 rate

    Update(dt);
    DrawStatus();
}

// ---- COMMAND ----------------------------------------------------------------
void HandleCommand(string cmd)
{
    if (cmd == "reload")
    {
        LoadConfig();
        Rebuild();
        Echo("RTC reloaded.");
    }
    else if (cmd == "park")
    {
        ParkAll();
        _idleTimer = _idleSeconds; // force parked state
    }
    else
    {
        Echo("Unknown: " + cmd);
    }
}

// ---- MAIN UPDATE ------------------------------------------------------------
void Update(double dt)
{
    if (_turrets.Count == 0)
    {
        _statusLine = "NO TURRETS FOUND";
        return;
    }

    // --- Find best target ---
    // Priority: WC top threat > first vanilla/TC turret with a target
    MyDetectedEntityInfo bestTarget = new MyDetectedEntityInfo();
    bool hasTarget = false;

    if (_wcReady)
    {
        var threats = new Dictionary<MyDetectedEntityInfo, float>();
        _wc.GetSortedThreats(Me, threats);
        foreach (var kv in threats)
        {
            bestTarget = kv.Key;
            hasTarget  = true;
            break; // first = highest threat
        }
    }

    if (!hasTarget)
    {
        foreach (var t in _turrets)
        {
            if (!t.IsWorking) continue;
            if (t.HasTarget())
            {
                bestTarget = t.GetTarget();
                hasTarget  = true;
                break;
            }
        }
    }

    // --- Act on target state ---
    if (hasTarget)
    {
        _idleTimer = 0;

        Vector3D aimPoint = ComputeAimPoint(bestTarget, dt);

        // WC: set grid AI focus so all WC turrets prioritize same entity
        if (_wcReady)
            _wc.SetAiFocus(Me, bestTarget.EntityId, 0);

        if (_syncTurrets)
        {
            foreach (var t in _turrets)
            {
                if (!t.IsWorking) continue;

                if (t.Kind == TurretEntry.TurretKind.WeaponCore)
                {
                    // WC target was set above via SetAiFocus; optionally per-weapon:
                    _wc.SetWeaponTarget(t.Block, bestTarget.EntityId, 0);
                }
                else
                {
                    // Vanilla, framework mod, TurretControlBlock:
                    // TrackTarget = shared entity priority hint + lead position
                    if (t.Vanilla != null)
                        t.Vanilla.TrackTarget(aimPoint, bestTarget.Velocity);
                }
            }
            _statusLine = "ENGAGING (SYNC)";
        }
        else
        {
            _statusLine = "ENGAGING";
        }
    }
    else
    {
        _idleTimer += dt;

        if (_idleTimer >= _idleSeconds)
        {
            ParkAll();
            _statusLine = "PARKED";
        }
        else
        {
            _statusLine = "STANDBY (" + ((int)(_idleSeconds - _idleTimer)) + "s)";
        }
    }
}

// ---- LEAD COMPUTATION -------------------------------------------------------
Vector3D ComputeAimPoint(MyDetectedEntityInfo target, double dt)
{
    Vector3D pos = target.Position;
    if (!_leadComp || _projSpeed < 1f) return pos;

    double dist = Vector3D.Distance(Me.GetPosition(), pos);
    if (dist < 1.0) return pos;

    Vector3D targetVel = target.Velocity;
    Vector3D ownVel    = Vector3D.Zero;

    IMyShipController ctrl = GetController();
    if (ctrl != null)
        ownVel = ctrl.GetShipVelocities().LinearVelocity;

    Vector3D relVel = targetVel - ownVel;

    // Iterative 2-pass lead
    double tof  = dist / _projSpeed;
    Vector3D lead = pos + relVel * tof;
    double dist2  = Vector3D.Distance(Me.GetPosition(), lead);
    double tof2   = dist2 / _projSpeed;
    lead = pos + relVel * tof2;

    return lead;
}

// ---- PARK -------------------------------------------------------------------
void ParkAll()
{
    float azRad = _parkAzimuth  * (float)(Math.PI / 180.0);
    float elRad = _parkElevation * (float)(Math.PI / 180.0);

    foreach (var t in _turrets)
    {
        if (!t.IsWorking) continue;
        if (t.Kind == TurretEntry.TurretKind.WeaponCore)
            continue; // WC turrets: no park API in PB
        t.Park(azRad, elRad);
    }
}

// ---- REBUILD ----------------------------------------------------------------
void Rebuild()
{
    // Re-activate WeaponCore if enabled
    _wcReady = false;
    if (_useWC)
    {
        try { _wcReady = _wc.Activate(Me); } catch { _wcReady = false; }
    }

    _turrets.Clear();
    _surfaces.Clear();
    _wcTurretCount     = 0;
    _vanillaTurretCount = 0;
    _tcTurretCount     = 0;

    // --- Collect all candidate blocks ---
    var allBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType(allBlocks, b => b.IsSameConstructAs(Me));

    // Build WC weapon set first so we can classify correctly
    var wcSet = new HashSet<long>();
    if (_wcReady)
    {
        foreach (var b in allBlocks)
        {
            bool isWc = false;
            try { isWc = _wc.HasCoreWeapon(b); } catch { }
            if (isWc) wcSet.Add(b.EntityId);
        }
    }

    // --- IMyTurretControlBlock (Warfare 2 DLC) ---
    var tcBlocks = new List<IMyTurretControlBlock>();
    GridTerminalSystem.GetBlocksOfType(tcBlocks, b =>
        b.IsSameConstructAs(Me) &&
        (string.IsNullOrEmpty(_turretTag) || b.CustomName.Contains(_turretTag)));
    foreach (var tc in tcBlocks)
    {
        var entry = new TurretEntry();
        entry.Block = tc;
        entry.TCtrl = tc;
        if (wcSet.Contains(tc.EntityId))
        {
            entry.Kind = TurretEntry.TurretKind.WeaponCore;
            _wcTurretCount++;
        }
        else
        {
            entry.Kind = TurretEntry.TurretKind.TurretControl;
            _tcTurretCount++;
        }
        _turrets.Add(entry);
    }

    // --- IMyLargeTurretBase (vanilla, framework mod, most DLC) ---
    // Exclude any already added as TurretControlBlock
    var tcIds = new HashSet<long>();
    foreach (var tc in tcBlocks) tcIds.Add(tc.EntityId);

    var vanillaBlocks = new List<IMyLargeTurretBase>();
    GridTerminalSystem.GetBlocksOfType(vanillaBlocks, b =>
        b.IsSameConstructAs(Me) &&
        !tcIds.Contains(b.EntityId) &&
        (string.IsNullOrEmpty(_turretTag) || b.CustomName.Contains(_turretTag)));
    foreach (var vt in vanillaBlocks)
    {
        var entry = new TurretEntry();
        entry.Block   = vt;
        entry.Vanilla = vt;
        if (wcSet.Contains(vt.EntityId))
        {
            entry.Kind = TurretEntry.TurretKind.WeaponCore;
            _wcTurretCount++;
        }
        else
        {
            entry.Kind = TurretEntry.TurretKind.Vanilla;
            _vanillaTurretCount++;
        }
        _turrets.Add(entry);
    }

    // --- Pure WC blocks (not IMyLargeTurretBase, not IMyTurretControlBlock) ---
    // e.g. custom WC-only weapons that are just IMyTerminalBlock
    var addedIds = new HashSet<long>();
    foreach (var t in _turrets) addedIds.Add(t.Block.EntityId);
    foreach (var b in allBlocks)
    {
        if (addedIds.Contains(b.EntityId)) continue;
        if (!wcSet.Contains(b.EntityId)) continue;
        if (string.IsNullOrEmpty(_turretTag) || b.CustomName.Contains(_turretTag))
        {
            var entry = new TurretEntry();
            entry.Block = b;
            entry.Kind  = TurretEntry.TurretKind.WeaponCore;
            _turrets.Add(entry);
            _wcTurretCount++;
        }
    }

    // --- LCD surfaces ---
    if (!string.IsNullOrEmpty(_lcdTag))
    {
        foreach (var b in allBlocks)
        {
            if (!b.CustomName.Contains(_lcdTag)) continue;
            var p = b as IMyTextSurfaceProvider;
            if (p == null) continue;
            for (int i = 0; i < p.SurfaceCount; i++)
            {
                var s = p.GetSurface(i);
                s.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                _surfaces.Add(s);
            }
        }
    }

    Echo("RTC: " + _turrets.Count + " turrets"
        + " [V:" + _vanillaTurretCount
        + " TC:" + _tcTurretCount
        + " WC:" + _wcTurretCount + "]"
        + "  LCD:" + _surfaces.Count
        + "  WC:" + (_wcReady ? "YES" : "no"));
}

// ---- CONTROLLER CACHE -------------------------------------------------------
IMyShipController GetController()
{
    _controllerTick++;
    if (_controllerTick < 100 && _controller != null) return _controller;
    _controllerTick = 0;
    var list = new List<IMyShipController>();
    GridTerminalSystem.GetBlocksOfType(list, b => b.IsSameConstructAs(Me));
    _controller = null;
    foreach (var c in list)
    {
        if (c.CanControlShip) { _controller = c; break; }
    }
    if (_controller == null && list.Count > 0) _controller = list[0];
    return _controller;
}

// ---- STATUS LCD -------------------------------------------------------------
void DrawStatus()
{
    if (_surfaces.Count == 0) return;

    int total    = _turrets.Count;
    int engaging = 0;
    foreach (var t in _turrets)
        if (t.IsWorking && t.HasTarget()) engaging++;

    string text = "=== RTC v2.0 ===\n"
        + "Turrets  : " + total
            + " [V:" + _vanillaTurretCount
            + " TC:" + _tcTurretCount
            + " WC:" + _wcTurretCount + "]\n"
        + "WC Active: " + (_wcReady ? "YES" : "no") + "\n"
        + "Engaging : " + engaging + "\n"
        + "Sync     : " + (_syncTurrets ? "ON" : "OFF") + "\n"
        + "Lead     : " + (_leadComp   ? "ON" : "OFF") + "\n"
        + "ProjSpd  : " + _projSpeed + " m/s\n"
        + "Idle     : " + ((int)_idleTimer) + " / " + ((int)_idleSeconds) + "s\n"
        + "Park Az  : " + _parkAzimuth + " deg\n"
        + "Park El  : " + _parkElevation + " deg\n"
        + "State    : " + _statusLine + "\n";

    foreach (var s in _surfaces)
        s.WriteText(text);
}

// ---- CONFIG -----------------------------------------------------------------
void LoadConfig()
{
    var ini = new MyIni();
    if (!ini.TryParse(Me.CustomData) || !ini.ContainsSection(SECTION))
        WriteDefaults(ini);

    _turretTag    = ini.Get(SECTION, "TurretTag").ToString(DEF_TAG);
    _lcdTag       = ini.Get(SECTION, "LcdTag").ToString(DEF_LCD_TAG);
    _parkAzimuth  = (float)ini.Get(SECTION, "ParkAzimuth").ToDouble(DEF_PARK_AZ);
    _parkElevation = (float)ini.Get(SECTION, "ParkElevation").ToDouble(DEF_PARK_EL);
    _idleSeconds  = (float)ini.Get(SECTION, "IdleSeconds").ToDouble(DEF_IDLE_SEC);
    _syncTurrets  = ini.Get(SECTION, "SyncTurrets").ToBoolean(DEF_SYNC);
    _leadComp     = ini.Get(SECTION, "LeadCompensation").ToBoolean(DEF_LEAD);
    _projSpeed    = (float)ini.Get(SECTION, "ProjectileSpeed").ToDouble(DEF_PROJ_SPD);
    _useWC        = ini.Get(SECTION, "UseWeaponCore").ToBoolean(DEF_USE_WC);
}

void WriteDefaults(MyIni ini)
{
    ini.Set(SECTION, "TurretTag",        DEF_TAG);
    ini.Set(SECTION, "LcdTag",           DEF_LCD_TAG);
    ini.Set(SECTION, "ParkAzimuth",      DEF_PARK_AZ);
    ini.Set(SECTION, "ParkElevation",    DEF_PARK_EL);
    ini.Set(SECTION, "IdleSeconds",      DEF_IDLE_SEC);
    ini.Set(SECTION, "SyncTurrets",      DEF_SYNC);
    ini.Set(SECTION, "LeadCompensation", DEF_LEAD);
    ini.Set(SECTION, "ProjectileSpeed",  DEF_PROJ_SPD);
    ini.Set(SECTION, "UseWeaponCore",    DEF_USE_WC);
    Me.CustomData = ini.ToString();
}

// =============================================================================
//  WcPbApi - WeaponCore Programmable Block API wrapper
//  Gracefully no-ops if WeaponCore is not loaded.
// =============================================================================
public class WcPbApi
{
    Action<IMyTerminalBlock, IDictionary<MyDetectedEntityInfo, float>> _getSortedThreats;
    Func<IMyTerminalBlock, bool>                _hasCoreWeapon;
    Func<IMyTerminalBlock, long, int, bool>     _setAiFocus;
    Action<IMyTerminalBlock, long, int>         _setWeaponTarget;
    Func<long, bool>                            _hasGridAi;

    public bool Activate(IMyTerminalBlock pb)
    {
        var prop = pb.GetProperty("WcPbAPI");
        if (prop == null) return false;
        var dict = prop.As<IReadOnlyDictionary<string, Delegate>>().GetValue(pb);
        if (dict == null) return false;
        Assign(dict, "GetSortedThreats", ref _getSortedThreats);
        Assign(dict, "HasCoreWeapon",    ref _hasCoreWeapon);
        Assign(dict, "SetAiFocus",       ref _setAiFocus);
        Assign(dict, "SetWeaponTarget",  ref _setWeaponTarget);
        Assign(dict, "HasGridAi",        ref _hasGridAi);
        return true;
    }

    void Assign<T>(IReadOnlyDictionary<string, Delegate> d, string key, ref T field)
        where T : class
    {
        Delegate del;
        if (d.TryGetValue(key, out del)) field = del as T;
    }

    public void GetSortedThreats(IMyTerminalBlock pb,
        IDictionary<MyDetectedEntityInfo, float> col)
    { if (_getSortedThreats != null) _getSortedThreats(pb, col); }

    public bool HasCoreWeapon(IMyTerminalBlock b)
    { return _hasCoreWeapon != null && _hasCoreWeapon(b); }

    public bool SetAiFocus(IMyTerminalBlock pb, long entityId, int priority)
    { return _setAiFocus != null && _setAiFocus(pb, entityId, priority); }

    public void SetWeaponTarget(IMyTerminalBlock weapon, long entityId, int weaponId)
    { if (_setWeaponTarget != null) _setWeaponTarget(weapon, entityId, weaponId); }

    public bool HasGridAi(long gridEntityId)
    { return _hasGridAi != null && _hasGridAi(gridEntityId); }
}
