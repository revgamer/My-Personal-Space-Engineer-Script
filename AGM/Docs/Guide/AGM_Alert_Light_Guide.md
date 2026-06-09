# AGM Alert Light & Corner LCD Guide

**Script:** AutoGrid Manager v1.3+
**Author:** RevGamer

---

## Overview

AGM can control light blocks and corner LCDs to show the status of any monitored system. Each block is configured independently via its own Custom Data — no block renaming needed.

---

## Setup

### Step 1 — Add the tag to Custom Data

Open the block's Custom Data and add:

```ini
[AGM-LIGHT]
watch=Battery
```

That's it. AGM detects the block on the next rescan (~10 seconds) and starts controlling it.

### Step 2 — Nothing else needed

- No [AGM-S] tag required
- No block renaming required
- Works on any light block or any LCD/corner LCD that is a text surface provider

---

## Watch Values

| watch= | What it monitors |
|---|---|
| Battery | Battery alert level |
| Cargo | Cargo stock alert level |
| Hydrogen | Hydrogen tank level |
| Oxygen | Oxygen tank level |
| Uranium | Uranium stock level |
| Production | Production alert level |
| Charging | Reactor charging state |
| Power OK | Power stable indicator |
| (leave blank) | Overall AGM alert level |

---

## Alert States and Colours

| State | Light colour | Corner LCD border |
|---|---|---|
| OK / Online | Green | Green |
| Warning | Amber | Amber |
| Critical | Red blinking | Red |

---

## Light Blocks

Any light block — interior light, spotlight, corner light etc.

AGM sets colour, blink (solid for OK/Warning, 1 second blink for Critical), and keeps it enabled.

Example:

```
Interior Light
Custom Data:
[AGM-LIGHT]
watch=Battery
```

---

## Corner LCDs

Any text surface provider — corner LCD, small LCD, wide LCD, button panel screen etc.

AGM draws:
- Topic name large and centred — e.g. BATTERY
- Status below — ONLINE / WARNING / CRITICAL
- Coloured border matching the alert state
- AGM version small at the bottom

Wide LCD — topic left, status right on same line.
Square/tall LCD — topic large centred, status below.
Font scales automatically to fit any panel size.

Example:

```
Corner LCD
Custom Data:
[AGM-LIGHT]
watch=Uranium
```

---

## Multiple Blocks

Each block has its own Custom Data so you can have as many as you want:

```
Interior Light A    ->  [AGM-LIGHT] watch=Battery
Interior Light B    ->  [AGM-LIGHT] watch=Cargo
Corner LCD Left     ->  [AGM-LIGHT] watch=Hydrogen
Corner LCD Right    ->  [AGM-LIGHT] watch=Production
Spotlight           ->  [AGM-LIGHT] watch=          (overall alert)
```

---

## Important Notes

- Do NOT add [AGM-S] to these blocks — [AGM-LIGHT] blocks are excluded from dashboard screens automatically. Adding [AGM-S] too causes CoreDashboard to flicker on them.
- Do NOT put CoreDashboard in the Custom Data of an alert block.
- Alert corner LCDs are redrawn every tick — no flicker.
- AGM rescans every ~10 seconds — new alert blocks detected automatically without recompiling.

---

## Alert Thresholds (PB Custom Data)

```ini
[Alerts]
battery_low_percent=25
hydrogen_low_percent=20
oxygen_low_percent=20
uranium_low_kg=5
cargo_warning_percent=90
cargo_full_percent=98
```
