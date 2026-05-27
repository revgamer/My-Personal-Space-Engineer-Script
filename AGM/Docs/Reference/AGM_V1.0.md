# AutoGrid Manager V1.0

Project: **AutoGrid Manager**
Short name: **AGM**
Target version: **1.0**
Game: **Space Engineers**
Script type: **Programmable Block script family**
Author: **RevGamer**

---

## 1. Vision

AGM V1.0 has moved away from one giant all-in-one Programmable Block and is now a small family of focused scripts.

The goal is simple:

- Keep each PB below the Space Engineers character limit.
- Keep each PB below the instruction limit.
- Make debugging easier.
- Let dashboards keep updating even while logistics or production is doing heavy work.
- Keep the in-game setup readable with clear tags, groups, and Custom Data.

AGM should feel like one system, but internally it should be split into modules.

---

## 2. Core-Controlled Module System

### AGM Core

AGM Core is the required central controller for the V1.0 system.

Core does not do every job itself. Instead, Core owns the shared rules and coordinates AGM modules.

Responsibilities:

- Common tag rules.
- Shared config conventions.
- Version/status screen.
- Module registry.
- Module enable/disable flags.
- Shared warnings.
- Global pause/resume.
- Global docked-grid policy.
- Standard command names.
- Standard screen/module tags.
- Future shared block discovery cache.

AGM modules should be able to run in a degraded standalone mode for debugging, but the intended V1.0 design is:

```text
AGM Core
  -> AGM Power
  -> AGM Logistics
  -> AGM Production
  -> future AGM modules
```

Core is the place the player should look first when something is wrong.

### Module Contract

Each AGM module should:

- Have its own PB tag.
- Read Core settings from PB Custom Data or a shared Core data block.
- Write a short status report.
- Respect Core pause/enable flags.
- Never assume another module is healthy.
- Fail safely if Core is missing.

Core should not spam-run other PBs every tick. Modules can run on their own update loop and read shared Core state.

### AGM Power

Power monitoring and power state publishing.

Responsibilities:

- Battery, reactor, solar, wind, and hydrogen engine group scanning.
- Published `[PowerState]` for Core dashboards.
- PB front boot/status screen.
- Future power automation policies.

Wall LCD dashboards are rendered by Core.

### AGM Logistics

Cargo assignment and item sorting.

Responsibilities:

- Auto-rename eligible cargo containers.
- Assign new typed cargo when storage fills.
- Move wrong items into the correct typed cargo.
- Move assembler/refinery outputs into storage.
- Maintain sorter status and warnings.

This PB is the replacement for the current sorter logic.

### AGM Production

Assembler and refinery automation.

Responsibilities:

- V1 monitor dashboard state for assemblers and refineries.
- Published `[ProductionState]` for Core dashboards.
- Autocrafting component quotas.
- Assembler queue priority.
- Basic refinery priority.

This should be separate from Logistics because production queues and item movement are both instruction-heavy.

---

## 3. Core PB

Recommended Core PB name:

```text
PB AutoGrid Manager Core {AGM-Core}
```

Recommended Core responsibilities in-game:

- Show whether modules are online.
- Show latest warning from each module.
- Hold shared settings.
- Let the player pause modules.
- Let the player set global dock/protection rules.

Core PB Custom Data:

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

