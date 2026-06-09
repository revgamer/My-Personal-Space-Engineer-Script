// =============================================================================
//  SKP RADAR  -  Space Engineers Programmable Block script
// -----------------------------------------------------------------------------
//  A PPI-style "scope" radar that fuses every detection source on your grid:
//
//     * Sensor blocks      -> short range proximity contacts
//     * Camera blocks       -> auto-scanning raycast (also detects asteroids)
//     * WeaponCore          -> long range weapon/turret targets (auto-detected,
//                              gracefully ignored if the mod is not present)
//     * Antennas / IGC      -> optional "radar net": friendly grids share contacts
//
//  Contacts are drawn on an LCD as blips with vertical altitude "stalks"
//  (up = above you, down = below you), colored by relationship.
//
//  HOW TO USE
//  ----------
//  1. Paste this whole file into a Programmable Block's code editor.
//  2. Build at least one LCD / cockpit screen whose NAME contains the LCD tag
//     (default: [Radar]).  A wide/square screen looks best.
//  3. (Optional) Build sensors, cameras, antennas. Enable the cameras.
//  4. Hit "Run" once. Configure via the block's Custom Data (see below).
//
//  CUSTOM DATA (auto-filled on first run):
//     [Radar]
//     LcdTag=Radar          ; screens whose name contains this get the scope
//     InfoTag=RadarInfo     ; screens whose name contains this get the contact list
//     WideTag=RadarWide     ; screens whose name contains this get the wide-area
//                           ;   top-down scope (fixed long range, see below)
//     WideRange=50000       ; radius (metres) of the wide-area scope
//     WideAngle=0           ; tilt of the wide scope (0 = flat top-down drop view)
//     Reference=            ; name of cockpit/remote used for orientation
//                           ;   (blank = first ship controller, else the PB)
//     ProjectionAngle=55    ; scope tilt in degrees (0 = flat top-down, ~55 = angled)
//     AutoRange=true        ; auto-set scope radius to your turrets' max range
//     Range=5000            ; scope radius (metres) when AutoRange is off
//     HoldSeconds=4         ; keep a stale contact this long after last seen
//     ScanCameras=true      ; auto-raycast through cameras
//     ScanAsteroids=true    ; include voxels (asteroids/planets) as contacts
//     UseWeaponCore=true    ; use WeaponCore targets if the mod is available
//     Network=false         ; share/receive contacts over IGC (uses antennas)
//     NetworkTag=SKPRadar  ; IGC channel for the radar net
//     ShowLabels=true       ; draw distance text next to each blip
//     FriendlyNames=        ; comma-separated name fragments to force-color BLUE
//                           ;   (your fleet, e.g. "Odyssey,Reclaimer,SKP")
//     WeaponsTag=RadarWeapons ; screens whose name contains this get the weapons
//                           ;   fire-control panel
//     WeaponGroup=          ; name of a block group of WeaponCore weapons to fire
//                           ;   (blank = every WeaponCore weapon on the grid)
//     FireRange=8000        ; max engagement range (metres) for barrage/aim
//     TargetLatch=6         ; stay locked on a target this long before switching
//     AimGain=6             ; gyro turn responsiveness when Aim is on
//     TickRate=10           ; script update period: 1, 10, or 100 game ticks
//                           ;   (higher = lighter on the server, less responsive)
//
//  RADAR COMMANDS (send as the PB's "Run" argument or via a button):
//     reload                ; re-read Custom Data and re-scan the grid's blocks
//     rangeup / rangedown   ; manually zoom the tactical scope out / in
//     auto                  ; return the scope to auto range (turret max range)
//     clear                 ; wipe all current contacts
//
//  WEAPONS COMMANDS (send as the PB's "Run" argument or via a button):
//     arm / disarm          ; master safety; nothing fires unless ARMED
//     aim                   ; toggle gyro slew-to-target
//     barrage               ; toggle distributed fire: spread every weapon across
//                           ;   all locked targets and fire at once
// =============================================================================

// ----------------------------- CONFIG FIELDS --------------------------------
string _lcdTag        = "[Radar]";
string _infoTag       = "[RadarInfo]";
string _wideTag       = "[RadarWide]";
string _referenceName = "";
double _projAngle     = 55;       // scope tilt in degrees (0 = flat)
double _wideRange     = 50000;    // wide-area scope radius (metres)
double _wideAngle     = 0;        // wide-area scope tilt (0 = flat top-down)
bool   _autoRange     = true;
double _range         = 5000;
double _turretRange   = 5000;     // computed max turret targeting range
double _holdSeconds   = 4;
bool   _scanCameras   = true;
bool   _scanAsteroids = true;
bool   _useWeaponCore = true;
bool   _network       = false;
string _networkTag    = "SKPRadar";
bool   _showLabels    = true;
string _friendlyNames = "";
readonly List<string> _friendlyList = new List<string>();

string _weaponsTag    = "[RadarWeapons]";
string _weaponGroup   = "";        // block group of WC weapons to control (blank = all)
double _fireRange     = 8000;      // max engagement range for barrage / aim
double _targetLatch   = 6;         // seconds to stay on a target before re-selecting
double _aimGain       = 6;         // gyro responsiveness when aiming
int    _tickRate      = 10;        // script update period in game ticks (1/10/100)

// ----------------------------- RUNTIME STATE --------------------------------
readonly MyIni _ini = new MyIni();
readonly Dictionary<long, Track> _tracks = new Dictionary<long, Track>();

readonly List<IMySensorBlock>      _sensors   = new List<IMySensorBlock>();
readonly List<IMyCameraBlock>      _cameras   = new List<IMyCameraBlock>();
readonly List<IMyRadioAntenna>     _antennas  = new List<IMyRadioAntenna>();
readonly List<IMyLargeTurretBase>  _turrets   = new List<IMyLargeTurretBase>();
readonly List<IMyTurretControlBlock> _turretCtl = new List<IMyTurretControlBlock>();
readonly List<IMyTerminalBlock>    _wcWeapons = new List<IMyTerminalBlock>();
readonly List<IMyTextSurface>      _surfaces  = new List<IMyTextSurface>();
readonly List<IMyTextSurface>      _infoSurfaces = new List<IMyTextSurface>();
readonly List<IMyTextSurface>      _strategicSurfaces = new List<IMyTextSurface>();
readonly List<IMyTextSurface>      _weaponSurfaces = new List<IMyTextSurface>();
readonly List<IMyGyro>             _gyros        = new List<IMyGyro>();
readonly List<IMyTerminalBlock>    _groupWeapons = new List<IMyTerminalBlock>();
readonly List<Track>               _sortList  = new List<Track>();
readonly List<Track>               _barrageTargets = new List<Track>(); // scratch for distribution
Vector3D _drawOrigin;
IMyShipController _reference;

readonly List<MyDetectedEntityInfo> _scratch = new List<MyDetectedEntityInfo>();
readonly Dictionary<MyDetectedEntityInfo, float> _wcThreats = new Dictionary<MyDetectedEntityInfo, float>();
readonly HashSet<long> _myGridIds = new HashSet<long>(); // own ship + subgrids
readonly StringBuilder _status = new StringBuilder();

readonly WcPbApi _wc = new WcPbApi();
bool _wcReady;

IMyBroadcastListener _listener;

