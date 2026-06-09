# Rev Radar System — How It Works (Reference)

Author: RevGamer  
Script: `Rev_Radar.cs` — Space Engineers Programmable Block

---

## Overview

Rev Radar is a PPI-style (Plan Position Indicator) radar scope for Space Engineers.
It fuses contacts from multiple detection sources, draws a circular scope on an LCD using
the sprite API, and optionally controls WeaponCore weapons as a fire-control panel.

---

## Vanilla Compatibility

| Feature | Vanilla? | Notes |
|---|---|---|
| Sensor block contacts | YES | `IMySensorBlock.DetectedEntities()` — fully vanilla |
| Camera raycasts | YES | `IMyCameraBlock.Raycast()` — fully vanilla |
| Turret target detection | YES | `IMyLargeTurretBase.GetTargetedEntity()` — fully vanilla |
| Custom Turret Controller targets | YES | `IMyTurretControlBlock.GetTargetedEntity()` — fully vanilla |
| LCD scope drawing | YES | Sprite API — fully vanilla |
| IGC radar net | YES | `IGC.SendBroadcastMessage` — fully vanilla |
| WeaponCore targets | MOD REQUIRED | Needs WeaponCore mod. Gracefully skipped if absent. |
| WeaponCore gyro aim | MOD REQUIRED | Gyro override itself is vanilla; target feed needs WC. |
| WeaponCore barrage | MOD REQUIRED | `WcPbApi` — no-ops cleanly without the mod. |

**Summary:** You can run Rev Radar in a fully vanilla game. You get sensors, cameras,
and turret target detection out of the box. WeaponCore features are additive extras
that activate only when the mod is present.

---

## Block Setup

Place these blocks and tag their names:

| Tag in block name | Block type | Purpose |
|---|---|---|
| `[Radar]` | LCD / cockpit screen | Main tactical scope (default) |
| `[RadarInfo]` | LCD | Contact list table |
| `[RadarWide]` | LCD | Wide-area top-down scope |
| `[RadarWeapons]` | LCD | Weapons fire-control panel (WeaponCore) |
| *(any)* | Sensor block | Short-range proximity contacts |
| *(any)* | Camera block | Raycasting contacts (auto-enabled) |
| *(any)* | Large Turret Base | Vanilla turret target feed |
| *(any)* | Turret Control Block | Custom turret target feed |
| *(any)* | Radio Antenna | IGC radar net (optional) |
| *(any)* | Gyroscope | Used only for gyro aim (WeaponCore) |
| *(any)* | Cockpit / Remote Control | Orientation reference |

All blocks must be on the same construct as the Programmable Block.

---

## Detection Sources

The script gathers contacts every tick from four sources simultaneously.

### 1. Sensor Blocks (source tag: `S`)

`IMySensorBlock.DetectedEntities()` returns every entity inside the sensor's
configured detection box. Range depends on the sensor's slider settings in-game.
Good for close-range proximity alerts.

### 2. Camera Raycasts (source tag: `C`)

Cameras raycast in a round-robin sweep pattern, advancing one camera per tick.
Each camera fires at one of five angles: straight ahead, slightly up, slightly down,
slightly right, slightly left. Distance is capped at the smaller of configured range
and available scan charge. Cameras recharge over time, so more cameras = faster
coverage. Cameras detect asteroids as well as grids and players.

### 3. Vanilla Turrets (source tag: `T`)

Every `IMyLargeTurretBase` and `IMyTurretControlBlock` that currently has a target
reports it via `GetTargetedEntity()`. This only gives you what the turret is actively
tracking, not a wide search volume.

### 4. WeaponCore (source tag: `W`)

When the WeaponCore mod is active, the script calls:
- `GetSortedThreats()` — every hostile the grid AI detects, full 360 degrees
- `GetObstructions()` — entities blocking a firing solution (often asteroids)
- `GetAiFocus()` — the currently locked primary target

This is the most complete detection source when WC is installed. It sees contacts
that are behind you, beyond camera range, or outside sensor boxes.

### IGC Radar Net (source tag: `N`)

When `Network=true` in Custom Data, the script broadcasts its local track list over
IGC on the configured channel and receives contacts from other friendly grids on the
same channel. Received contacts are tagged as remote and are not re-broadcast.

---

## Contact Tracking

All contacts feed into a shared track table (dictionary keyed by entity ID).

