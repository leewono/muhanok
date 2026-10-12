using System;

namespace Muhanok.Domain
{
    // Runtime limits for the authored map chunks, applied without editing the chunk prefabs.
    [Serializable]
    public sealed class RenderBudget
    {
        // Metres of track laid out ahead of the runner. 96 suited the old 17 m/s cap; intervals top out near 9.5 m/s.
        public float LookAhead = 72f;
        // Props whose largest dimension is below this are "small": no shadow casting and distance-culled.
        public float SmallPropSize = 1.2f;
        public bool SmallPropShadows = false;
        // Small props beyond this distance ahead are hidden; lights outside [-LightBehind, LightAhead] are switched off.
        public float DetailDistance = 45f;
        public float LightAhead = 55f, LightBehind = 12f;
    }
}
