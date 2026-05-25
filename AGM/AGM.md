# AGM_Chatgpt.md — AutoGrid Manager Continuation Guide

Project: **AutoGrid Manager**  
Short name: **AGM**  
Author: **RevGamer**  
Current testing build family: **0.3.x test phase**  
Current test LCD tag: `[AGM-S]`  
Current PB/main controller tag: `{AGM-Main}`  
Game: **Space Engineers**  
Script type: **Programmable Block script**

---

## 1. Project summary

AutoGrid Manager is a Space Engineers Programmable Block script being developed as a unified grid-management system.

The design goal is to combine:

- Automatic LCD-style display commands
- inventory totals
- cargo fill status
- power status
- missing quota display
- category sorting
- R-SILO / GOAT-like item stock screens
- real item icons using Space Engineers sprite IDs
- multi-LCD stock boards
- boot/reboot screens
- a branded Programmable Block controller screen

AGM is currently in **testing phase**, not final public release.

Current refresh pacing:

- The PB runs on `Update10` for responsive LCD updates.
- Inventory totals refresh every 30 game ticks, roughly every 0.5 seconds.
- Block rescans run every 300 game ticks, roughly every 5 seconds.
- AGM draws 1 LCD per run, roughly 6 LCDs per second.
- Crafting checks run every 300 game ticks, roughly every 5 seconds.
- Sorting checks run every 100 game ticks, roughly every 1.67 seconds.

---

## 2. Important use / permission context

The user has permission to use GOAT-style icons/methods for their **personal script**.

For private RevGamer testing, AGM may use the same item sprite approach from the user's R-SILO script.

For any public Workshop release:

- Do not copy GOAT Sorter source/assets unless explicit redistribution permission exists.
- Use clean-room AGM code.
- Use Space Engineers built-in sprite IDs where possible.
- Credit appropriately if any third-party inspiration or permitted code is used.
- Keep private-only builds clearly labelled as private/personal-use if they include permission-limited work.

---

## 3. Current tag system

### 3.1 Programmable Block / controller screen tag

Use this only on the main AGM Programmable Block:

```text
{AGM-Main}
```

Example PB name:

```text
PB AutoGrid Manager {AGM-Main}
```

Purpose:

- Enables AGM branding on the Programmable Block front display.
- Shows boot/reboot status on the PB's own screen.
- Shows online/status summary during normal running.
- Keeps PB/controller display separate from inventory LCDs.

---

### 3.2 External inventory LCD test tag

Use this on normal LCD panels / text panels:

```text
[AGM-S]
```

Example LCD names:

```text
LCD Components Left [AGM-S]
LCD Components Right [AGM-S]
LCD Ore 1 [AGM-S]
LCD Ingots Status [AGM-S]
```

Purpose:

- Testing tag for stock/sprite display screens.
- Prevents flicker with older `[AGM]`, `[LCD]`, AutoLCD2, or previous AGM PBs.
- Keep this tag during test phase.

---

### 3.3 Legacy / compatibility tags

Older AGM and AutoLCD testing used:

```text
[AGM]
[LCD]
```

Avoid using those on current test LCDs if old scripts are still running, because multiple PBs may write to the same screen and cause flicker.

---

## 4. Current command examples

### 4.0 Power dashboard

LCD Custom Data:

```text
PowerDashboard=RAB Base
```

PB Custom Data:

```ini
[power:RAB Base]
batteries=G:[RAB] Batteries
reactors=G:[RAB] Nuclear Reactors
solar=
wind=
hydrogen=
include_ungrouped=false
```

Only configured, non-empty source lines should show on the LCD when
`include_ungrouped=false`. For asteroid bases, leave `wind=` and
`hydrogen=` blank to hide those rows.

### 4.1 Single inventory screen

```text
IndustrialInventory=Component
```

```text
IndustrialInventory=Ore
```

```text
IndustrialInventory=Ingot
```

```text
IndustrialInventory=Ammo
```

---

### 4.1a Autocrafting LCD

