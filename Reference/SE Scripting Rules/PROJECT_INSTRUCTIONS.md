# Project Instructions For Space Engineers Scripts

Use these instructions whenever creating, copying, repairing, or reviewing a
Space Engineers 1 Programmable Block script.

## Required Workflow

1. Keep the complete final script below **100,000 characters**. This is a hard
   project limit, including comments and whitespace.
2. Read every supplied reference script completely before writing or editing.
   Work deliberately. Reuse working reference code as closely as practical and
   make only the changes needed for the requested behavior.
3. Preserve the reference script's established naming, state handling,
   configuration format, update frequency, and helper methods unless a change
   is necessary.
4. Never provide placeholders, pseudocode, omitted sections, or "unchanged
   code here" in a requested final script. Provide one complete ready-to-paste
   script.
5. Do not claim that a script works until it has been checked against the
   Programmable Block API and reviewed with the verification checklist.

## Compatibility Rules

- Target **Space Engineers 1 Programmable Block C#**, not a session mod,
  plugin, Visual Script, or ordinary modern .NET project.
- Use only types and members exposed by the current Programmable Block API.
- Do not assume a C# feature or .NET API is allowed merely because an external
  IDE compiles it.
- Do not use `MyAPIGateway`; it is a ModAPI entry point, not a Programmable
  Block API entry point.
- Respect runtime instruction limits. Spread heavy work across updates when
  necessary.
- Avoid unnecessary allocations, repeated full-grid scans, and repeated
  parsing inside frequently called code.

## Mandatory TypeId Check

Every TypeId must be classified and verified before use:

- **Block definition TypeId** identifies a placed block definition. It commonly
  looks like `MyObjectBuilder_CargoContainer` and is paired with a block
  SubtypeId.
- **Inventory item TypeId** identifies an item stack. It commonly looks like
  `MyObjectBuilder_Ore`, `MyObjectBuilder_Ingot`,
  `MyObjectBuilder_Component`, or `MyObjectBuilder_AmmoMagazine`.
- Never use a block TypeId where an item TypeId is expected, or an item TypeId
  where a block definition is expected.
- Verify every TypeId/SubtypeId pair against the installed game definitions,
  current API output, or a known-good supplied reference.
- Treat an unknown, guessed, or ambiguous TypeId as an error until verified.
- Read and check both required local catalogs before finalizing TypeId logic:
  `SE_BlockTypeIds.md` for blocks and `SE_ItemTypeIds.md` for items.
- The local catalogs and SBC files use short TypeIds such as `CargoContainer`,
  `Ore`, and `Component`. Runtime `TypeIdString` and parsed script IDs commonly
  use `MyObjectBuilder_CargoContainer`, `MyObjectBuilder_Ore`, and
  `MyObjectBuilder_Component`. Do not mistake the prefix difference for a
  different definition.

## Mandatory Final Review

Before presenting any file:

- Check matching `{}`, `()`, `[]`, and quotation marks.
- Check commas, semicolons, operators, and method-call arguments.
- Check every variable, method, class, enum, and interface reference.
- Check generic types, casts, null handling, list indices, and inventory
  indices.
- Check `Main` update-source handling and `Runtime.UpdateFrequency`.
- Check block scope, ownership assumptions, grid scope, and block-name
  assumptions.
- Check every block and item TypeId/SubtypeId pair.
- Check that the complete script is under 100,000 characters.
- Re-read suspicious or inconsistent lines and correct all discovered errors.

See [07_VERIFICATION_CHECKLIST.md](07_VERIFICATION_CHECKLIST.md) for the full
review procedure.
