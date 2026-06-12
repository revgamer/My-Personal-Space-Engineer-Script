# Space Engineers Script Reference Pack

> 5 community scripts — how they work, what they do, advantages, and improvement ideas.
> All are **unminified source** — no minified versions detected.

---

## Table of Contents

1. [TCES — Turret Controller Enhancement Script](#1-tces--turret-controller-enhancement-script)
2. [SHART — Script Handling Activation of Remote Timers](#2-shart--script-handling-activation-of-remote-timers)
3. [EZ IIM to GSIM — Migration Tool](#3-ez-iim-to-gsim--migration-tool)
4. [SE-DOS — Operating System v3.0](#4-se-dos--operating-system-v30)
5. [LAM — Laser Antenna Manager](#5-lam--laser-antenna-manager)

---

## 1. TCES — Turret Controller Enhancement Script

**Author:** Whiplash141
**Version:** 1.15.0 (2026-05-28)

### What It Does

Extends the vanilla **Custom Turret Controller (CTC)** block with features the base game does not support:

- Auto-configures the CTC block on setup
- Rotor **rest angles** — turret returns to a parked position when not in use
- Supports **more than 2 rotors** (vanilla CTC is limited to azimuth + elevation only)
- Rotor **stabilization** to counteract ship movement
- **Synchronized turrets** — slave turrets mirror a master turret using the `[SYNC]` group tag
- **Weapon deadzones** — define azimuth/elevation angle ranges where weapons are suppressed (prevent friendly fire on your own hull)
- Auto-computed **deviation angle** for rotor targeting accuracy
- **Title screen** on the PB display

### Architecture

| Class | Purpose |
|---|---|
| `TCESTurret` | One CTC group — owns rotor state machines, rest logic, deadzone config |
| `TCESSynced` | One SYNC group — mirrors another turret's rotors |
| `InvertibleRotor` | Wraps `IMyMotorStator` with an invert multiplier |
| `RotorConfig` | Per-rotor INI config (rest angle, rest speed, stabilization) |
| `WeaponDeadzone` | Per-weapon azimuth/elevation suppression range |
| `StateMachine` | Generic state machine driving rotor control and rest sequencing |
| `GridConnectionSolver` | Resolves which rotors belong to which CTC across subgrid connections |

### State Machine States

```
ManualControl  -> player is driving the turret
AiControl      -> turret is tracking a target (AI mode)
WaitForRest    -> no input for N seconds, waiting before moving to rest
MoveToRest     -> actively rotating back to rest angle
Idle           -> at rest, doing nothing
```

Rest can be sequenced: `AzimuthFirst` or `ElevationFirst` (set per-group in Custom Data).

### Setup

All config lives in **Custom Data of the PB** — do not edit script variables.

```ini
[TCES - General]
Group name tag = TCES
Synced group name tag = SYNC
Azimuth rotor name tag = Azimuth
Elevation rotor name tag = Elevation
Should auto return to rest angle = true
Auto return to rest angle delay (s) = 2
Auto compute deviation angle = true
Draw title screen = true

[TCES - Rotor]
Rest angle order = AzimuthFirst
Rest angle (deg) = 0
Rest speed multiplier = 1.0
Enable stabilization = false

[TCES - Weapon Deadzone 1]
Azimuth angle range (deg) = {min:-30 max:30}
Elevation angle range (deg) = {min:-15 max:15}
```

Group your CTC + rotors + weapons in a block group named `TCES` (or your custom tag).
For synced turrets: group rotors + weapons under a group named `SYNC` (or your custom tag).

### Advantages for Your Build

- Prevents turrets from clipping through your own ship when idle (rest angles)
- Stops weapons firing into hull sections (deadzones) — great for fixed forward weapons on a UNSC ship
- Synced turrets let you build paired left/right AA mounts that track as one
- Stabilization helps on fast-moving ships where rotors drift with momentum

### Improvement Ideas

| Area | Idea |
|---|---|
| Multi-PB sync | Add IGC broadcast so turret groups on docked ships can sync without being on the same grid |
| Target handoff | When one CTC loses a target, broadcast its last known target GPS to neighboring CTCs |
| AGM integration | Hook into AGM ammo stock dashboard — suppress turret if ammo below threshold |
| OPTRE weapons | Extend deadzone config to handle ODST launchers or vehicle turrets with unusual geometry |

---

## 2. SHART — Script Handling Activation of Remote Timers

**Author:** Whiplash141

### What It Does

Lets you **remotely trigger, start, or stop Timer Blocks on other grids** using antennas and IGC (Inter-Grid Communication). Wireless remote control for automation chains.

- Each SHART instance has a **Unique ID (UID)** — auto-generated, stored in Custom Data
- Sends/receives `trigger`, `start`, or `stop` commands over IGC broadcast
- Supports **subaddresses** — one receiver can control multiple timers by name
- Works with both **regular antennas** and **laser antennas** (laser must be linked)
- `rename` argument generates a fresh UID to avoid channel collision

### Architecture

| Component | Purpose |
|---|---|
| `IGC.RegisterBroadcastListener(_id)` | Listens on the UID channel |
| `IGC.SendBroadcastMessage(_id, payload)` | Sends to a remote SHART by UID |
| `_timer` (IMyTimerBlock) | Default timer on the receiver |
| `_subaddressTimers` (Dictionary) | Named timers for subaddress routing |
| `TimerAction` enum | None / Start / Trigger / Stop |
| `RemoteTimerScreenManager` | Draws send/receive state on PB display |

### Message Flow

```
Sender PB
  argument: "trigger REMOTE_UID"
       |
       v
  IGC.SendBroadcastMessage(REMOTE_UID, payload)
       |
       v  (antenna range)
                              Receiver PB
                              IGC callback fires
                              ProcessMessages()
                              timer.ApplyAction("TriggerNow")
```

### Custom Data Format

```ini
[SHART - General]
Unique Identifier (UID) = MyShipSHART
Timer to trigger on receive = Timer Block

[SHART - Subaddresses]
 |primary,Timer Block 1
 |secondary,Timer Block 2
 |tertiary,Timer Block 3
```

### Arguments

| Argument | Effect |
|---|---|
| `trigger UID` | Trigger default timer on receiver UID |
| `trigger UID subaddress` | Trigger named subaddress timer on receiver UID |
| `start UID` | Start countdown on receiver UID |
| `stop UID` | Stop countdown on receiver UID |
| `rename` | Generate new UID for this SHART |

### Advantages for Your Build

- Chain base automation to a docked ship without hardwiring toolbar actions
- Trigger a ship's departure sequence (close airlocks, enable thrusters) remotely from the base
- Works across any antenna range — laser or broadcast
- Subaddresses let one base PB control many different ship procedures

### Improvement Ideas

| Area | Idea |
|---|---|
| Two-way confirmation | Add ACK reply so sender knows the command was received and executed |
| Encrypted UID | Use a hashed UID to prevent other players triggering your timers on PvP servers |
| IGC unicast option | Use `IGC.SendUnicastMessage` to a known address instead of broadcast (more secure) |
| SHART + TCES | Trigger turret rest on dock — when ship connects, SHART fires, TCES moves all turrets to rest angle |
| AGM hook | When AGM detects low ammo, SHART broadcasts to a supply ship to trigger its departure timer |

---

## 3. EZ IIM to GSIM — Migration Tool

**Author:** Community
**Version:** TC2.0.1

### What It Does

A **one-time migration utility** that converts an existing **Isy's Inventory Manager (IIM)** installation to **GSIM (Grid Stock Inventory Manager)** without losing your autocrafting config or container setup.

This is NOT a permanent script. Run it once, complete the migration, then remove it.

### What Gets Converted

| IIM Data | GSIM Equivalent | How |
|---|---|---|
| Autocrafting LCD custom data (want/have numbers) | GSIM AutoCrafting LCD format | `ConvertLCDs` command |
| `[Special]` container tags | GSIM `[Stock]` container tags | `ConvertContainers` command |
| Special containers on docked ships | Same, across connectors | `ConvertDockedContainers` command |

### Migration Flow

```
Step 1: Install this script on any available PB (NOT the IIM PB yet)
Step 2: Run argument "BackUpIIM"
        -> Script copies all IIM autocrafting data to a safe buffer

Step 3: Install GSIM over the IIM PB
        -> Follow GSIM setup, name your AutoCrafting LCD "[AutoCrafting]"

Step 4: Run argument "ConvertLCDs"
        -> AutoCrafting LCD data rewritten to GSIM format with old numbers preserved

Step 5: Run argument "ConvertContainers"
        -> All [Special] containers renamed to [Stock]

Step 6 (optional): Connect docked ships, run "ConvertDockedContainers"
        -> Converts Special containers on connected grids
```

### Architecture

| Component | Purpose |
|---|---|
| Timed/step processor | Splits work across multiple ticks to prevent 50k instruction crash on large grids |
| Backup buffer | Holds IIM data in PB storage between steps |
| LCD scanner | Finds `[AutoCrafting]` and `[AutoCraftingExtension:x]` LCDs |
| Container scanner | Finds all containers matching `[Special]` keyword across local + docked grids |

### Configuration (top of script)

```csharp
// IIM special container keyword (default)
string iimSpecialKeyword = "[Special]";

// GSIM stock container keyword (match your GSIM config)
string gsimStockKeyword = "[Stock]";
```

### Advantages for Your Build

- Tested on 200+ container grids — safe for large bases
- Preserves all want/have numbers — no re-entering autocrafting values
- Handles docked ships separately
- Step-by-step on-screen prompts — no guesswork

### Improvement Ideas

| Area | Idea |
|---|---|
| Dry-run mode | Add a `Preview` argument that shows what would be changed without doing it |
| Rollback | Add a `Restore` command to revert to IIM data if something goes wrong |
| Log output | Write a full change log to a named LCD for audit |
| AGM migration | Build a similar tool for migrating from IIM/GSIM to AGM |

---

## 4. SE-DOS — Operating System v3.0

**Author:** DrHousexx

### What It Does

A **multi-LCD display system**. One PB detects all LCDs on the grid automatically and renders different information panels based on each LCD's Custom Data. No name tags on blocks — all config is in Custom Data.

### Screen Modes

Set in each LCD's Custom Data as `MODE|BLOCK_NAME`.

#### Inventory Modes

| Custom Data | Shows |
|---|---|
| `ALL\|ALL` | All items on the entire grid, paginated (16 per page) |
| `ALL\|ContainerName` | Items from one specific container |
| `ORE\|ALL` | Raw ores only, full grid |
| `INGOT\|ALL` | Ingots only, full grid |
| `COMPONENT\|ALL` | Components only, full grid |
| `TOOL\|ALL` | Tools only, full grid |
| `AMMO\|ALL` | Ammo only, full grid |
| `ALL\|ALL\|8` | Full inventory, 8 items per page |

#### Machine Modes

| Custom Data | Shows |
|---|---|
| `REFINERY\|RefineryName` | Status, current item, queue (4), input/output bars, output ingots |
| `ASSEMBLER\|AssemblerName` | Status, current item, queue (4), input/output inventory + bars |
| `DRILL\|ALL` | All drills: ON/OFF/ERR, ore content, capacity |
| `DRILL\|DrillName` | Specific drill panel |
| `DASHBOARD\|ANY` | Grid overview: block counters, battery %, inventory %, refinery %, assembler %, animated spinner |

#### Blueprint Mode

| Custom Data | Shows |
|---|---|
| `PROJECT\|ProjectorName` | Projector status, block count, remaining, buildable now, components list (16 in 2 cols), assembler queue |

To queue components into an assembler, run PB argument: `ProjectorName|AssemblerName`

### Commands (PB Argument)

| Command | Effect |
|---|---|
| `reload` | Re-scan all LCDs, reload config |
| `bright+` / `bright-` | Brightness +/-10% |
| `bright N` | Set brightness 0-100 |
| `contrast+` / `contrast-` | Contrast +/-10% |
| `contrast N` | Set contrast 0-100 |
| `display B C` | Set brightness and contrast in one command |
| `ProjectorName\|AssemblerName` | Queue blueprint components into assembler |

### Global Settings (top of script)

```csharp
const int DEFAULT_ITEMS_PER_PAGE = 16;
const bool SHOW_CREDITS = true;
const UpdateFrequency UPDATE_RATE = UpdateFrequency.Update100;  // ~1.67s refresh
```

### Advantages for Your Build

- Zero name-tag pollution — all config in Custom Data
- One PB drives unlimited LCDs across the whole grid
- PROJECT mode is directly useful for construction — shows exactly what components you need and queues them
- DASHBOARD mode gives a clean "ship health" overview for cockpit panels
- Compatible with AGM/GSIM because it only reads inventory — it does not sort

### Improvement Ideas

| Area | Idea |
|---|---|
| Sprite mode rendering | Currently text-mode only — upgrade to full sprite LCD for icons and bars |
| Battery per-block breakdown | DASHBOARD shows total %; add a per-battery LCD mode |
| Hydrogen/oxygen panel | Add `GAS\|TankName` mode showing gas level and fill rate |
| Connector status | Add `DOCK\|ConnectorName` mode showing docked ship name and status |
| AGM integration | Pull AGM stock data and add a `STOCK\|ItemName` mode showing want vs have |
| Alert system | Add threshold warnings (battery < 20%) with color changes on DASHBOARD |
| Night mode shortcut | Wire `display 30 40` to a button panel for cockpit night mode |

---

## 5. LAM — Laser Antenna Manager

### What It Does

Manages one **Laser Antenna** and automatically cycles through a list of GPS target coordinates until it establishes a connection. If it cannot connect to any target, it retries after 30 update cycles.

- GPS targets stored in PB Custom Data, separated by `/`
- Displays connection status on the PB's built-in LCD (surface 0)
- Detects status changes and updates display only when state changes (efficient)
- Handles: Connected, Connecting, RotatingToTarget, SearchingTarget, OutOfRange, Idle

### Architecture

| Component | Purpose |
|---|---|
| `LAM` static class | Shared state: antenna ref, GPS array, current index, run count, last display state |
| `ConfigureLAM()` | Reads Custom Data, gets antenna ref, resets state |
| `Connect()` | Advances to next GPS index, calls `SetTargetCoords` + `Connect` |
| `DisplayState()` | Writes status to PB LCD surface 0 and Echo |
| `Main()` loop | Runs at Update100; handles retry timers and status change detection |

### Connection Logic

```
ConnectedGPS = -1  (no attempt yet)
      |
      v
Connect() called -> index advances to 0, SetTargetCoords(GPS[0]), Connect()
      |
      v
If SearchingTarget or OutOfRange for 5 cycles -> try next GPS
If all GPS exhausted -> ConnectedGPS = -2 (can't connect)
If ConnectedGPS = -2 for 30 cycles -> restart from GPS[0]
If Idle at any point -> restart attempt immediately
```

### Custom Data Format

```
GPS:Base Alpha:12345.6:7890.1:2345.6:#FF75C9F1:
/GPS:Base Beta:-5000.0:3200.0:1100.0:#FF75C9F1:
/GPS:Relay Station:22000.0:-1500.0:800.0:#FF75C9F1:
```

One GPS per line, separated by `/`. Copy coords using "Copy my coords" from a Laser Antenna.

### Arguments

| Argument | Effect |
|---|---|
| `config` | Re-read Custom Data and restart |
| `restart` | Reset connection state without re-reading config |

### Advantages for Your Build

- Fully automated — no player interaction needed to maintain long-range comms
- Supports relay chains — list intermediate relays before the final destination
- Graceful fallback — if a relay goes offline it tries the next one
- Status on PB display — visible at a glance whether comms are up

### Improvement Ideas

| Area | Idea |
|---|---|
| Multi-antenna support | Find all blocks tagged `[LAM]` and manage each independently |
| Force unique connections | Prevent two LAM antennas connecting to the same target GPS |
| External LCD output | Write status to a named LCD instead of only PB surface 0 |
| Connect by argument | `connect 2` to force connection to GPS index 2 directly |
| Priority ordering | Mark some GPS as priority — try these first before cycling |
| IGC integration | When connected, send an IGC broadcast to notify other scripts that comms are live |
| SHART integration | Trigger a SHART timer when connection is established or lost |
| AGM integration | Feed connection state into AGM dashboard as a comms status indicator |

---

## Cross-Script Integration Map

```
TCES ─────────────────────────────────────────────────────┐
  (turret rest on dock)                                    |
                                                           v
SHART ─── wireless trigger ──────────────────────> Base Timer Chain
  ^                                                        |
  |                                                        v
LAM ──── comms up/down event ──────────────────> Notification / alert LCD
                                                           |
                                                           v
SE-DOS ──── PROJECT mode ──────────────────────> Blueprint queue -> Assembler
                                                           |
                                                           v
EZ IIM->GSIM ─── one-time migration ──────────> GSIM running on base
```

All five can coexist on the same grid with zero conflicts — they use different block types and Custom Data namespaces.
