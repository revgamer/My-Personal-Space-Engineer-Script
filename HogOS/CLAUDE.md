# CLAUDE.md -- HogOS

AI agent instructions for the HogOS script. Read this before touching any file in this folder.

---

## What HogOS Is

HogOS (Hog Operating System) is a Programmable Block LCD management script
for RevGamer's family of mining ships: GroundHog, SpaceHog, and HydroHog.
It drives a 4-surface cockpit display using a custom sprite painter
(HogPainter) built on the ExcavOS radial gauge approach.

Current version: **2.0**
Source file: `HogOS\Scripts\HogOS.cs`

---

## File Structure

```
HogOS\
  Scripts\
    HogOS.cs       -- formatted source (the file you edit)
  CLAUDE.md        -- this file
```

There is no minified version or build step for HogOS. The formatted source
is pasted directly into the Programmable Block editor in-game.

---

## Required Reading Before Any Edit

Per PROJECT_INSTRUCTIONS.md and SE-SCRIPTING-RULES.md:

1. Read the current `HogOS.cs` from disk before making any change. Never
   patch from memory or a stale copy.
2. Read `SE-SCRIPTING-RULES.md` in the Reference folder for API rules,
   C# version constraints, and confirmed runtime behaviours.
3. Run brace-balance validation (comment/string-aware Python parser) before
   writing the final file.
4. Verify total character count is under 100,000 before presenting the script.

---

## Screens

| Screen name | Config value | Purpose |
|---|---|---|
| Splash | `Splash` | Boot logo (MinerIcon_3); auto-switches to dock status panel when `[HogOS-Dock]` connector is Connected or Connectable |
| Loading | `Loading` | Immersive boot sequence shown on cockpit entry; auto-disposes when complete |
| Power | `Power` | Battery level, charge status, ETA + reactor fuel level, online count, net MW flow |
| OreCargo | `OreCargo` | Ore inventory list sorted by mass descending, with ore icons and mass labels |
| Weight | `Weight` | Two ExcavOS-style radial gauges: lift thrust usage (top) and cargo capacity (bottom) |
| Blank | `Blank` | Turns the surface off; also used as the safety default for unknown screen names |

Utility, Drills, and DrillManager were removed in v2.0. Do not re-add them
without explicit instruction.

---

## Dock Status Panel

The Splash screen auto-switches to a dock panel when any connector whose
`CustomName` contains `[HogOS-Dock]` has status `Connected` or `Connectable`.
It reverts to the boot logo when the connector is unconnected.

The dock panel shows:
- STATUS: LOCKED (green) or CONNECTABLE (amber)
- DOCKED TO: the other grid's `CubeGrid.CustomName` (only when Connected)
- CONNECTOR: the connector block's own `CustomName`

Grid name is read via `conn.OtherConnector.CubeGrid.CustomName`. Always
null-check `OtherConnector` before accessing it.

---

## Custom Data Setup

Add to any LCD or Cockpit Custom Data:

```
[HogOS]
Surface0=Splash
Surface1=Power
Surface2=OreCargo
Surface3=Weight
```

Optional keys (all default to empty string / 0.9):

```
[HogOS]
CargoTrackGroupName    =
LiftThrustersGroupName =
StopThrustersGroupName =
LiftThresholdWarning   = 0.9
```

The PB's own surface 0 is always forced to Splash regardless of Custom Data.
Other blocks (LCDs, cockpits) use their Custom Data surface assignments.

To enable dock-aware Splash screens, tag a connector's name:
`[Connector Name] [HogOS-Dock]`
No Custom Data entry needed -- the tag in the block name is enough.

---

## HogPainter Rules

HogPainter is a static helper class for all sprite drawing. All coordinates
are surface-local (0,0 = top-left). The painter adds `_offset` internally --
never add `vp.X`/`vp.Y` manually outside the painter.

Key methods:

| Method | Notes |
|---|---|
| `Setup(surface, frame)` | Must be called first in every Draw method. Sets Width, Height, Center, colour palette. |
| `DrawHeader(frame, margin, name, angle, color)` | Draws the standard HogOS header; returns Y position after it (~34px) |
| `Radial(pos, size, value, subText, bars, flip)` | ExcavOS-style arc gauge. `flip=true` for bottom gauge (arc opens upward). |
| `FilledRect`, `Border`, `Divider` | Basic primitives |
| `Text`, `TextCentered`, `TextRight` | Text with surface-local position |
| `Sprite`, `SpriteCentered` | Texture sprites with surface-local position |
| `ProgressBar` | Bordered fill bar with optional icon |
| `Measure(text, fs)` | Returns pixel size of a string at a given font scale |

`_measSb` is a plain (not `static readonly`) `static StringBuilder` field.
Colour fields (`Primary`, `Accent`, etc.) are plain `static` fields reassigned
in every `Setup()` call.

---

## SE API Rules That Apply Here

These are confirmed hard limits from testing. Do not work around them:

- **No `static readonly` fields.** Compile error in SE PB scripts. Use plain
  `static` (reassigned on update) or instance fields.
- **No field-level `new Color()` initializers.** Must be inside a method.
- **No C# 7+ syntax.** No inline `out var`, no `$"..."` interpolation, no
  tuples. Use `string.Format(...)` and pre-declared `out` variables.
- **No `namespace`, no `using` statements, no class wrapper.** Paste class
  body members only.
- **Plain ASCII in all string literals.** No em dashes, arrows, or smart
  quotes -- the minifier will fail. Comments may contain non-ASCII.
- **`static readonly` on arrays** (e.g. `Quotes[]` in LoadingScreen) **is
  also forbidden.** Use a plain instance field instead.

---

## What Not to Change Without Explicit Instruction

- The `[HogOS-Dock]` tag name
- The `OreCargo` screen NAME value (changed from v1.4's `CargoOre`)
- The logo path `Textures\FactionLogo\Miners\MinerIcon_3.dds`
- The `LiftThresholdWarning` default of 0.9
- The `BOOT_TIME` constant of 3.0f
- The update frequency: `Update10 | Update100`
- The 19-step pipeline architecture inherited from the v1.4 base

---

## Verification Checklist Before Presenting Any Change

Per PROJECT_INSTRUCTIONS.md mandatory final review:

1. Every `{}`, `()`, `[]` pair is balanced -- use a comment/string-aware parser
2. Every variable, method, class, and interface reference is defined
3. No `static readonly` field declarations
4. No non-ASCII characters in string literals
5. No C# 7+ syntax
6. `OtherConnector` is null-checked before use
7. All screen `NAME` constants match the `ScreenFactory` switch cases exactly
8. Total character count is under 100,000
9. Trace the Splash -> IsDocked -> DrawDockPanel and Splash -> DrawLogo paths

---

## Do Not Touch

- `F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\` --
  reference scripts are read-only; never modify them
- Any other script folder (AGM, RTC, Horizon Sentinel, RNB) -- changes to
  HogOS must stay inside the HogOS folder
- Do not start coding new features until explicitly told to