double _time;          // seconds since program start
int    _camIndex;      // round-robin camera scan
int    _refreshTimer;  // periodic block rescan
string _focusDbg = "focus=?"; // WeaponCore AI-focus diagnostic
string _turretDbg = "tgt=?";  // turret/threat-count diagnostic

// Weapons fire-control state (persisted in Storage so it survives recompiles).
bool   _armed;                 // master arm; nothing fires unless true
bool   _aim;                   // gyro slew-to-target
bool   _barrage;               // distribute all weapons across all targets & fire
long   _wpnTargetId;           // entity id currently engaged (0 = none)
double _wpnLatchTime;          // _time when the current target was acquired
bool   _groupFiring;           // last commanded fire state (avoid spamming WC)
bool   _gyrosOverridden;       // are we currently driving the gyros?
int    _barrageTargetCount;    // targets engaged on the last barrage pass (panel)

public Program()
{
    LoadConfig();
    LoadState();
    Rebuild();
    Runtime.UpdateFrequency = FreqFromTicks(_tickRate);
}

// Map a tick period (1/10/100) to an update frequency. Higher = lighter on the
// server but less responsive aiming/drawing.
UpdateFrequency FreqFromTicks(int ticks)
{
    if (ticks <= 1)   return UpdateFrequency.Update1;
    if (ticks >= 100) return UpdateFrequency.Update100;
    return UpdateFrequency.Update10;
}

// Persist the weapon toggles so an "armed/aiming" ship stays that way after a
// recompile or world reload. Format: "armed;aim;barrage".
public void Save()
{
    Storage = (_armed ? "1" : "0") + ";" + (_aim ? "1" : "0") + ";" + (_barrage ? "1" : "0");
}

void LoadState()
{
    if (string.IsNullOrEmpty(Storage)) return;
    var f = Storage.Split(';');
    if (f.Length >= 3)
    {
        _armed   = f[0] == "1";
        _aim     = f[1] == "1";
        _barrage = f[2] == "1";
    }
}

public void Main(string argument, UpdateType updateSource)
{
    _time += Runtime.TimeSinceLastRun.TotalSeconds;

    if (!string.IsNullOrWhiteSpace(argument))
        HandleCommand(argument.Trim().ToLowerInvariant());

    // Periodically re-scan the grid for added/removed blocks (every ~10s).
    if (++_refreshTimer >= 60) { _refreshTimer = 0; Rebuild(); }

    GatherContacts();
    ReceiveNetwork();
    BroadcastNetwork();
    Prune();
    UpdateAutoRange();
    UpdateWeapons();
    DrawAll();
    WriteEcho();
}

// =============================================================================
//  CONFIG
// =============================================================================
void LoadConfig()
{
    bool parsed = _ini.TryParse(Me.CustomData);
    if (!parsed || !_ini.ContainsSection("Radar"))
    {
        _ini.Clear();
        _ini.AddSection("Radar");
        _ini.Set("Radar", "LcdTag",        _lcdTag);
        _ini.Set("Radar", "InfoTag",       _infoTag);
        _ini.Set("Radar", "WideTag",       _wideTag);
        _ini.Set("Radar", "Reference",     _referenceName);
        _ini.Set("Radar", "ProjectionAngle", _projAngle);
        _ini.Set("Radar", "WideRange",     _wideRange);
        _ini.Set("Radar", "WideAngle",     _wideAngle);
        _ini.Set("Radar", "AutoRange",     _autoRange);
        _ini.Set("Radar", "Range",         _range);
        _ini.Set("Radar", "HoldSeconds",   _holdSeconds);
        _ini.Set("Radar", "ScanCameras",   _scanCameras);
        _ini.Set("Radar", "ScanAsteroids", _scanAsteroids);
        _ini.Set("Radar", "UseWeaponCore", _useWeaponCore);
        _ini.Set("Radar", "Network",       _network);
        _ini.Set("Radar", "NetworkTag",    _networkTag);
        _ini.Set("Radar", "ShowLabels",    _showLabels);
        _ini.Set("Radar", "FriendlyNames", _friendlyNames);
        _ini.Set("Radar", "WeaponsTag",    _weaponsTag);
        _ini.Set("Radar", "WeaponGroup",   _weaponGroup);
        _ini.Set("Radar", "FireRange",     _fireRange);
        _ini.Set("Radar", "TargetLatch",   _targetLatch);
        _ini.Set("Radar", "AimGain",       _aimGain);
        _ini.Set("Radar", "TickRate",      _tickRate);
        Me.CustomData = _ini.ToString();
        BuildFriendlyList();
        return;
    }

    _lcdTag        = _ini.Get("Radar", "LcdTag").ToString(_lcdTag);
    _infoTag       = _ini.Get("Radar", "InfoTag").ToString(_infoTag);
    _wideTag       = _ini.Get("Radar", "WideTag").ToString(_wideTag);
    _referenceName = _ini.Get("Radar", "Reference").ToString(_referenceName);
    _projAngle     = _ini.Get("Radar", "ProjectionAngle").ToDouble(_projAngle);
    _wideRange     = _ini.Get("Radar", "WideRange").ToDouble(_wideRange);
    _wideAngle     = _ini.Get("Radar", "WideAngle").ToDouble(_wideAngle);
    _autoRange     = _ini.Get("Radar", "AutoRange").ToBoolean(_autoRange);
    _range         = _ini.Get("Radar", "Range").ToDouble(_range);
    _holdSeconds   = _ini.Get("Radar", "HoldSeconds").ToDouble(_holdSeconds);
    _scanCameras   = _ini.Get("Radar", "ScanCameras").ToBoolean(_scanCameras);
    _scanAsteroids = _ini.Get("Radar", "ScanAsteroids").ToBoolean(_scanAsteroids);
    _useWeaponCore = _ini.Get("Radar", "UseWeaponCore").ToBoolean(_useWeaponCore);
    _network       = _ini.Get("Radar", "Network").ToBoolean(_network);
    _networkTag    = _ini.Get("Radar", "NetworkTag").ToString(_networkTag);
    _showLabels    = _ini.Get("Radar", "ShowLabels").ToBoolean(_showLabels);
    _friendlyNames = _ini.Get("Radar", "FriendlyNames").ToString(_friendlyNames);
    _weaponsTag    = _ini.Get("Radar", "WeaponsTag").ToString(_weaponsTag);
    _weaponGroup   = _ini.Get("Radar", "WeaponGroup").ToString(_weaponGroup);
    _fireRange     = _ini.Get("Radar", "FireRange").ToDouble(_fireRange);
    _targetLatch   = _ini.Get("Radar", "TargetLatch").ToDouble(_targetLatch);
    _aimGain       = _ini.Get("Radar", "AimGain").ToDouble(_aimGain);
    _tickRate      = _ini.Get("Radar", "TickRate").ToInt32(_tickRate);
    if (_range < 100) _range = 100;
    if (_wideRange < 1000) _wideRange = 1000;
    if (_fireRange < 100) _fireRange = 100;
    BuildFriendlyList();
}

// Parse the comma-separated FriendlyNames into a lowercase lookup list.
void BuildFriendlyList()
{
    _friendlyList.Clear();
    foreach (var part in _friendlyNames.Split(','))
    {
        var p = part.Trim().ToLowerInvariant();
        if (p.Length > 0) _friendlyList.Add(p);
    }
}

