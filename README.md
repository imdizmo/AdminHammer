# AdminHammer

An admin tool for server owners and game masters, built from **SeneaL UI**'s own interface kit so it looks like the rest of your SeneaL game.

[Thunderstore](https://thunderstore.io/c/valheim/p/Jatify/AdminHammer/) · [GitHub](https://github.com/imdizmo/AdminHammer) · [jatify.com](https://jatify.com/#adminhammer)

## What it does

- **Admin hammer.** A gold hammer for admins. Right click opens the admin panel, like the hammer opens the build menu.
- **Spawn creatures like building.** Pick a creature, set its stars (0-2) and a count, and a ghost of it follows your aim. Left click places it, the mouse wheel turns it, right click goes back to the menu. You can also spawn a creature next to any online player.
- **Drop items.** Pick an item and an amount (1, 10, 50 or a full stack) and it spills out in front of you, or a chosen player, like a broken chest.
- **World modifiers, live.** The same sliders and switches as Valheim's world-modifier screen (combat, death penalty, resources, raids, portals, no build cost, passive mobs, ...), applied on the server at once. Only the menu's own settings are used, so the world is never flagged as cheated.
- **Every creature by biome**, with a rendered portrait of each (cached after the first time), Deep North and Ashlands included. Hover anything for details.
- **Add-on tabs.** Other mods can add their own tab to the panel (`AdminHammerApi.AddTab`).

## Screenshots

![Spawn creatures: pick a biome and a creature, set stars and count](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/creatures.webp)

![A troll placed where you aim](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/spawned-troll.webp)

![Spawn items: pick an item and an amount, then drop it](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/items.webp)

![Dropped items, ready to pick up](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/dropped-items.webp)

![World modifiers, applied live on the server](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/world-modifiers.webp)

The Buffs tab in the screenshots comes from a separate add-on and is not part of AdminHammer.

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) 5.4.2202 or newer
- [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) 2.30.2 or newer
- [SeneaL UI](https://thunderstore.io/c/valheim/p/seneaL/SeneaL_UI/) 1.2.5 or newer

A mod manager (r2modman, Gale, Thunderstore App) installs them for you.

## Getting the hammer

- Type `/adminhammer` in chat (or `adminhammer` in the F5 console), or
- craft it at a workbench: 1 wood, 1 stone.

`Insert` also opens the panel (configurable in `jatify.adminhammer.cfg`).

## Who can use it

The host of a local world, or an admin of a dedicated server. Put your Steam ID in the server's `adminlist.txt` **in both forms**, one per line:

```
76561198000000000
Steam_76561198000000000
```

The server accepts the bare ID for admin commands, but the game's own client check only matches the `Steam_` form.

Only you need the mod: nothing is required on the server or on other players' clients. Players without it see your hand empty while you hold the hammer.

## Good to know

- Things you spawn are **not** marked as cheated (Valheim's own `spawn` command would mark them).
- Placing creatures needs the Admin hammer in hand; switching to anything else clears the selection.
- If you remove the mod, the Admin hammer disappears from your inventory the next time you load.

## Credits

- Interface: drawn at runtime with [SeneaL UI](https://thunderstore.io/c/valheim/p/seneaL/SeneaL_UI/)'s components, theme and icons, by seneaL. None of SeneaL UI's files are included in this package; it must be installed separately.
- Icons: SeneaL UI's adaptation of [Phosphor Icons](https://phosphoricons.com) (MIT License, © 2023 Phosphor Icons).
- Item and command registration: [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/), by the Valheim Modding team.
- Mod loader: [BepInEx](https://github.com/BepInEx/BepInEx).
- Creature biomes: Valheim's own spawn tables, plus the creature list of the [Valheim Wiki](https://valheim.weirdgloop.org/w/Creatures).

AdminHammer is an unofficial, fan-made mod. It is not made, endorsed or supported by seneaL or Iron Gate AB. Please report AdminHammer bugs here, not to SeneaL UI. Valheim is a trademark of Iron Gate AB.

Source: https://github.com/imdizmo/AdminHammer (MIT; the MIT licence covers AdminHammer's own code only)
