# AutoGrid Manager v1.5 -- Step-By-Step Setup

---

## 1. Install

Paste minified `Script.cs` into a Programmable Block. Compile.
AGM writes default Custom Data on first run.

---

## 2. Block Groups

Create these groups (names are your choice -- update config to match):

```
Base Batteries
Base Reactors
Base Assemblers
Base Refineries
Base Ice Generators
Base Hydrogen Tanks
Base Oxygen Tanks
```

---

## 3. PB Custom Data -- Minimum Config

```ini
[Core]
enabled=true
power=true
logistics=true
production=true
include_docked_grids=false

[Power:Base]
batteries=G:Base Batteries
reactors=G:Base Reactors

[Logistics]
auto_assign=true
max_moves_per_run=2

[Production]
enabled=true
monitor_only=false
autocraft_components=true
auto_disassemble=false
assemblers=G:Base Assemblers
refineries=G:Base Refineries
max_queue_per_run=5
max_queue_amount=5000
```

---

## 4. LCD Screens

Add `[AGM-S]` to LCD block name. Put one command in LCD Custom Data.

```
CoreDashboard
PowerDashboard page=1
ReactorRefuel
BatteryControl
LogisticsDashboard
ProductionDashboard page=1
ProductionDetails
ProductionWarnings
ComponentStock page=1
OreStock page=1
IngotStock page=1
AmmoStock page=1
ToolStock page=1
BottleStock page=1
FoodStock page=1
SeedStock page=1
IngredientStock page=1
Autocrafting page=1
FuelLifeSupport
AlertDashboard
```

---

## 5. Alert Lights and Corner LCDs

Put `[AGM-LIGHT]` in block **Custom Data**:

```ini
[AGM-LIGHT]
watch=Battery
```

Watch values: Battery, Cargo, Hydrogen, Oxygen, Uranium, Production, Charging, Power OK

Do NOT add `[AGM-S]` to these blocks.

---

## 6. Cargo Containers

Put type tag in block name OR Custom Data:

```
{Ore 1}       {Ingot 1}       {Component 1}
{Ammo 1}      {Tools 1}       {Bottle 1}
{Food 1}      {Seed 1}        {Ingredient 1}
```

Lower number fills first. Add `{Ore 2}` etc for overflow containers.

---

## 7. Docked Ships -- No Sorting

Put `{No AGM}` in the **connector** Custom Data or block name to stop AGM pulling from the docked ship:

```
{No AGM}
```

---

## 8. Component Quotas

Under `[AssemblerPriority]` in PB Custom Data:

```ini
AutoCrafting=Component
SteelPlate=70000
InteriorPlate=70000
Construction=70000
Computer=10000
Motor=15000
MetalGrid=10000
SmallTube=10000
LargeTube=10000
Display=5000
BulletproofGlass=5000
PowerCell=5000
SolarCell=1000
Detector=1000
RadioCommunication=1000
Medical=200
Reactor=10000
Thrust=12000
GravityGenerator=500
Superconductor=10000
```

---

## 9. Troubleshooting

| Problem | Fix |
|---------|-----|
| LCD blank | `[AGM-S]` in block name + valid command in Custom Data |
| Autocrafting not queuing | `monitor_only=false` in [Production] |
| Docked ship still sorted | `{No AGM}` in connector Custom Data, recompile PB |
| Corner LCD blank | `[AGM-LIGHT]` in Custom Data only, no `[AGM-S]` on same block |
| Instruction limit | Lower `max_moves_per_run` and `max_queue_per_run` |
| Screens crash on entry | Recompile PB -- draw system is crash-protected in v1.5 |
