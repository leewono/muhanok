using System;
using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation
{
    internal sealed class ResultScreen
    {
        private readonly HudSkin skin;
        private readonly CachedText distance = new CachedText(), score = new CachedText(), bestDistance = new CachedText(), bestScore = new CachedText(), kcal = new CachedText();
        public ResultScreen(HudSkin hudSkin) { skin = hudSkin; }
        public void Draw(RunSession session, float best, int bestPoints, Action retry)
        {
            skin.Card(new Rect(355, 112, 570, 520), new Color(.05f, .07f, .1f, .93f));
            GUI.color = new Color(1f, .2f, .1f); GUI.Label(new Rect(417, 151, 510, 90), "GAME OVER", skin.Style(48, FontStyle.Bold));
            GUI.color = Color.white;
            GUI.Label(new Rect(408, 280, 510, 40), distance.Get(Mathf.RoundToInt(session.Score.Distance), value => "Distance          " + value.ToString("N0") + " m"), skin.Label);
            GUI.Label(new Rect(408, 332, 510, 40), score.Get(session.Score.Score, value => "Score               " + value.ToString("N0")), skin.Label);
            GUI.Label(new Rect(408, 388, 510, 40), bestDistance.Get(Mathf.RoundToInt(best), value => "Best Distance   " + value.ToString("N0") + " m"), skin.Small);
            GUI.Label(new Rect(408, 434, 510, 40), bestScore.Get(bestPoints, value => "Best Score        " + value.ToString("N0")), skin.Small);
            if (session.Workout.WeightKg.HasValue)
                GUI.Label(new Rect(408, 477, 510, 35), kcal.Get((long)Math.Round(session.Workout.EstimatedKcal * 10), value => "예상 소모  " + (value / 10.0).ToString("F1") + " kcal"), skin.Small);
            if (GUI.Button(new Rect(423, 528, 435, 61), "다시 하기", skin.Button)) retry();
        }
    }
}
