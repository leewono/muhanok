using System;
using System.Collections.Generic;
using Muhanok.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace Muhanok.Presentation
{
    // What the settings window reads and changes in the game.
    public interface ISettingsHost
    {
        UserSettings Settings { get; }
        UserSettings Defaults { get; }
        IReadOnlyList<string> CameraDevices { get; }
        IHudData Hud { get; }
        void ApplySettings(bool save);
    }

    // UI Toolkit settings window. Layout and look live in Resources/MuhanokUI/SettingsWindow.uxml/.uss so the
    // UI/UX team can restyle it in UI Builder; this script only finds controls by name and binds them.
    public sealed class SettingsWindow : MonoBehaviour
    {
        private const string Folder = "MuhanokUI/";
        private static readonly string[] Presets = { "높음", "보통", "저사양 (전시용 노트북)", "사용자 지정" };
        private static readonly string[] FrameCaps = { "30 fps", "60 fps", "제한 없음" };
        private static readonly string[] ShadowNames = { "끄기", "낮음", "높음" };
        private static readonly string[] Pages = { "graphics", "sound", "body", "display" };
        private ISettingsHost host;
        private UIDocument document;
        private VisualElement root;
        private Image preview;
        private Label status;
        private Font font;
        private bool syncing;
        public bool IsOpen { get; private set; }
        public event Action Closed;

        public void Configure(ISettingsHost settingsHost)
        {
            host = settingsHost;
            var tree = Resources.Load<VisualTreeAsset>(Folder + "SettingsWindow");
            if (tree == null) { Debug.LogError("Muhanok: Resources/MuhanokUI/SettingsWindow.uxml is missing."); return; }
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.name = "Muhanok Settings Panel";
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>(Folder + "MuhanokRuntimeTheme");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = .5f;
            panel.sortingOrder = 100;
            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = tree;
            root = document.rootVisualElement;
            // Korean glyphs: default to the Windows Korean font unless the USS sets its own font.
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24);
            root.style.unityFontDefinition = FontDefinition.FromFont(font);
            root.style.position = Position.Absolute; root.style.left = 0; root.style.top = 0; root.style.right = 0; root.style.bottom = 0;
            Bind();
            SetVisible(false);
        }

        private T Find<T>(string name) where T : VisualElement
        {
            var element = root.Q<T>(name);
            if (element == null) Debug.LogWarning("Muhanok settings: element '" + name + "' was not found in SettingsWindow.uxml.");
            return element;
        }

        private void Bind()
        {
            var s = host.Settings;
            Dropdown("quality-preset", Presets, () => (int)s.Preset, i => { if (i < 3) s.ApplyPreset((QualityPreset)i); else s.Preset = QualityPreset.Custom; Refresh(); });
            SliderBind("render-scale", () => s.RenderScale, v => { s.RenderScale = v; Custom(); });
            Dropdown("frame-cap", FrameCaps, () => Math.Max(0, Array.IndexOf(UserSettings.FrameCaps, s.FrameCap)), i => { s.FrameCap = UserSettings.FrameCaps[i]; Custom(); });
            ToggleBind("vsync", () => s.VSync, v => s.VSync = v);
            Dropdown("shadow-quality", ShadowNames, () => (int)s.Shadows, i => { s.Shadows = (ShadowSetting)i; Custom(); });
            ToggleBind("ambient-occlusion", () => s.AmbientOcclusion, v => { s.AmbientOcclusion = v; Custom(); });
            ToggleBind("gpu-resident-drawer", () => s.GpuResidentDrawer, v => s.GpuResidentDrawer = v);
            SliderBind("detail-distance", () => s.DetailDistance, v => { s.DetailDistance = v; Custom(); });
            ToggleBind("fullscreen", () => s.Fullscreen, v => s.Fullscreen = v);
            SliderBind("music-volume", () => s.MusicVolume, v => s.MusicVolume = v);
            SliderBind("effects-volume", () => s.EffectsVolume, v => s.EffectsVolume = v);
            ToggleBind("lite-model", () => s.LiteLandmarkModel, v => s.LiteLandmarkModel = v);
            SliderBind("lane-sensitivity", () => s.LaneSensitivity, v => s.LaneSensitivity = v);
            ToggleBind("performance-overlay", () => s.PerformanceOverlay, v => s.PerformanceOverlay = v);
            ToggleBind("pose-diagnostics", () => s.PoseDiagnostics, v => s.PoseDiagnostics = v);
            var camera = Find<DropdownField>("camera-device");
            if (camera != null) camera.RegisterValueChangedCallback(e => { if (syncing) return; s.CameraDevice = camera.index <= 0 ? "" : e.newValue; host.ApplySettings(false); });
            for (int i = 0; i < Pages.Length; i++) { string page = Pages[i]; var tab = Find<Button>("tab-" + page); if (tab != null) tab.clicked += () => ShowPage(page); }
            Clicked("close-button", () => Close(true));
            Clicked("apply-button", () => Close(true));
            Clicked("reset-button", () => { CopyInto(host.Defaults, s); host.ApplySettings(false); Refresh(); });
            Clicked("recalibrate-button", () => host.Hud?.Recalibrate());
            var slot = Find<VisualElement>("camera-preview");
            if (slot != null)
            {
                // Mirrored like the in-game preview.
                preview = new Image { scaleMode = ScaleMode.ScaleToFit, uv = new Rect(1, 0, -1, 1) };
                preview.style.flexGrow = 1; slot.Add(preview);
            }
            status = Find<Label>("pose-status");
            ShowPage(Pages[0]);
        }

        private void Custom() { if (!syncing) { host.Settings.Preset = QualityPreset.Custom; Refresh(); } }
        private void Clicked(string name, Action action) { var button = Find<Button>(name); if (button != null) button.clicked += action; }
        private void Dropdown(string name, string[] choices, Func<int> read, Action<int> write)
        {
            var field = Find<DropdownField>(name); if (field == null) return;
            field.choices = new List<string>(choices);
            field.RegisterValueChangedCallback(e => { if (syncing || field.index < 0) return; write(field.index); host.ApplySettings(false); });
            field.userData = read;
        }
        private void SliderBind(string name, Func<float> read, Action<float> write)
        {
            var field = Find<Slider>(name); if (field == null) return;
            field.RegisterValueChangedCallback(e => { if (syncing) return; write(e.newValue); host.ApplySettings(false); });
            field.userData = read;
        }
        private void ToggleBind(string name, Func<bool> read, Action<bool> write)
        {
            var field = Find<Toggle>(name); if (field == null) return;
            field.RegisterValueChangedCallback(e => { if (syncing) return; write(e.newValue); host.ApplySettings(false); });
            field.userData = read;
        }

        // Pushes the current settings into every control without firing change callbacks.
        private void Refresh()
        {
            if (root == null) return;
            syncing = true;
            root.Query<DropdownField>().ForEach(f => { if (f.userData is Func<int> read) f.index = read(); });
            root.Query<Slider>().ForEach(f => { if (f.userData is Func<float> read) f.SetValueWithoutNotify(read()); });
            root.Query<Toggle>().ForEach(f => { if (f.userData is Func<bool> read) f.SetValueWithoutNotify(read()); });
            var camera = root.Q<DropdownField>("camera-device");
            if (camera != null)
            {
                var choices = new List<string> { "자동 선택" }; choices.AddRange(host.CameraDevices);
                camera.choices = choices;
                int index = choices.IndexOf(host.Settings.CameraDevice);
                camera.index = index < 0 ? 0 : index;
            }
            syncing = false;
        }

        private void ShowPage(string page)
        {
            foreach (var name in Pages)
            {
                var element = root.Q<VisualElement>("page-" + name);
                if (element != null) element.style.display = name == page ? DisplayStyle.Flex : DisplayStyle.None;
                var tab = root.Q<Button>("tab-" + name);
                if (tab != null) tab.EnableInClassList("settings-tab--selected", name == page);
            }
        }

        public void Open()
        {
            if (root == null) return;
            Refresh(); SetVisible(true); IsOpen = true;
        }
        public void Close(bool save)
        {
            if (!IsOpen) return;
            host.ApplySettings(save); SetVisible(false); IsOpen = false; Closed?.Invoke();
        }
        private void SetVisible(bool visible) { if (root != null) root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None; }

        private void Update()
        {
            if (!IsOpen || host?.Hud == null) return;
            if (preview != null && preview.image != host.Hud.CameraTexture) preview.image = host.Hud.CameraTexture;
            if (status != null)
            {
                var hud = host.Hud; string text = PoseStatusText.Korean(hud.PoseStatus);
                if (hud.PoseStatus == PoseStatus.NoCamera || hud.PoseStatus == PoseStatus.NoModel) text = hud.PoseMessage;
                if (status.text != text) status.text = text;
            }
        }

        private static void CopyInto(UserSettings from, UserSettings to) => JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(from), to);
        private void OnDestroy() { if (font != null) Destroy(font); }
    }
}
