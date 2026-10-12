using System;
using System.Collections.Generic;

namespace Muhanok.Domain
{
    // Chunk order for one run. The first lap keeps the authored order; later laps form intervals:
    // a mine-cart rest followed by a sprint whose length follows the configured sprint time.
    // Height rules: 0-3 ground, 7 climbs, 4/5/6 stay up, 11 descends, 12 ground.
    public sealed class RoutePlan
    {
        public static readonly int[] AuthoredLap = { 0, 1, 2, 3, 7, 4, 5, 6, 11, 12 };
        private readonly List<int> types = new List<int>(64);
        private readonly List<int> cycles = new List<int>(64);
        private readonly Random random;
        private readonly RunConfig config;
        private readonly bool interval;
        private int cycle;
        public RoutePlan(RunConfig tuning, int seed)
        {
            config = tuning; interval = tuning != null && tuning.IntervalMode; random = new Random(seed);
            Append(AuthoredLap);
        }
        public static RoutePlan Authored() => new RoutePlan(null, 0);
        public int TypeAt(int sequence) { Ensure(sequence); return types[Math.Max(0, sequence)]; }
        public int CycleAt(int sequence) { Ensure(sequence); return cycles[Math.Max(0, sequence)]; }
        private void Ensure(int sequence) { while (Math.Max(0, sequence) >= types.Count) AppendLap(); }
        private void Append(IEnumerable<int> lap) { foreach (int type in lap) { types.Add(type); cycles.Add(cycle); } cycle++; }
        private void AppendLap()
        {
            if (!interval) { Append(AuthoredLap); return; }
            var lap = new List<int>(24) { 0 };
            int rest = Math.Max(2, (int)Math.Round(config.RestSeconds * config.RestSpeed / RouteSurface.Length));
            for (int i = 0; i < rest; i++) lap.Add(i % 2 == 0 ? 1 : 2);
            float sprintSpeed = Math.Min(config.MaxSprintSpeed, config.SprintSpeed + config.SprintSpeedStep * cycle);
            // Fixed sprint chunks: transition, stairs, checkpoint, descent and the next mine start.
            int flexible = Math.Max(2, (int)Math.Round(config.SprintSeconds * sprintSpeed / RouteSurface.Length) - 5);
            int zoo = Math.Min(3, Math.Max(1, flexible / 3));
            int prison = Math.Max(1, flexible - zoo);
            lap.Add(3); lap.Add(7);
            // Alternate the two corridor variants so no type repeats more than the chunk pool holds.
            int first = random.Next(2);
            for (int i = 0; i < prison; i++) lap.Add(4 + (first + i) % 2);
            lap.Add(6); lap.Add(11);
            for (int i = 0; i < zoo; i++) lap.Add(12);
            Append(lap);
        }
    }
}
