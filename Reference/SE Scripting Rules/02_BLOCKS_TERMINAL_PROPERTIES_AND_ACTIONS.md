# Blocks And The Grid Terminal System

## Prefer Interface Types

Use the narrowest useful in-game interface:

```csharp
IMyBatteryBlock battery;
IMyCargoContainer cargo;
IMyShipConnector connector;
IMyTextPanel panel;
```

An interface type such as `IMyCargoContainer` is not the same thing as a block
definition TypeId such as `MyObjectBuilder_CargoContainer`.

## Finding Blocks

Exact name:

```csharp
var panel = GridTerminalSystem.GetBlockWithName("Status LCD") as IMyTextPanel;
```

By interface:

```csharp
readonly List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();

void RefreshBatteries()
{
    _batteries.Clear();
    GridTerminalSystem.GetBlocksOfType(_batteries, b => b.IsSameConstructAs(Me));
}
```

By group:

```csharp
readonly List<IMyTerminalBlock> _groupBlocks = new List<IMyTerminalBlock>();

bool RefreshGroup(string name)
{
    _groupBlocks.Clear();
    var group = GridTerminalSystem.GetBlockGroupWithName(name);
    if (group == null)
        return false;

    group.GetBlocks(_groupBlocks);
    return true;
}
```

## Grid And Construct Scope

Understand the intended scope before filtering:

- `block.CubeGrid == Me.CubeGrid` means the exact same grid only.
- `block.IsSameConstructAs(Me)` includes mechanically connected grids in the
  same construct, such as grids attached through rotors, hinges, or pistons.
- Connectors can change which blocks are visible or considered connected in
  ways that matter to the script. Test the intended behavior.
- Block groups and names are user-controlled and may be missing or duplicated.

## Block State Checks

Common concepts:

- `IsFunctional`: built enough and not too damaged to function.
- `IsWorking`: currently able to work under present conditions.
- `Enabled`: terminal on/off state for functional blocks.
- `Closed`: block/entity is no longer usable.

Use the property that matches the behavior being tested. An enabled block is
not necessarily working.

## Controlling Blocks

Prefer typed properties and methods:

```csharp
battery.ChargeMode = ChargeMode.Recharge;
connector.Connect();
door.OpenDoor();
```

Terminal actions (`ApplyAction`) and terminal properties are useful for
features not exposed through a typed member, but their string IDs are less
discoverable and easier to break. Verify action/property IDs before use.

## Definition Identity

When exact block variants matter, inspect the block definition:

```csharp
MyDefinitionId id = block.BlockDefinition;
string typeId = id.TypeId.ToString();
string subtypeId = id.SubtypeId.ToString();
```

Never guess an exact subtype from the display name. Display names and custom
names are not stable definition identifiers.

## References

- Grid Terminal System:
  <https://spaceengineers.wiki.gg/wiki/Scripting/The_Grid_Terminal_System>
- Controlling things:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Controlling_Things>
- PB type listing:
  <https://malforge.github.io/spaceengineers/pbapi/Type-Definition-Listing.html>

# Terminal Properties And Actions

Terminal properties and actions expose controls shown in a block's terminal.
They use string IDs such as `OnOff`, `Recharge`, `Radius`, or `ShowOnHUD`.

The current PB API documentation warns that these are largely obsolete for
vanilla blocks because typed `IMy...` interfaces now expose most functionality
with less overhead. Prefer typed properties and methods whenever available.

## Source Listing

- Complete generated list by block interface:
  <https://malforge.github.io/spaceengineers/pbapi/List-Of-Terminal-Properties-And-Actions.html>
- Official explanation:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Terminal_Properties_And_Actions>

## Applying An Action

```csharp
IMyTerminalAction action = block.GetActionWithName("OnOff_On");
if (action != null)
    action.Apply(block);
```

The extension method form is also available:

```csharp
block.ApplyAction("OnOff_On");
```

Use `GetActionWithName` when the action may not exist and the script needs to
handle that safely.

## Reading And Writing A Terminal Property

```csharp
ITerminalProperty property = block.GetProperty("ShowOnHUD");
if (property != null && property.TypeName == "Boolean")
{
    bool current = property.As<bool>().GetValue(block);
    property.As<bool>().SetValue(block, !current);
}
```

Convenience extension methods exist for common types:

```csharp
bool shown = block.GetValueBool("ShowOnHUD");
block.SetValueBool("ShowOnHUD", true);

float radius = block.GetValueFloat("Radius");
block.SetValueFloat("Radius", 5000f);
```

Property IDs and types must match exactly.

## Discovering Actions And Properties

```csharp
readonly List<IMyTerminalAction> _actions = new List<IMyTerminalAction>();
readonly List<ITerminalProperty> _properties = new List<ITerminalProperty>();

void PrintTerminalControls(IMyTerminalBlock block)
{
    _actions.Clear();
    block.GetActions(_actions);
    for (int i = 0; i < _actions.Count; i++)
        Echo("Action: " + _actions[i].Id);

    _properties.Clear();
    block.GetProperties(_properties);
    for (int i = 0; i < _properties.Count; i++)
        Echo("Property: " + _properties[i].Id + " : " + _properties[i].TypeName);
}
```

Runtime discovery is especially useful for modded blocks and after game
updates. Bound or stage the output because some blocks expose many entries.

## Terminal Access Rules

- Prefer a typed `IMy...` property or method first.
- Treat action/property IDs as case-sensitive exact strings.
- Check for `null` before using a discovered action or property.
- Verify the property's type before reading or writing it.
- Do not confuse terminal IDs with block or item TypeIds.
- Expect modded blocks to add or omit terminal controls.
- Do not enumerate all actions/properties during every update.
