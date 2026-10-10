using System;
using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation
{
    // Optional body-weight input shown before a run.
    internal sealed class SetupScreen
    {
        private readonly HudSkin skin;
        private readonly WorkoutEstimate probe = new WorkoutEstimate();
        private string weightText = "", validation = "";
        public SetupScreen(HudSkin hudSkin) { skin = hudSkin; }
        public void Open() { validation = ""; }
        public void Draw(Action<string> beginRun)
        {
            skin.Card(new Rect(0, 0, 1280, 720), new Color(.025f, .035f, .06f, .80f));
            skin.Card(new Rect(375, 150, 530, 450), new Color(.055f, .075f, .11f, .97f));
            GUI.color = new Color(1, .78f, .32f); GUI.Label(new Rect(420, 185, 440, 50), "탈출 준비", skin.Style(36, FontStyle.Bold));
            GUI.color = Color.white;
            GUI.Label(new Rect(420, 251, 450, 34), "몸무게 (선택 입력)", skin.Label);
            GUI.SetNextControlName("MuhanokWeight"); weightText = GUI.TextField(new Rect(420, 302, 350, 54), weightText, 7, skin.Input);
            GUI.Label(new Rect(790, 309, 70, 40), "kg", skin.Label);
            GUI.Label(new Rect(420, 375, 440, 62), "몸동작 시간으로 예상 소모 칼로리를 계산합니다.\n입력하지 않아도 게임을 시작할 수 있습니다.", skin.Style(17, FontStyle.Normal));
            GUI.color = new Color(1, .55f, .35f); GUI.Label(new Rect(420, 441, 440, 28), validation, skin.Small); GUI.color = Color.white;
            if (skin.FlatButton(new Rect(420, 488, 440, 51), "시작하기", true))
            {
                if (probe.SetWeight(weightText)) beginRun?.Invoke(weightText);
                else validation = "10~350kg 범위의 숫자를 입력해 주세요.";
            }
            if (skin.FlatButton(new Rect(420, 551, 440, 32), "입력 없이 시작", false)) beginRun?.Invoke("");
        }
    }
}
