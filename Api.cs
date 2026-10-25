using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jatify.AdminHammer
{
    // For add-on mods: an extra tab in the admin panel. Register in your plugin's Awake
    // (with [BepInDependency(Jatify.AdminHammer.Plugin.Guid)]); the panel is built when the HUD appears.
    // Build your tab from AdminHammerUi so it matches the panel.
    public static class AdminHammerApi
    {
        public sealed class Tab
        {
            public string Name;          // tab label (marked "add-on" in the header)
            public string Icon;          // unused since 1.1 (tabs are text, from the Jatify design); kept for old add-ons
            public string Title;         // unused since 1.1; kept for old add-ons
            public bool Targets;         // show the header's Target picker on this tab (Target, TargetChanged)
            public Action<RectTransform> Build;   // once: fill this 1100 x 559 body (between the header and the footer, top-left origin)
            public Action Show;          // each time the tab is shown or the panel opens on it
            public Action Tick;          // every frame while the tab is shown
            public Action<RectTransform> Footer;  // fill the 1100 x 43 footer's left with key hints (AdminHammerUi.Key); the panel adds the rest
        }

        internal static readonly List<Tab> Tabs = new List<Tab>();

        public static void AddTab(Tab tab)
        {
            if (tab == null || string.IsNullOrEmpty(tab.Name)) throw new ArgumentException("tab needs a Name");
            Tabs.Add(tab);
        }

        // the header's target: "" for me, else an online player's name
        public static string Target => Plugin.CurrentPanel?.Target ?? "";
        public static Action TargetChanged;

        // the panel's status line (the inspector's, or the World list's); a top-left message while the panel is closed
        public static void Status(string text)
        {
            var p = Plugin.CurrentPanel;
            if (p != null && p.Open) p.Flash(text); else Toast(text);
        }

        // a top-left message, as before 1.1
        public static void Toast(string text) => MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, text);
    }
}
