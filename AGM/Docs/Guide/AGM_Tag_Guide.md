# AGM v1.4 — Block Tag Reference Guide

**Script:** AutoGrid Manager v1.4
**Author:** RevGamer

All configuration is done via block names and Custom Data. No coding required.

---

## LCD / Screen Tags

### `[AGM-S]` — Dashboard Screen
Put in the **block name** of any LCD or text surface provider.

Add a dashboard command in the **Custom Data** to control what is shown:

| Custom Data | Display |
|---|---|
| `CoreDashboard` | System overview — power, logistics, production, alerts |
| `AlertDashboard` | Alert status overview |
| `WarningDashboard` | Warning details |
| `PowerDashboard page=1` | Power overview |
| `ReactorRefuel` | Reactor uranium status |
| `BatteryControl` | Battery/reactor automation |
| `LogisticsDashboard` | Sorting status |
| `ProductionDashboard page=1` | Production overview |
| `ProductionDetails` | Assembler details — scroll, sorted Adv then Basic A-Z |
| `ProductionWarnings` | Refinery details — sorted A-Z |
| `InventoryStock page=1` | All items stock |
| `OreStock page=1` | Ore stock |
| `IngotStock page=1` | Ingot stock |
| `ComponentStock page=1` | Component stock |
| `AmmoStock page=1` | Ammo stock |
| `ToolStock page=1` | Tool stock |
| `BottleStock page=1` | Bottle stock |
| `Autocrafting page=1` | Autocrafting quotas |
| `FuelLifeSupport` | H2/O2 tanks and life support |
| `LifeSupport` | Life support only |

Multi-page: add multiple LCDs with `page=1`, `page=2`, `page=3` etc.

**Important:** Never add `[AGM-S]` to a block that also has `[AGM-LIGHT]` in its Custom Data.

---

### `[AGM-LIGHT]` — Alert Light / Corner LCD
Put in the **Custom Data** of any light block or corner LCD.

```ini
[AGM-LIGHT]
watch=Battery
```

| watch= value | Monitors |
|---|---|
| `Battery` | Battery charge level |
| `Cargo` | Cargo fill level |
| `Hydrogen` | Hydrogen tank level |
| `Oxygen` | Oxygen tank level |
| `Uranium` | Uranium stock |
| `Production` | Production alert |
| `Charging` | Reactor charging state |
| `Power OK` | Power stable indicator |
| *(leave blank)* | Overall AGM alert level |

Alert states: **OK** = green, **Warning** = amber, **Critical** = red/blinking.

Corner LCDs show the topic name large (e.g. `BATTERY`) with status below and a coloured border. Redrawn every tick — never flickers.

**Do NOT add `[AGM-S]` to these blocks.** They are managed separately.

---

## Cargo Container Tags

Put these in the **block name** to tell AGM where to sort items.

| Tag | Items sorted into it |
|---|---|
| `{Ore 1}` | Ores |
| `{Ingot 1}` | Ingots |
| `{Component 1}` | Components |
| `{Ammo 1}` | Ammo |
| `{Tool 1}` | Tools |
| `{Bottle 1}` | Bottles |

**Number = priority.** Lower number fills first. When a container hits 98% full, sorting spills to the next number.

Multiple containers of the same type:
```
Large Cargo Container {Ore 1}
Large Cargo Container {Ore 2}
Large Cargo Container {Ore 3}
```

If `auto_assign=true` in PB Custom Data, AGM automatically assigns untagged containers when needed.

### Protection Tags (block name)

| Tag | Effect |
|---|---|
| `[No Sorting]` | AGM ignores the entire grid connected via this connector |
| `{Locked}` | Container never used as a sort destination |
| `{Manual}` | Assembler/refinery excluded from all production management |
| `{Hidden}` | Block excluded from all AGM scanning and counting |

---

## Assembler Custom Data Tag

Put in the **Custom Data** of an assembler to control how AGM uses it.

```ini
[AGM]
autocraft=true
disassemble=true
```

| Key | Default | Effect |
|---|---|---|
| `autocraft=true` | true | AGM queues autocrafting jobs to this assembler |
| `autocraft=false` | — | AGM never queues crafting to this assembler |
| `disassemble=true` | false | AGM uses this assembler for auto-disassembly |
| `disassemble=false` | — | AGM never uses this assembler for disassembly |

**No `[AGM]` tag** = assembler is used for autocrafting by default (same as `autocraft=true`).

**Examples:**

Dedicated craft assembler (default, no tag needed):
```
Assembler [M]
```

Dedicated disassembly assembler:
```ini
[AGM]
autocraft=false
disassemble=true
```

Assembler that does both:
```ini
[AGM]
autocraft=true
disassemble=true
```

Assembler details page shows:
- `[M]` = master (not cooperative mode)
- `[D]` = dedicated disassembly
- `C+D` = does both craft and disassembly

---

## PB Custom Data — Production Section

```ini
[Production]
monitor_only=false
autocraft_components=true
auto_disassemble=false
sort_assembler_queue=true
sort_refinery_input=true
max_queue_per_run=2
max_queue_amount=500
assemblers=G:[BMS-II] Assemblers
refineries=G:[BMS-II] Refineries
```

| Key | Default | Effect |
|---|---|---|
| `monitor_only=false` | true | **Must be false** for autocrafting to queue anything |
| `autocraft_components=true` | true | Enable autocrafting |
| `auto_disassemble=false` | false | Enable auto-disassembly of excess components |
| `max_queue_per_run=2` | 2 | How many quota items to process per cycle |
| `max_queue_amount=500` | 500 | Max items queued per component per cycle |
| `assemblers=G:Group Name` | — | Use a specific block group for assemblers |
| `refineries=G:Group Name` | — | Use a specific block group for refineries |

---

## Autocrafting Quotas

Under `[AssemblerPriority]` in PB Custom Data, after the `AutoCrafting=Component` trigger line:

```ini
[AssemblerPriority]
SteelPlate
InteriorPlate
...
AutoCrafting=Component
SteelPlate=70000
InteriorPlate=70000
Construction=70000
Computer=10000
Motor=15000
```

AGM crafts components when stock falls below the quota. Disassembles when stock exceeds the quota (if `auto_disassemble=true`).

---

## Quick Reference

| Where | Tag | Purpose |
|---|---|---|
| LCD block name | `[AGM-S]` | Dashboard screen |
| Light/LCD Custom Data | `[AGM-LIGHT]` | Alert light or corner LCD |
| Cargo block name | `{Ore 1}` etc. | Sort destination for item type |
| Cargo block name | `{Locked}` | Exclude from sort destinations |
| Any block name | `{Manual}` | Exclude from production management |
| Any block name | `{Hidden}` | Exclude from all scanning |
| Assembler Custom Data | `[AGM]` | Control craft/disassembly per assembler |
| Connector block name | `[No Sorting]` | Exclude docked grid from sorting |
