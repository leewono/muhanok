using System.Collections.Generic;
using UnityEngine;

namespace Muhanok.Presentation
{
    // Shared IMGUI resources. Styles are created once inside OnGUI and reused every frame.
    internal sealed class HudSkin
    {
        public readonly Font Font;
        private readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
        private readonly Dictionary<int, GUIStyle> centeredStyles = new Dictionary<int, GUIStyle>();
        private GUIStyle button, flatButton, panelStyle, inputStyle;
        private Texture2D panelTexture, coinIcon, distanceIcon;
        public HudSkin() { Font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 28); }
        public GUIStyle Large => Style(74, FontStyle.Bold);
        public GUIStyle Small => Style(20, FontStyle.Normal);
        public GUIStyle Label => Style(25, FontStyle.Bold);
        public GUIStyle Button => button ??= new GUIStyle(GUI.skin.button) { font = Font, fontSize = 22 };
        public GUIStyle Input => inputStyle ??= new GUIStyle(GUI.skin.textField) { font = Font, fontSize = 30, alignment = TextAnchor.MiddleLeft };
        public Texture2D CoinIcon => coinIcon ??= CreateHudIcon(false);
        public Texture2D DistanceIcon => distanceIcon ??= CreateHudIcon(true);
        public GUIStyle Style(int size, FontStyle weight)
        {
            int key = size * 4 + (int)weight;
            if (!styles.TryGetValue(key, out var style))
                styles[key] = style = new GUIStyle(GUI.skin.label) { font = Font, fontSize = size, fontStyle = weight, normal = { textColor = Color.white } };
            return style;
        }
        public GUIStyle Centered(int size, FontStyle weight)
        {
            int key = size * 4 + (int)weight;
            if (!centeredStyles.TryGetValue(key, out var style))
                centeredStyles[key] = style = new GUIStyle(Style(size, weight)) { alignment = TextAnchor.MiddleCenter };
            return style;
        }
        public void Card(Rect r, Color color)
        {
            if (panelStyle == null)
            {
                panelTexture = new Texture2D(48, 48, TextureFormat.RGBA32, false); panelTexture.hideFlags = HideFlags.DontSave;
                panelTexture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
                {
                    float dx = Mathf.Max(12 - x, x - 35), dy = Mathf.Max(12 - y, y - 35);
                    float distance = Mathf.Sqrt(Mathf.Max(0, dx) * Mathf.Max(0, dx) + Mathf.Max(0, dy) * Mathf.Max(0, dy));
                    float alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(11, 12, distance));
                    panelTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                panelTexture.Apply(); panelStyle = new GUIStyle(); panelStyle.normal.background = panelTexture; panelStyle.border = new RectOffset(12, 12, 12, 12);
            }
            var old = GUI.color; GUI.color = color; GUI.Box(r, GUIContent.none, panelStyle); GUI.color = old;
        }
        // Main call to action: saturated orange, white bold text with a shadow, brighter on hover and pulsing gently.
        public bool PrimaryButton(Rect rect, string text, int size = 28)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 3f);
            Card(new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6), new Color(1f, .85f, .45f, .55f + .35f * pulse));
            Card(rect, hover ? new Color(1f, .6f, .1f) : new Color(1f, .48f, .02f));
            Card(new Rect(rect.x + 4, rect.y + 3, rect.width - 8, rect.height * .42f), new Color(1f, 1f, 1f, .18f));
            var label = Centered(size, FontStyle.Bold);
            GUI.color = new Color(.35f, .12f, 0f, .8f); GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, label);
            GUI.color = Color.white; GUI.Label(rect, text, label);
            bool pressed = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.color = Color.white; return pressed;
        }
        public bool FlatButton(Rect rect, string text, bool primary)
        {
            if (flatButton == null)
            {
                flatButton = new GUIStyle(Button);
                foreach (var state in new[] { flatButton.normal, flatButton.hover, flatButton.active, flatButton.focused }) state.background = null;
            }
            GUI.color = primary ? new Color(1, .76f, .23f) : new Color(.12f, .42f, .67f);
            Card(rect, GUI.color);
            GUI.color = primary ? new Color(.15f, .09f, .025f) : Color.white;
            bool pressed = GUI.Button(rect, text, flatButton); GUI.color = Color.white; return pressed;
        }
        public void Dispose()
        {
            if (Font != null) Object.Destroy(Font);
            if (panelTexture != null) Object.Destroy(panelTexture);
            if (coinIcon != null) Object.Destroy(coinIcon);
            if (distanceIcon != null) Object.Destroy(distanceIcon);
        }
        private static Texture2D CreateHudIcon(bool crown)
        {
            var image = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f, radius = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(29, 30, radius));
                Color color = radius > 25 ? new Color(1, .87f, .30f) : radius > 22 ? new Color(.72f, .40f, .035f) : new Color(1, .71f, .055f);
                if (radius < 17) color = new Color(1, .78f, .10f);
                if (radius > 16 && radius < 18) color = new Color(1, .91f, .43f);
                if (crown)
                {
                    float top = 46 - Mathf.Min(Mathf.Abs(x - 13), Mathf.Min(Mathf.Abs(x - 32), Mathf.Abs(x - 51))) * 1.55f;
                    alpha = x >= 10 && x <= 53 && y >= 12 && y <= top ? 1 : 0;
                    color = y < 18 ? new Color(.85f, .49f, .03f) : new Color(1, .78f, .11f);
                    if ((x - 32) * (x - 32) + (y - 23) * (y - 23) < 12) color = new Color(1, .94f, .62f);
                }
                color.a = alpha; image.SetPixel(x, y, color);
            }
            image.Apply(); return image;
        }
    }
    // Rebuilds a label only when the displayed value changes.
    internal sealed class CachedText
    {
        private long key = long.MinValue;
        private string text = "";
        public string Get(long value, System.Func<long, string> format)
        {
            if (value != key) { key = value; text = format(value); }
            return text;
        }
    }
}
