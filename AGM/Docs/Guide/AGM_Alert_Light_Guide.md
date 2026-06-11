# AGM Alert Light and Corner LCD Guide

**Script:** AutoGrid Manager v1.5
**Author:** RevGamer

---

## Overview

AGM controls light blocks and corner LCDs to show system status. Each block is configured via its own Custom Data -- no block renaming needed.

---

## Setup

Open the block Custom Data and add:

```ini
[AGM-LIGHT]
watch=Battery
```

AGM picks it up on the next rescan. No `[AGM-S]` needed. No block rename needed.

---

## Watch Values

| watch= | What it monitors |
|--------|-----------------|
| `Battery` | Battery alert level |
| `Cargo` | Cargo stock alert level |
| `Hydrogen` | Hydrogen tank level |
| `Oxygen` | Oxygen tank level |
| `Uranium` | Uranium stock level |
| `Production` | Production alert level |
| `Charging` | Reactor charging state |
| `Power OK` | Power stable indicator |
| *(blank)* | Overall AGM alert level |

---

## Alert States and Colours

| State | Light colour | Corner LCD border |
|-------|-------------|------------------|
| OK | Green | Green |
| Warning | Amber | Amber |
| Critical | Red blinking | Red |

---

## Light Blocks

Any interior light, spotlight, or corner light.

AGM sets colour, blink rate (solid for OK/Warning, 1-second blink for Critical), keeps enabled.

```
Interior Light
Custom Data:
[AGM-LIGHT]
watch=Battery
```

---

## Corner LCDs

Any text surface provider -- corner LCD, small LCD, wide LCD, button panel screen.

AGM draws:
- Topic name large and centred (e.g. BATTERY)
- Status below (ONLINE / WARNING / CRITICAL)
- Coloured border matching alert state
- Tiny AGM version at the bottom

Wide LCD: topic left, status right on same line.
Square/tall LCD: topic centred, status below.
Font scales automatically to any panel size.

Redrawn every tick -- never flickers.

```
Corner LCD
Custom Data:
[AGM-LIGHT]
watch=Uranium
```

---

## Multiple Blocks

Each block has its own Custom Data:

```
Interior Light A    ->  [AGM-LIGHT] / watch=Battery
Interior Light B    ->  [AGM-LIGHT] / watch=Cargo
Corner LCD Left     ->  [AGM-LIGHT] / watch=Hydrogen
Corner LCD Right    ->  [AGM-LIGHT] / watch=Production
Spotlight           ->  [AGM-LIGHT] / (blank = overall)
```

---

## Important Notes

- Do NOT add `[AGM-S]` to alert blocks -- `[AGM-LIGHT]` blocks are automatically excluded from dashboard screens. Adding `[AGM-S]` as well causes CoreDashboard to flicker on them.
- AGM rescans every ~10 seconds -- new alert blocks detected automatically.
- Alert LCDs are drawn every tick via `DrawAlertLcds()` -- completely separate from the staged draw cycle.

---

## Alert Thresholds (PB Custom Data)

```ini
[Alerts]
battery_low_percent=25
hydrogen_low_percent=20
oxygen_low_percent=20
uranium_low_kg=5
ingot_low_percent=20
component_low_percent=20
ammo_low_percent=20
```
