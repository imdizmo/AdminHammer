using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace Jatify.AdminHammer
{
    // Admin panel in the Jatify design (Jatify UI's look when installed, the design's own colours and Valheim's font
    // otherwise): spawn creatures (placed like the hammer) and items (spilled like a broken chest) and set the world
    // modifiers live. Only for the host or a server admin
    // (adminlist.txt). Nothing it spawns is marked cheated, and it only sets world keys the
    // world-modifier menu itself offers, so the world is never flagged either.
    [BepInPlugin(Guid, "AdminHammer", "1.1.0")]
    [BepInDependency("jatify.ui", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]   // nothing needed on the server or friends' clients
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jatify.adminhammer";
        internal static Plugin Instance;
        private static ConfigEntry<KeyCode> _key;
        private static Panel _panel;
        private static bool _openedByHammer;
        private Harmony _harmony;
        private static int _closedFrame = -1;

        // Esc closes the panel and nothing else (Menu.Update would open the pause menu): EscPatch
        internal static bool EscClose() { if (_panel == null || !_panel.Open) return false; _panel.Close(); _closedFrame = Time.frameCount; return true; }

        private static readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> _hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        private static bool OnRightAction()
        {
            var es = UnityEngine.EventSystems.EventSystem.current; if (es == null) return false;
            _hits.Clear(); es.RaycastAll(new UnityEngine.EventSystems.PointerEventData(es) { position = Input.mousePosition }, _hits);
            foreach (var h in _hits) { var c = h.gameObject.GetComponentInParent<AdminHammerUi.Clicks>(); if (c != null && c.Right != null) return true; }
            return false;
        }

        internal static bool PanelOpen => _panel != null && _panel.Open;
        internal static Panel CurrentPanel => _panel;
        internal static string KeyName => _key != null ? _key.Value.ToString() : "Insert";

        private void Awake()
        {
            Instance = this;
            Log.Bind(Logger);
            _key = Config.Bind("General", "Key", KeyCode.Insert, "Opens and closes the admin panel. Not F9 (Valheim cycles the controller layout).");
            _harmony = new Harmony(Guid); _harmony.PatchAll(typeof(Plugin).Assembly);
            Hammer.Register();
            if (!CommandManager.Instance.CustomCommands.Any(c => c.Name == "adminhammer")) CommandManager.Instance.AddConsoleCommand(new HammerCommand());   // a hot reload keeps the first one
        }

        // a hot reload (ScriptEngine) unloads this copy: its patches, panel and placing ghost go with it
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Placer.Stop();
            if (_panel != null) _panel.Destroy();
            _panel = null;
        }

        private void Update()
        {
            if (Hud.instance == null || Player.m_localPlayer == null) { if (_panel != null && !Hud.instance) _panel = null; return; }
            if (_panel == null)
            {
                _panel = new Panel(Hud.instance.m_rootObject.transform);
            }

            if (Input.GetKeyDown(_key.Value) && !TypingPatch.Active && !Console.IsVisible() && !(Chat.instance != null && Chat.instance.HasFocus()))
            {
                if (!Spawner.CanAdmin) MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Admin panel: you are not an admin on this server");
                else { Placer.Stop(); _panel.Toggle(); }
            }
            // like the hammer's build menu: right click closes it (not on a tile with its own right click: Buffs toggles)
            if (_panel.Open && Input.GetMouseButtonDown(1) && !TypingPatch.Active && !OnRightAction()) { _panel.Close(); _closedFrame = Time.frameCount; }
            // The Admin hammer: right click opens the panel, where the vanilla hammer opens the build menu.
            if (Hammer.Equipped && !_panel.Open && Time.frameCount != _closedFrame && !Placer.Active && Input.GetMouseButtonDown(1) && !Menu.IsVisible()
                && !InventoryGui.IsVisible() && !Minimap.IsOpen() && !Console.IsVisible() && !(Chat.instance != null && Chat.instance.HasFocus()))
            {
                if (!Spawner.CanAdmin) MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Admin hammer: you are not an admin on this server");
                else { _panel.Show(); _openedByHammer = true; }
            }
            // Opened from the hammer: closes when the hammer leaves the hand, like the build menu.
            if (_openedByHammer && (!_panel.Open || !Hammer.Equipped)) { if (_panel.Open) _panel.Close(); _openedByHammer = false; }
            _panel.Tick();
            Placer.Tick();
            if (Placer.MenuRequested)
            {
                Placer.MenuRequested = false;
                _panel.Show();
                _openedByHammer = Hammer.Equipped;
            }
        }
    }

    // Esc with the panel open closes it, not opens the pause menu (Valheim reads Esc in Menu.Update)
    [HarmonyPatch(typeof(Menu), "Update")]
    internal static class EscPatch
    {
        private static bool Prefix() => !(ZInput.GetKeyDown(KeyCode.Escape) && Plugin.EscClose());
    }

    // While the panel is open the game treats it as a menu: free cursor, no player input.
    [HarmonyPatch(typeof(Menu), nameof(Menu.IsVisible))]
    internal static class MenuVisiblePatch
    {
        private static void Postfix(ref bool __result) { if (Plugin.PanelOpen) __result = true; }
    }

    // Valheim frees the cursor only when no menu reports itself visible, so the patch above alone
    // keeps it captured. While the panel is open, unlock and show it ourselves and skip the game's capture logic.
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    internal static class PanelCursorPatch
    {
        private static bool Prefix()
        {
            if (!Plugin.PanelOpen) return true;
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
            return false;
        }
    }

    // While one of the panel's text fields (search, profile name) has focus, Valheim is told that chat has focus, so it
    // ignores game keys (M opened the map while naming a profile: user).
    [HarmonyPatch]
    internal static class TypingPatch
    {
        internal static bool Active
        {
            get
            {
                var go = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject; var p = Plugin.CurrentPanel;
                if (go == null || p == null || !p.Open) return false;
                var f = go.GetComponent<TMPro.TMP_InputField>();
                return f != null && f.isFocused && go.transform.IsChildOf(p.Root);
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(Chat), nameof(Chat.HasFocus))]
        private static void ChatFocus(ref bool __result) { if (!__result && Active) __result = true; }
        [HarmonyPostfix, HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
        private static void TextVisible(ref bool __result) { if (!__result && Active) __result = true; }
    }

    // Placing creatures uses the mouse buttons: don't swing the weapon in hand meanwhile.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class NoAttackWhilePlacingPatch
    {
        private static bool Prefix(Humanoid __instance, ref bool __result)
        {
            if (!Placer.Active || __instance != Player.m_localPlayer) return true;
            __result = false;
            return false;
        }
    }
}
