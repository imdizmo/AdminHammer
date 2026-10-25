using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Jatify.AdminHammer.AdminHammerUi;

namespace Jatify.AdminHammer
{
    // The admin panel from the Jatify design (live artifact, AdminHammer view): 1100 x 660, a header with the tabs, the
    // target and Close; a rail (search, categories), the tiles with their options, an inspector with the one action
    // that matters; a key-hint footer. World is one column of rows. Add-on tabs (AdminHammerApi) fill the body.
    // ponytail: a change rebuilds the parts it touches (no pooling); a few hundred tiles is fine.
    internal sealed class Panel
    {
        private enum Tab { Creatures, Items, World }
        private const float W = 1100f, H = 660f, Head = 56f, Foot = 44f, Body = H - Head - Foot, RailW = 232f, InsW = 288f, WorkW = W - RailW - InsW;

        private readonly RectTransform _root, _head, _body, _foot;
        private RectTransform _cats, _toolbar, _grid, _ins, _popup;
        private TMP_InputField _search;

        private int _tab;   // 0..2 ours, then add-on tabs
        private readonly Dictionary<Tab, string> _cat = new Dictionary<Tab, string> { [Tab.Creatures] = "Meadows", [Tab.Items] = "Materials" };
        private readonly Dictionary<Tab, string> _sel = new Dictionary<Tab, string>();
        private string _query = "";
        private int _stars, _count = 1, _amount = 10;
        private bool _tamed;
        private string _target = "";   // "" = me / where I aim, else a player's name
        private string _status = ""; private float _statusUntil;
        private readonly List<RectTransform> _ext = new List<RectTransform>();

        internal bool Open => _root != null && _root.gameObject.activeSelf;
        internal string Target => _target;
        private Tab Ours => (Tab)Mathf.Min(_tab, 2);
        private AdminHammerApi.Tab Ext => _tab >= 3 && _tab - 3 < AdminHammerApi.Tabs.Count ? AdminHammerApi.Tabs[_tab - 3] : null;

        internal Panel(Transform hudroot)
        {
            _root = Node(hudroot, "AdminHammerPanel");
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0.5f, 0.5f); _root.anchoredPosition = new Vector2(0f, 30f); _root.sizeDelta = new Vector2(W, H);
            var bg = Box(_root, "Panel", new Color(0.078f, 0.078f, 0.086f, 0.9f), C("glassLine"), 14); Fill(bg.rectTransform);
            _head = Place(Node(_root, "Head"), 0f, 0f, W, Head);
            Line(_root, 0f, Head, W);
            _body = Place(Node(_root, "Body"), 0f, Head + 1f, W, Body - 1f);
            Line(_root, 0f, H - Foot, W);
            _foot = Place(Node(_root, "Foot"), 0f, H - Foot + 1f, W, Foot - 1f);
            foreach (var t in AdminHammerApi.Tabs)
            {
                var area = Place(Node(_root, "Ext " + t.Name), 0f, Head + 1f, W, Body - 1f);
                try { t.Build?.Invoke(area); } catch (Exception e) { Log.Warn($"tab {t.Name}: {e}"); }
                area.gameObject.SetActive(false); _ext.Add(area);
            }
            _root.gameObject.SetActive(false);
            Catalog.IconsChanged += () => { if (Open && _tab == 0) _iconsDirty = true; };   // portraits arrive over a few seconds
        }

        private bool _iconsDirty; private float _repaintAt;

        internal void Toggle() { if (Open) Close(); else Show(); }

        internal void Show()
        {
            Catalog.Build(); Modifiers.Load();
            _root.gameObject.SetActive(true); _root.SetAsLastSibling();
            BuildTab();
        }

        internal void Close() { ClosePopup(); _root.gameObject.SetActive(false); }
        internal void Destroy() { if (_root != null) UnityEngine.Object.Destroy(_root.gameObject); }

        internal void Tick()
        {
            if (!Open) return;
            if (Ext != null) { try { Ext.Tick?.Invoke(); } catch (Exception e) { Log.Warn($"tab {Ext.Name}: {e}"); } }
            if (_iconsDirty && Time.unscaledTime >= _repaintAt) { _iconsDirty = false; _repaintAt = Time.unscaledTime + 0.5f; PaintGrid(); }
            if (_status.Length > 0 && Time.unscaledTime > _statusUntil) { _status = ""; if (Ext == null && Ours != Tab.World) PaintInspector(); }
            if (Input.GetKeyDown(KeyCode.Slash) && _search != null && !_search.isFocused && Ext == null) _search.ActivateInputField();
        }

