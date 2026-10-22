using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jatify.AdminHammer
{
    // For add-on mods: an extra tab in the admin panel. Register in your plugin's Awake
    // (with [BepInDependency(Jatify.AdminHammer.Plugin.Guid)]); the panel is built when the HUD appears.
    public static class AdminHammerApi
    {
        public sealed class Tab
        {
            public string Name;          // tab label
            public string Icon;          // a SeneaL UI icon name (Icons.Get)
            public string Title;         // window title while the tab is open
            public Action<RectTransform> Build;   // once: fill this 1052 x 552 area (top-left origin)
            public Action Show;          // each time the tab is shown or the panel opens on it
            public Action Tick;          // every frame while the tab is shown
        }

        internal static readonly List<Tab> Tabs = new List<Tab>();

        public static void AddTab(Tab tab)
        {
            if (tab == null || string.IsNullOrEmpty(tab.Name)) throw new ArgumentException("tab needs a Name");
            Tabs.Add(tab);
        }

        // Status line in the panel's style (top-left message), for add-ons.
        public static void Toast(string text) => MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, text);
    }
}
