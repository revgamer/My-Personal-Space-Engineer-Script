# Horizon Sentinel Unified Design

## Goal

Build one reusable large-ship script that can:

- show the Horizon Sentinel logo
- monitor ship readiness
- warn before unsafe takeoff or planet entry
- show cargo, power, jump, fuel, ammo, pressure, and damage
- support bridge LCDs and control seat screens
- later support optional auto ascend, auto descend, alignment, and broadcast

## Core Architecture

### Main Loop

Recommended update flow:

1. Parse commands.
2. Reload config if `CustomData` changed.
3. Refresh block cache on a slower interval.
4. Scan live values on each update cycle.
5. Calculate safety states.
6. Draw active screens.
7. Broadcast status if enabled.

Recommended update frequency:

- `Update10` for animated logo and active displays.
- Block cache refresh every 5-10 seconds.
- Heavy scans spread across ticks later if instruction count gets high.

### Data Layers

Use separate layers:

- **Config**
  - names, groups, thresholds, screen assignment
- **Block Cache**
  - batteries, tanks, cargo, jump drives, turrets, vents, thrusters, gyros, controllers
- **Metrics**
  - percentages, counts, masses, damage counts
- **Safety State**
  - ready/caution/not ready/critical
- **Renderer**
  - logo, bars, gauges, status cards, warnings
- **Commands**
  - page selection, refresh, future automation

## Config Model

Use PB `CustomData`.

Recommended first config:

```ini
[Horizon Sentinel]
Main LCD=Horizon Sentinel LCD
Use Control Seat Screens=true
Ship Controller=
Default Page=overview
Broadcast Status=true
Broadcast Tag=HORIZON_SENTINEL_STATUS

[Screens]
Overview LCD=Horizon Sentinel LCD
Flight LCD=Horizon Sentinel Flight
Systems LCD=Horizon Sentinel Systems
Cockpit LCD=
Cockpit Surface=0

[Groups]
Jump Drives=HS Jump Drives
Hydrogen Tanks=HS Hydrogen Tanks
Oxygen Tanks=HS Oxygen Tanks
Turrets=HS Turrets
Cargo=HS Cargo
Air Vents=HS Air Vents
Thrusters=HS Thrusters
Lifting Thrusters=HS Lifting Thrusters
Cruising Thrusters=HS Cruising Thrusters
Braking Thrusters=HS Braking Thrusters
Gyros=HS Gyros
Reactors=HS Reactors
Solar Panels=HS Solar Panels

[Thresholds]
Cargo Warning Percent=85
Battery Low Percent=25
Fuel Low Percent=25
Fuel Critical Percent=10
Ammo Low Percent=20
Pressure Warning Percent=80
Lift Margin Warning Percent=15
Planet Entry Fuel Warning Percent=35
Battery Reactor Start Percent=25
Battery Reactor Stop Percent=100
Hydrogen Balance Difference Percent=5

[Automation]
Auto Reactor Charging=false
Auto Hydrogen Balancing=false
Auto Ammo Balancing=false
Respect Manual Reactor On=true
```

Config should be optional. If groups are missing, the script should fall back to scanning same-grid blocks where possible.

## Screen System

### Screen Targets

Support:

- named LCD panels
- cockpits/control seats through `IMyTextSurfaceProvider`
- surface indexes
- PB screen fallback

Screen object:

```text
Name
BlockName
SurfaceIndex
Page
CompactMode
```

### Recommended Screens

Minimum:

- 1 LCD or PB screen with page cycling.

Planned tag-based layout:

- `[HS:Splash]`
  - PB screen or logo LCD
  - Horizon Sentinel splash/logo
- `[HS:Pilot]`
  - control seat/cockpit screen
  - power, fuel, oxygen level
  - Fancy Bar Display style compact bars
- `[HS:Jump]`
  - LCD or Corner LCD
  - jump drive gauge chart, online/offline, charging/ready, damage
- `[HS:Cargo]`
  - LCD
  - cargo status using Fancy Bar Display style
- `[HS:Descent]`
  - LCD
  - planet entry and descent safety check
- `[HS:Combat]`
  - control seat/cockpit screen or combat LCD
  - turret status and ammo using Fancy Bar Display style
- `[HS:Damage]`
  - LCD
  - damage and air vent leaking warning
