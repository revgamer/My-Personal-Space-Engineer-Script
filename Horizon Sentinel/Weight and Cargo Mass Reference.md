# Weight and Cargo Mass Reference

## Purpose

Horizon Sentinel needs cargo weight logic for:

- takeoff safety
- planet entry warning
- descent safety
- cargo overload warnings
- "can lift in gravity" estimates
- "remove cargo before planet" warnings

This document records the known Space Engineers item mass/volume data and the recommended PB scripting approach.

## Best Rule For Scripts

For live ship safety, do **not** depend only on hardcoded item mass tables.

Use the game API whenever possible:

```csharp
IMyInventory inv = block.GetInventory(i);
double cargoMassKg = (double)inv.CurrentMass;
double cargoVolumeM3 = (double)inv.CurrentVolume;
double maxVolumeM3 = (double)inv.MaxVolume;
```

Reason:

- `CurrentMass` returns the current total mass of items inside an inventory in kg.
- `CurrentVolume` returns current volume in cubic meters.
- `MaxVolume` returns max inventory volume in cubic meters.
- 1 cubic meter = 1000 liters.
- Modded items and changed definitions can make hardcoded tables wrong.

Use ship controller mass for total ship mass:

```csharp
MyShipMass mass = controller.CalculateShipMass();
int baseMassKg = mass.BaseMass;
int totalMassKg = mass.TotalMass;
int cargoMassKg = mass.TotalMass - mass.BaseMass;
```

Important:

- `BaseMass` is the ship without cargo.
- `TotalMass` includes cargo.
- Some edge cases exist with connected grids, player inventory, subgrids, and game updates, so Horizon Sentinel should show both API ship mass and inventory-scanned cargo mass when debugging.

## Item Definition Rules

Space Engineers physical item definitions include:

- `Mass`: kg per item/unit
- `Volume`: liters occupied in inventory

The official SBC item definition reference says item mass affects cargo mass, and component mass affects block base mass when used in blocks.

Source:

- https://spaceengineers.wiki.gg/wiki/Modding/Reference/SBC/Items/PhysicalItem_Definition

PB inventory source:

- https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/VRage.Game.ModAPI.Ingame.IMyInventory.html

## Ores

Most vanilla ores are effectively:

- mass: `1 kg`
- volume: `0.37 L`
- density: about `2.703 kg/L`

This means a full cargo container of ore is much lighter than a full cargo container of dense ingots such as platinum, gold, or uranium.

Known ore types:

| Ore | Mass | Volume | Density |
|---|---:|---:|---:|
| Stone | 1 kg | 0.37 L | 2.703 kg/L |
| Iron Ore | 1 kg | 0.37 L | 2.703 kg/L |
| Nickel Ore | 1 kg | 0.37 L | 2.703 kg/L |
| Cobalt Ore | 1 kg | 0.37 L | 2.703 kg/L |
| Magnesium Ore | 1 kg | 0.37 L | 2.703 kg/L |
| Silicon Ore | 1 kg | 0.37 L | 2.703 kg/L |
| Silver Ore | 1 kg | 0.37 L | 2.703 kg/L |
| Gold Ore | 1 kg | 0.37 L | 2.703 kg/L |
| Platinum Ore | 1 kg | 0.37 L | 2.703 kg/L |
| Uranium Ore | 1 kg | 0.37 L | 2.703 kg/L |
| Ice | usually treated as ore-like cargo | check live API | check live API |
| Scrap Metal | ore-like refining item | check live API | check live API |

Sources:

- https://spaceengineers.wiki.gg/wiki/Cobalt_Ore
- https://spaceengineers.wiki.gg/wiki/Iron_Ore
- https://spaceengineers.wiki.questlinehero.com/content/ores/
- https://spaceengineers.fandom.com/wiki/Category:Ores
- Google Sheet: Space Engineers Data by Chicken Nugget
  - https://docs.google.com/spreadsheets/d/156mIkBxN5k-rA0z5jD28CgNehFOM-Ib5enwpykIKAfU/edit?gid=1887655615

## Google Sheet: Ores & Ingots Extract

The linked Google Sheet is accessible. The useful tab is:

```text
Ores & Ingots
```

Extracted fields:

- material name
- ore volume per 1 kg
- ingot volume per 1 kg
- base refine time for 1 kg ore
- refine ratio
- mined ore ratio

