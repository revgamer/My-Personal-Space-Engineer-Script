# HogOS Setup

Paste `HogOS/Scripts/HogOS.cs` into a Programmable Block.

On first run, HogOS writes readable defaults into the PB Custom Data. Edit these values for the vehicle.

## Profiles

```ini
[HogOS]
ShipName=GroundHog 1
Profile=GroundHog
Theme=Amber
```

Use:

- `GroundHog` for atmospheric miners
- `SpaceHog` for ion miners
- `HydroHog` for hydrogen miners

## Display Surfaces

All Hog cockpit variants are built around 4 screens. Put this on cockpit Custom Data:

```ini
[HogOS]
Surface0=Power
Surface1=Fuel
Surface2=Weight
Surface3=Dock
```

Optional wall LCD or swapped cockpit pages:

```ini
[HogOS]k
Surface0=CargoOre
Surface1=Utility
Surface2=Drills
Surface3=Splash
```

## Main Commands

```text
toggle_gaa
set_gaa_pitch +5
cruise on
cruise off
cruise 20
cruise +5

```ini
[Blocks]
DockRemote=Hog Dock Remote
```

`dock_start` flies the remote to the saved waypoint and connects when the connector becomes ready. `dock_stop` cancels the approach.

## Current Limits

This first build does connector management and manual unload, but not full PAM-style path recording or automatic flight docking yet.
