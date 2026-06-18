# Final Script Verification Checklist

Complete this checklist before presenting a script or calling it ready to
paste.

## 1. Reference Fidelity

- Read every supplied reference script from beginning to end.
- Confirm copied sections were not shortened, omitted, or subtly changed.
- Confirm required names, tags, groups, commands, configuration keys, and
  default values match the request.
- Confirm all deliberate changes are understood.

## 2. Syntax And Structure

- Match every `{` with `}`, `(` with `)`, and `[` with `]`.
- Match all string quotation marks.
- Check commas, semicolons, colons, periods, and operators.
- Check every method call's argument count, order, and types.
- Check every declared method's return paths.
- Check scopes and ensure names are declared before use.
- Check duplicate declarations and spelling/capitalization differences.

## 3. Programmable Block Compatibility

- Confirm this is PB code, not ModAPI, plugin, or ordinary .NET code.
- Confirm every API type, property, and method exists in the current PB API.
- Account for known PB API documentation link-generator defects documented in
  `06_API_REFERENCE_MAP.md`; a broken generated link is not proof that the API
  member is unavailable.
- Confirm no `MyAPIGateway` or forbidden API is used.
- Confirm used C# and .NET features are accepted by the in-game compiler.
- Confirm the complete script is below 100,000 characters.

## 4. Lifecycle And Runtime

- Check `Program()`, `Main`, and `Save()` signatures.
- Check `Runtime.UpdateFrequency` and `UpdateType` flag handling.
- Check recurring work is bounded and not unnecessarily run every tick.
- Check lists and caches do not grow forever.
- Check heavy work is staged when needed.
- Check exceptions cannot silently disable essential behavior.

## 5. Blocks And Scope

- Check every block lookup for null/missing results.
- Check exact grid versus same-construct scope.
- Check block groups and custom names are documented.
- Check casts use the correct `IMy...` interface.
- Check block state tests use the intended property.
- Check exact block variants by `BlockDefinition` when required.
- Prefer typed interface members over terminal string properties/actions.
- Verify every terminal property/action ID and its value type.

## 6. Block TypeId And Item TypeId

- Read both required local TypeId catalogs completely.
- Classify every TypeId as either a block definition or inventory item.
- Check no block TypeId is used for an inventory item.
- Check no item TypeId is used as a block definition.
- Check short SBC/catalog form versus full runtime `MyObjectBuilder_` form.
- Verify every TypeId/SubtypeId pair from installed definitions, current API
  output, or a known-good reference.
- Check case and spelling exactly.
- Check modded definitions separately.

## 7. Inventories

- Check `HasInventory`, `InventoryCount`, and every inventory index.
- Check source and destination inventory roles.
- Check conveyor connectivity and destination acceptance assumptions.
- Check transfer return values where available.
- Check stack/index changes during transfers.
- Check `MyFixedPoint` conversions do not lose required precision.

## 8. Persistence, Displays, Sprites, And IGC

- Check `Storage` parsing and version/fallback behavior.
- Check `Me.CustomData` errors are reported clearly.
- Check surface indexes and `ContentType`.
- Check text does not append forever accidentally.
- Check sprite positions account for `TextureSize` and `SurfaceSize`.
- Check every sprite frame is disposed.
- Check sprite texture names against the Sprite Listing or `GetSprites`.
- Check sprite native aspect ratios and font names.
- Check sprite count and redraw frequency are bounded.
- Check IGC tag, payload type, sender assumptions, callback, and queue bounds.

## 9. Final Read-Through

- Read the entire final script slowly from top to bottom.
- Recheck every suspicious, inconsistent, or unusually formatted line.
- Search for `TODO`, placeholder text, omitted-code comments, and debug-only
  behavior.
- Confirm the delivered file is complete and ready to paste.

## Recommended Test Sequence

1. Compile in an SE-aware IDE or MDK environment.
2. Paste and compile in a test-world Programmable Block.
3. Test with required blocks present.
4. Test with required blocks missing or renamed.
5. Test empty and full inventories where relevant.
6. Test save/reload and recompile behavior.
7. Test commands, update triggers, and IGC callbacks.
8. Watch PB detailed info for exceptions, instruction-limit failures, and
   unexpected runtime cost.

## References

- Debugging:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Debugging_Your_Scripts>
- MDK: <https://spaceengineers.wiki.gg/wiki/Scripting/MDK>
- PB API: <https://malforge.github.io/spaceengineers/pbapi/>
