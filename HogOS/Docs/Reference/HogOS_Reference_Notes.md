# HogOS Reference Notes

Generated from:

- `C:\Users\corra\Downloads\ExcavOS.txt`
- `C:\Users\corra\Downloads\autolevelminer.txt`
- `C:\Users\corra\Downloads\MrX's ADV Miner OS.txt`
- `C:\Users\corra\Downloads\DRONE BRAIN.txt`
- `C:\Users\corra\Downloads\PAM Path Auto Miner - Reborn.txt`
- `C:\Users\corra\Downloads\HogOS.txt`
- `F:\Space Engineers Script\SE-SCRIPTS\SE-SCRIPTING-RULES.md`

## Goal

Build a personal mining operating system for the Hog vehicle family:

- `GroundHog`: atmospheric miner.
- `SpaceHog`: ion miner.
- `HydroHog`: hydrogen miner.

Requested feature direction:

- Strong styled display system.
- Autolevel / gravity align.
- Auto dock.
- Efficient fuel and power system.
- Cruise control.
- Readable Custom Data setup/config.

## Executive Recommendation

Do not directly merge all scripts into one pasted script. Use them as references and port selected ideas into HogOS modules.

Reason: ExcavOS, PAM, and current HogOS are full frameworks with overlapping responsibilities. PAM is also obfuscated/minified and difficult to maintain. A direct merge would create a script that is too large, hard to debug, and likely near the 100,000 character programmable block limit.

Best path:

1. Keep HogOS as the main framework and display layer.
2. Port the simple autolevel idea into the existing `UtilityManager`.
3. Add a new `DockManager` inspired by PAM and Drone Brain, but write it cleanly for HogOS.
4. Expand the existing power/fuel tracking into an `EnergyManager`.
5. Keep PAM as an optional external specialist script for fully automated path mining, not as code inside HogOS.

## Current HogOS

Current HogOS is already a good base. It is not just a dashboard; it already has an operating-system shape.

### What It Does

- Uses a `HogOS` root class with boot state, version, periodic initialization, and screen updates.
- Uses `MyIni` config from the programmable block Custom Data.
- Registers surfaces from LCDs/cockpits using a `[HogOS]` Custom Data section.
- Supports multiple named screens:
  - `Splash`
  - `Loading`
  - `Power`
  - `CargoOre`
  - `Weight`
  - `Utility`
  - `Drills`
  - `Blank`
- Uses a sprite drawing helper with an AGM-style cyan/teal HUD theme.
- Tracks cargo volume and ore amounts.
- Tracks lift thrust, lift usage, stopping distance, and stopping time.
- Tracks battery status, reactor fuel, and estimated power/fuel time.
- Supports gravity alignment using gyros.
- Supports basic cruise control with forward and reverse thruster override.
- Supports drill health/status.
- Supports ore dump sorter toggles.

### Main Commands

- `toggle_gaa`: toggle gravity alignment.
- `set_gaa_pitch +5`: adjust gravity alignment pitch.
- `toggle_cruise`: toggle cruise control.
- `set_cruise 20`: set cruise target speed.
- `set_cruise +5`: adjust cruise target speed.
- `dump Iron`: toggle a sorter whitelist filter for ore.

### Strong Parts

- Best starting point for the new HogOS.
- Clean modular layout compared with the other large scripts.
- Good display approach: sprite-based, surface-local coordinates, proper boot sequence.
- Already has the features most relevant to GroundHog mining: weight, cargo, drills, gravity align, stop distance.

### Gaps To Fill

- No automatic docking path or connector approach logic yet.
- Hydrogen/fuel logic is weaker than battery/reactor logic in the current version.
- No dedicated mode profiles for `GroundHog`, `SpaceHog`, and `HydroHog`.
- No safety state machine for mining, returning, docking, and unloading.
- No IGC fleet/status layer unless borrowed later from Drone Brain/PAM patterns.
- Custom Data should be expanded into a readable setup format so the ship can be configured without editing code.

## ExcavOS

