# AutoGrid Manager Roadmap

**AutoGrid Manager (AGM)** is a modular Space Engineers programmable block system by **RevGamer**.

The goal of AGM is to turn a base, station, or ship into a managed grid system with dashboards, automation, alerts, and optional add-on systems.

AGM should stay modular so players can choose what they want:

- **AGM Core** â€” base/grid management
- **AGM FleetNet** â€” optional fleet status monitoring
- **AGM ADS** â€” optional automated defence system
- **AGM Relay** â€” on hold / future research

---

# AGM Script Family

## AGM Core

**AutoGrid Manager - Core System**

The main AGM script. This handles the normal base or ship management features.

Core areas:

- Power monitoring
- Cargo logistics
- Production monitoring
- Autocrafting support
- Fuel and life-support monitoring
- Stock dashboards
- Alert dashboard
- Warning light control
- Reactor refuel monitoring
- Battery and reactor automation

AGM Core should remain the main script and should not become too combat-heavy.

---

## AGM FleetNet

**AutoGrid Manager - Fleet Broadcast**

Optional companion script for players who use multiple ships, miners, cargo craft, drones, or patrol ships.

FleetNet can report:

- Grid name
- Grid role
- Position
- Distance from base
- Direction / heading
- Power level
- Cargo level
- Hydrogen level
- Oxygen level
- Ammo level, if enabled
- Status
- Last seen time

FleetNet should be useful for miners, cargo ships, patrol ships, drones, barges, and outposts.

---

## AGM ADS

**AutoGrid Manager - Automated Defence System**

Optional companion script for combat bases, PvP servers, or defended stations.

ADS can manage:

- Enemy / unknown contact detection
- Threat levels
- Turret activation
- Shield activation
- Defence warning lights
- Siren / sound block alerts
- Ammo readiness checks
- Defence dashboard

ADS should be separate from AGM Core so peaceful players do not need to install combat automation.

---

## AGM Relay

**AutoGrid Manager - Relay Node**

Status: **On hold / future research**

AGM Relay was planned as a message-forwarding system between bases, outposts, satellites, and fleet nodes.

However, for stealth gameplay, relay communication needs more testing because normal antenna / IGC communication can still expose a signal to NPCs or other players.

AGM Relay may return later if a reliable stealth-friendly method is found.

Possible future relay ideas:

- Short-range local relay
- Pulse transmission relay
- SecureLink pairing key
- Message authentication
- Hop count loop protection
- Courier drone / data mule support

For now, AGM Relay is not part of the main development path.

---

# AGM Core Roadmap

## Version 1.1 - Alert System

Planned features:

- AlertDashboard
- Warning light tags
- Battery low / full light states
- Cargo full warnings
- Hydrogen low warnings
- Oxygen low warnings
- Uranium low warnings

---

## AlertDashboard

The AlertDashboard should provide one central warning screen for the whole grid.

Example LCD:

```text
AGM ALERTS

Battery:        LOW
Cargo:          WARNING
Hydrogen:       OK
Oxygen:         OK
Uranium:        LOW
Production:     OK

Status: WARNING
```

Suggested LCD commands:

```text
AlertDashboard
WarningDashboard
AGM-Alerts
```

---

## Warning Light Tags

AGM should control tagged lights based on system status.

Example block names:

```text
[AGM-LIGHT] Battery Low
[AGM-LIGHT] Reactor Charging
[AGM-LIGHT] Power OK
[AGM-LIGHT] Cargo Full
[AGM-LIGHT] Hydrogen Low
[AGM-LIGHT] Oxygen Low
[AGM-LIGHT] Uranium Low
[AGM-LIGHT] Production Warning
[AGM-LIGHT] General Alert
```

Suggested colours:

| State | Colour |
|---|---|
| Normal / clear | Green |
| Warning | Amber |
| Critical | Red |
| Reactor charging | Amber |
| Battery full | Green |
| Battery low | Red |

Suggested config:

```ini
[Alerts]
enabled=true
warning_lights=true
battery_low_percent=25
battery_full_percent=100
cargo_warning_percent=90
cargo_full_percent=98
hydrogen_low_percent=20
oxygen_low_percent=20
uranium_low_kg=5
```