bool IsFriendlyName(string name)
{
    if (_friendlyList.Count == 0 || string.IsNullOrEmpty(name)) return false;
    var n = name.ToLowerInvariant();
    foreach (var f in _friendlyList)
        if (n.Contains(f)) return true;
    return false;
}

void HandleCommand(string cmd)
{
    if (cmd == "reload") { LoadConfig(); Rebuild(); Runtime.UpdateFrequency = FreqFromTicks(_tickRate); }
    else if (cmd == "rangeup")   { _autoRange = false; _range *= 1.5; SaveRange(); }
    else if (cmd == "rangedown") { _autoRange = false; _range = Math.Max(100, _range / 1.5); SaveRange(); }
    else if (cmd == "auto")      { _autoRange = true; SaveRange(); UpdateAutoRange(); }
    else if (cmd == "clear")     { _tracks.Clear(); }
    else if (cmd == "arm")       { _armed = true;  Save(); }
    else if (cmd == "disarm")    { _armed = false; SetGroupFire(false); ClearWeaponTargets(); ReleaseGyros(); Save(); }
    else if (cmd == "aim")       { _aim = !_aim; if (!_aim) ReleaseGyros(); Save(); }
    else if (cmd == "barrage")   { _barrage = !_barrage; if (!_barrage) { SetGroupFire(false); ClearWeaponTargets(); } Save(); }
}

void SaveRange()
{
    _ini.TryParse(Me.CustomData);
    _ini.Set("Radar", "AutoRange", _autoRange);
    _ini.Set("Radar", "Range", Math.Round(_range));
    Me.CustomData = _ini.ToString();
}

// =============================================================================
//  BLOCK DISCOVERY
// =============================================================================
void Rebuild()
{
    _sensors.Clear(); _cameras.Clear(); _antennas.Clear(); _surfaces.Clear();
    _turrets.Clear(); _turretCtl.Clear(); _infoSurfaces.Clear(); _strategicSurfaces.Clear();
    _weaponSurfaces.Clear(); _gyros.Clear();

    GridTerminalSystem.GetBlocksOfType(_sensors,   b => b.IsSameConstructAs(Me));
    GridTerminalSystem.GetBlocksOfType(_cameras,   b => b.IsSameConstructAs(Me));
    GridTerminalSystem.GetBlocksOfType(_antennas,  b => b.IsSameConstructAs(Me));
    GridTerminalSystem.GetBlocksOfType(_turrets,   b => b.IsSameConstructAs(Me));
    GridTerminalSystem.GetBlocksOfType(_turretCtl, b => b.IsSameConstructAs(Me));
    GridTerminalSystem.GetBlocksOfType(_gyros,     b => b.IsSameConstructAs(Me));

    // Record every grid EntityId of our own construct (ship + subgrids) so we
    // never plot ourselves as a contact -- the centre triangle already is us.
    _myGridIds.Clear();
    var ownBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType(ownBlocks, b => b.IsSameConstructAs(Me));
    foreach (var b in ownBlocks) _myGridIds.Add(b.CubeGrid.EntityId);

    // Pick an orientation reference: named controller -> any controller -> PB.
    _reference = null;
    var controllers = new List<IMyShipController>();
    GridTerminalSystem.GetBlocksOfType(controllers, b => b.IsSameConstructAs(Me));
    if (!string.IsNullOrEmpty(_referenceName))
        _reference = controllers.Find(c => c.CustomName.Contains(_referenceName));
    if (_reference == null && controllers.Count > 0)
        _reference = controllers.Find(c => c.CanControlShip) ?? controllers[0];

    // Collect target surfaces.
    var providerBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType(providerBlocks, b =>
        b is IMyTextSurfaceProvider && b.IsSameConstructAs(Me)
        && (b.CustomName.Contains(_lcdTag) || b.CustomName.Contains(_infoTag)
            || b.CustomName.Contains(_wideTag) || b.CustomName.Contains(_weaponsTag)));
    foreach (var tb in providerBlocks)
    {
        var p = tb as IMyTextSurfaceProvider;
        List<IMyTextSurface> target;
        if (tb.CustomName.Contains(_infoTag))         target = _infoSurfaces;
        else if (tb.CustomName.Contains(_wideTag))    target = _strategicSurfaces;
        else if (tb.CustomName.Contains(_weaponsTag)) target = _weaponSurfaces;
        else                                          target = _surfaces;
        for (int i = 0; i < p.SurfaceCount; i++)
            target.Add(p.GetSurface(i));
    }
    foreach (var s in _surfaces)          { s.ContentType = ContentType.SCRIPT; s.Script = ""; }
    foreach (var s in _infoSurfaces)      { s.ContentType = ContentType.SCRIPT; s.Script = ""; }
    foreach (var s in _strategicSurfaces) { s.ContentType = ContentType.SCRIPT; s.Script = ""; }
    foreach (var s in _weaponSurfaces)    { s.ContentType = ContentType.SCRIPT; s.Script = ""; }

    foreach (var cam in _cameras) cam.EnableRaycast = _scanCameras;

    // WeaponCore: try to activate; harmless no-op if the mod isn't loaded.
    _wcReady = false;
    if (_useWeaponCore)
    {
        try { _wcReady = _wc.Activate(Me); } catch { _wcReady = false; }
    }

    _wcWeapons.Clear();
    if (_wcReady)
    {
        var allBlocks = new List<IMyTerminalBlock>();
        GridTerminalSystem.GetBlocksOfType(allBlocks, b => b.IsSameConstructAs(Me));
        foreach (var b in allBlocks)
        {
            bool isWeapon = false;
            try { isWeapon = _wc.HasCoreWeapon(b); } catch { }
            if (isWeapon) _wcWeapons.Add(b);
        }
    }

    _groupWeapons.Clear();
    if (!string.IsNullOrWhiteSpace(_weaponGroup))
    {
        var grp = GridTerminalSystem.GetBlockGroupWithName(_weaponGroup);
        if (grp != null)
        {
            var grpBlocks = new List<IMyTerminalBlock>();
            grp.GetBlocks(grpBlocks, b => b.IsSameConstructAs(Me));
            foreach (var b in grpBlocks)
            {
                bool isWeapon = false;
                try { isWeapon = !_wcReady || _wc.HasCoreWeapon(b); } catch { }
                if (isWeapon) _groupWeapons.Add(b);
            }
        }
    }
    else
    {
        _groupWeapons.AddRange(_wcWeapons);
    }

    if (_network)
    {
        if (_listener == null || _listener.Tag != _networkTag)
            _listener = IGC.RegisterBroadcastListener(_networkTag);
    }

    UpdateAutoRange();
}

void UpdateAutoRange()
{
    if (!_autoRange) return;
    double max = 0;
    foreach (var t in _turrets)
        if (t != null && t.IsFunctional && t.Range > max) max = t.Range;
    foreach (var c in _turretCtl)
        if (c != null && c.IsFunctional && c.Range > max) max = c.Range;
    _turretRange = max;
    if (max >= 100) _range = max;
}

