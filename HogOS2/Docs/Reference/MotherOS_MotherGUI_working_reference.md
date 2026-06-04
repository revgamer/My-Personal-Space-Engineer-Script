# Mother OS + Mother GUI Working Reference

Generated: 2026-06-02

Sources read:

- Mother Docs home: https://lukejamesmorrison.github.io/mother-docs/
- Cheatsheet: https://lukejamesmorrison.github.io/mother-docs/Cheatsheet.html
- Mother OS examples: https://lukejamesmorrison.github.io/mother-docs/IngameScript/Examples.html
- Mother GUI docs: https://lukejamesmorrison.github.io/mother-docs/MotherGUI/
- Mother GUI commands: https://lukejamesmorrison.github.io/mother-docs/MotherGUI/Commands.html
- Mother GUI configuration: https://lukejamesmorrison.github.io/mother-docs/MotherGUI/Configuration.html
- Mother GUI MenuView: https://lukejamesmorrison.github.io/mother-docs/MotherGUI/MenuView.html
- Mother GUI views: https://lukejamesmorrison.github.io/mother-docs/MotherGUI/Views.html
- Pasted script 1: Mother OS v1.1.0, 12 May 2026
- Pasted script 2: Mother GUI v0.1.0, 12 May 2026

## What "Unminified" Means Here

The pasted scripts are minified and obfuscated. Their public command strings, hook names, Space Engineers block API calls, and high-level module behavior can be reconstructed reliably. Their internal class and method names cannot be faithfully restored without the original source map, so this reference uses descriptive names instead of pretending the obfuscated identifiers have canonical names.

Use this as a practical rebuild/reference guide for writing routines, menus, and future non-minified helper scripts around Mother OS and Mother GUI.

## Mental Model

Mother OS is the automation and command bus.

- Runs in a programmable block.
- Reads commands from terminal arguments and configured custom data.
- Controls blocks directly: doors, lights, pistons, rotors, hinges, batteries, tanks, sorters, thrusters, wheels, timers, programmable blocks, screens, etc.
- Monitors block state and fires hooks such as `onOpen`, `onClose`, `onPressurized`, `onLock`, and `onDetach`.
- Supports delayed routines, local storage, variables, IGC communication, remote commands, almanac/grid discovery, and command sharing.

Mother GUI is the display and menu layer.

- Runs in a programmable block.
- Turns supported text surfaces into interactive screens.
- Reads display assignments and menus from `Custom Data`.
- Provides view navigation commands like `view/up`, `view/down`, `view/select`, `view/back`, and `view/go`.
- Renders `MenuView`, `RotorView`, `HingeView`, `PistonView`, and `DoorView`.
- Menu items can execute Mother-style commands, so a GUI button can call `door/open`, `light/color`, `battery/auto`, `view/go`, etc.

## Shared Core Shape

Both pasted scripts include a slim Mother Core:

- `Program()` creates a central app object and registers modules.
- `Main(argument, updateType)` delegates into the central app.
- `Save()` serializes local storage into programmable block `Storage`.
- Modules register commands with a command bus.
- Modules can watch block properties and emit events/hooks when state changes.
- A terminal/console module displays boot status, log lines, and command output.
- A block catalogue scans and resolves blocks/groups by name, tag, or special target.
- IGC/network modules support ping/almanac style discovery and remote command forwarding.
- Local storage exposes `get` and `set`.

## Mother OS Command Surface

This list comes from the docs cheatsheet plus the command strings found in the pasted Mother OS script.

### Core

| Command | Purpose |
|---|---|
| `boot` | Re-run Mother boot/setup. |
| `help` | Print available commands. |
| `clear` | Clear the terminal output. |
| `ping` | Discover/update other Mother grids on the network. |
| `rename <Name> [--unique]` | Rename the current grid. |
| `wait <seconds>` | Delay the next command in a routine. |
| `purge <modules,> --force` | Clear module data such as almanac/storage. |
| `get <Key>` | Read local storage. |
| `set <Key> <Value>` | Write local storage. |
| `print <Text>` | Print text to the console. |

