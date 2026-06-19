# Farm Plot Status LCD Script v10.27 — Reference

> **Study reference only.**
> Single-file PB script, readable (non-minified) source, 54,163 characters,
> 1,795 lines. Internal version string: "v10.27 Tight Sprite Spacing".
> Settings version counter `SETTINGS_VERSION = 27`.

---

## What It Does

A farm-plot monitoring dashboard: it scans every Farm Plot block tagged (by
name or block group) `[Farm Blocks]`, reads each plot's planted/alive/ready
state through the real `IMyFarmPlotLogic` component when available (falling
back to DetailedInfo text-parsing for non-vanilla or modded farm blocks that
don't expose that interface), aggregates the counts into a single status
verdict, and paints that verdict plus a per-state breakdown and an optional
water/irrigation percentage onto every LCD tagged `[Farm LCD]`. It also
periodically cycles farm plots off/on to force a state refresh, since some
farm-plot implementations only update their reported state when toggled.

This is a "traffic light" style status board, not a per-plot grid display —
all plots tagged under one PB's farm group are summarized into one verdict
(`NO FARM BLOCKS FOUND` / `CROP PROBLEM` / `NO CROPS` / `FULLY GROWN` /
`GROWING`) shown identically on every linked LCD.

---

## Status Verdict Logic

```
total == 0                          -> "NO FARM BLOCKS FOUND" (red)
dead > 0                            -> "CROP PROBLEM"          (red)
cropCount == 0 (nothing planted)    -> "NO CROPS"               (red)
ready > 0 AND no growing/dead/unknown
    AND (not requiring full-plant OR all plots filled)
                                     -> "FULLY GROWN"           (green)
otherwise                            -> "GROWING"               (blue)
```

`REQUIRE_ALL_PLOTS_PLANTED_FOR_GREEN` (off by default) optionally withholds
the green "FULLY GROWN" verdict until every single plot slot is filled, not
just every *planted* plot being ready — useful if partially-empty farms
should still nag the player rather than show all-clear.

---

## Per-Plot State Detection (Two Paths)

### Path A — Real Farm Logic (`GetFarmInfo`)
When a block exposes `IMyFarmPlotLogic` (found by iterating
`functional.Components` and casting), the script reads `IsPlantPlanted`,
`IsAlive`, `IsPlantFullyGrown`, `OutputItem.SubtypeName`, `OutputItemAmount`,
and `AmountOfSeedsRequired` directly — no text parsing needed. Water ratio is
read from a co-located `IMyResourceStorageComponent.FilledRatio` if present.
All property reads are wrapped in `try/catch` since not every implementation
guarantees every property is safe to read in every state.

### Path B — Text-Parsing Fallback (`GetPlotStateFromText`)
For any block that doesn't expose the real interface (older blocks, certain
mods), the script concatenates `CustomName + DetailedInfo + CustomData`,
lowercases it, and searches for known phrase sets (`DEAD_WORDS`,
`READY_WORDS`, `EMPTY_WORDS`, `GROWING_WORDS` — all configurable via the
`[Keywords]` CustomData section) plus a `"growth progress:"` /
`"growth:"` / `"progress:"` percentage label scrape
(`GetPercentAfterLabel`). It also tracks **previous** growth/crop values per
block (`lastGrowthById`/`lastCropById`, persisted to `Storage`) so it can
detect a 100% -> low% transition as "this plot was just harvested and
replanted" and correctly report `Growing` instead of misreading a brand new
seedling as still being the old finished crop.

### Manual Harvested Override
The `harvested` console command snapshots every currently-`Ready` plot's
EntityId into `harvestedOverrideIds`, which makes `GetPlotState` report those
specific plots as `Empty` (i.e. "already harvested, ignore the ready state")
until either the plot's growth drops below ~100% again (new planting
detected) or the `unharvest` command clears the whole override list. This
exists because in survival/multiplayer, plots can sit "ready" for a while
after a manual harvest action that the farm-plot logic itself doesn't
register as "now empty" until something else changes — the override lets the
player tell the dashboard "I already took this one" without needing to wait.

