# HogOS

HogOS is a personal Space Engineers mining operating system for:

- `GroundHog`: atmospheric miner
- `SpaceHog`: ion miner
- `HydroHog`: hydrogen miner

Live script:

```text
Scripts/HogOS.cs
```

Reference and planning notes:

```text
Docs/Reference/HogOS_Reference_Notes.md
```

## Clean Build

Version `2.0-clean` rebuilds HogOS as a smaller, easier-to-maintain PB script:

- readable profile Custom Data
- 4-screen cockpit layout
- permanent cockpit screens: `Power`, `CargoOre`, `Weight`, `Utility`
- cruise control
- gyro auto-level

Dock, unload, path, fuel-only, drill-only, and splash pages are not included in this focused build.
