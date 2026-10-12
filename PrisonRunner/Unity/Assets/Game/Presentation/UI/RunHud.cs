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
        private readonly CachedText phase = new CachedText();
        private readonly CachedText[] itemTimers = { new CachedText(), new CachedText(), new CachedText() };
        public RunHud(HudSkin hudSkin) { skin = hudSkin; }
        public void Draw(RunSession session, ChunkSpawner map, float fade, IHudData data, Action pause)
        {
            var state = session.Flow.State;
            var style = data?.Visitor?.Style ?? PlayStyle.Keyboard;
            GUI.color = new Color(1, 1, 1, fade);
            DrawCards(session, map, pause);
            if (session.Config.IntervalMode) DrawInterval(session);
            DrawItems(session);
            GUI.color = Color.white;
            if (data != null && PlayStyleInfo.UsesCamera(style)) PosePreview.Draw(skin, new Rect(1020, 470, 236, 190), data, true);
            GUI.Label(new Rect(30, 667, 960, 32), Help(style, session.Runner.Riding), skin.Small);
            if (data != null) DrawBanners(data, state);
            if (state == GameFlowState.Caught) GUI.Label(new Rect(350, 294, 900, 100), session.Reason == CaptureReason.BananaPursuit ? "경찰에게 잡혔어요!" : session.Reason == CaptureReason.BrokenRail ? "철로에서 추락!" : "충돌!", skin.Style(48, FontStyle.Bold));
        }
        private static string Help(PlayStyle style, bool riding)
        {
            if (style == PlayStyle.FullBody) return riding ? "몸을 좌우로 옮겨 무너진 철로 반대쪽으로 · 휴식 구간" : "몸을 좌우로 이동 · 점프 · 숙이기 · 무릎 높이 들기 · 제자리 달리기";
            if (style == PlayStyle.UpperBody) return riding ? "상체를 좌우로 기울여 무너진 철로 반대쪽으로 · 휴식 구간" : "상체 기울이기 · 두 손 들기 = 점프 · 숙이기 · 팔 크게 흔들기 = 무릎 들기";
            return riding ? "A / D  좌우 기울이기 · 무너진 철로 반대쪽으로" : "A / D  레인     SPACE  점프     S / ↓  슬라이드     W  하이니";
        }
        // Interval bar: sprint (walking) or rest (cart) with the seconds left in this section.
        private void DrawInterval(RunSession session)
        {
            bool rest = session.Resting;
            var rect = new Rect(440, 76, 400, 34);
            skin.Card(rect, new Color(.035f, .045f, .065f, .85f));
            float seconds = session.PhaseRemainingSeconds;
            GUI.color = rest ? new Color(.3f, .85f, .5f) : new Color(1f, .45f, .25f);
            GUI.DrawTexture(new Rect(rect.x + 6, rect.yMax - 7, (rect.width - 12) * Mathf.Clamp01(seconds / (rest ? session.Config.RestSeconds : session.Config.SprintSeconds)), 3), Texture2D.whiteTexture);
            GUI.color = Color.white;
            long key = (rest ? 1000 : 0) + (long)Mathf.Ceil(seconds) + 10000L * session.IntervalCycle;
            GUI.Label(rect, phase.Get(key, k => (k % 10000 >= 1000 ? "휴식 · 수레" : "질주 " + (k / 10000 + 1) + "세트") + "  " + (k % 1000) + "초"), skin.Centered(19, FontStyle.Bold));
        }
        private void DrawItems(RunSession session)
        {
            int row = 0;
            for (int i = 0; i < ItemCatalog.All.Length; i++)
            {
                var item = ItemCatalog.All[i];
                if (!session.Items.IsActive(item.Kind)) continue;
                var rect = new Rect(24, 292 + row * 42, 230, 36); row++;
                skin.Card(rect, item.Kind == ItemKind.Magnet ? new Color(.55f, .12f, .12f, .9f) : item.Kind == ItemKind.DoubleScore ? new Color(.6f, .45f, .05f, .9f) : new Color(.12f, .35f, .65f, .9f));
                int id = i;
                GUI.Label(rect, itemTimers[i].Get((long)Mathf.Ceil(session.Items.Remaining(item.Kind)), s => ItemCatalog.All[id].Name + "  " + s + "초"), skin.Centered(18, FontStyle.Bold));
            }
        }
        private void DrawBanners(IHudData data, GameFlowState state)
        {
            float start = data.StartBanner;
            if (start > 0 && state == GameFlowState.Running)
            {
                bool calibrating = data.PoseStatus == PoseStatus.Calibrating || data.PoseStatus == PoseStatus.Searching;
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01(start * 2));
                var rect = new Rect(340, 200, 600, 120);
                skin.Card(rect, new Color(.02f, .03f, .05f, .75f));
                GUI.color = new Color(1, .82f, .3f, Mathf.Clamp01(start * 2));
                GUI.Label(rect, calibrating ? "바르게 서 주세요" : start > 1f ? "준비!" : "출발!", skin.Centered(56, FontStyle.Bold));
                GUI.color = Color.white;
            }
            if (data.CaughtFlash > 0) { GUI.color = new Color(.9f, .05f, .05f, data.CaughtFlash * .45f); GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture); GUI.color = Color.white; }
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
