using System.Collections.Generic;
using Muhanok.Domain;
using UnityEngine;
using UnityEngine.Rendering;

namespace Muhanok.Presentation.Map
{
    // Per-chunk render limits measured once when the pooled chunk is created:
    // small props stop casting shadows and are hidden far ahead; point lights only run near the runner.
    public sealed class ChunkRenderBudget
    {
        private readonly List<Renderer> smallProps = new List<Renderer>();
        private readonly List<Light> lights = new List<Light>();
        private bool detailVisible = true;
        public int SmallPropCount => smallProps.Count;
        public int LightCount => lights.Count;

        public ChunkRenderBudget(MuhanokMapChunk chunk, RenderBudget budget)
        {
            foreach (var renderer in chunk.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                // Gameplay objects (hazards, coins, floor items, rail gaps) always stay visible.
                if (renderer.GetComponentInParent<ObstacleController>(true) != null || renderer.GetComponentInParent<CoinController>(true) != null
                    || renderer.GetComponentInParent<FloorItemController>(true) != null || renderer.GetComponentInParent<CartRailGap>(true) != null) continue;
                // Mesh bounds times scale works while the pooled chunk is inactive (Renderer.bounds does not).
                var size = Vector3.Scale(filter.sharedMesh.bounds.size, renderer.transform.lossyScale);
                if (Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)) >= budget.SmallPropSize) continue;
                smallProps.Add(renderer);
                if (!budget.SmallPropShadows) renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var light in chunk.GetComponentsInChildren<Light>(true))
                if (light.type == LightType.Point || light.type == LightType.Spot) lights.Add(light);
        }

        public void Apply(float chunkStartZ, float runnerZ, RenderBudget budget, float detailScale)
        {
            bool detail = chunkStartZ - runnerZ < budget.DetailDistance * detailScale;
            if (detail != detailVisible)
            {
                detailVisible = detail;
                foreach (var renderer in smallProps) if (renderer != null) renderer.enabled = detail;
            }
            float ahead = budget.LightAhead * Mathf.Max(.5f, detailScale);
            foreach (var light in lights)
            {
                if (light == null) continue;
                float d = light.transform.position.z - runnerZ;
                bool on = d > -budget.LightBehind && d < ahead;
                if (light.enabled != on) light.enabled = on;
            }
        }
    }
}
