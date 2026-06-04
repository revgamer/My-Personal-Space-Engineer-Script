# Phase 2 Script Study

## Source Scripts

User-provided study scripts:

- **Automatic LCDs 2** by MMaster
- **Fancy Status Displays / Fancy Bar Display**
- **InfoGraphx** by SwiftyTheFox
- **InfoGraphx positioning/config reference**

These scripts should be used as design inspiration only. Horizon Sentinel should not become a direct copy of any one script. It should be smaller, ship-safety focused, and easier to configure for one vessel.

## Automatic LCDs 2 Study

Useful ideas:

- Uses a tag system to find LCDs managed by the script.
- Reads script configuration from Programmable Block `CustomData`.
- Supports a boot sequence, which fits Horizon Sentinel's logo/initialization screen.
- Has many commands and display modes, proving that a PB script can be flexible through text configuration.
- Uses block lookup helpers and command parsing so users do not need to edit code for every screen.
- Handles many block types and inventory/status checks.

Ideas to borrow:

- A simple LCD tag such as `[HS]`.
- A boot/logo page before the main status screens.
- `CustomData` configuration for LCD names, groups, thresholds, and screen assignment.
- Friendly warnings when blocks or groups are missing.
- Page cycling with `next`, `prev`, and specific page commands.

Ideas to avoid:

- Too many commands at the start.
- Very large general-purpose display logic.
- Obfuscated/minified structure that is hard to maintain.

Horizon Sentinel direction:

Use the same philosophy of configurable LCDs, but keep Horizon Sentinel focused on ship readiness and safety.

## Fancy Bar Display Study

Useful ideas:

- Uses `CustomData` tags to decide what each screen should show.
- Supports multiple text surfaces on cockpits/control seats using surface indexes.
- Has bar display types:
  - wide horizontal bar
  - small horizontal bar
  - tall vertical bar
  - short vertical bar
  - icon modes
- Has explicit position control through tags like `Position(...)`.
- Calculates battery status from stored power and max stored power.
- Calculates cargo status from inventory current volume and max volume.
- Calculates jump drive charge from current stored power and max stored power.
- Calculates power producer load from current output and max output.
- Uses color changes based on percentage.
- Supports filtered inventory counting, which is useful for ammo.

Useful calculations:

- Battery percentage:
  - total current stored power / total max stored power
- Cargo percentage:
  - total current inventory volume / total max inventory volume
- Jump drive charge:
  - total current stored power / total max stored power
- Power generation load:
  - total current output / total max output
- Ammo:
  - scan weapon or cargo inventories and count matching ammo items

Ideas to borrow:

- Horizontal bars for cargo, power, fuel, and ammo.
- Wide bar format for main screens.
- Small bar format for compact cockpit/control seat screens.
- Grouped totals rather than listing every block first.
- Positioning using a normalized layout grid so screens stay readable.

Ideas to avoid:

- Huge tag language for phase 3.
- Too many display modes before the core safety logic exists.

Horizon Sentinel direction:

Use a small internal drawing library:

- `DrawBar`
- `DrawGauge`
- `DrawStatusLine`
- `DrawWarning`
- `DrawHeader`

Do not start with a full general Fancy Display clone.

## InfoGraphx Study

Useful ideas:

- Has a reusable `Graph` class for bars, gauges, hexagons, text, and icons.
- Separates graph appearance from graph data updates.
- Uses graph types:
  - `GAUGE`
  - `BAR`
  - `HEXAGON`
  - `TEXT`
  - `ICON`
- Uses content types:
  - battery
  - power generation
  - hydrogen
  - oxygen
  - storage
  - air pressure
  - jump drive
  - working count
- Uses source types:
  - block name
  - block group
- Supports graph color thresholds with `AddColorPoint`.
- Uses icons such as:
  - `IconEnergy`
  - `IconHydrogen`
  - `IconOxygen`
  - storage square icon
  - arrow icon for jump
- Draws graphs with `MySpriteDrawFrame`, which matches our Horizon Sentinel logo code.

Useful calculations:

- Hydrogen tank percentage:
  - sum `Capacity * FilledRatio` for hydrogen tanks
  - divide by total hydrogen tank capacity
- Oxygen tank percentage:
  - sum `Capacity * FilledRatio` for oxygen tanks
  - divide by total oxygen tank capacity
- Air pressure:
  - average air vent oxygen level or room pressure result
- Jump drive:
  - sum `CurrentStoredPower`
  - divide by sum `MaxStoredPower`
- Battery:
  - sum `CurrentStoredPower`
  - divide by sum `MaxStoredPower`
- Storage:
  - sum inventory current volume
  - divide by sum inventory max volume

Ideas to borrow:

- Reusable graph widget object.
- Graph update definitions: source name, content type, source type.
- Threshold-based colors.
- Gauge chart for jump drives.
- Horizontal bar chart for fuel/cargo/power/ammo.
- Screen target assignment by LCD name plus surface index.

Ideas to avoid:

- Exposing a very complex config language too early.
- Rebuilding every graph type if Horizon Sentinel only needs bars, gauges, and text in phase 3.

Horizon Sentinel direction:

Create a smaller `HsGraph` or `Widget` system:

- `WidgetKind.Bar`
- `WidgetKind.Gauge`
- `WidgetKind.Text`
- `WidgetKind.Status`

Then map ship data into widgets.

## Positioning Study

Important display lessons:

- Always use `surface.TextureSize`, `surface.SurfaceSize`, and viewport offset.
- Draw relative to the viewport, not hardcoded absolute LCD coordinates only.
- Control seat/cockpit screens may need surface index handling.
- Some blocks have unusual screen margins, so a viewport helper is useful.
- Use stable positions for repeated widgets so screens do not jump around.

Recommended Horizon Sentinel coordinate style:

- Calculate `unit = min(viewport.Width, viewport.Height)`.
- Use percentages of viewport size for layout.
- Define rows/columns:
  - header row
  - main gauge area
  - bar list area
  - warning footer
- Use compact cockpit layout separately from large LCD layout.

## Safety Logic Lessons

The study scripts are display-heavy, not flight-safety-heavy. Horizon Sentinel needs additional logic beyond them:

- thrust direction and lift margin
- ship mass and cargo mass
- natural gravity
- fuel reserve for descent/ascent
- gyro status
- connector/landing gear lock warning
- damage state for critical systems
- planet entry warning

## Phase 2 Conclusions

Horizon Sentinel should combine:

- AutoLCD-style configuration and LCD discovery
- Fancy Display-style horizontal bars and inventory/power calculations
- InfoGraphx-style graph widgets and gauge charts
- Our existing Horizon Sentinel logo style
- New ship-safety logic focused on takeoff, planet entry, and readiness

The result should be a purpose-built ship monitor, not a generic all-block display script.
