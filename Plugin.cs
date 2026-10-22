using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace Jatify.AdminHammer
{
    // Admin panel in SeneaL UI's style: spawn creatures (placed like the hammer) and items (spilled
    // like a broken chest) and set the world modifiers live. Only for the host or a server admin
    // (adminlist.txt). Nothing it spawns is marked cheated, and it only sets world keys the
    // world-modifier menu itself offers, so the world is never flagged either.
    [BepInPlugin(Guid, "AdminHammer", "1.0.0")]
    [BepInDependency("seneaL.valheim.ui")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]   // nothing needed on the server or friends' clients
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jatify.adminhammer";
        internal static Plugin Instance;
        private static ConfigEntry<KeyCode> _key;
        private static Panel _panel;
        private static bool _openedByHammer;

        internal static bool PanelOpen => _panel != null && _panel.Open;

        private void Awake()
        {
            Instance = this;
            Log.Bind(Logger);
            _key = Config.Bind("General", "Key", KeyCode.Insert, "Opens and closes the admin panel. Not F7 (SeneaL UI's settings since 1.1.9) or F9 (Valheim cycles the controller layout).");
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Hammer.Register();
            CommandManager.Instance.AddConsoleCommand(new HammerCommand());
        }

        private void Update()
        {
            if (Hud.instance == null || Player.m_localPlayer == null) { if (_panel != null && !Hud.instance) _panel = null; return; }
            if (_panel == null)
            {
                _panel = new Panel(Hud.instance.m_rootObject.transform);
                TooltipTicker.Panel = _panel;
                if (GetComponent<TooltipTicker>() == null) gameObject.AddComponent<TooltipTicker>();
            }

            if (Input.GetKeyDown(_key.Value) && !Console.IsVisible() && !(Chat.instance != null && Chat.instance.HasFocus()))
            {
                if (!Spawner.CanAdmin) MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Admin panel: you are not an admin on this server");
                else { Placer.Stop(); _panel.Toggle(); }
            }
            // The Admin hammer: right click opens the panel, where the vanilla hammer opens the build menu.
            if (Hammer.Equipped && !_panel.Open && !Placer.Active && Input.GetMouseButtonDown(1) && !Menu.IsVisible()
                && !InventoryGui.IsVisible() && !Minimap.IsOpen() && !Console.IsVisible() && !(Chat.instance != null && Chat.instance.HasFocus()))
            {
                if (!Spawner.CanAdmin) MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Admin hammer: you are not an admin on this server");
                else { _panel.Show(); _openedByHammer = true; }
            }
            // Opened from the hammer: closes when the hammer leaves the hand, like the build menu.
            if (_openedByHammer && (!_panel.Open || !Hammer.Equipped)) { if (_panel.Open) _panel.Close(); _openedByHammer = false; }
            if (_panel.Open && Input.GetKeyDown(KeyCode.Escape)) _panel.Close();
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

    // While the panel is open the game treats it as a menu: free cursor, no player input.
    [HarmonyPatch(typeof(Menu), nameof(Menu.IsVisible))]
    internal static class MenuVisiblePatch
    {
        private static void Postfix(ref bool __result) { if (Plugin.PanelOpen) __result = true; }
    }

    // Valheim frees the cursor only when no menu reports itself visible, so the patch above alone
    // keeps it captured. While the panel is open, unlock and show it ourselves (as SeneaL does for
    // its smelter window) and skip the game's capture logic.
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
