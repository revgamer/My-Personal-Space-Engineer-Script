# Sprite Drawing On LCDs And Text Surfaces

Sprites let a Programmable Block draw text, icons, textures, and simple shapes
on any accessible `IMyTextSurface`.

## Exact Sprite Name Catalog

Use the generated Sprite Listing as the primary visual catalog:

<https://malforge.github.io/spaceengineers/pbapi/Sprite-Listing.html>

It shows each exact sprite ID, native image size, and thumbnail. Always copy
the ID exactly. Some listed entries are flagged as missing/bad definitions, so
confirm questionable entries with `surface.GetSprites(...)` in the current
game.

## Required Setup

```csharp
IMyTextSurface surface = Me.GetSurface(0);
surface.ContentType = ContentType.SCRIPT;
surface.Script = "";
surface.ScriptBackgroundColor = Color.Black;
```

- Set `ContentType` to `ContentType.SCRIPT`.
- Clear `surface.Script` when drawing your own sprites.
- Obtain the correct surface from an `IMyTextSurfaceProvider`.
- Check `SurfaceCount` before calling `GetSurface(index)`.

## Coordinate System

Sprite positions are measured in pixels relative to the full texture canvas.

```csharp
Vector2 viewportPosition = (surface.TextureSize - surface.SurfaceSize) * 0.5f;
Vector2 viewportCenter = viewportPosition + surface.SurfaceSize * 0.5f;
```

- `TextureSize` is the full drawable texture.
- `SurfaceSize` is the visible area.
- The visible area's top-left corner is usually not `(0, 0)`.
- Add `viewportPosition` to coordinates intended to be relative to the visible
  surface.
- Different LCD shapes and block surfaces have different aspect ratios. Do not
  assume every surface is square.

## Drawing Frame

All sprites for one update are submitted through a frame:

```csharp
using (MySpriteDrawFrame frame = surface.DrawFrame())
{
    // Add sprites here.
}
```

The frame must be disposed. The `using` block submits the frame when it ends.
Do not retain a frame between script runs.

## Drawing Text

```csharp
var text = MySprite.CreateText(
    "SYSTEM ONLINE",
    "Debug",
    Color.White,
    1.2f,
    TextAlignment.CENTER);

text.Position = viewportCenter;

using (MySpriteDrawFrame frame = surface.DrawFrame())
{
    frame.Add(text);
}
```

Text rules:

- Verify the font name. Common choices include `Debug` and `Monospace`.
- `RotationOrScale` controls text scale.
- `Alignment` affects how text is anchored to `Position`.
- Use `surface.MeasureStringInPixels(...)` when layout depends on exact text
  dimensions.

## Drawing Shapes And Textures

Sprites with `SpriteType.TEXTURE` use a texture name:

```csharp
var background = new MySprite(
    SpriteType.TEXTURE,
    "SquareSimple",
    viewportCenter,
    surface.SurfaceSize,
    new Color(12, 18, 28));

var bar = new MySprite(
    SpriteType.TEXTURE,
    "SquareSimple",
    viewportPosition + new Vector2(20f, 100f),
    new Vector2(300f, 24f),
    Color.Green,
    null,
    TextAlignment.LEFT);
```

Useful built-in texture concepts include:

- `SquareSimple` for rectangles, bars, borders, and backgrounds.
- `Circle` for circular indicators.
- `CircleHollow`, `SquareHollow`, `Triangle`, `RightTriangle`, and `SemiCircle`
  for simple dashboard geometry.
- `Arrow`, `Cross`, `Danger`, `Online`, `Offline`, and `No Entry` for status
  indicators.
- `IconEnergy`, `IconHydrogen`, and `IconOxygen` for resource dashboards.
- Game icon textures for status displays.

Texture names must exist in the game's sprite catalog. Never guess an icon name
in a final script. Verify it from the current game or by listing available
sprites.

## Inventory Item Icons As Sprites

Many inventory item icons use their full runtime Item TypeId/SubtypeId as the
sprite ID:

```csharp
"MyObjectBuilder_Ore/Iron"
"MyObjectBuilder_Ingot/Uranium"
"MyObjectBuilder_Component/SteelPlate"
"MyObjectBuilder_AmmoMagazine/NATO_25x184mm"
"MyObjectBuilder_GasContainerObject/HydrogenBottle"
```

This is another reason to distinguish inventory Item TypeIds from block
definition TypeIds. Check the exact item pair in `SE_ItemTypeIds.md`, then
confirm that the corresponding sprite exists in the Sprite Listing or through
`GetSprites`.

