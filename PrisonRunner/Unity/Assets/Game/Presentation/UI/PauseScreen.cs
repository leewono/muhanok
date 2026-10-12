using System;
using UnityEngine;

namespace Muhanok.Presentation
{
    internal sealed class PauseScreen
    {
        private readonly HudSkin skin;
        public PauseScreen(HudSkin hudSkin) { skin = hudSkin; }
        public void Draw(Action resume, Action settings)
        {
            skin.Card(new Rect(0, 0, 1280, 720), new Color(.025f, .035f, .06f, .75f));
            skin.Card(new Rect(420, 235, 440, 270), new Color(.055f, .075f, .11f, .97f));
            GUI.color = Color.white; GUI.Label(new Rect(470, 265, 350, 65), "잠깐 쉬어가기", skin.Style(34, FontStyle.Bold));
            if (skin.PrimaryButton(new Rect(470, 350, 340, 58), "계속하기", 26)) resume?.Invoke();
            if (skin.FlatButton(new Rect(470, 422, 340, 48), "설정", false)) settings?.Invoke();
        }
    }
}
