# HogOS Setup

Paste `HogOS/Scripts/HogOS.cs` into a Programmable Block.

On first run, HogOS writes readable defaults into the PB Custom Data. Custom Data is for setup/config. Toolbar button commands should be PB run arguments.

## PB Custom Data

```ini
[HogOS]
ShipName=GroundHog 1
Profile=GroundHog
AutoProfile=True
Theme=AGM

[Boot]
Seconds=5

[Screens]
Surface0=Power
Surface1=CargoOre
Surface2=Weight
Surface3=Utility

[Groups]
LiftThrusters=[RGH] Lift Thrusters
StopThrusters=[RGH] Brake Thrusters
ForwardThrusters=[RGH] Cruising Thrusters
ReverseThrusters=[RGH] Brake Thrusters
CargoTrack=
AlignGyros=[RGH] Gyros
Drills=[RGH] Drills

[Blocks]
Controller=

[Safety]
LiftWarning=0.9
LiftCutoff=0.98
CargoReturn=0.9
BatteryReturn=0.2
HydrogenReturn=0.25

[Cruise]
Enabled=False
TargetSpeed=20
UseReverseThrusters=True

[Mining]
SlowSpeed=0.03
FastSpeed=1.0
TerrainClearingSpeed=2.5
AutoLevel=False
```

Profiles:

- `GroundHog` for atmospheric miners
- `SpaceHog` for ion miners
- `HydroHog` for hydrogen miners

With `AutoProfile=True`, HogOS scans thrusters on the same construct and sets the profile automatically:

- atmospheric thrusters: `GroundHog`
- ion thrusters: `SpaceHog`
- hydrogen thrusters: `HydroHog`

## Toolbar Arguments

System:

```text
reboot
write_config
```

Gyro auto-level:

```text
toggle_gyro
set_gyro_pitch_0
set_gyro_pitch +5
set_gyro_pitch -5
```

Cruise:

```text
stop
toggle_cruise
cruise on
cruise off
cruise 20
cruise +5
cruise -5
cruise_full
cruise_reset
set_cruise 20
```

Mining modes:

```text
mine_slow
mine_fast
terrain_clear
mine_stop
mine stop
mine slow
mine fast
mine clear
```

## Notes

- HogOS shows a temporary loading screen on all LCDs when the PB starts, when you enter the cockpit, or when you run `reboot`.
- `[Boot] Seconds` controls how long the loading screen stays up.
- LCD styling follows each surface's Script foreground/background colors, similar to ExcavOS. Set the LCD foreground color for the main HUD color and background color for the screen base.
- HogOS automatically upgrades older PB Custom Data into the clean release layout when required sections are missing.
- `write_config` preserves current values and rewrites PB Custom Data into the clean release layout shown above.
- `Boot` and `Splash` can still be used as permanent page names, but normal cockpit screens no longer need them for startup.
- `Weight` shows mass, lift use graph, lift force, and lift thruster status. Cargo capacity is only on `CargoOre`.
- `cruise_full` sets cruise to the configured `[Cruise] TargetSpeed` and turns cruise on.
- `cruise_reset` resets cruise to `[Cruise] TargetSpeed` and turns cruise off.
- Mining mode commands use `[Groups] ForwardThrusters`, set cruise to the configured mining speed, and turn cruise on.
- Mining mode cruise is forward-only. Reverse/brake thruster override is disabled while mining mode is active.
- Mining mode commands also turn `[Groups] Drills` on. Leave `Drills=` blank to use every drill on the same construct. `stop`, `cruise_stop`, and `mine_stop` turn drills off.
- `terrain_clear` enables the drill Terrain Clearing Mode option and turns drills on.
- Set `[Mining] AutoLevel=True` only if you want mining modes to turn gyro auto-level on.
- `stop`, `cruise_stop`, and `mine_stop` turn cruise/mining mode off, clear thrust override, release gyro override, and turn drills off.
- Old aliases still work: `toggle_gaa`, `set_gaa_pitch +5`.
- Dock, unload, path, fuel-only, drill-only, and splash pages are not included in this focused build.