## Native Size And Display Size

The Sprite Listing's size is the source texture's native aspect ratio, not the
required on-screen size. A sprite can be drawn at any `Size`, but stretching a
non-square source into a square will distort it.

Examples:

- `SquareSimple` is `4x4` and scales cleanly into rectangles.
- `Circle` is `512x512` and should usually be drawn with equal width/height.
- `Online_wide` and `Offline_wide` are `512x128`, a 4:1 aspect ratio.
- Item icons are commonly `128x128`.
- Poster and LCD art may be landscape, portrait, or square.

Preserve the native aspect ratio when distortion matters.

## Listing Available Sprite Textures

```csharp
readonly List<string> _sprites = new List<string>();

void ListSprites(IMyTextSurface surface)
{
    _sprites.Clear();
    surface.GetSprites(_sprites);

    for (int i = 0; i < _sprites.Count; i++)
        Echo(_sprites[i]);
}
```

Because the list can be large, print or process it over multiple runs if
necessary.

## Complete Dashboard Example

```csharp
void DrawDashboard(IMyTextSurface surface, float chargePercent)
{
    surface.ContentType = ContentType.SCRIPT;
    surface.Script = "";

    Vector2 viewport = (surface.TextureSize - surface.SurfaceSize) * 0.5f;
    Vector2 center = viewport + surface.SurfaceSize * 0.5f;

    float clamped = MathHelper.Clamp(chargePercent, 0f, 1f);
    float maximumWidth = surface.SurfaceSize.X - 40f;
    float filledWidth = maximumWidth * clamped;

    using (MySpriteDrawFrame frame = surface.DrawFrame())
    {
        frame.Add(new MySprite(
            SpriteType.TEXTURE,
            "SquareSimple",
            center,
            surface.SurfaceSize,
            new Color(10, 14, 20)));

        frame.Add(new MySprite(
            SpriteType.TEXTURE,
            "SquareSimple",
            viewport + new Vector2(20f, 130f),
            new Vector2(maximumWidth, 30f),
            new Color(45, 50, 60),
            null,
            TextAlignment.LEFT));

        frame.Add(new MySprite(
            SpriteType.TEXTURE,
            "SquareSimple",
            viewport + new Vector2(20f, 130f),
            new Vector2(filledWidth, 30f),
            clamped < 0.2f ? Color.Red : Color.Green,
            null,
            TextAlignment.LEFT));

        MySprite title = MySprite.CreateText(
            "BATTERY",
            "Debug",
            Color.White,
            1.2f,
            TextAlignment.CENTER);
        title.Position = viewport + new Vector2(surface.SurfaceSize.X * 0.5f, 50f);
        frame.Add(title);

        MySprite value = MySprite.CreateText(
            (clamped * 100f).ToString("0") + "%",
            "Debug",
            Color.White,
            1f,
            TextAlignment.CENTER);
        value.Position = viewport + new Vector2(surface.SurfaceSize.X * 0.5f, 190f);
        frame.Add(value);
    }
}
```

## Layout And Rotation Rules

- Base layout on `SurfaceSize`, not hard-coded assumptions about a specific
  LCD.
- Use consistent margins and relative positions.
- `RotationOrScale` means rotation in radians for texture sprites and scale for
  text sprites.
- Sprite `Alignment` changes the anchor point for position and size.
- Test corner LCDs, cockpit screens, wide LCDs, and transparent LCDs
  separately.

## Performance Rules

- Redraw only when values change or at a sensible slow update frequency.
- Avoid creating hundreds of sprites every tick.
- Cache static text and layout values where practical.
- Keep sprite-list discovery out of the normal drawing loop.
- Build and dispose one frame per surface update.
- Spread updates across ticks when drawing to many surfaces.

## Sprite Verification Checklist

- Surface exists and its index is valid.
- `ContentType` is `SCRIPT`.
- Visible viewport offset is included.
- Textures and fonts exist.
- Positions and sizes fit the visible surface.
- Alignment and rotation/scale are intentional.
- Frame is disposed.
- Redraw frequency and sprite count are bounded.

## References

- Exact Sprite Listing:
  <https://malforge.github.io/spaceengineers/pbapi/Sprite-Listing.html>
- Displaying things:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Displaying_Things>
- PB API `IMyTextSurface`, `MySprite`, and `MySpriteDrawFrame`:
  <https://malforge.github.io/spaceengineers/pbapi/>
