# Core Programmable Block Rules

## Script Shape

The editor wraps the submitted code into a generated `Program` class. The
usual entry points are:

```csharp
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save()
{
    Storage = _state;
}

public void Main(string argument, UpdateType updateSource)
{
    Echo("Running");
}
```

- `Program()` runs when the script is compiled or loaded. Initialize fields,
  read configuration, restore `Storage`, find blocks, and set update frequency
  here.
- `Main(string argument, UpdateType updateSource)` runs when triggered.
- `Save()` is called when the world saves and before recompilation in relevant
  cases. Put persistent state into `Storage`.
- Fields persist between calls while the PB instance remains loaded.
- Local variables do not persist after the method returns.

## Built-In Program Members

Common members available to the script include:

| Member | Purpose |
| --- | --- |
| `GridTerminalSystem` | Find accessible terminal blocks and groups. |
| `Me` | This Programmable Block as `IMyProgrammableBlock`. |
| `Runtime` | Update frequency and runtime/instruction information. |
| `Storage` | Persistent string storage for this script. |
| `IGC` | Inter-Grid Communication. |
| `Echo(text)` | Write diagnostic text to the PB terminal detail area. |

## Programmable Block Versus Mod API

Programmable Blocks run in a restricted environment.

- Use namespaces and members listed in the PB API documentation.
- `Sandbox.ModAPI.Ingame` interfaces are intended for PB use.
- `MyAPIGateway` and unrestricted game/session access belong to mods.
- A member visible in a DLL or the general ModAPI docs may still be forbidden
  in a PB.
- Compilation in Visual Studio alone does not prove in-game compatibility.

## Update Sources

`updateSource` is a flags value. Test flags with bitwise operations:

```csharp
if ((updateSource & UpdateType.Update100) != 0)
{
    RunPeriodicWork();
}

if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0)
{
    HandleCommand(argument);
}
```

Do not rely on equality when more than one flag may be present.

## Configuration

Use `Me.CustomData` for user-editable configuration. Prefer
`MyIni` over fragile manual splitting when the configuration is INI-shaped.

```csharp
MyIni _ini = new MyIni();

bool ReadConfig()
{
    MyIniParseResult result;
    if (!_ini.TryParse(Me.CustomData, out result))
    {
        Echo(result.ToString());
        return false;
    }

    string groupName = _ini.Get("General", "GroupName").ToString("Cargo");
    return true;
}
```

Keep persistent machine state in `Storage`, not mixed into user configuration,
unless the reference script intentionally uses another format.

## Failure Handling

- Validate block lookups before dereferencing them.
- Handle missing groups, renamed blocks, empty inventories, and disconnected
  conveyor paths.
- Prefer a clear `Echo` error and skipped operation over a null-reference
  exception.
- During debugging, catch exceptions at callback boundaries only when useful,
  print the exception, and avoid silently swallowing failures.

## References

- Anatomy: <https://spaceengineers.wiki.gg/wiki/Scripting/The_Anatomy_of_a_Script>
- First script: <https://spaceengineers.wiki.gg/wiki/Scripting/Your_First_Script>
- Debugging:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Debugging_Your_Scripts>
- PB API: <https://malforge.github.io/spaceengineers/pbapi/>

# Runtime And Performance

Space Engineers must simulate the world while running scripts. A PB execution
has a limited instruction budget; exceeding it terminates that run. Design
recurring scripts to do bounded work.

## Update Frequencies

```csharp
Runtime.UpdateFrequency = UpdateFrequency.None;
Runtime.UpdateFrequency = UpdateFrequency.Once;
Runtime.UpdateFrequency = UpdateFrequency.Update1;
Runtime.UpdateFrequency = UpdateFrequency.Update10;
Runtime.UpdateFrequency = UpdateFrequency.Update100;
```

| Frequency | Typical use |
| --- | --- |
| `None` | Command-only scripts triggered manually, by timer, or by another PB. |
| `Once` | Continue work on the next available update. |
| `Update1` | Fast control loops only when genuinely required. |
| `Update10` | Responsive automation with moderate cost. |
| `Update100` | Monitoring, inventory summaries, and slow automation. |

Use bitwise operators to combine or remove frequencies:

```csharp
Runtime.UpdateFrequency |= UpdateFrequency.Once;
Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
```

## Runtime Diagnostics

Useful `Runtime` values include:

- `CurrentInstructionCount`
- `MaxInstructionCount`
- `LastRunTimeMs`
- `TimeSinceLastRun`
- `UpdateFrequency`

Exact available members must be checked in the current PB API.

## Performance Rules

- Cache stable block references and refresh them deliberately.
- Reuse lists by calling `Clear()` before filling them again.
- Do not scan every block every tick unless the grid is known to be tiny.
- Do not rebuild large strings every tick when the display did not change.
- Cache parsed TypeIds, `MyIni` configuration, and repeated constants.
- Split large inventory scans, raycasts, sorting jobs, and display updates over
  multiple executions.
- Use `Update1` only for behavior that truly needs every simulation tick.
- Measure before optimizing: print instruction count and last run time during
  testing.

## Staged Work Pattern

```csharp
int _stage;

public void Main(string argument, UpdateType updateSource)
{
    if (_stage == 0)
        RefreshBlocks();
    else if (_stage == 1)
        ScanInventories();
    else
        UpdateDisplays();

    _stage = (_stage + 1) % 3;
}
```

For very large jobs, use a state machine or coroutine pattern and schedule
`UpdateFrequency.Once` until the job is complete.

Methods such as `GetBlocksOfType`, `GetBlocks`, `GetItems`, and group block
lookups fill a supplied list. Clear reusable lists before calls unless
appending is intentional.

## Runtime References

- Runtime: <https://spaceengineers.wiki.gg/wiki/Scripting/The_Runtime>
- Do's and Don'ts:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Do%27s_and_Don%27ts>
- Continuous running:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Continuous_Running_No_Timers_Needed>
- Coroutines:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Coroutines_-_Run_operations_over_multiple_ticks>