ExcavOS is the closest ancestor to HogOS. Current HogOS appears to have already evolved from it.

### What It Does

- Provides a full mini-framework:
  - `ScriptHandler`
  - `ScriptConfig`
  - screen handlers
  - registered surface providers
  - block finders
  - cargo, system, utility, and weight managers
- Uses `[ExcavOS]` Custom Data sections on displays.
- Draws multiple screen types:
  - `ExcavOS`
  - `LoadingScreen`
  - `CargoOre`
  - `Cargo`
  - `Weight`
  - `Utility`
  - `LockScreen`
  - blank screen
- Tracks:
  - cargo
  - lift thrust
  - weight/capacity
  - stop distance
  - gravity align
  - cruise control
  - batteries
  - hydrogen tanks
  - reactors/uranium
  - dump sorters

### Useful Ideas For HogOS

- Keep the registered display-provider pattern.
- Keep the `BlockFinder<T>` helper idea.
- Keep modular managers.
- Reuse the concept of screens as independent handlers.
- Compare ExcavOS `UtilityManager` with HogOS `UtilityManager`: ExcavOS has hydrogen tank calculation that current HogOS should restore or improve.

### Do Not Directly Merge

HogOS already contains the better, more polished version of many ExcavOS ideas. Use ExcavOS only as a reference for missing pieces, especially hydrogen tracking and older utility behavior.

## autolevelminer

This is a small focused script for aligning a mining arm to natural gravity.

### What It Does

- Uses a remote control to read natural gravity.
- Converts gravity into the local grid frame.
- Controls one rotor for X axis correction.
- Controls a hinge/rotor group for Z axis correction.
- Accepts a run argument as a degree offset for the Z axis.
- Uses tolerance values to stop jitter.

### Useful Ideas For HogOS

- Good for `GroundHog` drill-arm leveling.
- Better treated as an optional `ArmLevelManager`, separate from whole-vehicle gravity alignment.
- Current HogOS gravity align levels the ship using gyros; autolevelminer levels a drill arm using rotors/hinges. These are different features and both are useful.

### Issues To Fix If Ported

- It allocates a new hinge list inside the hot path.
- It assumes `h[0]` exists.
- It does not null-check rotor, remote control, or hinge group.
- It uses fixed block names instead of Custom Data config.

## MrX's ADV Miner OS

MrX's script is a practical miner safety/status script focused on thrust-to-weight and unloading.

### What It Does

- Calculates thrust capacity in six directions.
- Finds the current gravity/down axis from the main cockpit.
- Compares ship mass against thrust capacity.
- Shows status on `[TWR]` LCDs/cockpit screens.
- Colors tagged lights and LCD text green/yellow/red.
- Plays tagged warning sounds.
- Turns drills off when overweight or unsafe.
- Enables `[CRUSHER]` refineries when overweight.
- Supports `UNLOAD` command to transfer ore/ingots/ice through a connected connector to cargo on the connected grid.
- Keeps persistent error messages.

### Useful Ideas For HogOS

- Add a `TwrSafetyManager` or expand `WeightAnalyser`:
  - six-axis thrust capacity
  - current down-axis indicator
  - orbit/zero-g maneuverability check
  - automatic drill shutdown on overweight
- Add an `UnloadManager`:
  - move ore/ice/ingots to station cargo through connected connector
  - prefer target cargo with a tag such as `[HOG-ORE]` or `[HOG-CARGO]`
- Add staged warnings:
  - display color
  - light color
  - optional sound blocks

### Caution

The script header declares CC BY-NC-SA 4.0. Treat it as reference unless you intend HogOS to follow compatible licensing. Reimplement the behavior cleanly rather than copying large code blocks.

## Drone Brain

Drone Brain is a defense drone state machine, not a miner script. It is still useful for docking and safety-state design.

### What It Does

- Uses a fixed set of named blocks:
  - antenna
  - connector
  - remote control
  - AI flight block
  - AI offensive block
  - launch timer
  - return timer
  - battery/thruster/gyro/light/weapon groups
