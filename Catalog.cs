using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Jatify.AdminHammer
{
    // Everything the admin panel can spawn, read from the game's own registries once a world is
    // loaded: creatures from ZNetScene (anything with a Character that isn't a player), items from
    // ObjectDB. Creatures have no portraits in Valheim, so each uses its trophy's icon when it drops
    // one. Categories are ours: creatures by faction, items by item type.
    internal static class Catalog
    {
        internal sealed class Entry
        {
            public string Prefab, Name, Category;
            public Sprite Icon;
            public bool Boss, Tameable;
            public int MaxStack = 1;
        }

        internal static readonly List<Entry> Creatures = new List<Entry>();
        internal static readonly List<Entry> Items = new List<Entry>();

        internal static readonly string[] CreatureCategories =
            { "Meadows", "Black Forest", "Swamp", "Mountains", "Plains", "Mistlands", "Ashlands", "Deep North", "Ocean", "Bosses", "Other" };
        internal static readonly string[] ItemCategories =
            { "Materials", "Food", "Weapons", "Ammo", "Armour", "Tools", "Trophies", "Other" };

        private static bool _built;

        internal static void Build()
        {
            if (_built || ZNetScene.instance == null || ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0) return;
            _built = true;
            foreach (var go in ObjectDB.instance.m_items)
            {
                var drop = go != null ? go.GetComponent<ItemDrop>() : null;
                if (drop == null) continue;
                var s = drop.m_itemData.m_shared;
                if (s.m_icons == null || s.m_icons.Length == 0 || s.m_icons[0] == null) continue;
                var name = Localization.instance.Localize(s.m_name);
                if (string.IsNullOrEmpty(name) || name.StartsWith("[")) continue;   // unlocalised dev items
                Items.Add(new Entry { Prefab = go.name, Name = name, Icon = s.m_icons[0], Category = ItemCategory(s.m_itemType), MaxStack = Mathf.Max(1, s.m_maxStackSize) });
            }
            Items.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            var (homes, rest) = SpawnBiomes();
            foreach (var go in ZNetScene.instance.m_prefabs)
            {
                var c = go != null ? go.GetComponent<Character>() : null;
                if (c == null || c is Player || go.GetComponent<BaseAI>() == null) continue;
                var name = Localization.instance.Localize(c.m_name);
                if (string.IsNullOrEmpty(name) || name.StartsWith("[")) continue;
                Creatures.Add(new Entry { Prefab = go.name, Name = name, Icon = TrophyIcon(go), Boss = c.m_boss, Tameable = go.GetComponent<Tameable>() != null, Category = Category(c, go.name, name, homes, rest) });
            }
            Creatures.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            Portraits();
            Log.Info($"Catalog: {Creatures.Count} creatures, {Items.Count} items");
        }

        // A portrait of every creature, rendered once by Jotunn (scripts stripped from a copy, cached
        // on disk across sessions) and queued so opening the panel never stalls. The trophy (or none)
        // shows until each one lands. Creatures face +Z; turned ~155 degrees for a three-quarter front.
        internal static event System.Action IconsChanged;

        private static void Portraits() => Plugin.Instance.StartCoroutine(RenderPortraits());

        private static System.Collections.IEnumerator RenderPortraits()
        {
            int n = 0;
            foreach (var e in Creatures.ToArray())
            {
                var go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(e.Prefab) : null;
                if (go == null) continue;
                Sprite sprite = null;
                try
                {
                    sprite = Jotunn.Managers.RenderManager.Instance.Render(new Jotunn.Managers.RenderManager.RenderRequest(go)
                    {
                        Rotation = Quaternion.Euler(8f, 155f, 0f), Width = 128, Height = 128, UseCache = true,
                    });
                }
                catch (System.Exception ex) { Log.Warn($"no portrait for {e.Prefab}: {ex.Message}"); }
                if (sprite != null) { e.Icon = sprite; IconsChanged?.Invoke(); }
                if (++n % 3 == 0) yield return null;   // a few per frame: no stall when the panel opens
            }
        }

        private static Sprite TrophyIcon(GameObject go)
        {
            var drops = go.GetComponent<CharacterDrop>();
            if (drops == null) return null;
            foreach (var d in drops.m_drops)
            {
                if (d.m_prefab == null) continue;
                var item = d.m_prefab.GetComponent<ItemDrop>();
                if (item != null && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                    return item.m_itemData.GetIcon();
            }
            return null;
        }

        // Where each creature lives, from the game's own open-world spawn tables (the zone controller's
        // SpawnSystem lists). A creature in several biomes goes under the earliest in progression.
        private static readonly (Heightmap.Biome biome, string name)[] Progression =
        {
            (Heightmap.Biome.Meadows, "Meadows"), (Heightmap.Biome.BlackForest, "Black Forest"), (Heightmap.Biome.Swamp, "Swamp"),
            (Heightmap.Biome.Mountain, "Mountains"), (Heightmap.Biome.Plains, "Plains"), (Heightmap.Biome.Mistlands, "Mistlands"),
            (Heightmap.Biome.AshLands, "Ashlands"), (Heightmap.Biome.DeepNorth, "Deep North"), (Heightmap.Biome.Ocean, "Ocean"),
        };

        // A creature's home is in its single-biome entries (the most such entries wins, a tie goes to
        // the later biome): broad entries like "Charred: other biomes once Fader is dead" list many
        // biomes at once. Only-broad creatures use the dungeon table, then the earliest biome they list.
        // Spawn-table home first (it tells same-named creatures apart, like the Deep North's frozen
        // "Greydwarf"), then the wiki's list by name, then the dungeon table and broad spawn entries.
        private static string Category(Character c, string prefab, string name, Dictionary<string, string> homes, Dictionary<string, string> rest)
        {
            if (c.m_boss) return "Bosses";
            if (homes.TryGetValue(prefab, out var h)) return h;
            if (Wiki.TryGetValue(name, out var w)) return w;
            if (rest.TryGetValue(prefab, out var r)) return r;
            return CreatureCategory(c.m_faction);
        }

        private static (Dictionary<string, string> homes, Dictionary<string, string> rest) SpawnBiomes()
        {
            var votes = new Dictionary<string, Dictionary<int, int>>();
            var union = new Dictionary<string, Heightmap.Biome>();
            var ctrl = ZoneSystem.instance != null ? ZoneSystem.instance.m_zoneCtrlPrefab : null;
            if (ctrl != null)
                foreach (var sys in ctrl.GetComponentsInChildren<SpawnSystem>(true))
                    foreach (var list in sys.m_spawnLists)
                        if (list != null)
                            foreach (var sp in list.m_spawners)
                            {
                                if (sp == null || !sp.m_enabled || sp.m_prefab == null) continue;
                                var n = sp.m_prefab.name;
                                union[n] = (union.TryGetValue(n, out var u) ? u : 0) | sp.m_biome;
                                int i = System.Array.FindIndex(Progression, x => x.biome == sp.m_biome);
                                if (i < 0) continue;   // more than one biome: not a home
                                if (!votes.TryGetValue(n, out var v)) votes[n] = v = new Dictionary<int, int>();
                                v[i] = (v.TryGetValue(i, out var c) ? c : 0) + 1;
                            }
            var homes = new Dictionary<string, string>();
            var rest = new Dictionary<string, string>(Dungeon);
            foreach (var kv in union)
            {
                if (votes.TryGetValue(kv.Key, out var v))
                    homes[kv.Key] = Progression[v.OrderByDescending(x => x.Value).ThenByDescending(x => x.Key).First().Key].name;
                else if (!rest.ContainsKey(kv.Key))
                    foreach (var (b, name) in Progression) if ((kv.Value & b) != 0) { rest[kv.Key] = name; break; }
            }
            Log.Info($"spawn-table homes for {homes.Count} creatures");
            return (homes, rest);
        }

        // From the Valheim wiki creature list (valheim.weirdgloop.org/w/Creatures, 2026-10-14), by English name.
        private static readonly Dictionary<string, string> Wiki = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Eikthyr", "Bosses" }, { "The Elder", "Bosses" }, { "Bonemass", "Bosses" }, { "Moder", "Bosses" },
            { "Yagluth", "Bosses" }, { "The Queen", "Bosses" }, { "Fader", "Bosses" }, { "Kall Fimbulbringer", "Bosses" },
            { "Brenna", "Bosses" }, { "Geirrhafa", "Bosses" }, { "Zil & Thungr", "Bosses" }, { "Lord Reto", "Bosses" },
            { "Boar", "Meadows" }, { "Neck", "Meadows" }, { "Greyling", "Meadows" }, { "Greydwarf", "Black Forest" },
            { "Greydwarf Brute", "Black Forest" }, { "Greydwarf Shaman", "Black Forest" }, { "Troll", "Black Forest" }, { "Skeleton", "Black Forest" },
            { "Rancid Remains", "Black Forest" }, { "Ghost", "Black Forest" }, { "Root", "Black Forest" }, { "Bear", "Black Forest" },
            { "Serpent", "Ocean" }, { "Abomination", "Swamp" }, { "Blob", "Swamp" }, { "Oozer", "Swamp" },
            { "Draugr", "Swamp" }, { "Draugr Elite", "Swamp" }, { "Leech", "Swamp" }, { "Surtling", "Swamp" },
            { "Wraith", "Swamp" }, { "Writhan", "Swamp" }, { "Kvastur", "Swamp" }, { "Wolf", "Mountains" },
            { "Drake", "Mountains" }, { "Fenring", "Mountains" }, { "Stone Golem", "Mountains" }, { "Bat", "Mountains" },
            { "Ulv", "Mountains" }, { "Cultist", "Mountains" }, { "Deathsquito", "Plains" }, { "Lox", "Plains" },
            { "Fuling", "Plains" }, { "Fuling Berserker", "Plains" }, { "Fuling Shaman", "Plains" }, { "Growth", "Plains" },
            { "Vile", "Plains" }, { "Seeker", "Mistlands" }, { "Seeker Soldier", "Mistlands" }, { "Seeker Brood", "Mistlands" },
            { "Gjall", "Mistlands" }, { "Tick", "Mistlands" }, { "Dvergr Rogue", "Mistlands" }, { "Dvergr Mage", "Mistlands" },
            { "Bonemaw", "Ashlands" }, { "Lava Blob", "Ashlands" }, { "Fallen Valkyrie", "Ashlands" }, { "Charred Warlock", "Ashlands" },
            { "Charred Warrior", "Ashlands" }, { "Charred Marksman", "Ashlands" }, { "Charred Twitcher", "Ashlands" }, { "Volture", "Ashlands" },
            { "Morgen", "Ashlands" }, { "Asksvin", "Ashlands" }, { "Skugg", "Ashlands" }, { "Barka", "Deep North" },
            { "Elaking", "Deep North" }, { "Eyeless One", "Deep North" }, { "Fallen Warrior", "Deep North" }, { "Gammeltroll", "Deep North" },
            { "Hexen", "Deep North" }, { "Krigen", "Deep North" }, { "Shapeless Pulp", "Deep North" }, { "Tiny Pulp", "Deep North" },
            { "Moose", "Deep North" }, { "Crow", "Black Forest" }, { "Gull", "Meadows" }, { "Ash Crow", "Ashlands" },
            { "Leviathan", "Ocean" }, { "Deer", "Meadows" }, { "Hare", "Mistlands" }, { "Baby Seal", "Deep North" },
            { "Seal", "Deep North" },
        };

        // Creatures that only spawn in dungeons or locations (not in the open-world tables).
        private static readonly Dictionary<string, string> Dungeon = new Dictionary<string, string>
        {
            { "Skeleton", "Black Forest" }, { "Skeleton_Poison", "Black Forest" }, { "Ghost", "Black Forest" }, { "Troll", "Black Forest" },
            { "Draugr", "Swamp" }, { "Draugr_Elite", "Swamp" }, { "Draugr_Ranged", "Swamp" }, { "BlobElite", "Swamp" }, { "Wraith", "Swamp" },
            { "Dverger", "Mistlands" }, { "DvergerMage", "Mistlands" }, { "DvergerMageFire", "Mistlands" }, { "DvergerMageIce", "Mistlands" }, { "DvergerMageSupport", "Mistlands" },
            { "SeekerBrood", "Mistlands" }, { "SeekerBrute", "Mistlands" },
        };

        // Last resort for anything neither table knows. ponytail: factions only roughly follow biomes.
        private static string CreatureCategory(Character.Faction f)
        {
            switch (f)
            {
                case Character.Faction.AnimalsVeg: return "Meadows";
                case Character.Faction.ForestMonsters: return "Black Forest";
                case Character.Faction.Undead: return "Swamp";
                case Character.Faction.Demon: return "Swamp";
                case Character.Faction.MountainMonsters: return "Mountains";
                case Character.Faction.PlainsMonsters: return "Plains";
                case Character.Faction.MistlandsMonsters: return "Mistlands";
                case Character.Faction.Dverger: return "Mistlands";
                case Character.Faction.SeaMonsters: return "Ocean";
                default: return f.ToString().Contains("Ash") ? "Ashlands" : "Other";
            }
        }

        private static string ItemCategory(ItemDrop.ItemData.ItemType t)
        {
            switch (t)
            {
                case ItemDrop.ItemData.ItemType.Material: return "Materials";
                case ItemDrop.ItemData.ItemType.Consumable: return "Food";
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Torch: return "Weapons";
                case ItemDrop.ItemData.ItemType.Ammo:
                case ItemDrop.ItemData.ItemType.AmmoNonEquipable: return "Ammo";
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Utility: return "Armour";
                case ItemDrop.ItemData.ItemType.Tool: return "Tools";
                case ItemDrop.ItemData.ItemType.Trophy: return "Trophies";
                default: return "Other";
            }
        }
    }
}
