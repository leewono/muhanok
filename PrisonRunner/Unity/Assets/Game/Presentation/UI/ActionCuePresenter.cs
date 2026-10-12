using Muhanok.Domain;
using Muhanok.Presentation.Map;
using UnityEngine;

namespace Muhanok.Presentation
{
    // Picks the instruction for the nearest obstacle the runner must answer next.
    public static class ActionCuePresenter
    {
        public static string Find(RunSession session, ChunkSpawner map)
        {
            if (map == null || session.Paused) return null;
            float nearest = float.MaxValue; string cue = null;
            foreach (var active in map.Active)
            {
                if (session.Runner.Riding)
                {
                    foreach (var gap in active.Chunk.RailGaps)
                    {
                        float d = gap.transform.position.z - session.Runner.Z;
                        if (gap.Passed || d < 0 || d > 14 || d >= nearest) continue;
                        nearest = d; cue = gap.RequiredLean < 0 ? "←  왼쪽으로 기울이세요" : "오른쪽으로 기울이세요  →";
                    }
                }
                else
                {
                    Consider(session, active.Chunk.Obstacles, ref nearest, ref cue);
                    Consider(session, active.Chunk.ExerciseObstacles, ref nearest, ref cue);
                    Consider(session, active.Chunk.DynamicObstacles, ref nearest, ref cue);
                }
            }
            return cue;
        }
        private static void Consider(RunSession session, System.Collections.Generic.IEnumerable<ObstacleController> hazards, ref float nearest, ref string cue)
        {
            if (hazards == null) return;
            float lookAhead = Mathf.Max(9f, session.TargetSpeed * 1.35f);
            foreach (var hazard in hazards)
            {
                float d = hazard.transform.position.z - session.Runner.Z;
                if (!hazard.gameObject.activeSelf || hazard.Passed || d < 0 || d > lookAhead || d >= nearest) continue;
                if (!hazard.FullExerciseRow)
                {
                    RouteSurface.Sample(hazard.transform.position.z, out float offset, out _);
                    if (Mathf.Abs(session.Runner.Lane * session.Config.LaneSpacing + offset - hazard.transform.position.x) > .95f) continue;
                }
                nearest = d;
                cue = hazard.GetComponent<ClosingSlideGate>() != null ? "↓  문 아래로 슬라이딩!" : hazard.Kind == ObstacleKind.Pipe ? "↓  슬라이드하세요" : hazard.Kind == ObstacleKind.Stair ? "무릎을 높이 들어 올리세요" : hazard.Kind == ObstacleKind.Crate || hazard.Kind == ObstacleKind.Cart ? "↑  점프하세요" : "←  옆 레인으로 피하세요  →";
            }
        }
    }
}