- Listens for IGC commands:
  - `LAUNCH`
  - `RETURN`
- Broadcasts status:
  - name
  - type
  - state
  - position
  - battery percentage
- State machine:
  - `DOCKED`
  - `LAUNCHED`
  - `AIRBORNE`
  - `RETURNING`
  - `DAMAGED`
- Sets docked mode:
  - disables flight/offensive systems
  - turns thrusters/gyros off
  - sets batteries to recharge
  - lowers antenna range
- Sets launch/return mode:
  - enables thrusters/gyros
  - sets batteries auto
  - changes antenna range
  - triggers timers
- Forces return on low battery, damage, or weapon/thruster loss.
- Attempts connector connect when returning.

### Useful Ideas For HogOS

- Add a small state machine:
  - `Idle`
  - `Mining`
  - `Returning`
  - `Docking`
  - `Docked`
  - `Unloading`
  - `Warning`
- Add status broadcast for multiple Hogs later:
  - `HOG_STATUS`
  - ship name
  - class
  - state
  - cargo
  - battery/hydrogen
  - connector status
- Use the battery and damage thresholds as return conditions.

### Important Fix

Drone Brain calls `timer.Trigger()`. Your SE scripting rules say `IMyTimerBlock.Trigger()` is unreliable. Use:

```csharp
timer.ApplyAction("TriggerNow");
```

## PAM Path Auto Miner - Reborn

PAM is the most capable automation reference, but it is obfuscated/minified and should not be merged directly.

### What It Does

- Detects mode from blocks:
  - miner if drills exist
  - grinder if grinders exist
  - shuttle mode otherwise
- Uses `[PAM]` tags and Custom Data.
- Records paths and docking positions.
- Saves path/home/job data to `Storage`.
- Flies path jobs using remote control, gyros, thrusters, connectors, batteries, tanks, and sorters.
- Supports docking, undocking, unloading, charging, hydrogen filling, and waiting states.
- Supports two connector shuttle mode.
- Has a menu UI navigated by run arguments:
  - `UP`
  - `DOWN`
  - `APPLY`
  - `UPLOOP`
  - `DOWNLOOP`
- Has direct run arguments including:
  - `PATHHOME`
  - `PATH`
  - `START`
  - `STOP`
  - `CONT`
  - `JOBPOS`
  - `HOMEPOS`
  - `FULL`
  - `ALIGN`
  - `RESET`
  - `SHUTTLE`
  - `NEXT`
  - `PREV`
  - `UNDOCK`
- Tracks energy and logistics:
  - max cargo load
  - battery minimum
  - hydrogen minimum
  - uranium minimum
  - unload ice on/off
  - work speeds
  - acceleration limit
  - damage behavior
- Optional broadcast/controller mode using IGC.

### Useful Ideas For HogOS

- Auto dock should borrow PAM's concept, not its code:
  - record dock connector position/orientation
  - store it in `Storage`
  - approach in stages
  - slow down near connector
  - connect when connector is `Connectable`
  - stockpile tanks / recharge batteries when docked
- Add path recording later, after docking is stable.
- Add job safety rules:
  - return at cargo threshold
  - return at battery threshold
  - return at hydrogen threshold
  - return on damaged drills/thrusters

### Do Not Directly Merge

PAM uses obfuscated symbols and compacted logic. It is excellent as a behavior reference, but poor as maintainable HogOS source.

## Merge Feasibility

| Source | Merge? | Recommendation |
|---|---:|---|
| Current HogOS | Yes | Use as the base. |
| ExcavOS | Partial | Compare and port missing hydrogen/fuel ideas. |
| autolevelminer | Partial | Port as `ArmLevelManager` for drill-arm leveling. |
| MrX ADV Miner OS | Partial | Port TWR safety, warnings, unload behavior. |
| Drone Brain | Partial | Port state-machine and status broadcast ideas; fix timer triggering. |
| PAM | No direct merge | Rebuild selected docking/path ideas cleanly. |

## Proposed HogOS Architecture

### Core