### Terminal Blocks

| Command | Purpose |
|---|---|
| `block/on <Block|Group>` | Enable blocks. |
| `block/off <Block|Group>` | Disable blocks. |
| `block/toggle <Block|Group>` | Toggle enabled state. |
| `block/action <Block|Group> <Action> <...Args>` | Run a terminal action. |
| `block/actions <Block>` | Print available block actions. |
| `block/config <Block|Group> <Section.Key> <Value>` | Write block custom data config. |
| `block/rename <Block|Group> <NewName>` | Rename blocks. |
| `tag/get <Tag>` | List blocks with a tag. |
| `tag/set <Block|Group> <Tag>` | Apply a tag to blocks. |

Hooks: `onOn`, `onOff`.

### Air Vents

| Command | Purpose |
|---|---|
| `vent/pressurize <AirVent|Group>` | Set `Depressurize=false`. |
| `vent/depressurize <AirVent|Group>` | Set `Depressurize=true`. |
| `vent/toggle <AirVent|Group>` | Toggle vent mode. |

Hooks: `onDepressurized`, `onDepressurizing`, `onPressurized`, `onPressurizing`.

### Batteries

| Command | Purpose |
|---|---|
| `battery/charge <Battery|Group>` | Set `ChargeMode.Recharge`. |
| `battery/discharge <Battery|Group>` | Set `ChargeMode.Discharge`. |
| `battery/auto <Battery|Group>` | Set `ChargeMode.Auto`. |
| `battery/toggle <Battery|Group>` | Cycle Auto -> Recharge -> Discharge. |

### Cockpits

| Command | Purpose |
|---|---|
| `dampeners/on [Cockpit|Group]` | Enable dampeners. |
| `dampeners/off [Cockpit|Group]` | Disable dampeners. |
| `handbrake/on [Cockpit|Group]` | Enable handbrake. |
| `handbrake/off [Cockpit|Group]` | Disable handbrake. |

Hooks: `onOccupied`, `onEmpty`.

### Doors

| Command | Purpose |
|---|---|
| `door/open <Door|Group>` | Open doors. |
| `door/close <Door|Group>` | Close doors. |
| `door/toggle <Door|Group>` | Open if closed, close if open. |

Hooks: `onOpen`, `onOpening`, `onClose`, `onClosing`.

### Screens

| Command | Purpose |
|---|---|
| `screen/bgcolor <Screen|Group> <Color>` | Set background color. |
| `screen/color <Screen|Group> <Color>` | Set text/foreground color. |
| `screen/print <Screen|Group> <Text>` | Print text to a screen. |

Colors can be names such as `red`, `green`, `blue`, `yellow`, `cyan`, `magenta`, `orange`, `white`, `black`, RGB triplets like `255,0,0`, or hex like `#FF0000`.

### Gas Tanks

| Command | Purpose |
|---|---|
| `tank/stockpile <Tank|Group> <true|false>` | Set stockpile. |
| `tank/share <Tank|Group> <true|false>` | Set share stockpile behavior where supported. |
| `tank/toggle <Tank|Group>` | Toggle stockpile. |

### Hinges, Rotors, and Pistons

Hinges and rotors share a lot of mechanical behavior: target angle, limits, lock/unlock, attach/detach, speed control, and reset. Pistons use distance/speed/limits instead of angle.

| Family | Commands |
|---|---|
| Hinge | `hinge/rotate`, `hinge/ulimit`, `hinge/llimit`, `hinge/lock`, `hinge/unlock`, `hinge/reset`, `hinge/attach`, `hinge/detach`, `hinge/speed` |
| Rotor | `rotor/rotate`, `rotor/ulimit`, `rotor/llimit`, `rotor/lock`, `rotor/unlock`, `rotor/reset`, `rotor/attach`, `rotor/detach`, `rotor/speed` |
| Piston | `piston/distance`, `piston/ulimit`, `piston/llimit`, `piston/stop`, `piston/attach`, `piston/detach`, `piston/reset`, `piston/speed` |

