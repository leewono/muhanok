using System;
using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation
{
    // Visitor setup: name for today's ranking, optional weight, play style with a live camera check, and item unlocks.
    internal sealed class SetupScreen
    {
        private readonly HudSkin skin;
        private readonly WorkoutEstimate probe = new WorkoutEstimate();
        private string name = "", weightText = "", validation = "";
        private PlayStyle style = PlayStyle.FullBody;
        public SetupScreen(HudSkin hudSkin) { skin = hudSkin; }
        public void Open(IHudData data)
        {
            validation = "";
            var visitor = data?.Visitor;
            if (visitor != null) { name = visitor.Name; weightText = visitor.Weight; style = visitor.Style; }
            data?.PreviewStyle(style);
        }
        public void Draw(IHudData data, Action<VisitorProfile> beginRun)
        {
            skin.Card(new Rect(0, 0, 1280, 720), new Color(.025f, .035f, .06f, .82f));
            skin.Card(new Rect(150, 70, 980, 590), new Color(.055f, .075f, .11f, .97f));
            GUI.color = new Color(1, .78f, .32f); GUI.Label(new Rect(195, 92, 500, 50), "탈출 준비", skin.Style(36, FontStyle.Bold));
            GUI.color = Color.white;
            GUI.Label(new Rect(195, 150, 200, 34), "이름 (순위표)", skin.Label);
            GUI.SetNextControlName("MuhanokName"); name = GUI.TextField(new Rect(195, 186, 400, 50), name, 10, skin.Input);
            GUI.Label(new Rect(195, 246, 260, 34), "몸무게 (선택)", skin.Label);
            GUI.SetNextControlName("MuhanokWeight"); weightText = GUI.TextField(new Rect(195, 282, 300, 50), weightText, 7, skin.Input);
            GUI.Label(new Rect(510, 289, 70, 40), "kg", skin.Label);
            GUI.Label(new Rect(195, 344, 300, 34), "플레이 방식", skin.Label);
            for (int i = 0; i < 3; i++)
            {
                var option = (PlayStyle)i;
                if (Choice(new Rect(195 + i * 136, 380, 128, 48), PlayStyleInfo.Label(option), option == style) && option != style)
                {
                    style = option; data?.PreviewStyle(style);
                }
            }
            GUI.Label(new Rect(195, 434, 420, 60), Guide(style), skin.Style(16, FontStyle.Normal));
            DrawItems(data, new Rect(195, 494, 400, 92));
            GUI.color = new Color(1, .55f, .35f); GUI.Label(new Rect(640, 548, 440, 28), validation, skin.Small); GUI.color = Color.white;
            if (data != null && PlayStyleInfo.UsesCamera(style)) PosePreview.Draw(skin, new Rect(640, 150, 450, 340), data, true);
            else KeyboardGuide(new Rect(640, 150, 450, 340));
            if (skin.PrimaryButton(new Rect(640, 584, 450, 56), "시작하기"))
            {
                if (!probe.SetWeight(weightText)) { validation = "몸무게는 10~350kg 숫자로 입력해 주세요."; return; }
                beginRun?.Invoke(new VisitorProfile { Name = name.Trim(), Weight = weightText.Trim(), Style = style });
            }
            if (data != null && PlayStyleInfo.UsesCamera(style) && skin.FlatButton(new Rect(640, 500, 450, 40), "자세 다시 맞추기 (R)", false)) data.Recalibrate();
        }
        private bool Choice(Rect rect, string text, bool selected)
        {
            skin.Card(rect, selected ? new Color(1, .76f, .23f) : new Color(.12f, .16f, .22f));
            GUI.color = selected ? new Color(.15f, .09f, .025f) : Color.white;
            bool pressed = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.Label(rect, text, skin.Centered(22, FontStyle.Bold)); GUI.color = Color.white;
            return pressed;
        }
        private static string Guide(PlayStyle style) =>
            style == PlayStyle.FullBody ? "몸 전체가 보이게 서세요. 좌우로 이동, 점프, 숙이기, 무릎 높이 들기."
            : style == PlayStyle.UpperBody ? "허리 위가 보이게 서거나 앉으세요. 상체 기울이기, 두 손 들기 = 점프, 숙이기, 팔 크게 흔들기 = 무릎 들기."
            : "A/D 이동, Space 점프, S 숙이기, W 무릎 들기.";
        private void KeyboardGuide(Rect area)
        {
            skin.Card(area, new Color(.02f, .03f, .05f, .9f));
            string[] lines = { "A / ←   왼쪽", "D / →   오른쪽", "Space   점프", "S / ↓   숙이기 (누르고 있기)", "W / ↑   무릎 들기 (계단)" };
            for (int i = 0; i < lines.Length; i++) GUI.Label(new Rect(area.x + 40, area.y + 40 + i * 52, area.width - 80, 40), lines[i], skin.Label);
        }
        private void DrawItems(IHudData data, Rect area)
        {
            skin.Card(area, new Color(.08f, .1f, .14f, .95f));
            var progress = data?.Progress;
            if (progress == null) return;
            GUI.Label(new Rect(area.x + 12, area.y + 6, area.width - 24, 26), progress.UnlockAll ? "아이템: 전시 모드 · 모두 사용 가능" : "전시 코인 풀  " + progress.CoinPool + "개", skin.Style(17, FontStyle.Bold));
            for (int i = 0; i < ItemCatalog.All.Length; i++)
            {
                var item = ItemCatalog.All[i];
                var rect = new Rect(area.x + 12 + i * 128, area.y + 38, 120, 46);
                bool unlocked = progress.IsUnlocked(item.Kind);
                if (unlocked) { skin.Card(rect, new Color(.2f, .5f, .3f)); GUI.Label(rect, item.Name, skin.Centered(17, FontStyle.Bold)); }
                else if (skin.FlatButton(rect, item.Name + " " + item.UnlockCost, progress.CoinPool >= item.UnlockCost)) data.Unlock(item.Kind);
            }
        }
    }
}
