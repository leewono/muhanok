using System.Collections.Generic;
using UnityEngine;

namespace MuhanokOverlay
{
    public static class Palette
    {
        public static readonly Color Ground = new Color32(0x1E, 0x14, 0x0E, 0xFF);
        public static readonly Color Panel = new Color32(0x2B, 0x1D, 0x14, 0xFF);
        public static readonly Color Wood = new Color32(0x6B, 0x44, 0x26, 0xFF);
        public static readonly Color WoodDim = new Color32(0x4A, 0x30, 0x20, 0xFF);
        public static readonly Color Dark = new Color32(0x12, 0x0B, 0x07, 0xFF);
        public static readonly Color Track = new Color32(0x3A, 0x27, 0x1A, 0xFF);
        public static readonly Color Gold = new Color32(0xFF, 0xC8, 0x3D, 0xFF);
        public static readonly Color Cream = new Color32(0xFF, 0xF3, 0xDC, 0xFF);
        public static readonly Color Sand = new Color32(0xE9, 0xD3, 0xB0, 0xFF);
        public static readonly Color Muted = new Color32(0x9C, 0x84, 0x67, 0xFF);
        public static readonly Color Outline = new Color32(0x2A, 0x17, 0x0B, 0xFF);
        public static readonly Color Monkey = new Color32(0x8A, 0x4B, 0x26, 0xFF);
        public static readonly Color Silver = new Color32(0xD8, 0xD8, 0xD8, 0xFF);
        public static readonly Color Bronze = new Color32(0xD9, 0x92, 0x5B, 0xFF);
        public static readonly Color MeFill = new Color32(0x3A, 0x2E, 0x14, 0xFF);
        public static readonly Color MeNote = new Color32(0xFF, 0xD5, 0x8C, 0xFF);

        public static Color A(Color c, float a) { c.a = a; return c; }
        public static string Hex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }
    }

    // 1280x720 virtual screen, letterboxed like the main game
    public static class Screen720
    {
        public static Matrix4x4 Matrix
        {
            get
            {
                float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
                float x = (Screen.width - 1280f * scale) * 0.5f;
                float y = (Screen.height - 720f * scale) * 0.5f;
                return Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, Vector3.one * scale);
            }
        }
    }

    public sealed class OverlaySkin
    {
        readonly Font titleFont, bodyFont, osFont;
        readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
        readonly Dictionary<string, GUIContent> contents = new Dictionary<string, GUIContent>();
        Texture2D bigTex, smallTex, circleTex;
        GUIStyle bigBox, smallBox, circleBox;

        public OverlaySkin(Font title, Font body)
        {
            if (title == null || body == null)
                osFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "AppleGothic", "Arial" }, 32);
            titleFont = title != null ? title : osFont;
            bodyFont = body != null ? body : (title != null ? title : osFont);
        }

        public GUIStyle Style(int size, bool title, TextAnchor anchor, bool bold = false)
        {
            int key = (size * 2 + (title ? 1 : 0)) * 64 + (int)anchor * 2 + (bold ? 1 : 0);
            GUIStyle s;
            if (!styles.TryGetValue(key, out s))
            {
                s = new GUIStyle(GUI.skin.label)
                {
                    font = title ? titleFont : bodyFont,
                    fontSize = size,
                    alignment = anchor,
                    richText = true,
                    wordWrap = false,
                    clipping = TextClipping.Overflow,
                    fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0)
                };
                s.normal.textColor = Color.white;
                styles[key] = s;
            }
            return s;
        }

        // own GUIContent per string; the shared temp content gets mixed up with editor labels in Unity 6
        GUIContent Content(string text)
        {
            if (text == null) text = "";
            GUIContent c;
            if (!contents.TryGetValue(text, out c))
            {
                if (contents.Count > 512) contents.Clear();
                c = new GUIContent(text);
                contents[text] = c;
            }
            return c;
        }

        public void Text(Rect r, string text, int size, Color color, TextAnchor anchor, bool title = true, bool bold = false)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.Label(r, Content(text), Style(size, title, anchor, bold));
            GUI.color = old;
        }

        public void OutlinedText(Rect r, string outlineText, string text, int size, Color color, Color outline, float width, TextAnchor anchor)
        {
            var style = Style(size, true, anchor);
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.45f);
            GUI.Label(new Rect(r.x, r.y + 5, r.width, r.height), Content(outlineText), style);
            GUI.color = outline;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                GUI.Label(new Rect(r.x + Mathf.Cos(a) * width, r.y + Mathf.Sin(a) * width, r.width, r.height), Content(outlineText), style);
            }
            GUI.color = color;
            GUI.Label(r, Content(text), style);
            GUI.color = old;
        }

        public void Box(Rect r, Color color)
        {
            Init();
            var style = Mathf.Min(r.width, r.height) >= 28 ? bigBox : smallBox;
            var old = GUI.color;
            GUI.color = color;
            GUI.Box(r, GUIContent.none, style);
            GUI.color = old;
        }

        public void BorderBox(Rect r, Color fill, Color border, float width)
        {
            Box(r, border);
            Box(new Rect(r.x + width, r.y + width, r.width - width * 2, r.height - width * 2), fill);
        }

        public void Circle(Rect r, Color color)
        {
            Init();
            var old = GUI.color;
            GUI.color = color;
            GUI.Box(r, GUIContent.none, circleBox);
            GUI.color = old;
        }

        public void Fill(Rect r, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public bool Button(Rect r, string label, Color fill, Color border, Color text, int size)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            if (border.a > 0) BorderBox(r, hover ? Color.Lerp(fill, Color.white, 0.12f) : fill, border, 2);
            else Box(r, hover ? Color.Lerp(fill, Color.white, 0.15f) : fill);
            Text(r, label, size, text, TextAnchor.MiddleCenter);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        void Init()
        {
            if (bigBox != null) return;
            bigTex = Rounded(48, 12);
            smallTex = Rounded(24, 6);
            circleTex = Rounded(64, 32);
            bigBox = BoxStyle(bigTex, 12);
            smallBox = BoxStyle(smallTex, 6);
            circleBox = BoxStyle(circleTex, 0);
        }

        static GUIStyle BoxStyle(Texture2D tex, int border)
        {
            var s = new GUIStyle();
            s.normal.background = tex;
            s.border = new RectOffset(border, border, border, border);
            return s;
        }

        static Texture2D Rounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float cx = Mathf.Clamp(fx, radius, size - radius);
                    float cy = Mathf.Clamp(fy, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(fx, fy), new Vector2(cx, cy));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius - d + 0.5f)));
                }
            }
            tex.Apply();
            return tex;
        }

        public void Dispose()
        {
            if (osFont != null) Object.Destroy(osFont);
            if (bigTex != null) Object.Destroy(bigTex);
            if (smallTex != null) Object.Destroy(smallTex);
            if (circleTex != null) Object.Destroy(circleTex);
        }
    }
}
