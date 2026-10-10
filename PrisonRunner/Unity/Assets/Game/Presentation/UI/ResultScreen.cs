using System;
using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation
{
    // Result: the panel slides in, then today's ranking rolls in row by row and the visitor's row pulses.
    internal sealed class ResultScreen
    {
        private const int Rows = 7;
        private readonly HudSkin skin;
        private readonly CachedText distance = new CachedText(), score = new CachedText(), kcal = new CachedText(), rank = new CachedText(), pool = new CachedText();
        private readonly string[] rowText = new string[Rows];
        private readonly RankingEntry[] rowSource = new RankingEntry[Rows];
        private float shownAt = -1;
        public ResultScreen(HudSkin hudSkin) { skin = hudSkin; }
        public void Hide() { shownAt = -1; }
        public void Draw(RunSession session, IHudData data, Action retry, Action nextVisitor)
        {
            if (shownAt < 0) shownAt = Time.unscaledTime;
            float t = Time.unscaledTime - shownAt;
            float slide = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .45f));
            var panel = new Rect(110 - slide * 700, 92, 470, 540);
            skin.Card(panel, new Color(.05f, .07f, .1f, .94f));
            GUI.color = new Color(1f, .2f, .1f); GUI.Label(new Rect(panel.x + 40, 118, 400, 70), "GAME OVER", skin.Style(48, FontStyle.Bold));
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 40, 200, 400, 40), distance.Get(Mathf.RoundToInt(session.Score.Distance), v => "거리      " + v.ToString("N0") + " m"), skin.Label);
            GUI.Label(new Rect(panel.x + 40, 246, 400, 40), score.Get(session.Score.Score, v => "점수      " + v.ToString("N0")), skin.Label);
            int myRank = data?.LastRank ?? 0;
            GUI.color = new Color(1, .82f, .3f);
            GUI.Label(new Rect(panel.x + 40, 298, 400, 50), rank.Get(myRank, v => v > 0 ? "오늘 " + v + "위!" : "오늘의 기록"), skin.Style(36, FontStyle.Bold));
            GUI.color = Color.white;
            if (session.Workout.WeightKg.HasValue)
                GUI.Label(new Rect(panel.x + 40, 356, 400, 34), kcal.Get((long)Math.Round(session.Workout.EstimatedKcal * 10), v => "예상 소모  " + (v / 10.0).ToString("F1") + " kcal"), skin.Small);
            if (data?.Progress != null && !data.Progress.UnlockAll)
                GUI.Label(new Rect(panel.x + 40, 392, 420, 34), pool.Get(data.Progress.CoinPool * 1000L + session.Score.Coins, v => "코인 " + (v % 1000) + "개 적립 · 전시 코인 풀 " + (v / 1000) + "개"), skin.Small);
            if (GUI.Button(new Rect(panel.x + 40, 470, 390, 58), "다시 하기", skin.Button)) { Hide(); retry?.Invoke(); }
            if (skin.FlatButton(new Rect(panel.x + 40, 540, 390, 48), "다음 관람객", false)) { Hide(); nextVisitor?.Invoke(); }
            DrawRanking(data, t);
        }
        private void DrawRanking(IHudData data, float t)
        {
            var today = data?.Today;
            float appear = Mathf.Clamp01((t - .35f) / .3f);
            var board = new Rect(620 + (1 - appear) * 500, 92, 560, 540);
            skin.Card(board, new Color(.05f, .07f, .1f, .94f));
            GUI.color = new Color(1, .78f, .32f); GUI.Label(new Rect(board.x + 30, 112, 500, 50), "오늘의 탈옥 순위", skin.Style(34, FontStyle.Bold)); GUI.color = Color.white;
            if (today == null) return;
            int count = Mathf.Min(Rows, today.Entries.Count);
            for (int i = 0; i < count; i++)
            {
                float rowTime = t - .7f - i * .12f;
                if (rowTime < 0) break;
                float enter = Mathf.SmoothStep(0, 1, Mathf.Clamp01(rowTime / .25f));
                var entry = today.Entries[i];
                bool mine = ReferenceEquals(entry, data.LastEntry);
                var row = new Rect(board.x + 24 + (1 - enter) * 300, 176 + i * 54, 512, 48);
                float pulse = mine ? .5f + .5f * Mathf.Sin(Time.unscaledTime * 6) : 0;
                skin.Card(row, mine ? Color.Lerp(new Color(.55f, .4f, .05f), new Color(.9f, .7f, .1f), pulse) : new Color(.1f, .13f, .18f, .95f));
                if (!ReferenceEquals(rowSource[i], entry) || rowText[i] == null)
                {
                    rowSource[i] = entry;
                    rowText[i] = (i + 1) + "   " + entry.Name + "   " + entry.Score.ToString("N0") + "점   " + Mathf.RoundToInt(entry.Distance) + "m   " + entry.Style;
                }
                GUI.color = new Color(1, 1, 1, enter); GUI.Label(new Rect(row.x + 18, row.y + 8, row.width - 30, 34), rowText[i], skin.Style(21, FontStyle.Bold)); GUI.color = Color.white;
            }
            if (data.LastRank > Rows && t > 1.8f)
            {
                var mine = new Rect(board.x + 24, 176 + Rows * 54 + 6, 512, 40);
                skin.Card(mine, new Color(.55f, .4f, .05f));
                GUI.Label(mine, data.LastRank + "위   " + data.LastEntry?.Name, skin.Centered(20, FontStyle.Bold));
            }
        }
    }
}