---

# Version 1.2 - Power Dashboard v2

Planned features:

- Power Dashboard v2
- Reactor Refuel page
- Battery auto-reactor charging
- Reactor safety config
- Power control warning lights

---

## Power Dashboard Page 1

Main power overview.

Example LCD:

```text
POWER STATUS

Battery:        64%
Stored:         48 MWh / 75 MWh
Input:          12 MW
Output:         8 MW

Reactors:       2 online / 4 total
Solar:          12
Wind:           8
Hydrogen:       2

State:          STABLE
```

Suggested LCD command:

```text
PowerDashboard 1
```

---

## Power Dashboard Page 2 - Reactor Refuel

Reactor refuel should start as **monitor-only**.

Example LCD:

```text
REACTOR REFUEL

Reactors:              4
Uranium in reactors:   12.5 kg
Uranium stock:         350 kg
Lowest reactor:        Reactor 3 - 0.2 kg

Status: REFUEL NEEDED
```

Suggested LCD commands:

```text
PowerDashboard 2
ReactorRefuel
AGM-Reactor
```

Suggested config:

```ini
[ReactorRefuel]
enabled=true
min_uranium_per_reactor=2
target_uranium_per_reactor=10
uranium_low_warning_kg=5
auto_refuel=false
```

Recommended default:

```ini
auto_refuel=false
```

Automatic uranium transfer can be risky if configured incorrectly, so reactor refuel should begin as a warning/dashboard feature first.

---

## Power Dashboard Page 3 - Battery Control

Battery control should allow reactors to automatically charge batteries when power is low.

Example LCD:

```text
BATTERY CONTROL

Mode:           AUTO REACTOR CHARGE
Battery:        18%
Low trigger:    25%
Full trigger:   100%

Reactors:       FORCED ON
Light state:    RED

Status: BATTERY LOW
```

Suggested LCD commands:

```text
PowerDashboard 3
BatteryControl
AGM-Battery
```

Suggested config:

```ini
[PowerControl]
enabled=true
auto_reactor_charge=true
battery_low_percent=25
battery_full_percent=100
control_reactors=G:Base Reactors
control_batteries=G:Base Batteries
turn_reactors_off_when_full=true
amber_while_charging=true
minimum_reactors_online=0
never_turn_off_reactors_if_output_above_percent=80
```

---

## Battery / Reactor Automation Logic

If battery level is below the configured low percentage:

```text
- Turn selected reactors ON
- Set battery warning lights RED
- Show status: BATTERY LOW
```

If reactors are charging batteries:

```text
- Keep selected reactors ON
- Set reactor charging lights AMBER
- Show status: REACTOR CHARGING
```

If battery level reaches full:

```text
- Check reactor safety rules
- If safe, turn selected reactors OFF
- Set power OK lights GREEN
- Show status: BATTERY FULL
```

Safety rule:

```text
Do not turn reactors off if the base is under heavy power load.
```

This avoids blackouts when the base still needs reactor power.

---

# Version 1.3 - Production Dashboard v2

Planned features:

- Production details page
- Assembler current job
- Refinery current ore
- Missing resource warnings
- Blocked assembler warnings
- Blocked refinery warnings

---

## Production Dashboard Page 1

Main production overview.

Example LCD:

```text
PRODUCTION STATUS

State:          ACTIVE
Mode:           ACTIVE
Assemblers:     3 / 4 producing
Queued:         4 machines
Refineries:     2 / 3 producing
Autocraft:      2 queued this run

Status: ONLINE
```

Suggested LCD command:

```text
ProductionDashboard 1
```

---

## Production Dashboard Page 2

Machine details.

Example LCD:

```text
PRODUCTION DETAILS

Assembler 1:    Steel Plate
Assembler 2:    Motor
Assembler 3:    Computer
Assembler 4:    IDLE

Refinery 1:     Iron Ore
Refinery 2:     Cobalt Ore
Refinery 3:     IDLE

Last queued:    500 Steel Plate
Status: ACTIVE
```

Suggested LCD commands:

```text
ProductionDashboard 2
ProductionDetails
AGM-Production2
```

---

## Production Warnings

