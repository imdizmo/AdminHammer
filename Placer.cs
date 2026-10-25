using UnityEngine;

namespace Jatify.AdminHammer
{
    // Creature placement like the hammer: the creature's look follows the aim point, left click
    // places one (count times, scattered a little), right click or Esc stops, the wheel turns it.
    // The ghost is only the creature's visual meshes, copied under an inactive parent so none of
    // their scripts ever wake, then stripped of every script.
    internal static class Placer
    {
        internal static bool Active => _prefab != null;
        internal static bool MenuRequested;   // right click while placing: back to the panel, like the hammer's build menu
        internal static string Prefab => _prefab;

        private static string _prefab;
        private static int _stars, _count;
        private static bool _tamed;
        private static GameObject _ghost;
        private static float _yaw;
        private static Vector3 _point;
        private static bool _hit;
        private static int _mask;

        // Placing needs the Admin hammer in hand (only it places; anything else in hand never spawns).
        // Picked without it: equip it from the inventory, or say how to get one.
        internal static bool Begin(string prefab, int stars, int count, bool tamed = false)
        {
            Stop();
            if (!Hammer.Equipped && !Hammer.Equip())
            {
                Hint("Placing creatures needs the Admin hammer in hand: type /adminhammer");
                return false;
            }
            var go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (go == null) return false;
            _prefab = prefab; _stars = stars; _count = Mathf.Max(1, count); _tamed = tamed;
            _yaw = Player.m_localPlayer != null ? Player.m_localPlayer.transform.eulerAngles.y + 180f : 0f;
            _mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            _ghost = Ghost(go);
            Hint($"Placing {Localization.instance.Localize(go.GetComponent<Character>()?.m_name ?? prefab)} - click to place, right click for the menu");
            return true;
        }

        internal static void Stop()
        {
            if (_ghost != null) Object.Destroy(_ghost);
            _ghost = null; _prefab = null;
        }

        internal static void Tick()
        {
            if (!Active) return;
            if (Player.m_localPlayer == null || GameCamera.instance == null) { Stop(); return; }
            if (!Hammer.Equipped) { Stop(); return; }   // the hammer left the hand: selection cleared, like the vanilla hammer
            var cam = GameCamera.instance.transform;
            _hit = Physics.Raycast(cam.position, cam.forward, out var rh, 60f, _mask);
            _point = rh.point;
            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (wheel != 0f) _yaw += wheel > 0f ? 22.5f : -22.5f;
            if (_ghost != null)
            {
                _ghost.SetActive(_hit);
                _ghost.transform.SetPositionAndRotation(_point, Quaternion.Euler(0f, _yaw, 0f));
            }
            if (Input.GetMouseButtonDown(1)) { Stop(); MenuRequested = true; return; }   // Esc is the game menu's alone
            if (Menu.IsVisible()) return;   // game menu open: keep the selection, place nothing
            if (_hit && Input.GetMouseButtonDown(0))
                for (int i = 0; i < _count; i++)
                {
                    var off = i == 0 ? Vector3.zero : Vector3.ProjectOnPlane(Random.insideUnitSphere, Vector3.up).normalized * Random.Range(1f, 2.5f);
                    Spawner.Creature(_prefab, _point + off + Vector3.up * 0.2f, Quaternion.Euler(0f, _yaw, 0f), _stars, _tamed);
                }
        }

        private static GameObject Ghost(GameObject prefab)
        {
            var holder = new GameObject("AdminHammerGhostHolder");
            holder.SetActive(false);
            var copy = Object.Instantiate(prefab.transform.Find("Visual") != null ? prefab.transform.Find("Visual").gameObject : prefab, holder.transform);
            foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
            foreach (var c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var rb in copy.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            foreach (var a in copy.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            copy.transform.SetParent(null, false);
            copy.name = "AdminHammerGhost";
            Object.Destroy(holder);
            copy.SetActive(true);
            return copy;
        }

        private static void Hint(string text) => MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, text);
    }
}
