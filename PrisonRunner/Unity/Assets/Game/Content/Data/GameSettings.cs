using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Content
{
    [CreateAssetMenu(menuName = "Muhanok/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        public RunConfig Run = new RunConfig();
        [Header("Body tracking (Inference Engine)")]
        public PoseCameraOptions Camera = new PoseCameraOptions();
        public MotionTuning Motion = new MotionTuning();
        [Header("Map rendering budget")]
        public RenderBudget Render = new RenderBudget();
        [Header("Exhibition")]
        [Tooltip("Exhibition build: every item is available without spending the shared coin pool.")]
        public bool UnlockAllItems = true;
        [Range(0, 1)] public float ItemChancePerChunk = .35f;
        [Range(0, 1)] public float MusicVolume = .35f, EffectsVolume = .7f;
    }
}
