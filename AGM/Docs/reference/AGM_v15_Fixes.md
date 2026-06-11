# AutoGrid Manager v1.5 -- Full Changelog and Fix Log

## v1.5 -- Current Release

### New Features
- Food, Seed, Ingredient stock screens (FoodStock, SeedStock, IngredientStock)
- Cargo type tags now work in Custom Data as well as block name
- auto_disassemble=false added to default config
- Corner LCD alert displays with watch= key
- All screens now have consistent cyan border (drawn on VP edge, 6px thick)

### Bug Fixes

**Autocrafting**
- FindBpFor rewritten -- validates blueprints against real assembler CanUseBlueprint instead of guessing IDs with TryParse
- QueueToAllMasters -- queues to first available master only, coop assemblers pick up automatically
- Assemblers stuck in Disassembly mode now switched to Assembly before queuing
- max_queue_amount default raised 500->5000, cap raised 5000->100000
- max_queue_per_run default raised 2->5, cap raised 10->20
- QueueCompQuotas skips items already sufficiently queued

**Disassembly**
- DisassembleExcess skips any component with assembly queued -- cannot fight autocrafting

**Docked Grid Exclusion**
- Connector scan uses EntityId match instead of IsSameConstructAs to avoid picking up ship connectors
- _dockedGridIds stores IMyCubeGrid references (IIM pattern) not EntityIds
- Block filter checks b.CubeGrid==dg and b.CubeGrid.IsSameConstructAs(dg) to catch subgrids
- _dockedGridIds.Remove(Me.CubeGrid) safety -- base never excludes itself
- [No Sorting] tag checked in both connector Custom Data and block name
- Both sides of connector pair added to exclusion set to handle piston subgrid ships

**Corner LCD**
- Corner LCD is both IMyLightingBlock and IMyTextSurfaceProvider -- removed if(light==null) gate so LCDs always register

**Draw System**
- PrepSurf -- null check and try/catch
- VP() -- null surface and zero-size viewport guard, fallback to 512x512
- DrawScreen -- null block guard, safe GetSurface with try/catch
- DrawAlertLcds -- per-entry try/catch, dead entries removed automatically
- ScanBlocks -- safe GetSurface when registering alert LCDs
- Boot draws -- try/catch on both PB and screen provider
- DrawPowerDash inset fixed 2f->10f (border was invisible)
- All screen borders moved to VP edge at 6px -- visible on any LCD size and angle

**Item Categories**
- Food (_ConsumableItem, _Consumable), Seed (_TreeObject), Ingredient (IsFoodIngredient) added
- ItemCategory, ItemIcon, CargoTypeFromBlock, EnsureBaselineDestinations, StockKind all updated
- HasDashboardCmd updated with FoodStock, SeedStock, IngredientStock
