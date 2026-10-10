using System;
using Muhanok.Domain;
using Muhanok.Presentation.Map;
using UnityEngine;

namespace Muhanok.Presentation
{
    // In-run HUD: score cards, area name, cart balance, status, action cue, pause button and capture message.
    internal sealed class RunHud
    {
        private readonly HudSkin skin;
        private readonly CachedText coins = new CachedText(), distance = new CachedText(), kcal = new CachedText(), speed = new CachedText();
        private string poseSource, poseText;
        public RunHud(HudSkin hudSkin) { skin = hudSkin; }
        public void Draw(RunSession session, ChunkSpawner map, float fade, string poseStatus, Action pause)
        {
            var state = session.Flow.State;
            GUI.color = new Color(1, 1, 1, fade);
            DrawCards(session, map, pause);
            GUI.color = Color.white;
            if (!ReferenceEquals(poseStatus, poseSource)) { poseSource = poseStatus; poseText = "POSE  " + poseStatus; }
            GUI.Label(new Rect(960, 641, 300, 42), poseText, skin.Small);
            GUI.Label(new Rect(30, 667, 860, 32), session.Runner.Riding ? "A / D  좌우 기울이기 · 무너진 철로 반대쪽으로" : "A / D  레인     SPACE  점프     S / ↓  슬라이드     W  하이니", skin.Small);
            if (state == GameFlowState.Caught) GUI.Label(new Rect(350, 294, 900, 100), session.Reason == CaptureReason.BananaPursuit ? "경찰에게 잡혔어요!" : session.Reason == CaptureReason.BrokenRail ? "철로에서 추락!" : "충돌!", skin.Style(48, FontStyle.Bold));
        }
        private void DrawCards(RunSession session, ChunkSpawner map, Action pause)
        {
            skin.Card(new Rect(24, 24, 230, 65), new Color(.035f, .045f, .065f, .88f));
            skin.Card(new Rect(24, 100, 230, 65), new Color(.035f, .045f, .065f, .88f));
            GUI.color = Color.white; GUI.DrawTexture(new Rect(39, 32, 49, 49), skin.CoinIcon);
            GUI.color = Color.white; GUI.Label(new Rect(123, 34, 120, 45), coins.Get(session.Score.Coins, value => value.ToString()), skin.Style(34, FontStyle.Bold));
            GUI.color = Color.white; GUI.DrawTexture(new Rect(39, 108, 49, 49), skin.DistanceIcon);
            GUI.color = Color.white; GUI.Label(new Rect(121, 108, 130, 40), distance.Get(Mathf.RoundToInt(session.Score.Distance), value => value.ToString("N0") + " m"), skin.Label);
            skin.Card(new Rect(514, 23, 252, 47), new Color(.035f, .045f, .065f, .78f));
            GUI.Label(new Rect(514, 29, 252, 35), RouteSurface.AreaAt(session.Runner.Z), skin.Centered(25, FontStyle.Bold));
            if (session.Workout.WeightKg.HasValue)
            {
                skin.Card(new Rect(24, 177, 230, 50), new Color(.035f, .045f, .065f, .88f));
                GUI.Label(new Rect(38, 190, 220, 30), kcal.Get((long)Math.Round(session.Workout.EstimatedKcal * 10), value => "예상  " + (value / 10.0).ToString("F1") + " kcal"), skin.Small);
            }
            if (session.Runner.Riding)
            {
                GUI.Label(new Rect(950, 32, 220, 35), "수레 · 좌우 균형", skin.Small);
                GUI.color = new Color(.1f, .12f, .17f); GUI.DrawTexture(new Rect(950, 71, 220, 12), Texture2D.whiteTexture);
                GUI.color = new Color(1, .72f, .12f); GUI.DrawTexture(new Rect(950, 71, 220 * RouteSurface.CartProgress(session.Runner.Z), 12), Texture2D.whiteTexture);
            }
            else
            {
                GUI.color = session.Runner.Slowing ? new Color(1, .65f, .2f) : Color.white;
                GUI.Label(new Rect(935, 36, 242, 38), StatusText(session), skin.Style(17, FontStyle.Bold));
            }
            GUI.color = Color.white;
            skin.Card(new Rect(24, 238, 230, 44), new Color(.035f, .045f, .065f, .88f));
            GUI.Label(new Rect(38, 247, 210, 30), speed.Get((long)Math.Round(session.CurrentSpeed * 10), value => "속도  " + (value / 10.0).ToString("F1") + " m/s"), skin.Small);
            DrawActionCue(ActionCuePresenter.Find(session, map));
            if (skin.FlatButton(new Rect(1190, 23, 66, 64), session.Paused ? "▶" : "Ⅱ", false)) pause?.Invoke();
        }
        private static string StatusText(RunSession session) =>
            session.Flow.State == GameFlowState.Caught && session.Reason == CaptureReason.BananaPursuit ? "바나나 2회 · 추격 종료"
            : session.BananaPursuit ? "경찰이 따라잡고 있어요!"
            : session.Runner.Slowing ? "미끄러짐 · 감속"
            : session.BananaHits > 0 ? "바나나 1 / 2 · 추격 주의"
            : "장애물 충돌 시 종료";
        private void DrawActionCue(string cue)
        {
            if (cue == null) return;
            skin.Card(new Rect(415, 560, 450, 54), new Color(.035f, .045f, .065f, .83f));
            GUI.color = Color.white; GUI.Label(new Rect(415, 565, 450, 45), cue, skin.Centered(23, FontStyle.Bold));
        }
    }
}
