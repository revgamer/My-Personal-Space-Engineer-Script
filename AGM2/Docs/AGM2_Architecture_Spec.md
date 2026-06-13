# AutoGrid Manager v2.0 — Architecture & Configuration Specification

Author: RevGamer
Status: APPROVED SPEC — do not begin coding without reading this document first
Created: 2026-06-13

---

## 1. Overview

AGM v2.0 is a full grid manager for Space Engineers programmable blocks.

Core responsibilities:
- Category sorting — move items into correctly typed cargo containers
- [Stock] containers — fill/drain individual containers to per-item quotas
- Autocrafting — queue assemblers to maintain configured item amounts
- Production monitoring — assembler and refinery status dashboards
- Power, fuel, and alert systems — Phase 2, ported from v1.5

AGM v2.0 is a clean rebuild. It does not share code with AGM v1.5.
The existing Scripts/AGM.cs is preserved as a reference only.

---

## 2. Scripting Constraints

All code must follow Space Engineers programmable block rules:

- C# 6 only. No tuples, no string interpolation ($""), no inline out declarations
- No namespace wrapper, no class wrapper, no using statements in the paste
- No static fields (static methods are fine)
- No System.IO, System.Net, System.Threading, System.Reflection
- Plain ASCII only in all string literals — no smart quotes, em dashes, accented chars
- Script size limit: 100,000 compiled characters after minification
- Instruction limit: 50,000 junctions per Main() call — use staged execution
- Reuse List<T> fields with .Clear() instead of allocating new lists in hot paths
- Never sort from IMyReactor, IMyGasGenerator, or IMyGasTank inventories
- Wrap all block access in try/catch — a broken block must never stop the loop

---

## 3. Grid Scope

AGM v2.0 manages the SAME GRID ONLY.

Filter: b.CubeGrid == Me.CubeGrid

Docked ships connected via a connector tagged [AGM:NoSort] in the connector block name
are automatically excluded. Their grids are added to a HashSet<IMyCubeGrid> exclusion set.

Rotors, pistons, and hinges attach subgrids. AGM does NOT manage subgrids —
same-grid filter excludes them automatically.

---

## 4. Tag System

### 4.1 LCD / Surface Blocks

Add an [AGM] section to the block's Custom Data.
Each line is a dashboard command with an optional page number.

Example — single screen, Ore stock page 1:

  [AGM]
  Ore Dashboard = page1

Example — second screen, same dashboard page 2:

  [AGM]
  Ore Dashboard = page2

Example — screen showing Component stock:

  [AGM]
  Component Dashboard = page1

Example — PB front screen (no command needed, drawn automatically):

  No Custom Data required. PB surface 0 always shows the boot/status animation.

Multiple commands on one screen are supported:

  [AGM]
  Core Dashboard = page1

All dashboard command names are listed in Section 7.

### 4.2 Category Containers

Add an [AGM] section to the cargo container's Custom Data.
The type= key identifies what category of items this container accepts.

  [AGM]
  type = Ore

  [AGM]
  type = Ingot

  [AGM]
  type = Component

  [AGM]
  type = Ammo

  [AGM]
  type = Tool

  [AGM]
  type = Bottle

  [AGM]
  type = Food

  [AGM]
  type = Seed

  [AGM]
  type = Ingredient

Optional: mark a container as Locked (AGM never touches it):

  [AGM]
  type = Component
  locked = true

Optional: mark a container as Hidden (excluded from stock counts):

  [AGM]
  type = Ammo
  hidden = true

### 4.3 [Stock] Containers

Add [Stock] anywhere in the cargo container's BLOCK NAME.
Example block names:

  [Stock] Main Supplies
  Cargo Container [Stock]
  [Stock] Ore Reserve

On first scan AGM writes a template into the container's Custom Data.
The template is wrapped in markers:

  === AGM Stock BEGIN ===
  ...
  === AGM Stock END ===

AGM reads only inside this section. Content outside the markers is never touched.
AGM never rewrites the amounts after the template is generated.
User edits the template and AGM reads it every scan.

### 4.4 Connector Exclusion Tag

Add [AGM:NoSort] to a connector's block name to exclude the docked grid.

  Connector [AGM:NoSort] Ship Dock
  [AGM:NoSort] Fighter Bay Left

Any grid connected to a tagged connector is added to the exclusion set.
Items are never pulled from or pushed to excluded grids.

### 4.5 Locked / Manual Containers (no [AGM] section)