// =============================================================================
//  CONTACT GATHERING
// =============================================================================
void GatherContacts()
{
    _turretDbg = "tgt=none";

    foreach (var s in _sensors)
    {
        if (!s.IsWorking) continue;
        _scratch.Clear();
        s.DetectedEntities(_scratch);
        foreach (var info in _scratch) Ingest(info, "S");
    }

    if (_wcReady && _wc.HasGridAi(Me.CubeGrid.EntityId))
    {
        _wcThreats.Clear();
        try { _wc.GetSortedThreats(Me, _wcThreats); } catch { }
        foreach (var kv in _wcThreats) Ingest(kv.Key, "W", true);

        _scratch.Clear();
        try { _wc.GetObstructions(Me, _scratch); } catch { }
        foreach (var info in _scratch) Ingest(info, "W", false);

        try
        {
            var focus = _wc.GetAiFocus(Me.CubeGrid.EntityId, 0);
            if (!focus.IsEmpty())
            {
                Ingest(focus, "W", true);
                _focusDbg = "focus=" + focus.Name;
            }
            else _focusDbg = "focus=none";
        }
        catch { _focusDbg = "focus=err"; }

        _turretDbg = "threats=" + _wcThreats.Count;
    }
    else if (_wcReady)
    {
        _focusDbg = "focus=no gridAI";
    }

    foreach (var tur in _turrets)
    {
        if (tur == null || !tur.IsFunctional || !tur.HasTarget) continue;
        var info = tur.GetTargetedEntity();
        if (!info.IsEmpty()) Ingest(info, "T");
    }
    foreach (var ctl in _turretCtl)
    {
        if (ctl == null || !ctl.IsFunctional || !ctl.HasTarget) continue;
        var info = ctl.GetTargetedEntity();
        if (!info.IsEmpty()) Ingest(info, "T");
    }

    if (_scanCameras && _cameras.Count > 0)
        ScanCameras();
}

void ScanCameras()
{
    float[] pitches = { 0f, 8f, -8f, 0f, 0f };
    float[] yaws    = { 0f, 0f,  0f, 10f, -10f };

    var cam = _cameras[_camIndex % _cameras.Count];
    _camIndex++;
    if (cam == null || !cam.IsWorking || !cam.EnableRaycast) return;

    double dist = Math.Min(_range, cam.AvailableScanRange);
    if (dist < 50) return;

    int slot = _camIndex % pitches.Length;
    if (!cam.CanScan(dist)) return;
    var hit = cam.Raycast(dist, pitches[slot], yaws[slot]);
    if (!hit.IsEmpty())
        Ingest(hit, "C");
}

void Ingest(MyDetectedEntityInfo info, string source, bool threat = false)
{
    if (info.IsEmpty()) return;
    if (_myGridIds.Contains(info.EntityId)) return;

    bool voxel = info.Type == MyDetectedEntityType.Asteroid
              || info.Type == MyDetectedEntityType.Planet;
    if (voxel && !_scanAsteroids) return;

    Track t;
    if (!_tracks.TryGetValue(info.EntityId, out t))
    {
        t = new Track();
        _tracks[info.EntityId] = t;
    }

    t.Id        = info.EntityId;
    t.Name      = string.IsNullOrEmpty(info.Name) ? info.Type.ToString() : info.Name;
    t.Position  = info.Position;
    t.Velocity  = info.Velocity;
    t.Type      = info.Type;
    t.LastSeen  = _time;
    t.Source    = source;
    t.Remote    = false;
    if (threat) t.Threat = true;
    if (info.Relationship != MyRelationsBetweenPlayerAndBlock.NoOwnership)
        t.Relation = info.Relationship;
}

void Prune()
{
    var dead = new List<long>();
    foreach (var kv in _tracks)
        if (_time - kv.Value.LastSeen > _holdSeconds)
            dead.Add(kv.Key);
    foreach (var id in dead) _tracks.Remove(id);
}

// =============================================================================
//  IGC RADAR NET
// =============================================================================
void BroadcastNetwork()
{
    if (!_network) return;
    bool haveAntenna = false;
    foreach (var a in _antennas)
        if (a.IsWorking && a.EnableBroadcasting) { haveAntenna = true; break; }
    if (!haveAntenna) return;

    var sb = new StringBuilder();
    foreach (var kv in _tracks)
    {
        var t = kv.Value;
        if (t.Remote) continue;
        sb.Append(t.Id).Append(';')
          .Append(Math.Round(t.Position.X)).Append(';')
          .Append(Math.Round(t.Position.Y)).Append(';')
          .Append(Math.Round(t.Position.Z)).Append(';')
          .Append((int)t.Relation).Append(';')
          .Append((int)t.Type).Append('|');
    }
    if (sb.Length > 0)
        IGC.SendBroadcastMessage(_networkTag, sb.ToString());
}

void ReceiveNetwork()
{
    if (!_network || _listener == null) return;
    while (_listener.HasPendingMessage)
    {
        var msg = _listener.AcceptMessage();
        var data = msg.Data as string;
        if (string.IsNullOrEmpty(data)) continue;

        var entries = data.Split('|');
        foreach (var e in entries)
        {
            var f = e.Split(';');
            if (f.Length < 6) continue;
            long id; double x, y, z; int rel, type;
            if (!long.TryParse(f[0], out id)) continue;
            if (!double.TryParse(f[1], out x)) continue;
            if (!double.TryParse(f[2], out y)) continue;
            if (!double.TryParse(f[3], out z)) continue;
            if (!int.TryParse(f[4], out rel)) continue;
            if (!int.TryParse(f[5], out type)) continue;
            if (id == Me.CubeGrid.EntityId) continue;

            Track t;
            if (!_tracks.TryGetValue(id, out t)) { t = new Track(); _tracks[id] = t; }
            if (!t.Remote && _time - t.LastSeen < 1.0) continue;
            t.Id       = id;
            t.Position = new Vector3D(x, y, z);
            t.Relation = (MyRelationsBetweenPlayerAndBlock)rel;
            t.Type     = (MyDetectedEntityType)type;
            t.Name     = t.Type.ToString();
            t.LastSeen = _time;
            t.Source   = "N";
            t.Remote   = true;
        }
    }
}

// =============================================================================
//  RENDERING
// =============================================================================
void DrawAll()
{
    if (_reference == null) return;
    MatrixD m = _reference.WorldMatrix;
    Vector3D origin = m.Translation;
    _drawOrigin = origin;

    foreach (var surface in _surfaces)
        DrawScope(surface, m, origin, _range, _projAngle, null, false);
    foreach (var surface in _strategicSurfaces)
        DrawScope(surface, m, origin, _wideRange, _wideAngle, "WIDE", true);
    foreach (var surface in _infoSurfaces)
        DrawInfo(surface, m, origin);
    foreach (var surface in _weaponSurfaces)
        DrawWeapons(surface, origin);
}

