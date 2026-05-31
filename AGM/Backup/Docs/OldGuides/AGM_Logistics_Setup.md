# AGM Logistics Setup

Script file:

```text
Scripts/AGM.cs
```

Suggested programmable block name:

```text
PB AutoGrid Manager
```

AGM Logistics runs inside the unified AGM script.

If `logistics=false` or `global_pause=true`, AGM will not sort, rename cargo, or move items.

Use an `[AGM-S]` LCD with `LogisticsDashboard` in Custom Data.

## Core PB Custom Data

Core must include:

```ini
[Core]
enabled=true
logistics=true
global_pause=false
include_docked_grids=false
no_sorting_tag=[No Sorting]
locked_tag={Locked}
manual_tag={Manual}
hidden_tag={Hidden}

```

## Logistics Custom Data

```ini
[Logistics]
auto_assign=true
max_moves_per_run=2
```

## Cargo Tags

AGM Logistics auto-scans cargo and assigns empty untyped containers when a category has no usable destination or the existing destination is full. It uses the largest suitable empty cargo first, similar to IIM.

Large Grid Cargo Container or Bulk Cargo Container:

```text
{Ore 1}
{Ingot 1}
{Component 1}
```

Small Cargo Container:

```text
{Ammo 1}
{Tool 1}
{Bottle 1}
```

When Logistics assigns a cargo, it also writes metadata into that cargo's Custom Data:

```ini
[AGM-Logistics]
managed=true
type=Ore
index=1
size=Large
```

That means the sorter can still understand the cargo type even if you later edit the visible block name.

Protected cargo:

```text
{Locked}
{Manual}
{Hidden}
```

Dock/ship protection:

```text
[No Sorting]
```

## Dashboard Data

LCD Custom Data:

```text
LogisticsDashboard
```

Sorter dashboard alias:

```text
SorterDashboard
```

AGM Logistics publishes a `[LogisticsState]` section in its own PB Custom Data.
AGM Core reads that section and renders `LogisticsDashboard` on `[AGM-S]` LCDs.

## Theme

Logistics uses the same AGM V1 neon cyan/teal HUD theme as Core, Power, and Production.

## First Test

1. Make sure AGM Core is online.
2. Make sure Core has `logistics=true` and `global_pause=false`.
3. Name one large cargo `{Ore 1}`.
4. Name one large cargo `{Component 1}`.
5. Put a component into `{Ore 1}`.
6. Logistics should move it to `{Component 1}`.

If `{Component 1}` is full and an empty large/bulk cargo exists, Logistics should rename it to `{Component 2}` and use it.



