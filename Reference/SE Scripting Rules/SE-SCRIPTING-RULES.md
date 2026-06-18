# Space Engineers Programmable Block Scripting Rules

Consolidated quick reference for writing, copying, repairing, and reviewing
Space Engineers 1 Programmable Block scripts.

Last verified: 2026-06-14

For detailed explanations, use the numbered handbook chapters beside this file.

## Mandatory Working Rules

1. Keep the complete final script below **100,000 characters**.
2. Read every supplied reference script completely before editing or copying.
3. Preserve proven reference behavior unless a requested change requires
   altering it.
4. Provide complete ready-to-paste scripts, never placeholders or omitted
   sections.
5. Check both `SE_BlockTypeIds.md` and `SE_ItemTypeIds.md` before finalizing
   TypeId logic.
6. Complete the final verification checklist before presenting a script.

## What To Paste

The in-game editor expects `Program` class-body members:

```csharp
private readonly List<IMyBatteryBlock> _batteries =
    new List<IMyBatteryBlock>();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save()
{
}

public void Main(string argument, UpdateType updateSource)
{
}
```

Do not add an outer `namespace` or `Program : MyGridProgram` wrapper to a
ready-to-paste in-game script. The game supplies the wrapper and standard
imports.

## PB API Boundary

- A normal .NET or IDE compile does not prove PB compatibility.
- Use only members present in the current Programmable Block API.
- Do not use `MyAPIGateway`; it belongs to ModAPI scripts.
- No normal file access, network access, reflection, or threading.
- Prefer typed `IMy...` members over terminal action/property string IDs.
- Terminal string IDs remain useful where typed access is missing or known to
  behave differently in-game.

Primary sources:

- <https://malforge.github.io/spaceengineers/pbapi/>
- <https://spaceengineers.wiki.gg/wiki/Scripting>
- [06_API_REFERENCE_MAP.md](06_API_REFERENCE_MAP.md)

## Lifecycle And Runtime

| Member | Purpose |
| --- | --- |
| `Program()` | Initialize fields, config, storage, block caches, listeners, and update frequency. |
| `Main(string, UpdateType)` | Handle periodic updates, commands, and callbacks. |
| `Save()` | Serialize persistent state into `Storage`. |
| `Runtime` | Inspect instruction/runtime cost and schedule updates. |
| `Me.CustomData` | User-editable configuration. |
| `Storage` | Script-owned persistent string state. |

Treat `UpdateType` as flags:

```csharp
if ((updateSource & UpdateType.Update100) != 0)
    RunPeriodicWork();
```

Performance:

- Reuse lists and `StringBuilder` instances in hot paths.
- Clear lists before APIs that fill supplied lists.
- Cache stable block references and refresh deliberately.
- Spread heavy work across runs.
- Use `Update1` only when truly required.
- Measure with `Runtime.CurrentInstructionCount` and `LastRunTimeMs`.

## Block Scope

```csharp
block.CubeGrid == Me.CubeGrid
```

Means the exact same grid.

```csharp
block.IsSameConstructAs(Me)
```

Includes the same grid and mechanically linked subgrids through rotors,
pistons, hinges, and similar mechanical links. It does **not** mean exact same
grid and must not be described as excluding subgrids. Connector-linked grids
are not included by this method.

## TypeId Separation

Never mix these layers:

| Layer | Example |
| --- | --- |
| C# interface | `IMyCargoContainer` |
| Block TypeId | `CargoContainer` / `MyObjectBuilder_CargoContainer` |
| Block SubtypeId | `LargeBlockLargeContainer` |
| Item TypeId | `Component` / `MyObjectBuilder_Component` |
| Item SubtypeId | `SteelPlate` |
| Terminal property ID | `ShowOnHUD` |
| Terminal action ID | `OnOff_On` |
| Sprite item-icon ID | `MyObjectBuilder_Component/SteelPlate` |

Required catalogs:

```text
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_BlockTypeIds.md
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_ItemTypeIds.md
```

Use short TypeIds in SBC/catalog context and full `MyObjectBuilder_` forms
where the runtime API expects them.

## Configuration

`MyIni` is a built-in PB API configuration parser. Prefer it for INI-style
`CustomData`:

```csharp
readonly MyIni _ini = new MyIni();

bool ReadConfig()
{
    MyIniParseResult result;
    if (!_ini.TryParse(Me.CustomData, out result))
    {
        Echo(result.ToString());
        return false;
    }

    string tag = _ini.Get("General", "Tag").ToString("[SCRIPT]");
    return true;
}
```