Each track stores:
- World position and velocity
- Entity type (LargeGrid, SmallGrid, Character, Asteroid, etc.)
- Faction relationship (Owner, Friendly, Neutral, Enemy, NoOwnership)
- `Threat` flag — sticky; set by WC threat feed, never downgraded
- Last-seen time
- Source tag

Contacts are pruned after `HoldSeconds` (default 4 seconds) with no update.

The `Threat` flag is sticky intentionally: if WC marks something as hostile,
a later sensor or camera pass that returns NoOwnership for the same entity
cannot clear the red color.

---

## Scope Rendering

The tactical scope (`[Radar]`) is drawn entirely with SE sprites inside a
`MySpriteDrawFrame`. No text-mode output is used.

### Coordinate Projection

The scope is a 2D projection of 3D space using the ship's orientation reference.

For each contact:
1. Compute relative vector from ship to contact in world space.
2. Project onto the ship's **Right** and **Forward** axes to get the ground plane X/Y.
3. Project onto the ship's **Up** axis to get altitude.
4. Divide ground components by the scope range to get a 0-1 normalised position.
5. Multiply by scope radius in pixels and offset from screen center.

The `ProjectionAngle` setting (default 55 degrees) tilts the scope view. At 0 degrees
it is a flat top-down map. At 55 degrees it is an angled perspective view where
forward contacts appear near the top and altitude is clearly visible.
The forward axis is squashed by `cos(angle)` to create the ellipse shape.

### Altitude Stalks

Each contact is drawn at two positions:
- A small dot on the **ground plane** (where the contact would be if it were at your
  altitude) — this shows bearing and range.
- The **blip head** offset vertically by the altitude component — above your plane
  means blip above ground dot, below means blip below ground dot.

A line connects the two. On the flat wide-area scope, stalks are suppressed.

### Off-Scope Contacts

Contacts beyond the current range are not hidden. They are drawn as small dots
pinned to the ellipse rim at the correct bearing, with a distance label.

### Contact Colors

| Color | Meaning |
|---|---|
| Red | Hostile (enemy faction or WC threat flag) |
| Blue | Own ships / Owner faction / FriendlyNames list |
| Green | Allied faction |
| Yellow | Neutral or NoOwnership (unidentified) |
| Orange | Meteor |
| Grey | Asteroid / voxel |
| White halo | WeaponCore lock ring (drawn around WC-flagged threats) |

### Contact Shapes

| Shape | Meaning |
|---|---|
| Triangle | Large grid or small grid |
| Circle | Character, unknown, or anything else |
| Square | Asteroid / voxel |

---

## Wide-Area Scope (`[RadarWide]`)

Same drawing code as the tactical scope but with a fixed long range (`WideRange`,
default 50 km) and a separate tilt angle (`WideAngle`, default 0 = flat top-down).
Altitude stalks are disabled on this view for a clean strategic map feel.

---

## Contact List (`[RadarInfo]`)

A sorted table showing all current contacts. Sorted by priority then distance:

1. Hostiles (red) — nearest first
2. Neutral / unidentified (yellow) — nearest first
3. Friendly / owner (blue/green) — nearest first
4. Asteroids (grey) — nearest first

Columns: Contact name, Distance, Bearing (degrees, 0 = ahead), Type abbreviation.

If more contacts exist than fit on the screen, an overflow count is shown.

---

## Auto Range

When `AutoRange=true` (default), the scope radius is automatically set to the
longest targeting range of any turret on the grid. This means the scope edge
always matches how far your turrets can actually see. Use `rangeup` / `rangedown`
PB arguments to override manually; `auto` argument restores auto mode.

---

## Weapons Panel (`[RadarWeapons]`)

Requires WeaponCore. Shows live fire-control state:
- WC API status, armed/disarmed state
- Current target name, last-seen age, range, in-range status
- Aim and barrage mode status
- Spread distribution (how many weapons vs how many targets)
- Live firing state

### Commands

| Argument | Effect |
|---|---|
| `arm` | Enable weapons; nothing fires without this |
| `disarm` | Disable weapons, stop firing, release gyros |
| `aim` | Toggle gyro slew -- turns the ship to face the current target |
| `barrage` | Toggle distributed fire across all engageable contacts |

### Barrage Logic

When barrage is on, all weapons in the configured group (or all WC weapons if no
group is set) are distributed across all engageable targets in priority order.
Each target gets at least one weapon. Leftover weapons are stacked on the highest
priority targets. All weapons fire simultaneously.

### Gyro Aim

