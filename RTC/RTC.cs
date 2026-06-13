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

    // Park this turret - ONLY stops idle rotation spin.
    // Do NOT set Azimuth/Elevation - Whiplash confirmed this breaks turret AI firing.
    public void Park(float azRad, float elRad)
    {
        if (Vanilla != null)
        {
            Vanilla.EnableIdleRotation = false; // stop random idle spin only
            // AI targeting and firing remain fully active
        }
        // TurretControl and WeaponCore-only: no park API available through PB
    }

    // RestoreAI is no longer used - AI stays on always
    public void RestoreAI() { }

    public float DetectedAz      = 0f;
    public float DetectedEl      = 0f;
    public bool  Snapshotted     = false;
    public bool  WasUnderControl = false; // tracks manual control state

    public long TargetEntityId()
    {
        if (!HasTarget()) return 0L;
        return GetTarget().EntityId;
    }
}

// ---- CONFIG -----------------------------------------------------------------
const string SECTION         = "RTC";
const string DEF_TAG         = "";
const string DEF_GROUP       = "";        // optional block group name for turrets
const string DEF_LCD_TAG     = "[RTC]";
const string DEF_AMMO_TAG    = "[AMMO]";
const float  DEF_REFILL_PCT  = 0.5f;    // refill when below 50%
const float  DEF_PARK_AZ     = 0f;
const float  DEF_PARK_EL     = 0f;
const float  DEF_IDLE_SEC    = 5f;
const bool   DEF_SYNC        = true;
const bool   DEF_LEAD        = true;
const float  DEF_PROJ_SPD    = 380f;
const bool   DEF_USE_WC      = true;

string _turretTag;
string _turretGroup;
string _ammoTag;
float  _refillPct;
string _lcdTag;
float  _parkAzimuth;
float  _parkElevation;
float  _idleSeconds;
bool   _syncTurrets;
bool   _leadComp;
float  _projSpeed;
bool   _useWC;

// ---- STATE ------------------------------------------------------------------
List<TurretEntry>        _turrets    = new List<TurretEntry>();
List<IMyTextSurface>     _surfaces   = new List<IMyTextSurface>();
List<IMyTerminalBlock>   _ammoBoxes  = new List<IMyTerminalBlock>();

// Commander CTC - [CMD] tagged block
IMyTurretControlBlock    _cmdBlock   = null;
List<IMyUserControllableGun> _cmdWeapons = new List<IMyUserControllableGun>();
const string             CMD_TAG     = "[CMD]";
bool                     _cmdWasFiring = false;
bool                     _cmdWasActive  = false;
bool                     _cmdEnabled    = true;  // toggle with "cmd" argument


double _idleTimer    = 0;
int    _rebuildTick  = 0;
int    _ammoTick     = 0;
int    _scrollTick   = 0;
const int REBUILD_INTERVAL = 300; // ~30s at Update10
const int AMMO_INTERVAL    = 100; // restock check every ~10s at Update10
const float RESET_IDLE_SECONDS = 30f; // reset to 0/0 after this much idle time

string _statusLine      = "INIT";
int    _bootTick        = 0;
bool   _bootDone        = false;
const int BOOT_TICKS    = 30; // ~3s at Update10
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
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    Echo("RTC v1.0 booting...");
}

public void Save() { }