void DrawScope(IMyTextSurface surface, MatrixD m, Vector3D origin,
               double range, double projAngle, string title, bool flat)
{
    Vector2 size = surface.TextureSize;
    Vector2 view = surface.SurfaceSize;
    Vector2 pad  = (size - view) * 0.5f;
    Vector2 center = pad + view * 0.5f;

    float tilt = (float)Math.Cos(projAngle * Math.PI / 180.0);
    if (tilt < 0.2f) tilt = 0.2f;
    if (tilt > 1f)   tilt = 1f;
    float scope = Math.Min(view.X * 0.46f, view.Y * 0.42f / tilt);

    var frame = surface.DrawFrame();

    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", pad + view * 0.5f,
        view, Color.Black));

    Color grid = new Color(0, 120, 160);
    Color faint = new Color(0, 70, 95);

    DrawEllipse(frame, center, scope,         scope * tilt,         grid,  64, 1.6f);
    DrawEllipse(frame, center, scope * 0.75f, scope * 0.75f * tilt, faint, 56, 1.2f);
    DrawEllipse(frame, center, scope * 0.50f, scope * 0.50f * tilt, faint, 48, 1.2f);
    DrawEllipse(frame, center, scope * 0.25f, scope * 0.25f * tilt, faint, 40, 1.2f);

    DrawLine(frame, new Vector2(center.X - scope, center.Y),
                    new Vector2(center.X + scope, center.Y), faint, 1.2f);
    DrawLine(frame, new Vector2(center.X, center.Y - scope * tilt),
                    new Vector2(center.X, center.Y + scope * tilt), faint, 1.2f);

    float sweep = (float)(_time % 4.0 / 4.0 * Math.PI * 2.0);
    Vector2 sweepEnd = center + new Vector2(
        (float)Math.Sin(sweep) * scope, -(float)Math.Cos(sweep) * scope * tilt);
    DrawLine(frame, center, sweepEnd, new Color(0, 200, 120, 90), 2f);

    string rangeText = range >= 1000 ? (range / 1000.0).ToString("0.#") + " km" : Math.Round(range) + " m";
    AddText(frame, "RANGE " + rangeText,
        new Vector2(center.X, pad.Y + 6), 0.55f, grid, TextAlignment.CENTER);
    if (!string.IsNullOrEmpty(title))
        AddText(frame, title, new Vector2(pad.X + 6, pad.Y + 6), 0.5f, grid, TextAlignment.LEFT);

    frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", center,
        new Vector2(16, 18), Color.White, null, TextAlignment.CENTER, 0f));

    int shown = 0;
    foreach (var kv in _tracks)
    {
        if (DrawBlip(frame, kv.Value, m, origin, center, scope, tilt, range, flat)) shown++;
    }

    AddText(frame, "CONTACTS " + shown,
        new Vector2(center.X, pad.Y + view.Y - 22), 0.45f, grid, TextAlignment.CENTER);
    AddText(frame, SourceLine(),
        new Vector2(pad.X + 6, pad.Y + view.Y - 22), 0.4f, faint, TextAlignment.LEFT);

    frame.Dispose();
}

// =============================================================================
//  CONTACT LIST
// =============================================================================
void DrawInfo(IMyTextSurface surface, MatrixD m, Vector3D origin)
{
    Vector2 size = surface.TextureSize;
    Vector2 view = surface.SurfaceSize;
    Vector2 pad  = (size - view) * 0.5f;
    float w = view.X, h = view.Y;

    var frame = surface.DrawFrame();

    Color cyan  = new Color(0, 150, 200);
    Color faint = new Color(0, 80, 110);

    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", pad + view * 0.5f, view, Color.Black));
    AddText(frame, "RADAR CONTACTS", new Vector2(pad.X + w * 0.5f, pad.Y + 6),
        0.7f, cyan, TextAlignment.CENTER);

    _sortList.Clear();
    foreach (var kv in _tracks) _sortList.Add(kv.Value);
    _sortList.Sort(CompareContacts);

    float colName = pad.X + 10;
    float colDist = pad.X + w * 0.56f;
    float colBrg  = pad.X + w * 0.74f;
    float colType = pad.X + w * 0.88f;
    float y = pad.Y + 40;
    AddText(frame, "CONTACT", new Vector2(colName, y), 0.42f, faint, TextAlignment.LEFT);
    AddText(frame, "DIST",    new Vector2(colDist, y), 0.42f, faint, TextAlignment.LEFT);
    AddText(frame, "BRG",     new Vector2(colBrg,  y), 0.42f, faint, TextAlignment.LEFT);
    AddText(frame, "TYP",     new Vector2(colType, y), 0.42f, faint, TextAlignment.LEFT);
    y += 22;
    DrawLine(frame, new Vector2(pad.X + 6, y - 4), new Vector2(pad.X + w - 6, y - 4), faint, 1f);

    const float rowH = 20f;
    int maxRows = (int)((h - (y - pad.Y) - 24) / rowH);
    if (maxRows < 1) maxRows = 1;
    int count = Math.Min(_sortList.Count, maxRows);

    for (int i = 0; i < count; i++)
    {
        var t = _sortList[i];
        Color c = RelationColor(t);
        string name = ContactLabel(t);
        if (name.Length > 18) name = name.Substring(0, 18);
        double dist = Vector3D.Distance(t.Position, origin);

        AddText(frame, name,                 new Vector2(colName, y), 0.45f, c, TextAlignment.LEFT);
        AddText(frame, FormatDist(dist),     new Vector2(colDist, y), 0.45f, c, TextAlignment.LEFT);
        AddText(frame, Bearing(t, m, origin).ToString("000"), new Vector2(colBrg, y), 0.45f, c, TextAlignment.LEFT);
        AddText(frame, ShortType(t),         new Vector2(colType, y), 0.45f, c, TextAlignment.LEFT);
        y += rowH;
    }

    AddText(frame, _sortList.Count + " CONTACTS", new Vector2(pad.X + w * 0.5f, pad.Y + h - 20),
        0.4f, cyan, TextAlignment.CENTER);
    if (_sortList.Count > count)
        AddText(frame, "+" + (_sortList.Count - count), new Vector2(pad.X + w - 8, pad.Y + h - 20),
            0.4f, faint, TextAlignment.RIGHT);

    frame.Dispose();
}

int CompareContacts(Track a, Track b)
{
    int pa = ContactPriority(a), pb = ContactPriority(b);
    if (pa != pb) return pa.CompareTo(pb);
    double da = Vector3D.DistanceSquared(a.Position, _drawOrigin);
    double db = Vector3D.DistanceSquared(b.Position, _drawOrigin);
    return da.CompareTo(db);
}

int ContactPriority(Track t)
{
    if (IsVoxel(t)) return 3;
    if (t.Threat || t.Relation == MyRelationsBetweenPlayerAndBlock.Enemies) return 0;
    if (t.Relation == MyRelationsBetweenPlayerAndBlock.Neutral ||
        t.Relation == MyRelationsBetweenPlayerAndBlock.NoOwnership) return 1;
    return 2;
}

string FormatDist(double d)
{
    if (d >= 1000) return (d / 1000.0).ToString("0.0") + "km";
    return Math.Round(d) + "m";
}

string ShortType(Track t)
{
    if (IsVoxel(t)) return "AST";
    switch (t.Type)
    {
        case MyDetectedEntityType.LargeGrid:      return "LG";
        case MyDetectedEntityType.SmallGrid:      return "SG";
        case MyDetectedEntityType.CharacterHuman: return "CHR";
        case MyDetectedEntityType.CharacterOther: return "CHR";
        case MyDetectedEntityType.Meteor:         return "MET";
        default:                                  return "?";
    }
}

int Bearing(Track t, MatrixD m, Vector3D origin)
{
    Vector3D rel = t.Position - origin;
    double right = Vector3D.Dot(rel, m.Right);
    double fwd   = Vector3D.Dot(rel, m.Forward);
    double b = Math.Atan2(right, fwd) * 180.0 / Math.PI;
    if (b < 0) b += 360;
    return (int)Math.Round(b) % 360;
}