If a container has no [AGM] section and no [Stock] in its name, AGM still
uses it as a source during sorting (items may be pulled from it) but will not
assign it as a destination unless it is already the correct category container.

To fully exclude a container from all AGM operations:

  [AGM]
  locked = true

---

## 5. [Stock] Container Custom Data Format

### 5.1 Generated Template

When AGM first finds a [Stock] container with no existing template, it writes:

=== AGM Stock BEGIN ===
; AutoGrid Manager v2.0 - Stock Container Template
; Edit the lines below. AGM will never rewrite this section.
;
; MODES:
;   ItemName = 5000        Target: fill to 5000, remove excess above 5000
;   ItemName = 5000M       Minimum: fill to 5000, keep excess (do not remove)
;   ItemName = 5000L       Limiter: do not add, remove above 5000
;   ItemName = All         Accept all of this item until container is full
;   ItemName = Disabled    AGM ignores this item in this container
;   Append P to pin item to top of stock dashboard: ItemName = 5000P
;   Modes can combine: ItemName = 5000MP  (min + pinned)
;
; CATEGORY FILTER (optional - limits what AGM sorts into this container):
;   category = Component
;
; PRIORITY (optional - lower number = higher priority, default 5):
;   priority = 1
;
; --- Uncomment and edit items below ---
;
; === Components ===
; SteelPlate = 5000
; Motor = 500
; Computer = 500
; Construction = 1000
; MetalGrid = 200
; LargeTube = 200
; SmallTube = 500
; InteriorPlate = 2000
; BulletproofGlass = 100
; Display = 50
; Detector = 20
; Girder = 200
; Gravity = 10
; Medical = 10
; PowerCell = 50
; RadioCommunication = 20
; Reactor = 10
; SolarCell = 50
; Superconductor = 20
; Thrust = 50
;
; === Ores ===
; Iron = 10000
; Nickel = 5000
; Cobalt = 5000
; Silicon = 3000
; Gold = 1000
; Silver = 1000
; Platinum = 500
; Uranium = 200
; Magnesium = 500
; Stone = 5000
; Ice = 5000
;
; === Ingots ===
; Iron = 5000
; Nickel = 2000
; Cobalt = 1000
; Silicon = 1000
; Gold = 200
; Silver = 500
; Platinum = 100
; Uranium = 50
; Magnesium = 100
; Stone = 1000
;
; === Ammo ===
; NATO_5p56x45mm = 2000
; NATO_25x184mm = 500
; Missile200mm = 500
;
; === Tools ===
; AngleGrinderItem = 5
; HandDrillItem = 5
; WelderItem = 5
; AngleGrinderItem = 5
;
=== AGM Stock END ===

### 5.2 Item Name Rules

Item names in the template use SubtypeId only (no MyObjectBuilder_ prefix).
AGM resolves the full MyItemType internally using ItemCatalog.
Matching is case-insensitive.

### 5.3 Mode Parsing

Parse each uncommented line inside the markers:
  Split on first '=' → key (item name), value (mode string)
  Strip whitespace from both sides
  Mode string parsing:
    Contains 'P' (case insensitive) → pinned = true, strip P from string
    Equals "All" (case insensitive)      → mode = All
    Equals "Disabled" (case insensitive) → mode = Disabled
    Ends with 'M' → mode = Minimum, parse numeric prefix
    Ends with 'L' → mode = Limiter, parse numeric prefix
    Is numeric only → mode = Target, parse as double
    Otherwise → log parse error, skip line

### 5.4 Multiple [Stock] Containers Sharing an Item

When two [Stock] containers both list the same item:
  Container with lower priority= number is filled first.
  Default priority = 5.
  Ties broken by block EntityId (deterministic).
  Overflow from priority-1 container goes to priority-2 container if it also stocks that item.
  Final overflow goes to the matching category container.
  If no category container exists, item stays in source and a warning is logged.

---

## 6. Autocrafting Configuration (PB Custom Data)

### 6.1 Format