- `HogOS`
  - boot timing
  - config generation
  - surface registration
  - command dispatch
  - update throttling
- `HogContext`
  - owns all managers
  - exposes state to screens

### Managers

- `CargoManager`
  - cargo totals
  - ore breakdown
  - fill percentage
  - fill rate
- `SystemManager`
  - active controller
  - thrusters
  - gyros
  - connectors
  - drills
- `EnergyManager`
  - batteries
  - reactors
  - hydrogen tanks
  - hydrogen engines
  - stockpile/recharge modes
  - ETA/efficiency display
- `WeightAnalyser`
  - lift usage
  - six-axis thrust-to-weight
  - stop distance
  - current down axis
  - safety thresholds
- `UtilityManager`
  - whole-grid gravity align
  - cruise control
  - dump sorters
- `ArmLevelManager`
  - rotor/hinge leveling for GroundHog drill arm
  - remote-control gravity reference
  - pitch/roll offsets
- `DockManager`
  - record dock
  - approach dock
  - connector connect/disconnect
  - docked resource modes
  - unload command
- `SafetyManager`
  - overweight drill cutoff
  - low fuel return warning
  - damaged drill/thruster warning
  - optional warning lights/sounds
- `CommsManager`
  - optional IGC status broadcast
  - later fleet screen support

### Screens

- `Splash`
- `Power`
- `Fuel`
- `CargoOre`
- `Weight`
- `Utility`
- `Dock`
- `Drills`
- `Warnings`
- `Profile`

## Feature Plan

### Phase 1 - HogOS Foundation

- Keep current HogOS.
- Add docs and setup guide.
- Make command parsing safer:
  - check `parts.Length`
  - avoid direct indexing without validation
  - use invariant culture if needed
- Split energy from utility:
  - move battery/reactor/hydrogen into `EnergyManager`.
- Restore hydrogen tank display from ExcavOS.

### Phase 2 - Display Styling

- Use the AGM-style cyan/teal HUD identity for HogOS.
- Use profile accent colors:
  - `GroundHog`: cyan/green utility style.
  - `SpaceHog`: cyan/teal space style.
  - `HydroHog`: cyan/blue hydrogen style.
- Add compact cockpit layouts.
- Add large wall-LCD layouts.
- Add warning banner strip shared by all screens.
- Pull theme/profile choices from Custom Data.

### Phase 3 - Autolevel

- Keep current gyro gravity align for vehicle leveling.
- Add `ArmLevelManager` based on autolevelminer:
  - configurable remote control
  - configurable X rotor
  - configurable hinge/rotor group
  - pitch/roll offsets
  - null checks
  - no hot-path list allocation
- Commands:
  - `arm_level`
  - `arm_level off`
  - `arm_pitch +5`
  - `arm_roll -3`

### Phase 3.5 - Cruise Control

- Keep the existing HogOS cruise-control idea.
- Make it profile-aware:
  - `GroundHog`: atmospheric forward/reverse thrusters, low-speed mining cruise.
  - `SpaceHog`: ion thrusters, dampener-friendly asteroid approach cruise.
  - `HydroHog`: hydrogen thrusters, heavier acceleration and fuel-aware cruise.
- Add readable commands:
  - `cruise on`
  - `cruise off`
  - `cruise 20`
  - `cruise +5`
  - `cruise -5`
- Keep old commands as aliases:
  - `toggle_cruise`
  - `set_cruise 20`

### Phase 4 - Efficient Fuel System

- Track:
  - battery percentage
  - battery net MW
  - hydrogen tank percentage
  - hydrogen engine state
  - reactor fuel percentage
  - estimated time remaining
- Docked behavior:
  - batteries recharge
  - hydrogen tanks stockpile
  - engines off unless emergency
- Mining behavior:
  - batteries auto
  - hydrogen tanks not stockpile
  - reactors/engines enabled only when needed or configured.
- Add efficiency states:
  - `Eco`
  - `Work`
  - `Boost`
  - `Docked`
  - `Emergency`

### Phase 5 - Auto Dock

