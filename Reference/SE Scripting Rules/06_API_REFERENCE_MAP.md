# Programmable Block API Reference Map

Use the PB API documentation to confirm that every type and member is allowed
in an in-game Programmable Block script.

## Main References

| Reference | Use |
| --- | --- |
| [PB API home](https://malforge.github.io/spaceengineers/pbapi/) | Search all PB-accessible types and members. |
| [Type Definition Listing](https://malforge.github.io/spaceengineers/pbapi/Type-Definition-Listing.html) | Find block definition TypeId/SubtypeId pairs and matching interfaces. |
| [Terminal Properties And Actions](https://malforge.github.io/spaceengineers/pbapi/List-Of-Terminal-Properties-And-Actions.html) | Find terminal control string IDs and property value types. |
| [Sprite Listing](https://malforge.github.io/spaceengineers/pbapi/Sprite-Listing.html) | Find exact sprite texture IDs, native sizes, and thumbnails. |
| [Scripting hub](https://spaceengineers.wiki.gg/wiki/Scripting) | Tutorials, concepts, examples, runtime, IGC, displays, and debugging. |
| [Keen ModAPI reference](https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/index.html) | Broader mod API documentation; do not assume entries are PB-safe. |

## How To Search The PB API

- Search a type such as `IMyBatteryBlock`, `IMyInventory`, or `MySprite`.
- Search a member such as `GetItems`, `ChargeMode`, or `DrawFrame`.
- Prefix search with `T:` for types or `M:` for members when needed.
- Follow inherited interface members; useful methods may be declared on a base
  interface rather than the specific block interface.

The PB API site is community-maintained and may lag behind a game update.
Verify questionable or newly added behavior against the installed game.

## Which Reference Answers Which Question?

| Question | Check |
| --- | --- |
| Can a PB use this class, method, or property? | PB API search |
| Which `IMy...` interface controls this block? | Type Definition Listing and local `SE_BlockTypeIds.md` |
| What is the exact placed-block TypeId/SubtypeId? | Local `SE_BlockTypeIds.md`, then installed CubeBlocks SBC |
| What is the exact inventory-item TypeId/SubtypeId? | Local `SE_ItemTypeIds.md`, then installed item SBC |
| What is an action/property string ID? | Terminal Properties And Actions listing |
| What is the exact sprite texture ID and aspect ratio? | Sprite Listing, then `IMyTextSurface.GetSprites` |
| How do runtime, IGC, LCDs, or debugging work? | Official scripting wiki |
| Is a ModAPI member usable in a PB? | Confirm it separately in the PB API |

## Important Separation

Do not mix these identifiers:

- C# interface: `IMyBatteryBlock`
- Block TypeId: `BatteryBlock` / `MyObjectBuilder_BatteryBlock`
- Block SubtypeId: `LargeBlockBatteryBlock`
- Terminal property ID: `ChargeMode`
- Terminal action ID: `Recharge_On`
- Item TypeId: `Component` / `MyObjectBuilder_Component`
- Item SubtypeId: `PowerCell`

Each belongs to a different API or definition layer.

## Local Required Catalogs

```text
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_BlockTypeIds.md
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_ItemTypeIds.md
```

Read both completely when exact TypeId behavior matters.

## Full PB API Link Audit

Audit date: **2026-06-14**

The audit checked:

- All 5,224 entries in the site's `search-index.json`.
- All 3,821 unique indexed paths.
- All 3,257 unique underlying indexed pages.
- All 3,409 unique internal hyperlinks found by crawling 3,207 valid generated
  HTML pages.
- Every fragment link for a matching target anchor.
- The three top-level generated listings shown in the sidebar.

Top-level listing results:

| Listing | Result |
| --- | --- |
| `List-Of-Terminal-Properties-And-Actions.html` | Working |
| `Sprite-Listing.html` | Working |
| `Type-Definition-Listing.html` | Working |

Actual generated-page hyperlink results:

- 3,409 unique internal hyperlinks checked.
- 22 hyperlinks return `404`.
- 0 actual hyperlink fragment anchors are missing.
- 3 temporary `503` responses passed three later retries and are not broken.

The 22 broken embedded hyperlinks are generated links to enum-value pages,
closed-generic pages, and array-type pages:

```text
Sandbox.ModAPI.Ingame.MyShipConnectorStatus@Connectable.html
Sandbox.ModAPI.Ingame.MyShipConnectorStatus@Connected.html
Sandbox.ModAPI.Ingame.MyShipConnectorStatus@Unconnected.html
Sandbox.ModAPI.Interfaces.ITerminalProperty%7BBoolean%7D.html
Sandbox.ModAPI.Interfaces.ITerminalProperty%7BColor%7D.html
Sandbox.ModAPI.Interfaces.ITerminalProperty%7BSingle%7D.html
VRage.Collections.ListReader%7BTerminalActionParameter%7D.html
VRage.Game.GUI.TextPanel.MySerializableSprite%5B%5D.html
VRage.Game.GUI.TextPanel.MySprite%5B%5D.html
VRageMath.CurveKey%5B%5D.html
VRageMath.Direction%5B%5D.html
VRageMath.Line%5B%5D.html
VRageMath.MyCuboidSide%5B%5D.html
VRageMath.Plane%5B%5D.html
VRageMath.Vector2%5B%5D.html
VRageMath.Vector2D%5B%5D.html
VRageMath.Vector2I%5B%5D.html
VRageMath.Vector3%5B%5D.html
VRageMath.Vector3D%5B%5D.html
VRageMath.Vector3I%5B%5D.html
VRageMath.Vector4%5B%5D.html
VRageMath.Vector4D%5B%5D.html
```

Search/sidebar index results:

- 48 indexed target pages return `404`.
- Most are namespace/container pages or internal helper/generic types for which
  the generator did not create an HTML page.
- 464 indexed member-fragment links point to anchors absent from otherwise
  valid pages.
- These search-index failures do not mean the documented PB members themselves
  are unavailable in-game; they are documentation navigation defects.

## Link Reliability Rules

- Trust a working type/member content page over a broken namespace link.
- If a search-result fragment does not jump to a member, search the member name
  or open its separate `Type@Member.html` page.
- Treat array and closed-generic links returning `404` as documentation
  generator defects; inspect the containing method signature instead.
- Recheck the current API and installed game after updates because this audit
  is dated.
