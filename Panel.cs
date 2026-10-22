using System;
using System.Collections.Generic;
using System.Linq;
using SeneaLUI.Components;
using SeneaLUI.Core;
using SeneaLUI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Jatify.AdminHammer
{
    // The admin panel, built from SeneaL's own UI kit and laid out like its build menu
    // (BuildMenuView: 1100x640 panel, tag column at x24, controls strip at x264, 72 px slots),
    // so it follows SeneaL's look and settings. Three tabs: creatures, items, world.
    // ponytail: rebuilds a tab's rows and slots on every change (no pooling); ~400 slots is fine.
    internal sealed class Panel
    {
        private enum Tab { Creatures, Items, World }

        private readonly RectTransform _root, _panel, _specialRoot, _strip, _subs, _hints;
        private readonly CanvasGroup _group;
        private readonly TextMeshProUGUI _title, _specialText;
        private readonly Slot _special;
        private readonly Seg _tabs;
        private readonly SearchBox _search;
        private readonly ScrollBox _tags, _grid, _mods;

        private Tab _tab = Tab.Creatures;
        private string _cat = "Meadows", _query = "", _sel;
        private int _stars, _count = 1, _amount = 10;
        private string _target = "";   // "" = me / where I aim, else a player's name

        internal bool Open => _root != null && _root.gameObject.activeSelf;
        internal Transform Root => _root;

        internal Panel(Transform hudroot)
        {
            _root = Ui.Node(hudroot, "AdminHammerPanel");
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0.5f, 0.5f);
            _root.anchoredPosition = Vector2.zero;
            _root.SetAsLastSibling();
            _group = _root.gameObject.AddComponent<CanvasGroup>();

            _panel = Ui.Center(_root, "panel", 0f, 50f, 1100f, 640f);
            Ui.Panel(_panel, Skin.Sliced(64, Styles.Panel), Styles.Panel);
            Ui.Img(Ui.Stretch(_panel, "inner", -5f), Skin.Sliced(48, Styles.PanelInner), Color.white).type = Image.Type.Sliced;
            Ui.Img(_panel, null, Palette.Clear).raycastTarget = true;

            var heading = TextStyles.Heading;
            _title = Ui.Label(_panel, "title", heading, 0f, 24f, 20f + (32f - heading.Size * 1.3f) * 0.5f + heading.Baseline(1.3f), 500f, TextAlignmentOptions.TopLeft);
            // Icon segments sized by their labels, as SeneaL's build menu does (By Usage / By Material ...).
            var ext = AdminHammerApi.Tabs;
            _tabs = new Seg(_panel, "tabs", new[] { "trophy", "chest", "gear" }.Concat(ext.Select(t => t.Icon ?? "star")).ToArray(), OnTab);
            _tabs.SetLabels(new[] { "Creatures", "Items", "World" }.Concat(ext.Select(t => t.Name)).ToArray(), false);
            _tabs.Root.anchoredPosition = new Vector2(-24f, -20f);

            _specialRoot = Ui.TL(_panel, "special", 24f, 66f, 220f, 56f);
            _special = new Slot(_specialRoot, "s", 0f, 0f, 56, 40, null);
            var rowName = TextStyles.RowName;
            _specialText = Ui.Label(_specialRoot, "t", rowName, 0f, 68f, (56f - rowName.Size * 1.3f) * 0.5f + rowName.Baseline(1.3f), 152f, TextAlignmentOptions.TopLeft);
            _specialText.overflowMode = TextOverflowModes.Ellipsis;

            _search = new SearchBox(_panel, "search", 220f);
            _search.Root.anchoredPosition = new Vector2(24f, -134f);
            _search.Changed += q => { _query = (q ?? "").Trim().ToLowerInvariant(); Paint(); };

            _tags = new ScrollBox(_panel, "tags", 220f, 380f, 129f);
            _tags.Place(24f, 178f, 220f, 380f, 4f);

            _strip = Ui.TL(_panel, "strip", 264f, 66f, 812f, 34f);
            Ui.Img(Ui.TL(_panel, "stripline", 264f, 100f, 812f, 1f), null, Palette.Rgba(214, 178, 110, 0.16f));
            _subs = Ui.TL(_panel, "subs", 264f, 110f, 812f, 26f);
            _grid = new ScrollBox(_panel, "grid", 10 * 80f - 8f, 420f, 160f);
            _grid.Place(264f, 146f, 10 * 80f - 8f, 420f, 8f);
            _mods = new ScrollBox(_panel, "mods", 812f, 456f, 120f);
            _mods.Place(264f, 110f, 800f, 456f, 4f);
            _hints = Ui.TL(_panel, "hints", 264f, 578f, 812f, 40f);
            foreach (var t in ext)
            {
                var area = Ui.TL(_panel, "ext" + t.Name, 24f, 66f, 1052f, 552f);
                try { t.Build?.Invoke(area); } catch (Exception e) { Log.Warn($"tab {t.Name}: {e}"); }
                area.gameObject.SetActive(false);
                _ext.Add(area);
            }

            _root.gameObject.SetActive(false);
            // Portraits arrive over a few seconds: repaint the grid as they land (throttled).
            Catalog.IconsChanged += () => { if (Open && _tab == Tab.Creatures) _iconsDirty = true; };
        }

        private readonly List<RectTransform> _ext = new List<RectTransform>();
        private int ExtIndex => (int)_tab - 3;
        private AdminHammerApi.Tab Ext => ExtIndex >= 0 ? AdminHammerApi.Tabs[ExtIndex] : null;

        private bool _iconsDirty;
        private float _repaintAt;

        internal void Toggle() { if (Open) Close(); else Show(); }

        internal void Show()
        {
            Catalog.Build();
            Modifiers.Load();
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Paint();
        }

        internal void Close()
        {
            _search.Blur();
            _root.gameObject.SetActive(false);
        }

        internal void Tick()
        {
            if (!Open) return;
            if (Ext != null) { try { Ext.Tick?.Invoke(); } catch (Exception e) { Log.Warn($"tab {Ext.Name}: {e}"); } }
            if (_iconsDirty && Time.unscaledTime >= _repaintAt) { _iconsDirty = false; _repaintAt = Time.unscaledTime + 0.5f; PaintGrid(); PaintTags(); }
            _search.Tick(true);
            _grid.Tick(Hud.instance);
            _tags.Tick(Hud.instance);
            _mods.Tick(Hud.instance);
        }

        private void OnTab(int i)
        {
            _tab = (Tab)i; _sel = null; _query = ""; _search.Clear();
            if (Ext != null) { Paint(); return; }
            _cat = _tab == Tab.Creatures ? "Meadows" : _tab == Tab.Items ? "Materials" : "";
            Paint();
        }

        // ---- painting ------------------------------------------------------------------------

        private void Paint()
        {
            _tabs.SetOn((int)_tab);
            for (int i = 0; i < _ext.Count; i++) _ext[i].gameObject.SetActive(i == ExtIndex);
            bool ext = Ext != null;
            _specialRoot.gameObject.SetActive(!ext); _strip.gameObject.SetActive(!ext); _hints.gameObject.SetActive(!ext);
            if (ext)
            {
                _title.text = Ext.Title ?? Ext.Name;
                _grid.SetActive(false); _tags.SetActive(false); _search.Root.gameObject.SetActive(false); _subs.gameObject.SetActive(false); _mods.SetActive(false);
                try { Ext.Show?.Invoke(); } catch (Exception e) { Log.Warn($"tab {Ext.Name}: {e}"); }
                return;
            }
            _title.text = _tab == Tab.Creatures ? "Spawn creatures" : _tab == Tab.Items ? "Spawn items" : "World modifiers";
            bool grid = _tab != Tab.World;
            _grid.SetActive(grid); _tags.SetActive(grid); _search.Root.gameObject.SetActive(grid);
            _subs.gameObject.SetActive(grid); _mods.SetActive(!grid);
            PaintSpecial();
            if (grid) { PaintTags(); PaintGrid(); PaintSubs(); }
            else PaintWorld();
            PaintStrip();
            PaintHints();
        }

        private List<Catalog.Entry> Source => _tab == Tab.Creatures ? Catalog.Creatures : Catalog.Items;
        private string[] Categories => _tab == Tab.Creatures ? Catalog.CreatureCategories : Catalog.ItemCategories;

        private IEnumerable<Catalog.Entry> Visible() =>
            _query.Length > 0 ? Source.Where(e => e.Name.ToLowerInvariant().Contains(_query) || e.Prefab.ToLowerInvariant().Contains(_query))
                              : Source.Where(e => e.Category == _cat);

        private void PaintSpecial()
        {
            var e = _sel != null ? Source.FirstOrDefault(x => x.Prefab == _sel) : null;
            _special.SetIcon(e?.Icon ?? Icons.Get(_tab == Tab.Creatures ? "stag" : _tab == Tab.Items ? "chest" : "gear"), e != null);
            _specialText.text = e != null ? e.Name : _tab == Tab.Creatures ? "Pick a creature" : _tab == Tab.Items ? "Pick an item" : "Live on the server";
        }

        private void PaintTags()
        {
            Clear(_tags.Content);
            float y = 0f;
            foreach (var cat in Categories)
            {
                var items = Source.Where(e => e.Category == cat).ToList();
                if (items.Count == 0) continue;
                bool on = _query.Length == 0 && cat == _cat;
                var row = Ui.TL(_tags.Content, "t" + cat, 0f, y, 220f, 40f);
                var band = Ui.Img(row, on ? Skin.Sliced(40, Styles.RowOn) : null, on ? Color.white : Palette.Clear);
                band.type = Image.Type.Sliced; band.raycastTarget = true;
                if (on) Ui.Img(Ui.Place(Ui.Node(row, "bar"), 0f, 0.5f, 0f, 0f, 3f, 26f), null, Palette.Gold);
                Ui.Img(Ui.Place(Ui.Node(row, "fr"), 0f, 0.5f, 12f, 0f, 30f, 30f), Skin.Box(30, 30, Styles.SlotEmpty, 0), Color.white);
                var icon = Ui.Icon(row, "i", items.FirstOrDefault(e => e.Icon != null)?.Icon, Color.white, 0f, 0.5f, 15f, 0f, 24f);
                icon.preserveAspect = true;
                var nameStyle = TextStyle.Medium(15f, on ? Palette.Parchment : Palette.Linen);
                Ui.Label(row, "n", nameStyle, 0f, 54f, (40f - nameStyle.Size * 1.3f) * 0.5f + nameStyle.Baseline(1.3f), 120f, TextAlignmentOptions.TopLeft).text = cat;
                var countStyle = TextStyle.SemiBold(12f, Palette.Ash);
                Ui.Label(row, "c", countStyle, 1f, -12f, (40f - countStyle.Size * 1.3f) * 0.5f + countStyle.Baseline(1.3f), 44f, TextAlignmentOptions.TopRight).text = items.Count.ToString();
                var c = cat;
                Click(row, () => { _cat = c; _query = ""; _search.Clear(); Paint(); });
                y += 43f;
            }
            _tags.SetContentHeight(y);
        }

        private void PaintGrid()
        {
            Clear(_grid.Content);
            int i = 0;
            foreach (var e in Visible())
            {
                var slot = new Slot(_grid.Content, "p" + i, (i % 10) * 80f, (i / 10) * 80f, 72, 58, null, false);
                slot.SetIcon(e.Icon ?? Icons.Get("stag"), e.Prefab == _sel);
                Ui.Img(slot.Root, null, Palette.Clear).raycastTarget = true;
                if (e.Boss)
                {
                    var star = Ui.TR(slot.Root, "star", 5f, 5f, 14f, 14f);
                    Ui.Icon(star, "i", Icons.Get("star"), Palette.Gold, 0.5f, 0.5f, 0f, 0f, 14f);
                }
                var entry = e;
                Tooltip.Bind(slot.Root, () => new Tip().Reset(entry.Name, 1).AddRow(entry.Category, entry.Prefab));
                Click(slot.Root, () => Pick(entry));
                i++;
            }
            _grid.SetContentHeight(Mathf.CeilToInt(i / 10f) * 80f);
            _grid.ScrollTop();
        }

        private void PaintSubs()
        {
            Clear(_subs);
            float x = 0f;
            var who = new List<string> { "" };
            who.AddRange(Spawner.Players().Select(p => p.name));
            foreach (var name in who)
            {
                string label = name.Length == 0 ? (_tab == Tab.Creatures ? "Where I aim" : "Me") : name;
                var n = name;
                x += Button(_subs, label, x, 0f, 26, _target == n, () => { _target = n; Paint(); }) + 6f;
            }
        }

        private void PaintStrip()
        {
            Clear(_strip);
            var lbl = TextStyles.CardLabel;
            float x = 0f;
            if (_tab == Tab.Creatures)
            {
                x = Label(_strip, "STARS", x);
                for (int s = 0; s <= 2; s++) { int v = s; x += Button(_strip, s == 0 ? "-" : new string('*', s), x, 3f, 28, _stars == s, () => { _stars = v; Paint(); }, 36f) + 4f; }
                x = Label(_strip, "COUNT", x + 14f);
                foreach (int n in new[] { 1, 3, 5, 10 }) { int v = n; x += Button(_strip, n.ToString(), x, 3f, 28, _count == n, () => { _count = v; Paint(); }, 40f) + 4f; }
            }
            else if (_tab == Tab.Items)
            {
                x = Label(_strip, "AMOUNT", x);
                foreach (int n in new[] { 1, 10, 50, 0 }) { int v = n; x += Button(_strip, n == 0 ? "Stack" : n.ToString(), x, 3f, 28, _amount == n, () => { _amount = v; Paint(); }, n == 0 ? 64f : 44f) + 4f; }
            }
        }

        private void PaintHints()
        {
            Clear(_hints);
            float x = 0f;
            // "Close [Esc]" bottom right, like SeneaL's build menu.
            var esc = new Keycap(_hints, "esc", 24, 26f, 8f, Styles.Key, TextStyles.Key);
            esc.Set("Esc");
            float escW = Mathf.Max(34f, esc.Root.sizeDelta.x);
            esc.Root.anchoredPosition = new Vector2(812f - escW, -8f);
            var close = Ui.Label(_hints, "close", TextStyles.Hint, 0f, 812f - escW - 160f, 26f, 152f, TextAlignmentOptions.TopRight);
            close.text = "Close";
            if (_tab == Tab.Creatures)
            {
                x = Key(_hints, "LMB", "place", x); x = Key(_hints, "RMB", "menu", x); x = Key(_hints, "Wheel", "turn", x);
            }
            else if (_tab == Tab.Items)
            {
                var drop = new GoldButton(_hints, "drop", 140f, Drop, 36);
                drop.SetLabel("Drop");
                drop.SetEnabled(_sel != null);
                drop.Root.anchoredPosition = new Vector2(812f - escW - 80f - 140f, -2f);
                x = Key(_hints, "Click", "pick an item", x);
            }
            else
            {
                Ui.Label(_hints, "w", TextStyles.Hint, 0f, 0f, 24f, 700f, TextAlignmentOptions.TopLeft).text =
                    "Only the world-modifier menu's own settings, so the world is never flagged as cheated.";
            }
        }

        private void PaintWorld()
        {
            Clear(_mods.Content);
            var cur = Modifiers.Current();
            float y = 0f;
            foreach (var s in Modifiers.Sliders)
            {
                int active = Modifiers.Active(s, cur);
                var row = Row(_mods.Content, s.Name, s.Tip, y);
                float x = 220f;
                for (int i = 0; i < s.Settings.Count; i++)
                {
                    int idx = i; var sl = s;
                    x += Button(row, s.Settings[i].name, x, 12f, 28, i == active, () => { Modifiers.Apply(sl, idx); Toast($"{sl.Name}: {sl.Settings[idx].name}"); Later(PaintWorld); }) + 4f;
                }
                y += 58f;
            }
            foreach (var t in Modifiers.Toggles)
            {
                var row = Row(_mods.Content, t.Name, t.Tip, y);
                var sw = new SwitchBox(row, "sw", on => { Modifiers.Set(t, on); Toast($"{t.Name}: {(on ? "on" : "off")}"); Later(PaintWorld); });
                sw.Set(cur.Contains(t.Key));
                Ui.Place(sw.Root, 1f, 0.5f, -16f, 0f, sw.Root.sizeDelta.x, sw.Root.sizeDelta.y);
                y += 58f;
            }
            _mods.SetContentHeight(y);
        }

        // ---- actions -------------------------------------------------------------------------

        private void Pick(Catalog.Entry e)
        {
            _sel = e.Prefab;
            if (_tab == Tab.Creatures)
            {
                if (_target.Length == 0) { if (Placer.Begin(e.Prefab, _stars, _count)) Close(); else Paint(); return; }
                var p = Spawner.Players().FirstOrDefault(x => x.name == _target);
                if (p.name == null) { Toast($"{_target} is not online"); return; }
                for (int i = 0; i < _count; i++)
                    Spawner.Creature(e.Prefab, p.pos + p.forward * 5f + Vector3.up + UnityEngine.Random.insideUnitSphere * 2f, Quaternion.LookRotation(-p.forward), _stars);
                Toast($"{_count} x {e.Name} next to {_target}");
            }
            Paint();
        }

        private void Drop()
        {
            var e = Catalog.Items.FirstOrDefault(x => x.Prefab == _sel);
            if (e == null) return;
            int amount = _amount == 0 ? e.MaxStack : _amount;
            if (_target.Length == 0)
            {
                var me = Player.m_localPlayer;
                if (me != null) Spawner.Spill(e.Prefab, amount, me.transform.position, me.transform.forward);
                Toast($"{amount} x {e.Name}");
            }
            else
            {
                var p = Spawner.Players().FirstOrDefault(x => x.name == _target);
                if (p.name == null) { Toast($"{_target} is not online"); return; }
                Spawner.Spill(e.Prefab, amount, p.pos, p.forward);
                Toast($"{amount} x {e.Name} for {_target}");
            }
        }

        // ---- small builders ------------------------------------------------------------------

        private static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject); }

        private static void Click(RectTransform rt, Action a) => rt.gameObject.AddComponent<UIInputHandler>().m_onLeftClick = _ => a();

        // A small SeneaL button, gold when on; returns its width.
        private static float Button(RectTransform parent, string label, float x, float y, int h, bool on, Action a, float w = 0f)
        {
            if (w <= 0f) w = Mathf.Max(56f, 18f + label.Length * 8f);
            var b = new GoldButton(parent, "b" + label, w, a, h, !on, on ? null : TextStyles.BtnSmallDark);
            b.SetLabel(label);
            b.Root.anchoredPosition = new Vector2(x, -y);
            return w;
        }

        private static float Label(RectTransform parent, string text, float x)
        {
            var s = TextStyles.CardLabel;
            var t = Ui.Label(parent, "l" + text, s, 0f, x, 22f, 120f, TextAlignmentOptions.TopLeft);
            t.text = text;
            return x + t.preferredWidth + 10f;
        }

        private static float Key(RectTransform parent, string key, string text, float x)
        {
            var k = new Keycap(parent, "k" + key, 24, 26f, 8f, Styles.Key, TextStyles.Key);
            k.Set(key);
            k.Root.anchoredPosition = new Vector2(x, -8f);
            float w = Mathf.Max(26f, k.Root.sizeDelta.x);
            var t = Ui.Label(parent, "kt" + key, TextStyles.Hint, 0f, x + w + 6f, 26f, 160f, TextAlignmentOptions.TopLeft);
            t.text = text;
            return x + w + 12f + t.preferredWidth + 18f;
        }

        private static RectTransform Row(RectTransform parent, string name, string tip, float y)
        {
            var row = Ui.TL(parent, "r" + name, 0f, y, 800f, 52f);
            Ui.Img(row, Skin.Sliced(52, Styles.SkillRow), Color.white).type = Image.Type.Sliced;
            var title = TextStyle.SemiBold(14f, Palette.Parchment);
            Ui.Label(row, "n", title, 0f, 16f, 22f, 196f, TextAlignmentOptions.TopLeft).text = name;
            if (!string.IsNullOrEmpty(tip))
            {
                var t = Ui.Label(row, "d", TextStyles.Desc, 0f, 16f, 40f, 196f, TextAlignmentOptions.TopLeft);
                t.text = tip; t.fontSize = 11f; t.overflowMode = TextOverflowModes.Ellipsis;
            }
            return row;
        }

        private static void Toast(string text) => MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, text);

        // The server's keys come back a moment after we send them; repaint then.
        private void Later(Action a) => Plugin.Instance.StartCoroutine(After(0.6f, a));
        private static System.Collections.IEnumerator After(float s, Action a) { yield return new WaitForSeconds(s); a(); }
    }
}