[Modules]
power=PB AutoGrid Manager Power {AGM-Power}
logistics=PB AutoGrid Manager Logistics {AGM-Logistics}
production=PB AutoGrid Manager Production {AGM-Production}
```

Core should eventually write a compact machine-readable state to its own Custom Data or Storage, for example:

```ini
[CoreState]
core_online=true
power_enabled=true
logistics_enabled=true
production_enabled=true
global_pause=false
last_warning=
```

---

## 4. Global Tags

### AGM-managed screen

```text
[AGM-S]
```

Used on LCDs/cockpit surfaces that AGM should draw on.

### Logistics PB

```text
{AGM-Logistics}
```

Recommended name:

```text
PB AutoGrid Manager Logistics {AGM-Logistics}
```

### Power PB

```text
{AGM-Power}
```

Recommended name:

```text
PB AutoGrid Manager Power {AGM-Power}
```

### Production PB

```text
{AGM-Production}
```

Recommended name:

```text
PB AutoGrid Manager Production {AGM-Production}
```

---

## 5. Module Communication

Space Engineers PB-to-PB communication is limited, so V1.0 should keep module communication simple.

Recommended approach:

- Core stores shared config/state in Core PB Custom Data or Storage.
- Modules find Core by `{AGM-Core}`.
- Modules read Core config on a slow interval or when `reload` is run.
- Modules write their own status to their own PB Custom Data or Echo.
- Core reads module PB Custom Data to build wall dashboards.

Do not make Core manually run every module every tick. That would create more instruction problems.

Preferred runtime model:

```text
AGM Core       Update100 or Update10
AGM Power      Update10/Update100 staged
AGM Logistics  Update10/Update100 staged
AGM Production Update100 staged
```

---

## 6. AGM Logistics Design

AGM Logistics should be opinionated and predictable.

It should not try to copy every IIM feature. It should focus on:

- typed cargo assignment,
- wrong-item cleanup,
- overflow assignment,
- cargo dashboard,
- clear warnings.

---

## 7. Cargo Naming System

AGM Logistics should rename cargo containers with numbered type tags.

### Large Grid Cargo / Bulk Cargo

Large storage should be used for high-volume materials:

```text
{Ore 1}
{Ore 2}
{Ingot 1}
{Ingot 2}
{Component 1}
{Component 2}
```

Rules:

- Ore, Ingot, and Component storage should use Large Grid Cargo Containers or Bulk Cargo Containers.
- When `{Ore 1}` becomes full, AGM can assign an empty eligible container as `{Ore 2}`.
- Same behavior for Ingots and Components.
- Containers should be assigned in order.
- AGM should only auto-rename empty untyped containers unless the container already has a known AGM type tag.

### Small Cargo

Small storage should be used for lower-volume categories:

```text
{Ammo 1}
{Tools 1}
{Bottle 1}
```

Rules:

- Ammo, Tools, and Bottles should use Small Cargo Containers.
- AGM should not assign large containers for Ammo/Tools/Bottles unless explicitly configured later.

---

## 8. Protected Cargo Tags

AGM Logistics should never auto-rename or drain containers with these tags:

```text
{Locked}
{Manual}
{Hidden}
```

Meaning:

- `{Locked}`: do not sort from or into this container.
- `{Manual}`: user-controlled container; leave it alone.
- `{Hidden}`: exclude from counting and dashboard if possible.

Optional later:

```text
{Special}
```

Special containers would behave like IIM loadout containers, but this should not be part of the first Logistics pass.

---

## 9. Item Categories

AGM Logistics should classify items by Space Engineers item type:

| AGM category | SE type |
|---|---|
| Ore | Ore |
| Ingot | Ingot |
| Component | Component |
| Ammo | AmmoMagazine |
| Tools | PhysicalGunObject |
| Bottle | OxygenContainerObject / GasContainerObject |

Consumables, datapads, packages, and modded objects can be added later.

---

## 10. Auto Assignment Rules

AGM Logistics should assign new containers when:

- A typed category exists.
- All existing containers for that category are full.
- There is an eligible empty untyped container.
- The container size matches the category rules.

Example:

```text
{Ore 1} is full
Empty Large Cargo Container exists
AGM renames it to {Ore 2}
Ore can now flow into {Ore 2}
```

If no eligible container exists, AGM should warn:

```text
Ore storage full - no empty Large/Bulk Cargo available
```

---

## 11. Sorting Rules

AGM Logistics should move items from wrong places to correct places.

Examples:

```text
Component inside {Ingot 1}
Move to {Component 1}
```

```text
Ore inside assembler output or generic cargo
Move to {Ore 1} or {Ore 2}
```

```text
Ammo inside {Component 1}
Move to {Ammo 1}
```

Sorter sources:

- Cargo containers.
- Assembler output inventories.
- Refinery output inventories.
- Basic assembler/survival kit output inventories.

Sorter should not drain:

- Refinery input.
- Assembler input if it is needed for production.
- O2/H2 generator ice inventory.
- Reactors.
- Locked/manual/hidden cargo.

---

## 12. Logistics PB Custom Data

Recommended first V1 config:

```ini
[Logistics]
enabled=true
auto_assign=true
include_docked_grids=false
max_moves_per_run=2
full_percent=98

[CargoTypes]
large=Ore,Ingot,Component
small=Ammo,Tools,Bottle

[CargoBlocks]
large_keywords=Large Cargo Container,Bulk Cargo Container
small_keywords=Small Cargo Container