Use this on an `[AGM-S]` LCD to show autocrafting quotas and let AGM queue
missing vanilla component crafts in assemblers.

```text
AutoCrafting=Component
SteelPlate=20000
InteriorPlate=5000
Construction=10000
Computer=2000
Motor=3000
```

Plain English behavior:

- If `SteelPlate=20000` and the grid has `12000`, AGM queues about `8000`
  more Steel Plates.
- AGM counts what is already in assembler queues, so it should not keep
  spamming the same request every cycle.
- This first pass supports known vanilla component blueprints. Modded item
  learning can be added later.

Short numbers are allowed:

```text
SteelPlate=20k
InteriorPlate=5k
```

Multi-LCD pages can be linked from the LCD name. Put the full quota list
only on page 1.

First LCD name:

```text
LCD Autocrafting [AGM-S] !LINK:A1
```

AutoLCD2-style spacing is also accepted:

```text
LCD Autocrafting [AGM-S] !LINK:A 1
```

First LCD Custom Data:

```text
AutoCrafting=Component
SteelPlate=50000
InteriorPlate=50000
Construction=50000
Computer=5000
Motor=10000
MetalGrid=5000
Girder=5000
SmallTube=5000
LargeTube=5000
Display=1000
BulletproofGlass=1000
PowerCell=1000
SolarCell=1000
Detector=500
RadioCommunication=500
Medical=200
Reactor=500
Thrust=1000
GravityGenerator=100
Superconductor=500
Explosives=500
Canvas=200
```

Second LCD name:

```text
LCD Autocrafting [AGM-S] !LINK:A2
```

Second LCD Custom Data can be empty. It will reuse page 1's quota list
and show page 2.

This is also okay if you want a visible page hint in Custom Data:

```text
AutoCrafting=Component:2
```

With a linked LCD name, AGM treats that as command-only data and still
borrows the full quota list from page 1.

This also works without the colon:

```text
LCD Autocrafting [AGM-S] !LINKA:2
```

Unknown modded components only show once AGM has seen at least one in
inventory. They will be marked `NO BP` until blueprint learning is added.

---

### 4.2 Manual page control

Format:

```text
IndustrialInventory=<Category>:<Page>
```

Examples:

```text
IndustrialInventory=Component:1
IndustrialInventory=Component:2
IndustrialInventory=Ore:1
IndustrialInventory=Ore:2
```

---

### 4.3 Manual page + rows per page

Format:

```text
IndustrialInventory=<Category>:<Page>:<RowsPerPage>
```

Examples:

```text
IndustrialInventory=Component:1:14
IndustrialInventory=Component:2:14
IndustrialInventory=Ore:1:6
IndustrialInventory=Ore:2:6
```

Rows per page is used when forcing multi-screen output even if the category could fit on one screen.

---

## 5. Multi-LCD layouts

AGM should support two main multi-LCD layout modes:

1. **Wide / horizontal**
2. **Vertical / stacked**

The purpose is to make two or more LCDs feel like one larger display.

---

### 5.1 Wide / horizontal layout

Use when LCDs are side by side.

Two LCD example:

Left LCD:

```text
IndustrialInventoryWide=Component:1:14:left
```

Right LCD:

```text
IndustrialInventoryWide=Component:2:14:right
```

Three LCD example:

```text
IndustrialInventoryWide=Component:1:14:left
IndustrialInventoryWide=Component:2:14:middle
IndustrialInventoryWide=Component:3:14:right
```

Expected behaviour:

- Left screen does not draw an unnecessary right-side join border.
- Right screen does not draw an unnecessary left-side join border.
- Middle screens avoid left and right join borders.
- Screens use the same visual style.
- Page counts and row counts line up.
- The result should feel like one extended horizontal display.

---

### 5.2 Vertical / stacked layout

Use when LCDs are mounted above/below each other.

Two LCD example:

Top LCD:

```text
IndustrialInventoryVertical=Component:1:8:top
```

Bottom LCD:

```text
IndustrialInventoryVertical=Component:2:8:bottom
```

Three LCD example:

```text
IndustrialInventoryVertical=Component:1:8:top
IndustrialInventoryVertical=Component:2:8:middle
IndustrialInventoryVertical=Component:3:8:bottom
```

Expected behaviour:

- Top screen does not need a heavy bottom join border.
- Bottom screen does not need a heavy top join border.
- Middle screen avoids top and bottom join borders.
- Category title should not feel duplicated awkwardly.
- Rows and bars should align vertically where possible.

---

## 6. Current visual style direction

The user wants the stock screens to look similar to their R-SILO/GOAT-style reference screenshot.

Preferred stock-board look:

- warm black/brown panel
- orange/yellow outer border
- bold category title
- item icon on left
- item name
- current amount text
- optional quota text
- progress bar to the right
- `+ more` marker if not all rows fit
- clear page marker
- clean industrial HUD feel

Important: user previously disliked some versions that had a mismatched yellow border on one screen and blue/teal styling on the other. Multi-LCD screens must use one consistent renderer.

---

## 7. Current PB/controller screen design

The Programmable Block front screen should be controlled only when the PB name contains:

```text
{AGM-Main}
```

Example:

```text
PB AutoGrid Manager {AGM-Main}
```

### 7.1 Boot/reboot PB screen

During boot/reboot, PB front screen should show:

```text
AGM
AutoGrid Manager
by RevGamer
REBOOT %
```

With a loading bar.

### 7.2 Normal PB status screen

During normal running, PB front screen should show:

```text
AGM
AutoGrid Manager
ONLINE / PAUSED
LCD count
Cargo count
Items count
```

### 7.3 Important PB screen viewport note

For the PB front display, use viewport drawing:

```csharp
RectangleF((surface.TextureSize - surface.SurfaceSize) * 0.5f, surface.SurfaceSize)
```

Do **not** draw using `surface.SurfaceSize` from origin `(0,0)` only, or the output may appear tiny, offset, or with a side strip.

---

## 8. Boot/reboot behaviour

AGM should show boot/loading screen on:

- `{AGM-Main}` PB front screen
- external `[AGM-S]` LCDs if they are found
- PB Echo output

### 8.1 Run arguments

```text
reload
```

Reload config and start boot visual.

```text
rescan
```

Rescan blocks and start boot visual.

```text
reboot
```

Show AGM boot visual again without needing to recompile.

```text
reset
```

Clear state and start fresh.

---

## 9. PB Echo screen design

The PB terminal Echo area should show a branded status screen.

During boot:

```text
AGM
AutoGrid Manager
by RevGamer

REBOOT / BOOT SEQUENCE
[||||||||..............] 40%
Main PB: ON {AGM-Main}
```

During normal running:

```text
AGM
AutoGrid Manager
by RevGamer

Version: 0.3.x
State  : RUNNING
Main PB: ON {AGM-Main}
LCDs   : 4
Cargo  : 12
Items  : 53
Sort   : ON

Args: reload | rescan | reboot
```

---

## 10. R-SILO / item icon method

The correct R-SILO script uses real Space Engineers item sprite IDs.

### 10.1 Ore icons

Examples:

```csharp
"MyObjectBuilder_Ore/Iron"
"MyObjectBuilder_Ore/Nickel"
"MyObjectBuilder_Ore/Cobalt"
"MyObjectBuilder_Ore/Silicon"
"MyObjectBuilder_Ore/Magnesium"
"MyObjectBuilder_Ore/Silver"
"MyObjectBuilder_Ore/Gold"
"MyObjectBuilder_Ore/Platinum"
"MyObjectBuilder_Ore/Uranium"
"MyObjectBuilder_Ore/Stone"
"MyObjectBuilder_Ore/Scrap"
"MyObjectBuilder_Ore/Ice"
```

### 10.2 Ingot icons

Examples:

```csharp
"MyObjectBuilder_Ingot/Iron"
"MyObjectBuilder_Ingot/Nickel"
"MyObjectBuilder_Ingot/Cobalt"
"MyObjectBuilder_Ingot/Silicon"
"MyObjectBuilder_Ingot/Magnesium"
"MyObjectBuilder_Ingot/Silver"
"MyObjectBuilder_Ingot/Gold"
"MyObjectBuilder_Ingot/Platinum"
"MyObjectBuilder_Ingot/Uranium"
"MyObjectBuilder_Ingot/Stone"
```

Note: Gravel may map to Stone-style sprite depending on game availability.

### 10.3 Component icons

For components:

```csharp
"MyObjectBuilder_Component/" + subtype
```

Examples:

```csharp
"MyObjectBuilder_Component/SteelPlate"
"MyObjectBuilder_Component/InteriorPlate"
"MyObjectBuilder_Component/Construction"
"MyObjectBuilder_Component/MetalGrid"
"MyObjectBuilder_Component/SmallTube"
"MyObjectBuilder_Component/LargeTube"
"MyObjectBuilder_Component/Motor"
"MyObjectBuilder_Component/Display"
"MyObjectBuilder_Component/Computer"
"MyObjectBuilder_Component/PowerCell"
```

### 10.4 Ammo / modded items

For ammo and modded items:

```csharp
item.Type.TypeId + "/" + item.Type.SubtypeId
```

This is better for WeaponCore / modded ammo because it reads whatever is actually present.

### 10.5 Drawing sprite icons

Use:

```csharp
frame.Add(new MySprite(SpriteType.TEXTURE, spriteName, center, size, color));
```

Recommended helper:

```csharp
void TryDrawSprite(MySpriteDrawFrame frame, string spriteName, Vector2 center, Vector2 size, Color col) {
    try {
        frame.Add(new MySprite(SpriteType.TEXTURE, spriteName, center, size, col));
    }
    catch {
        // fallback icon if sprite does not exist
    }
}
```

---

## 11. Current inventory categories

AGM should support:

```text
Ore
Ingot
Component
Ammo
Tool
Bottle
```

Internal key examples:

```text
Ore/Iron
Ingot/Iron
Component/SteelPlate
AmmoMagazine/NATO_25x184mm
PhysicalGunObject/AngleGrinder4Item
GasContainerObject/HydrogenBottle
OxygenContainerObject/OxygenBottle
```

---

## 12. Sorting / cargo tags

AGM can sort cargo containers by item category. Sorting is conservative:
it moves only a few stacks per pass so the PB does not hit the Space
Engineers instruction limit.

Sorting is off by default. Enable it in the AGM Programmable Block Custom
Data:

```ini
enable_sorting=true
```

Suggested cargo names:

```text
Large Cargo Container [Ore]
Large Cargo Container [Ingot]
Large Cargo Container [Component]
Large Cargo Container [Ammo]
Large Cargo Container [Tool]
Large Cargo Container [Bottle]
Large Cargo Container [Inventory]
Large Cargo Container [Locked]
Large Cargo Container [Hidden]
```

Plural tags also work: `[Ores]`, `[Ingots]`, `[Components]`, `[Tools]`,
and `[Bottles]`.

IIM-style plain keywords also work in cargo names, so names like
`Cargo Ores`, `Cargo Ingots`, and `Cargo Components` are recognized even
without brackets. Brackets are still recommended because they are easier to
read and less ambiguous.

Meaning:

| Tag | Meaning |
|---|---|
| `[Ore]` | Ore destination |
| `[Ingot]` | Ingot destination |
| `[Component]` | Component destination |
| `[Ammo]` | Ammo destination |
| `[Tool]` | Tool destination |
| `[Bottle]` | Hydrogen/Oxygen bottle destination |
| `[Inventory]` | General inventory / fallback |
| `[Locked]` | Do not move items from/to this container |
| `[Hidden]` | Exclude from visible quota/count logic where supported |
| `[GOAT]` | Compatibility-style general inventory marker |

Auto expansion:

- If all `[Ore]` containers are full and AGM finds an `[Inventory]` cargo,
  it renames that fallback cargo to `[Ores]`.
- `[GOAT]` cargo is also treated as general fallback and is renamed the same
  way when assigned.
- Untagged cargo containers are also treated as assignable fallback cargo.
- The same applies for `[Ingots]`, `[Components]`, `[Ammo]`, `[Tools]`, and
  `[Bottles]`.
- `[Locked]` and `[Hidden]` cargo containers are never auto-renamed.

Sorter destinations are cargo containers. Sorter sources are cargo containers
plus assembler/refinery output inventories. AGM deliberately does not drain
assembler or refinery input inventories, because that would fight active
production.

Manual test run:

```text
sort
```

The PB Echo panel shows `SortDbg` with the last sorter status, cargo count,
number of moved stacks, and the last moved item route.

Sorter dashboard LCD:

```text
SorterDashboard
```

Aliases:

```text
Sorter
AutoSorter
```

The sorter dashboard shows whether sorting is online, total cargo fill,
type-container counts, fallback/locked/hidden counts, last sorter status,
last moved item, and the source-to-destination route.

---

## 12.1 Fuel & Life Support dashboard

Use this on an `[AGM-S]` LCD:

```text
FuelLifeSupport=Base
```

PB Custom Data:

```ini
[LifeSupport:Base]
hydrogen=G:Hydrogen Tanks
oxygen=G:Oxygen Tanks
generators=G:O2 Generators
include_ungrouped=false
```

Use `FuelLifeSupport` without `=Base` if you want AGM to use all detected
hydrogen tanks, oxygen tanks, and O2/H2 generators. `LifeSupport` is also
accepted as a shorter LCD command.

The dashboard shows:

- Hydrogen tank fill bar and percent
- Oxygen tank fill bar and percent
- O2/H2 generator count: working / online / total
- Ice loaded in generators
- Total ice stock
- Oxygen and hydrogen bottle counts
- Base pressurized status from air vents

AGM only monitors air vents that opt in with `[AGM-S]` in the block name and
`InteriorVent` in the vent Custom Data.

Example air vent name:

```text
Base Air Vent [AGM-S]
```

Example air vent Custom Data:

```text
InteriorVent
```

AGM tags monitored air vents as `[Pressurized]` or `[Leaking]`.
Base pressurized shows `OK` when at least one monitored air vent is good and no
monitored air vent is leaking. A vent is considered good when it is working,
can pressurize, and reports oxygen level at or above 95%. The dashboard also
shows the first leaking vent names.

---

## 13. Known issues / debugging notes

### 13.1 Flickering LCDs

Cause: two scripts writing to the same LCD.

Fix:

- use only `[AGM-S]` on test LCDs
- remove `[AGM]` and `[LCD]` from AGM test screens
- disable older AGM PBs
- disable AutoLCD2 PB if it targets the same display

---

### 13.2 LCD command changed but screen did not update

Earlier issue was caused by slow refresh.

Testing builds may use:

```csharp
UpdateFrequency.Update1 | UpdateFrequency.Update100
```

For final server-friendly version, use:

```csharp
UpdateFrequency.Update10 | UpdateFrequency.Update100
```

with CustomData hash detection.

---

### 13.3 Page 2 does not show

Possible causes:

- not enough items to require page 2
- row count too high
- both commands are on the same LCD
- old screen is still being controlled by another PB

Use forced rows:

```text
IndustrialInventory=Component:1:8
IndustrialInventory=Component:2:8
```

Each LCD should have only one industrial command.

---

### 13.4 PB front screen too small / offset

Cause: wrong viewport.

Fix:

```csharp
RectangleF((surface.TextureSize - surface.SurfaceSize) * 0.5f, surface.SurfaceSize)
```

---

### 13.5 Blue HUD/GPS text over LCD

If the user sees blue text such as:

```text
ICE
Fe, Ur, Pt
20km
```

