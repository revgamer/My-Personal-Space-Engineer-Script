# Space Engineers â€” Complete Block TypeId Reference

Read from game files: `C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Content\Data\CubeBlocks\`

Last updated: AGM v1.5 session (June 2026)

---

## How Block TypeIds Work

In SE scripting, blocks are identified by their `BlockDefinition.TypeIdString` and `BlockDefinition.SubtypeId`.

- Non-functional blocks (armor, structural, interior) use TypeId `CubeBlock`
- Functional blocks use a specific TypeId like `Reactor`, `Assembler`, `JumpDrive` etc
- In scripts: `b.BlockDefinition.TypeIdString` returns e.g. `MyObjectBuilder_Reactor`
- For interface checks: cast to `IMyReactor`, `IMyAssembler` etc

---

## Armor â€” TypeId: `CubeBlock`

### Light Armor (CubeBlocks_Armor.sbc)
Block, Slope, Corner, CornerInv, Slope2Base, Slope2Tip, Corner2Base, Corner2Tip,
RoundSlope, RoundCorner, RoundCornerInv, HalfArmorBlock, HalfSlopeArmorBlock
- Large grid: prefix `LargeBlockArmor...`
- Small grid: prefix `SmallBlockArmor...`

### Heavy Armor
Same shapes as light armor
- Large grid: prefix `LargeHeavyBlockArmor...`
- Small grid: prefix `SmallHeavyBlockArmor...`

### Extended Armor Shapes (CubeBlocks_Armor_2.sbc)
CornerSquare, CornerSquareInverted, HalfCorner, HalfSlopeCorner, HalfSlopeCornerInverted,
HalfSlopedCorner, HalfSlopedCornerBase, HalfSlopeInverted, SlopedCorner, SlopedCornerBase, SlopedCornerTip

### Transition Armor (CubeBlocks_Armor_3.sbc)
RaisedSlopedCorner, SlopeTransition, SlopeTransitionBase, SlopeTransitionTip,
SlopeTransitionMirrored, SquareSlopedCornerBase, SquareSlopedCornerTip, SquareSlopedCornerTipInv

### Armor Panels (CubeBlocks_ArmorPanels.sbc)
Full Panel, Half Panel, Quarter Panel, Center Panel, Half Center Panel,
Sloped Panel, Half Sloped Panel, Sloped Side Panel, 2x1 Sloped Panel variants,
Round Panel, Round Corner Panel, Round Face Panel
- Light: suffix `...Light`
- Heavy: suffix `...Heavy`

---

## Interior Blocks â€” TypeId: `CubeBlock`

| SubtypeId | Description |
|---|---|
| LargeBlockInteriorWall | Interior wall |
| LargeCoverWall | Cover wall |
| LargeCoverWallHalf | Half cover wall |
| LargeInteriorPillar | Interior pillar |
| LargeRamp | Ramp |
| LargeStairs | Stairs |
| LargeSteelCatwalk | Catwalk straight |
| LargeSteelCatwalk2Sides | Catwalk 2-sided |
| LargeSteelCatwalkCorner | Catwalk corner |
| LargeSteelCatwalkPlate | Catwalk plate |
| Passage2 | Passage |
| Passage2Wall | Passage wall |
| AirDuct1 | Air duct straight |
| AirDuct2 | Air duct |
| AirDuctCorner | Air duct corner |
| AirDuctGrate | Air duct grate |
| AirDuctRamp | Air duct ramp |
| AirDuctT | Air duct T |
| AirDuctX | Air duct X |

### Interior Seats â€” TypeId: `Cockpit`
| SubtypeId | Description |
|---|---|
| PassengerSeatLarge | Large passenger seat |
| PassengerSeatSmall | Small passenger seat |
| PassengerSeatSmallNew | Small passenger seat (new) |
| PassengerSeatSmallOffset | Small passenger seat (offset) |

### Ladders â€” TypeId: `Ladder2`
| SubtypeId | Description |
|---|---|
| LadderShaft | Ladder shaft |
| LadderSmall | Small ladder |

---

## Structural â€” TypeId: `CubeBlock`

| SubtypeId | Description |
|---|---|
| LargeBlockStructural_Frame | Structural frame |
| LargeBlockStructural_Platform | Structural platform |
| LargeBlockStructural_PlatformTriangle | Triangular platform |
| LargeBlockStructural_SupportBeam3x3 | Support beam 3x3 |
| LargeBlockStructural_SupportBeam4x4 | Support beam 4x4 |
| LargeBlockStructural_SupportBeam5x5 | Support beam 5x5 |

---

## Power

### Reactors â€” TypeId: `Reactor`
| SubtypeId | Grid | Description |
|---|---|---|
| LargeBlockLargeGenerator | Large | Large reactor |
| LargeBlockSmallGenerator | Large | Small reactor |
| SmallBlockLargeGenerator | Small | Large reactor |
| SmallBlockSmallGenerator | Small | Small reactor |
| LargeBlockLargeGeneratorWarfare2 | Large | Warfare large reactor |
| LargeBlockSmallGeneratorWarfare2 | Large | Warfare small reactor |
| SmallBlockLargeGeneratorWarfare2 | Small | Warfare large reactor |
| SmallBlockSmallGeneratorWarfare2 | Small | Warfare small reactor |

### Batteries â€” TypeId: `BatteryBlock`
| SubtypeId | Grid |
|---|---|
| LargeBlockBatteryBlock | Large |
| SmallBlockBatteryBlock | Small |
| SmallBlockSmallBatteryBlock | Small |
| LargeBlockBatteryBlockWarfare2 | Large |
| SmallBlockBatteryBlockWarfare2 | Small |
| LargeBlockPrototechBattery | Large |
| SmallBlockPrototechBattery | Small |

### Solar Panels â€” TypeId: `SolarPanel`
| SubtypeId | Grid |
|---|---|
| LargeBlockSolarPanel | Large |
| SmallBlockSolarPanel | Small |

### Wind Turbines â€” TypeId: `WindTurbine`
| SubtypeId | Grid |
|---|---|
| LargeBlockWindTurbine | Large |

### Hydrogen Engines â€” TypeId: `HydrogenEngine`
| SubtypeId | Grid |
|---|---|
| LargeHydrogenEngine | Large |
| SmallHydrogenEngine | Small |
| LargePrototechReactor | Large |

---

## Propulsion

### Thrusters â€” TypeId: `Thrust`
| SubtypeId | Type |
|---|---|
| LargeBlockLargeThrust | Large grid â€” ion large |
| LargeBlockSmallThrust | Large grid â€” ion small |
| SmallBlockLargeThrust | Small grid â€” ion large |
| SmallBlockSmallThrust | Small grid â€” ion small |
| LargeBlockLargeAtmosphericThrust | Large grid â€” atmo large |
| LargeBlockSmallAtmosphericThrust | Large grid â€” atmo small |
| SmallBlockLargeAtmosphericThrust | Small grid â€” atmo large |
| SmallBlockSmallAtmosphericThrust | Small grid â€” atmo small |
| LargeBlockLargeHydrogenThrust | Large grid â€” hydro large |
| LargeBlockSmallHydrogenThrust | Large grid â€” hydro small |
| SmallBlockLargeHydrogenThrust | Small grid â€” hydro large |
| SmallBlockSmallHydrogenThrust | Small grid â€” hydro small |
| LargeBlockLargeModularThruster | Large grid â€” modular large |
| LargeBlockSmallModularThruster | Large grid â€” modular small |
| SmallBlockLargeModularThruster | Small grid â€” modular large |
| SmallBlockSmallModularThruster | Small grid â€” modular small |
| LargeBlockPrototechThruster | Large â€” Prototech |
| SmallBlockPrototechThruster | Small â€” Prototech |
Flat and DShape variants also exist for atmospheric thrusters.

### Gyroscopes â€” TypeId: `Gyro`
| SubtypeId | Grid |
|---|---|
| LargeBlockGyro | Large |
| SmallBlockGyro | Small |
| LargeBlockPrototechGyro | Large |
| SmallBlockPrototechGyro | Small |

### Jump Drives â€” TypeId: `JumpDrive`
| SubtypeId | Grid |
|---|---|
| LargeJumpDrive | Large |
| LargePrototechJumpDrive | Large |
| SmallPrototechJumpDrive | Small |

---

## Production

### Assemblers â€” TypeId: `Assembler`
| SubtypeId | Description |
|---|---|
| BasicAssembler | Basic assembler |
| LargeAssembler | Advanced assembler |
| LargePrototechAssembler | Prototech assembler |
| FoodProcessor | Food processor (Apex Survival) |

### Refineries â€” TypeId: `Refinery`
| SubtypeId | Description |
|---|---|
| LargeRefinery | Standard refinery |
| Blast Furnace | Blast furnace |
| LargePrototechRefinery | Prototech refinery |
| SmallPrototechRefinery | Small Prototech refinery |

### Upgrade Modules â€” TypeId: `UpgradeModule`
| SubtypeId | Description |
|---|---|
| LargeProductivityModule | Productivity |
| LargeEffectivenessModule | Effectiveness |
| LargeEnergyModule | Energy |

### Survival Kit â€” TypeId: `SurvivalKit`
| SubtypeId |
|---|
| SurvivalKit |
| SurvivalKitLarge |

---

## Control

### Cockpits â€” TypeId: `Cockpit`
| SubtypeId | Description |
|---|---|
| LargeBlockCockpit | Large cockpit |
| LargeBlockCockpitSeat | Seat cockpit |
| LargeBlockStandingCockpit | Standing cockpit |
| SmallBlockCockpit | Small cockpit |
| SmallBlockFlushCockpit | Flush cockpit |
| SmallBlockStandingCockpit | Small standing cockpit |
| OpenCockpitLarge | Open cockpit large |
| OpenCockpitSmall | Open cockpit small |
| CockpitOpen | Open cockpit |
| DBSmallBlockFighterCockpit | Fighter cockpit |
| RoverCockpit | Rover cockpit |
| PassengerBench | Passenger bench |
| LargeBlockLabDeskSeat | Lab desk seat |

### Remote Controls â€” TypeId: `RemoteControl`
| SubtypeId |
|---|
| LargeBlockRemoteControl |
| SmallBlockRemoteControl |

### Programmable Blocks â€” TypeId: `MyProgrammableBlock`
| SubtypeId |
|---|
| LargeProgrammableBlock |
| SmallProgrammableBlock |

### Timer Blocks â€” TypeId: `TimerBlock`
| SubtypeId |
|---|
| TimerBlockLarge |
| TimerBlockSmall |

### Event Controllers â€” TypeId: `EventControllerBlock`
| SubtypeId |
|---|
| EventControllerLarge |
| EventControllerSmall |

### Button Panels â€” TypeId: `ButtonPanel`
| SubtypeId |
|---|
| ButtonPanelLarge |
| ButtonPanelSmall |
| LargeButtonPanelPedestal |
| SmallButtonPanelPedestal |

### Sensor Blocks â€” TypeId: `SensorBlock`
| SubtypeId |
|---|
| LargeBlockSensor |
| SmallBlockSensor |

---

## Cargo and Logistics

### Cargo Containers â€” TypeId: `CargoContainer`
| SubtypeId | Description |
|---|---|
| LargeBlockLargeContainer | Large cargo |
| LargeBlockSmallContainer | Small cargo (large grid) |
| SmallBlockLargeContainer | Large cargo (small grid) |
| SmallBlockMediumContainer | Medium cargo |
| SmallBlockSmallContainer | Small cargo |
| LargeBlockCargoTerminal | Cargo terminal |
| LargeBlockCargoTerminalHalf | Half cargo terminal |
| LargeBlockLabCabinet | Lab cabinet |
| LargeBlockLabCornerDesk | Lab corner desk |
| LargeBlockWeaponRack | Weapon rack |
| SmallBlockWeaponRack | Small weapon rack |

### Conveyors â€” TypeId: `ConveyorSorter`
| SubtypeId |
|---|
| LargeBlockConveyorSorter |
| MediumBlockConveyorSorter |
| SmallBlockConveyorSorter |

### Collectors â€” TypeId: `Collector`
| SubtypeId |
|---|
| Collector |
| CollectorSmall |

### Connectors â€” TypeId: `ShipConnector`
| SubtypeId | Description |
|---|---|
| Connector | Large connector |
| ConnectorMedium | Medium connector |
| ConnectorSmall | Small connector |
| LargeBlockInsetConnector | Inset connector |
| LargeBlockInsetConnectorSmall | Small inset connector |
| SmallBlockInsetConnector | Small grid inset connector |
| SmallBlockInsetConnectorMedium | Small grid medium inset |
| LargeBlockStructural_PlatformConnector | Structural platform connector |

---

## Weapons

### Fixed Weapons â€” TypeId: `SmallGatlingGun` / `SmallMissileLauncher` / `SmallMissileLauncherReload`
| SubtypeId | TypeId | Description |
|---|---|---|
| SmallGatlingGunWarfare2 | SmallGatlingGun | Gatling gun |
| SmallMissileLauncherWarfare2 | SmallMissileLauncher | Missile launcher |
| SmallRocketLauncherReload | SmallMissileLauncherReload | Reloadable rocket launcher |

### Turrets â€” TypeId: `LargeTurretBase`
| SubtypeId | Description |
|---|---|
| SmallGatlingTurret | Gatling turret |
| SmallMissileTurret | Missile turret |
| LargeInteriorTurret | Interior turret |
| AutoCannonTurret | Autocannon turret |
| LargeBlockMediumCalibreTurret | Medium calibre turret |
| LargeCalibreTurret | Large calibre turret |
| SmallBlockMediumCalibreTurret | Small grid medium calibre |

### Fixed Railguns â€” TypeId: `WeaponBlock`
| SubtypeId | Description |
|---|---|
| LargeRailgun | Large railgun |
| SmallRailgun | Small railgun |
| LargeBlockLargeCalibreGun | Large calibre gun |
| SmallBlockAutocannon | Autocannon |
| SmallBlockMediumCalibreGun | Medium calibre gun |
| LargeMissileLauncher | Large missile launcher |
| LargeFlareLauncher | Large flare launcher |
| SmallFlareLauncher | Small flare launcher |

### Warheads â€” TypeId: `Warhead`
| SubtypeId |
|---|
| LargeWarhead |
| SmallWarhead |

### Turret Control â€” TypeId: `TurretControlBlock`
| SubtypeId |
|---|
| LargeTurretControlBlock |
| SmallTurretControlBlock |

### Decoys â€” TypeId: `Decoy`
| SubtypeId |
|---|
| LargeDecoy |
| SmallDecoy |

---

## Medical

### Medical Rooms â€” TypeId: `MedicalRoom`
| SubtypeId | Description |
|---|---|
| LargeMedicalRoom | Medical room |
| LargeRefillStation | Large refill station |
| InsetRefillStation | Inset refill station |
| SmallRefillStation | Small refill station |

### Cryo Chambers â€” TypeId: `CryoChamber`
| SubtypeId |
|---|
| LargeBlockCryoChamber |
| SmallBlockCryoChamber |
| LargeBlockBedFree |
| LargeBlockCryoLabVat |

---

## Gas and Fuel

### Gas Tanks â€” TypeId: `GasTank`
| SubtypeId | Type |
|---|---|
| LargeHydrogenTank | Hydrogen â€” large |
| LargeHydrogenTankSmall | Hydrogen â€” small (large grid) |
| SmallHydrogenTank | Hydrogen â€” large (small grid) |
| SmallHydrogenTankSmall | Hydrogen â€” small (small grid) |
| OxygenTankSmall | Oxygen (large grid) |
| SmallOxygenTankSmall | Oxygen (small grid) |
| LargeBlockOxygenTankLab | Oxygen lab tank |
| LargeHydrogenTankSmallLab | Hydrogen lab tank |
| SmallHydrogenTankLab | Hydrogen lab (small grid) |

### Oxygen Generators (Ice) â€” TypeId: `OxygenGenerator`
| SubtypeId | Description |
|---|---|
| OxygenGeneratorSmall | Standard O2/H2 generator |
| LargeBlockOxygenGeneratorLab | Lab O2/H2 generator |
| SmallBlockOxygenGeneratorLab | Small lab generator |
| IrrigationSystem | Irrigation system (Apex Survival) |

### Oxygen Farms â€” TypeId: `OxygenFarm`
| SubtypeId |
|---|
| LargeBlockOxygenFarm |

### Air Vents â€” TypeId: `AirVent`
| SubtypeId |
|---|
| AirVentFull |
| SmallAirVent |
| SmallAirVentFull |

### Algae Farm / Farm Plot â€” TypeId: `FunctionalBlock`
| SubtypeId | Description |
|---|---|
| LargeBlockAlgaeFarm | Algae farm (Apex Survival) |
| LargeBlockFarmPlot | Farm plot (Apex Survival) |

---

## Communications

### Radio Antennas â€” TypeId: `RadioAntenna`
| SubtypeId |
|---|
| LargeBlockRadioAntenna |
| LargeBlockCompactRadioAntenna |
| SmallBlockRadioAntenna |

### Laser Antennas â€” TypeId: `LaserAntenna`
| SubtypeId |
|---|
| LargeBlockLaserAntenna |
| SmallBlockLaserAntenna |

### Beacons â€” TypeId: `Beacon`
| SubtypeId |
|---|
| LargeBlockBeacon |
| SmallBlockBeacon |

### Broadcast Controllers â€” TypeId: `BroadcastController`
| SubtypeId |
|---|
| LargeBlockBroadcastController |
| SmallBlockBroadcastController |

### Transponders â€” TypeId: `TransponderBlock`
| SubtypeId |
|---|
| LargeBlockTransponder |
| SmallBlockTransponder |

---

## Mechanical

### Pistons â€” TypeId: `PistonBase` / `ExtendedPistonBase`
| SubtypeId |
|---|
| LargePistonBase |
| SmallPistonBase |

### Rotors â€” TypeId: `MotorStator`
| SubtypeId |
|---|
| LargeStator |
| SmallStator |
| SmallAdvancedStator |
| SmallAdvancedStatorSmall |

### Hinges â€” TypeId: `MotorAdvancedStator`
| SubtypeId |
|---|
| LargeHinge |
| MediumHinge |
| SmallHinge |
| LargeAdvancedStator |

### Wheels â€” TypeId: `MotorSuspension`
Suspension1x1, Suspension2x2, Suspension3x3, Suspension5x5 (Large grid)
SmallSuspension1x1, etc (Small grid)
ShortSuspension variants for each size
Mirrored variants for each

### Merge Blocks â€” TypeId: `MergeBlock`
| SubtypeId |
|---|
| LargeShipMergeBlock |
| SmallShipMergeBlock |
| SmallShipSmallMergeBlock |

### Landing Gear â€” TypeId: `LandingGear`
| SubtypeId |
|---|
| LargeBlockLandingGear |
| LargeBlockSmallMagneticPlate |
| SmallBlockLandingGear |
| SmallBlockSmallMagneticPlate |

---

## Displays

### LCD Panels â€” TypeId: `TextPanel`
| SubtypeId | Description |
|---|---|
| LargeLCDPanel | Large LCD |
| LargeLCDPanelWide | Wide LCD |
| LargeTextPanel | Text panel |
| SmallLCDPanel | Small LCD |
| SmallLCDPanelWide | Small wide LCD |
| SmallTextPanel | Small text panel |
| LargeBlockCorner_LCD_1 | Corner LCD 1 |
| LargeBlockCorner_LCD_2 | Corner LCD 2 |
| LargeBlockCorner_LCD_Flat_1 | Flat corner LCD 1 |
| LargeBlockCorner_LCD_Flat_2 | Flat corner LCD 2 |
| SmallBlockCorner_LCD_1 | Small corner LCD 1 |
| SmallBlockCorner_LCD_2 | Small corner LCD 2 |
| SmallBlockCorner_LCD_Flat_1 | Small flat corner LCD 1 |
| SmallBlockCorner_LCD_Flat_2 | Small flat corner LCD 2 |

### Lights â€” TypeId: `InteriorLight` / `ReflectorLight`
| SubtypeId | TypeId |
|---|---|
| SmallLight | InteriorLight |
| SmallBlockSmallLight | InteriorLight |
| LargeBlockLight_1corner | InteriorLight |
| LargeBlockLight_2corner | InteriorLight |
| SmallBlockLight_1corner | InteriorLight |
| SmallBlockLight_2corner | InteriorLight |
| LargeLightPanel | InteriorLight |
| SmallLightPanel | InteriorLight |
| LargeBlockFrontLight | ReflectorLight |
| SmallBlockFrontLight | ReflectorLight |

### Searchlights â€” TypeId: `Searchlight`
| SubtypeId |
|---|
| LargeSearchlight |
| SmallSearchlight |

---

## Gravity

### Gravity Generators â€” TypeId: `GravityGenerator`
Standard and spherical variants

### Artificial Mass â€” TypeId: `VirtualMass`
| SubtypeId |
|---|
| VirtualMassLarge |
| VirtualMassSmall |

### Space Ball â€” TypeId: `SpaceBall`
| SubtypeId |
|---|
| SpaceBallLarge |
| SpaceBallSmall |

---

## Cameras and Detection

### Cameras â€” TypeId: `CameraBlock`
| SubtypeId |
|---|
| LargeCameraBlock |
| SmallCameraBlock |

### Ore Detectors â€” TypeId: `OreDetector`
| SubtypeId |
|---|
| LargeOreDetector |
| SmallBlockOreDetector |

### Projectors â€” TypeId: `Projector`
| SubtypeId |
|---|
| LargeProjector |
| SmallProjector |

---

## Tools (Ship)

### Drills â€” TypeId: `Drill`
| SubtypeId |
|---|
| LargeBlockDrill |
| SmallBlockDrill |
| LargeBlockPrototechDrill |

### Welders â€” TypeId: `ShipWelder`
| SubtypeId |
|---|
| LargeShipWelder |
| SmallShipWelder |

### Grinders â€” TypeId: `ShipGrinder`
| SubtypeId |
|---|
| LargeShipGrinder |
| SmallShipGrinder |

---

## Parachutes â€” TypeId: `Parachute`
| SubtypeId |
|---|
| LgParachute |
| SmParachute |

---

## Sound â€” TypeId: `SoundBlock`
| SubtypeId |
|---|
| LargeBlockSoundBlock |
| SmallBlockSoundBlock |

---

## AGM Interface Types

These are the C# interfaces AGM uses to check block types in scripting:

| Interface | Block type |
|---|---|
| `IMyReactor` | Reactors |
| `IMyBatteryBlock` | Batteries |
| `IMySolarPanel` | Solar panels |
| `IMyPowerProducer` | All power producers |
| `IMyAssembler` | Assemblers + Food Processor |
| `IMyRefinery` | Refineries + Blast Furnace |
| `IMyGasTank` | Gas tanks (H2 and O2) |
| `IMyGasGenerator` | O2/H2 generators + Irrigation |
| `IMyCargoContainer` | Cargo containers |
| `IMyShipConnector` | Connectors |
| `IMyLargeTurretBase` | All turrets |
| `IMyUserControllableGun` | Fixed weapons |
| `IMySmallGatlingGun` | Gatling gun |
| `IMySmallMissileLauncher` | Missile launcher |
| `IMyTextSurface` | Any surface that can draw |
| `IMyTextSurfaceProvider` | Blocks with screens (LCD, cockpit, PB) |
| `IMyLightingBlock` | Lights + corner LCDs |
| `IMyAirVent` | Air vents |
| `IMyProgrammableBlock` | Programmable blocks |
| `IMyShipDrill` | Drills |
| `IMyShipWelder` | Welders |
| `IMyShipGrinder` | Grinders |
| `IMyCockpit` | Cockpits + seats |
| `IMyMotorStator` | Rotors |
| `IMyPistonBase` | Pistons |
| `IMyMechanicalConnectionBlock` | Rotors + pistons + hinges |
| `IMyJumpDrive` | Jump drives |
| `IMyGyro` | Gyroscopes |
| `IMyThrust` | Thrusters |
| `IMyLandingGear` | Landing gear + magnetic plates |
| `IMySensorBlock` | Sensor blocks |
| `IMyTimerBlock` | Timer blocks |
| `IMyProjector` | Projectors |

---

# DLC Block TypeIds

---

## Apex Survival Pack

### Reskins (same TypeId/interface as vanilla)
| TypeId | SubtypeId | Vanilla equivalent |
|---|---|---|
| Drill | LargeBlockDrillReskin | LargeBlockDrill |
| Drill | SmallBlockDrillReskin | SmallBlockDrill |
| ShipWelder | LargeShipWelderReskin | LargeShipWelder |
| ShipWelder | SmallShipWelderReskin | SmallShipWelder |
| ShipGrinder | LargeShipGrinderReskin | LargeShipGrinder |
| ShipGrinder | SmallShipGrinderReskin | SmallShipGrinder |
| OreDetector | LargeOreDetectorReskin | LargeOreDetector |
| OreDetector | SmallOreDetectorReskin | SmallOreDetector |
| OxygenFarm | LargeBlockOxygenFarmReskin | LargeBlockOxygenFarm |
| SurvivalKit | SurvivalKitLargeReskin | SurvivalKitLarge |
| SurvivalKit | SurvivalKitSmallReskin | SurvivalKit |

### New Blocks
| TypeId | SubtypeId | Description |
|---|---|---|
| CubeBlock | LargeBlockConduitStraight | Conduit straight |
| CubeBlock | LargeBlockConduitCorner | Conduit corner |
| CubeBlock | LargeBlockConduitDown/Up | Conduit down/up |
| CubeBlock | LargeBlockConduitJunction | Conduit junction |
| CubeBlock | LargeBlockConduitBoxes | Conduit boxes |
| CubeBlock | LargeStorageBin1/2/3 | Storage bins (large) |
| CubeBlock | SmallStorageBin1 | Storage bin (small) |
| CubeBlock | LargeWarningSign14/15/16 | Warning signs |
| FunctionalBlock | LargeBlockAlgaeFarmReskin | Algae farm reskin |
| FunctionalBlock | LargeBlockConduitDamaged | Damaged conduit |
| InteriorLight | LargeBlockInsetTerrariumDesert | Desert terrarium |
| InteriorLight | LargeBlockInsetTerrariumForest | Forest terrarium |
| InteriorLight | LargeBlockInsetPlanter | Inset planter |
| InteriorLight | LargeBlockConduitLight | Conduit light |

---

## Automation DLC

All blocks are vanilla blocks — this DLC adds PB, Timer, Sensor, Event Controller, Flight Movement, Offensive/Defensive Combat, Path Recorder, Turret Control, Sound Block, Button Panel, Target Dummy.

| TypeId | SubtypeId |
|---|---|
| MyProgrammableBlock | LargeProgrammableBlock / SmallProgrammableBlock |
| TimerBlock | TimerBlockLarge / TimerBlockSmall |
| SensorBlock | LargeBlockSensor / SmallBlockSensor |
| EventControllerBlock | EventControllerLarge / EventControllerSmall |
| FlightMovementBlock | LargeFlightMovement / SmallFlightMovement |
| OffensiveCombatBlock | LargeOffensiveCombat / SmallOffensiveCombat |
| DefensiveCombatBlock | LargeDefensiveCombat / SmallDefensiveCombat |
| PathRecorderBlock | LargePathRecorderBlock / SmallPathRecorderBlock |
| TurretControlBlock | LargeTurretControlBlock / SmallTurretControlBlock |
| SoundBlock | LargeBlockSoundBlock / SmallBlockSoundBlock |
| ButtonPanel | ButtonPanelLarge / ButtonPanelSmall / Pedestal variants |
| TargetDummyBlock | TargetDummy |
| BasicMissionBlock | LargeBasicMission / SmallBasicMission |

---

## Contact Pack

### New Functional Blocks
| TypeId | SubtypeId | Description |
|---|---|---|
| Cockpit | LargeBlockModularBridgeCockpit | Modular bridge cockpit |
| Cockpit | LargeBlockCaptainDesk | Captain desk cockpit |
| ButtonPanel | LargeBlockModularBridgeButtonPanel | Bridge button panel |
| CargoContainer | SmallBlockModularContainer | Modular container |
| CryoChamber | SmallBlockBunkBed | Bunk bed |
| TerminalBlock | SmallBlockFirstAidCabinet | First aid cabinet |
| TerminalBlock | SmallBlockKitchenFridge/Microwave/Oven | Kitchen appliances |
| CubeBlock | SmallBlockKitchenCoffeeMachine/Sink | Kitchen fixtures |

### Modular Bridge (CubeBlock)
Corner, Corner2x1, Floorless variants, HalfSlopedCorner, RaisedSlopedCorner, SlopedCornerBase, SideL/R, Floor, Empty

### Reskins
| TypeId | SubtypeId |
|---|---|
| ExtendedPistonBase | LargePistonBaseReskin / SmallPistonBaseReskin |
| LargeGatlingTurret | LargeGatlingTurretReskin / SmallGatlingTurretReskin |
| LargeMissileTurret | LargeMissileTurretReskin / SmallMissileTurretReskin |
| RadioAntenna | LargeBlockCompactRadioAntennaReskin / SmallBlockCompactRadioAntennaReskin |

### New Lights
| TypeId | SubtypeId | Description |
|---|---|---|
| ReflectorLight | LargeBlockFloodlight | Floodlight large |
| ReflectorLight | LargeBlockFloodlightAngled | Angled floodlight |
| ReflectorLight | LargeBlockFloodlightCornerL/R | Corner floodlights |
| ReflectorLight | SmallBlockFloodlight variants | Small floodlights |
| Door | LargeBlockEvenWideDoor | Wide door |
| Door | LargeBlockSmallGate | Small gate |

---

## Core Systems Pack

### Armor
| TypeId | SubtypeId | Description |
|---|---|---|
| CubeBlock | LargeBlockRoundEdge / SmallBlockRoundEdge | Round edge armor |
| CubeBlock | LargeBlockRoundEdgeCorner variants | Round edge corners |
| CubeBlock | LargeBlockRoundHalfEdge variants | Round half edge |
| CubeBlock | LargeBlockRoundEdgeSlopeBase/Tip | Round edge slopes |

### Blocks
| TypeId | SubtypeId | Description |
|---|---|---|
| Cockpit | LargeBlockSuspendedControlSeat A/B | Suspended control seat |
| Cockpit | SmallBlockSuspendedControlSeat A/B | Small suspended seat |
| Door | LargeBlockCentredDoor / Glass variant | Centred door |
| Door | LargeBlockHalfCentredDoor / Glass | Half centred door |
| Door | SmallBlockCentredDoor / Glass | Small centred door |
| InteriorLight | LargeBlockTrofferLight | Trofter ceiling light |
| InteriorLight | LargeBlockHalfTrofferLight / Inv | Half trofter light |

### Reskins
| TypeId | SubtypeId |
|---|---|
| HydrogenEngine | LargeHydrogenEngineReskin / SmallHydrogenEngineReskin |
| JumpDrive | LargeJumpDriveReskin |
| LandingGear | LargeBlockLandingGearReskin / SmallBlockLandingGearReskin |
| Thrust | LargeBlockLargeHydrogenThrustReskin + 3 more variants |

---

## Decorative Pack 1

| TypeId | SubtypeId | Description |
|---|---|---|
| CargoContainer | LargeBlockLockers | Lockers |
| CargoContainer | LargeBlockLockerRoom / Corner | Locker room |
| Cockpit | LargeBlockBathroom / BathroomOpen | Bathroom |
| Cockpit | LargeBlockToilet | Toilet |
| Cockpit | LargeBlockCouch / CouchCorner | Couch |
| Cockpit | LargeBlockDesk / DeskCorner / DeskCornerInv | Desk |
| Cockpit | LargeBlockCockpitIndustrial | Industrial cockpit |
| Cockpit | SmallBlockCockpitIndustrial | Small industrial cockpit |
| CryoChamber | LargeBlockBed | Bed |
| CubeBlock | LargeBlockDeskChairless variants | Desk (no chair) |
| Kitchen | LargeBlockKitchen | Kitchen |
| Planter | LargeBlockPlanters | Planters |
| Projector | LargeBlockConsole | Console |

---

## Decorative Pack 2

| TypeId | SubtypeId | Description |
|---|---|---|
| CubeBlock | Catwalk / CatwalkCorner / CatwalkHalf variants | Catwalks |
| CubeBlock | RailingStraight / RailingCorner / RailingDouble etc | Railings |
| CubeBlock | GratedStairs / GratedHalfStairs | Grated stairs |
| CubeBlock | Freight1 / Freight2 / Freight3 | Freight boxes |
| CubeBlock | Shower | Shower |
| CubeBlock | WindowWall / WindowWallLeft / WindowWallRight | Window walls |
| TextPanel | TransparentLCDLarge / TransparentLCDSmall | Transparent LCD |
| Jukebox | Jukebox | Jukebox |
| VendingMachine | FoodDispenser | Food dispenser |
| LCDPanelsBlock | LabEquipment / MedicalStation | LCD lab equipment |
| ReflectorLight | RotatingLightLarge / RotatingLightSmall | Rotating lights |

---

## Decorative Pack 3

### Truss System (CubeBlock)
Truss, TrussAngled, TrussAngledSmall, TrussFloor, TrussFloorAngled, TrussFloorAngledInverted, TrussFloorHalf, TrussFloorT, TrussFloorX, TrussFrame, TrussHalf, TrussHalfSmall, TrussSloped, TrussSlopedFrame, TrussSlopedSmall, TrussSmall

| TypeId | SubtypeId | Description |
|---|---|---|
| CubeBlock | LargeBarrel / LargeBarrelStack / LargeBarrelThree | Barrels |
| CubeBlock | SmallBarrel | Small barrel |
| CryoChamber | LargeBlockCryoRoom | Cryo room |
| CryoChamber | LargeBlockHalfBed / HalfBedOffset | Half beds |
| CryoChamber | LargeBlockInsetBed | Inset bed |
| Cockpit | LargeBlockInsetPlantCouch | Plant couch |
| Cockpit | SmallBlockCapCockpit | Cap cockpit |
| CargoContainer | LargeBlockInsetBookshelf | Inset bookshelf |
| ButtonPanel | LargeBlockInsetButtonPanel | Inset button panel |
| InteriorLight | LargeBlockInsetAquarium | Inset aquarium |
| InteriorLight | LargeBlockInsetKitchen | Inset kitchen |
| Jukebox | LargeBlockInsetEntertainmentCorner | Entertainment corner |
| Ladder2 | TrussLadder | Truss ladder |
| MedicalRoom | LargeMedicalRoomReskin | Medical room reskin |
| TerminalBlock | LargeCrate | Large crate |
| Warhead | LargeExplosiveBarrel / SmallExplosiveBarrel | Explosive barrels |
| WindTurbine | LargeBlockWindTurbineReskin | Wind turbine reskin |

### Colorable Solar Panels
| TypeId | SubtypeId |
|---|---|
| SolarPanel | LargeBlockColorableSolarPanel + Corner + CornerInverted |
| SolarPanel | SmallBlockColorableSolarPanel + Corner + CornerInverted |

### LCD Panels
| TypeId | SubtypeId | Description |
|---|---|---|
| TextPanel | HoloLCDLarge / HoloLCDSmall | Holographic LCD |
| TextPanel | LargeCurvedLCDPanel / SmallCurvedLCDPanel | Curved LCD |
| TextPanel | LargeDiagonalLCDPanel / SmallDiagonalLCDPanel | Diagonal LCD |
| TextPanel | LargeFullBlockLCDPanel / SmallFullBlockLCDPanel | Full block LCD |

---

## Economy / Economy Deluxe

| TypeId | SubtypeId | Description |
|---|---|---|
| ContractBlock | ContractBlock | Contract block |
| StoreBlock | StoreBlock | Store block |
| StoreBlock | AtmBlock | ATM block |
| SafeZoneBlock | SafeZoneBlock | Safe zone |
| FunctionalBlock | ServicesTerminal | Services terminal |
| VendingMachine | VendingMachine | Vending machine |

---

## Economy 2 Pack

| TypeId | SubtypeId | Description |
|---|---|---|
| CargoContainer | LargeBlockBulkContainerA/B/C | Bulk containers |
| CubeBlock | LargeBlockLargeVivarium / CornerVivarium | Large vivarium |
| CubeBlock | LargeBlockSmallVivarium / CornerVivarium | Small vivarium |
| CubeBlock | LargeBlockNarrowViewport + slope variants | Narrow viewports |
| CubeBlock | LargeBlockFloorPlanSign1-21 | Floor plan signs |
| Door | LargeBlockAngledDoorA/B | Angled doors |
| Door | SmallBlockAngledDoorA | Small angled door |
| LCDPanelsBlock | LargeBlockBillboard / Billboard Round | Billboard LCD |
| SafeZoneBlock | SafeZoneBlockReskin | Safe zone reskin |

---

## Fieldwork DLC

### Lab Blocks (CubeBlock)
LargeBlockLabDesk, LargeBlockLabSink, LargeBlockFloorCenter, LargeBlockFloorDecal, LargeBlockFloorEdge, LargeBlockFloorPassage, LargeBlockFloorSlab, SmallBlockFloorCenter, SmallBlockFloorSlab

### Corridor Round System (CubeBlock)
CorridorRound, CorridorRoundCorner, CorridorRoundT, CorridorRoundTransition, CorridorRoundX

### Pipe System (CubeBlock)
LargeBlockPipesStraight1/2, LargeBlockPipesCorner, LargeBlockPipesCornerInner/Outer, LargeBlockPipesEnd, LargeBlockPipesJunction

| TypeId | SubtypeId | Description |
|---|---|---|
| Door | CorridorRoundDoor / CorridorRoundDoorInv | Round corridor door |
| Door | LargeBlockLabDoor / LargeBlockLabDoorInv | Lab door |
| CargoContainer | LargeBlockCargoTerminal / Half | Cargo terminal |
| CargoContainer | LargeBlockLabCabinet / LabCornerDesk | Lab storage |
| Cockpit | LargeBlockLabDeskSeat | Lab desk seat |
| CryoChamber | LargeBlockCryoLabVat | Cryo lab vat |
| LCDPanelsBlock | LabEquipment1/3 / LargeBlockLabDeskMicroscope | LCD lab equipment |
| InteriorLight | CorridorRoundLight | Round corridor light |
| OxygenGenerator | LargeBlockOxygenGeneratorLab / SmallBlockOxygenGeneratorLab | Lab O2 generator |
| GasTank | LargeBlockOxygenTankLab / LargeHydrogenTankSmallLab / SmallHydrogenTankLab | Lab gas tanks |
| ExhaustBlock | LargeExhaustCap / SmallExhaustCap | Exhaust caps |
| TerminalBlock | LargeFreezer | Freezer |
| InteriorLight | LabEquipment2 | Lab equipment light |

---

## Frostbite DLC

| TypeId | SubtypeId | Description |
|---|---|---|
| CubeBlock | DeadBody01-06 | Dead body props |
| Door | LargeBlockGate | Large gate door |
| Door | LargeBlockOffsetDoor | Offset door |
| RadioAntenna | LargeBlockRadioAntennaDish | Dish antenna |

---

## Grid AI Pack

### Warning Signs (CubeBlock)
LargeWarningSign1-13, SmallWarningSign1-13

### Pipe Work (CubeBlock)
PipeWorkBlockA, PipeWorkBlockB, AngledInteriorWallA, AngledInteriorWallB

| TypeId | SubtypeId | Description |
|---|---|---|
| AirVent | AirVentFan / AirVentFanFull | Fan air vents |
| AirVent | SmallAirVentFan / SmallAirVentFanFull | Small fan vents |
| CameraBlock | LargeCameraTopMounted / SmallCameraTopMounted | Top mounted cameras |
| Cockpit | SpeederCockpit / SpeederCockpitCompact | Speeder cockpits |
| EmotionControllerBlock | EmotionControllerLarge / EmotionControllerSmall | Emotion controller |
| InteriorLight | LargeBlockInsetLight / SmallBlockInsetLight | Inset lights |
| MyProgrammableBlock | LargeProgrammableBlockReskin / SmallProgrammableBlockReskin | PB reskins |
| SensorBlock | LargeBlockSensorReskin / SmallBlockSensorReskin | Sensor reskins |
| TimerBlock | TimerBlockReskinLarge / TimerBlockReskinSmall | Timer reskins |
| ButtonPanel | LargeBlockAccessPanel3 | Access panel |
| TerminalBlock | LargeBlockAccessPanel1/2/4 | Access panels |
| TerminalBlock | SmallBlockAccessPanel1/2/3/4 | Small access panels |

---

## Industrial Pack

### Beam Blocks (CubeBlock)
LargeGridBeamBlock, LargeGridBeamBlockEnd, LargeGridBeamBlockHalf, LargeGridBeamBlockHalfSlope, LargeGridBeamBlockJunction, LargeGridBeamBlockRound, LargeGridBeamBlockSlope, LargeGridBeamBlockSlope2x1Base/Tip, LargeGridBeamBlockTJunction (and Small variants)

| TypeId | SubtypeId | Description |
|---|---|---|
| Assembler | LargeAssemblerIndustrial | Industrial assembler |
| Refinery | LargeRefineryIndustrial | Industrial refinery |
| CargoContainer | LargeBlockLargeIndustrialContainer | Industrial cargo |
| GasTank | LargeHydrogenTankIndustrial | Industrial H2 tank |
| CubeBlock | LargeBlockCylindricalColumn / SmallBlockCylindricalColumn | Cylindrical columns |
| ConveyorSorter | LargeBlockConveyorSorterIndustrial | Industrial sorter |
| Conveyor | LargeBlockConveyorPipeT/Intersection/Junction | Industrial conveyors |
| ConveyorConnector | LargeBlockConveyorPipeCorner/End/Flange/Seamless | Conveyor pipe connectors |
| LandingGear | LargeBlockMagneticPlate / SmallBlockMagneticPlate | Magnetic plates |
| ButtonPanel | VerticalButtonPanelLarge / VerticalButtonPanelSmall | Vertical button panels |
| Thrust | LargeBlockLargeHydrogenThrustIndustrial + 3 variants | Industrial H2 thrusters |

---

## Prototech DLC

| TypeId | SubtypeId |
|---|---|
| Assembler | LargePrototechAssembler |
| Refinery | LargePrototechRefinery / SmallPrototechRefinery |
| BatteryBlock | LargeBlockPrototechBattery / SmallBlockPrototechBattery |
| Gyro | LargeBlockPrototechGyro / SmallBlockPrototechGyro |
| JumpDrive | LargePrototechJumpDrive / SmallPrototechJumpDrive |
| HydrogenEngine | LargePrototechReactor |
| Drill | LargeBlockPrototechDrill |
| Thrust | LargeBlockPrototechThruster / SmallBlockPrototechThruster |
| Ingot | PrototechScrap (item, not block) |

---

## Scrap Race Pack

### Off-Road Suspensions (MotorSuspension)
OffroadSuspension1x1/2x2/3x3/5x5 (mirrored variants)
OffroadSmallSuspension1x1/2x2/3x3/5x5 (mirrored variants)
OffroadShortSuspension + OffroadSmallShortSuspension variants

### Off-Road Wheels (Wheel)
OffroadWheel1x1/2x2/3x3/5x5
OffroadSmallWheel1x1/2x2/3x3/5x5
OffroadRealWheel + mirrored variants

| TypeId | SubtypeId | Description |
|---|---|---|
| CubeBlock | BarredWindow / Face / Side / Slope | Barred windows |
| CubeBlock | StorageShelf1/2/3 | Storage shelves |
| CubeBlock | Viewport1 / Viewport2 | Viewports |
| Cockpit | BuggyCockpit | Buggy cockpit |
| ExhaustBlock | LargeExhaustPipe / SmallExhaustPipe | Exhaust pipes |
| InteriorLight | OffsetLight | Offset light |
| ReflectorLight | OffsetSpotlight | Offset spotlight |

---

## Signals Pack

### Truss Pillars (CubeBlock)
TrussPillar, TrussPillarCorner, TrussPillarDiagonal, TrussPillarOffset, TrussPillarSlanted, TrussPillarSmall, TrussPillarT, TrussPillarX

### Corridor System (CubeBlock)
Corridor, CorridorCorner, CorridorNarrow, CorridorT, CorridorX, CorridorDoubleWindow, CorridorWindow, CorridorWindowRoof

### Extended Windows (CubeBlock)
ExtendedWindow, ExtendedWindowCorner, ExtendedWindowCornerInverted, ExtendedWindowDiagonal, ExtendedWindowDome, ExtendedWindowEnd, Railing variants

### Inset Walls (CubeBlock)
LargeBlockInsetWall, LargeBlockInsetWallCorner, LargeBlockInsetWallCornerInverted, LargeBlockInsetWallPillar, LargeBlockInsetWallSlope

### Console Modules
| TypeId | SubtypeId | Description |
|---|---|---|
| CubeBlock | LargeBlockConsoleModule / Corner | Console module |
| CubeBlock | SmallBlockConsoleModule / Corner / InvertedCorner | Small console module |
| Cockpit | LargeBlockConsoleModuleScreens | Console screens cockpit |
| Cockpit | LargeBlockConsoleModuleInvertedCorner | Console corner cockpit |
| ButtonPanel | LargeBlockConsoleModuleButtons / Small variant | Console buttons |
| TextPanel | SmallBlockConsoleModuleScreens | Console screen LCD |
| Door | LargeBlockNarrowDoor / LargeBlockNarrowDoorHalf | Narrow doors |
| InteriorLight | CorridorLight / CorridorNarrowStowage | Corridor lights |
| InteriorLight | LargeBlockInsetWallLight | Inset wall light |
| InteriorLight | TrussPillarLight / TrussPillarLightSmall | Truss pillar lights |
| Decoy | TrussPillarDecoy | Truss pillar decoy |
| CubeBlock | SmallBlockExtendedWindow + variants | Small extended windows |

---

## Sparks of the Future Pack

### Neon Tubes (EmissiveBlock — TypeId: EmissiveBlock)
| SubtypeId | Description |
|---|---|
| LargeNeonTubesStraight1/2 | Straight neon |
| LargeNeonTubesBendDown/Up | Bent neon |
| LargeNeonTubesCorner | Corner neon |
| LargeNeonTubesCircle | Circle neon |
| LargeNeonTubesStraightDown | Downward neon |
| LargeNeonTubesStraightEnd1/2 | End caps |
| LargeNeonTubesT / LargeNeonTubesU | T and U shapes |
Small variants for all above.

### SciFi Thrusters (Thrust)
| SubtypeId | Description |
|---|---|
| LargeBlockLargeThrustSciFi | Large SciFi ion large |
| LargeBlockSmallThrustSciFi | Large SciFi ion small |
| SmallBlockLargeThrustSciFi | Small SciFi ion large |
| SmallBlockSmallThrustSciFi | Small SciFi ion small |
| LargeBlockLargeAtmosphericThrustSciFi | Large SciFi atmo large |
| LargeBlockSmallAtmosphericThrustSciFi | Large SciFi atmo small |
| SmallBlockLargeAtmosphericThrustSciFi | Small SciFi atmo large |
| SmallBlockSmallAtmosphericThrustSciFi | Small SciFi atmo small |

### Large LCD Panels (TextPanel)
| SubtypeId | Description |
|---|---|
| LargeLCDPanel3x3 | 3x3 LCD panel |
| LargeLCDPanel5x3 | 5x3 LCD panel |
| LargeLCDPanel5x5 | 5x5 LCD panel |

### Other Blocks
| TypeId | SubtypeId | Description |
|---|---|---|
| CubeBlock | LargeBlockSciFiWall | SciFi wall |
| CubeBlock | LargeBlockBarCounter / Corner | Bar counter |
| ButtonPanel | LargeSciFiButtonPanel | SciFi button panel |
| ButtonPanel | LargeSciFiButtonTerminal | SciFi button terminal |
| TerminalBlock | LargeBlockSciFiTerminal | SciFi terminal |
| Door | SmallSideDoor | Small side door |

---

## Warfare 1

| TypeId | SubtypeId | Description |
|---|---|---|
| CubeBlock | HalfWindow / HalfWindowCorner / HalfWindowCornerInv | Half windows |
| CubeBlock | HalfWindowDiagonal / HalfWindowInv / HalfWindowRound | Half window variants |
| CubeBlock | FireCover / FireCoverCorner | Fire cover |
| CubeBlock | Embrasure | Embrasure |
| CubeBlock | PassageSciFi / PassageScifiCorner / PassageSciFiGate | SciFi passage system |
| CubeBlock | PassageSciFiIntersection / PassageSciFiTjunction / PassageSciFiWall / PassageSciFiWindow | SciFi passage variants |
| CargoContainer | LargeBlockWeaponRack / SmallBlockWeaponRack | Weapon racks |
| InteriorLight | PassageSciFiLight | SciFi passage light |

---

## Warfare 2

### Hangar Doors (AirtightHangarDoor)
| SubtypeId | Description |
|---|---|
| AirtightHangarDoorWarfare2A | Hangar door A |
| AirtightHangarDoorWarfare2B | Hangar door B |
| AirtightHangarDoorWarfare2C | Hangar door C |

### Modular Thrusters (Thrust)
| SubtypeId |
|---|
| LargeBlockLargeModularThruster |
| LargeBlockSmallModularThruster |
| SmallBlockLargeModularThruster |
| SmallBlockSmallModularThruster |

### Warfare Reactors (Reactor)
| SubtypeId |
|---|
| LargeBlockLargeGeneratorWarfare2 |
| LargeBlockSmallGeneratorWarfare2 |
| SmallBlockLargeGeneratorWarfare2 |
| SmallBlockSmallGeneratorWarfare2 |

### Warfare Batteries (BatteryBlock)
| SubtypeId |
|---|
| LargeBlockBatteryBlockWarfare2 |
| SmallBlockBatteryBlockWarfare2 |

### Other Blocks
| TypeId | SubtypeId | Description |
|---|---|---|
| Door | SlidingHatchDoor / SlidingHatchDoorHalf | Sliding hatch |
| HeatVentBlock | LargeHeatVentBlock / SmallHeatVentBlock | Heat vent |
| CubeBlock | BridgeWindow1x1Face / FaceInverted / Slope | Bridge windows |
| Searchlight | LargeSearchlight / SmallSearchlight | Searchlights |
| SmallGatlingGun | SmallGatlingGunWarfare2 | Warfare gatling |
| SmallMissileLauncher | SmallMissileLauncherWarfare2 | Warfare missile launcher |
| InteriorLight | LargeLightPanel / SmallLightPanel | Light panels |
| Cockpit | LargeBlockStandingCockpit / SmallBlockStandingCockpit | Standing cockpits |
| Cockpit | PassengerBench | Passenger bench |