[Protection]
locked={Locked}
manual={Manual}
hidden={Hidden}
```

Notes:

- `max_moves_per_run=2` keeps the PB safer under instruction limits.
- `full_percent=98` matches the style of IIM's "nearly full" behavior.
- `include_docked_grids=false` should be the default to avoid stealing from ships.

---

## 13. Logistics Dashboard

LCD Custom Data:

```text
LogisticsDashboard
```

Alternative shorter command:

```text
Logistics
```

Suggested layout:

```text
AGM LOGISTICS

Ore          1/2 active   98%
Ingot        1/1 active   42%
Component    2/3 active   70%
Ammo         1/1 active   12%
Tools        1/1 active    5%
Bottle       1/1 active   22%

Moved this tick: 2
Last move: SteelPlate -> {Component 1}
Warning: none
```

Dashboard should show:

- active containers per type,
- total fill per type,
- fallback/untyped cargo count,
- locked/manual/hidden count,
- moves this tick,
- last moved item,
- last source and destination,
- current warning.

---

## 14. Core Wall LCD Commands

All wall LCDs use `[AGM-S]` in the block name. Core reads one command from Custom Data and draws the screen.

Example LCD commands:

```text
CoreDashboard
PowerDashboard
LogisticsDashboard
SorterDashboard
ProductionDashboard
InventoryStock
OreStock
IngotStock
ComponentStock
AmmoStock
ToolStock
BottleStock
Autocrafting
FuelLifeSupport
LogisticsDashboard
```

---

## 15. Fuel & Life Support Groups

LCD Custom Data:

```text
FuelLifeSupport
```

PB Custom Data:

```ini
[LifeSupport:Base]
hydrogen=G:Hydrogen Tanks
oxygen=G:Oxygen Tanks
generators=G:O2 Generators
include_ungrouped=false
```

Interior vents should opt in manually.

Vent name:

```text
Base Air Vent [AGM-S]
```

Vent Custom Data:

```text
InteriorVent
```

AGM Core can tag monitored vents as:

```text
[Pressurized]
[Leaking]
```

---

## 16. Power Groups

LCD Custom Data:

```text
PowerDashboard
```

PB Custom Data:

```ini
[power:Base]
batteries=G:Base Batteries
reactors=G:Base Reactors
solar=G:Base Solar
wind=
hydrogen=G:Base Hydrogen Engines
other=
include_ungrouped=false
```

---

## 17. V1.0 Development Order

Recommended implementation order:

1. Create `AGM_Core.cs`.
2. Add Core PB Custom Data config and module registry.
3. Add Core status/dashboard screen.
4. Create `AGM_Power.cs`.
5. Make Power publish `[PowerState]`.
6. Create `AGM_Logistics.cs`.
7. Implement cargo detection and typed cargo dashboard state.
8. Implement auto assignment for empty cargo.
9. Implement sorter moves for wrong items.
10. Add warnings and last-action dashboard state.
11. Create `AGM_Production.cs`.
12. Move autocrafting into Production.
13. Keep all wall LCD rendering inside Core.

This order keeps the base usable while each module is built.

---

## 18. First AGM Logistics Acceptance Test

Setup:

```text
Large Cargo Container {Ore 1}
Large Cargo Container {Ingot 1}
Large Cargo Container {Component 1}
Small Cargo Container {Ammo 1}
Small Cargo Container {Tools 1}
Small Cargo Container {Bottle 1}
Empty Large Cargo Container
Empty Small Cargo Container
```

Test cases:

- Put ore in `{Component 1}`.
- AGM moves it to `{Ore 1}`.
- Put component in `{Ingot 1}`.
- AGM moves it to `{Component 1}`.
- Fill `{Ore 1}`.
- AGM renames the empty large container to `{Ore 2}`.
- Put ammo in `{Ore 1}`.
- AGM moves it to `{Ammo 1}`.
- Fill `{Ammo 1}`.
- AGM renames the empty small container to `{Ammo 2}`.

Pass condition:

- No wrong item remains in a typed cargo when valid destination space exists.
- New cargo is assigned only when needed.
- Locked/manual/hidden cargo is untouched.
- Dashboard reports the latest move and warnings clearly.

---

## 19. Design Rule

AGM should be strict by default:

```text
If a block is not clearly opted in, do not control it.
```

This protects ships, manual cargo, special setups, and docked grids.
