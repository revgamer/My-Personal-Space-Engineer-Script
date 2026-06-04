# HogOS2 Setup

Paste `HogOS2/Scripts/HogOS.cs` into a Programmable Block.

HogOS2 now keeps the cockpit screen layout hardcoded:

```ini
[HogOS2]
Surface0=OreCargo
Surface1=Menu
Surface2=Weight
Surface3=Utility
```

You do not need a `[Screens]` section in the PB Custom Data. Use display/cockpit Custom Data only if you want to override a specific display provider.

## PB Custom Data

HogOS2 writes this clean setup when old/stale config is detected:

```ini
[HogOS2]
ShipName=Rev Spacehog 01

[Menu]
0=1 - Slow Mining=mine/slow
1=2 - Fast Mining=mine/fast
2=3 - Clear Terrain=mine/terrain
3=4 - Cancel=hog/stop
4=5 - Flight Cruising On=flight/cruise on
5=6 - Flight Level On/Off=flight/level toggle
6=7 - Power Dashboard screen=view/go Power

[Groups]
LiftThrusters=[RSH] Lift Thrusters
StopThrusters=[RSH] Brake Thrusters
ForwardThrusters=[RSH] Cruising Thrusters
ReverseThrusters=[RSH] Brake Thrusters
AlignGyros=[RSH] Gyros
Drills=[RSH] Drills

[Safety]
CargoReturn=0.9
BatteryReturn=0.2
HydrogenReturn=0.25

[Cruise]
TargetSpeed=20
UseReverseThrusters=False

[Mining]
SlowSpeed=0.03
FastSpeed=1.0
TerrainClearingSpeed=2.5
AutoLevel=True
```

Boot is fixed at 6 seconds in code and triggers on script start, `hog/boot`, and cockpit entry. No `[Boot]` Custom Data section is needed.

## Toolbar Arguments

Menu/display navigation:

```text
view/up
view/down
view/select
view/go Menu
view/go Power
view/go OreCargo
view/go Weight
view/go Utility
```

`view/go` changes only the `Menu` surface. The permanent cockpit screens stay permanent:

- Surface0: `OreCargo`
- Surface2: `Weight`
- Surface3: `Utility`

Cruise:

```text
flight/cruise on
flight/cruise off
flight/cruise 20
flight/cruise +5
flight/cruise -5
```

`flight/cruise on` always uses `[Cruise] TargetSpeed`. Mining mode speeds are separate and do not overwrite normal flight cruise.

Gyro auto-level:

```text
flight/level toggle
flight/level on
flight/level off
flight/level_pitch +5
flight/level_zero
```

Flight Level is useful on planets and moons where natural gravity exists. In deep space, there is no natural gravity vector for the script to align against, so HogOS2 releases gyro override and reports `No natural gravity`.

Mining:

```text
mine/slow
mine/fast
mine/terrain
hog/stop
```

Routines can be chained:

```text
mine/slow; flight/level on;
hog/stop; view/go Menu;
```
