# Horizon Sentinel Reference

## Purpose

Horizon Sentinel is a large-ship safety and readiness monitor for Space Engineers.

The script should answer one main question:

> Is this ship safe and ready to fly, jump, fight, and survive?

It should monitor cargo load, ship mass, gravity risk, power, jump drives, hydrogen, oxygen, air pressure, turret ammo, and damage. The script should be reusable on any ship, not tied to one ship name.

## Display Style

The first screen should use the Horizon Sentinel logo:

- dark ocean sunrise background
- shield outline
- white crosshair circle inside the shield
- pulsing red center dot
- `Horizon Sentinel` title
- orange/red ornament line under the title

Future status screens can use the same color language:

- green: safe / ready / online
- yellow: caution / partial / low
- orange: warning / needs attention
- red: unsafe / critical / damaged
- gray: offline / missing / disabled

## Suggested Module Names

Instead of only "Cargo Safety Check", better module names could be:

- **Lift Safety**
- **Mass Safety**
- **Cargo Lift Safety**
- **Takeoff Safety**
- **Gravity Clearance**
- **Launch Readiness**
- **Load Safety**

Best name for the cargo/mass/gravity system:

**Takeoff Safety**

Reason: it clearly covers cargo fullness, ship mass, gravity, thrust margin, and whether the ship can safely lift off.

## Main Modules

### 0. Flight Assist

Purpose:

Assist with safe planetary flight operations while still letting the pilot stay in control.

This should be optional and conservative. Automatic movement can crash a ship if the script has bad data, missing thrusters, wrong orientation, damaged gyros, or unexpected gravity.

Suggested modes:

- **Auto Ascend**
- **Auto Descend**
- **Planet Entry Warning**
- **Descent Alignment**
- **Broadcast Status**

#### Auto Ascend

Purpose:

Help the ship climb away from a planet or moon safely.

Checks before activation:

- cockpit/remote control found
- gyros online
- upward/lift thrusters online
- fuel available
- power available
- connectors/landing gear unlocked
- no critical damage
- ship is not too heavy for local gravity

Possible behavior:

- keep ship level
- apply upward thrust override
- reduce thrust override when clear of gravity
- stop if fuel, power, or damage becomes unsafe
- stop if pilot gives a cancel command

Possible PB commands:

- `ascend`
- `ascend stop`
- `abort`

#### Auto Descend

Purpose:

Help the ship descend from space toward a planet or moon with controlled speed and level attitude.

Checks before activation:

- ship has enough thrust to slow down in target gravity
- hydrogen fuel is enough for braking and landing
- batteries/reactors can support flight systems
- gyros are online
- braking thrust exists
- cargo/mass is safe for the target planet

Possible behavior:

- keep ship level with planetary gravity
- limit descent speed
- warn if gravity gets stronger than expected
- stop/abort if ship becomes unsafe
- display `DESCENT SAFE`, `DESCENT CAUTION`, or `ABORT DESCENT`

Possible PB commands:

- `descend`
- `descend stop`
- `abort`

#### Planet Entry Warning

Purpose:

Warn the pilot before entering planetary gravity if the ship is too heavy or not equipped to leave/land safely.

Checks:

- current ship mass
- cargo mass
- fuel percentage
- battery reserve
- available thrust
- expected gravity
- damaged thrusters/gyros
- tank stockpile state

Possible warnings:

- `PLANET ENTRY SAFE`
- `HEAVY FOR PLANET`
- `FUEL TOO LOW FOR LANDING`
- `CAN LAND, MAY NOT LIFT`
- `DO NOT ENTER GRAVITY`
- `REMOVE CARGO BEFORE PLANET`

Important idea:

The script should warn before the ship commits to a planet. If it predicts that the ship can descend but cannot take off again, it should clearly say so.

#### Descent Alignment

Purpose:

Keep the ship level relative to planetary gravity during descent.

Checks:

- natural gravity vector from cockpit/remote control
- ship orientation
- gyro status
- dampeners/thrust status

Possible behavior:

- align ship up direction opposite to gravity
- keep belly/lift thrusters facing downward if the ship is designed that way
- avoid flipping ship aggressively
- allow pilot override/cancel

Notes:

- This needs careful tuning because different ships have different "up" directions.
- The script should support a config setting for the main ship controller and orientation.