Configured in the Programmable Block's own Custom Data.
AGM owns the [Autocrafting] section. User content outside this section is preserved.

  [Autocrafting]
  enabled = true
  assemble_margin = 5
  disassemble_margin = 10
  auto_disassemble = false
  sort_queue = true

  ; === Components ===
  SteelPlate = 5000
  InteriorPlate = 2000
  Motor = 500
  Computer = 500
  LargeTube = 200
  SmallTube = 500
  Construction = 1000
  MetalGrid = 200
  BulletproofGlass = 100
  Display = 50
  Detector = 20
  Girder = 200
  Gravity = 10
  Medical = 10
  PowerCell = 50
  RadioCommunication = 20
  Reactor = 10
  SolarCell = 50
  Superconductor = 20
  Thrust = 50
  Explosives = 10
  Canvas = 10

  ; === Ammo ===
  NATO_5p56x45mm = 2000
  NATO_25x184mm = 500
  Missile200mm = 500
  LargeCalibreAmmo = 200
  MediumCalibreAmmo = 500
  AutocannonClip = 500
  SmallRailgunAmmo = 100
  LargeRailgunAmmo = 50

  ; === Tools ===
  AngleGrinderItem = 5
  HandDrillItem = 5
  WelderItem = 5
  AngleGrinder2Item = 3
  HandDrill2Item = 3
  Welder2Item = 3
  AngleGrinder3Item = 2
  HandDrill3Item = 2
  Welder3Item = 2
  AngleGrinder4Item = 1
  HandDrill4Item = 1
  Welder4Item = 1

### 6.2 Margin Logic

assemble_margin = 5 means: only start crafting if stock < (quota - 5%).
Example: quota 1000, margin 5 → craft only when stock < 950.

disassemble_margin = 10 means: only disassemble if stock > (quota + 10%).
Example: quota 1000, margin 10 → disassemble only when stock > 1100.
auto_disassemble = false by default — user must enable explicitly.

### 6.3 Assembler Selection

AGM uses all assemblers on the same grid.
Assemblers with [AGM:Manual] in their block name are excluded.
Blueprint lookup uses IMyAssembler.CanUseBlueprint(MyDefinitionId).
Blueprint IDs are cached after first successful lookup.
If no blueprint is found for an item, log a warning and skip.

### 6.4 Queue Splitting

When sort_queue = true, AGM distributes queue items across assemblers
to avoid one assembler doing everything. Each assembler gets an equal share.

---

## 7. Dashboard Commands

All commands go in the [AGM] section of an LCD's Custom Data.

  [AGM]
  CommandName = pageN

Page number is optional. Default is page 1 if omitted.

### 7.1 Core / Status
  Core Dashboard         — grid name, AGM status, quick stats
  Alert Dashboard        — all system alert states in one view

### 7.2 Sorting / Logistics
  Logistics Dashboard    — container fill levels by category

### 7.3 Stock
  Inventory Dashboard = page1    — all items across all categories
  Ore Dashboard = page1
  Ingot Dashboard = page1
  Component Dashboard = page1
  Ammo Dashboard = page1
  Tool Dashboard = page1
  Bottle Dashboard = page1

### 7.4 Autocrafting
  Autocraft Dashboard = page1    — quota vs stock for all autocrafted items

### 7.5 Production
  Production Dashboard = page1   — assembler/refinery overview
  Production Dashboard = page2   — assembler details (current job)
  Production Dashboard = page3   — refinery details (current ore)

### 7.6 Power (Phase 2)
  Power Dashboard = page1        — battery/reactor/solar overview
  Power Dashboard = page2        — reactor refuel status
  Battery Dashboard              — battery charge control status

### 7.7 Fuel / Life Support (Phase 2)
  Fuel Dashboard                 — H2/O2 tanks, generators, vents

---

## 8. Architecture — Module Responsibilities

### 8.1 Scheduler

Update frequency: Update1 (every tick, ~16ms). Same as GOAT Sorter.
This gives the fastest possible sorting response on any grid size.
The instruction guard is critical at this frequency.

Fields:
  int _stage         — current pipeline stage (0..N)
  int _tick          — tick counter for rescan timing
  double _dt         — delta time from Runtime.TimeSinceLastRun

Behaviour:
  Main() runs every Update1 tick.
  Each tick runs one slice of the current stage.
  When a stage completes, advance _stage to next.
  When _stage reaches the last stage, reset to 0.
  Rescan happens every 1800 ticks (~30 seconds at Update1).
  If Runtime.CurrentInstructionCount > 35000, stop current stage,
    save resume position, continue next tick from saved position.
  This means small grids finish each stage in one tick (instant),
    large grids spread work across multiple ticks automatically.
  No hard move cap — instruction count is the only limiter.

Stages (Phase 1):
  0  BlockScanner.Scan()
  1  DockedGridFilter.Rebuild()
  2  InventoryCounter.Count()
  3  StockManager.Process()
  4  CategorySorter.Sort()
  5  AutocraftManager.Process()
  6  LcdRouter.Draw()

