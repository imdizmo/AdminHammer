using System.Collections.Generic;
using UnityEngine;

namespace Jatify.AdminHammer
{
    // The admin panel's actions. Spawned creatures and items are plain world objects: nothing is
    // marked cheated (the user's choice; the game's own spawn command would mark them).
    internal static class Spawner
    {
        // A creature with 0-2 stars (level = stars + 1), facing `rot`; tamed when asked and it can be (we own it: we made it).
        internal static void Creature(string prefab, Vector3 pos, Quaternion rot, int stars, bool tamed = false)
        {
            var go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (go == null) { Log.Warn($"no prefab {prefab}"); return; }
            var c = Object.Instantiate(go, pos, rot).GetComponent<Character>();
            if (c != null && stars > 0) c.SetLevel(stars + 1);
            if (c != null && tamed && c.GetComponent<Tameable>() != null) c.SetTamed(true);
        }

        // `amount` of an item put down at `pos` as whole stacks (placed with the hammer: user), no spill
        internal static void Place(string prefab, int amount, Vector3 pos, Quaternion rot)
        {
            var go = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            var drop = go != null ? go.GetComponent<ItemDrop>() : null;
            if (drop == null) { Log.Warn($"no item {prefab}"); return; }
            var data = drop.m_itemData.Clone(); data.m_dropPrefab = go;   // as in Spill: DropItem needs it
            int max = Mathf.Max(1, data.m_shared.m_maxStackSize), i = 0;
            for (int left = amount; left > 0; left -= max, i++)
                ItemDrop.DropItem(data, Mathf.Min(left, max), pos + Vector3.up * (0.15f + 0.25f * i), rot);   // more stacks pile up
        }

        // `amount` of an item spilling down in front of `target` (position + forward), like a
        // broken chest: stacks fall from ~3 m with a little outward and upward kick.
        internal static void Spill(string prefab, int amount, Vector3 at, Vector3 forward)
        {
            var go = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            var drop = go != null ? go.GetComponent<ItemDrop>() : null;
            if (drop == null) { Log.Warn($"no item {prefab}"); return; }
            // The registry's own ItemData has no m_dropPrefab (Valheim fills it on pickup/craft), and
            // DropItem instantiates from it: a copy with it set.
            var data = drop.m_itemData.Clone();
            data.m_dropPrefab = go;
            int max = Mathf.Max(1, data.m_shared.m_maxStackSize);
            Vector3 centre = at + forward.normalized * 2f + Vector3.up * 3f;
            for (int left = amount; left > 0; left -= max)
            {
                int n = Mathf.Min(left, max);
                var pos = centre + Random.insideUnitSphere * 0.4f;
                var d = ItemDrop.DropItem(data, n, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                var body = d != null ? d.GetComponent<Rigidbody>() : null;
                if (body != null)
                {
                    var kick = Random.insideUnitSphere; kick.y = Mathf.Abs(kick.y);
                    body.linearVelocity = kick * 2.5f + Vector3.up * 1.5f;
                }
            }
        }

        // Online players the panel can target: name and where they stand. Loaded characters give
        // exact positions; otherwise the server's player list (only if they share their position).
        internal static List<(string name, Vector3 pos, Vector3 forward)> Players()
        {
            var list = new List<(string, Vector3, Vector3)>();
            var seen = new HashSet<string>();
            foreach (var p in Player.GetAllPlayers())
            {
                if (p == null || p == Player.m_localPlayer) continue;
                list.Add((p.GetPlayerName(), p.transform.position, p.transform.forward));
                seen.Add(p.GetPlayerName());
            }
            if (ZNet.instance != null)
                foreach (var info in ZNet.instance.GetPlayerList())
                    if (!seen.Contains(info.m_name) && info.m_publicPosition && Player.m_localPlayer != null && info.m_name != Player.m_localPlayer.GetPlayerName())
                        list.Add((info.m_name, info.m_position, Vector3.forward));
            return list;
        }

        // World keys go through the server (an admin's remote 'setkey'/'removekey'), so every
        // player gets them at once. In a local world we are the server and set them directly.
        internal static void SetKey(string key)
        {
            if (ZNet.instance == null || ZoneSystem.instance == null) return;
            if (ZNet.instance.IsServer()) ZoneSystem.instance.SetGlobalKey(key);
            else ZNet.instance.RemoteCommand("setkey " + key);
        }

        internal static void RemoveKey(string name)
        {
            if (ZNet.instance == null || ZoneSystem.instance == null) return;
            if (ZNet.instance.IsServer()) ZoneSystem.instance.RemoveGlobalKey(name);
            else ZNet.instance.RemoteCommand("removekey " + name);
        }

        // The client's own check only matches "Steam_<id>" lines, while the server also accepts a bare
        // id (what adminlist.txt usually holds), so accept either form here.
        internal static bool CanAdmin
        {
            get
            {
                if (ZNet.instance == null) return false;
                if (ZNet.instance.LocalPlayerIsAdminOrHost()) return true;
                var me = UserInfo.GetLocalUser().UserId;
                var list = ZNet.instance.GetAdminList();
                return list != null && (list.Contains(me.ToString()) || list.Contains(me.m_userID.ToString()));
            }
        }
    }
}