| Material | Ore Volume for 1 kg | Ingot Volume for 1 kg | Base Refine Time | Refine Ratio | Mined Ore Ratio |
|---|---:|---:|---:|---:|---:|
| Iron | 0.37 L | 0.127 L | 0.05 s | 0.700 | 5.0 |
| Nickel | 0.37 L | 0.112 L | 2.00 s | 0.400 | 5.0 |
| Cobalt | 0.37 L | 0.112 L | 4.00 s | 0.300 | 5.0 |
| Magnesium | 0.37 L | 0.575 L | 0.50 s | 0.007 | 5.0 |
| Silicon | 0.37 L | 0.429 L | 0.60 s | 0.700 | 5.0 |
| Silver | 0.37 L | 0.095 L | 1.00 s | 0.100 | 1.5 |
| Gold | 0.37 L | 0.052 L | 0.40 s | 0.010 | 3.5 |
| Platinum | 0.37 L | 0.047 L | 4.00 s | 0.005 | 1.6 |
| Uranium | 0.37 L | 0.052 L | 4.00 s | 0.007 | 1.5 |
| Stone/Dirt/Sand | 0.37 L | 0.37 L | 0.10 s | 0.900 | 5.0 |
| Asteroid Ice | 0.37 L | n/a | n/a | n/a | 4.5 |
| Planet Ice/Snow | 0.37 L | n/a | n/a | n/a | 5.0 |
| Scrap | n/a | 0.254 L | 0.04 s | 0.800 | n/a |

Notes:

- The sheet confirms the same main cargo lesson: normal ore volume is `0.37 L per kg`.
- Dense ingots are much more dangerous for planet entry than ore.
- Platinum, gold, and uranium ingots are the strongest cargo-mass risk.
- The sheet also has `Thrust Calc`, which may be useful later for takeoff/descent calculations.

## Ingots / Materials

Vanilla material mass and volume:

| Material | Mass | Volume | Density |
|---|---:|---:|---:|
| Cobalt Ingot | 1 kg | 0.112 L | 8.93 kg/L |
| Gold Ingot | 1 kg | 0.052 L | 19.23 kg/L |
| Iron Ingot | 1 kg | 0.127 L | 7.87 kg/L |
| Magnesium Powder | 1 kg | 0.575 L | 1.74 kg/L |
| Nickel Ingot | 1 kg | 0.112 L | 8.93 kg/L |
| Platinum Ingot | 1 kg | 0.047 L | 21.28 kg/L |
| Silicon Wafer | 1 kg | 0.429 L | 2.33 kg/L |
| Silver Ingot | 1 kg | 0.095 L | 10.53 kg/L |
| Gravel | 1 kg | 0.37 L | 2.70 kg/L |
| Uranium Ingot | 1 kg | 0.052 L | 19.23 kg/L |

Source:

- https://spaceengineers.wiki.questlinehero.com/content/materials/

Important density notes:

- Platinum ingots are the densest listed material.
- Gold and uranium ingots are also very dense.
- Magnesium powder is much lighter per liter.
- Gravel has ore-like density.
- A cargo container full of platinum/gold/uranium ingots is extremely heavy compared with the same container full of ore.

## Components

Vanilla component mass and volume:

| Component | Mass | Volume | Density |
|---|---:|---:|---:|
| Bulletproof Glass | 15 kg | 8 L | 1.875 kg/L |
| Canvas | 15 kg | 8 L | 1.875 kg/L |
| Computer | 0.2 kg | 1 L | 0.200 kg/L |
| Construction Component | 8 kg | 2 L | 4.000 kg/L |
| Detector Components | 5 kg | 6 L | 0.833 kg/L |
| Display | 8 kg | 6 L | 1.333 kg/L |
| Explosives | 2 kg | 2 L | 1.000 kg/L |
| Girder | 6 kg | 2 L | 3.000 kg/L |
| Gravity Generator Components | 800 kg | 200 L | 4.000 kg/L |
| Interior Plate | 3 kg | 5 L | 0.600 kg/L |
| Large Steel Tube | 25 kg | 38 L | 0.658 kg/L |
| Medical Components | 150 kg | 160 L | 0.938 kg/L |
| Metal Grid | 6 kg | 15 L | 0.400 kg/L |
| Motor | 24 kg | 8 L | 3.000 kg/L |
| Power Cell | 25 kg | 40 L | 0.625 kg/L |
| Radio-Communication Components | 8 kg | 70 L | 0.114 kg/L |
| Reactor Components | 25 kg | 8 L | 3.125 kg/L |
| Small Steel Tube | 4 kg | 2 L | 2.000 kg/L |
| Solar Cell | 6 kg | 12 L | 0.500 kg/L |
| Steel Plate | 20 kg | 3 L | 6.667 kg/L |
| Superconductor Component | 15 kg | 8 L | 1.875 kg/L |
| Thruster Components | 40 kg | 10 L | 4.000 kg/L |
| Zone Chip | 0.25 kg | 0.2 L | 1.250 kg/L |

Source:

- https://spaceengineers.wiki.questlinehero.com/content/components/

Important density notes:

- Steel Plate is very dense for a component.
- Thruster Components, Construction Components, and Gravity Generator Components are also heavy per liter.
- Radio-Communication Components are extremely light per liter.
- Component cargo can be very different from ore cargo even at the same fill percentage.

## Ammunition

Ammo is also classified with components in some references.

Known older/common ammo reference:

| Ammo | Mass | Volume | Density |
|---|---:|---:|---:|
| 25x184mm NATO Ammo Container / Gatling Ammo Box | 35 kg | 16 L | 2.188 kg/L |
| 200mm Missile Container / Missile | 45 kg | 60 L | 0.750 kg/L |

Sources:

- https://spaceengineers.wiki.gg/wiki/25x184mm_NATO_ammo_container
- https://spaceengineers.fandom.com/wiki/Rocket

For Horizon Sentinel:

- Use live inventory item data or `CurrentMass` for actual ammo mass.
- Use item subtype counting for ammo balancing.
- Do not assume all turrets use vanilla ammo.

## Refining And Weight Change

Ore refining usually reduces mass because ore converts to less material by weight.

Examples:

- Iron Ore to Iron Ingots: about 70% weight ratio in a refinery.
- Cobalt Ore to Cobalt Ingots: about 30% weight ratio in a refinery.
- Uranium Ore does not become the same mass of uranium ingots; refining has conversion efficiency.

Sources:

- https://spaceengineers.wiki.gg/wiki/Iron_Ore
- https://spaceengineers.wiki.gg/wiki/Cobalt_Ore
- https://spaceengineers.wiki.questlinehero.com/content/ores/

For Horizon Sentinel:

- Treat raw ore and refined ingots as different cargo risk classes.
- A ship carrying dense ingots may be much heavier than a ship carrying the same volume percentage of ore.

## Cargo Risk Classes

For takeoff/planet entry warnings, Horizon Sentinel can classify cargo by density:

### Low Density

Usually less dangerous for takeoff:

- Computers
- Radio components
- Interior plates
- large tubes
- power cells
- solar cells
- missiles by density

### Medium Density

Moderate effect:

- ores
- gravel
- ammo boxes
- motors
- girders
- reactor components

### High Density

Major takeoff risk:

- platinum ingots
- gold ingots
- uranium ingots
- silver ingots
- cobalt/nickel ingots
- iron ingots
- steel plates

## Horizon Sentinel Calculation Plan

### Live Cargo Mass

Preferred:

```csharp
double ScanInventoryMassKg(List<IMyTerminalBlock> blocks)
{
    double mass = 0;
    for (int b = 0; b < blocks.Count; b++)
    {
        for (int i = 0; i < blocks[b].InventoryCount; i++)
        {
            mass += (double)blocks[b].GetInventory(i).CurrentMass;
        }
    }
    return mass;
}
```

### Live Cargo Volume

```csharp
double usedM3 = (double)inv.CurrentVolume;
double maxM3 = (double)inv.MaxVolume;
double fillPercent = maxM3 > 0 ? usedM3 / maxM3 * 100.0 : 0.0;
```

### Density Estimate

```csharp
double densityKgPerL = volumeLiters > 0 ? massKg / volumeLiters : 0;
```

### Worst-Case Full Cargo Estimate

Use this when warning the pilot:

```text
current cargo mass + remaining volume * worst-case density
```

Possible worst-case densities:

- ore-like cargo: `2.703 kg/L`
- iron/steel common heavy cargo: `6.667-7.87 kg/L`
- dense ingot worst case: `21.28 kg/L` for platinum ingots

Horizon Sentinel can show:

- `Current Mass`
- `Cargo Mass`
- `Cargo Fill`
- `Cargo Density`
- `Full Cargo Estimate`
- `Worst Case Planet Risk`

## Takeoff Formula Link

For planetary takeoff:

```text
required lift force = mass kg * gravity m/s^2
lift margin = available lift thrust / required lift force
```

For Earth-like gravity:

```text
required lift force = mass kg * 9.81
```

Example:

```text
Ship total mass: 1,000,000 kg
Gravity: 9.81 m/s^2
Required hover thrust: 9,810,000 N
Safe lift should be higher than this, preferably with margin.
```

Horizon Sentinel should warn:

- below 1.0 lift ratio: cannot hover
- 1.0-1.15: unsafe/weak
- 1.15-1.35: caution
- above 1.35: safer

These margins are design choices and should be configurable.

## Implementation Recommendation

Use three mass views:

1. **Controller Mass**
   - `CalculateShipMass().BaseMass`
   - `CalculateShipMass().TotalMass`

2. **Inventory Scan Mass**
   - sum all chosen cargo inventories with `CurrentMass`

3. **Prediction Mass**
   - estimate future mass if cargo fills with ore/ingots/components

This gives the pilot both the real current state and the risk of continuing to load cargo before takeoff or planet entry.
