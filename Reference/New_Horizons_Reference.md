# New Horizons (Artificial Horizon HUD) — Reference

> **Study reference only — minified source, not authored by RevGamer.**
> Single-file PB script, 99,830 characters minified (under the 100k limit with
> roughly 170 bytes of headroom — see Size Notes below).
> No `[Script]` version banner in CustomData; internal `const string` markers
> identify it as a derivative of the classic "Artificial Horizon" HUD concept,
> rebuilt with datalink, voxel-collision warning, and dock-alignment systems
> layered on top.

---

## What It Does

New Horizons is a flight-instrument HUD script. It turns one or more LCDs (or
cockpit/seat text surfaces) into an artificial horizon display — similar to a
real aircraft attitude indicator — and layers on a stack of additional
systems: GPS/contact markers, an inter-ship "datalink" broadcast protocol,
voxel (planet/asteroid) collision warnings, a braking-thrust guidance marker,
connector docking alignment (2D and pseudo-3D), a mini local radar mode, and
fuel/ammo status bars. Everything is drawn with raw sprites — no
`TextSurface.Script` presets are used; the script owns the entire draw frame.

It is the same broad genre as Horizon Sentinel (RevGamer's own script) but
takes a different architectural approach worth comparing against:

| Aspect | New Horizons | Horizon Sentinel (RevGamer's) |
|---|---|---|
| Core concept | Real-time attitude indicator (sky/ground roll+pitch) | Multi-page status dashboard (power, cargo, descent, etc.) |
| Page model | One continuously-rendered HUD per surface | Paged navigation between named screens |
| Docking | Auto-detected nearby connector alignment overlay | Pre-departure checklist page |
| Networking | Custom `AHDL` datalink broadcast protocol over IGC | None currently |
| Radar | Built-in top-down local radar render mode | None currently |

---

## Feature Inventory

### 1. Attitude / Artificial Horizon Core
- Classic sky/ground split rendered as a rotated, translated square sprite,
  with the horizon line, roll angle, and pitch offset all computed from the
  controlled ship's `WorldMatrix` versus local gravity (`CalculateAHParameters`).
- Elevation ladder marks every 30 degrees using `AH_GravityHudPositiveDegrees`
  / `AH_GravityHudNegativeDegrees` textures, rotated and mirrored per side.
- In zero-gravity ("space mode"), instead draws a 3-axis (X/Y/Z) reference
  gizmo (`CalculateSpaceParameters` / `DrawSpace`) so the pilot can still tell
  orientation without a horizon.
- Velocity vector and retrograde marker (`AH_VelocityVector` texture) plotted
  at a screen position derived from the local-frame velocity direction.
- Speed/altitude (or speed/acceleration in space) text boxes with vertical
  speed sub-readout, using `MeasureStringInPixels` to size the box per text.

### 2. Ground/Voxel Collision Warning
- Up to N forward-facing cameras (any block named/tagged to match the surface
  tag) raycast forward each tick (`CalculateVoxelCollision`), filtered to
  `MyDetectedEntityType.Planet` / `.Asteroid` only.
- Computes closing rate via `Dot(velocity, hitDirection)` and a "voxel hit
  time" estimate; flags `VcWarning` once the projected distance crosses the
  configured warning/fast-beep/touch thresholds.
- A second, independent "ground rush" warning (`CW` flag) exists for the
  in-gravity attitude page: it tracks `TryGetPlanetElevation` per-tick,
  smooths the vertical rate through a 5-sample `CircularBuffer<double>`, and
  raises a flashing "PULL UP" textbox plus a converging cross-hair marker when
  time-to-impact drops under `TimeToCollisionThreshold`.
- Both warnings independently drive the sound-block alert (`PlaySounds`),
  picking whichever condition is more urgent that tick.

### 3. GPS / Contact Markers
- Parses `GPS:Name:X:Y:Z:colorHex:` lines out of a dedicated CustomData
  section per LCD (`ParseGMs`), with collision-avoiding label layout
  (`Rp`/overlap repulsion) and label truncation when two markers get too
  close together on-screen.
- "Mixed signal" clustering: when several markers fall within ~5 degrees of
  each other they collapse into a single bracket icon with a "N Mixed
  Signals" label instead of overlapping individually (`Mix`).

### 4. Datalink (Inter-Ship Broadcast)
- A custom text-pipe protocol (`AHDL|name|x|y|z|vx|vy|vz|colorHex|M0/M1|G,gridsize|C,connName,...`)
  broadcast over IGC on a configurable channel tag (default tied to
  `DatalinkTag`, `"AH_DATALINK"` is the documented default literal seen
  elsewhere in this protocol family).
- Each ship can optionally include its connector geometry (position, forward,
  up vectors) in the broadcast, which is what lets a *receiving* ship draw a
  remote ship's connectors and attempt 2D/3D docking alignment against them
  without ever being on the same grid.
- Listener-side parsing (`TryParseDatalink`) is defensive: malformed
  fields are skipped per-field with `TryParse`, never throwing on a bad
  packet from an out-of-sync remote script version.
- Also folds in local turret-detected targets (`Ht()` — any
  `IMyLargeTurretBase` / `IMyTurretControlBlock`'s currently tracked entity)
  into the same internal contact dictionary as red "threat" markers.

### 5. Connector / Docking Alignment
- `TryDock` scans every local connector against every known remote ship's
  saved connector geometry (from the datalink contact list, or from "saved
  connectors" persisted in CustomData via the `cn` console argument) and picks
  the best-scoring candidate by combined distance/angle/closing-speed.
- Two presentation modes per LCD: a 2D pitch/roll/yaw HUD overlay
  (`DrawDock`/`Dk2`) or, when `Show3D` is enabled, a full pseudo-3D wireframe
  rendering of the target connector's grid silhouette (cone/cylinder/cube
  approximation, `D3`) built entirely from line-sprite primitives via a
  custom perspective projection (`Pj`).
- `AutoAlign()` will optionally take over gyro control
  (`GyroOverride`/`Pitch`/`Yaw`/`Roll`) to automatically rotate the ship to
  face a docking target once `dock` argument triggers it, using a
  closed-form rotation-vector approach (axis-angle composition) rather than a
  PID loop.
- A standalone `Cap()` console command captures the *current* connector's
  position/forward/up as a manually-saved 3-point GPS triplet, written into a
  `[SAVED CONNECTORS]` CustomData block — a way to remember a fixed dock pad's
  geometry even without a live datalink broadcast.

### 6. Radar Mode
- A separate, optionally-tagged "Horizon Radar" surface renders a 2D top-down
  polar grid (`DrawRadar`) with range rings sized to nice round numbers via
  `Math.Pow(2, floor(log2(...)))`, auto-range support that expands to fit the
  farthest visible GPS/datalink contact plus padding, and the same contact
  set (GPS + datalink ships) projected through the ship's local frame.
- Supports an optional "search cone" sweep visual (`Sr`) for markers whose
  name contains `"Search"`.

### 7. Fuel / Ammo / Dampener Status
- Hydrogen tank fill percentage (filtered to tanks whose `SubtypeId` contains
  `"Hydro"`) and aggregate ammo-magazine fill fraction (grouped by subtype
  substring filter) are drawn as small bars flanking the horizon line.
- "Dampeners off" mode draws an animated corner-bracket border
  (`DrawDampenersOffBorder`) around each LCD's actual visible viewport,
  correctly accounting for multi-LCD "wall" spans (`WC`/`WR`/`WI`/`WJ`) so the
  border doesn't draw mid-wall on a multi-screen layout.

### 8. Multi-LCD "Wall" Stitching
- Any LCDs sharing the same `[WallN]` tag in their name are auto-clustered
  into a single logical wide/tall display (`Wal`/`Wl`/`Wc`), determining row/
  column position by nearest-grid-cell matching against the panel's own
  local axes — this lets one HUD render span several physically separate LCD
  panels as one coherent picture.

### 9. Boot/Status Screen
- The Programmable Block's own first surface (`AHT` class) renders a fixed
  status readout: datalink channel, TX/RX counts, contact count, last-seen
  contact name, and the rolling internal log (`Log.Default`), refreshed every
  5 seconds via `_titleCacheTimer`.

### 10. Config System
- A self-documenting `CVal<T>` family (`CStr`/`CDbl`/`CBool`/`CV3`/`CCol`/
  `CEnum<T>`) wraps every tunable in a typed accessor that auto-reads from
  and auto-writes back to `Me.CustomData` via `MyIni`, grouped into named
  `CSec` sections (General, GPS Markers, Thrust Marker, Voxel Collision,
  Dampeners, Datalink, Connector Alignment, Colors). Unknown/invalid values
  fall back to the compiled-in default rather than throwing.
- Per-LCD settings (display offsets, radar range/auto-range, which optional
  overlays are enabled) are stored in a parallel CustomData block on each
  target surface itself, separate from the PB's own settings.

---

## Notable Implementation Patterns

- **Static helper shorthand.** The whole file leans on extremely short static
  helpers (`Dt` = dot, `Cx` = cross, `Rv` = rotate vector, `Fc`/`Fs` = float
  cos/sin, `V2` = `new Vector2`, `Co` = `new Color`, `Sp` = `MySprite.CreateSprite`)
  to keep the minified character count down. This is a deliberate minifier
  strategy, not accidental obfuscation — every one of these has a single,
  consistent meaning throughout the file.
- **Migrated CustomData preservation.** When a non-PB surface's CustomData
  needs to be claimed for HUD config (`Wp`/`Ds`/`Hcd`), the *original* contents
  are not discarded — they get moved into a `[MIGRATED CUSTOM DATA FROM ...]`
  block inside the PB's own CustomData (`StripM`/`AddM`/`Rm`/`Gm`) so nothing
  the player wrote on that screen is lost, and the migration can be reversed
  by re-pointing to a different surface tag later.
- **Defensive parse-everywhere.** Every external string (datalink packets,
  GPS lines, color hex, CustomData ini values) goes through `TryParse`/`TryGetX`
  with an explicit fallback constant — there are no un-guarded `Parse()` calls
  that could throw and halt the PB on a malformed remote packet or a
  hand-edited CustomData typo.
- **Single draw pass, no caching across ticks beyond what `MySpriteDrawFrame`
  itself buffers.** Every surface is fully redrawn every `Update1` tick; the
  script accepts this cost in exchange for the smoothest possible needle/
  horizon motion. This is a different performance trade-off than RevGamer's
  own scripts, which generally prefer cached/staged redraws.

---

## Console Arguments

| Argument | Effect |
|---|---|
| `cn` | Saves the current best-available connector's geometry into the `[SAVED CONNECTORS]` CustomData block (`Cap()`). |
| `dock` | Toggles the auto-align gyro-assist routine (`Dock()`) — connects if a connector is ready, otherwise engages `AutoAlign()`. |
| *(none)* | Normal per-tick HUD/radar/dock-overlay render loop. |

---

## TypeId / API Surface Used

No inventory item TypeIds are parsed by string in this script beyond the
ammo-magazine fraction lookup, which filters by **SubtypeId substring**
rather than a full `MyItemType`/`TypeId` comparison:

```csharp
if (ty.TypeId.EndsWith("AmmoMagazine")) { string st = ty.SubtypeId; ... }
```

This is a string-suffix check against the runtime `TypeIdString`
(`MyObjectBuilder_AmmoMagazine`), which is the correct **inventory item**
TypeId category — it is never confused with a block definition TypeId
anywhere in the file. Hydrogen tanks are identified the same way, by
substring-matching `BlockDefinition.SubtypeId.Contains("Hydro")` rather than
an exact subtype list — broad but safe, since no vanilla or DLC hydrogen tank
subtype omits "Hydro" from its name.

Block-side APIs used include `IMyShipController`, `IMyShipConnector`,
`IMyThrust`, `IMyCameraBlock` (raycast), `IMyRadioAntenna`, `IMyLandingGear`,
`IMyLargeTurretBase` / `IMyTurretControlBlock`, `IMyGyro`, `IMyGasTank`,
`IMySoundBlock`, and `IMyTextSurface` / `IMyTextSurfaceProvider`. All are
standard Programmable Block API members; none require ModAPI access.

---

## Size Notes

At 99,830 characters this script is extremely close to the 100,000-character
PB compile limit — only about 170 characters of headroom remain. Any future
modification to this file (bug fix, new feature, or merge with another
script) must re-measure the final character count before considering the
change complete; there is essentially no slack left for additions without
first trimming something else (e.g. shortening a config key name or removing
an unused optional feature).