public void Main(string argument, UpdateType updateSource)
{
    // Boot sequence - run for BOOT_TICKS before going live
    if (!_bootDone)
    {
        _bootTick++;
        DrawBoot(_bootTick, BOOT_TICKS);
        if (_bootTick >= BOOT_TICKS)
        {
            _bootDone = true;
            Rebuild();
            Echo("RTC v1.0 ready. Turrets: " + _turrets.Count);
        }
        return;
    }

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

    _ammoTick++;
    if (_ammoTick >= AMMO_INTERVAL)
    {
        _ammoTick = 0;
        RestockAmmo();
    }

    _scrollTick++;
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
    else if (cmd == "cmd")
    {
        _cmdEnabled = !_cmdEnabled;
        if (!_cmdEnabled)
        {
            // Force cleanup when disabling
            _cmdWasActive = true;
        }
        Echo("CMD mode: " + (_cmdEnabled ? "ON" : "OFF"));
    }
    else if (cmd == "cmdf")
    {
        _cmdFiring = true;
    }
    else if (cmd == "cmds")
    {
        _cmdFiring = false;
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

    // --- Commander mode: if CMD block is under player control, sync all turrets ---
    if (_cmdEnabled && _cmdBlock != null && _cmdBlock.IsUnderControl)
    {
        _cmdWasActive = true;
        UpdateCommanderSync(dt);
        return; // skip normal targeting - CMD handles everything
    }

    // --- Sync mode off when CMD is active ---
    else if (_cmdWasActive)
    {
        // Commander just released - runs once on transition out
        _cmdWasActive = false;
        _cmdWasFiring = false;
        _cmdFiring    = false;

        // Stop all turrets firing
        foreach (var t in _turrets)
            if (t.Vanilla != null) t.Vanilla.ApplyAction("Shoot_Off");

        // Park CTC rotors back to 0/0
        if (_cmdBlock != null)
        {
            var azR = _cmdBlock.AzimuthRotor;
            var elR = _cmdBlock.ElevationRotor;
            if (azR != null)
            {
                azR.TargetVelocityRPM = 0f;
                azR.SetValueFloat("LowerLimit", 0f);
                azR.SetValueFloat("UpperLimit", 0f);
            }
            if (elR != null)
            {
                elR.TargetVelocityRPM = 0f;
                elR.SetValueFloat("LowerLimit", 0f);
                elR.SetValueFloat("UpperLimit", 0f);
            }
        }

        // Re-enable AI on all turrets
        foreach (var t in _turrets)
            if (t.Vanilla != null)
            {
                t.Vanilla.ResetTargetingToDefault();
                t.Vanilla.EnableIdleRotation = true; // must be AFTER ResetTargetingToDefault
            }
    }
    bool effectiveSync = _syncTurrets && !(_cmdEnabled && _cmdWasActive);


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

    // Also check manually controlled turrets - if player is aiming at something
    // use that as the shared target for all turrets
    IMyLargeTurretBase controlledTurret = null;
    if (!hasTarget)
    {
        foreach (var t in _turrets)
        {
            if (!t.IsWorking) continue;
            if (t.HasTarget())
            {
                bestTarget = t.GetTarget();
                hasTarget  = true;
                // If this turret is under player control, flag it
                if (t.Vanilla != null && t.Vanilla.IsUnderControl)
                    controlledTurret = t.Vanilla;
                break;
            }
        }
    }

    // If no target yet, check if a player-controlled turret has acquired one
    if (!hasTarget)
    {
        foreach (var t in _turrets)
        {
            if (t.Vanilla == null || !t.Vanilla.IsUnderControl) continue;
            if (!t.HasTarget()) continue;
            bestTarget      = t.GetTarget();
            hasTarget       = true;
            controlledTurret = t.Vanilla;
            break;
        }
    }

    // --- Act on target state ---
    if (hasTarget)
    {
        _idleTimer = 0;
        // Restore normal update rate when engaging
        if (Runtime.UpdateFrequency != UpdateFrequency.Update10)
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        // Re-enable idle rotation on all turrets when combat starts
        foreach (var t in _turrets)
            if (t.Vanilla != null) t.Vanilla.EnableIdleRotation = true;

        Vector3D aimPoint = ComputeAimPoint(bestTarget, dt);

        // WC: set grid AI focus so all WC turrets prioritize same entity
        if (_wcReady)
            _wc.SetAiFocus(Me, bestTarget.EntityId, 0);

        if (effectiveSync)
        {
            foreach (var t in _turrets)
            {
                if (!t.IsWorking) continue;
                if (t.Vanilla != null && t.Vanilla.IsUnderControl) continue;

                if (t.Kind == TurretEntry.TurretKind.WeaponCore)
                {
                    _wc.SetWeaponTarget(t.Block, bestTarget.EntityId, 0);
                }
                else
                {
                    if (t.Vanilla != null)
                        t.Vanilla.TrackTarget(aimPoint, bestTarget.Velocity);
                }
            }
            string syncLabel = controlledTurret != null ? "MANUAL SYNC" : "ENGAGING (SYNC)";
            _statusLine = syncLabel;
        }
        else
        {
            _statusLine = "ENGAGING";
        }
    }
    else
    {
        _idleTimer += dt;

        // Detect turrets just released from manual control - force park
        foreach (var t in _turrets)
        {
            if (t.Vanilla == null) continue;
            bool underControl = t.Vanilla.IsUnderControl;
            if (t.WasUnderControl && !underControl)
            {
                // Just released - park immediately and reset idle timer
                float az = _parkAzimuth  * (float)(Math.PI / 180.0);
                float el = _parkElevation * (float)(Math.PI / 180.0);
                t.Park(az, el);
                _idleTimer = _idleSeconds; // force parked state immediately
            }
            t.WasUnderControl = underControl;
        }

        string cmdTag2 = _cmdEnabled ? " [CMD ON]" : "";
        _statusLine = "STANDBY" + cmdTag2;
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
    // Under 30s idle: park to configured ParkAzimuth/ParkElevation
    // Over 30s idle:  hard reset to 0/0
    bool resetToZero = _idleTimer >= RESET_IDLE_SECONDS;
    float azRad = resetToZero ? 0f : _parkAzimuth  * (float)(Math.PI / 180.0);
    float elRad = resetToZero ? 0f : _parkElevation * (float)(Math.PI / 180.0);

    foreach (var t in _turrets)
    {
        if (!t.IsWorking) continue;
        if (t.Kind == TurretEntry.TurretKind.WeaponCore)
            continue; // WC turrets: no park API in PB

        // If player is manually controlling this turret:
        // - restore idle rotation so they can aim freely
        // - skip park override so inputs aren't fought
        if (t.Vanilla != null && t.Vanilla.IsUnderControl)
        {
            t.Vanilla.EnableIdleRotation = true;
            continue;
        }

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

    // --- Build candidate block list ---
    // TurretGroup takes priority over TurretTag
    var candidateBlocks = new List<IMyTerminalBlock>();
    if (!string.IsNullOrEmpty(_turretGroup))
    {
        var grp = GridTerminalSystem.GetBlockGroupWithName(_turretGroup);
        if (grp != null)
            grp.GetBlocks(candidateBlocks);
        else
            Echo("RTC: group '" + _turretGroup + "' not found - falling back to full scan");
    }
    if (candidateBlocks.Count == 0)
        candidateBlocks = allBlocks;

    // --- IMyTurretControlBlock (Warfare 2 DLC) ---
    var tcBlocks = new List<IMyTurretControlBlock>();
    foreach (var b in candidateBlocks)
    {
        var tc = b as IMyTurretControlBlock;
        if (tc == null || !b.IsSameConstructAs(Me)) continue;
        if (!string.IsNullOrEmpty(_turretTag) && !b.CustomName.Contains(_turretTag)) continue;
        tcBlocks.Add(tc);
        var entry = new TurretEntry();
        entry.Block = tc; entry.TCtrl = tc;
        if (wcSet.Contains(tc.EntityId))
        { entry.Kind = TurretEntry.TurretKind.WeaponCore; _wcTurretCount++; }
        else
        { entry.Kind = TurretEntry.TurretKind.TurretControl; _tcTurretCount++; }
        _turrets.Add(entry);
    }

    // --- IMyLargeTurretBase (vanilla, framework mod, most DLC) ---
    var tcIds = new HashSet<long>();
    foreach (var tc in tcBlocks) tcIds.Add(tc.EntityId);
    foreach (var b in candidateBlocks)
    {
        var vt = b as IMyLargeTurretBase;
        if (vt == null || !b.IsSameConstructAs(Me)) continue;
        if (tcIds.Contains(b.EntityId)) continue;
        if (!string.IsNullOrEmpty(_turretTag) && !b.CustomName.Contains(_turretTag)) continue;
        var entry = new TurretEntry();
        entry.Block = vt; entry.Vanilla = vt;
        if (wcSet.Contains(vt.EntityId))
        { entry.Kind = TurretEntry.TurretKind.WeaponCore; _wcTurretCount++; }
        else
        { entry.Kind = TurretEntry.TurretKind.Vanilla; _vanillaTurretCount++; }
        _turrets.Add(entry);
    }

    // --- Pure WC blocks ---
    var addedIds = new HashSet<long>();
    foreach (var t in _turrets) addedIds.Add(t.Block.EntityId);
    foreach (var b in candidateBlocks)
    {
        if (addedIds.Contains(b.EntityId) || !wcSet.Contains(b.EntityId)) continue;
        if (!string.IsNullOrEmpty(_turretTag) && !b.CustomName.Contains(_turretTag)) continue;
        var entry = new TurretEntry();
        entry.Block = b; entry.Kind = TurretEntry.TurretKind.WeaponCore;
        _turrets.Add(entry);
        _wcTurretCount++;
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
                s.ContentType           = VRage.Game.GUI.TextPanel.ContentType.SCRIPT;
                s.ScriptBackgroundColor = new Color(0, 0, 0, 255);
                s.BackgroundColor       = new Color(0, 0, 0, 255);
                _surfaces.Add(s);
            }
        }
    }

    // --- Ammo containers ---
    _ammoBoxes.Clear();
    if (!string.IsNullOrEmpty(_ammoTag))
    {
        foreach (var b in allBlocks)
        {
            if (b.CustomName.Contains(_ammoTag) && b.HasInventory)
                _ammoBoxes.Add(b);
        }
    }

    // Snapshot current Az/El for each turret on first detect
    foreach (var t in _turrets)
    {
        if (t.Snapshotted) continue;
        if (t.Vanilla != null)
        {
            t.DetectedAz  = t.Vanilla.Azimuth;
            t.DetectedEl  = t.Vanilla.Elevation;
            t.Snapshotted = true;
        }
    }

    // --- Commander CTC block ---
    _cmdBlock = null;
    _cmdWeapons.Clear();
    foreach (var b in allBlocks)
    {
        if (!b.CustomName.Contains(CMD_TAG)) continue;
        var ctc = b as IMyTurretControlBlock;
        if (ctc != null) { _cmdBlock = ctc; break; }
    }
    // Scan weapons on CTC subgrid
    if (_cmdBlock != null)
    {
        var azRotor = _cmdBlock.AzimuthRotor;
        var elRotor = _cmdBlock.ElevationRotor;
        if (azRotor != null && azRotor.TopGrid != null)
            GridTerminalSystem.GetBlocksOfType(_cmdWeapons, w =>
                w.CubeGrid == azRotor.TopGrid ||
                (elRotor != null && elRotor.TopGrid != null && w.CubeGrid == elRotor.TopGrid));
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

// ---- COMMANDER SYNC ---------------------------------------------------------
// _cmdFiring is set by PB argument "cmdf" (fire on) and "cmds" (fire off)
// Bind these to CTC toolbar: left click = "cmdf" run, release = "cmds" run
bool _cmdFiring = false;

void UpdateCommanderSync(double dt)
{
    if (_cmdBlock == null) return;

    // Raycast from CTC camera to get exact world hit point
    var cam = _cmdBlock.Camera;
    if (cam == null) return;

    Vector3D camForward = cam.WorldMatrix.Forward;
    Vector3D camPos     = cam.GetPosition();
    Vector3D aimPoint;

    MyDetectedEntityInfo hit = cam.Raycast(2000.0, 0f, 0f);
    if (!hit.IsEmpty() && hit.HitPosition.HasValue)
        aimPoint = hit.HitPosition.Value;
    else
        aimPoint = camPos + camForward * 2000.0;

    // Check if any weapon on the CTC subgrid is firing
    bool isFiring = false;
    foreach (var w in _cmdWeapons)
        if (w.IsShooting) { isFiring = true; break; }
    // Also honour manual cmdf/cmds argument
    if (_cmdFiring) isFiring = true;

    // Use TrackTarget with the world hit point + zero velocity
    // No az/el math needed - TrackTarget handles the aiming internally
    foreach (var t in _turrets)
    {
        if (!t.IsWorking) continue;
        if (t.Vanilla != null && t.Vanilla.IsUnderControl) continue;
        if (t.Vanilla == null) continue;

        t.Vanilla.EnableIdleRotation = false;
        t.Vanilla.TrackTarget(aimPoint, Vector3.Zero);

        if (isFiring && !_cmdWasFiring)
            t.Vanilla.ApplyAction("Shoot_On");
        else if (!isFiring && _cmdWasFiring)
            t.Vanilla.ApplyAction("Shoot_Off");
    }

    _cmdWasFiring = isFiring;
    _idleTimer    = 0;
    _statusLine   = _cmdFiring ? "CMD FIRE" : "CMD SYNC [ON]";

    if (Runtime.UpdateFrequency != UpdateFrequency.Update1)
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
}


// ---- BOOT SCREEN ------------------------------------------------------------
void DrawBoot(int tick, int total)
{
    // PB echo progress
    int pct = (int)((float)tick / total * 100f);
    Echo("RTC v1.0 booting... " + pct + "%" + "\nInitialising systems...");

    // LCD boot screen - steps shown progressively
    // 0=title, 1=ver, 2=blank, 3=init, 4=blank, 5=config, 6=turrets, 7=wc, 8=lcd
    int totalSteps = 9;
    int linesShown = 1 + (int)((float)tick / total * (totalSteps - 1));

    foreach (var surface in _surfaces)
    {
        Vector2 texSize  = surface.TextureSize;
        Vector2 surfSize = surface.SurfaceSize;
        Vector2 pad      = (texSize - surfSize) * 0.5f;
        Vector2 ctr      = pad + surfSize * 0.5f;
        float   wide     = surfSize.X;
        float   half     = wide * 0.5f;
        float   top      = -surfSize.Y * 0.5f + 20f;

        Color bg     = new Color(  5,  10,  18, 255);
        Color accent = new Color(  0, 170, 255, 255);
        Color gray   = new Color(110, 110, 110, 255);
        Color green  = new Color(  0, 220, 100, 255);
        Color dark   = new Color( 12,  22,  38, 255);

        using (var frame = surface.DrawFrame())
        {
            // Background
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                pad + surfSize * 0.5f, surfSize, bg));

            float y = top;

            // Title
            frame.Add(new MySprite(SpriteType.TEXT, "REV TURRET CONTROLLER",
                new Vector2(0f, y) + ctr,
                null, accent, "DEBUG", TextAlignment.CENTER, 0.7f));
            y += 26f;
            frame.Add(new MySprite(SpriteType.TEXT, "v1.0",
                new Vector2(0f, y) + ctr,
                null, gray, "DEBUG", TextAlignment.CENTER, 0.55f));
            y += 30f;

            // Progressive boot lines
            string[] bootLines = new string[6] {
                "Loading config",
                "Scanning turrets",
                "Checking WeaponCore",
                "Preparing ammo system",
                "Setting up LCD",
                "System ready"
            };
            int bootShown = (int)((float)tick / total * bootLines.Length);
            for (int i = 0; i < bootShown && i < bootLines.Length; i++)
            {
                Color lc = (i == bootShown - 1) ? accent : gray;
                string prefix = (i == bootShown - 1) ? "> " : "  ";
                frame.Add(new MySprite(SpriteType.TEXT, prefix + bootLines[i],
                    new Vector2(-half + 12f, y) + ctr,
                    null, lc, "DEBUG", TextAlignment.LEFT, 0.55f));
                y += 18f;
            }

            // Progress bar
            float barY   = surfSize.Y * 0.5f - 30f;
            float barW   = wide - 20f;
            float filled = barW * ((float)tick / total);
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                new Vector2(0f, barY - 8f) + ctr, new Vector2(barW, 10f), dark));
            if (filled > 0f)
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                    new Vector2(-barW * 0.5f + filled * 0.5f, barY - 8f) + ctr,
                    new Vector2(filled, 10f), green));
            frame.Add(new MySprite(SpriteType.TEXT, pct + "%",
                new Vector2(0f, barY + 4f) + ctr,
                null, gray, "DEBUG", TextAlignment.CENTER, 0.5f));
        }
    }
}

