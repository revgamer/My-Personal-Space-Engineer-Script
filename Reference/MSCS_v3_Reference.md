# MSCS v3 (Multi-Station Cargo System) — Reference

> **Study reference only.**
> Three cooperating scripts, all version `3.2.0` (Cargo Ship Controller is
> tagged `3.2.0` too even though its own internal history notes stop short of
> documenting it — see Version Notes). Designed to sit on top of an existing
> "PMV4" (`ProductionManagementv3`) production-management script rather than
> replace it; MSCS only ever talks to PMV4 over IGC broadcast channels, it
> never reads PMV4's CustomData or source directly except to discover a
> couple of optional tag overrides.

This is a three-role dispatch network:

```
   OrderStation  <---EXCHANGE_REQUEST--->  SupplyStation
        |                                       |
        |  (drone assignment, cargo move        |
        |   commands, status broadcasts)        |
        +-------------------+-------------------+
                             |
                      CargoShipController
                      (one per drone grid)
```

- **OrderStation** sits at a remote outpost. It watches its own local PMV4
  surplus/deficit lists and periodically asks a named SupplyStation for an
  exchange: "send me X, I'll give you Y back."
- **SupplyStation** is the dispatcher. It owns the drone pool, decides which
  drone goes where, loads outbound cargo through its own local PMV4, and
  unloads returning drones.
- **CargoShipController** is the "dumb" drone brain. It has zero manifest
  knowledge — it only flies where told (via a separate third-party SAM
  autopilot PB), reports docked/arrived, and manages its own systems
  (thrusters off / batteries recharging / H2 stockpiling) while parked.

---

## Protocol v3 — IGC Channels

All three scripts share this exact channel vocabulary (`const string CH_*`),
so changing one name must change it everywhere it appears:

| Channel | Direction | Purpose |
|---|---|---|
| `PMV4_INVENTORY` | PMV4 -> SS / OS | Broadcasts `INVENTORY`/`SURPLUS`/`DEFICIT` item maps, filtered by receiving grid ID. |
| `PMV4_CARGO_CMD` | SS/OS -> PMV4 | `PUSH`, `PULL`, `PUSH_SURPLUS` commands targeting a docked drone grid by name. |
| `PMV4_CARGO_STATUS` | PMV4 -> SS/OS, and SS -> Drone | `PUSH_COMPLETE`, `PULL_COMPLETE`, `TRANSFER_COMPLETE`, `BUSY`. |
| `EXCHANGE_REQUEST` | OS -> SS | OS asks for an exchange: wants/offers/flags/connector/location-tags. |
| `EXCHANGE_ACCEPTED` | SS -> OS | SS confirms a mission was dispatched, with manifest + ETA + drone capacity. |
| `EXCHANGE_REJECTED` | SS -> OS | SS declines, with a human-readable reason string. |
| `MISSION_COMPLETE` | SS -> OS | Round trip finished, OS clears its cooldown. |
| `SS_INVENTORY` | SS -> OS | SS's own shippable stock, for the OS "Supply" display section. |
| `DRONE_AVAILABLE` | Drone -> SS | Idle-and-fueled drone announces itself to its home SS. |
| `DRONE_ASSIGN` | SS -> Drone | SS tells a specific drone to fly to a destination connector. |
| `DRONE_ARRIVED` | Drone -> SS/OS | Drone reports it has docked somewhere. |
| `DRONE_NACK` | Drone -> SS | Drone refuses an assignment (busy/refueling/preflight fail). |
| `STATION_STATUS` | SS -> (broadcast) | Periodic mission/drone-count heartbeat. |
| `SAM` | (third-party autopilot) -> SS/OS | SAMv2/SAMv2V connector-geometry broadcast, parsed for home-connector picking. |

All payloads are plain `|`-delimited strings (no JSON, no binary) — consistent
with the rest of RevGamer's IGC-using scripts (RNB, AGM) and easy to debug by
eye from the antenna's `Echo` output.