Common options observed in docs/script behavior:

- `--speed=<number>` for movement speed.
- `--free=true` for speed commands where limits/lock should not constrain motion.
- Shared motion options where a group can split a target across multiple blocks.

Hooks: `onMoving`, `onStop`, plus attach/detach hooks from mechanical connection monitoring.

### Landing Gear

| Command | Purpose |
|---|---|
| `gear/lock <Gear|Group>` | Lock landing gear. |
| `gear/unlock <Gear|Group>` | Unlock landing gear. |
| `gear/toggle <Gear|Group>` | Toggle lock. |
| `gear/auto <Gear|Group> <true|false>` | Set autolock. |

Hooks: `onLock`, `onUnlock`, `onReady`.

### Lights

| Command | Purpose |
|---|---|
| `light/color <Light|Group> <Color>` | Set light color. |
| `light/blink <Light|Group> <Preset|Interval> [--length=...] [--offset=...]` | Set blink behavior. |
| `light/intensity <Light|Group> <Value>` | Set intensity. |
| `light/reset <Light|Group>` | Reset light blink/color-ish settings. |

Blink presets found in the script include named modes such as `off`/short/medium-like presets, with custom interval/length/offset also supported.

### Other Blocks

| Command | Purpose |
|---|---|
| `pb/run <ProgrammableBlock|Group> <Argument>` | Run another programmable block. |
| `sorter/drain <Sorter|Group> <true|false>` | Set conveyor sorter drain-all behavior. |
| `sound/play <SoundBlock|Group>` | Play sound. |
| `sound/stop <SoundBlock|Group>` | Stop sound. |
| `sound/set <SoundBlock|Group> <SoundName>` | Change selected sound. |
| `thruster/thrust <Thruster|Group> <Override>` | Set thrust override. |
| `timer/start <Timer|Group>` | Start timer. |
| `timer/trigger <Timer|Group>` | Trigger timer now. |
| `timer/stop <Timer|Group>` | Stop timer. |
| `wheel/height <Wheel|Group> <Value>` | Set suspension height. |
| `wheel/power <Wheel|Group> <Value>` | Set propulsion/power. |
| `wheel/friction <Wheel|Group> <Value>` | Set friction. |
| `wheel/strength <Wheel|Group> <Value>` | Set suspension strength. |

## Mother OS Routine Syntax

Commands are separated with semicolons:

```text
light/color AirlockLight red; wait 5; light/color AirlockLight green;
```

Use quotes around names with spaces:

```text
door/open "Main Hangar Door";
```

Use `this` inside a block hook to refer to the block that fired the hook:

```ini
[hooks]
onOpen=
| wait 5;
| door/close this;
```

Use custom commands/routines in the Mother programmable block custom data:

```ini
[Commands]
DeployDrill=
| hinge/rotate DrillHinge 0;
| rotor/rotate DrillRotor 65;
| piston/distance DrillPiston 10 --speed=0.2;
```

Remote commands use an `@GridName` target form:

```text
@Mothership door/open MainHangarDoor;
```

## Mother OS Hooks

Hooks can live on the block custom data:

```ini
[hooks]
onPressurized=light/color "Airlock Light" green; door/open "Inner Door";
onDepressurized=light/color "Airlock Light" red; door/open "Outer Door";
```

Or centrally on the Mother programmable block, with the block/group name prefixed:

```ini
[hooks]
DrillPiston.onOn=light/color DrillIndicatorLight green;
"Emergency Batteries".onOff=light/blink "Battery Indicators" off;
```

Known hook families:

- Terminal blocks: `onOn`, `onOff`
- Air vents: `onDepressurized`, `onDepressurizing`, `onPressurized`, `onPressurizing`
- Cockpits: `onOccupied`, `onEmpty`
- Doors: `onOpen`, `onOpening`, `onClose`, `onClosing`
- Landing gear: `onLock`, `onUnlock`, ready-to-lock style events
- Mechanical connection blocks: `onAttach`, `onDetach`
- Merge blocks: `onMerge`, `onUnmerge`
- Moving mechanics: `onMoving`, `onStop`
- Script lifecycle: `onBoot`

## Mother GUI Configuration

Mother GUI is configured with `Custom Data` in two places:

- The Mother GUI programmable block: named menus and default menu.
- Each display-capable block: surface assignments and optional inline menus.

### Surface Assignments

```ini
[general]
scale=1.15
size=0

[surfaces]
0=MainMenu
1=RotorView "Port Rotor"
```

Rules:

- Surface indices are zero-based.
- Parameters are optional.
- Names with spaces should be quoted.
- `scale` adjusts render scale.
- `size` overrides default text size behavior where needed.

Supported display targets include LCD/text panels and multi-surface blocks such as cockpits, programmable blocks, and sound blocks. Multi-surface targets can use `Block Name:SurfaceIndex` style addressing.

### Named Menus

Named menus live on the Mother GUI programmable block:

```ini
[general]
defaultMenu=MainMenu

[menu:MainMenu]
Mechanical=
.Ramp=view/go self "RotorView" "Ramp Rotor"
.Lift=view/go self "PistonView" "Lift Piston"
.Hangar Door=view/go self "DoorView" "Hangar Door"
Power=
.Batteries=battery/auto Main Batteries
```

### Inline Menus

Inline menus live directly on a display block:

```ini
[surfaces]
0=MenuView

[menu]
Airlock=
.Open Outer Door=door/open "Outer Door"
.Close Outer Door=door/close "Outer Door"
.Depressurize=vent/depressurize "Airlock Vent"
```

### Nested Menu Syntax

Leading dots define depth. Every line needs `=`.

```ini
[menu:EngineeringMenu]
Power=
.Reactors=
..Main On=block/on Main Reactor
..Main Off=block/off Main Reactor
.Batteries=
..Charge=battery/charge Main Batteries
..Auto=battery/auto Main Batteries

Mechanical=
.Lift=view/go self "PistonView" "Lift Piston"
.Ramp=view/go self "RotorView" "Ramp Rotor"
```

Duplicate labels can be hidden behind an internal ID:

```ini
[menu]
Light 1=
.1:Red=light/color MenuLight1 red
.2:Green=light/color MenuLight1 green

Light 2=
.3:Red=light/color MenuLight2 red
.4:Green=light/color MenuLight2 green
```

## Mother GUI Commands

### Navigation

| Command | Purpose |
|---|---|
| `view/up <Display>` | Move cursor up. |
| `view/down <Display>` | Move cursor down. |
| `view/select <Display>` | Select the current item or enter a group. |
| `view/back <Display>` | Go back one menu/view step. |
| `view/go <Display> <ViewOrMenu> [Parameter]` | Open a view, switch to a named menu, or jump to a menu path. |

Examples:

```text
view/up "Bridge LCD";
view/down "Bridge LCD";
view/select "Bridge LCD";
view/back "Bridge LCD";
view/go "Bridge LCD" "RotorView" "Port Rotor";
view/go "Bridge LCD" "EngineeringMenu";
view/go "Bridge LCD" "BridgeMenu > Ship Systems > Mechanical";
```

Inside a menu item, use `self` to target the display that owns the menu:

```ini
.Hangar Door=view/go self "DoorView" "Hangar Door"
```

### Screen Management