AGM should detect production bottlenecks and explain why production is blocked.

Example LCD:

```text
PRODUCTION WARNINGS

Motor:          Cobalt ingots low
Computer:       Silicon ingots low
Steel Plate:    Assembler busy
Metal Grid:     Cobalt ingots low
Reactor Comp:   Silver ingots low

Status: BOTTLENECK FOUND
```

Suggested config:

```ini
[ProductionV2]
enabled=true
show_machine_details=true
show_current_blueprint=true
show_refinery_input=true
show_missing_resources=true
show_blocked_assemblers=true
show_blocked_refineries=true
```

---

# AGM FleetNet Roadmap

AGM FleetNet is an optional companion script for tracking friendly ships and remote grids.

---

## Fleet Broadcast Data

A ship can broadcast:

```text
Grid name
Grid role
Position
Velocity
Direction
Distance from base
Battery percentage
Cargo percentage
Hydrogen percentage
Oxygen percentage
Ammo percentage
Current status
Last seen time
```

---

## Fleet Roles

Supported roles could include:

```text
Miner
Cargo
Scout
Patrol
Defence
Drone
Barge
Base
Station
Carrier
Factory
```

Role matters because different grid types need different monitoring.

Example miner config:

```ini
[FleetBroadcast]
role=Miner
track_cargo=true
track_ammo=false
track_hydrogen=true
track_power=true
track_position=true
```

Example patrol ship config:

```ini
[FleetBroadcast]
role=Patrol
track_cargo=false
track_ammo=true
track_hydrogen=true
track_power=true
track_position=true
track_defence=true
```

---

## Fleet Dashboard

Example LCD:

```text
AGM FLEETNET

GroundHog-1
Role:      Miner
Range:     3.2 km
Heading:   North-East
Power:     84%
Cargo:     62%
Hydrogen:  91%
Status:    ACTIVE

SpaceHog-2
Role:      Space Miner
Range:     18.4 km
Heading:   Up / Forward
Power:     41%
Cargo:     92%
Hydrogen:  34%
Status:    RETURN SOON
```

Suggested LCD commands:

```text
FleetDashboard
FleetMiners
FleetCargo
FleetCombat
FleetAll
```

---

## Lost Contact Warning

Example:

```text
LOST CONTACT

Grid:       SpaceHog-2
Last seen:  8 minutes ago
Status:     SIGNAL LOST
```

Suggested config:

```ini
[FleetNet]
lost_contact_seconds=120
critical_lost_seconds=300
```

---

## Return Recommendation

FleetNet can suggest when a ship should return.

Example:

```text
RETURN RECOMMENDED

Ship:       SpaceHog-2
Cargo:      94%
Hydrogen:   28%
Distance:   12 km

Reason: Cargo nearly full and hydrogen low.
```

FleetNet does not need to control the ship. It can simply warn the player.

---

# AGM ADS Roadmap

AGM ADS is an optional defence script.

It should remain separate from AGM Core so players can choose whether they want combat automation.

---

## Defence System Features

Planned features:

- Enemy contact detection
- Unknown contact detection
- Threat level monitoring
- Turret activation
- Shield activation
- Warning lights
- Siren / sound block alerts
- Ammo readiness dashboard
- Defence power readiness
- Safe list / friendly list

---

## Detection Sources

Possible detection sources:

```text
Camera raycast
Turret target detection
Sensor blocks
Antenna / beacon signals
WeaponCore radar, if available
Manual alert command
```

The exact detection system depends on what vanilla Space Engineers and installed mods expose to programmable blocks.

---

## Defence Modes

```text
Standby
- Turrets off
- Shields idle
- Green lights

Watch
- Detection active
- Turrets idle
- Shields ready
- Amber lights

Defence Active
- Turrets online
- Shields online
- Red lights
- Siren active

Lockdown
- Full defence active
- Shields online
- Turrets online
- Red alert lights
- Broadcast warning to FleetNet
```

---

## Threat Levels

```text
Threat Level 0: Clear
Threat Level 1: Unknown contact
Threat Level 2: Armed contact
Threat Level 3: Hostile contact
Threat Level 4: Under attack
```

Example LCD:

```text
AGM DEFENCE SYSTEM

Status:       CONTACT DETECTED
Threat:       ENEMY GRID
Range:        2.4 km
Bearing:      041Â°
Turrets:      ONLINE
Shields:      RAISING
Ammo:         82%
Mode:         DEFENCE ACTIVE
```

---

## Defence Tags

Example block names:

```text
[AGM-ADS] Turret
[AGM-ADS] Shield
[AGM-ADS] Radar
[AGM-ADS] Light
[AGM-ADS] Siren
[AGM-ADS] Camera
```

Alternative CustomData:

```ini
[AGM-ADS]
role=turret
```

```ini
[AGM-ADS]
role=shield
```

---

## Safe List

Suggested config:

```ini
[FriendlyGrids]
GroundHog
SpaceHog
RMC Base
RAC Command
RevGamer Barge
```

Unknown contacts can be treated as warnings.

Confirmed hostile contacts can trigger defence mode.

---

# AGM SecureLink

AGM SecureLink is a proposed security layer for AGM FleetNet, ADS, and future relay systems.

It does not hide antenna signals, but it can stop untrusted messages from being accepted.

---

## Purpose

SecureLink can:

- Pair trusted AGM grids using a shared secret key
- Reject untrusted FleetNet messages
- Reject fake defence alerts
- Reject fake relay messages
- Add message IDs to prevent replay loops
- Add hop count to prevent relay loops
- Keep AGM dashboards clean from unknown broadcasts

---

## Example Config

```ini
[SecureLink]
enabled=true
network_id=RMC-NET
pairing_key=ChangeThisKey
require_auth=true
ignore_untrusted=true
```

Example message format:

```text
AGM|RMC-NET|MSG-004291|SpaceHog-2|Miner|Cargo:62|Power:84|H2:91|AUTH:8F2A91
```

The receiving PB checks the network ID and auth code before trusting the message.

---

# AGM Relay - On Hold

AGM Relay is currently on hold.

Reason:

- Relay communication still relies on antenna / IGC behaviour.
- Long-range broadcasting may expose the relay to NPCs or other players.
- Short-range 2.5 km relay chains would require too many relay nodes over long distances.
- Laser antenna is closer to a true private link, but it can be buggy or inconvenient.
- Courier/data mule ideas may be more stealth-friendly.

Possible future idea:

```text
AGM CourierLink
```

Instead of constant antenna communication, a courier drone or ship physically travels between grids and transfers stored AGM data when close enough.

This would allow stealth-friendly data movement without permanent long-range broadcasting.

---

# Recommended Development Order

## Phase 1 - AGM Core v1.1

```text
AlertDashboard
Warning light tags
Battery low / full light states
Cargo full warnings
Hydrogen / oxygen warnings
Uranium low warning
```

---

## Phase 2 - AGM Core v1.2

```text
Power Dashboard v2
Reactor Refuel page
Battery auto-reactor charging
Reactor safety config
Power control warning lights
```

---

## Phase 3 - AGM Core v1.3

```text
Production details page
Assembler current job
Refinery current ore
Missing resource warnings
Blocked assembler/refinery warnings
```

---

## Phase 4 - AGM FleetNet

```text
Friendly grid broadcast
Fleet dashboard
Range and direction tracking
Power / cargo / fuel / ammo status
Lost-contact warning
Return-to-base recommendation
SecureLink support
```

---

## Phase 5 - AGM ADS

```text
Threat detection
Turret activation
Shield activation
Defence lights
Siren alerts
Ammo readiness
Safe list
Threat dashboard
SecureLink support
```

---

## Phase 6 - Future Research

```text
AGM Relay
AGM SecureLink improvements
AGM CourierLink
Pulse relay mode
Long-range stealth communication research
```

---

# Final AGM Family

```text
AGM Core
Base management, power, cargo, production, fuel, alerts

AGM FleetNet
Friendly grid tracking and fleet status

AGM ADS
Automated defence and threat response

AGM SecureLink
Shared-key trust layer for AGM messages

AGM Relay
On hold / future research

AGM CourierLink
Possible future stealth data courier system
```

This modular design keeps AGM lightweight for normal players, while allowing advanced players to build a full command and defence network if they want it.