---

## Auto-Toggle Refresh

Some farm plot implementations apparently only push their internal state
forward when they are toggled off and back on (a known quirk worth flagging
if you re-use this technique elsewhere). `HandleAutoToggleRefresh()` runs a
small state machine independent of the main display logic: every
`TOGGLE_EVERY_TICKS` (default 18) ticks it disables every farm block for
`TOGGLE_OFF_TICKS` (default 1) tick, then re-enables them. While toggled off,
if `HOLD_DISPLAY_DURING_REFRESH` is true (default), the LCDs keep showing the
*previous* cached display values (`lastStatusColor`, `lastTotal`, etc.)
rather than flashing a misleading "no crops" reading during the brief window
where the plots are intentionally disabled.

---

## LCD Rendering — Pure Sprite Renderer

No `ContentType.TEXT_AND_IMAGE` / `WriteText` path is used for the farm
status display itself (only the `debug` command's text dump uses
`WriteText`, as a deliberate fallback for readability while debugging). The
normal display is 100% `MySpriteDrawFrame` sprites:

- A solid-color background rect sized to the real visible **surface size**
  (not texture size) so sloped/corner/wide LCDs don't get the background
  miscentered or clipped (`GetSurfaceMetrics`).
- Status/count/water text drawn twice per line — once as a 4-or-8-direction
  outline pass in `COLOR_TEXT_OUTLINE`, then once as the real text in
  `COLOR_TEXT` on top — for legibility against the colored background
  without needing native LCD background color writes.
- Two layout modes selected automatically by aspect ratio
  (`USE_WIDE_LAYOUT` + `WIDE_LAYOUT_ASPECT` threshold, default 1.65):
  `PaintStackedSpriteText` (4 centered rows) for normal/tall panels, or
  `PaintWideSpriteText` (two-column: status+count on the left, breakdown+water
  on the right) for ultra-wide panels.
- `FitTextScale` uses `MeasureStringInPixels` to shrink any line that would
  overflow its allotted box, so long status text or large plot counts never
  get clipped off a small LCD.

### Dedicated-Server Sprite Sync Workaround
`AddFrameSyncBreaker` and the small per-call scale jitter inside
`AddTextSprite` are explicitly commented as a "Whiplash-style" workaround: on
a dedicated server, changing only some text/colors in a `MySpriteDrawFrame`
between calls can apparently cause some clients to only receive a partial
sprite update (stale text staying on screen). By varying the number of
leading empty sprites per frame (1-3, cycled by `renderFrame + surfaceId`)
and nudging each text sprite's scale by a tiny imperceptible amount
(`±0.0007`) every other frame, every sprite index and parameter changes
slightly every tick, which reportedly forces the client to treat it as a real
update rather than a no-op. This is a known community workaround pattern
(also referenced in `AVOID_SCRIPT_COLOR_WRITES`, which similarly avoids
`ScriptBackgroundColor`/`ScriptForegroundColor` writes in favor of painting
the background as a plain sprite instead).

---

## Console Commands

| Argument | Effect |
|---|---|
| `setup` | Force-overwrite CustomData with defaults, reload, refresh, re-enable farms if configured. |
| `reload` | Re-read CustomData and refresh the block list without touching defaults. |
| `refresh` | Re-scan for farm/LCD/water blocks (tags or groups) without reloading settings. |
| `debug` | Persistent debug mode: dumps full plot info (first 3 plots) and config state to every LCD via `WriteText`, plus `Echo`. |
| `normal` | Exit debug or LCD-test mode, return to the normal sprite dashboard. |
| `harvested` / `clear` | Mark all currently-ready plots as manually harvested (suppress their green state). |
| `unharvest` / `unclear` | Clear all harvested overrides. |
| `forceon` | Cancel any in-progress auto-toggle-off cycle and force every farm block back on immediately. |
| `lcdtest` | Paint a calibration pattern (border box, crosshair, surface/texture pixel dimensions) on every found LCD — useful for checking sloped/corner LCD viewport alignment. |

