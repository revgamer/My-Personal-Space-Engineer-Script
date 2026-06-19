# Uploaded Scripts — 2026-06-18 Batch Summary

Five scripts were uploaded for documentation and reference-folder filing.
None of them are RevGamer-authored projects (AGM, RTC, Horizon Sentinel, RNB)
— all five are external reference scripts, same category as the existing
GOAT Sorter / IIM Sorter / ExcavOS entries already in `Reference\Scripts\`.

| Script | Size (chars) | Format | Reference doc |
|---|---|---|---|
| New Horizons | 99,830 | Minified, single line per logical block | `New_Horizons_Reference.md` |
| MSCS v3 — Supply Station | 42,828 | Minified | `MSCS_v3_Reference.md` |
| MSCS v3 — Order Station | 41,441 | Minified | `MSCS_v3_Reference.md` (shared doc) |
| MSCS v3 — Cargo Ship Controller | 21,194 | Minified | `MSCS_v3_Reference.md` (shared doc) |
| Farm Plot Status LCD v10.27 | 54,163 | Readable source (not minified) | `Farm_Plot_Status_LCD_Reference.md` |

All five were read completely (no skipped sections) before writing their
reference docs. TypeId verification was run across all five — no block
TypeId / item TypeId category confusion was found in any of them.

## What's New (relative to nothing — these are first-time additions)

These five scripts have no prior version in this project's memory or chat
history, so "what's new" means new to the Reference folder, not a diff
against an earlier revision:

- **New Horizons** is a full artificial-horizon flight HUD with an
  inter-ship datalink protocol, voxel collision warning, braking-thrust
  marker, 2D/3D dock alignment overlay, and a built-in local radar mode — all
  in one script. Closest comparison point in this project is Horizon
  Sentinel, but the architecture is different (continuous attitude-indicator
  render vs. paged dashboard) and there's no overlap in actual code.
- **MSCS v3** is a three-script dispatcher/drone/requester cargo network
  that sits on top of a separate "PMV4" production-management script via
  IGC broadcast only (never touching PMV4's files directly). It's the same
  general problem space as RNB (RevGamer's Nanobot manager) and AGM (station
  inventory), but solves multi-station *transport* rather than single-station
  sorting or nanobot construction.
- **Farm Plot Status LCD v10.27** is a small, focused status-board script:
  one verdict (empty/growing/ready/dead) aggregated across a tagged group of
  farm plots, painted with a from-scratch sprite renderer including a
  documented dedicated-server sprite-sync workaround.

## Filing

All three new `.md` reference docs plus the five raw `.txt` source files were
copied into:

```
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Scripts\
```

matching the existing pattern where `Reference\*.md` holds the annotated
write-ups and `Reference\Scripts\*.txt` holds the raw source they describe.