        internal Transform Root => _root;

        // the panel's status line (and add-ons' through AdminHammerApi.Status)
        internal void Flash(string text) { _status = text; _statusUntil = Time.unscaledTime + 2.6f; if (Ext == null && Ours != Tab.World) PaintInspector(); else if (Ours == Tab.World && Ext == null) PaintWorld(); }

        // ---- header: the brand, tabs, target, Close ----------------------------------------------------------------
        private void PaintHead()
        {
            Clear(_head); ClosePopup();
            var icon = ObjectDB.instance?.GetItemPrefab(Hammer.Prefab)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();   // the Admin hammer's own icon
            if (icon != null) { var i = JatifyLook.Img(_head, icon, Color.white, "Brand"); i.preserveAspect = true; Place(i.rectTransform, 18f, 14f, 28f, 28f); }
            var brand = Text(_head, "AdminHammer", "bold", 16f, Color.white, icon != null ? 54f : 18f, 0f, 160f, Head);
            float x = (icon != null ? 54f : 18f) + Width(brand) + 24f;
            var names = new List<(string name, bool addon)> { ("Creatures", false), ("Items", false), ("World", false) };
            names.AddRange(AdminHammerApi.Tabs.Select(t => (t.Name, true)));
            for (int i = 0; i < names.Count; i++)
            {
                int k = i; bool on = i == _tab;
                var hit = Box(_head, "Tab", NoColor, NoColor, 1);
                var t = Text(hit.transform, names[i].name, "medium", 14f, on ? Color.white : C("dim"), 12f, 0f, 200f, Head);
                float w = 12f + Width(t) + 12f;
                if (names[i].addon)
                {   // "add-on": the tab comes from another mod
                    var chip = Box(hit.transform, "Addon", NoColor, C("glassLine"), 4); chip.raycastTarget = false;
                    var ct = Text(chip.transform, "add-on", "mono", 10f, C("mute"), 0f, 0f, 60f, 16f, TextAlignmentOptions.Center);
                    float cw = Width(ct) + 8f; Place(chip.rectTransform, w - 4f, (Head - 16f) / 2f, cw, 16f); Fill(ct.rectTransform); w += cw + 8f;
                }
                Place(hit.rectTransform, x, 0f, w, Head);
                if (on) { var u = Box(hit.transform, "On", C("accent"), NoColor, 1); u.raycastTarget = false; Place(u.rectTransform, 12f, Head - 2f, w - 24f, 2f); }
                On(hit, () => { _tab = k; _query = ""; BuildTab(); });
                x += w + 4f;
            }
            // right: target (not on World), Close
            float right = W - 16f;
            var close = Box(_head, "Close", NoColor, C("glassLine"), 8);
            var ctext = Text(close.transform, "Close", "medium", 13f, C("dim"), 12f, 0f, 60f, 32f);
            float cw2 = 12f + Width(ctext) + 8f;
            float kx = Key(close.transform, "Esc", null, cw2, 32f);
            Place(close.rectTransform, right - (kx - 10f), 12f, kx - 10f, 32f); On(close, Close);
            right -= kx - 10f + 10f;
            if (Ours != Tab.World || Ext != null)
            {
                if (Ext != null && !Ext.Targets) return;
                string label = _target.Length > 0 ? _target : Ours == Tab.Creatures && Ext == null ? "Where I aim" : "Me";
                var box = Box(_head, "Target", C("raised"), C("glassLine"), 8);
                var tl = Text(box.transform, "Target", "medium", 13f, C("dim"), 12f, 0f, 60f, 32f); float tw = 12f + Width(tl) + 8f;
                var val = Box(box.transform, "Value", C("raised2"), NoColor, 6);
                var vt = Text(val.transform, label + "  ▾", "medium", 13f, Color.white, 10f, 0f, 160f, 26f); float vw = 10f + Width(vt) + 12f;
                Place(val.rectTransform, tw, 3f, vw, 26f);
                float bw = tw + vw + 4f; Place(box.rectTransform, right - bw, 12f, bw, 32f);
                On(val, () => Popup(right - bw + tw, 12f + 32f + 4f));
            }
        }