---

## Block Discovery

Three independent scans feed into the same dedup'd lists, all gated by
`SAME_CONSTRUCT_ONLY` (default true, restricting to `Me.IsSameConstructAs(b)`
so a docked ship's LCDs/farms aren't accidentally swept in):

1. Direct block name/CustomName tag match (`[Farm Blocks]`, `[Farm LCD]`,
   `[Farm Water]` by default, all renameable via the `[Tags]` CustomData
   section).
2. Block **group** name match — any group whose name contains the same tag
   contributes its member blocks the same way a tagged individual block
   would.
3. A farm block is accepted even *without* the tag if `IsFarmPlot()` returns
   true (real `IMyFarmPlotLogic` component present, or
   `BlockDefinition.SubtypeName` contains `"FarmPlot"`) — so vanilla/modded
   farm plots are picked up automatically, while the tag remains the
   mechanism for opting in non-obvious blocks (e.g. tagging a custom
   container as a water source) or scoping to a specific subset of plots on
   a grid with multiple farms.

Water blocks require `InventoryCount > 0` to be accepted (so a tag placed on
a block with no inventory, by mistake, doesn't silently report 0/0 = "N/A"
forever without at least being discoverable as a configuration error during
debugging).

---

## Persistence (`Storage` Format)

```
G|<EntityId>|<growthPercent>
C|<EntityId>|<cropTypeString>
H|<EntityId>
```

One line per tracked value; `G`/`C` lines feed the text-parsing fallback's
"was this plot just replanted" detection, `H` lines are the harvested
override set. Re-loaded on `Program()` via `LoadStorage()`.

---

## TypeId / API Surface Used

This script does not parse any inventory item TypeIds at all — it has no
need to identify ore/ingot/component stacks. The only inventory-adjacent code
is the water tank percentage calculation, which sums raw
`IMyInventory.CurrentVolume` / `MaxVolume` across every inventory index on
every tagged water block — a volume ratio, not an item-type lookup.

The one block-classification check worth flagging for the TypeId-verification
checklist is **not** a TypeId check at all, but a `SubtypeName` substring
fallback:

```csharp
return block.BlockDefinition.SubtypeName.IndexOf("FarmPlot",
    StringComparison.OrdinalIgnoreCase) >= 0;
```

This only runs as a fallback when `IMyFarmPlotLogic` cast fails, so it is a
defensive secondary signal, not the primary classification path — correct
usage, no TypeId category confusion present.

Other block-side APIs used: `IMyFunctionalBlock` (`.Components`, `.Enabled`,
`.ApplyAction`), `IMyResourceStorageComponent`, `IMyTextPanel`,
`IMyBlockGroup`, and `IMyInventory`. All are standard PB API surface.

---

## Notable Implementation Quirks Worth Knowing Before Reusing This Pattern

- **`ApplyAction("OnOff_On"/"OnOff_Off")` is called in addition to setting
  `.Enabled` directly** (`SetFarmBlocksEnabled`). This looks redundant at
  first glance — setting `Enabled` should be suffient — but is likely there
  because some block/mod implementations only react to the terminal action
  call, not a raw property set, for triggering their internal "just got
  turned on" refresh logic. If adapting this toggle-refresh trick for a
  different block type, keep both calls rather than assuming one suffices.
- **Two completely separate settings-versioning mechanisms exist
  side-by-side**: the file-level comment header version ("v10.27") and the
  CustomData `[Script] Version=27` integer that `EnsureCustomData` checks
  against `SETTINGS_VERSION` to decide whether to force-regenerate defaults.
  Bumping one without the other will not trigger a CustomData reset on
  existing deployments — both need to move together if a future settings
  schema change should force a clean defaults rewrite.