#### Broadcast Status

Purpose:

Broadcast ship safety/readiness status to other programmable blocks, command ships, stations, or fleet displays.

Possible systems:

- IGC broadcast channel
- antenna online for long-range broadcast
- optional listener script on a base/station

Possible broadcast data:

- ship name
- master readiness state
- cargo percentage
- mass
- fuel percentage
- power percentage
- jump charge percentage
- damage count
- planet entry warning
- current flight assist mode

Suggested broadcast tag:

`HORIZON_SENTINEL_STATUS`

Example broadcast text:

```text
Ship=Manta Ray
State=CAUTION
Mode=DESCENT
Mass=4250000
Cargo=82
Fuel=41
Power=76
Jump=100
Warning=HEAVY_FOR_PLANET
```

### 1. Takeoff Safety

Purpose:

Check whether the ship can safely take off based on ship mass, cargo load, gravity, and available thrust.

Checks:

- current ship mass
- base ship mass
- cargo mass
- total mass
- local natural gravity
- available upward thrust
- whether dampeners/lift thrust can beat gravity
- cargo fullness percentage
- overloaded containers/connectors/refineries/assemblers

Possible status:

- `SAFE TO LIFT`
- `HEAVY BUT SAFE`
- `CARGO WARNING`
- `UNSAFE IN GRAVITY`
- `REMOVE CARGO`
- `NO GRAVITY DATA`

Display idea:

- mass readout: `Mass: 4,250,000 kg`
- cargo readout: `Cargo: 82%`
- lift margin: `Lift Margin: 18%`
- gauge chart for cargo fullness
- warning text if ship is too heavy for planetary takeoff

PB/API notes:

- Use ship controller/cockpit/remote control for ship mass and gravity.
- Use thrusters grouped by direction if possible.
- Cargo mass can be approximated from inventories.
- A later implementation should decide whether to calculate thrust by orientation or by named groups.

### 2. Power Safety

Purpose:

Check if the ship has enough power generation and battery reserve for flight and emergency use.

Checks:

- batteries online/offline
- battery charge percentage
- current input/output
- reactors online/offline
- solar/wind if relevant
- hydrogen engines if present
- total stored power
- power overload risk

Possible status:

- `POWER READY`
- `BATTERY LOW`
- `REACTOR OFFLINE`
- `POWER DEFICIT`
- `EMERGENCY RESERVE`

Display idea:

- battery gauge
- generation vs usage bar
- count: `Batteries: 8/8 Online`
- alert if battery is below chosen threshold, for example 25%

### 3. Jump Drive Status

Purpose:

Monitor all jump drives, usually from a named block group.

Suggested group name:

`HS Jump Drives`

Checks:

- jump drive count
- online/offline
- enabled/disabled
- charging status
- charge percentage
- damaged/nonfunctional drives
- total ready jump drives

Possible status:

- `JUMP READY`
- `CHARGING`
- `PARTIAL CHARGE`
- `OFFLINE`
- `DAMAGED`
- `NO JUMP GROUP`

Display idea:

- gauge chart for total charge
- per-drive small bars
- text like `Jump: 74% Charging`
- count like `Online: 3/4`

### 4. Fuel Status

Purpose:

Monitor hydrogen fuel and warn when the ship is not safe for flight or travel.

Checks:

- hydrogen tank fill percentage
- tank online/offline
- stockpile on/off
- damaged tanks
- hydrogen engines if present
- low fuel threshold
- full tank status

Possible status:

- `FUEL READY`
- `FUEL LOW`
- `CRITICAL FUEL`
- `TANK STOCKPILE ON`
- `TANK DAMAGED`
- `NO TANKS FOUND`

Display idea:

- horizontal fuel gauge
- tank count: `Tanks: 6/6 Online`
- warning if stockpile is enabled during flight
- thresholds:
  - green: 50-100%
  - yellow: 25-49%
  - orange: 10-24%
  - red: 0-9%

### 5. Turret And Ammo Status

Purpose:

If turrets are installed, monitor combat readiness and ammunition.

Checks:

- turret count
- online/offline
- functional/damaged
- enabled/disabled
- has ammo
- total ammo count by item type
- per-turret ammo if possible

Possible status:

- `WEAPONS READY`
- `LOW AMMO`
- `NO AMMO`
- `TURRETS OFFLINE`
- `TURRETS DAMAGED`
- `NO TURRETS FOUND`

Display idea:

- horizontal ammo bar
- turret count: `Turrets: 12/14 Online`
- ammo count summary:
  - `NATO 25x184mm: 12,400`
  - `Missiles: 180`
  - `Railgun Sabots: 24`

Notes:

- Ammo checking should scan turret inventories and cargo inventories.
- Some weapon mods may use custom ammo types, so this module should avoid hardcoding only vanilla ammo later.

### 6. Pressurization Status

Purpose:

Monitor ship air vents and room pressure safety.

Checks:

- air vent count
- pressurized/depressurized rooms
- oxygen level
- vent online/offline
- vent damaged
- depressurize mode
- oxygen tanks status if present

Possible status:

- `PRESSURIZED`
- `PARTIAL PRESSURE`
- `NO PRESSURE`
- `VENTS OFFLINE`
- `VENT DAMAGED`
- `DEPRESSURIZE MODE`

Display idea:

- pressure gauge
- `Pressure: 97%`
- count: `Vents: 8/8 Online`
- warning if any important vent is unpressurized

### 7. Damage Status

Purpose:

Check ship health by finding damaged, nonfunctional, or incomplete blocks.

Checks:

- damaged blocks
- nonfunctional blocks
- incomplete blocks
- critical systems damaged
- block groups with damage

Critical systems:

- cockpit/remote control
- thrusters
- gyros
- reactors/batteries
- hydrogen tanks
- oxygen tanks
- jump drives
- turrets
- cargo containers
- connectors
- air vents

Possible status:

- `NO DAMAGE`
- `MINOR DAMAGE`
- `SYSTEM DAMAGE`
- `CRITICAL DAMAGE`
- `REPAIR REQUIRED`

Display idea:

- list top damaged blocks
- category count:
  - `Thrusters: 2 damaged`
  - `Jump Drives: 1 damaged`
  - `Vents: 1 damaged`
- red warning if any critical block is nonfunctional

## Master Safety Check

Purpose:

Combine all modules into one final ship readiness result.

Possible master states:

- `READY`
- `CAUTION`
- `NOT READY`
- `CRITICAL`

Takeoff readiness should consider:

- ship mass
- cargo load
- local gravity
- thrust margin
- fuel level
- power reserve
- gyro status
- cockpit/remote control status
- damaged critical blocks

Jump readiness should consider:

- jump drive charge
- jump drive functional status
- power availability
- mass restrictions if relevant
- fuel and oxygen are not required for jumping, but should still be shown for survival readiness

Combat readiness should consider:

- turret online count
- ammo status
- power status
- damage status

Survival readiness should consider:

- oxygen tanks
- air vents
- fuel
- power
- pressurization

## Recommended Config

The script should eventually read settings from Programmable Block `CustomData`.

Example:

```ini
[Horizon Sentinel]
Main LCD=Horizon Sentinel LCD
Logo Screen=true
Ship Controller=
Use Control Seat Screens=true
Control Seat Screen Mode=Pilot
Enable Flight Assist=false
Broadcast Status=true
Broadcast Tag=HORIZON_SENTINEL_STATUS

[Groups]
Jump Drives=HS Jump Drives
Hydrogen Tanks=HS Hydrogen Tanks
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
Descent Max Speed=50
Ascend Target Speed=40
Battery Reactor Start Percent=25
Battery Reactor Stop Percent=100
Hydrogen Balance Difference Percent=5

[Flight Assist]
Ship Up Direction=Auto
Allow Gyro Override=false
Allow Thrust Override=false
Auto Stop In Zero Gravity=true
Require Manual Confirmation=true

[Automation]
Auto Reactor Charging=false
Auto Hydrogen Balancing=false
Hydrogen Balance Mode=Stockpile
Auto Ammo Balancing=false
```

## Suggested Extra Modules

### Thruster Status

Checks:

- thrusters online/offline
- damaged thrusters
- thrust by direction
- hydrogen thrusters missing fuel
- atmospheric/ion/hydrogen balance
- lifting thruster group
- cruising thruster group
- braking thruster group
- group thrust output/load
- missing or empty thruster groups