### 8.2 Config

Reads PB Custom Data once per rescan.
Parses [Autocrafting] section into:
  Dictionary<string, double>  _quotas     — SubtypeId -> target amount
  bool   _autocraftEnabled
  double _assembleMargin      (percent, default 0)
  double _disassembleMargin   (percent, default 0)
  bool   _autoDisassemble     (default false)
  bool   _sortQueue           (default true)

Preserves all content outside [Autocrafting] section.
Reports parse errors via Echo().
Never silently drops unknown keys — logs them as warnings.

### 8.3 BlockScanner

Runs GetBlocksOfType once per rescan cycle.
Filters: b.CubeGrid == Me.CubeGrid

Populates:
  List<IMyTerminalBlock>    _allBlocks
  List<IMyShipConnector>    _connectors
  List<IMyCargoContainer>   _cargo
  List<IMyAssembler>        _assemblers
  List<IMyRefinery>         _refineries
  List<IMyReactor>          _reactors
  List<IMyBatteryBlock>     _batteries
  List<IMyGasTank>          _tanks
  List<IMyGasGenerator>     _generators
  List<IMyAirVent>          _vents
  List<IMyTerminalBlock>    _lcds    — all IMyTextSurfaceProvider blocks with [AGM] in CustomData

Does NOT scan subgrids or docked ships.
Docked grid filtering is done by DockedGridFilter after this step.

### 8.4 DockedGridFilter

Reads _connectors list.
For each connector with [AGM:NoSort] in its CustomName:
  If status == Connected, add OtherConnector.CubeGrid to _excludedGrids.
Clears exclusion set before each rebuild.

_excludedGrids: HashSet<IMyCubeGrid>

Helper: bool IsExcluded(IMyTerminalBlock b) => _excludedGrids.Contains(b.CubeGrid)

### 8.5 ItemCatalog

Static lookup tables (no reflection, no runtime scanning).
Maps SubtypeId strings to:
  string Category    — "Ore", "Ingot", "Component", "Ammo", "Tool", "Bottle", "Food", "Seed", "Ingredient"
  string Icon        — full sprite ID e.g. "MyObjectBuilder_Component/SteelPlate"
  string DisplayName — human-readable name e.g. "Steel Plate"

Also provides:
  string GetCategory(MyItemType t)    — derives from TypeId suffix
  string GetIcon(MyItemType t)        — builds sprite ID from TypeId + SubtypeId
  string GetDisplayName(MyItemType t) — splits CamelCase SubtypeId

Unknown/modded items use:
  Category  = derived from TypeId suffix (best effort)
  Icon      = "IconInventory" (fallback)
  DisplayName = SubtypeId with CamelCase split

### 8.6 InventoryCounter

Counts total amount of every item type across all non-excluded, non-locked inventories.
Result: Dictionary<MyItemType, double> _totals

Skips: reactors, gas generators, gas tanks (never read their inventories).
Skips: locked containers ([AGM] locked=true).
Skips: excluded docked grids.

Also counts per-category totals for logistics dashboard:
  Dictionary<string, double> _categoryTotals   — category -> total count

### 8.7 StockManager

For each [Stock] container:
  1. Parse Custom Data between markers (read only, never write after template)
  2. For each configured item line:
     a. Count current amount in this specific container
     b. Apply mode logic:
        Target  → if amount < quota: pull from sources. if amount > quota: push excess to overflow
        Minimum → if amount < quota: pull from sources. never push excess
        Limiter → never pull. if amount > quota: push excess to overflow
        All     → pull from any source until container full. never push
        Disabled → skip