| Command | Purpose |
|---|---|
| `screen/content <Display> none|script|text` | Change content type. |
| `screen/script <Display> <ScriptName>` | Put screen in script mode and pick a script. |
| `screen/scripts <Display>` | Print available display scripts. |

Examples:

```text
screen/content "Bridge LCD" text;
screen/content "Bridge LCD" script;
screen/script "Bridge LCD" "TSS_ArtificialHorizon";
screen/scripts "Bridge LCD";
```

Built-in script names called out in the docs include:

- `TSS_ClockAnalog`
- `TSS_ArtificialHorizon`
- `TSS_ClockDigital`
- `TSS_FactionIcon`
- `TSS_EnergyHydrogen`
- `TSS_FactionStationAdvert`
- `TSS_Gravity`
- `TSS_Velocity`
- `TSS_TargetingInfo`
- `TSS_VendingMachine`
- `TSS_Weather`
- `TSS_Jukebox`

## Mother GUI Views

| View | Purpose | Parameter |
|---|---|---|
| `MenuView` | Hierarchical menu navigation and command execution. | Optional named/inline menu resolution. |
| `RotorView` | Live rotor dial: angle, RPM, torque, braking torque, lock state. | Optional rotor name. |
| `HingeView` | Live hinge semi-circular dial and lock state. | Optional hinge name. |
| `PistonView` | Live piston extension, limits, velocity, bar. | Optional piston name. |
| `DoorView` | Live door open percent and state. | Optional door name. |

If a block-specific view has no parameter, Mother GUI falls back to the first matching block it can resolve.

Widescreen behavior:

- On wide displays, `view/go` can open a live view in a side panel while the menu remains visible.
- On smaller displays, the selected view replaces the menu until `view/back`.

## Practical Combined Pattern

Use Mother GUI as a cockpit/control-room interface and Mother OS as the executor.

```ini
; Cockpit custom data
[general]
scale=1.05

[surfaces]
0=MainMenu
1=DoorView "Outer Airlock Door"
```

```ini
; Mother GUI programmable block custom data
[general]
defaultMenu=MainMenu

[menu:MainMenu]
Airlock=
.Pressurize=vent/pressurize "Airlock Vent"
.Depressurize=vent/depressurize "Airlock Vent"
.Inner Door=door/open "Inner Door"
.Outer Door=door/open "Outer Door"
.Door Status=view/go self "DoorView" "Outer Airlock Door"

Power=
.Battery Auto=battery/auto "Main Batteries"
.Battery Charge=battery/charge "Main Batteries"

Lights=
.Ready=light/color "Status Lights" green
.Warning=light/blink "Status Lights" med
.Clear=light/blink "Status Lights" off
```

```ini
; Airlock vent custom data
[hooks]
onPressurized=light/color "Airlock Light" green; door/open "Inner Door";
onDepressurized=light/color "Airlock Light" red; door/open "Outer Door";
```

## Notes For Future Refactoring

If you want a human-readable source tree later, the clean route is not line-by-line deobfuscation. It is better to rebuild a wrapper/reference implementation around the public command surface:

- `Core/App`
- `Core/CommandBus`
- `Core/CommandContext`
- `Core/BlockCatalogue`
- `Core/Hooks`
- `Core/Scheduler`
- `Core/Storage`
- `Core/Network`
- `Modules/Blocks`
- `Modules/Mechanics`
- `Modules/Lights`
- `Modules/Screens`
- `GUI/MenuModel`
- `GUI/MenuRenderer`
- `GUI/ViewRegistry`
- `GUI/Views/RotorView`
- `GUI/Views/HingeView`
- `GUI/Views/PistonView`
- `GUI/Views/DoorView`

Suggested temporary folder currently created:

```text
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\__pending_name_mother_reference
```

Rename ideas to discuss:

- `Mother-Control-Reference`
- `MotherOS-GUI-Workbench`
- `Ship-Control-Console`
- `Mother-Bridge-Toolkit`
- `SE-Mother-Automation-Lab`