When `aim` is on and a target is selected, the script overrides all gyros to turn
the ship's forward axis toward the target. Uses per-gyro local axis transform:
each gyro gets its own local pitch/yaw/roll derived from the world-space rotation
vector. Gyros are released immediately when aim is turned off.

---

## Custom Data Reference

Paste into the Programmable Block's Custom Data. Auto-generated on first run.

```ini
[Radar]
LcdTag=[Radar]
InfoTag=[RadarInfo]
WideTag=[RadarWide]
Reference=
ProjectionAngle=55
WideRange=50000
WideAngle=0
AutoRange=true
Range=5000
HoldSeconds=4
ScanCameras=true
ScanAsteroids=true
UseWeaponCore=true
Network=false
NetworkTag=SKPRadar
ShowLabels=true
FriendlyNames=
WeaponsTag=[RadarWeapons]
WeaponGroup=
FireRange=8000
TargetLatch=6
AimGain=6
TickRate=10
```

| Key | Default | Notes |
|---|---|---|
| `LcdTag` | `[Radar]` | Tag in LCD name for the tactical scope |
| `InfoTag` | `[RadarInfo]` | Tag in LCD name for the contact list |
| `WideTag` | `[RadarWide]` | Tag in LCD name for the wide-area scope |
| `Reference` | *(blank)* | Cockpit/remote name for orientation. Blank = first available. |
| `ProjectionAngle` | `55` | 0 = flat top-down, 55 = angled perspective |
| `WideRange` | `50000` | Wide scope radius in metres |
| `WideAngle` | `0` | Wide scope tilt (0 = flat) |
| `AutoRange` | `true` | Match scope radius to longest turret range |
| `Range` | `5000` | Scope radius in metres when AutoRange is off |
| `HoldSeconds` | `4` | Seconds to keep a contact after last detection |
| `ScanCameras` | `true` | Enable camera raycasting |
| `ScanAsteroids` | `true` | Include voxels as contacts |
| `UseWeaponCore` | `true` | Use WC mod if present (harmless if absent) |
| `Network` | `false` | Share contacts over IGC with other grids |
| `NetworkTag` | `SKPRadar` | IGC channel name |
| `ShowLabels` | `true` | Draw contact name and distance labels on scope |
| `FriendlyNames` | *(blank)* | Comma-separated name fragments forced to blue (your fleet) |
| `WeaponsTag` | `[RadarWeapons]` | Tag for weapons panel LCD |
| `WeaponGroup` | *(blank)* | Block group of WC weapons to control. Blank = all WC weapons. |
| `FireRange` | `8000` | Max range (metres) for barrage/aim targeting |
| `TargetLatch` | `6` | Seconds to hold a target before re-selecting |
| `AimGain` | `6` | Gyro turn speed when aiming |
| `TickRate` | `10` | Update period in ticks: 1 / 10 / 100 |

---

## PB Run Arguments

| Argument | Effect |
|---|---|
| `reload` | Re-read Custom Data and re-scan all blocks |
| `rangeup` | Zoom out (multiply range by 1.5x) |
| `rangedown` | Zoom in (divide range by 1.5x) |
| `auto` | Return to auto range (match turret range) |
| `clear` | Wipe all contacts |
| `arm` | Arm weapons |
| `disarm` | Disarm weapons |
| `aim` | Toggle gyro aim |
| `barrage` | Toggle barrage fire |

---

## Performance Notes

- Block lists are rescanned every ~60 ticks (~10 seconds) automatically. Adding
  or removing blocks takes effect without recompiling.
- Camera raycasts are round-robin: one camera per tick, one angle slot per tick.
  This keeps charge usage low. More cameras = more coverage per second.
- WeaponCore threat scan runs every tick when WC is active. Use `TickRate=10`
  or higher on busy servers.
- The track table never grows unbounded -- pruning runs every tick based on
  `HoldSeconds`. Stale contacts are removed.

---

## Minimal Vanilla Setup (no mods)

1. Paste `Rev_Radar.cs` into a Programmable Block.
2. Name an LCD: `Main LCD [Radar]`
3. Add sensors pointed in the directions you care about.
4. Add cameras pointed forward.
5. Run the PB once. Custom Data auto-populates.
6. Done. The scope appears immediately.

For a contact list screen, name another LCD: `Contact List [RadarInfo]`

---

## File Map

```
Rev_Radar.cs              Paste into Programmable Block
Rev_Radar_Reference.md    This file -- setup and reference
```