Manual parsing can be appropriate for a deliberately tiny format, but it is
not required merely because the data is stored in `CustomData`.

## Important Current API Facts

- `MyItemType` has `Parse` and typed `MakeOre`, `MakeIngot`,
  `MakeComponent`, `MakeAmmo`, and related helpers. It does not expose
  `TryParse` in the current PB API.
- `MyDefinitionId` exposes `TryParse`.
- `IMyEventControllerBlock` **does exist** in the current PB API and exposes
  condition-related properties. This does not imply it can directly perform
  every toolbar/configuration operation a player can.
- `IMyTimerBlock.Trigger()` exists in the current PB API. If a known-good
  reference or in-game test requires `ApplyAction("TriggerNow")`, preserve that
  proven workaround and document why.
- `IMyLightingBlock.BlinkLength` is documented as `0` to `1`, but current
  production scripts in the reviewed repository use `50f` for 50 percent.
  Treat this as an API-versus-terminal/runtime discrepancy and verify against
  the actual target block before changing working code.
- Typed members are preferred, but Broadcast Controller message slots are a
  practical case where verified terminal properties/actions are used.

## Terminal Properties And Actions

Primary list:

<https://malforge.github.io/spaceengineers/pbapi/List-Of-Terminal-Properties-And-Actions.html>

```csharp
IMyTerminalAction action = block.GetActionWithName("TriggerNow");
if (action != null)
    action.Apply(block);
```

```csharp
ITerminalProperty property = block.GetProperty("ShowOnHUD");
if (property != null && property.TypeName == "Boolean")
    property.As<bool>().SetValue(block, true);
```

Rules:

- Confirm the exact action/property ID and property value type.
- Check for `null`.
- Prefer typed access first.
- Preserve a proven terminal-action workaround when typed access is unreliable
  in the target environment.

## Broadcast Controller Production Pattern

The reviewed HERMES/PTA production scripts use:

```csharp
StringBuilder message = new StringBuilder();
message.Append("Alert text");

broadcastController.SetValue<StringBuilder>("Message7", message);
broadcastController.ApplyAction("Transmit Message 8");
```

Message properties are zero-based (`Message0` to `Message7`) while transmit
actions are one-based (`Transmit Message 1` to `Transmit Message 8`).

## Sprite Production Pattern

Primary catalog:

<https://malforge.github.io/spaceengineers/pbapi/Sprite-Listing.html>

Useful reusable helpers from the reviewed production style:

```csharp
RectangleF Viewport(IMyTextSurface surface)
{
    return new RectangleF(
        (surface.TextureSize - surface.SurfaceSize) * 0.5f,
        surface.SurfaceSize);
}

void Fill(MySpriteDrawFrame frame, RectangleF area, Color color)
{
    frame.Add(new MySprite(
        SpriteType.TEXTURE,
        "SquareSimple",
        area.Position + area.Size * 0.5f,
        area.Size,
        color));
}
```

Sprite rules:

- Set `ContentType = ContentType.SCRIPT`.
- Clear `surface.Script` for custom drawing.
- Account for viewport offset.
- Use `using (var frame = surface.DrawFrame())`.
- Measure text with `MeasureStringInPixels` for adaptive layouts.
- Base layout on actual `SurfaceSize`.
- Verify exact sprite IDs and aspect ratios.
- Inventory item icons often use the full item ID as the sprite ID.

## Persistence And IGC

- Store machine state in `Storage`; store user config in `Me.CustomData`.
- Include a version in complex persisted formats.
- Parse stored data defensively.
- Bound IGC queue processing per run.
- Verify message tags, payload types, and sender assumptions.
- For reliable delivery, a retry queue plus unicast acknowledgment is a proven
  pattern, but it pauses while the grid is unloaded.

## Final Verification

Before presenting a script:

- Check all braces, brackets, parentheses, quotes, commas, and semicolons.
- Check every variable, method, type, interface, cast, and argument.
- Check PB API availability.
- Check block scope and missing-block handling.
- Check both Block TypeIds and Item TypeIds.
- Check inventory indices, transfers, and `MyFixedPoint` conversions.
- Check runtime/update behavior and instruction cost.
- Check sprite texture names, viewport math, and frame disposal.
- Check terminal property/action IDs and value types.
- Check complete script length is below 100,000 characters.
- Read the complete final script slowly from top to bottom.

Full checklist: [07_VERIFICATION_CHECKLIST.md](07_VERIFICATION_CHECKLIST.md)
