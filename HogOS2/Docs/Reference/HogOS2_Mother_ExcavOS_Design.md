# HogOS2 MotherOS + Mother GUI + ExcavOS Design Notes

## Goal

HogOS2 should feel like a miner-focused bridge console:

- HogOS provides the ship-specific mining brains.
- MotherOS inspires the public command language.
- Mother GUI inspires the display/menu interaction model.
- ExcavOS inspires the screen style, boot immersion, and surface-driven colors.

## Why Not Paste MotherOS Directly

The MotherOS and Mother GUI scripts are minified/obfuscated. Their public behavior is clear, but their internal class names are not useful for maintainable development.

HogOS2 therefore borrows:

- slash command style
- semicolon command routines
- `view/up`, `view/down`, `view/select`, `view/back`, `view/go`
- menu entries that execute commands
- temporary boot display behavior

It does not directly merge the obfuscated Mother runtime into the HogOS script.

## HogOS2 Command Layer

HogOS2 keeps legacy HogOS commands and adds Mother-style aliases:

| HogOS2 alias | Legacy behavior |
|---|---|
| `hog/boot` | `boot` / `reboot` |
| `hog/stop` | `stop` |
| `flight/cruise on` | `cruise on` |
| `flight/cruise off` | `cruise off` |
| `flight/cruise 20` | `cruise 20` |
| `flight/level toggle` | `toggle_gyro` |
| `flight/level_pitch +5` | `set_gyro_pitch +5` |
| `mine/slow` | `mine_slow` |
| `mine/fast` | `mine_fast` |
| `mine/terrain` | `terrain_clear` |

Commands can be chained:

```text
mine/slow; flight/level on;
```

## HogOS2 GUI Layer

The first GUI pass is a single `Menu` screen:

- entries are configured in PB Custom Data under `[Menu]`
- each entry is `Label=command`
- `view/up` and `view/down` move the cursor
- `view/select` executes the selected command
- `view/go <Page>` temporarily forces all displays to a page such as `Menu`, `Power`, `Weight`, or `Utility`

This is intentionally small. It gives the ship a Mother GUI feel without turning the PB script into a large framework.

## ExcavOS Style Choices

HogOS2 keeps and extends the ExcavOS style decisions already present in HogOS:

- screens read `ScriptForegroundColor` and `ScriptBackgroundColor`
- primary/secondary/dim colors are derived per surface
- cockpit screens use compact left-label/right-value rows
- dividers are thin and secondary-colored
- boot is a temporary overlay, not only a permanent screen page

## Next Good Features

Recommended next passes:

1. Add nested menu groups.
2. Add a persistent page target per display instead of global `view/go`.
3. Add a `Fuel` page for hydrogen tanks and engines.
4. Add Mother-style hooks for local events such as cargo full, low battery, low hydrogen, and overload.
