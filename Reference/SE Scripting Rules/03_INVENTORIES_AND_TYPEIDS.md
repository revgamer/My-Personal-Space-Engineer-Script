# Inventories And TypeIds

## Required Local Catalogs

Before writing or reviewing TypeId logic, read both files completely:

```text
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_BlockTypeIds.md
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_ItemTypeIds.md
```

- `SE_BlockTypeIds.md` is the placed-block definition catalog.
- `SE_ItemTypeIds.md` is the inventory-item catalog.
- Always check both catalogs so a valid ID from the wrong category is not used.

## The Two TypeId Categories

This distinction must be checked in every script.

### Block Definition TypeId

Identifies a **placed block definition**:

```text
MyObjectBuilder_CargoContainer / LargeBlockLargeContainer
MyObjectBuilder_BatteryBlock / LargeBlockBatteryBlock
MyObjectBuilder_Assembler / LargeAssembler
```

Use block definition identity when checking which placed block variant a
terminal block represents. Obtain it from `block.BlockDefinition` rather than
guessing.

### Inventory Item TypeId

Identifies an **item stack inside an inventory**:

```text
MyObjectBuilder_Ore / Iron
MyObjectBuilder_Ingot / Iron
MyObjectBuilder_Component / SteelPlate
MyObjectBuilder_AmmoMagazine / NATO_25x184mm
```

Use `MyItemType` for inventory item identity:

```csharp
readonly MyItemType IronOre =
    MyItemType.MakeOre("Iron");

readonly MyItemType SteelPlate =
    MyItemType.MakeComponent("SteelPlate");

readonly MyItemType GatlingAmmo =
    MyItemType.Parse("MyObjectBuilder_AmmoMagazine/NATO_25x184mm");
```

Do not use `MyObjectBuilder_CargoContainer` to search an inventory. It is a
block definition TypeId, not an inventory item TypeId.

## Short SBC Form Versus Runtime Form

The local catalogs and installed `.sbc` definitions use the short TypeId form.
Runtime script strings commonly use the full object-builder form:

| Category | Catalog/SBC form | Runtime/script form |
| --- | --- | --- |
| Block | `Reactor` | `MyObjectBuilder_Reactor` |
| Block | `MyProgrammableBlock` | `MyObjectBuilder_MyProgrammableBlock` |
| Item | `Ore` | `MyObjectBuilder_Ore` |
| Item | `Component` | `MyObjectBuilder_Component` |
| Item | `AmmoMagazine` | `MyObjectBuilder_AmmoMagazine` |

Compare like with like. Do not prepend `MyObjectBuilder_` twice, and do not
compare an SBC short name directly with a runtime full string without
normalizing or intentionally using the matching API property.

## Common Vanilla Item TypeIds

Verified against the installed SE1 definitions on 2026-06-14:

| Item category | TypeId | Example SubtypeIds |
| --- | --- | --- |
| Ore | `MyObjectBuilder_Ore` | `Iron`, `Nickel`, `Cobalt`, `Stone`, `Ice`, `Uranium` |
| Ingot | `MyObjectBuilder_Ingot` | `Iron`, `Nickel`, `Cobalt`, `Stone`, `Uranium` |
| Component | `MyObjectBuilder_Component` | `SteelPlate`, `Construction`, `Motor`, `Computer` |
| Ammo | `MyObjectBuilder_AmmoMagazine` | `NATO_25x184mm`, `Missile200mm`, `AutocannonClip` |
| Gas bottle | `MyObjectBuilder_GasContainerObject` | `HydrogenBottle` |
| Oxygen bottle | `MyObjectBuilder_OxygenContainerObject` | `OxygenBottle` |
| Hand tool/weapon | `MyObjectBuilder_PhysicalGunObject` | `WelderItem`, `HandDrillItem`, `AutomaticRifleItem` |
| Consumable | `MyObjectBuilder_ConsumableItem` | `Medkit`, `Powerkit`, `ClangCola` |
| Datapad | `MyObjectBuilder_Datapad` | `Datapad` |
| Currency | `MyObjectBuilder_PhysicalObject` | `SpaceCredit` |
| Seed | `MyObjectBuilder_SeedItem` | `Fruit`, `Grain`, `Mushrooms`, `Vegetables` |

Mods can add new TypeId/SubtypeId pairs. Verify modded items from their
definitions or by inspecting actual inventory items.

## Accessing Inventories

```csharp
if (!block.HasInventory)
    return;

for (int inventoryIndex = 0; inventoryIndex < block.InventoryCount; inventoryIndex++)
{
    IMyInventory inventory = block.GetInventory(inventoryIndex);
    Echo(inventory.CurrentVolume + " / " + inventory.MaxVolume);
}
```

Never assume every block has inventory index `0`, or that a multi-inventory
block uses the index expected by an unrelated block type.

## Reading Items

```csharp
readonly List<MyInventoryItem> _items = new List<MyInventoryItem>();

void Scan(IMyInventory inventory)
{
    _items.Clear();
    inventory.GetItems(_items);

    for (int i = 0; i < _items.Count; i++)
    {
        MyInventoryItem item = _items[i];
        Echo(item.Type.TypeId + "/" + item.Type.SubtypeId + ": " + item.Amount);
    }
}
```

`MyFixedPoint` is used for item amounts. Avoid careless casts that lose
fractional amounts for ores and ingots.

## Transferring Items

Before transferring:

- Confirm source and destination inventories exist.
- Confirm the destination can accept the item.
- Confirm inventories are connected when conveyor transfer is required.
- Re-read item indices after transfers because stacks can merge or move.
- Check the boolean result where the API returns one.
- Do not iterate forward over a changing item list without accounting for
  index changes.

Prefer item identity and actual inventory contents over hard-coded stack
positions.

## Parsing And Comparing IDs

```csharp
MyItemType steelPlate =
    MyItemType.Parse("MyObjectBuilder_Component/SteelPlate");

if (item.Type == steelPlate)
{
    // This stack contains steel plates.
}
```

For block definitions:

```csharp
MyDefinitionId definition = block.BlockDefinition;
bool isLargeCargo =
    definition.TypeId.ToString() == "MyObjectBuilder_CargoContainer"
    && definition.SubtypeId.ToString() == "LargeBlockLargeContainer";
```

Prefer typed constructors such as `MakeOre`, `MakeIngot`, and
`MakeComponent` where they clearly express intent. Parse and cache IDs once,
not repeatedly in a hot loop.

## Installed Definition Sources

Item definitions are found under:

```text
...\SpaceEngineers\Content\Data\Components.sbc
...\SpaceEngineers\Content\Data\PhysicalItems.sbc
...\SpaceEngineers\Content\Data\PhysicalItems_Food.sbc
...\SpaceEngineers\Content\Data\AmmoMagazines.sbc
```

Block definitions are primarily under:

```text
...\SpaceEngineers\Content\Data\CubeBlocks\*.sbc
```

In SBC files, IDs may be written without the `MyObjectBuilder_` prefix. The
runtime/API string form commonly includes it.

## References

- Required block catalog:
  `F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_BlockTypeIds.md`
- Required item catalog:
  `F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Reference\Space Engineer definiations\SE_ItemTypeIds.md`
- Controlling things and item definitions:
  <https://spaceengineers.wiki.gg/wiki/Scripting/Controlling_Things>
- PB API `MyItemType` and `IMyInventory`:
  <https://malforge.github.io/spaceengineers/pbapi/>