// ---- STATUS LCD (SPRITE MODE) -----------------------------------------------
void DrawStatus()
{
    if (_surfaces.Count == 0) return;

    // Colors defined locally - SE PB does not support field initializers with new()
    Color C_BG     = new Color(  5,  10,  18, 255);
    Color C_DARK   = new Color( 12,  22,  38, 255);
    Color C_BORDER = new Color( 20,  60,  90, 255);
    Color C_ACCENT = new Color(  0, 170, 255, 255);
    Color C_GREEN  = new Color(  0, 220, 100, 255);
    Color C_AMBER  = new Color(255, 180,   0, 255);
    Color C_RED    = new Color(255,  50,  50, 255);
    Color C_GRAY   = new Color(110, 110, 110, 255);
    Color C_WHITE  = new Color(220, 220, 220, 255);

    bool anyEngaging = false;
    bool anyDamaged  = false;
    bool anyDepleted = false;
    foreach (var t in _turrets)
    {
        if (t.HasTarget()) anyEngaging = true;
        if (!t.IsWorking)  anyDamaged  = true;
        if (t.Vanilla != null && t.Vanilla.IsWorking)
        {
            var inv = t.Vanilla.GetInventory(0);
            if (inv != null)
            {
                var items = new List<MyInventoryItem>();
                inv.GetItems(items);
                if (items.Count == 0) anyDepleted = true;
            }
        }
    }

    string statusText;
    Color  statusColor;
    if (_turrets.Count == 0)    { statusText = "OFFLINE";  statusColor = C_RED;   }
    else if (anyEngaging)       { statusText = "ENGAGING"; statusColor = C_AMBER; }
    else if (anyDamaged)        { statusText = "DAMAGED";  statusColor = C_RED;   }
    else if (anyDepleted)       { statusText = "DEPLETED"; statusColor = C_RED;   }
    else                        { statusText = "PARKED";   statusColor = C_GREEN; }

    foreach (var surface in _surfaces)
    {
        Vector2 texSize  = surface.TextureSize;
        Vector2 surfSize = surface.SurfaceSize;
        Vector2 pad      = (texSize - surfSize) * 0.5f;
        Vector2 ctr      = pad + surfSize * 0.5f;
        float   wide     = surfSize.X;
        float   half     = wide * 0.5f;
        float   top      = -surfSize.Y * 0.5f + 6f;

        // How many rows fit for turret list
        // Layout: header=32, statusbar=34, divider+label=26, settings=5*17+20=105, footer=20
        // Remaining for turret rows = 512 - 6(margin) - 32 - 34 - 26 - 8 - 4 - 26 - 8 - 105 - 20 = ~243px
        // Each row = 16px => ~15 turrets max
        int maxRows = (int)((surfSize.Y - 6f - 32f - 34f - 26f - 8f - 4f - 26f - 8f - 105f - 20f) / 16f);
        if (maxRows < 1) maxRows = 1;

        // Scroll offset: advances one row every 60 ticks (~6s at Update10)
        int scrollOffset = 0;
        int total = _turrets.Count;
        if (total > maxRows)
            scrollOffset = (_scrollTick / 60) % (total - maxRows + 1);

        using (var frame = surface.DrawFrame())
        {
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                pad + surfSize * 0.5f, surfSize, C_BG));

            float y = top;

            // === HEADER ===
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                new Vector2(0f, y + 12f) + ctr, new Vector2(wide, 26f), C_DARK));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                new Vector2(-half + 2f, y + 12f) + ctr, new Vector2(4f, 26f), C_ACCENT));
            frame.Add(new MySprite(SpriteType.TEXT, "REV TURRET CONTROLLER  v1.0",
                new Vector2(0f, y + 2f) + ctr,
                null, C_ACCENT, "DEBUG", TextAlignment.CENTER, 0.65f));
            y += 32f;

            // === STATUS BAR ===
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                new Vector2(0f, y + 12f) + ctr, new Vector2(wide, 26f), statusColor));
            frame.Add(new MySprite(SpriteType.TEXT, statusText,
                new Vector2(0f, y + 2f) + ctr,
                null, C_BG, "DEBUG", TextAlignment.CENTER, 0.75f));
            y += 34f;

            Divider(frame, ctr, y, wide); y += 8f;

            // === TURRET LIST ===
            frame.Add(new MySprite(SpriteType.TEXT, "TURRETS",
                new Vector2(-half + 8f, y) + ctr,
                null, C_GRAY, "DEBUG", TextAlignment.LEFT, 0.55f));
            y += 18f;

            for (int i = 0; i < maxRows && (i + scrollOffset) < total; i++)
            {
                var t = _turrets[i + scrollOffset];
                string tName = t.Block.CustomName; // full name, no truncation
                Color tCol;
                string tState;
                if (!t.IsWorking)       { tCol = C_RED;   tState = "DAMAGED"; }
                else if (t.HasTarget()) { tCol = C_AMBER; tState = "ENGAGE"; }
                else                    { tCol = C_GREEN; tState = "PARKED"; }
                frame.Add(new MySprite(SpriteType.TEXT, tName,
                    new Vector2(-half + 12f, y) + ctr,
                    null, C_WHITE, "DEBUG", TextAlignment.LEFT, 0.5f));
                frame.Add(new MySprite(SpriteType.TEXT, "* " + tState,
                    new Vector2(half - 6f, y) + ctr,
                    null, tCol, "DEBUG", TextAlignment.RIGHT, 0.5f));
                y += 16f;
            }
            y += 4f;

            Divider(frame, ctr, y, wide); y += 8f;

            // === AMMO COUNT BARS ===
            frame.Add(new MySprite(SpriteType.TEXT, "AMMO",
                new Vector2(-half + 8f, y) + ctr,
                null, C_GRAY, "DEBUG", TextAlignment.LEFT, 0.55f));
            y += 14f;

            float barW2 = wide - 12f;
            int ammoTurrets = 0;
            int ammoFull    = 0;
            foreach (var t in _turrets)
            {
                if (t.Vanilla == null || !t.Vanilla.IsWorking) continue;
                var inv = t.Vanilla.GetInventory(0);
                if (inv == null) continue;
                ammoTurrets++;
                float maxV = (float)inv.MaxVolume;
                float curV = (float)inv.CurrentVolume;
                float pct2 = maxV > 0f ? Math.Min(curV / maxV, 1f) : 0f;
                Color ac   = pct2 >= 0.5f ? C_GREEN : (pct2 > 0.1f ? C_AMBER : C_RED);
                float fill = barW2 * pct2;
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                    new Vector2(0f, y + 3f) + ctr, new Vector2(barW2, 5f), C_DARK));
                if (fill > 0f)
                    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
                        new Vector2(-barW2 * 0.5f + fill * 0.5f, y + 3f) + ctr,
                        new Vector2(fill, 5f), ac));
                y += 8f;
                if (pct2 >= 0.9f) ammoFull++;
            }
            if (ammoTurrets == 0)
            {
                frame.Add(new MySprite(SpriteType.TEXT, "no data",
                    new Vector2(-half + 8f, y) + ctr,
                    null, C_GRAY, "DEBUG", TextAlignment.LEFT, 0.5f));
                y += 10f;
            }
            y += 4f;

            frame.Add(new MySprite(SpriteType.TEXT, "TURRET SETTINGS",
                new Vector2(-half + 8f, y) + ctr,
                null, C_GRAY, "DEBUG", TextAlignment.LEFT, 0.55f));
            y += 18f;

            Row(frame, ctr, y, wide, "CMD Mode",
                _cmdEnabled ? "ON" : "OFF",
                _cmdEnabled ? C_AMBER : C_GRAY);                           y += 17f;
            Row(frame, ctr, y, wide, "Sync",
                _syncTurrets ? "Enabled" : "Disabled",
                _syncTurrets ? C_GREEN : C_GRAY);                          y += 17f;
            Row(frame, ctr, y, wide, "Lead Compensation",
                _leadComp ? "Enabled" : "Disabled",
                _leadComp ? C_GREEN : C_GRAY);                             y += 17f;
            Row(frame, ctr, y, wide, "Projectile Speed",
                _projSpeed + " m/s", C_WHITE);                             y += 17f;
            Row(frame, ctr, y, wide, "Park Azimuth",
                _parkAzimuth + " deg", C_WHITE);                           y += 17f;
            Row(frame, ctr, y, wide, "Park Elevation",
                _parkElevation + " deg", C_WHITE);                         y += 20f;

            // === FOOTER ===
            Divider(frame, ctr, y, wide); y += 4f;
            frame.Add(new MySprite(SpriteType.TEXT, "RevGamer",
                new Vector2(0f, y) + ctr,
                null, C_BORDER, "DEBUG", TextAlignment.CENTER, 0.5f));
        }
    }
}

