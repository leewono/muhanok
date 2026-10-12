using System;
using System.Collections.Generic;
using Muhanok.Domain;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Muhanok.Presentation
{
    // Applies graphics options at runtime on a copy of the URP asset, so play mode never edits project assets.
    // Everything is restored on Dispose.
    public sealed class GraphicsSettingsApplier : IDisposable
    {
        private readonly RenderPipelineAsset original;
        private readonly UniversalRenderPipelineAsset working;
        private readonly float baseShadowDistance;
        private readonly List<ScriptableRendererFeature> ambientOcclusion = new List<ScriptableRendererFeature>();
        private readonly List<bool> ambientOcclusionWas = new List<bool>();
        private readonly Dictionary<Light, LightShadows> lightShadows = new Dictionary<Light, LightShadows>();

        public GraphicsSettingsApplier()
        {
            original = QualitySettings.renderPipeline;
            var source = (original != null ? original : GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if (source == null) return;
            working = UnityEngine.Object.Instantiate(source);
            working.name = source.name + " (Runtime)";
            baseShadowDistance = source.shadowDistance;
            QualitySettings.renderPipeline = working;
            // SSAO is a feature on the shared renderer data; remember its state and put it back on exit.
            foreach (var data in working.rendererDataList)
            {
                if (data == null) continue;
                foreach (var feature in data.rendererFeatures)
                    if (feature is ScreenSpaceAmbientOcclusion) { ambientOcclusion.Add(feature); ambientOcclusionWas.Add(feature.isActive); }
            }
        }

        public void Apply(UserSettings s)
        {
            UnityEngine.Application.targetFrameRate = s.FrameCap > 0 ? s.FrameCap : -1;
            QualitySettings.vSyncCount = s.VSync ? 1 : 0;
            if (!UnityEngine.Application.isEditor && Screen.fullScreen != s.Fullscreen) Screen.fullScreen = s.Fullscreen;
            if (working != null)
            {
                // Setting the mode re-validates the asset and URP rebuilds the pipeline with or without the drawer.
                working.gpuResidentDrawerMode = s.GpuResidentDrawer ? GPUResidentDrawerMode.InstancedDrawing : GPUResidentDrawerMode.Disabled;
                working.renderScale = s.RenderScale;
                // Below native resolution, FSR upscaling keeps edges sharper than plain bilinear.
                working.upscalingFilter = s.RenderScale < .99f ? UpscalingFilterSelection.FSR : UpscalingFilterSelection.Auto;
                working.shadowDistance = s.Shadows == ShadowSetting.Off ? 0f : s.Shadows == ShadowSetting.Low ? Mathf.Min(baseShadowDistance, 25f) : baseShadowDistance;
                working.mainLightShadowmapResolution = s.Shadows == ShadowSetting.High ? 2048 : 1024;
                working.additionalLightsShadowmapResolution = s.Shadows == ShadowSetting.High ? 2048 : 512;
            }
            for (int i = 0; i < ambientOcclusion.Count; i++) if (ambientOcclusion[i] != null) ambientOcclusion[i].SetActive(s.AmbientOcclusion && ambientOcclusionWas[i]);
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                if (!lightShadows.ContainsKey(light)) lightShadows[light] = light.shadows;
                var authored = lightShadows[light];
                light.shadows = authored == LightShadows.None || s.Shadows == ShadowSetting.Off ? LightShadows.None : s.Shadows == ShadowSetting.Low ? LightShadows.Hard : authored;
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < ambientOcclusion.Count; i++) if (ambientOcclusion[i] != null) ambientOcclusion[i].SetActive(ambientOcclusionWas[i]);
            foreach (var pair in lightShadows) if (pair.Key != null) pair.Key.shadows = pair.Value;
            if (working == null) return;
            QualitySettings.renderPipeline = original;
            UnityEngine.Object.Destroy(working);
        }
    }
}