        // the target's list, under its button
        private void Popup(float x, float y)
        {
            if (_popup != null) { ClosePopup(); return; }
            var opts = new List<string> { "" }; opts.AddRange(Spawner.Players().Select(p => p.name));
            var box = Box(_root, "TargetList", new Color(0.094f, 0.094f, 0.106f, 0.98f), C("glassLine"), 8); _popup = box.rectTransform;
            float w = 180f, yy = 4f;
            foreach (var o in opts)
            {
                string n = o; bool on = o == _target;
                var row = Box(box.transform, "Opt", on ? C("raised2") : NoColor, NoColor, 6); Place(row.rectTransform, 4f, yy, w - 8f, 30f);
                Text(row.transform, o.Length == 0 ? (Ours == Tab.Creatures && Ext == null ? "Where I aim" : "Me") : o, "medium", 13f, on ? Color.white : C("dim"), 10f, 0f, w - 28f, 30f);
                On(row, () => { _target = n; ClosePopup(); AdminHammerApi.TargetChanged?.Invoke(); BuildTab(); });
                yy += 32f;
            }
            Place(_popup, Mathf.Min(x, W - w - 8f), y, w, yy + 2f);
        }
        private void ClosePopup() { if (_popup != null) UnityEngine.Object.Destroy(_popup.gameObject); _popup = null; }

        // ---- body ---------------------------------------------------------------------------------------------------
        private void BuildTab()
        {
            for (int i = 0; i < _ext.Count; i++) _ext[i].gameObject.SetActive(_tab - 3 == i);
            PaintHead(); Clear(_body); _search = null; _cats = _toolbar = _grid = _ins = null;
            _body.gameObject.SetActive(Ext == null);
            if (Ext != null) { try { Ext.Show?.Invoke(); } catch (Exception e) { Log.Warn($"tab {Ext.Name}: {e}"); } PaintFoot(); return; }
            if (Ours == Tab.World) { PaintWorld(); PaintFoot(); return; }
            // rail: search, categories
            Line(_body, RailW, 0f, 1f, Body - 1f);
            _search = Search(_body, "Search " + (Ours == Tab.Creatures ? "creatures" : "items"), 12f, 14f, RailW - 24f, q => { _query = (q ?? "").Trim().ToLowerInvariant(); PaintCats(); PaintToolbar(); PaintGrid(); });
            _cats = Scroll(_body, "Cats", 12f, 60f, RailW - 24f, Body - 74f);
            // work: toolbar, tiles
            _toolbar = Place(Node(_body, "Toolbar"), RailW + 1f, 0f, WorkW - 1f, 58f);
            var tl = Box(_body, "ToolbarLine", LineSoft, NoColor, 1); tl.raycastTarget = false; Place(tl.rectTransform, RailW + 1f, 58f, WorkW - 1f, 1f);
            _grid = Scroll(_body, "Grid", RailW + 1f, 59f, WorkW - 1f, Body - 60f);
            // inspector
            Line(_body, RailW + WorkW, 0f, 1f, Body - 1f);
            _ins = Place(Node(_body, "Inspector"), RailW + WorkW + 1f, 0f, InsW - 1f, Body - 1f);
            PaintCats(); PaintToolbar(); PaintGrid(); PaintInspector(); PaintFoot();
        }

        private List<Catalog.Entry> Source => Ours == Tab.Creatures ? Catalog.Creatures : Catalog.Items;
        private string[] Categories => Ours == Tab.Creatures ? Catalog.CreatureCategories : Catalog.ItemCategories;
        private List<Catalog.Entry> Visible() =>
            (_query.Length > 0 ? Source.Where(e => e.Name.ToLowerInvariant().Contains(_query) || e.Prefab.ToLowerInvariant().Contains(_query))
                               : Source.Where(e => e.Category == _cat[Ours])).ToList();
        private Catalog.Entry Selected => _sel.TryGetValue(Ours, out var p) ? Source.FirstOrDefault(e => e.Prefab == p) : null;

