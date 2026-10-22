using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jatify.AdminHammer
{
    // The world-modifier screen's own sliders and toggles. Only keys that screen can set are used,
    // so the world never counts as cheated. Read from ServerOptionsGUI at runtime (it keeps them in a
    // static array after the start menu); the table below is the same data as a fallback.
    internal static class Modifiers
    {
        internal sealed class Slider
        {
            public string Name, Tip;
            public int Default;
            public List<(string name, List<string> keys)> Settings = new List<(string, List<string>)>();
        }

        internal sealed class Toggle { public string Name, Tip, Key; }

        internal static readonly List<Slider> Sliders = new List<Slider>();
        internal static readonly List<Toggle> Toggles = new List<Toggle>();

        internal static void Load()
        {
            if (Sliders.Count > 0) return;
            try { FromGame(); } catch (System.Exception e) { Log.Warn("modifier read failed, using the built-in table: " + e.Message); }
            if (Sliders.Count == 0) Fallback();
        }

        private static void FromGame()
        {
            var f = typeof(ServerOptionsGUI).GetField("m_modifiers", BindingFlags.Static | BindingFlags.NonPublic);
            if (!(f?.GetValue(null) is KeyUI[] all)) return;
            foreach (var ui in all)
            {
                // Destroyed with the start scene, but their managed fields still hold the data.
                if (ui is KeySlider s && (int)s.m_modifier != 0)   // 0 = the difficulty preset row: skip
                {
                    var sl = new Slider { Name = Words(s.m_modifier.ToString()), Tip = Loc(s.m_toolTip), Default = s.m_defaultIndex };
                    foreach (var st in s.m_settings) sl.Settings.Add((Loc(st.m_name), new List<string>(st.m_keys)));
                    Sliders.Add(sl);
                }
                else if (ui is KeyToggle t && !Toggles.Any(x => x.Key == t.m_enabledKey))
                    Toggles.Add(new Toggle { Key = t.m_enabledKey, Name = ToggleName(t), Tip = Loc(t.m_toolTip) });
            }
        }

        // "DeathPenalty" -> "Death penalty"
        private static string Words(string s) =>
            System.Text.RegularExpressions.Regex.Replace(s, "(?<=[a-z])([A-Z])", m => " " + m.Value.ToLowerInvariant());

        // The menu's label key is its tooltip key without "_tooltip" ($menu_firehazards_tooltip ->
        // $menu_firehazards); a few keys don't follow that, so fall back to a readable key name.
        private static string ToggleName(KeyToggle t)
        {
            foreach (var key in new[] { t.m_toolTip?.Replace("_tooltip", ""), "$menu_" + t.m_enabledKey })
            {
                if (string.IsNullOrEmpty(key)) continue;
                var s = Loc(key);
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("[") && !s.StartsWith("$")) return s;
            }
            return char.ToUpperInvariant(t.m_enabledKey[0]) + t.m_enabledKey.Substring(1);
        }

        private static string Loc(string s) => Localization.instance != null && s != null && s.StartsWith("$") ? Localization.instance.Localize(s) : s;

        private static void Fallback()
        {
            Slider S(string n, int d, params (string, string[])[] xs)
            {
                var s = new Slider { Name = n, Default = d };
                foreach (var (a, k) in xs) s.Settings.Add((a, k.ToList()));
                return s;
            }
            Sliders.Add(S("Combat", 2, ("Very easy", new[] { "playerdamage 125", "enemydamage 50", "enemyspeedsize 90" }), ("Easy", new[] { "playerdamage 110", "enemydamage 75", "enemyspeedsize 95" }),
                ("Normal", new string[0]), ("Hard", new[] { "playerdamage 85", "enemydamage 150", "enemyspeedsize 110", "enemyleveluprate 120" }), ("Very hard", new[] { "playerdamage 70", "enemydamage 200", "enemyspeedsize 120", "enemyleveluprate 140" })));
            Sliders.Add(S("Death penalty", 3, ("Casual", new[] { "deathkeepequip", "SkillReductionRate 15" }), ("Very easy", new[] { "SkillReductionRate 15" }), ("Easy", new[] { "SkillReductionRate 50" }),
                ("Normal", new string[0]), ("Hard", new[] { "DeathDeleteUnequipped", "SkillReductionRate 150" }), ("Hardcore", new[] { "deathdeleteitems", "deathskillsreset" })));
            Sliders.Add(S("Resources", 2, ("x 0.5", new[] { "resourcerate 50" }), ("x 0.75", new[] { "resourcerate 75" }), ("Normal", new string[0]), ("x 1.5", new[] { "resourcerate 150" }), ("x 2", new[] { "resourcerate 200" }), ("x 3", new[] { "resourcerate 300" })));
            Sliders.Add(S("Raids", 3, ("None", new[] { "eventrate 0" }), ("Much less", new[] { "eventrate 200" }), ("Less", new[] { "eventrate 150" }), ("Normal", new string[0]), ("More", new[] { "eventrate 60" }), ("Much more", new[] { "eventrate 30" })));
            Sliders.Add(S("Portals", 1, ("Teleport all", new[] { "teleportall" }), ("Normal", new string[0]), ("No boss portals", new[] { "nobossportals" }), ("No portals", new[] { "noportals" })));
            foreach (var (k, n) in new[] { ("nobuildcost", "No build cost"), ("nomap", "No map"), ("passivemobs", "Passive mobs"), ("playerevents", "Player-based raids"), ("fire", "Fire hazards"), ("teleportall", "Teleport all"), ("noportals", "No portals") })
                Toggles.Add(new Toggle { Key = k, Name = n });
        }

        // The key's name: "eventrate 60" -> "eventrate".
        internal static string KeyName(string key) => key.Split(' ')[0].ToLowerInvariant();

        // The world's current keys, lower-cased, as the server last synced them.
        internal static HashSet<string> Current() =>
            new HashSet<string>(ZoneSystem.instance != null ? ZoneSystem.instance.GetGlobalKeys().Select(k => k.ToLowerInvariant()) : Enumerable.Empty<string>());

        // Which setting of a slider is active: the one whose keys are all present (the most keys
        // wins, so "Casual" beats "Very easy"); none matching = its "Normal" (no keys).
        internal static int Active(Slider s, HashSet<string> cur)
        {
            int best = -1, bestN = -1;
            for (int i = 0; i < s.Settings.Count; i++)
            {
                var keys = s.Settings[i].keys;
                if (keys.All(k => cur.Contains(k.ToLowerInvariant())) && keys.Count > bestN) { best = i; bestN = keys.Count; }
            }
            return best;
        }

        // Switch a slider: drop every key its other settings use, then set this one's.
        internal static void Apply(Slider s, int index)
        {
            var mine = new HashSet<string>(s.Settings[index].keys.Select(KeyName));
            foreach (var name in s.Settings.SelectMany(x => x.keys).Select(KeyName).Distinct())
                if (!mine.Contains(name)) Spawner.RemoveKey(name);
            foreach (var k in s.Settings[index].keys) Spawner.SetKey(k.ToLowerInvariant());
        }

        internal static void Set(Toggle t, bool on)
        {
            if (on) Spawner.SetKey(t.Key); else Spawner.RemoveKey(t.Key);
        }
    }
}
