# ExcavOS Study Reference

Generated: 2026-06-02

Sources:

- Pasted ExcavOS script.
- Space Engineers Wiki gyroscope page: https://spaceengineers.fandom.com/wiki/Gyroscope
- Official/deprecated notice from Fandom points to the newer official wiki: https://spaceengineers.wiki.gg/

## What ExcavOS Is

ExcavOS is a cockpit/LCD operating system for a mining grid. It combines:

- Custom Data configuration.
- surface registration for cockpits and LCDs.
- temporary loading/lock overlays.
- cargo, weight, utility, and system managers.
- gravity align and cruise control commands.
- sprite-based drawing through a reusable `Painter`.

The script is not just a dashboard. It has a small framework shape:

```text
Program
  ExcavOS : ScriptHandler
    ExcavOSContext
      CargoManager
      SystemManager
      WeightAnalizer
      UtilityManager
    RegisteredProvider
      ScreenHandler per surface
      temporary immersive ScreenHandler per surface
```

## Program Flow

`Program()` creates storage, creates `ExcavOS`, and enables `Update10 | Update100`.

`Main(argument, updateSource)` delegates into `ExcavOS.Update(...)`.

`Save()` lets the context save persistent values into `Storage`.

The framework has two update speeds:

- `Update10`: refresh managers and draw screens.
- `Update100`: fetch/reload blocks and surfaces.

## Surface System

ExcavOS scans same-construct terminal blocks that are `IMyTextSurfaceProvider` and whose Custom Data contains an `[ExcavOS]` section.

For each display block it creates or reuses a `RegisteredProvider`.

The PB itself is forced to the main `ExcavOS` screen. Other display providers read their surface assignment from Custom Data.

The important pattern:

```text
RegisteredProvider
  permanent screen handlers
  immersive temporary screen handlers
```

If an immersive handler exists, it draws instead of the normal page. When `ShouldDispose()` returns true, the normal page returns automatically.

## Loading And Lock Screens

`LoadingScreen` is temporary. It stores:

- start time.
- loading duration.
- phrase/quote behavior.

Draw behavior:

- set the surface/frame through `Painter`.
- draw title near the top.
- draw a centered miner faction logo.
- draw one fake initialization phrase near the bottom.
- draw one progress bar near the bottom.

It disposes itself when the elapsed time exceeds the loading duration.

`LockScreen` is another temporary overlay shown when no user is in control, depending on the provider/immersion behavior.

## Painter

`Painter` is the main reason ExcavOS screens feel consistent.

It stores the active surface/frame and calculates:

- width.
- height.
- center.
- available size.
- surface offset.
- primary color from `surface.ScriptForegroundColor`.
- background color from `surface.ScriptBackgroundColor`.
- secondary color derived from the primary/background.

Common drawing helpers:

- `Text`
- `TextEx`
- `Sprite`
- `SpriteCentered`
- `RectangleEx`
- `FilledRectangleEx`
- `ProgressBar`
- `ProgressBarWithIconAndText`
- `ProgressBarVertical`
- `Radial`
- `FullRadial`

This is the style HogOS2 should copy: screens should ask the helper to draw repeated shapes rather than manually repeating sprite boilerplate everywhere.

## Cargo Manager

`CargoManager` scans inventory blocks, excluding sorters when no cargo tracking group is set.

It tracks:

- total current volume.
- total max volume.
- whether any ore exists.
- whether non-ore cargo exists.
- a dictionary of item type to amount/type id.

`CargoOre` displays only ore. `Cargo` displays non-ore cargo.

Cargo screens use item sprites, compact text rows, right-aligned amounts, and thin dividers.

## System Manager

`SystemManager` finds and caches:

- active cockpit/controller.
- lift thrusters.
- stop/brake thrusters.
- gyros.
- dump sorters.
- batteries.
- hydrogen tanks.
- reactors.
- hydrogen engines.

It prefers the active controller when possible and keeps block lists ready for other managers.

## Weight Analyzer

`WeightAnalizer` calculates:

- lift thrust needed.
- lift thrust available.
- stopping distance.
- stopping time.
- cargo fill rate over recent samples.
- warning state for disabled stop thrusters.

Lift thrust calculation:

- read ship physical mass from the active controller.
- read natural gravity strength.
- compare needed lift against lift thrusters projected along gravity.

Stop distance calculation:

- sum stop thruster force.
- divide by mass for deceleration.
- use current ship speed to estimate stopping time and distance.

Weight screen behavior:

- if there are no lift thrusters, treat it like rover mode and show cargo as a full radial.
- if lift thrusters exist, show lift usage as the upper radial and cargo usage as the lower radial.
- flash a danger icon when lift usage exceeds the warning threshold.

## Utility Manager

`UtilityManager` handles:

- gravity align.
- cruise control.
- dump sorter filter toggling.
- battery percentage.
- hydrogen tank percentage.
- uranium/reactor fuel.
- saving gravity/cruise state.

Commands include:

```text
toggle_gaa
set_gaa_pitch +5
toggle_cruise
set_cruise 20
dump Stone
```

## ExcavOS Gyro Logic

ExcavOS does not simply apply world rotation directly to all gyros.

Its `DoGravityAlign` method:

1. Gets the cockpit/controller orientation matrix.
2. Starts with controller local down.
3. Adjusts desired down by pitch offset:
   - negative pitch lerps down toward forward.
   - positive pitch lerps down toward backward.
4. Reads natural gravity and normalizes it.
5. For each gyro:
   - gets gyro orientation.
   - transforms desired controller down into gyro-local space.
   - transforms gravity into gyro-local space.
   - calculates a cross product rotation.
   - scales control velocity from the gyro's maximum yaw.
   - writes gyro `Pitch`, `Yaw`, and `Roll`.
   - enables `GyroOverride`.
6. If the error angle is tiny, it disables override for that gyro.

This matters because gyro override axes are local to each gyroscope. The Space Engineers wiki notes that override controls create constant torque/braking along a gyro axis, and gyros placed in different orientations can appear to have mismatched axes. It also notes that overridden gyros are not available for normal mouse control while override is active.

Reference:

- https://spaceengineers.fandom.com/wiki/Gyroscope

## ExcavOS Cruise Logic

ExcavOS cruise is a utility state. Commands toggle cruise and set the target speed.

The script uses configured thruster groups and the active controller speed to decide thrust override. The key implementation lesson for HogOS2 is:

- configuration should not repeatedly re-enable cruise after the player turns it off.
- mining modes may set a cruise speed, but normal cruise should clear mining mode.
- reverse/brake thrusters should be optional and disabled by default for forward cruise.

## What HogOS2 Should Borrow

Already ported or adapted:

- fixed temporary boot overlay.
- surface-driven color style.
- Menu screen command model.
- `view/go` navigation.
- ExcavOS-style gyro local-axis transform.
- ExcavOS-style Weight radial display.
- compact Utility rows.

Good next ports:

- cargo fill-rate sampling for Weight cargo ETA.
- bottom resource bars on Utility.
- sorter dump/jettison controls.
- proper per-display temporary overlays instead of one global boot timer.
- optional lock screen on cockpit exit.