        private void PaintCats()
        {
            if (_cats == null) return; Clear(_cats);
            float y = 0f;
            foreach (var cat in Categories)
            {
                int n = Source.Count(e => e.Category == cat); if (n == 0) continue;
                string c = cat;
                CatRow(_cats, cat, n, _query.Length == 0 && cat == _cat[Ours], y, RailW - 24f, () => { _cat[Ours] = c; _query = ""; if (_search != null) _search.text = ""; PaintCats(); PaintToolbar(); PaintGrid(); });
                y += 38f;
            }
            _cats.sizeDelta = new Vector2(0f, y);
        }

        private void PaintToolbar()
        {
            if (_toolbar == null) return; Clear(_toolbar);
            var list = Visible();
            float right = WorkW - 1f - 16f;
            if (Ours == Tab.Creatures)
            {
                right -= SegW(new[] { "1", "3", "5", "10" }); float cx = right;
                Seg(_toolbar, cx, 14f, new[] { "1", "3", "5", "10" }, Array.IndexOf(new[] { 1, 3, 5, 10 }, _count), i => { _count = new[] { 1, 3, 5, 10 }[i]; PaintToolbar(); PaintInspector(); });
                right = Label(_toolbar, "Count", cx) - 18f;
                right -= SegW(new[] { "0★", "1★", "2★" }); float sx = right;
                Seg(_toolbar, sx, 14f, new[] { "0★", "1★", "2★" }, _stars, i => { _stars = i; PaintToolbar(); PaintInspector(); });
                right = Label(_toolbar, "Stars", sx) - 18f - 38f;
                Switch(_toolbar, right, 18f, _tamed, on => { _tamed = on; PaintToolbar(); PaintInspector(); });   // user: tameable ones spawn tamed
                right = Label(_toolbar, "Tamed", right);
            }
            else
            {
                var opts = new[] { "1", "10", "50", "Stack" }; var vals = new[] { 1, 10, 50, 0 };
                right -= SegW(opts); Seg(_toolbar, right, 14f, opts, Array.IndexOf(vals, _amount), i => { _amount = vals[i]; PaintToolbar(); PaintInspector(); });
                right = Label(_toolbar, "Amount", right);
            }
            // the title in what is left, shortened with "…" if the controls need the room
            var n = Text(_toolbar, list.Count.ToString(), "mono", 12f, C("mute"), 0f, 1f, 40f, 58f); float nw = Width(n);
            var t = Text(_toolbar, _query.Length > 0 ? "Results" : _cat[Ours], "bold", 15f, Color.white, 16f, 0f, 200f, 58f);
            float tw = Mathf.Min(Width(t), right - 16f - 14f - 6f - nw - 16f); t.rectTransform.sizeDelta = new Vector2(Mathf.Max(20f, tw), 58f);
            Place(n.rectTransform, 16f + Mathf.Max(20f, tw) + 6f, 1f, nw + 2f, 58f);
        }
        // a small label right before a control, ending at x; returns its start
        private float Label(Transform parent, string s, float x)
        {
            var t = Text(parent, s, "medium", 12f, C("mute"), 0f, 0f, 80f, 58f); float w = Width(t);
            Place(t.rectTransform, x - 8f - w, 0f, w + 2f, 58f); return x - 8f - w;
        }
        private static float SegW(string[] labels)
        {
            float w = 2f;
            foreach (var l in labels) { var t = JatifyLook.Text(null, l, "medium", 13f, Color.white); w += Mathf.Max(32f, Width(t) + 20f) + 2f; UnityEngine.Object.Destroy(t.gameObject); }
            return w;
        }