- `[HS:Pressure]`
  - Corner LCD Top
  - pressurization, oxygen, leak warning
- `[HS:Thrusters]`
  - LCD or shared Flight LCD
  - lifting, cruising, and braking thruster group status
  - online/offline, damaged, thrust load, weak braking warning

Older simple 3-LCD bridge fallback:

- **Overview LCD**
  - master state
  - power bar
  - fuel bar
  - cargo bar
  - jump gauge
  - top warning
- **Flight LCD**
  - takeoff safety
  - planet entry warning
  - mass
  - gravity
  - lift margin
  - future auto ascend/descend mode
- **Systems LCD**
  - damage
  - pressurization
  - turret/ammo
  - gyros/thrusters
  - missing/damaged blocks
- **Control Seat Screen**
  - compact pilot warning
  - fuel
  - gravity
  - lift/entry safety
  - active mode

## Renderer Plan

### Widgets

Create small reusable drawing helpers:

- `DrawLogo`
- `DrawHeader`
- `DrawBar`
- `DrawGauge`
- `DrawStatusPill`
- `DrawWarningBox`
- `DrawTextLine`
- `DrawTitleOrnament`

Widget types:

- `BarWidget`
- `GaugeWidget`
- `TextWidget`
- `WarningWidget`

### Bars

Use horizontal bars for:

- cargo
- power
- fuel
- ammo
- pressure

Bar data:

```text
Label
Percent
ValueText
StatusColor
Icon
```

### Gauges

Use gauge charts for:

- jump drive charge
- lift margin
- planet entry confidence

Gauge data:

```text
Label
Percent
CenterText
Status
```

### Colors

Use Horizon Sentinel palette:

- safe green
- caution yellow
- warning orange
- critical red
- offline gray
- navy background
- sunrise orange accent
- white/silver text

## Metrics Plan

### Cargo

Blocks:

- cargo containers
- connectors
- cockpits/control seats
- refineries/assemblers if configured
- weapon inventories for ammo separately

Calculate:

- total current volume
- total max volume
- cargo percentage
- inventory mass using `IMyInventory.CurrentMass`
- cargo density estimate
- full-cargo mass prediction
- worst-case dense-cargo prediction

Safety use:

- warn if cargo exceeds threshold
- feed Takeoff Safety
- warn if cargo is dense enough to make planet entry unsafe
- use `Weight and Cargo Mass Reference.md` for density estimates, but prefer live API mass for current state

### Power

Blocks:

- batteries
- reactors
- reactor group `HS Reactors`
- solar panel group `HS Solar Panels`
- hydrogen engines
- solar/wind if present

Calculate:

- battery stored / max stored
- current output / max output
- current input/output
- online/functional count

Safety use:

- low power warning
- power deficit warning

Automation:

- if enabled, turn reactors on when batteries drop below configured start percent
- keep reactors on until batteries reach configured stop percent
- turn reactors off when full, unless manual/emergency override says to keep them on
- solar panels stay as normal maintenance power

### Jump Drives

Blocks:

- configured jump drive group first
- same-grid jump drives fallback

Calculate:

- charge percentage
- online count
- functional count
- charging/offline/damaged state

Display:

- gauge chart
- text `Charging`, `Ready`, `Offline`, or `Damaged`

### Fuel

Blocks:

- hydrogen tanks
- optionally oxygen tanks for survival screen

Calculate:

- hydrogen fill percentage
- tank online count
- functional count
- stockpile warning

Display:

- horizontal fuel bar
- warning if low/critical

Automation:

- optional hydrogen tank balancing
- compare tank fill percentages
- if tanks differ more than configured percentage, stockpile lower tanks until levels are close
- do not stockpile all tanks at once
- avoid balancing during takeoff, descent, combat, or critical fuel emergency

### Turrets And Ammo

Blocks:

- interior turrets
- gatling turrets
- missile turrets
- artillery/assault/cannon/turrets where API supports terminal weapon blocks
- modded weapons may need generic inventory scan

Calculate:

- turret online/functional count
- ammo inventory counts
- ammo volume percentage if exact capacity is hard

Display:

- horizontal ammo bar
- `Turrets: online/total`
- ammo summary by item subtype

Automation:

- optional turret ammo balancing
- scan turret inventories and ammo reserve cargo
- calculate ammo level per turret or turret group
- move ammo from reserve/high-ammo sources to low-ammo turrets
- support directional groups such as bottom/top/left/right/forward/rear
- warn if one group, such as bottom turrets, has fired too much and is low
- run slowly to avoid wasting instructions and avoid fighting active combat behavior

### Pressurization

Blocks:

- air vents
- oxygen tanks

Calculate:

- average oxygen level
- pressurized count
- depressurize mode count
- damaged/offline vents

Display:

- pressure bar
- `Pressurized`, `Partial`, `No Pressure`

### Thrusters

Blocks:

- all same-grid thrusters
- `HS Lifting Thrusters`
- `HS Cruising Thrusters`
- `HS Braking Thrusters`
- optional directional groups

Calculate:

- online count per group
- functional count per group
- damaged count per group
- current thrust / max effective thrust if available
- hydrogen thruster fuel starvation risk
- lift/brake/cruise readiness

Display:

- grouped horizontal bars:
  - `Lift`
  - `Cruise`
  - `Brake`
- warning if lift group is weak for takeoff
- warning if brake group is weak for descent
- warning if any group is missing

Safety use:

- Takeoff Safety depends on lifting thrusters.
- Planet Entry / Descent Safety depends on braking thrusters.
- Flight Assist depends on all three groups.

### Damage

Blocks:

- all terminal blocks on same grid
- critical groups tracked separately

Calculate:

- nonfunctional block count
- damaged critical count
- missing critical systems

Display:

- compact top damaged list
- critical warning

## Takeoff Safety Plan

Inputs:

- ship controller mass
- cargo mass/volume
- natural gravity
- upward thrust
- hydrogen fuel
- power
- gyro status
- connector/landing gear state

Output:

- `SAFE TO LIFT`
- `HEAVY BUT SAFE`
- `UNSAFE IN GRAVITY`
- `REMOVE CARGO`
- `NO GRAVITY DATA`

Implementation note:

Start with warning-only calculations. Do not add automatic thrust override until the warning model is trusted.

Mass calculation note:

- Use `controller.CalculateShipMass()` for ship base/total mass.
- Use inventory `CurrentMass` for scanned cargo mass.
- Use density tables only for prediction, for example "if remaining cargo space fills with iron/platinum/ore".

## Flight Assist Plan

Phase 3 should not fully automate flight yet. Add display and command placeholders first:

- mode: `MANUAL`
- `ascend`: show `NOT IMPLEMENTED - MONITOR ONLY`
- `descend`: show `NOT IMPLEMENTED - MONITOR ONLY`
- `align`: show `NOT IMPLEMENTED - MONITOR ONLY`
- `abort`: always clears future assist mode

Later phases:

- auto ascend
- auto descend
- gravity alignment
- planet entry avoidance warning
- IGC broadcast

## Commands

Initial commands:

- `logo`
- `overview`
- `flight`
- `systems`
- `takeoff`
- `jump`
- `fuel`
- `power`
- `combat`
- `pressure`
- `damage`
- `next`
- `prev`
- `refresh`
- `broadcast`
- `abort`

Future commands:

- `ascend`
- `descend`
- `align`

## Phase 3 Coding Plan

Recommended first code steps:

1. Move the current logo script into the Horizon Sentinel project folder as the base.
2. Add config parsing with defaults.
3. Add screen target discovery for LCDs and control seat surfaces.
4. Add block cache scanning.
5. Add metrics for:
   - cargo
   - power
   - jump drives
   - hydrogen fuel
6. Draw overview screen with bars/gauge.
7. Add pressure, turrets/ammo, and damage.
8. Add takeoff safety calculations.
9. Add page commands and cockpit compact view.
10. Add broadcast status.

Testing order:

- compile after each small step
- test logo only
- test screen discovery
- test one metric at a time
- test empty/missing group behavior
- test with one LCD, then 3 LCDs plus control seat

## Important Design Decision

Horizon Sentinel should be **monitor-first**.

Automation can come later, but the first reliable script should warn the pilot clearly:

- can take off
- cannot take off
- too heavy for planet
- enough fuel
- not enough fuel
- jump drives ready
- pressure safe
- weapons ready
- critical damage found

Once those warnings are trustworthy, auto ascend/descent becomes much safer to build.