string ContactLabel(Track t)
{
    if (IsVoxel(t)) return "Asteroid";
    bool nameless = string.IsNullOrEmpty(t.Name) ||
        t.Name.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
    if (t.Threat && nameless) return "Hostile";
    if (nameless && (t.Type == MyDetectedEntityType.CharacterHuman ||
                     t.Relation == MyRelationsBetweenPlayerAndBlock.Owner)) return "Player";
    return t.Name;
}

bool DrawBlip(MySpriteDrawFrame frame, Track t, MatrixD m, Vector3D origin,
              Vector2 center, float scope, float tilt, double range, bool flat)
{
    Vector3D rel = t.Position - origin;
    double right = Vector3D.Dot(rel, m.Right);
    double fwd   = Vector3D.Dot(rel, m.Forward);
    double up    = Vector3D.Dot(rel, m.Up);

    double ground = Math.Sqrt(right * right + fwd * fwd);
    double norm = ground / range;

    Color col = RelationColor(t);

    if (norm > 1.0)
    {
        double gx = ground > 0.001 ? right / ground : 0.0;
        double gy = ground > 0.001 ? fwd   / ground : -1.0;
        Vector2 edge = center + new Vector2((float)gx * scope, -(float)gy * scope * tilt);
        frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", edge, new Vector2(7, 7), col));
        if (_showLabels)
        {
            string olbl = ContactLabel(t);
            if (olbl.Length > 14) olbl = olbl.Substring(0, 14);
            AddText(frame, olbl + " " + Math.Round(rel.Length()) + "m",
                edge + new Vector2(0, -10), 0.32f, col, TextAlignment.CENTER);
        }
        return true;
    }

    Vector2 g = new Vector2(
        center.X + (float)(right / range) * scope,
        center.Y - (float)(fwd   / range) * scope * tilt);

    float alt = 0f;
    if (!flat)
    {
        alt = (float)(up / range) * scope;
        alt = MathHelper.Clamp(alt, -scope * 0.6f, scope * 0.6f);
    }
    Vector2 blip = new Vector2(g.X, g.Y - alt);

    Color c = RelationColor(t);

    if (!flat)
    {
        DrawLine(frame, g, blip, new Color(c.R, c.G, c.B, (byte)200), 1.6f);
        frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", g, new Vector2(4, 4), c));
    }

    string icon = "Circle";
    Vector2 isize = new Vector2(9, 9);
    bool voxel = IsVoxel(t);
    if (voxel) { icon = "SquareSimple"; }
    else if (t.Type == MyDetectedEntityType.LargeGrid ||
             t.Type == MyDetectedEntityType.SmallGrid) { icon = "Triangle"; isize = new Vector2(11, 12); }

    frame.Add(new MySprite(SpriteType.TEXTURE, icon, blip, isize, c));

    if (t.Threat)
        DrawCircle(frame, blip, 11f, new Color(255, 255, 255), 16, 1.4f);

    if (_showLabels)
    {
        string label = ContactLabel(t);
        if (label.Length > 18) label = label.Substring(0, 18);
        AddText(frame, label,
            new Vector2(blip.X + 7, blip.Y - 8), 0.35f, c, TextAlignment.LEFT);
    }
    return true;
}

bool IsVoxel(Track t)
{
    if (t.Type == MyDetectedEntityType.Asteroid ||
        t.Type == MyDetectedEntityType.Planet) return true;
    if (string.IsNullOrEmpty(t.Name)) return false;
    var n = t.Name.ToLowerInvariant();
    return n.Contains("voxel") || n.Contains("asteroid");
}

Color RelationColor(Track t)
{
    if (IsVoxel(t))
        return new Color(80, 80, 80);
    if (t.Type == MyDetectedEntityType.Meteor)
        return new Color(255, 140, 0);

    if (IsFriendlyName(t.Name))
        return new Color(60, 140, 255);

    if (t.Threat)
        return new Color(255, 50, 50);

    switch (t.Relation)
    {
        case MyRelationsBetweenPlayerAndBlock.Enemies:     return new Color(255, 50, 50);
        case MyRelationsBetweenPlayerAndBlock.Neutral:     return new Color(255, 210, 40);
        case MyRelationsBetweenPlayerAndBlock.Owner:       return new Color(60, 140, 255);
        case MyRelationsBetweenPlayerAndBlock.NoOwnership: return new Color(255, 210, 40);
        default:                                           return new Color(60, 230, 90);
    }
}

string SourceLine()
{
    _status.Clear();
    _status.Append("SEN:").Append(_sensors.Count);
    _status.Append(" CAM:").Append(_cameras.Count);
    _status.Append(" WC:").Append(_wcReady ? "ON" : "--");
    if (_network) _status.Append(" NET");
    return _status.ToString();
}

void WriteEcho()
{
    Echo("REV RADAR SYSTEM");
    Echo("Tracks: " + _tracks.Count);
    Echo("Surfaces: " + _surfaces.Count + "  Info: " + _infoSurfaces.Count + "  Wide: " + _strategicSurfaces.Count);
    Echo("Sensors: " + _sensors.Count + "  Cameras: " + _cameras.Count);
    Echo("Turrets: " + _turrets.Count + "  CTC: " + _turretCtl.Count + "  " + _turretDbg);
    Echo("WC weapons: " + _wcWeapons.Count + "  Group: " + _groupWeapons.Count + "  Gyros: " + _gyros.Count);
    Echo("Weapons: " + (_armed ? "ARMED" : "disarmed") + "  Aim:" + (_aim ? "on" : "off") + "  Barrage:" + (_barrage ? "on" : "off") + "  " + (_groupFiring ? "FIRING" : ""));
    Echo("Antennas: " + _antennas.Count);
    Echo("WeaponCore: " + (_wcReady ? "active" : "not found") + "  " + _focusDbg);
    Echo("Reference: " + (_reference != null ? _reference.CustomName : "NONE - add a cockpit!"));
    Echo("Range: " + Math.Round(_range) + " m  (" + (_autoRange ? "auto/turret" : "manual") + ")");
    if (_surfaces.Count == 0)
        Echo("\n! No screen found. Name an LCD to contain \"" + _lcdTag + "\".");
}

// ----------------------------- DRAW HELPERS ---------------------------------
void DrawLine(MySpriteDrawFrame frame, Vector2 a, Vector2 b, Color color, float width)
{
    Vector2 d = b - a;
    float len = d.Length();
    if (len < 0.5f) return;
    Vector2 mid = (a + b) * 0.5f;
    float rot = (float)Math.Atan2(d.Y, d.X);
    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", mid,
        new Vector2(len, width), color, null, TextAlignment.CENTER, rot));
}

void DrawCircle(MySpriteDrawFrame frame, Vector2 c, float r, Color color,
                int segments, float width)
{
    DrawEllipse(frame, c, r, r, color, segments, width);
}

void DrawEllipse(MySpriteDrawFrame frame, Vector2 c, float rx, float ry, Color color,
                 int segments, float width)
{
    Vector2 prev = c + new Vector2(rx, 0);
    for (int i = 1; i <= segments; i++)
    {
        float a = (float)(i / (double)segments * Math.PI * 2.0);
        Vector2 p = c + new Vector2((float)Math.Cos(a) * rx, (float)Math.Sin(a) * ry);
        DrawLine(frame, prev, p, color, width);
        prev = p;
    }
}