over the LCD, it is likely Space Engineers HUD/GPS overlay, not AGM LCD content.

---

## 14. Current recommended test setup

### 14.1 Programmable Block

Name:

```text
PB AutoGrid Manager {AGM-Main}
```

### 14.2 Component wide LCDs

Left:

```text
LCD Components Left [AGM-S]
```

Custom Data:

```text
IndustrialInventoryWide=Component:1:14:left
```

Right:

```text
LCD Components Right [AGM-S]
```

Custom Data:

```text
IndustrialInventoryWide=Component:2:14:right
```

### 14.3 Component vertical LCDs

Top:

```text
LCD Components Top [AGM-S]
```

Custom Data:

```text
IndustrialInventoryVertical=Component:1:8:top
```

Bottom:

```text
LCD Components Bottom [AGM-S]
```

Custom Data:

```text
IndustrialInventoryVertical=Component:2:8:bottom
```

### 14.4 Ore example

```text
IndustrialInventoryWide=Ore:1:6:left
IndustrialInventoryWide=Ore:2:6:right
```

---

## 15. Current code architecture

Current AGM code generally contains:

- `Program()`
- `Main(string argument, UpdateType updateSource)`
- config parser
- block scanner
- inventory indexer
- category sorter
- LCD renderer
- industrial sprite renderer
- stock-board renderer
- AGM boot renderer
- `{AGM-Main}` PB screen renderer
- Echo status renderer

Keep the script PB-ready. Do not wrap with:

```csharp
using ...
namespace ...
public class Program : MyGridProgram
```

The game provides the wrapper.

---

## 16. Future features roadmap

### 16.1 Near-term display features

1. **Finalise StockBoard style**
   - consistent R-SILO/GOAT-style layout
   - better row spacing
   - improved icon scaling
   - better text alignment
   - clearer page number
   - better `+ more` marker

2. **Joined LCD layout polish**
   - true wide join borders
   - true vertical join borders
   - optional title only on first screen
   - optional footer only on last screen
   - named layout groups

3. **Screen layout commands**
   - `StockBoard=Component`
   - `StockBoardWide=Component:1:14:left`
   - `StockBoardVertical=Component:1:8:top`
   - shorter aliases for easier use

4. **PB screen themes**
   - `PBTheme=Classic`
   - `PBTheme=Industrial`
   - `PBTheme=Minimal`
   - error/fault screen

5. **Boot animations**
   - scanning blocks
   - indexing inventory
   - sorting online
   - display ready

---

### 16.2 Inventory / quota features

1. **Quota parser from LCD Custom Data**
   - support lines like:
     ```text
     Steel Plate: 300K
     Computer: 5K
     Uranium: 2M
     ```
   - display current/quota
   - low stock red/amber/green

2. **Global PB quota config**
   - `[quotas]` section in PB Custom Data
   - use same quota data on all screens

3. **Missing stock screens**
   - `IndustrialMissing=Component`
   - `IndustrialMissing=All`

4. **Category totals**
   - ore total mass/volume
   - component total count
   - ammo total count
   - bottle count

5. **Search/filter screen**
   - `FindItem=SteelPlate`
   - `FindItem=Iron`

---

### 16.3 Sorting features

1. **Better category sorting**
   - ore to `[Ore]`
   - ingots to `[Ingot]`
   - components to `[Component]`
   - ammo to `[Ammo]`
   - tools/bottles to `[Tool]`/`[Bottle]`

2. **Stock containers**
   - fill named stock containers first
   - support cargo Custom Data:
     ```ini
     [stock]
     name=Printer Components
     Component/SteelPlate=20000
     Component/InteriorPlate=5000
     priority=10
     ```

3. **Overflow containers**
   - automatically use `[Inventory]` fallback
   - optional rename behaviour if desired later

4. **Safety**
   - respect `[Locked]`
   - respect `[Hidden]`
   - avoid item bouncing
   - limit transfers per tick
   - pause/resume sorting

---

### 16.4 Production features