Overflow routing:
  1. Other [Stock] containers that also list this item (by priority order)
  2. Matching category container (type= matching item's category)
  3. Any untagged cargo container with space
  4. Nowhere — log warning

Transfer calls:
  srcInv.TransferItemTo(dstInv, srcSlot, dstSlot, true, amount)
  Check dst inventory not full before transfer (use CurrentVolume < MaxVolume * 0.97)
  Wrap in try/catch — failed transfer logs warning, continues

Max transfers per stage call: 10 (configurable, prevents instruction overrun)

### 8.8 CategorySorter

For each source inventory (all non-locked, non-excluded blocks):
  For each item in inventory:
    Determine category via ItemCatalog.GetCategory()
    Find destination: first [Stock] container that lists this item (by priority)
    If no Stock container: find category container (type= matching category)
    If no category container: skip (leave in place, no error)
    Transfer item to destination

Sorting priority order:
  1. [Stock] containers listing this item specifically
  2. Category containers matching item category
  3. Leave in place

Never pull from: reactors, gas generators, gas tanks.
Never pull from: locked containers.
Never pull from: excluded docked grids.
Max moves per stage call: 20 (prevents instruction overrun)

### 8.9 AutocraftManager

Reads _quotas from Config.
For each item in quotas:
  stock = InventoryCounter total for that item
  needed = quota - stock
  margin check: only queue if needed > quota * (assembleMargin / 100.0)
  Find blueprint via _bpCache or CanUseBlueprint scan
  Distribute queue across all non-manual assemblers equally

Disassembly (if auto_disassemble = true):
  excess = stock - quota
  disassemble threshold: excess > quota * (disassembleMargin / 100.0)
  Queue disassembly on assemblers not currently assembling

Blueprint cache:
  Dictionary<string, MyDefinitionId> _bpCache  (SubtypeId -> blueprint ID)
  Try standard blueprint ID first: MyObjectBuilder_BlueprintDefinition/SubtypeId
  Try Component suffix: SubtypeId + "Component"
  Try Magazine suffix: SubtypeId + "Magazine"
  If CanUseBlueprint returns true, cache and use
  If no blueprint found after attempts: log warning once per item, skip

### 8.10 LcdRouter

On each draw stage:
  For each block in _lcds:
    Parse [AGM] section of CustomData
    For each command line: dispatch to matching draw function
    Wrap entire block in try/catch — one broken LCD must not stop others

Surface access:
  If block is IMyTextSurfaceProvider:
    Surface index defaults to 0
    Surface index can be specified: Command = page1, surface=1
  Corner LCD: IMyTextPanel — single surface, treated as surface 0

### 8.11 DrawKit

Shared sprite helper methods. All are instance methods (no static).

  void PrepSurf(IMyTextSurface s)
    Sets ContentType = SCRIPT, ScriptBackgroundColor = COL_BG, BackgroundColor = COL_BG

  RectangleF VP(IMyTextSurface s)
    Returns viewport: new RectangleF((TextureSize - SurfaceSize) * 0.5f, SurfaceSize)

  RectangleF Inset(RectangleF r, float a)
    Shrinks rect by a on all sides

  void Fill(MySpriteDrawFrame fr, RectangleF r, Color c)
    Draws SquareSimple at rect center

  void Border(MySpriteDrawFrame fr, RectangleF r, Color c, float t)
    Draws 4 Fill calls for top/bottom/left/right edges

  void Txt(MySpriteDrawFrame fr, string text, float x, float y, Color c, float scale, TextAlignment al)
    Draws TEXT sprite at position

  void FitTxt(MySpriteDrawFrame fr, string text, float x, float y, Color c, float scale, TextAlignment al, float maxWidth)
    Scales text down if it would overflow maxWidth

  void Row(MySpriteDrawFrame fr, RectangleF panel, float y, string label, string value, Color vc)
    Draws one data row: background + border + label (left) + value (right)

  void Bar(MySpriteDrawFrame fr, RectangleF r, double pct, Color fill, Color bg)
    Draws a progress bar

  void Icon(MySpriteDrawFrame fr, string spriteId, float x, float y, float size, Color c)
    Draws a TEXTURE sprite with fallback to IconInventory if spriteId fails

### 8.12 Dashboards

One method per dashboard type. All take (IMyTextSurface s, int page) parameters.
Dashboards never store state between calls except scroll positions.

Phase 1 dashboards:
  DrawCoreDash(s, page)        — grid name, version, stage status, last sort stats
  DrawLogisticsDash(s, page)   — category container fill levels
  DrawOreDash(s, page)         — ore stock list with progress bars
  DrawIngotDash(s, page)       — ingot stock list
  DrawComponentDash(s, page)   — component stock list
  DrawAmmoDash(s, page)        — ammo stock list
  DrawToolDash(s, page)        — tool stock list
  DrawBottleDash(s, page)      — bottle stock list
  DrawInventoryDash(s, page)   — all categories combined
  DrawAutocraftDash(s, page)   — quota vs stock for autocrafted items
  DrawProductionDash(s, page)  — assembler/refinery status

Phase 2 dashboards (port from v1.5):
  DrawPowerDash(s, page)
  DrawFuelDash(s, page)
  DrawAlertDash(s, page)

---

## 9. Theme

All colors are defined as readonly Color fields in Program.

  COL_BG       = new Color(1, 8, 13)       -- near-black navy background
  COL_PANEL    = new Color(2, 18, 28)       -- slightly lighter panel bg
  COL_PANEL2   = new Color(3, 58, 78)       -- row background
  COL_ACCENT   = new Color(38, 239, 255)    -- bright cyan accent / border
  COL_ACCENT2  = new Color(112, 247, 255)   -- light cyan header text
  COL_TEXT     = new Color(126, 246, 255)   -- standard text
  COL_DIM      = new Color(44, 177, 195)    -- dimmed / secondary text
  COL_OK       = new Color(97, 255, 214)    -- green OK state
  COL_WARN     = new Color(255, 202, 34)    -- amber warning
  COL_BAD      = new Color(255, 79, 66)     -- red critical / error
  COL_PROG_BG  = new Color(18, 48, 32)      -- progress bar background
  COL_PROG_FILL= new Color(255, 204, 36)    -- progress bar fill (yellow)

LCD surface sizes:
  1:1 square panel: typically 512x512 surface area
  Corner LCD: typically 178x178 or smaller
  Cockpit surfaces: varies — DrawKit must handle any SurfaceSize

Layout adapts: all positions calculated from SurfaceSize, not hardcoded pixels.

---

## 10. Custom Data Safety Rules

1. AGM reads the [AGM] section from LCD and container Custom Data.
2. AGM reads the [Autocrafting] section from PB Custom Data.
3. AGM writes the Stock template ONLY inside === AGM Stock BEGIN === / END === markers.
4. All content outside AGM-owned sections is NEVER modified.
5. After the Stock template is written once, AGM never rewrites amounts.
6. If a [Stock] container already has BEGIN/END markers, AGM skips template generation.
7. Malformed lines inside a Stock section log a warning and are skipped.
8. Malformed lines inside [Autocrafting] log a warning and are skipped.
9. AGM never renames blocks.
10. AGM never clears or resets Custom Data unless explicitly commanded via run argument.

---

## 11. Run Arguments

  scan     — force immediate rescan of all blocks
  pause    — stop all sorting and autocrafting (monitoring continues)
  resume   — resume from pause
  reset    — clear internal caches, trigger full rescan (does not touch Custom Data)
  debug    — print detailed state to Echo for one cycle

---

## 12. Error Handling

  - Try/catch around every block access, inventory transfer, and Custom Data read
  - Errors logged to Echo() with block name and error type
  - One broken block never stops the pipeline
  - If instruction count exceeds 35,000, defer remaining items to next tick
  - Echo() always shows: current stage, last error (if any), tick count, move count

---

## 13. Stage Implementation Plan

### Stage 1 — Scaffold + Sorting (CURRENT TARGET)
  - Program skeleton (constructor, Main, Save)
  - Scheduler (stage counter, tick counter, rescan timing)
  - Config (parse [Autocrafting] from PB Custom Data)
  - BlockScanner (same-grid scan, typed lists)
  - DockedGridFilter ([AGM:NoSort] connector exclusion)
  - ItemCatalog (category/icon/displayname lookup)
  - InventoryCounter (total amounts per item type)
  - CategorySorter (move items to category containers)
  - DrawKit (sprite helpers)
  - LcdRouter (dispatch [AGM] commands to draw functions)
  - Basic dashboards: Core, Logistics, Ore, Ingot, Component

### Stage 2 — [Stock] Containers
  - StockManager (template generator, mode parsing, fill/drain)
  - Stock dashboards: all category stock views
  - Overflow routing

### Stage 3 — Autocrafting
  - AutocraftManager (quota config, blueprint lookup, queue distribution)
  - Autocraft dashboard

### Stage 4 — Production Monitoring
  - ProductionMonitor dashboards (3 pages)

### Stage 5 — Power / Fuel / Alerts (Phase 2)
  - Port power, fuel, alert systems from AGM v1.5
  - Power, fuel, alert dashboards

---

## 14. File Layout

  AGM2/
    Scripts/
      AGM2.cs          -- active development source
    Docs/
      AGM2_Architecture_Spec.md    -- this document
      AGM2_Setup_Guide.md          -- user setup guide (written after Stage 1)
      AGM2_Custom_Data_Reference.md
    README.md

The existing AGM/Scripts/AGM.cs is untouched until RevGamer explicitly approves replacement.

---

## 15. Approval

This specification must be approved by RevGamer before coding begins.

Approved: YES — RevGamer confirmed rebuild with this spec on 2026-06-13