void AddText(MySpriteDrawFrame frame, string text, Vector2 pos, float scale,
             Color color, TextAlignment align)
{
    var s = MySprite.CreateText(text, "White", color, scale, align);
    s.Position = pos;
    frame.Add(s);
}

// =============================================================================
//  WEAPONS FIRE CONTROL
// =============================================================================
void UpdateWeapons()
{
    UpdateWeaponTarget();
    Track current = CurrentTarget();

    bool aiming = false, firing = false;
    if (_armed && _reference != null)
    {
        if (current != null && _wcReady)
        { try { _wc.SetAiFocus(Me, current.Id, 0); } catch { } }

        if (_aim && current != null) { AimAt(current.Position); aiming = true; }
        if (_barrage) firing = DistributeBarrage();
    }

    if (!aiming) ReleaseGyros();
    if (!firing) { SetGroupFire(false); _barrageTargetCount = 0; }
}

bool DistributeBarrage()
{
    if (!_wcReady || _reference == null || _groupWeapons.Count == 0) return false;
    Vector3D refPos = _reference.GetPosition();

    _barrageTargets.Clear();
    foreach (var kv in _tracks)
        if (IsEngageable(kv.Value, refPos)) _barrageTargets.Add(kv.Value);
    if (_barrageTargets.Count == 0) { _barrageTargetCount = 0; return false; }
    _barrageTargets.Sort(CompareContacts);
    _barrageTargetCount = _barrageTargets.Count;

    int weapons = _groupWeapons.Count;
    int targets = _barrageTargets.Count;
    int baseN = weapons / targets;
    int rem   = weapons % targets;

    int wi = 0;
    for (int ti = 0; ti < targets && wi < weapons; ti++)
    {
        int n = baseN + (ti < rem ? 1 : 0);
        for (int k = 0; k < n && wi < weapons; k++)
        {
            var weapon = _groupWeapons[wi++];
            if (weapon == null) continue;
            try { _wc.SetWeaponTarget(weapon, _barrageTargets[ti].Id, 0); } catch { }
        }
    }

    SetGroupFire(true);
    return true;
}

void ClearWeaponTargets()
{
    if (!_wcReady) return;
    foreach (var w in _groupWeapons)
    {
        if (w == null) continue;
        try { _wc.SetWeaponTarget(w, 0, 0); } catch { }
    }
}

Track CurrentTarget()
{
    Track t;
    if (_wpnTargetId != 0 && _tracks.TryGetValue(_wpnTargetId, out t)) return t;
    return null;
}

void UpdateWeaponTarget()
{
    if (_reference == null) { _wpnTargetId = 0; return; }
    Vector3D refPos = _reference.GetPosition();

    Track current = CurrentTarget();
    bool currentValid = IsEngageable(current, refPos);

    if (currentValid && (_time - _wpnLatchTime) < _targetLatch)
        return;

    Track best = SelectTarget(refPos);
    if (best != null)
    {
        if (best.Id != _wpnTargetId) { _wpnTargetId = best.Id; _wpnLatchTime = _time; }
    }
    else if (!currentValid)
    {
        _wpnTargetId = 0;
    }
}

bool IsEngageable(Track t, Vector3D refPos)
{
    if (t == null || IsVoxel(t)) return false;
    bool hostile = t.Threat || t.Relation == MyRelationsBetweenPlayerAndBlock.Enemies;
    if (!hostile) return false;
    if (_time - t.LastSeen > _holdSeconds) return false;
    return Vector3D.Distance(t.Position, refPos) <= _fireRange;
}

Track SelectTarget(Vector3D refPos)
{
    Track best = null;
    int bestPri = int.MaxValue;
    double bestDist = double.MaxValue;
    foreach (var kv in _tracks)
    {
        var t = kv.Value;
        if (!IsEngageable(t, refPos)) continue;
        int pri = ContactPriority(t);
        double d = Vector3D.DistanceSquared(t.Position, refPos);
        if (pri < bestPri || (pri == bestPri && d < bestDist))
        { best = t; bestPri = pri; bestDist = d; }
    }
    return best;
}

void AimAt(Vector3D targetPos)
{
    if (_reference == null || _gyros.Count == 0) return;
    MatrixD wm = _reference.WorldMatrix;
    Vector3D dir = targetPos - wm.Translation;
    if (dir.LengthSquared() < 1) return;
    dir = Vector3D.Normalize(dir);

    double yaw, pitch;
    GetRotationAngles(dir, wm, out yaw, out pitch);
    ApplyGyroOverride(pitch * _aimGain, yaw * _aimGain, 0, wm);
    _gyrosOverridden = true;
}

void GetRotationAngles(Vector3D dir, MatrixD wm, out double yaw, out double pitch)
{
    Vector3D local = Vector3D.TransformNormal(dir, MatrixD.Transpose(wm));
    Vector3D flat = new Vector3D(local.X, 0, local.Z);
    double flatLen = flat.Length();
    if (flatLen < 1e-9)
    {
        yaw = 0;
        pitch = local.Y > 0 ? Math.PI / 2 : -Math.PI / 2;
        return;
    }
    yaw   = Math.Acos(MathHelperD.Clamp(-flat.Z / flatLen, -1, 1)) * Math.Sign(local.X);
    pitch = Math.Acos(MathHelperD.Clamp(flatLen / local.Length(), -1, 1)) * Math.Sign(local.Y);
}

void ApplyGyroOverride(double pitch, double yaw, double roll, MatrixD wm)
{
    var rot = new Vector3D(-pitch, yaw, roll);
    var world = Vector3D.TransformNormal(rot, wm);
    foreach (var g in _gyros)
    {
        if (g == null || !g.IsFunctional) continue;
        var local = Vector3D.TransformNormal(world, MatrixD.Transpose(g.WorldMatrix));
        g.Pitch = (float)local.X;
        g.Yaw   = (float)local.Y;
        g.Roll  = (float)local.Z;
        g.GyroOverride = true;
    }
}

void ReleaseGyros()
{
    if (!_gyrosOverridden) return;
    _gyrosOverridden = false;
    foreach (var g in _gyros)
        if (g != null) g.GyroOverride = false;
}

void SetGroupFire(bool on)
{
    if (on == _groupFiring) return;
    _groupFiring = on;
    if (!_wcReady) return;
    foreach (var w in _groupWeapons)
    {
        if (w == null) continue;
        try { _wc.ToggleWeaponFire(w, on, true); } catch { }
    }
}