- Start with simple connector automation:
  - detect connected/connectable connector
  - connect/disconnect commands
  - apply docked energy modes
  - unload cargo.
- Keep flight/path docking separate from HogOS for now.

### Phase 6 - Automation

- Add return-to-dock triggers:
  - cargo full
  - low battery
  - low hydrogen
  - overweight
  - damage
- Add optional IGC status.
- Later, add path job recording if desired.

## Naming Thoughts

`HogOS` is good and memorable. I would keep it as the operating system name, then use profile names for vehicle classes.

Best naming structure:

- `HogOS`: the operating system.
- `GroundHog`: atmospheric mining profile.
- `SpaceHog`: ion mining profile.
- `HydroHog`: hydrogen mining profile.

Possible rename options if you want a more industrial feel:

- `HogWorks OS`
- `HogNav`
- `HogCore`
- `HogPilot`
- `HogMine OS`
- `BoarOS`
- `TuskOS`
- `HogForge`

Decision: keep `HogOS`. It is short, personal, and flexible.

## Custom Data Direction

HogOS should prefer readable Custom Data over hardcoded block names. The script can generate defaults on first run, then the player edits values in the programmable block.

Suggested PB Custom Data:

```ini
[HogOS]
ShipName=GroundHog 1
Profile=GroundHog
Theme=AGM

[Groups]
LiftThrusters=
StopThrusters=
ForwardThrusters=
ReverseThrusters=
AlignGyros=
DockConnectors=
DumpSorters=

[Blocks]
Controller=

[Safety]
LiftWarning=0.90
LiftCutoff=0.98
CargoReturn=0.90
BatteryReturn=0.20
HydrogenReturn=0.25

[Cruise]
Enabled=false
TargetSpeed=20
UseReverseThrusters=true

[Dock]
AutoRecharge=true
AutoStockpileHydrogen=true
OreCargoTag=[HOG-ORE]
```

Display surfaces should keep the existing readable style:

```ini
[HogOS]
Surface0=Power
Surface1=Fuel
Surface2=Weight
Surface3=Dock
```

All Hog cockpit variants use 4 screens. Extra pages such as `CargoOre`, `Utility`, `Drills`, and `Splash` should be used on wall LCDs or swapped into the cockpit when needed.

## Suggested New Features

- `Dock screen`: connector status, distance/approach stage, dock profile, unload state.
- `Mode profiles`: GroundHog / SpaceHog / HydroHog behavior toggles.
- `Safety strip`: one-line warning banner shown on every screen.
- `Overweight response`: warning first, then drill cutoff.
- `Smart unload`: ore/stone/ice whitelist options.
- `Maintenance screen`: damaged blocks, non-working drills, missing groups.
- `Flight recorder lite`: save last dock and last job location.
- `IGC status beacon`: publish Hog state to a base display.
- `Setup wizard`: generate missing Custom Data examples on PB first run.

## SE Scripting Rules To Respect

- Do not include `namespace`, `partial class Program`, or `using` statements in paste-ready PB code.
- Stay under 100,000 characters.
- Avoid static fields. Static methods are acceptable.
- Cache block references and reuse lists in hot paths.
- Prefer `Runtime.UpdateFrequency` over timer loops.
- Use `IsSameConstructAs(Me)` when cross-grid/subgrid filtering matters.
- Use direct typed properties where possible.
- Do not rely on `IMyTimerBlock.Trigger()`. Use `ApplyAction("TriggerNow")`.
- `IMyEventControllerBlock` is not scriptable in PB.
- `GetBlockWithName` is exact and case-sensitive.
- `MyItemType` has no `TryParse`.

## Immediate Next Build Target

For the first real HogOS implementation pass, build this:

1. `HogOS` base from current script.
2. New `EnergyManager` with battery, reactor, hydrogen tank, and hydrogen engine status.
3. New `DockManager` with connect/disconnect, docked energy modes, and manual unload.
4. Safer command parser.
5. New `Dock` and `Fuel` screens.
6. Keep PAM-style path automation as a later phase.
