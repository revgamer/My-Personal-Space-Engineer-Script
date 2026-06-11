# AutoGrid Manager Roadmap

## AGM Core -- v1.5 (Current)

### Completed in v1.5
- Food / Seed / Ingredient stock screens
- Cargo type tags in Custom Data (not just block name)
- FindBpFor rewritten -- real blueprint validation via CanUseBlueprint
- Assembler mode check before queuing
- DisassembleExcess cannot fight autocrafting
- Docked grid exclusion -- IIM pattern with IMyCubeGrid references
- Corner LCD fix -- removed if(light==null) gate
- All borders on VP edge at 6px -- visible on any LCD size
- Bulletproof draw system -- try/catch on every surface access
- max_queue_amount 5000 default, max_queue_per_run 5 default

### Next -- v1.6
- CoreDashboard AGM family PB scan
  - Scan same-construct PBs for other AGM scripts
  - Show Defence Grid / Relay status on CoreDashboard
  - No IGC -- simple GetBlocksOfType scan

## AGM Defence Grid -- Future
Separate PB. Threat detection, turret management, ammo readiness, defence alerts.
AGM Core detects it via PB name scan.

## AGM Relay -- On Hold
Long-range relay between bases. On hold.
