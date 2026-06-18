# Space Engineers 1 Programmable Block Scripting Reference

Research date: 2026-06-14

This handbook is for **Space Engineers 1 in-game Programmable Block scripts**.
It is not a guide to SE2, session mods, plugin development, or Visual Scripting.

## Read In This Order

1. [PROJECT_INSTRUCTIONS.md](PROJECT_INSTRUCTIONS.md) - rules to give an AI or
   developer before it works on a script.
2. [SE-SCRIPTING-RULES.md](SE-SCRIPTING-RULES.md) - consolidated ready-reference
   containing the most important rules and proven patterns.
3. [01_CORE_AND_RUNTIME.md](01_CORE_AND_RUNTIME.md) - script lifecycle, safe
   coding, update frequencies, instruction limits, and performance.
4. [02_BLOCKS_TERMINAL_PROPERTIES_AND_ACTIONS.md](02_BLOCKS_TERMINAL_PROPERTIES_AND_ACTIONS.md)
   - finding blocks, typed control, and terminal string IDs.
5. [03_INVENTORIES_AND_TYPEIDS.md](03_INVENTORIES_AND_TYPEIDS.md) - inventory
   access and the critical Block TypeId versus Item TypeId distinction.
6. [04_DISPLAYS_STORAGE_AND_IGC.md](04_DISPLAYS_STORAGE_AND_IGC.md) - LCDs,
   persistence, arguments, and inter-grid communication.
7. [05_SPRITE_DRAWING.md](05_SPRITE_DRAWING.md) - drawing text, shapes,
   textures, and custom dashboards on text surfaces.
8. [06_API_REFERENCE_MAP.md](06_API_REFERENCE_MAP.md) - useful PB API listings,
   link-audit findings, and how to use them.
9. [07_VERIFICATION_CHECKLIST.md](07_VERIFICATION_CHECKLIST.md) - checks to run
   before presenting or pasting a final script.

## Required Local TypeId Catalogs

For exact IDs, read these two personal reference files completely:

- Block definitions:
  `F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_BlockTypeIds.md`
- Inventory items:
  `F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_ItemTypeIds.md`

Use `SE_BlockTypeIds.md` only for placed block definitions and
`SE_ItemTypeIds.md` only for inventory item stacks. Do not mix their TypeIds.
The block catalog also covers exact subtype IDs, interfaces, grid variants,
and DLC block families.

## Source Priority

When sources disagree, use this order:

1. The currently installed game's files and behavior.
2. The two required local TypeId catalogs above.
3. The current Programmable Block API listing.
4. The official Space Engineers wiki scripting pages.
5. Keen documentation and release notes.
6. Older examples, forum posts, and Workshop scripts.

Old scripts are useful references, but APIs and definitions can change.

## Primary References

- Scripting hub: <https://spaceengineers.wiki.gg/wiki/Scripting>
- Programmable Block API: <https://malforge.github.io/spaceengineers/pbapi/>
- Type Definition Listing:
  <https://malforge.github.io/spaceengineers/pbapi/Type-Definition-Listing.html>
- Keen ModAPI reference:
  <https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/index.html>
- MDK: <https://spaceengineers.wiki.gg/wiki/Scripting/MDK>
- Installed definitions:
  `C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Content\Data`

The Keen ModAPI reference includes APIs that are not necessarily allowed in a
Programmable Block. Confirm that a member exists in the PB API listing before
using it in an in-game script.
