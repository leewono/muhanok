using System;
using UnityEngine;

namespace Muhanok.Presentation
{
    internal sealed class PauseScreen
    {
        private readonly HudSkin skin;
        public PauseScreen(HudSkin hudSkin) { skin = hudSkin; }
        public void Draw(Action resume)
        {
            skin.Card(new Rect(0, 0, 1280, 720), new Color(.025f, .035f, .06f, .75f));
            skin.Card(new Rect(420, 255, 440, 210), new Color(.055f, .075f, .11f, .97f));
            GUI.color = Color.white; GUI.Label(new Rect(470, 285, 350, 65), "잠깐 쉬어가기", skin.Style(34, FontStyle.Bold));
            if (skin.FlatButton(new Rect(470, 375, 340, 55), "계속하기", true)) resume?.Invoke();
        }
    }
}