---

## CargoShipController (drone brain)

### State Machine
`IDLE -> UNDOCK_PREP -> MISSION -> DOCKED -> UNDOCK_PREP -> RETURNING -> DOCKED_HOME -> IDLE`

| State | Behaviour |
|---|---|
| `IDLE` | Docked at home, fueling. Once both battery and H2 thresholds are met, broadcasts `AVAILABLE` every `AVAILABLE_INTERVAL` (18) ticks. |
| `UNDOCK_PREP` | Thrusters re-enabled, batteries off Recharge, H2 stockpile off; waits `UndockPrepTicks` ticks (lets H2 re-pressurize conveyors) then runs a `PreflightCheck` gate before triggering the SAM autopilot PB. |
| `MISSION` | In flight or just docked at destination; on docking sends `ARRIVED` and waits for PMV4 (via SS) to push cargo and signal `TRANSFER_COMPLETE`. |
| `DOCKED` | At destination, transfer done, refueling before the return-leg undock prep starts. |
| `RETURNING` | Flying home; on docking sends `ARRIVED` again. |
| `DOCKED_HOME` | Home dock, transfer (unload) done, refueling before returning to `IDLE`. |

### Safety / Preflight Gate
`PreflightCheck(thorough)` blocks a launch if: no thrusters exist, any
thruster is damaged, (when thorough) all thrusters are disabled, any battery
is still in `ChargeMode.Recharge`, or fuel levels are below the configured
minimums. A failed preflight during an *assignment* request results in a
`DRONE_NACK` rather than a silent drop, so the SupplyStation can immediately
try a different drone instead of leaving the mission stuck.

### Persistence
`Storage` encodes the current state plus whatever fields that state needs to
resume correctly (`STATE_UNDOCK_PREP|target|nextState|dest|homeConnector`,
etc.) so a world reload or script recompile resumes mid-mission rather than
forgetting it was ever assigned. `RecoverState()` re-arms the SAM trigger on
load if the drone was mid-flight, and sets `samRetryPending` so the very next
`Main()` tick retries the autopilot call if the SAM PB wasn't found
immediately (it may not have finished compiling yet at world load).

### Config Keys (`[CargoShip]` CustomData section)
`DroneID`, `HomeStation`, `Tags`, `AutopilotPBName` (default `[SAM]`),
`StatusLcdTag`, `MinBatteryPercent`, `MinH2Percent`, `ManageH2Stockpile`,
`ManageThrusters`, `ManageBatteries`, `UndockPrepTicks`.

---

## SupplyStation (dispatcher)

### Responsibilities
- Maintains `availableDrones` (from `DRONE_AVAILABLE` broadcasts, expired
  after `DRONE_STALE_TICKS` = 120 ticks of silence) and `activeMissions`
  (state machine: `LOADING -> OUTBOUND -> AT_DEST -> RETURNING -> UNLOADING`).
- On an `EXCHANGE_REQUEST`, builds a `canSend` map from local PMV4
  surplus/inventory (mode-dependent) plus locally-tracked ore (read directly
  from cargo containers tagged with the ore/mixed tags, **not** through
  PMV4), clamps it to the best available drone's cargo volume via
  `FairFillToCapacity`, reserves a home connector, and issues a single
  `PUSH` command to its local PMV4 to begin loading.
- `FairFillToCapacity` sorts items smallest-volume-first and gives each
  remaining item an equal share of remaining budget volume, so one bulky
  item (e.g. a stack of heavy ingots) cannot starve every other item out of
  the manifest entirely.