This is important for Takeoff Safety.

Suggested groups:

- `HS Thrusters`
- `HS Lifting Thrusters`
- `HS Cruising Thrusters`
- `HS Braking Thrusters`
- optional: `HS Left Thrusters`
- optional: `HS Right Thrusters`
- optional: `HS Up Thrusters`
- optional: `HS Down Thrusters`

Possible status:

- `LIFT READY`
- `CRUISE READY`
- `BRAKING READY`
- `LIFT DAMAGED`
- `BRAKING WEAK`
- `HYDROGEN THRUSTERS STARVED`
- `THRUSTER GROUP MISSING`

Display idea:

- grouped horizontal bars:
  - `Lift`
  - `Cruise`
  - `Brake`
- online count per group
- damaged count per group
- warning if braking thrust is too weak for descent

### Gyroscope Status

Checks:

- gyro count
- online/offline
- damaged gyros
- override enabled

Warning if no gyros are available.

### Reactor / Solar / Battery Automation

Purpose:

Use reactors as backup battery chargers and let solar panels provide normal maintenance power when available.

Suggested groups:

- `HS Reactors`
- `HS Solar Panels`
- batteries can be scanned automatically or grouped later

Behavior idea:

- solar panels stay online as normal maintenance power
- if battery charge drops below `25%`, turn reactors on
- reactors feed batteries until charge reaches `100%`
- when batteries reach full charge, turn reactors off again
- if ship is in danger, damaged, or power output is overloaded, keep reactors on and warn the pilot

Possible status:

- `SOLAR MAINTENANCE`
- `BATTERY LOW - REACTORS ON`
- `CHARGING BATTERIES`
- `BATTERIES FULL - REACTORS OFF`
- `REACTOR GROUP MISSING`
- `POWER AUTOMATION DISABLED`

Safety notes:

- This should be configurable and disabled by default until tested.
- The script should not fight the pilot if reactors are manually turned on for combat, emergency, or jump charging.
- A future config could include `Respect Manual Reactor On=true`.

### Hydrogen Tank Balancing

Purpose:

Keep hydrogen tanks at similar fill levels so the ship does not have two full tanks and two empty tanks when all tanks should share fuel evenly.

Example problem:

- 2 hydrogen tanks are full
- 2 hydrogen tanks are low
- script should try to equalize them

Possible behavior:

- read all hydrogen tank fill percentages
- find tanks above average and below average
- turn `Stockpile=true` on lower tanks so they pull hydrogen
- keep higher tanks available as the source
- when tank levels are close enough, turn stockpile off again

Example:

```text
Tank A: 100%
Tank B: 100%
Tank C: 20%
Tank D: 20%

Average: 60%
Action: stockpile C and D until all tanks are closer to same level.
```

Possible status:

- `HYDROGEN BALANCED`
- `BALANCING TANKS`
- `TANKS UNEVEN`
- `BALANCE DISABLED`
- `TANK DAMAGED`

Safety notes:

- Do not enable stockpile on all tanks at once.
- Do not balance during takeoff, combat, descent, or emergency low fuel unless explicitly allowed.
- Stop balancing if any tank is damaged or if hydrogen falls below a critical threshold.

### Turret Ammo Balancing

Purpose:

Keep turret ammunition balanced so one side or group of turrets does not run dry while other turrets still have plenty of ammo.

Example problem:

- bottom turrets fired heavily
- bottom turret ammo is low
- top/side turrets still have more ammo
- script should try to move ammo so all turrets have a similar amount

Suggested groups:

- `HS Turrets`
- optional: `HS Top Turrets`
- optional: `HS Bottom Turrets`
- optional: `HS Left Turrets`
- optional: `HS Right Turrets`
- optional: `HS Forward Turrets`
- optional: `HS Rear Turrets`

Possible behavior:

- scan turret inventories
- count ammo by ammo subtype
- calculate average ammo per turret or per turret group
- move ammo from high-ammo turrets/cargo to low-ammo turrets
- prefer refilling empty or nearly empty turrets first
- keep reserve ammo in cargo if configured

Possible status:

- `AMMO BALANCED`
- `BALANCING AMMO`
- `BOTTOM TURRETS LOW`
- `TURRET AMMO UNEVEN`
- `NO RESERVE AMMO`
- `AMMO BALANCE DISABLED`

