# ExcavOS Boot and Screen Style Reference

## What ExcavOS Does

ExcavOS treats LCD output as two layers:

- Normal assigned screens: `ExcavOS`, `Cargo`, `Weight`, `Utility`, etc.
- Temporary immersive screens: `LoadingScreen` and `LockScreen`.

The loading screen is not just a permanent page. It is created temporarily when cockpit immersion triggers, then removes itself after its timer finishes.

## Boot / Loading Flow

The important classes are:

- `RegisteredProvider`
- `LoadingScreen`
- `LockScreen`
- `ScreenHandler`

Each cockpit/LCD provider keeps two dictionaries:

```text
_screenHandlers      permanent page per surface
_immersiveHandlers   temporary overlay page per surface
```

When `EnableImmersion=True` and the block is a cockpit:

1. If the player enters the cockpit:
   - `WasUnderControl` changes from `false` to `true`.
   - Every configured surface gets a new `LoadingScreen`.
2. If the player exits the cockpit:
   - `WasUnderControl` changes from `true` to `false`.
   - Every configured surface gets a new `LockScreen`.
3. On every update:
   - If an immersive handler exists, it draws instead of the normal page.
   - If `ShouldDispose()` returns true, the immersive handler is removed.
   - The normal assigned screen comes back automatically.

This is the key pattern HogOS should copy.

## LoadingScreen Behavior

`LoadingScreen` stores:

```text
loadingStart
loadingTime
quotesPerLoading
currentQuote
quote
```

On creation:

- Start time is the script time accumulator.
- Loading duration is randomized between about 2 and 3 seconds.
- The number of phrase changes is randomized.

During draw:

- Calculate progress from elapsed time divided by loading duration.
- Draw title at top.
- Draw a central miner faction icon.
- Draw a short fake initialization phrase near the bottom.
- Draw a progress bar at the bottom.

Example phrases:

```text
Booting
Praying to Clang
Counting stones in cargo
Doing important stuff
Generating phantom forces
Connecting dots
```

The loading screen disposes itself when elapsed time is greater than `loadingTime`.

## ExcavOS Screen Style

ExcavOS style comes from the `Painter` helper. Screens do not manually repeat a lot of sprite boilerplate. They call small drawing helpers:

- `Painter.Text`
- `Painter.TextEx`
- `Painter.Sprite`
- `Painter.SpriteCentered`
- `Painter.RectangleEx`
- `Painter.FilledRectangleEx`
- `Painter.ProgressBar`
- `Painter.ProgressBarWithIconAndText`
- `Painter.Radial`
- `Painter.FullRadial`

## Color Model

ExcavOS reads colors from the actual LCD surface:

```text
PrimaryColor = surface.ScriptForegroundColor
BackgroundColor = surface.ScriptBackgroundColor
SecondaryColor = lighter/darker version of PrimaryColor
```

This means each LCD can be styled by changing its foreground/background colors in-game. The script does not hard-lock one theme everywhere.

For HogOS, this suggests:

- Keep a default AGM/HogOS color setup.
- But let each LCD surface color influence the final screen style.
- Use primary color for active/important information.
- Use secondary color for dividers, inactive values, bar outlines, and icon hints.
- Use background color as the full-screen base.

## Layout Rules

Most ExcavOS screens calculate layout from surface size:

```text
margin = 25 if width >= 512 else 5
gap = 10 if width >= 512 else 2
fontSize = 1.0 if width >= 512 else 0.8
```

That is why it survives both large LCDs and cockpit screens.

Common screen pattern:

1. Start at top-left margin.
2. Draw label on the left.
3. Draw value right-aligned.
4. Move down by measured text height plus gap.
5. Draw a thin separator line.
6. Repeat.

This is better than fixed large text rows for cockpit screens.

## Utility Screen Style

The `Utility` screen uses compact rows:

```text
Gravity Align      On (Level)
Cruise Control     On (20 m/s)
Stop               12.40m @ 2.10s
Jettison           Stone
```

Between each row it draws a thin secondary-color divider line.

At the bottom it uses three compact progress bars:

- Battery with `IconEnergy`
- Hydrogen with `IconHydrogen`
- Uranium with `MyObjectBuilder_Ingot/Uranium`

This is a strong pattern for HogOS Utility:

- Keep rows dense.
- Use right-aligned values.
- Use thin separators.
- Put small resource bars at the bottom.

## Cargo Screen Style

Cargo and ore screens:

- Use item sprites as icons.
- Draw item name left of row.
- Draw amount right-aligned.
- Draw a thin divider under each item.
- Show a centered empty-state icon and text if no cargo/ore exists.

For HogOS CargoOre:

- Keep the `Cargo` fill bar at top.
- Then list ores with item sprites if possible.
- Use thin lines between rows.
- Use a centered Stone/Ore icon when empty.

## Weight Screen Style

ExcavOS weight logic is more visual:

- It compares current ship load against lift capacity.
- It uses warning/danger thresholds.
- It can draw warning icons.
- It prefers compact visual information over many labels.

For HogOS Weight:

- Keep lift usage as the main visual.
- Use a vertical or radial capacity indicator, not only a flat bar.
- Show status text: `READY`, `HEAVY`, `OVERLOAD`, `NO LIFT`.
- Keep cargo off this screen unless needed; cargo belongs on CargoOre.

## Recommended HogOS Changes

### 1. Replace Boot Overlay With ExcavOS-Style Immersive Handler

Current HogOS has a timed boot overlay in `ScreenSys`.

Better ExcavOS-like design:

```text
normalScreens[surface]      assigned HogOS page
temporaryScreens[surface]   boot/lock/alert overlay
```

Then:

- On PB compile/start: add `BootScreen` temporary handler for each configured surface.
- On cockpit entry: add `BootScreen` temporary handler.
- On cockpit exit: optionally add `LockScreen` temporary handler.
- On `reboot`: add `BootScreen`.
- Once boot screen finishes, remove it and return to normal pages.

### 2. Use Surface Colors

HogOS currently uses hardcoded HUD colors. To feel like ExcavOS, it should read:

```text
surface.ScriptForegroundColor
surface.ScriptBackgroundColor
```

Then derive:

```text
Primary = foreground
Background = background
Secondary = lighter/darker primary
Dim = transparent secondary
```

This lets users theme each HOG miner without editing code.

### 3. Make Boot Screen More Visual

ExcavOS loading screen is simple:

```text
Top:       HogOS
Center:    mining/HOG icon or text mark
Bottom:    short loading phrase
Bottom:    progress bar
```

Avoid table rows during loading. The screenshot showed row text colliding on cockpit LCDs. A better HogOS boot screen should be:

```text
HogOS
v2.0

[simple HOG/mining emblem]

Profile scan...
[progress bar]
```

Only show one phrase at a time.

### 4. Keep Screens Dense and Separator-Based

For cockpit displays:

- Use small margins.
- Use measured text height.
- Use left label and right value.
- Use thin secondary-color separators.
- Avoid big panels inside panels.

### 5. Add Immersion Config

Recommended PB Custom Data:

```ini
[Boot]
Enabled=True
Seconds=3
OnCockpitEnter=True
OnCockpitExitLock=False
RandomPhrases=True
```

Recommended display Custom Data:

```ini
[HogOS]
EnableImmersion=True
Surface0=Power
Surface1=CargoOre
Surface2=Weight
Surface3=Utility
```

This mirrors the ExcavOS `EnableImmersion` idea while keeping HogOS sections clean.

## Short Implementation Plan for HogOS

1. Add a `TempScreen` concept to `ScreenSys`.
2. Move boot drawing into a small `DrawLoading` function that only has:
   - title
   - version
   - optional icon/emblem
   - one loading phrase
   - progress bar
3. Trigger temporary loading:
   - constructor/startup
   - cockpit entry
   - `reboot`
4. Optional lock screen on cockpit exit.
5. Update `Hud.Begin` to use the surface foreground/background colors.
6. Rework screen rows to match ExcavOS:
   - dynamic margin/gap/font
   - left/right rows
   - thin separator lines
   - icon progress bars where useful

## Bottom Line

ExcavOS feels polished because it has:

- Temporary immersive screens
- A small reusable painter
- Surface-driven colors
- Responsive cockpit/LCD sizing
- Dense left-label/right-value rows
- Thin separators
- Icon-based visual elements

To make HogOS feel similar, copy those patterns, not the whole script.
