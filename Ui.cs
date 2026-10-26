using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jatify.AdminHammer
{
    // The panel's parts from the Jatify design (AdminHammer view), public so add-ons (BuffMeDaddy) build
    // their tabs from the same pieces. Colours and fonts come from Jatify UI when it is installed, the design's own
    // values and Valheim's font otherwise (JatifyLook). Coordinates are top-left, in pixels.
    public static class AdminHammerUi
    {
        public static Color C(string token) => JatifyLook.C(token);
        public static readonly Color LineSoft = new Color(1f, 1f, 1f, 0.06f), Ink = new Color(0.024f, 0.082f, 0.051f), NoColor = new Color(0f, 0f, 0f, 0f);

        public static RectTransform Node(Transform parent, string name) => JatifyLook.Node(parent, name);
        public static RectTransform Place(RectTransform r, float x, float y, float w, float h) => JatifyLook.Place(r, x, y, w, h);
        public static RectTransform Fill(RectTransform r, float inset = 0f) => JatifyLook.Fill(r, inset);
        public static Image Box(Transform parent, string name, Color fill, Color line, int radius) => JatifyLook.Box(parent, name, fill, line, radius);
        public static RectTransform Scroll(Transform parent, string name, float x, float y, float w, float h) => JatifyLook.Scroll(parent, name, x, y, w, h);
        public static TMP_InputField Search(Transform parent, string placeholder, float x, float y, float w, Action<string> changed) => JatifyLook.Field(parent, placeholder, x, y, w, changed);
        public static void Clear(Transform t) => JatifyLook.Clear(t);

        // one line of text ("medium", "bold" or "mono"), placed
        public static TextMeshProUGUI Text(Transform parent, string s, string font, float size, Color c, float x, float y, float w, float h, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var t = JatifyLook.Text(parent, s, font, size, c, align); Place(t.rectTransform, x, y, w, h); return t;
        }
        // text that wraps to its width, top aligned
        public static TextMeshProUGUI Para(Transform parent, string s, float size, Color c, float x, float y, float w, float h)
        {
            var t = Text(parent, s, "medium", size, c, x, y, w, h, TextAlignmentOptions.TopLeft);
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Truncate; t.lineSpacing = -8f; return t;
        }
        public static float Width(TMP_Text t) => JatifyLook.Width(t);

        // a picture in a dark rounded box (tiles, the inspector), or the name's initials without one
        public static RectTransform IconBox(Transform parent, Sprite icon, string name, float x, float y, float size, int radius)
        {
            var b = Box(parent, "IconBox", new Color(0.078f, 0.078f, 0.086f, 0.9f), C("glassLine"), radius); b.raycastTarget = false; Place(b.rectTransform, x, y, size, size);
            if (icon != null) { var i = JatifyLook.Img(b.transform, icon, Color.white, "Icon"); i.preserveAspect = true; Fill(i.rectTransform, size * 0.12f); }
            else Fill(Text(b.transform, Initials(name), "bold", size * 0.36f, C("dim"), 0f, 0f, size, size, TextAlignmentOptions.Center).rectTransform);
            return b.rectTransform;
        }
        private static string Initials(string name)
        {
            var parts = (name ?? "?").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); string s = "";
            foreach (var p in parts) if (s.Length < 2 && char.IsLetter(p[0]) && (parts.Length < 2 || (p != "of" && p != "the" && p != "and"))) s += char.ToUpperInvariant(p[0]);
            return s.Length > 0 ? s : "?";
        }

        // clicks: left, double (left twice) and right
        public sealed class Clicks : MonoBehaviour, IPointerClickHandler
        {
            public Action Left, Double, Right;
            public void OnPointerClick(PointerEventData e)
            {
                if (e.button == PointerEventData.InputButton.Right) { Right?.Invoke(); return; }
                if (e.button != PointerEventData.InputButton.Left) return;
                if (e.clickCount >= 2 && Double != null) Double(); else Left?.Invoke();
            }
        }
        public static Clicks On(Graphic g, Action left, Action dbl = null, Action right = null)
        {
            g.raycastTarget = true; var c = g.gameObject.AddComponent<Clicks>(); c.Left = left; c.Double = dbl; c.Right = right; return c;
        }

        // a row in the left rail: a dot (green when on), the name, a count
        public static void CatRow(Transform parent, string name, int count, bool on, float y, float w, Action pick)
        {
            var row = Box(parent, "Cat", on ? C("raised2") : NoColor, NoColor, 8); Place(row.rectTransform, 0f, y, w, 36f);
            var dot = Box(row.transform, "Dot", on ? C("accent") : C("glassLine"), NoColor, 3); dot.raycastTarget = false; Place(dot.rectTransform, 10f, 14.5f, 7f, 7f);
            Text(row.transform, name, "medium", 14f, on ? Color.white : C("dim"), 27f, 0f, w - 70f, 36f);
            Text(row.transform, count.ToString(), "mono", 12f, C("mute"), w - 50f, 0f, 40f, 36f, TextAlignmentOptions.MidlineRight);
            On(row, pick);
        }

        // a tile in the grid: picture, name (two lines), outline green when picked
        public static RectTransform Tile(Transform parent, Sprite icon, string name, bool on, float x, float y, float w, Action pick, Action dbl = null, Action right = null)
        {
            var t = Box(parent, "Tile", on ? C("accentSoft") : C("raised"), on ? C("accent") : LineSoft, 10); Place(t.rectTransform, x, y, w, 60f);
            IconBox(t.transform, icon, name, 10f, 11f, 38f, 9);
            var n = Text(t.transform, name, "medium", 13f, Color.white, 58f, 6f, w - 68f, 48f);
            n.textWrappingMode = TextWrappingModes.Normal; n.lineSpacing = -12f; n.maxVisibleLines = 2;
            On(t, pick, dbl, right);
            return t.rectTransform;
        }
        // a small tag on a tile: "boss" (top right, orange), "on" (top right, green), "inf" (bottom right, ∞)
        public static void Tag(RectTransform tile, string text, string kind)
        {
            Color fg = kind == "boss" ? C("ember") : kind == "on" ? C("accent") : Color.white, bg = kind == "boss" ? C("emberSoft") : kind == "on" ? C("accentSoft") : C("raised2");
            var t = JatifyLook.Text(tile, text, kind == "inf" ? "bold" : "mono", kind == "inf" ? 12f : 9f, fg, TextAlignmentOptions.Center);
            float w = Width(t) + 8f, top = kind == "inf" ? 60f - 6f - 15f : 6f;
            var b = Box(tile, "Tag", bg, NoColor, 4); b.raycastTarget = false; Place(b.rectTransform, tile.sizeDelta.x - 7f - w, top, w, 15f);
            t.transform.SetParent(b.transform, false); Fill(t.rectTransform);
        }

        // segmented buttons (Stars, Count, Amount; the World sliders): green when on, or white when quiet; returns its width
        public static float Seg(Transform parent, float x, float y, string[] labels, int on, Action<int> pick, bool quiet = false)
        {
            var box = Box(parent, "Seg", C("raised"), C("glassLine"), 8); box.raycastTarget = false;
            float bx = 2f;
            for (int i = 0; i < labels.Length; i++)
            {
                int k = i; bool sel = i == on;
                var label = JatifyLook.Text(null, labels[i], "medium", 13f, sel ? (quiet ? new Color(0.07f, 0.07f, 0.07f) : Ink) : C("dim"), TextAlignmentOptions.Center);
                float w = Mathf.Max(32f, Width(label) + 20f);
                var b = Box(box.transform, "B", sel ? (quiet ? Color.white : C("accent")) : NoColor, NoColor, 6); Place(b.rectTransform, bx, 2f, w, 26f);
                label.transform.SetParent(b.transform, false); Fill(label.rectTransform);
                On(b, () => pick(k));
                bx += w + 2f;
            }
            Place(box.rectTransform, x, y, bx, 30f);
            return bx;
        }

        // an on / off switch (38 x 22)
        public static void Switch(Transform parent, float x, float y, bool on, Action<bool> set)
        {
            var track = Box(parent, "Switch", on ? C("accent") : C("raised2"), on ? C("accent") : C("glassLine"), 11); Place(track.rectTransform, x, y, 38f, 22f);
            var knob = Box(track.transform, "Knob", on ? Ink : C("dim"), NoColor, 8); knob.raycastTarget = false; Place(knob.rectTransform, on ? 19f : 3f, 3f, 16f, 16f);
            On(track, () => set(!on));
        }

        // the main action (green) and the quieter one (raised, outlined)
        public static Image Primary(Transform parent, string label, float x, float y, float w, float h, Action a, bool enabled = true)
        {
            var b = Box(parent, "Primary", enabled ? C("accent") : C("raised2"), NoColor, 10); Place(b.rectTransform, x, y, w, h);
            Fill(Text(b.transform, label, "bold", 14f, enabled ? Ink : C("mute"), 0f, 0f, w, h, TextAlignmentOptions.Center).rectTransform);
            if (enabled) On(b, a);
            return b;
        }
        public static Image Secondary(Transform parent, string label, float x, float y, float w, float h, Action a, bool enabled = true)
        {
            var b = Box(parent, "Secondary", C("raised"), C("glassLine"), 9); Place(b.rectTransform, x, y, w, h);
            Fill(Text(b.transform, label, "medium", 13f, enabled ? Color.white : C("mute"), 0f, 0f, w, h, TextAlignmentOptions.Center).rectTransform);
            if (enabled) On(b, a);
            return b;
        }

        // label / value rows (the inspector); returns the height used
        public static float Meta(Transform parent, float x, float y, float w, params (string k, string v)[] rows)
        {
            float yy = y;
            foreach (var (k, v) in rows)
            {
                Text(parent, k, "medium", 13f, C("mute"), x, yy, w * 0.45f, 22f);
                Text(parent, v, "medium", 13f, C("dim"), x + w * 0.35f, yy, w * 0.65f, 22f, TextAlignmentOptions.MidlineRight);
                yy += 25f;
            }
            return yy - y;
        }

        // a key hint (the footer): keycap, then what it does; returns the x after it
        public static float Key(Transform parent, string key, string what, float x, float h)
        {
            var kt = JatifyLook.Text(null, key, "mono", 11f, C("dim"), TextAlignmentOptions.Center); float kw = Width(kt) + 12f;
            var cap = Box(parent, "Key", C("raised2"), C("glassLine"), 5); cap.raycastTarget = false; Place(cap.rectTransform, x, (h - 20f) / 2f, kw, 20f);
            kt.transform.SetParent(cap.transform, false); Fill(kt.rectTransform);
            if (string.IsNullOrEmpty(what)) return x + kw + 18f;
            var t = Text(parent, what, "medium", 12.5f, C("mute"), x + kw + 7f, 0f, 300f, h); float tw = Width(t); t.rectTransform.sizeDelta = new Vector2(tw + 2f, h);
            return x + kw + 7f + tw + 18f;
        }

        // a section label in mono caps
        public static void Section(Transform parent, string text, float x, float y, float w)
        {
            var t = Text(parent, text.ToUpperInvariant(), "mono", 11f, C("mute"), x, y, w, 18f); t.characterSpacing = 8f;
        }
    }
}