Display idea:

- horizontal ammo bar
- group ammo bars:
  - `Top`
  - `Bottom`
  - `Left`
  - `Right`
  - `Forward`
  - `Rear`
- warning if any group is below threshold

Safety notes:

- This should be configurable and disabled by default until tested.
- Avoid moving ammo every tick; use slow balance intervals.
- Do not move ammo from actively firing turrets too aggressively.
- Modded weapons may use different ammo types, so matching ammo by turret inventory accepted items may need careful testing.

### Connector And Landing Gear Status

Checks:

- connectors locked/unlocked
- landing gear locked/unlocked
- merge blocks connected

Warn before takeoff if locked to station or dock.

### Door And Hangar Status

Checks:

- exterior doors open
- hangar doors open/closed
- airtight doors status

Useful for pressurization and launch checks.

### Antenna And Beacon Status

Checks:

- antenna online/offline
- beacon online/offline
- broadcasting enabled

Useful for large ships and survival recovery.

### Medical And Survival Systems

Checks:

- medical room online
- survival kit online
- oxygen generator online
- ice available

Useful for long-range ships.

### Production And Refinery Status

Checks:

- refineries online
- assemblers online
- production queue
- ingot/component shortages

This can be a later optional screen.

## LCD Recommendations

Minimum setup:

- **1 LCD** can work if the script rotates pages or uses commands like `next` and `prev`.
- A cockpit/control seat screen can also be used for the pilot's compact view.

Recommended setup:

- **3 LCDs** is the best practical bridge setup.
- **Control seat/cockpit screen** for immediate pilot warnings.

Suggested 3 LCD layout:

- **LCD 1: Overview**
  - master readiness
  - power
  - fuel
  - cargo
  - jump
  - most important warning
- **LCD 2: Flight Safety**
  - takeoff safety
  - planet entry warning
  - gravity
  - ship mass
  - lift margin
  - auto ascend/descend mode
- **LCD 3: Systems**
  - damage
  - pressure
  - turrets/ammo
  - vents
  - gyros/thrusters

Suggested control seat screen:

- **Pilot Compact**
  - master state
  - planet entry warning
  - auto ascend/descend mode
  - gravity
  - vertical speed
  - fuel
  - one-line critical alert

Control seat screen should be simple and readable while flying. It should not show long lists unless the pilot chooses a detailed page.

Luxury setup:

- **5 LCDs** if the ship has a large bridge.

Suggested 5 LCD layout:

- **LCD 1: Horizon Sentinel Overview**
- **LCD 2: Takeoff / Planet Entry**
- **LCD 3: Power / Fuel / Jump**
- **LCD 4: Combat / Ammo**
- **LCD 5: Pressure / Damage / Repair**

Best recommendation:

Start with **3 LCDs plus the control seat/cockpit screen**. One LCD is enough for testing, but three bridge LCDs plus the pilot screen makes Horizon Sentinel feel like a real ship system without requiring too much screen space.

## Planned Screen Tags

Horizon Sentinel should use clear screen tags in each LCD/control seat `CustomData`. This makes it work more like AutoLCD/Fancy Bar style: the script finds screens by tag and draws the correct page.

Recommended tag format:

```text
[HS:PageName]
```

### 1. Splashdown / Logo

Tag:

```text
[HS:Splash]
```

Target:

- Programmable Block screen
- optional LCD named for logo display

Purpose:

- Horizon Sentinel logo
- boot/status splash
- same style as the current PB screen logo

### 2. Pilot Power / Fuel / Oxygen

Tag:

```text
[HS:Pilot]
```

Target:

- control seat/cockpit screen

Purpose:

- power status
- fuel status
- oxygen level
- compact pilot warnings

Display style:

- Fancy Bar Display style
- horizontal compact bars
- simple icons
- readable while flying

### 3. Jump Drives

Tag:

```text
[HS:Jump]
```

Target:

- LCD
- Corner LCD if space is limited

Purpose:

- jump drive gauge chart
- online/offline count
- charging/ready status
- damaged jump drive warning

Display style:

- InfoGraphx-style gauge
- small status text under gauge

### 4. Cargo Status

Tag:

```text
[HS:Cargo]
```

Target:

- LCD

Purpose:

- cargo fill percentage
- cargo safety warning
- loaded/heavy warning
- storage group status

Display style:

- Fancy Bar Display style
- wide horizontal cargo bar

### 5. Planet Entry / Descent Safety

Tag:

```text
[HS:Descent]
```

Target:

- LCD

Purpose:

- planet entry warning
- descent safety
- too heavy for planet warning
- gravity, mass, lift margin, fuel reserve
- auto descend/align mode later

Display style:

- warning-first display
- large state text such as `SAFE`, `CAUTION`, `DO NOT ENTER`
- supporting bars/gauges

### 6. Turrets / Ammo

Tag:

```text
[HS:Combat]
```

Target:

- control seat/cockpit screen
- optional dedicated combat LCD

Purpose:

- turret online count
- ammo amount
- damaged/offline turret warning

Display style:

- Fancy Bar Display style
- horizontal ammo bar
- compact pilot combat view

### 7. Damage / Air Leak Warning

Tag:

```text
[HS:Damage]
```

Target:

- LCD

Purpose:

- damaged block count
- critical system damage
- air vent leaking warning
- leak/pressure risk summary

Display style:

- warning list
- critical systems first

### 8. Pressurization Corner LCD

Tag:

```text
[HS:Pressure]
```

Target:

- Corner LCD Top
- small dedicated pressure/leak display

Purpose:

- pressurization status
- oxygen level
- air vent leak warning
- depressurize mode warning

Display style:

- compact bar/status indicator
- big warning if leaking or not pressurized

## Screen Ideas

### Main Overview Screen

- logo at top or first boot
- master state
- four big bars:
  - Power
  - Fuel
  - Cargo
  - Jump
- small warnings list

### Flight Assist Screen

- current mode:
  - `MANUAL`
  - `AUTO ASCEND`
  - `AUTO DESCEND`
  - `ALIGNING`
  - `ABORT`
- planet entry safety
- gravity strength
- vertical speed
- mass and lift margin
- fuel reserve
- alignment status
- broadcast status

### Takeoff Screen

- mass
- cargo percentage
- gravity
- thrust margin
- takeoff decision

### Planet Entry Screen

- `SAFE TO ENTER`
- `DO NOT ENTER GRAVITY`
- expected lift status
- fuel reserve
- cargo warning
- landing/takeoff confidence

### Combat Screen

- turret status
- ammo bar
- damaged weapons

### Survival Screen

- oxygen
- pressure
- fuel
- medical/survival systems

### Damage Screen

- critical damaged systems
- damaged block count
- top repair list

## Development Phases

### Phase 1: Logo Screen

Status: started.

Current work:

- Horizon Sentinel LCD logo
- shield/crosshair mark
- animated red pulse
- ocean sunrise background

### Phase 2: Data Scanner

Goal:

Find and classify blocks:

- cargo blocks
- ship controllers
- batteries/reactors
- jump drives
- hydrogen tanks
- oxygen tanks
- turrets
- air vents
- thrusters
- gyros

### Phase 3: Safety Calculations

Goal:

Calculate:

- cargo percentage
- total fuel percentage
- total battery percentage
- jump charge percentage
- pressure percentage
- ammo summary
- damaged critical blocks

### Phase 4: Takeoff Safety

Goal:

Estimate whether the ship can lift in current gravity with current cargo.

This is the hardest module and should be tested carefully.

### Phase 5: Screens And Alerts

Goal:

Draw useful screens:

- overview
- takeoff
- jump
- power/fuel
- combat
- pressure
- damage

### Phase 6: Commands

Possible PB arguments:

- `logo`
- `overview`
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
- `ascend`
- `descend`
- `align`
- `broadcast`
- `abort`

### Phase 7: Flight Assist

Goal:

Add careful optional automation:

- auto ascend
- auto descend
- descent alignment
- planet entry warning
- broadcast status

This phase should only start after the monitoring and safety calculations are reliable.

## Notes For Future Scripts

When studying uploaded scripts, check for:

- how they find blocks
- how they draw gauges
- how they parse `CustomData`
- how they handle missing blocks
- how they calculate mass/thrust
- how they reduce instruction count
- how they cache block lists
- how they handle multiple LCDs

Horizon Sentinel should stay modular, so each safety module can be improved without rewriting the whole script.
