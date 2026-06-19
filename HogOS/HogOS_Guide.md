# HogOS v2.0 — Guide

**Author:** RevGamer (Simba "Davy" Jones)
**Version:** 2.0
**For:** GroundHog, SpaceHog, HydroHog — and any mining ship with a multi-surface cockpit

---

## Table of Contents

1. [What Is HogOS?](#1-what-is-hogos)
2. [What You Need](#2-what-you-need)
3. [Installing the Script](#3-installing-the-script)
4. [Setting Up the Programmable Block](#4-setting-up-the-programmable-block)
5. [Setting Up Your Cockpit Screens](#5-setting-up-your-cockpit-screens)
6. [Setting Up Extra LCDs](#6-setting-up-extra-lcds)
7. [Dock Status — The [HogOS-Dock] Tag](#7-dock-status----the-hogos-dock-tag)
8. [Screen Reference](#8-screen-reference)
9. [Optional Config Keys](#9-optional-config-keys)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. What Is HogOS?

HogOS is a cockpit display manager for mining ships. It reads your ship's
systems in real time and draws live data on your LCD screens — battery charge,
reactor fuel, ore inventory, lift capacity, and cargo fill — all in a clean
amber-on-black HUD style.

When you dock to a base or another ship, the boot logo screen automatically
switches to a dock status panel showing what you're connected to. When you
undock, it switches back.

### What it looks like

```
+---------------------------+    +---------------------------+
|  HogOS               v2.0 |    |  HogOS               v2.0 |
|  POWER SYSTEMS        [/] |    |  ORE CARGO  47.3%     [/] |
|  ......................... |    |  ......................... |
|  BATTERY              82.0%|    |  [O] Iron            4.20t|
|  [=========>          ]   |    |  ......................... |
|  STABLE     2.1h remaining|    |  [O] Nickel          1.85t|
|  ..........................|    |  ......................... |
|  REACTOR FUEL         61.0%|    |  [O] Silicon         0.94t|
|  [======>             ]   |    |  ......................... |
|  2 of 2 online            |    |  [O] Stone           0.31t|
|  ..........................|    |  ......................... |
|  NET  +0.42 MW            |
+---------------------------+    +---------------------------+

+---------------------------+    +---------------------------+
|       Lift thrust          |    |  HogOS               v2.0 |
|    .  .  .  .  .  .  .   |    |  DOCK STATUS          [/] |
|   . . . . . . . . . . .  |    |  ......................... |
|  . . . . . . . . . . . . |    |  STATUS            LOCKED  |
|        63.00%             |    |  ......................... |
|      Lift thrust          |    |  DOCKED TO                |
|  . . . . . . . . . . . . |    |  RTR Mining Platform       |
|   . . . . . . . . . . .  |    |  ......................... |
|    .  .  .  .  .  .  .   |    |  CONNECTOR                |
|        41.00%             |    |  Hog Dock [HogOS-Dock]    |
|      Cargo capacity       |
+---------------------------+    +---------------------------+
     Weight screen                   Splash (docked)
```

---

## 2. What You Need

| Requirement | Notes |
|---|---|
| A Programmable Block | One per ship -- any size |
| A cockpit or LCD surface | Any multi-surface block works (cockpit, fighter cockpit, LCD panel, corner LCD, etc.) |
| No mods required | HogOS is vanilla-only |

HogOS automatically scans your grid for batteries, reactors, cargo containers,
thrusters, and ship controllers. You do not need to name blocks in any special
way unless you want to use groups (see [Optional Config Keys](#9-optional-config-keys)).

---

## 3. Installing the Script

### Step 1 -- Copy the script

Open `HogOS\Scripts\HogOS.cs` in a text editor and copy the entire contents.

### Step 2 -- Open the Programmable Block

In-game, walk up to your Programmable Block and press **F** to interact, or
open it from the Terminal (K menu).

```
+----------------------------------+
|  Programmable Block              |
|                                  |
|  [Edit]  [Run]  [Delete]         |
|                                  |
|  Detailed info:                  |
|  No script loaded                |
+----------------------------------+
```

Click **Edit** to open the code editor.

### Step 3 -- Paste the script

Select all existing code in the editor (Ctrl+A) and delete it. Then paste the
HogOS script (Ctrl+V).

### Step 4 -- Check Code

Click **Check Code**. You should see:

```
+----------------------------------+
|  Compilation successful          |
|                                  |
|  [OK]                            |
+----------------------------------+
```

If you see errors, make sure you pasted the full script without any extra
`namespace` or `using` lines at the top -- HogOS must start directly with
the `private HogOS _os;` line.

### Step 5 -- Click OK

The script is now loaded. The PB detail panel will show:

```
HogOS v2.0 -- Booting...
```

---

## 4. Setting Up the Programmable Block

HogOS writes a default config to the PB's Own Data automatically on first run.
You do not need to edit the PB's Own Data unless you want to change groups.

The PB's own display (Surface 0) is **always** set to the Splash screen by the
script. You cannot override this from Custom Data -- it is locked to Splash so
you always have a status indicator on the PB itself.

---

## 5. Setting Up Your Cockpit Screens

This is the main setup step. You tell HogOS which screen to show on each
surface by adding a config block to your cockpit's **Custom Data**.

### How to open Custom Data

1. Open the Terminal (K)
2. Find your cockpit in the left panel and click it
3. On the right side, scroll down and click **Custom Data**

```
+-------------------------------------------+
|  RTR Mining Cockpit                       |
|  [On/Off]  [Share on HUD]                |
|                                           |
|  Custom Data:                             |
|  +---------------------------------------+|
|  |                                       ||
|  |  (empty -- click here to edit)       ||
|  |                                       ||
|  +---------------------------------------+|
+-------------------------------------------+
```

### What to type

Add the following block. Use exactly this format -- square brackets, no spaces
around the `=` sign, exact screen names.

```
[HogOS]
Surface0=Splash
Surface1=Power
Surface2=OreCargo
Surface3=Weight
```

**Example -- 4-screen cockpit (GroundHog layout):**

```
[HogOS]
Surface0=Splash
Surface1=Power
Surface2=OreCargo
Surface3=Weight
```

```
+----------+----------+
|          |          |
|  Splash  |  Power   |
| (logo /  | Battery  |
|  dock)   | Reactor  |
|          |          |
+----------+----------+
|          |          |
| OreCargo |  Weight  |
|  Iron    |  [arc]   |
|  Nickel  |  [arc]   |
|  Stone   |          |
+----------+----------+
        Cockpit
```

### Screen name reference

| Name | What it shows |
|---|---|
| `Splash` | HogOS logo at boot; switches to dock panel when docked |
| `Loading` | Animated boot bar (used automatically on cockpit entry) |
| `Power` | Battery + reactor gauges + net power flow |
| `OreCargo` | Live ore inventory sorted by mass |
| `Weight` | Lift thrust gauge + cargo capacity gauge |
| `Blank` | Turns the surface off (black screen) |

### Example -- if you only have 2 screens

```
[HogOS]
Surface0=Power
Surface1=OreCargo
```

### Example -- if you want one screen blank

```
[HogOS]
Surface0=Splash
Surface1=Power
Surface2=Blank
Surface3=OreCargo
```

### After saving Custom Data

HogOS re-scans every ~30 seconds. You can also recompile the PB (open Edit,
click Check Code, click OK) to apply changes immediately.

---

## 6. Setting Up Extra LCDs

You can put HogOS screens on any LCD panel or corner LCD on the same grid --
not just the cockpit. The process is identical: open the block's Custom Data
and add the same config block.

### Example -- LCD Panel showing Power only

```
[HogOS]
Surface0=Power
```

```
+---------------------------+
|  HogOS               v2.0 |
|  POWER SYSTEMS        [/] |
|  ..........................|
|  BATTERY              82.0%|
|  [=========>          ]   |
|  STABLE   2.1h remaining  |
|  ..........................|
|  REACTOR FUEL         61.0%|
|  [======>             ]   |
|  2 of 2 online            |
|  ..........................|
|  NET  +0.42 MW            |
+---------------------------+
```

### Example -- Corner LCD showing OreCargo

Corner LCDs have only one surface (Surface0).

```
[HogOS]
Surface0=OreCargo
```

### Important -- script mode

HogOS sets the screen to Script mode automatically when it draws. You do not
need to change the screen's Content Type manually. If a screen shows a static
image or text instead of the HogOS display, check that the block's Custom Data
has the `[HogOS]` section correctly formatted.

---

## 7. Dock Status -- The [HogOS-Dock] Tag

HogOS can detect when your ship docks and show connection info on any Splash
screen. To enable this, you tag your ship's docking connector.

### Step 1 -- Find your docking connector

Open the Terminal, find the connector you use to dock to your base or platform.

### Step 2 -- Add the tag to its name

Rename the connector so `[HogOS-Dock]` appears anywhere in its name.

**Before:**
```
Connector
```

**After:**
```
Connector [HogOS-Dock]
```

Or keep a more descriptive name:
```
Front Dock [HogOS-Dock]
```

That's it. No Custom Data needed on the connector.

### What happens when you dock

Any surface set to `Splash` automatically switches from the boot logo to a
dock status panel:

```
+---------------------------+
|  HogOS               v2.0 |
|  DOCK STATUS          [/] |
|  ..........................|
|  STATUS            LOCKED  |  <-- green when locked
|  ..........................|
|  DOCKED TO                |
|  RTR Mining Platform       |  <-- the other grid's name
|  ..........................|
|  CONNECTOR                |
|  Front Dock [HogOS-Dock]  |  <-- your connector's name
+---------------------------+
```

Status values:

| Status | Colour | Meaning |
|---|---|---|
| LOCKED | Green | Connector is fully locked and connected |
| CONNECTABLE | Amber | Connector is in range and ready to lock |

When you undock, the Splash screen reverts to the boot logo automatically.

### What if I have multiple docking connectors?

Add `[HogOS-Dock]` to each one you want to monitor. HogOS checks all tagged
connectors and shows the dock panel as soon as any of them is Connected or
Connectable.

---

## 8. Screen Reference

### Splash

Shows the HogOS logo with your name and version number at idle. Switches to
the dock status panel automatically when docked.

```
+---------------------------+
|                           |
|          [logo]           |
|                           |
|          HogOS            |
|    Hog Operating System   |
|                           |
| RevGamer (Simba "Davy"..  |
| ......................... |
|                      v2.0 |
+---------------------------+
```

### Loading

An animated boot sequence that plays when you sit in the cockpit. Shows a
random loading message and a progress bar. Disappears automatically after
1.5--3 seconds and the normal screens take over.

```
+---------------------------+
|          HogOS            |
|                           |
|          [logo]           |
|                           |
|                           |
| Scanning ore signatures...|
| [=======>                 |
|                       42% |
+---------------------------+
```

You do not assign this screen manually. It appears automatically when
`EnableImmersion = true` is set in the cockpit's Custom Data and you
sit down.

### Power

```
+---------------------------+
|  HogOS               v2.0 |
|  POWER SYSTEMS        [/] |
|  ..........................|
|  BATTERY              82.0%|
|  [=========>          ]   |
|  STABLE   2.1h remaining  |
|  ..........................|
|  REACTOR FUEL         61.0%|
|  [======>             ]   |
|  2 of 2 online            |
|  ..........................|
|  NET  +0.42 MW            |
+---------------------------+
```

Battery status values:

| Status | Colour | Meaning |
|---|---|---|
| STABLE | Amber | Input roughly equals output |
| CHARGING | Green | Net positive power (charging) |
| DRAINING | Red | Net negative power (draining) |
| NO BATTERIES | Red | No functional batteries found |

Battery bar turns red below 15%. Reactor bar turns red below 10%.

### OreCargo

```
+---------------------------+
|  HogOS               v2.0 |
|  ORE CARGO  47.3%     [/] |
|  ..........................|
|  [O] Iron            4.20t|
|  ..........................|
|  [O] Nickel          1.85t|
|  ..........................|
|  [O] Silicon         0.94t|
|  ..........................|
|  [O] Stone           0.31t|
|  ..........................|
+---------------------------+
```

Shows all ore types currently in any inventory on the grid, sorted by mass
descending. The header shows total cargo fill percentage. Mass is formatted as
`kg`, `t` (tonnes), or `Mt` (megatonnes) automatically.

If no ore is found, the screen shows a stone ore icon and "No ores detected".

### Weight

Two ExcavOS-style radial arc gauges stacked vertically.

```
+---------------------------+
|    .  .  .  .  .  .  .   |  <- dim arc bars (unfilled)
|   . . [=][=]. . . . . .  |  <- lit arc bars (filled)
|  . .[=][=][=][=]. . . . .|
|       63.00%              |  <- current value
|      Lift thrust          |  <- label
| ......................... |  <- separator
|  . .[=][=][=][=]. . . . .|  <- bottom gauge (flipped)
|   . . [=][=]. . . . . .  |
|    .  .  .  .  .  .  .   |
|       41.00%              |
|    +0.08%  full in 312s   |  <- fill rate when drilling
+---------------------------+
```

Top gauge: lift thrust usage (how hard your lift thrusters are working against
gravity). A flashing danger icon appears when lift usage exceeds 90%.

Bottom gauge: cargo capacity fill (combined across all inventories including
drill buffers). Shows fill rate and estimated time to full when actively mining.

If no lift thrusters are detected, only the cargo gauge is shown using the
full screen height.

---

## 9. Optional Config Keys

These go inside the `[HogOS]` section in the **PB's Own Data** (not the
cockpit). HogOS writes them automatically with their defaults on first run.

```
[HogOS]
CargoTrackGroupName    =
LiftThrustersGroupName =
StopThrustersGroupName =
LiftThresholdWarning   = 0.9
```

| Key | Default | What it does |
|---|---|---|
| `CargoTrackGroupName` | *(empty)* | If set, only counts inventory in this block group for cargo fill. Leave empty to scan the whole grid. |
| `LiftThrustersGroupName` | *(empty)* | If set, uses only this group as lift thrusters. Leave empty for auto-detection by direction. |
| `StopThrustersGroupName` | *(empty)* | If set, uses only this group for stopping distance calculation. Leave empty for auto-detection. |
| `LiftThresholdWarning` | `0.9` | Lift gauge flashes a danger icon above this fraction (0.9 = 90%). |

### EnableImmersion (cockpit only)

Add this to a cockpit's Custom Data alongside the surface assignments to enable
the Loading screen when you sit down:

```
[HogOS]
EnableImmersion = true
Surface0=Splash
Surface1=Power
Surface2=OreCargo
Surface3=Weight
```

---

## 10. Troubleshooting

| Problem | Cause | Fix |
|---|---|---|
| Screen shows static image or text instead of HogOS | Screen is not in Script mode | HogOS sets this automatically. Check the Custom Data has the `[HogOS]` section with no typos. Recompile the PB. |
| "No ores detected" on OreCargo but ore is in cargo | Sorters or conveyors are being excluded | HogOS skips Conveyor Sorters when no group is set. Set `CargoTrackGroupName` to a group that includes your cargo containers. |
| Weight screen shows "No controller" | No working ship controller found | Make sure your cockpit or remote control is functional and on the same construct as the PB. |
| Weight screen shows "Static grid" | Ship has no physics (parked on voxel, station mode) | Normal behaviour on a static grid. Launch the ship to see live readings. |
| Dock panel does not appear when docked | Connector tag is wrong or connector is on a different grid | Check the connector name contains exactly `[HogOS-Dock]`. The connector must be on the same construct as the PB. |
| Lift gauge shows 0% even in atmosphere | Lift thrusters not detected by direction | Set `LiftThrustersGroupName` to a block group containing your lift thrusters. |
| Screen is black | Surface is set to `Blank`, or the block is off | Check Custom Data. If intentional, this is correct behaviour. |
| PB shows compile error on paste | Extra `namespace` or `using` lines in the pasted text | Delete all content before pasting. HogOS must start with `private HogOS _os;`. |
| HogOS does not pick up a newly added LCD | Block scan cache takes up to 30 seconds to refresh | Wait 30 seconds or recompile the PB to force an immediate rescan. |