1. **Advanced auto-crafting**
   - current test build has basic vanilla component quota queueing
   - next: queue small batches
   - next: blueprint learning for modded items
   - next: optional master assembler mode

2. **Refinery manager**
   - ore priority
   - uranium protection
   - ice handling
   - refinery queue status LCD

3. **Production screen**
   - assembler status
   - refinery status
   - missing materials
   - queued items

---

### 16.5 Power / system screens

1. **Power dashboard**
   - battery percent
   - reactor output
   - hydrogen engine output
   - solar/wind if applicable

2. **Cargo dashboard**
   - total cargo fill
   - category fill
   - nearly full warning

3. **Connector / dock screen**
   - connected grids
   - pull status
   - no-pull tag support

4. **Reactor refuel**
   - uranium target per reactor
   - refuel command
   - reactor warning screen

---

### 16.6 Map / contact features

Inspired by Mother OS concepts.

Possible future commands:

```text
IndustrialMap=5000
Map=5000
```

Future map data:

```text
own grid centre
GPS waypoints
friendly IGC contacts
connector/docked grid markers
```

Implementation idea:

```csharp
relative = contact.position - own.position
x = dot(relative, own.Right)
y = dot(relative, own.Forward)
screenX = centreX + x / radius * width
screenY = centreY - y / radius * height
```

Start simple with GPS/manual contacts before IGC.

---

### 16.7 User interface / config features

1. **Theme config**
   ```ini
   [theme]
   style=StockBoard
   accent=Amber
   pb_screen=true
   ```

2. **Refresh config**
   ```ini
   [performance]
   lcd_update=Update10
   scan_every=100
   max_transfers_per_run=8
   ```

3. **Alias commands**
   ```text
   Stock=Component
   Wide=Component:1:14:left
   Vertical=Component:1:8:top
   ```

4. **Debug screen**
   ```text
   IndustrialDebug
   ```

---

## 17. Development rules for future ChatGPT sessions

When continuing AGM:

1. Always generate a **full PB-ready `.cs` file** when changing code.
2. Say clearly whether a file is:
   - PB-ready full script
   - addon/module only
   - markdown/reference only
3. Keep `[AGM-S]` for test LCDs unless user says finalise tags.
4. Keep `{AGM-Main}` for PB screen only.
5. Do not reuse `[LCD]` while testing unless deliberately testing AutoLCD compatibility.
6. Avoid asking unnecessary clarifying questions; make the best patch and provide exact setup instructions.
7. Include LCD names and Custom Data examples.
8. Mention if the script has not been tested inside the in-game PB editor.
9. Keep code paste-ready for Space Engineers PB editor.
10. Preserve RevGamer as author.

---

## 18. Current preferred testing commands

After pasting a new AGM script:

```text
reload
```

then:

```text
rescan
```

then optionally:

```text
reboot
```

If sorting should be safe/off during display testing:

```ini
enable_sorting=false
```

---

## 19. Current naming convention

PB:

```text
PB AutoGrid Manager {AGM-Main}
```

External LCDs:

```text
LCD Components Left [AGM-S]
LCD Components Right [AGM-S]
LCD Ore Left [AGM-S]
LCD Ore Right [AGM-S]
LCD Ingots [AGM-S]
LCD Ammo [AGM-S]
```

Cargo:

```text
Cargo Ores [Ore]
Cargo Ingots [Ingot]
Cargo Components [Component]
Cargo Ammo [Ammo]
Cargo Tools [Tool]
Cargo Bottles [Bottle]
Cargo General [Inventory]
Cargo Locked [Locked]
```

---

## 20. Current desired UX

The script should feel like a real in-universe industrial logistics OS:

```text
AGM
AutoGrid Manager
by RevGamer
```

It should be easy to use:

- name PB with `{AGM-Main}`
- name LCDs with `[AGM-S]`
- put one command in LCD Custom Data
- run `reload`
- run `rescan`

It should look polished enough for screenshots and eventual Workshop presentation, while still remaining practical for servers.

