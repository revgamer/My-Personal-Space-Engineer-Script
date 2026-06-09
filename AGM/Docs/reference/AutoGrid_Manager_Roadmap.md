# AutoGrid Manager Roadmap

**AutoGrid Manager (AGM)** is a modular Space Engineers programmable block system by **RevGamer**.

---

## Architecture Overview

AGM is an expanding family of scripts. Each script is its own PB but shares the AGM visual style (colours, fonts, draw helpers). Scripts do NOT communicate via IGC — AGM Core simply scans for other AGM PBs on the same grid and reports whether they are online.

```
AGM Core PB
  - Inventory management and auto-sorter
  - Automated production (assemblers, refineries, autocrafting)
  - Power management
  - Fuel and life support
  - Stock dashboards
  - Alert system
  - CoreDashboard shows which AGM family PBs are present and online

AGM Defence Grid PB  (future, separate PB, same grid)
  - Threat detection, turret management, defence alerts
  - AGM Core CoreDashboard shows Defence Grid: Online / Offline

AGM Relay  (on hold)
```

**Fleet tracking** is handled by **R.O.S** (Rev Operating System) — separate script family.

**Key rule:** AGM Core never receives IGC — it only scans for other AGM PBs by name/tag on the same construct.

---

## AGM Core — v1.3+

### Status: Active

### What It Does

AGM Core is the main base/station management script. It is an inventory manager with auto-sorter and automated production system, plus power, fuel, alerts, and stock dashboards.

### Completed

**Logistics / Auto-Sorter**
- Cargo sorting into tagged containers {Ore 1}, {Ingot 1}, {Component 1} etc.
- Auto-assignment of untagged containers
- Priority fill — lower number fills first
- {Locked}, {Manual}, {Hidden} protection tags
- Never sorts from reactors, gas generators, or gas tanks

**Automated Production**
- Assembler and refinery job monitoring
- Autocrafting quotas — queues components to maintain stock levels
- Basic Assembler routing — basic components to Basic Assemblers, advanced to advanced
- QueueToAllMasters() — spreads work across all idle master assemblers
- Cooperative mode detection — [M] label on masters, COOP on coop assemblers
- Refinery and assembler priority lists

**Power**
- Battery, reactor, solar, hydrogen engine monitoring
- Auto reactor charging with safety hold
- Reactor refuel monitoring per reactor

**Stock Dashboards**
- Ore, Ingot, Component, Ammo, Tool, Bottle stock pages

**Alerts**
- Alert dashboard, warning lights via [AGM-LIGHT] in Custom Data
- Corner LCD alert displays with watch= key
- [AGM-LIGHT] blocks excluded from screens — never flicker
- Drawn every tick via DrawAlertLcds() — no flicker

**Fuel / Life Support**
- H2/O2 tank status, generator status, vent leak detection

**Display System**
- [AGM-S] tag in block name + dashboard command in Custom Data
- Responsive layouts — small PB, wide LCD, tall LCD
- CoreDashboard — full LCD only, never corner LCD
- Boot screen on all displays

### AGM Core Next — v1.4

**Family Status on CoreDashboard**
- Scan same-construct PBs for AGM family scripts
- CoreDashboard shows:

```
CORE STATUS

Power:        ONLINE
Logistics:    ONLINE
Production:   ACTIVE
Alerts:       OK

Defence Grid: Online / Offline
```

- Detection: GetBlocksOfType IMyProgrammableBlock scan for AGM family tags
- No IGC, no extra script overhead

---

## AGM Defence Grid — Future

### Status: Not Started

Separate PB for combat bases and PvP servers.

- Threat detection, turret activation, warning lights, ammo readiness
- AGM Core detects it via PB scan and shows Defence Grid: Online on CoreDashboard

---

## AGM Relay — On Hold

Long-range relay between bases and fleet nodes. On hold pending stealth-friendly solution.

---

## Development Status

| Phase | Script | Status |
|---|---|---|
| v1.3 | AGM Core — logistics, production, power, alerts | Done |
| v1.3+ | AGM Core — basic assembler routing, coop detection, responsive layouts, alert corner LCDs | Done |
| v1.4 | AGM Core — family status PB scan on CoreDashboard | Next |
| — | AGM Defence Grid | Future |
| — | AGM Relay | On hold |
