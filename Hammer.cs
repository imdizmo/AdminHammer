using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Jatify.AdminHammer
{
    // The Admin Hammer: a gold-tinted clone of the vanilla hammer with no build pieces. Equipped,
    // right click opens the admin panel (where the hammer opens the build menu) and left click places
    // the creature picked there. Made at a workbench (1 wood, 1 stone) or given by /adminhammer.
    // Only this client knows the item: friends without the mod see an empty hand.
    internal static class Hammer
    {
        internal const string Prefab = "Jatify_AdminHammer";
        private static readonly Color Gold = new Color(1f, 0.78f, 0.36f);

        // Hot reloads (the dev profile's ScriptEngine): the item stays registered with Jotunn from the first load, which keeps
        // adding it to every world, so a reload finds it there and leaves it. Loaded into a running game without it, it is
        // made now and put into the open world's lists too.
        internal static void Register()
        {
            if (ItemManager.Instance.GetItem(Prefab) != null) return;
            if (ObjectDB.instance != null && ObjectDB.instance.m_items.Count > 0) { Add(); Live(); }
            else PrefabManager.OnVanillaPrefabsAvailable += Add;
        }

        private static void Live()
        {
            var item = ItemManager.Instance.GetItem(Prefab); var db = ObjectDB.instance; if (item == null || db == null || db.GetItemPrefab(Prefab) != null) return;
            db.m_items.Add(item.ItemPrefab);
            if (item.Recipe?.Recipe != null) db.m_recipes.Add(item.Recipe.Recipe);
            HarmonyLib.AccessTools.Method(typeof(ObjectDB), "UpdateRegisters")?.Invoke(db, null);
            var scene = ZNetScene.instance;
            if (scene != null && !scene.m_prefabs.Contains(item.ItemPrefab))
            {
                scene.m_prefabs.Add(item.ItemPrefab);
                HarmonyLib.AccessTools.FieldRefAccess<ZNetScene, System.Collections.Generic.Dictionary<int, GameObject>>("m_namedPrefabs")(scene)[Prefab.GetStableHashCode()] = item.ItemPrefab;
            }
        }

        private static void Add()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Add;
            var item = new CustomItem(Prefab, "Hammer", new ItemConfig
            {
                Name = "Admin hammer",
                Description = "Right click: the admin panel. Left click: place the creature picked there.",
                CraftingStation = CraftingStations.Workbench,
                Requirements = new[] { new RequirementConfig("Wood", 1, 0, false), new RequirementConfig("Stone", 1, 0, false) },
            });
            var shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_buildPieces = null;    // no vanilla build mode: right click is ours
            Tint(item.ItemPrefab);
            var icon = RenderManager.Instance.Render(item.ItemPrefab, RenderManager.IsometricRotation);
            if (icon != null) shared.m_icons = new[] { icon };
            ItemManager.Instance.AddItem(item);
        }

        // Own material copies: the clone shares the vanilla hammer's materials otherwise.
        private static void Tint(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = r.sharedMaterials.Select(m =>
                {
                    if (m == null) return m;
                    var copy = new Material(m);
                    if (copy.HasProperty("_Color")) copy.color = copy.color * Gold;
                    return copy;
                }).ToArray();
        }

        internal static bool Equipped
        {
            get
            {
                // Not GetCurrentWeapon(): Valheim doesn't count tools (the hammer) as weapons.
                var p = Player.m_localPlayer;
                if (p == null) return false;
                foreach (var it in p.GetInventory().GetEquippedItems())
                    if (it.m_dropPrefab != null && it.m_dropPrefab.name == Prefab) return true;
                return false;
            }
        }

        // Equips an Admin hammer from the inventory; false when there is none.
        internal static bool Equip()
        {
            var p = Player.m_localPlayer;
            if (p == null) return false;
            foreach (var it in p.GetInventory().GetAllItems())
                if (it.m_dropPrefab != null && it.m_dropPrefab.name == Prefab) return p.EquipItem(it);
            return false;
        }

        internal static void Give()
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            if (!Spawner.CanAdmin) { p.Message(MessageHud.MessageType.Center, "Admin hammer: you are not an admin on this server"); return; }
            // Not marked cheated (the user's choice for everything this mod spawns).
            if (p.GetInventory().AddItem(Prefab, 1, 1, 0, 0L, "", false) == null)
                p.Message(MessageHud.MessageType.Center, "No room in your inventory");
        }
    }

    // "/adminhammer" in chat or "adminhammer" in the F5 console. Not a cheat command, so chat allows it.
    internal sealed class HammerCommand : ConsoleCommand
    {
        public override string Name => "adminhammer";
        public override string Help => "Gives you the Admin hammer (admins only)";
        public override void Run(string[] args) => Hammer.Give();
    }
}