- Tracks SAM-broadcast connector geometry (`ProcessSAMBroadcasts`) purely to
  know which connectors exist and whether they're currently occupied, for
  `PickAvailableHomeConnector` reservation bookkeeping — it does not drive
  any docking guidance itself (that's the drone's own SAM autopilot's job).
- `HOLD` / `RESUME` console arguments pause new dispatch while letting
  in-flight missions finish naturally — useful for safely editing CustomData
  or doing station maintenance without aborting an active drone run.

### Ore Handling Caveat
Ore (Ice, Iron, Silicon, etc.) is deliberately **not** routed through PMV4's
inventory/surplus broadcast. The SupplyStation scans its own ore/mixed-tagged
containers directly (`ScanOwnOres`, every `ORE_SCAN_INTERVAL_TICKS` = 6 ticks)
and applies a separate `OreReserves` floor per ore subtype before anything
above that floor becomes shippable. This is called out explicitly in the
default CustomData comments — it is the one inventory category PMV4 is not
trusted to track for this system.

### Display System
A theme-able (`default`/`military`/`highcontrast`/`minimal`/`pm`) sprite
renderer (`DL`/`DLR`/`DLDot`/`DLSep`/`DLBanner` line-builder pattern) that
paginates status lines across however many LCDs are tagged into a named
display group, repeating the active section header at the top of each
overflow panel. Sections: `Status`, `Outbound`, `Inbound`, `Drones` (opt-in),
`Pending` (opt-in, shows recent rejections with reasons and age).

### Config Keys (`[SupplyStation]` CustomData section)
`StationID`, `ExchangeMode` (`deficit`|`warehouse`), `DroneSpeedMS`,
`DroneFillFactor`, `OreReserves`, `OreIngotTag`/`MixedTag` overrides, plus a
`---DISPLAY---` block for theme/section assignment per LCD tag group.

---

## OrderStation (requester)

### Responsibilities
- Periodically (`ReportInterval`, default 300s) evaluates its own PMV4
  deficits/surplus and, if either exceeds `ReportThreshold`, sends an
  `EXCHANGE_REQUEST` to its configured `SupplyStation`. Manual immediate
  requests are available via the `order` console argument, which instead
  pulls from a hand-edited `---ORDERS---` manifest block in CustomData.
- On `EXCHANGE_ACCEPTED`, tracks the incoming mission with a timeout
  (`eta * 2.5`, minimum 36 ticks) so a drone that never shows up doesn't
  leave the station permanently believing a mission is still active.
- On `DRONE_ARRIVED`, drives a two-phase local transfer
  (`PHASE_PULLING` then `PHASE_PUSHING`) against its own local PMV4: first
  pull whatever the drone is offering, then push back either an explicit
  return manifest (`BuildReturnManifest`, again capacity-clamped and
  fair-filled) or — if nothing was reserved as "WillCollect" — a generic
  `PUSH_SURPLUS` command that lets PMV4 decide what surplus to load.
- After a completed transfer, sets `missionCooldownEndTick` (1.5x the
  outbound ETA, minimum 36 ticks) before it will request from that supply
  station again — this throttles repeat requests while a drone is still
  physically in transit home.

### Export Behaviour
`ExportSurplus` (default true) automatically offers PMV4-reported surplus,
filtered by `ExportCategories`. An explicit `---EXPORTS---` CustomData block
can override this entirely with hand-picked items/quantities (or `int.MaxValue`
"export everything of this type" entries written as a bare
`Category.Subtype` line with no `:amount` suffix).

### Location Tag Gating
`LocationTags` (e.g. `PLANET`, `ATMO`, `SPACE`, `MOON`) declares constraints
about the *order station's own location* that a candidate drone must satisfy
via its own `Tags` list (set on the CargoShipController side) before the
SupplyStation will assign it to this OS's request — this is how, for example,
a wheeled/ground-only drone gets excluded from space-only deliveries, or vice
versa.

### Config Keys (`[OrderStation]` CustomData section)
`StationID`, `SupplyStation`, `ExportSurplus`, `ExportPriority`,
`ReportInterval`, `ReportThreshold`, `DroneFillFactor`, `RequestCategories`,
`ExportCategories`, `LocationTags`, `SupplyViewCategories`, plus
`---DISPLAY---`, `---ORDERS---`, and `---EXPORTS---` blocks.

---

## Shared Helper Code (duplicated across all three files)

`FairFillToCapacity`, `GetUnitVolumeL` (via `MyItemType(...).GetItemInfo().Volume`),
`ParseItemList`/`FormatItemList`, `ParseGPS`, the theme/`DisplayLine`/sprite
renderer, and the `GetDisplaySurfaces`/`ParseSurfaceIndex`/`HasTag` panel
discovery helpers are byte-for-byte (or very close to it) identical across
Supply Station and Order Station. This is expected for two independently
compiled PB scripts that need the same capability — there is no shared
library mechanism available inside a single PB script, so duplication is the
only option. If one copy is ever bug-fixed, the other two copies should be
checked for the same issue.

---

## TypeId / API Surface Used

Item TypeIds are always handled through `MyItemType`/string-key dictionaries
keyed as `"Category.Subtype"` (e.g. `"Ingot.Iron"`, `"Ore.Ice"`,
`"Component.SteelPlate"`), never directly as raw `MyObjectBuilder_*` strings
except in two correct, narrow cases:

```csharp
// SupplyStation.ScanOwnOres — correctly checking an inventory ITEM TypeId
if (item.Type.TypeId != "MyObjectBuilder_Ore") continue;

// Shared GetUnitVolumeL — correctly constructing an item TypeId dynamically
var info = new MyItemType("MyObjectBuilder_" + cat, sub).GetItemInfo();
```

Both are inventory item TypeId usages (never block definition TypeIds), and
the dynamic-construction path special-cases `Ingot.Gravel` to query as
`Ingot/Stone` instead — Gravel's in-game definition is registered under
the `Stone` subtype, so a literal `Ingot.Gravel` lookup would otherwise throw
and be silently caught (`catch { v = 0; }`), making that one item's volume
read as zero. This is a correct, deliberate special-case, not a bug.

Block-side APIs are limited to what each role actually needs:
CargoShipController uses `IMyShipConnector`, `IMyGasTank` (filtered by
`SubtypeId.IndexOf("Hydrogen", ...)`), `IMyBatteryBlock`, `IMyThrust`,
`IMyRadioAntenna`/`IMyLaserAntenna` (validation only), `IMyCargoContainer`,
`IMyShipController` (dampener override), and `IMyProgrammableBlock` (the SAM
autopilot, driven via `TryRun`). SupplyStation/OrderStation additionally use
`IMyTextPanel`/`IMyTextSurfaceProvider` for the display system and
`IMyProjector` (SupplyStation only, to detect an active blueprint "surge" in
deficit count).

---

## Version Notes

- **SupplyStation** carries an in-source changelog: `3.0.0` initial v3 release
  (dispatcher model, SAM advertiser connectors, home-connector reservations),
  `3.0.1` sloped-LCD rotation fix plus deficit-mode `SS_INVENTORY` exchangeable
  amounts, `3.1.0` outbound manifest capacity clamp + fair-fill,
  `DroneFillFactor`, `ASSIGN_NACK` unwind handling, docked-home sweep, `3.2.0`
  adds `HOLD`/`RESUME` dispatch pause commands.
- **OrderStation** is tagged `3.1.0` and does not carry the same changelog
  comment block as SupplyStation, but implements the matching `3.1.0`-era
  protocol (capacity-clamped fair-fill return manifests, `ASSIGN_NACK`-aware
  flow on the supply side). It does not yet implement a `HOLD`/`RESUME`
  equivalent of its own.
- **CargoShipController** is tagged `3.2.0` and matches the `ASSIGN_NACK`
  protocol plus the undock-prep/preflight-gate pattern that the `3.2.0`
  SupplyStation changelog implies, but has no internal changelog comment of
  its own to cross-check against.

If these three are ever updated independently, re-confirm all three still
agree on the wire format for `ASSIGN`/`ASSIGN_NACK`/`ARRIVED` and the
`HOLD`/`RESUME` semantics before deploying a mixed-version fleet.
