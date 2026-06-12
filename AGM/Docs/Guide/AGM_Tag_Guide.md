# AGM v1.5 -- Block Tag Reference

All configuration in Custom Data or block name. No coding needed.

---

## LCD / Screen Tags

### `[AGM-S]` -- Dashboard Screen

Put in the **block name** of any LCD or text surface provider.

Put one dashboard command in the **Custom Data**:

| Custom Data | Display |
|-------------|---------|
| `CoreDashboard` | System overview |
| `AlertDashboard` | Alert status |
| `WarningDashboard` | Warning details |
| `PowerDashboard page=1` | Power overview |
| `ReactorRefuel` | Reactor uranium status |
| `BatteryControl` | Battery/reactor automation |
| `LogisticsDashboard` | Sorting status |
| `ProductionDashboard page=1` | Production overview |
| `ProductionDetails` | Assembler details |
| `ProductionWarnings` | Refinery details |
| `InventoryStock page=1` | All items stock |
| `OreStock page=1` | Ore stock |
| `IngotStock page=1` | Ingot stock |
| `ComponentStock page=1` | Component stock |
| `AmmoStock page=1` | Ammo stock |
| `ToolStock page=1` | Tool stock |
| `BottleStock page=1` | Bottle stock |
| `FoodStock page=1` | Food stock (v1.5) |
| `SeedStock page=1` | Seed stock (v1.5) |
| `IngredientStock page=1` | Ingredient stock (v1.5) |
| `Autocrafting page=1` | Autocrafting quotas |
| `FuelLifeSupport` | H2/O2 tanks and life support |
| `LifeSupport` | Life support only |

Never add `[AGM-S]` to a block that also has `[AGM-LIGHT]` in its Custom Data.

---

### `[AGM-LIGHT]` -- Alert Light / Corner LCD

Put in the **Custom Data** of any light block or corner LCD:

```ini
[AGM-LIGHT]
watch=Battery
```

| watch= value | Monitors |
|--------------|----------|
| `Battery` | Battery charge level |
| `Cargo` | Cargo fill level |
| `Hydrogen` | Hydrogen tank level |
| `Oxygen` | Oxygen tank level |
| `Uranium` | Uranium stock |
| `Production` | Production alert |
| `Charging` | Reactor charging state |
| `Power OK` | Power stable indicator |
| *(blank)* | Overall AGM alert level |

Alert states: OK = green, Warning = amber, Critical = red blinking.

Corner LCDs show topic name large with status below and coloured border. Redrawn every tick.

Do NOT add `[AGM-S]` to these blocks.

---

## Cargo Container Tags

Put in the **block name** OR **Custom Data** (both work in v1.5):

| Tag | Items sorted in |
|-----|----------------|
| `{Ore 1}` | Ores |
| `{Ingot 1}` | Ingots |
| `{Component 1}` | Components |
| `{Ammo 1}` | Ammo |
| `{Tools 1}` | Tools |
| `{Bottle 1}` | Bottles |
| `{Food 1}` | Foods (v1.5) |
| `{Seed 1}` | Seeds (v1.5) |
| `{Ingredient 1}` | Ingredients (v1.5) |

Number = priority. Lower fills first. Spills to next number at 98% full.

Multiple containers: `{Ore 1}`, `{Ore 2}`, `{Ore 3}`

---

## Protection Tags

| Tag | Where | Effect |
|-----|-------|--------|
| `[No Sorting]` | Connector Custom Data or name | Excludes entire docked grid |
| `{Locked}` | Container name or Custom Data | Never used as sort destination |
| `{Manual}` | Block name or Custom Data | Excluded from production management |
| `{Hidden}` | Block name or Custom Data | Excluded from all AGM scanning |
| `[Air Vent]` | Air vent Custom Data or name | Includes vent in FuelLifeSupport monitoring. Do NOT tag airlock vents |

---

## Connector Tag -- Docked Ships

Put in the **connector** Custom Data (or block name):

```
[No Sorting]
```

AGM completely excludes the docked grid from sorting, stock scanning, and autocrafting.

---

## PB Custom Data -- Production Quick Ref

```ini
[Production]
monitor_only=false        <- MUST be false for autocrafting to run
autocraft_components=true
auto_disassemble=false    <- enable only when you want excess scrapped
```

Disassembly in v1.5 never fights autocrafting -- skips any component with assembly queued.

---

## Quick Reference Table

| Where | Tag | Purpose |
|-------|-----|---------|
| LCD block name | `[AGM-S]` | Dashboard screen |
| Light/LCD Custom Data | `[AGM-LIGHT]` | Alert light or corner LCD |
| Cargo name or Custom Data | `{Ore 1}` etc. | Sort destination |
| Cargo name or Custom Data | `{Locked}` | Exclude from sort |
| Block name or Custom Data | `{Manual}` | Exclude from production |
| Block name or Custom Data | `{Hidden}` | Exclude from all scanning |
| Connector name or Custom Data | `[No Sorting]` | Exclude docked grid |
