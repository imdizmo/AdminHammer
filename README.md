# AdminHammer

[![Join the Discord](https://jatify.com/badges/discord.png)](https://discord.gg/Ru9t2KdfVK) [![Buy me a coffee](https://jatify.com/badges/coffee.png)](https://buymeacoffee.com/imdizmo)

An admin tool for server owners and game masters, in the Jatify design. With [Jatify UI](https://jatify.com) installed it has Jatify UI's exact look; without it, the same design in Valheim's font. Needs no other UI mod.

[Thunderstore](https://thunderstore.io/c/valheim/p/Jatify/AdminHammer/) · [GitHub](https://github.com/imdizmo/AdminHammer) · [jatify.com](https://jatify.com/#adminhammer)

## What it does

- **Admin hammer.** A gold hammer for admins. Right click opens the admin panel, like the hammer opens the build menu.
- **Spawn creatures like building.** Pick a creature, set its stars (0-2) and a count, and a ghost of it follows your aim. Left click places it, the mouse wheel turns it, right click goes back to the menu. You can also spawn a creature next to any online player.
- **Drop items.** Pick an item and an amount (1, 10, 50 or a full stack) and it spills out in front of you, or a chosen player, like a broken chest.
- **World modifiers, live.** The same sliders and switches as Valheim's world-modifier screen (combat, death penalty, resources, raids, portals, no build cost, passive mobs, ...), applied on the server at once. Only the menu's own settings are used, so the world is never flagged as cheated.
- **Every creature by biome**, with a rendered portrait of each (cached after the first time), Deep North and Ashlands included. Hover anything for details.
- **Add-on tabs.** Other mods can add their own tab to the panel (`AdminHammerApi.AddTab`).

## Add-ons

- [BuffMeDaddy](https://thunderstore.io/c/valheim/p/Jatify/BuffMeDaddy/): a Buffs tab to give yourself or other players any status effect, and switch off its time limit. Optional; AdminHammer works without it.

## Screenshots

![Creatures: pick a biome and a creature, set stars, count and whether it spawns tamed](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/creatures.webp)

![Placing a Seeker Soldier where you aim](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/placing.webp)

![Items: pick an item and an amount, then drop it or place it where you aim](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/items.webp)

![Ten Midsummer Crowns placed and picked up](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/placed-items.webp)

![World modifiers, applied live on the server](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/world-modifiers.webp)

![The world rules: no build cost, player based raids, fire hazards, passive enemies, no map](https://raw.githubusercontent.com/imdizmo/AdminHammer/main/screenshots/world-rules.webp)

The Buffs tab in the screenshots comes from the [BuffMeDaddy](https://thunderstore.io/c/valheim/p/Jatify/BuffMeDaddy/) add-on and is not part of AdminHammer.

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) 5.4.2202 or newer
- [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) 2.30.2 or newer

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

- Interface: the Jatify design; with Jatify UI installed, its fonts, panels and colours.
- Item and command registration: [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/), by the Valheim Modding team.
- Mod loader: [BepInEx](https://github.com/BepInEx/BepInEx).
- Creature biomes: Valheim's own spawn tables, plus the creature list of the [Valheim Wiki](https://valheim.weirdgloop.org/w/Creatures).
- Code: written together with Claude (Anthropic's AI), which shows up as a co-author in the commit history.

AdminHammer is an unofficial, fan-made mod. It is not made, endorsed or supported by Iron Gate AB. Please report AdminHammer bugs here. Valheim is a trademark of Iron Gate AB.

Source: https://github.com/imdizmo/AdminHammer (MIT; the MIT licence covers AdminHammer's own code only)
