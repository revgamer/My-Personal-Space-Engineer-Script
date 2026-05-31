# AutoGrid Manager v1.3 Step-By-Step Setup

This guide is for the clean unified AGM layout:

```text
AGM/Scripts/AGM.cs
```

AGM v1.3 uses one programmable block. The PB runs logistics, power, reactor refuel, battery control, production monitoring, autocrafting, stock dashboards, alerts, and fuel/life support.

## 1. What To Do First

1. Make sure your base blocks are named or grouped clearly.
2. Create the block groups you want AGM to use.
3. Paste `Scripts/AGM.cs` into one programmable block.
4. Let AGM create default Custom Data.
5. Edit PB Custom Data to point AGM at your block groups.
6. Add `[AGM-S]` to LCD names and put one dashboard command in each LCD Custom Data.

Do not start by adding every LCD. Get the PB compiling first, then configure power and production groups, then add screens.

## 2. Create The Programmable Block

Create one programmable block.

Suggested name:

```text
PB AutoGrid Manager
```

Paste:

```text
AGM/Scripts/AGM.cs
```

Compile the PB. If the PB Custom Data is empty, AGM writes default config automatically.

## 3. Create Base Groups

AGM can scan connected grids, but for base-only control you should use block groups and set ungrouped scanning off.

Recommended groups:

```text
[RAB] Batteries
[RAB] Nuclear Reactors
[RAB] Assemblers
[RAB] Refineries
[RAB] Ice Generators
[RAB] Hydrogen Tanks
[RAB] Oxygen Tanks
```

Use your own tag if needed. The important part is that PB Custom Data points to the same group names.

## 4. Edit Core Config

Use this as the first section in PB Custom Data:

```ini
[Core]
enabled=true
power=true
logistics=true
production=true
global_pause=false
include_docked_grids=false
no_sorting_tag=[No Sorting]
locked_tag={Locked}
manual_tag={Manual}
hidden_tag={Hidden}
```

Set `include_docked_grids=false` when AGM should focus on the base only.

## 5. Configure Power

Use groups if you do not want AGM reading other connected-grid reactors or batteries.

```ini
[Power:Base]
batteries=G:[RAB] Batteries
reactors=G:[RAB] Nuclear Reactors
solar=
wind=
hydrogen=
include_ungrouped=false
```

Blank `solar`, `wind`, and `hydrogen` means AGM will show zero for those rows unless you add groups later.

## 6. Configure Reactor Refuel

Reactor refuel reads the configured reactor group. It does not need to touch other-grid reactors if `reactors=G:...` is set.

```ini
[ReactorRefuel]
enabled=true
min_uranium_per_reactor=2
target_uranium_per_reactor=10
uranium_low_warning_kg=5
auto_refuel=false
```

Start with `auto_refuel=false`. Turn it on only after the reactor page shows the correct reactor group.

## 7. Configure Battery Control

```ini
[PowerControl]
enabled=true
auto_reactor_charge=true
battery_low_percent=25
battery_full_percent=100
control_reactors=G:[RAB] Nuclear Reactors
control_batteries=G:[RAB] Batteries
turn_reactors_off_when_full=true
amber_while_charging=true
minimum_reactors_online=0
never_turn_off_reactors_if_output_above_percent=80
```

This lets AGM turn controlled reactors on when batteries are low, then turn them off when batteries are full unless load safety says not to.

## 8. Configure Fuel And Life Support

```ini
[FuelLifeSupport]
o2h2_generators=G:[RAB] Ice Generators
h2_tanks=G:[RAB] Hydrogen Tanks
o2_tanks=G:[RAB] Oxygen Tanks
include_ungrouped=false
```

Use `include_ungrouped=false` for a clean base-only setup.

## 9. Configure Logistics

```ini
[Logistics]
auto_assign=true
max_moves_per_run=2
```

Protection tags:

```text
[No Sorting]
{Locked}
{Manual}
{Hidden}
```

AGM will not sort protected cargo. It also avoids reactor, gas generator, and tank inventories so it does not fight Space Engineers' native fuel handling.

## 10. Configure Production

```ini
[Production]
monitor_only=true
autocraft_components=true
sort_assembler_queue=true
sort_refinery_input=true
max_queue_per_run=2
max_queue_amount=500
assemblers=G:[RAB] Assemblers
refineries=G:[RAB] Refineries
enabled=true
show_machine_details=true
show_current_blueprint=true
show_refinery_input=true
show_missing_resources=false
show_blocked_assemblers=false
show_blocked_refineries=false
missing_warning_below_percent=90
```

Keep `monitor_only=true` while testing. Change it only when you want AGM to queue autocrafting.

## 11. Set Refinery Priority

AGM reads this from PB Custom Data. The first ore in the list has highest priority.

```ini
[RefineryPriority]
Stone
Gold
Platinum
Uranium
Iron
Nickel
Cobalt
Silicon
Magnesium
Silver
```

## 12. Set Assembler Priority And Component Goals

```ini
[AssemblerPriority]
SteelPlate
InteriorPlate
Construction
Computer
Motor
Display
MetalGrid
SmallTube
LargeTube
GravityGenerator
Superconductor

AutoCrafting=Component
SteelPlate=70000
InteriorPlate=70000
Construction=70000
Computer=10000
Motor=15000
MetalGrid=10000
Girder=10000
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
Explosives=500
Canvas=200
ShieldComponent=2000
```

## 13. Add LCD Dashboards

Add `[AGM-S]` to the LCD name.

Put one command in the LCD Custom Data:

```text
PowerDashboard page=1
ReactorRefuel
BatteryControl
LogisticsDashboard
ProductionDashboard page=1
ProductionDetails
ProductionWarnings
ComponentStock page=1
Autocrafting page=1
FuelLifeSupport
AlertDashboard
```

More commands are listed in `AGM_v1.3_LCD_Dashboards.md`.

## 14. First Test Order

Use this order when setting up a new base:

1. Compile PB.
2. Check PB front screen shows AGM v1.3.
3. Add only one LCD with `CoreDashboard`.
4. Add one LCD with `PowerDashboard page=1`.
5. Confirm batteries/reactors are from your base group only.
6. Add `ReactorRefuel` and confirm reactor count.
7. Add production groups and check `ProductionDetails`.
8. Add stock/autocrafting pages.
9. Turn on production queue/autorefuel only after dashboards look right.

## 15. Troubleshooting

If a screen is blank:

- Confirm LCD name contains `[AGM-S]`.
- Confirm LCD Custom Data has one valid dashboard command.
- Recompile or run the PB again.

If AGM sees other-grid blocks:

- Use `G:Group Name` entries.
- Set `include_ungrouped=false` in that section.
- Check that the group does not include docked ship blocks.

If Space Engineers says instruction limit:

- Recompile after the latest `Scripts/AGM.cs`.
- Avoid using many unnecessary LCDs during testing.
- Keep `max_moves_per_run` and `max_queue_per_run` low.
