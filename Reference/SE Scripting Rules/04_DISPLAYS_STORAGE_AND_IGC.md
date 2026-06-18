# Displays, Storage, Arguments, And IGC

## Text Surfaces

Some blocks are directly text panels; others expose one or more surfaces
through `IMyTextSurfaceProvider`.

```csharp
var provider = block as IMyTextSurfaceProvider;
if (provider == null || provider.SurfaceCount == 0)
    return;

IMyTextSurface surface = provider.GetSurface(0);
surface.ContentType = ContentType.TEXT_AND_IMAGE;
surface.WriteText("System online", false);
```

- Validate `SurfaceCount` before selecting an index.
- Set the appropriate `ContentType`.
- `WriteText(text, false)` replaces text; `true` appends.
- Avoid appending every update unless an ever-growing log is intentional.
- Cache and update displays only when needed.

Sprite drawing uses a frame that must be disposed:

```csharp
using (MySpriteDrawFrame frame = surface.DrawFrame())
{
    // Add sprites to frame.
}
```

See [05_SPRITE_DRAWING.md](05_SPRITE_DRAWING.md) for the full sprite drawing
reference.

## Echo

`Echo` writes diagnostics to the Programmable Block terminal details. Use it
for concise status, missing-block messages, configuration errors, and runtime
measurements. It is not an LCD replacement.

## Arguments

`Main` receives a string argument from terminal runs, timer actions, buttons,
other scripts, and callback mechanisms.

Use explicit, documented commands and handle unknown input:

```csharp
void HandleCommand(string argument)
{
    string command = (argument ?? "").Trim().ToLowerInvariant();

    if (command == "refresh")
        RefreshBlocks();
    else if (command == "status")
        PrintStatus();
    else
        Echo("Unknown command: " + argument);
}
```

For structured configuration, prefer `MyIni`; for a small command grammar,
simple deliberate parsing is usually enough.

## Storage

`Storage` is a persistent string belonging to the PB script.

- Serialize only the state needed after save/reload/recompile.
- Include a format version when stored state may evolve.
- Parse defensively; corrupted or old state should fall back safely.
- Write persistent state in `Save()` and when important transitions require it.
- Do not confuse `Storage` with `Me.CustomData`.

## Inter-Grid Communication

IGC sends messages between Programmable Blocks:

```csharp
const string Tag = "MY_STATUS";

void SendStatus(string value)
{
    IGC.SendBroadcastMessage(Tag, value, TransmissionDistance.CurrentConstruct);
}
```

Listener setup:

```csharp
IMyBroadcastListener _listener;

public Program()
{
    _listener = IGC.RegisterBroadcastListener(Tag);
    _listener.SetMessageCallback(Tag);
}
```

Reading:

```csharp
void ReadMessages()
{
    while (_listener.HasPendingMessage)
    {
        MyIGCMessage message = _listener.AcceptMessage();
        if (message.Tag != Tag)
            continue;

        string data = message.Data as string;
        if (data != null)
            Echo(data);
    }
}
```

IGC rules:

- Use stable, distinctive tags.
- Verify the sender and payload shape when trust matters.
- Only supported serializable data types can be sent.
- Bound queue processing so a message burst does not exhaust instructions.
- Callback arguments and `UpdateType.IGC` must be handled deliberately.
- Broadcast range depends on the selected transmission distance and available
  communication path.

## References

- Displaying things:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Displaying_Things>
- IGC:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Antenna_Communication_%28IGC%29>
- Allowed IGC types:
  <https://spaceengineers.wiki.gg/wiki/Scripting/IGC_Allowed_Message_Types>
