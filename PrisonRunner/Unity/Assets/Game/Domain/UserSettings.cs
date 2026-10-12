using System;

namespace Muhanok.Domain
{
    public enum QualityPreset { High, Balanced, Low, Custom }
    public enum ShadowSetting { Off, Low, High }

    // Player/operator options from the settings window. Saved on this PC; applied by the presentation layer.
    [Serializable]
    public sealed class UserSettings
    {
        public QualityPreset Preset = QualityPreset.High;
        public float RenderScale = 1f;
        public int FrameCap = 60;          // 0 = unlimited
        public bool VSync = false;
        public ShadowSetting Shadows = ShadowSetting.High;
        public bool AmbientOcclusion = true;
        public bool Fullscreen = true;
        public float MusicVolume = .35f, EffectsVolume = .7f;
        public string CameraDevice = "";
        public bool LiteLandmarkModel = false;
        public float LaneSensitivity = 1f; // >1 needs less body movement
        public bool PerformanceOverlay = false;
        public bool PoseDiagnostics = true;

        public static readonly int[] FrameCaps = { 30, 60, 0 };

        // Presets only touch graphics; sound, camera and display options keep their values.
        public void ApplyPreset(QualityPreset preset)
        {
            Preset = preset;
            if (preset == QualityPreset.High) { RenderScale = 1f; Shadows = ShadowSetting.High; AmbientOcclusion = true; FrameCap = 60; }
            else if (preset == QualityPreset.Balanced) { RenderScale = .85f; Shadows = ShadowSetting.Low; AmbientOcclusion = false; FrameCap = 60; }
            else if (preset == QualityPreset.Low) { RenderScale = .7f; Shadows = ShadowSetting.Off; AmbientOcclusion = false; FrameCap = 30; }
        }
        public void Clamp()
        {
            RenderScale = Math.Max(.5f, Math.Min(1f, RenderScale));
            MusicVolume = Math.Max(0, Math.Min(1, MusicVolume)); EffectsVolume = Math.Max(0, Math.Min(1, EffectsVolume));
            LaneSensitivity = Math.Max(.6f, Math.Min(1.4f, LaneSensitivity));
            if (Array.IndexOf(FrameCaps, FrameCap) < 0) FrameCap = 60;
            CameraDevice = CameraDevice ?? "";
        }
        public UserSettings Clone() => (UserSettings)MemberwiseClone();
    }
}