void Row(MySpriteDrawFrame frame, Vector2 ctr, float y, float wide,
         string label, string value, Color valColor)
{
    Color gray = new Color(110, 110, 110, 255);
    frame.Add(new MySprite(SpriteType.TEXT, label,
        new Vector2(-wide * 0.5f + 8f, y) + ctr,
        null, gray, "DEBUG", TextAlignment.LEFT, 0.6f));
    frame.Add(new MySprite(SpriteType.TEXT, value,
        new Vector2(wide * 0.5f - 4f, y) + ctr,
        null, valColor, "DEBUG", TextAlignment.RIGHT, 0.6f));
}

void Divider(MySpriteDrawFrame frame, Vector2 ctr, float y, float wide)
{
    Color border = new Color(20, 60, 90, 255);
    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple",
        new Vector2(0f, y) + ctr,
        new Vector2(wide, 1f), border));
}

// ---- CONFIG -----------------------------------------------------------------
void LoadConfig()
{
    var ini = new MyIni();
    if (!ini.TryParse(Me.CustomData) || !ini.ContainsSection(SECTION))
        WriteDefaults(ini);

    _turretTag    = ini.Get(SECTION, "TurretTag").ToString(DEF_TAG);
    _turretGroup  = ini.Get(SECTION, "TurretGroup").ToString(DEF_GROUP);
    _ammoTag      = ini.Get(SECTION, "AmmoTag").ToString(DEF_AMMO_TAG);
    _refillPct    = (float)ini.Get(SECTION, "RefillThreshold").ToDouble(DEF_REFILL_PCT);
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
    ini.Set(SECTION, "TurretGroup",      DEF_GROUP);
    ini.Set(SECTION, "AmmoTag",          DEF_AMMO_TAG);
    ini.Set(SECTION, "RefillThreshold",  DEF_REFILL_PCT);
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


// ---- AMMO RESTOCK -----------------------------------------------------------
// Scans each turret inventory. If below refill threshold, pulls matching ammo
// from tagged containers. Handles any ammo type - no hardcoded subtypes.
void RestockAmmo()
{
    if (_ammoBoxes.Count == 0 || _turrets.Count == 0) return;

    foreach (var t in _turrets)
    {
        if (!t.IsWorking) continue;
        if (t.Vanilla == null) continue; // WC-only blocks: no standard inventory access

        var turretInv = t.Vanilla.GetInventory(0);
        if (turretInv == null) continue;

        // Check fill ratio
        float maxVol  = (float)turretInv.MaxVolume;
        float curVol  = (float)turretInv.CurrentVolume;
        if (maxVol <= 0f) continue;
        if (curVol / maxVol >= _refillPct) continue; // already above threshold

        // Find out what ammo type this turret uses by checking what it currently has
        // or what it accepts - use existing items as a hint first
        var turretItems = new List<MyInventoryItem>();
        turretInv.GetItems(turretItems);

        // If turret is empty we can't know what it wants - skip for now
        // (it will self-load when it fires; we restock once it has ammo to match)
        if (turretItems.Count == 0) continue;

        // Pull each ammo type found in the turret from ammo boxes
        foreach (var item in turretItems)
        {
            MyItemType ammoType = item.Type;

            // Search all ammo boxes for this type
            foreach (var box in _ammoBoxes)
            {
                if (!box.HasInventory) continue;

                for (int i = 0; i < box.InventoryCount; i++)
                {
                    var boxInv = box.GetInventory(i);
                    if (boxInv == null) continue;

                    var boxItems = new List<MyInventoryItem>();
                    boxInv.GetItems(boxItems);

                    foreach (var boxItem in boxItems)
                    {
                        if (boxItem.Type != ammoType) continue;
                        if ((float)turretInv.CurrentVolume >= (float)turretInv.MaxVolume) break;

                        // Transfer as much as will fit
                        boxInv.TransferItemTo(turretInv, boxItem);
                        break;
                    }

                    if ((float)turretInv.CurrentVolume >= (float)turretInv.MaxVolume) break;
                }
                if ((float)turretInv.CurrentVolume >= (float)turretInv.MaxVolume) break;
            }
        }
    }
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