void DrawWeapons(IMyTextSurface surface, Vector3D origin)
{
    Vector2 size = surface.TextureSize, view = surface.SurfaceSize;
    Vector2 pad = (size - view) * 0.5f;
    float left = pad.X + 10;
    float w = view.X;

    var frame = surface.DrawFrame();
    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", pad + view * 0.5f, view, Color.Black));

    Color cyan  = new Color(0, 150, 200);
    Color faint = new Color(0, 80, 110);
    Color hot = new Color(255, 60, 60);
    Color on  = new Color(60, 230, 90);
    Color off = new Color(120, 120, 120);
    Color lbl = cyan;

    AddText(frame, "WEAPONS CONTROL", new Vector2(pad.X + w * 0.5f, pad.Y + 6),
        0.7f, cyan, TextAlignment.CENTER);
    float y = pad.Y + 40;
    DrawLine(frame, new Vector2(pad.X + 6, y - 6), new Vector2(pad.X + w - 6, y - 6), faint, 1f);

    Track tgt = CurrentTarget();
    double dist = tgt != null ? Vector3D.Distance(tgt.Position, origin) : 0;
    bool inRange = tgt != null && dist <= _fireRange;

    y = WpnLine(frame, "WC API:",    _wcReady ? "READY" : "NOT FOUND", left, w, y, lbl, _wcReady ? on : off);
    y = WpnLine(frame, "State:",     _armed ? "ARMED" : "DISARMED",    left, w, y, lbl, _armed ? hot : off);
    y = WpnLine(frame, "Group:",     string.IsNullOrWhiteSpace(_weaponGroup) ? "(all WC)" : Trunc(_weaponGroup, 16), left, w, y, lbl, lbl);
    y = WpnLine(frame, "Weapons:",   _groupWeapons.Count.ToString(), left, w, y, lbl, lbl);
    y = WpnLine(frame, "FireRange:", Math.Round(_fireRange) + " m", left, w, y, lbl, lbl);
    y = WpnLine(frame, "Latch:",     _targetLatch.ToString("0.0") + " s", left, w, y, lbl, lbl);
    y += 10;

    Color tc = tgt != null ? RelationColor(tgt) : off;
    y = WpnLine(frame, "Target:",   tgt != null ? Trunc(ContactLabel(tgt), 16) : "None", left, w, y, lbl, tc);
    y = WpnLine(frame, "Seen:",     tgt != null ? (_time - tgt.LastSeen).ToString("0.0") + " s" : "-", left, w, y, lbl, tc);
    y = WpnLine(frame, "Range:",    tgt != null ? FormatDist(dist) : "-", left, w, y, lbl, tc);
    y = WpnLine(frame, "InRange:",  tgt != null ? (inRange ? "YES" : "NO") : "-", left, w, y, lbl, tgt != null ? (inRange ? on : off) : off);
    y = WpnLine(frame, "TargetId:", tgt != null ? tgt.Id.ToString() : "-", left, w, y, lbl, tc);
    y += 10;

    y = WpnLine(frame, "Aim:",      _aim ? "ON" : "OFF", left, w, y, lbl, _aim ? on : off);
    y = WpnLine(frame, "Barrage:",  _barrage ? "ON" : "OFF", left, w, y, lbl, _barrage ? on : off);
    string spread = _barrageTargetCount > 0
        ? _barrageTargetCount + " tgt / " + _groupWeapons.Count + " wpn"
        : "-";
    y = WpnLine(frame, "Spread:",   spread, left, w, y, lbl, _barrageTargetCount > 0 ? on : off);
    WpnLine(frame, "Firing:",       _groupFiring ? "FIRING" : "hold", left, w, y, lbl, _groupFiring ? hot : off);

    frame.Dispose();
}

float WpnLine(MySpriteDrawFrame frame, string label, string value,
              float left, float w, float y, Color lblC, Color valC)
{
    AddText(frame, label, new Vector2(left, y), 0.62f, lblC, TextAlignment.LEFT);
    AddText(frame, value, new Vector2(left + w * 0.42f, y), 0.62f, valC, TextAlignment.LEFT);
    return y + 32;
}

string Trunc(string s, int n)
{
    return !string.IsNullOrEmpty(s) && s.Length > n ? s.Substring(0, n) : s;
}

// =============================================================================
//  TRACK MODEL
// =============================================================================
class Track
{
    public long Id;
    public string Name;
    public Vector3D Position;
    public Vector3D Velocity;
    public MyDetectedEntityType Type;
    public MyRelationsBetweenPlayerAndBlock Relation;
    public double LastSeen;
    public string Source;
    public bool Remote;
    public bool Threat;
}

// =============================================================================
//  WEAPONCORE (WcPbApi)
// =============================================================================
public class WcPbApi
{
    Action<IMyTerminalBlock, IDictionary<MyDetectedEntityInfo, float>> _getSortedThreats;
    Action<IMyTerminalBlock, ICollection<MyDetectedEntityInfo>> _getObstructions;
    Func<long, int, MyDetectedEntityInfo> _getAiFocus;
    Func<long, bool> _hasGridAi;
    Func<IMyTerminalBlock, bool> _hasCoreWeapon;
    Func<IMyTerminalBlock, long, int, bool> _setAiFocus;
    Action<IMyTerminalBlock, bool, bool> _toggleWeaponFire;
    Action<IMyTerminalBlock, long, int> _setWeaponTarget;

    public bool Activate(IMyTerminalBlock pb)
    {
        var prop = pb.GetProperty("WcPbAPI");
        if (prop == null) return false;
        var dict = prop.As<IReadOnlyDictionary<string, Delegate>>().GetValue(pb);
        if (dict == null) return false;
        Assign(dict, "GetSortedThreats", ref _getSortedThreats);
        Assign(dict, "GetObstructions",  ref _getObstructions);
        Assign(dict, "GetAiFocus",       ref _getAiFocus);
        Assign(dict, "HasGridAi",        ref _hasGridAi);
        Assign(dict, "HasCoreWeapon",    ref _hasCoreWeapon);
        Assign(dict, "SetAiFocus",       ref _setAiFocus);
        Assign(dict, "ToggleWeaponFire", ref _toggleWeaponFire);
        Assign(dict, "SetWeaponTarget",  ref _setWeaponTarget);
        return true;
    }

    void Assign<T>(IReadOnlyDictionary<string, Delegate> d, string name, ref T field)
        where T : class
    {
        Delegate del;
        if (d.TryGetValue(name, out del)) field = del as T;
    }

    public void GetSortedThreats(IMyTerminalBlock pb, IDictionary<MyDetectedEntityInfo, float> col)
    { if (_getSortedThreats != null) _getSortedThreats(pb, col); }

    public void GetObstructions(IMyTerminalBlock pb, ICollection<MyDetectedEntityInfo> col)
    { if (_getObstructions != null) _getObstructions(pb, col); }

    public MyDetectedEntityInfo GetAiFocus(long gridEntityId, int priority)
    { return _getAiFocus != null ? _getAiFocus(gridEntityId, priority) : new MyDetectedEntityInfo(); }

    public bool HasGridAi(long gridEntityId)
    { return _hasGridAi != null && _hasGridAi(gridEntityId); }

    public bool HasCoreWeapon(IMyTerminalBlock block)
    { return _hasCoreWeapon != null && _hasCoreWeapon(block); }

    public bool SetAiFocus(IMyTerminalBlock pb, long target, int priority = 0)
    { return _setAiFocus != null && _setAiFocus(pb, target, priority); }

    public void ToggleWeaponFire(IMyTerminalBlock weapon, bool on, bool allWeapons)
    { if (_toggleWeaponFire != null) _toggleWeaponFire(weapon, on, allWeapons); }

    public void SetWeaponTarget(IMyTerminalBlock weapon, long target, int weaponId)
    { if (_setWeaponTarget != null) _setWeaponTarget(weapon, target, weaponId); }
}