        private void PaintGrid()
        {
            if (_grid == null) return; Clear(_grid);
            var list = Visible(); var sel = Selected;
            float tw = (WorkW - 1f - 32f - 16f) / 3f;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                var tile = Tile(_grid, e.Icon, e.Name, sel == e, 16f + (i % 3) * (tw + 8f), 14f + (i / 3) * 68f, tw,
                    () => { _sel[Ours] = e.Prefab; _status = ""; PaintGrid(); PaintInspector(); },
                    () => { _sel[Ours] = e.Prefab; Act(); });
                if (Ours == Tab.Creatures && e.Boss) Tag(tile, "BOSS", "boss");
            }
            if (list.Count == 0) Text(_grid, $"Nothing matches \"{_query}\". Try a shorter name.", "medium", 14f, C("mute"), 20f, 24f, WorkW - 40f, 24f);
            _grid.sizeDelta = new Vector2(0f, 14f + Mathf.CeilToInt(list.Count / 3f) * 68f + 10f);
        }

        private void PaintInspector()
        {
            if (_ins == null) return; Clear(_ins);
            float w = InsW - 1f - 36f, x = 18f;
            var e = Selected;
            if (e == null)
            {
                Para(_ins, Ours == Tab.Creatures ? "Pick a creature to place it where you aim or next to a player." : "Pick an item to drop it in front of you or a player.", 14f, C("mute"), x, 18f, w, 80f);
                Status(x, w); return;
            }
            IconBox(_ins, e.Icon, e.Name, x, 18f, 56f, 12);
            var name = Text(_ins, e.Name, "bold", 18f, Color.white, x + 68f, 20f, w - 68f, 28f); name.overflowMode = TextOverflowModes.Ellipsis;
            Text(_ins, e.Prefab, "mono", 12f, C("mute"), x + 68f, 48f, w - 68f, 18f);
            float y = 92f;
            if (Ours == Tab.Creatures)
            {
                y += Meta(_ins, x, y, w, (e.Category == "Bosses" ? "Type" : "Biome", e.Category == "Bosses" ? "Boss" : e.Category), ("Stars", _stars > 0 ? new string('★', _stars) : "None"),
                    ("Count", _count.ToString()), ("Tamed", !e.Tameable ? "Can't be tamed" : _tamed ? "Yes" : "No"), ("Target", _target.Length > 0 ? _target : "Where I aim")) + 14f;
                Para(_ins, _target.Length > 0 ? $"Spawns {_count} next to {_target}, facing them." : "The panel closes and a ghost follows your aim. Left click places, the wheel turns it, right click comes back here.", 12.5f, C("mute"), x, y, w, 70f);
                Primary(_ins, _target.Length > 0 ? $"Spawn next to {_target}" : "Place where I aim", x, Body - 1f - 18f - 22f - 50f, w, 42f, Act);
            }
            else
            {
                int amount = _amount == 0 ? e.MaxStack : _amount;
                y += Meta(_ins, x, y, w, ("Category", e.Category), ("Stack size", e.MaxStack.ToString()), ("Amount", _amount == 0 ? $"Full stack ({e.MaxStack})" : _amount.ToString()), ("For", _target.Length > 0 ? _target : "Me")) + 14f;
                Para(_ins, "Spills out like a broken chest. Double click a tile to drop straight away.", 12.5f, C("mute"), x, y, w, 50f);
                Primary(_ins, $"Drop {amount}", x, Body - 1f - 18f - 22f - 50f, w, 42f, Act);
            }
            Status(x, w);
        }
        private void Status(float x, float w) => Text(_ins, _status, "medium", 12f, C("accent"), x, Body - 1f - 18f - 22f, w, 20f);

        // ---- world ----------------------------------------------------------------------------------------------------
        private void PaintWorld()
        {
            Clear(_body);
            var list = Scroll(_body, "World", 0f, 0f, W, Body - 1f);
            float y = 14f, x = 18f, w = W - 36f;
            var ban = Box(list, "Banner", C("accentSoft"), NoColor, 10); ban.raycastTarget = false; Place(ban.rectTransform, x, y, w, 40f);
            Text(ban.transform, "Only the game's own world-modifier settings are used, so the world is never flagged as cheated. Changes apply live on the server.", "medium", 13f, C("accent"), 14f, 0f, w - 28f, 40f);
            y += 48f;
            var cur = Modifiers.Current();
            Section(list, "Difficulty", x + 2f, y + 6f, 300f); y += 30f;
            foreach (var s in Modifiers.Sliders)
            {
                var row = WorldRow(list, s.Name, s.Tip, x, y, w);
                var labels = s.Settings.Select(t => t.name).ToArray(); float sw = SegW(labels); var sl = s;
                Seg(row, w - 14f - sw, (64f - 30f) / 2f, labels, Modifiers.Active(s, cur), i => { Modifiers.Apply(sl, i); Flash($"{sl.Name}: {sl.Settings[i].name}"); Later(PaintWorld); }, true);
                y += 72f;
            }
            Section(list, "Rules", x + 2f, y + 6f, 300f); y += 30f;
            foreach (var t in Modifiers.Toggles)
            {
                var row = WorldRow(list, t.Name, t.Tip, x, y, w); var tt = t;
                Switch(row, w - 14f - 38f, 21f, cur.Contains(t.Key), on => { Modifiers.Set(tt, on); Flash($"{tt.Name}: {(on ? "on" : "off")}"); Later(PaintWorld); });
                y += 72f;
            }
            if (_status.Length > 0) Text(list, _status, "medium", 12f, C("accent"), x + 2f, y, w, 20f);
            list.sizeDelta = new Vector2(0f, y + 30f);
        }
        private static RectTransform WorldRow(Transform parent, string name, string tip, float x, float y, float w)
        {
            var row = Box(parent, "Row", C("raised"), LineSoft, 10); row.raycastTarget = false; Place(row.rectTransform, x, y, w, 64f);
            Text(row.transform, name, "bold", 14f, Color.white, 14f, 10f, 520f, 22f);
            if (!string.IsNullOrEmpty(tip)) { var p = Text(row.transform, tip, "medium", 12.5f, C("mute"), 14f, 32f, 520f, 22f); p.overflowMode = TextOverflowModes.Ellipsis; }
            return row.rectTransform;
        }

        // ---- footer ---------------------------------------------------------------------------------------------------
        private void PaintFoot()
        {
            Clear(_foot);
            float x = 16f, h = Foot - 1f;
            if (Ext != null) { try { Ext.Footer?.Invoke(_foot); } catch (Exception e) { Log.Warn($"tab {Ext.Name}: {e}"); } }
            else if (Ours == Tab.Creatures) { x = Key(_foot, "Click", "select", x, h); x = Key(_foot, "Double click", "place", x, h); Key(_foot, Plugin.KeyName, "open or close", x, h); }
            else if (Ours == Tab.Items) { x = Key(_foot, "Click", "select", x, h); x = Key(_foot, "Double click", "drop", x, h); Key(_foot, "/", "search", x, h); }
            else Key(_foot, Plugin.KeyName, "open or close", x, h);
            var r = Text(_foot, "Host or server admin only", "medium", 12.5f, C("mute"), 0f, 0f, 300f, h, TextAlignmentOptions.MidlineRight);
            Place(r.rectTransform, W - 16f - 300f, 0f, 300f, h);
        }

        // ---- actions --------------------------------------------------------------------------------------------------
        private void Act()
        {
            var e = Selected; if (e == null) return;
            if (Ours == Tab.Creatures)
            {
                if (_target.Length == 0) { if (Placer.Begin(e.Prefab, _stars, _count, _tamed)) Close(); return; }
                var p = Spawner.Players().FirstOrDefault(x => x.name == _target);
                if (p.name == null) { Flash($"{_target} is not online"); return; }
                for (int i = 0; i < _count; i++)
                    Spawner.Creature(e.Prefab, p.pos + p.forward * 5f + Vector3.up + UnityEngine.Random.insideUnitSphere * 2f, Quaternion.LookRotation(-p.forward), _stars, _tamed);
                Flash($"Spawned {_count} × {e.Name}{(_stars > 0 ? " " + new string('★', _stars) : "")}{(_tamed && e.Tameable ? ", tamed," : "")} next to {_target}");
            }
            else
            {
                int amount = _amount == 0 ? e.MaxStack : _amount;
                if (_target.Length == 0)
                {
                    var me = Player.m_localPlayer; if (me == null) return;
                    Spawner.Spill(e.Prefab, amount, me.transform.position, me.transform.forward);
                    Flash($"Dropped {amount} × {e.Name} in front of you");
                }
                else
                {
                    var p = Spawner.Players().FirstOrDefault(x => x.name == _target);
                    if (p.name == null) { Flash($"{_target} is not online"); return; }
                    Spawner.Spill(e.Prefab, amount, p.pos, p.forward);
                    Flash($"Dropped {amount} × {e.Name} for {_target}");
                }
            }
        }

        private static void Line(Transform parent, float x, float y, float w, float h = 1f)
        {
            var l = Box(parent, "Line", C("glassLine"), NoColor, 1); l.raycastTarget = false; Place(l.rectTransform, x, y, w, h);
        }

        // the server's keys come back a moment after we send them; repaint then
        private void Later(Action a) => Plugin.Instance.StartCoroutine(After(0.6f, a));
        private System.Collections.IEnumerator After(float s, Action a) { yield return new WaitForSeconds(s); if (Open) a(); }
    }
}
