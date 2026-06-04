# HogOS2

HogOS2 is the next personal Space Engineers mining operating system for:

- `GroundHog`: atmospheric miner
- `SpaceHog`: ion miner
- `HydroHog`: hydrogen miner

Live script:

```text
Scripts/HogOS.cs
```

## Direction

HogOS2 keeps the compact HogOS miner core and adds:

- MotherOS-style slash command aliases
- semicolon-separated command routines
- a Mother GUI-style `Menu` screen
- ExcavOS-style surface-driven colors and dense cockpit rows
- a fixed 6-second cockpit-entry boot overlay

This is not a direct paste of the obfuscated Mother OS or Mother GUI scripts. It is a maintainable HogOS implementation that borrows their public command/menu model.

## New In v2.1

- Config section changed to `[HogOS2]`.
- Default cockpit layout changed to `OreCargo`, `Menu`, `Weight`, `Utility`.
- Added `[Menu]` Custom Data entries in `Label=command` form.
- Removed generated `[Boot]`, `[Screens]`, and `[Blocks]` PB Custom Data.
- Reworked boot into a cleaner ExcavOS-style loading screen with a larger central mark.
- Reworked gyro align using ExcavOS-style gyro-local axis transforms.
- Reworked Weight into lift/cargo radial gauges inspired by ExcavOS.
- Fixed cruise-off behavior so config refresh does not re-enable cruise after you stop it.
- Reworked Menu into large numbered actions without command text clutter.
- Added GUI navigation commands:
  - `view/up`
  - `view/down`
  - `view/select`
  - `view/back`
  - `view/go Menu`
  - `view/go Power`
- Added Mother-style command aliases:
  - `hog/boot`
  - `hog/stop`
  - `flight/cruise on`
  - `flight/cruise off`
  - `flight/level toggle`
  - `mine/slow`
  - `mine/fast`
  - `mine/terrain`

## Notes

Paste `Scripts/HogOS.cs` into a Programmable Block. On first run, HogOS2 writes readable defaults into the PB Custom Data.

Use toolbar buttons, button panels, timers, or event controllers to run PB arguments. `view/go` changes only the `Menu` surface, so the other cockpit screens stay permanent.
