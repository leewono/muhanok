using UnityEngine;

namespace Muhanok.Presentation
{
    internal sealed class TitleScreen
    {
        private readonly HudSkin skin;
        public TitleScreen(HudSkin hudSkin) { skin = hudSkin; }
        public void Draw(Texture2D logo, float fade)
        {
            GUI.color = new Color(1f, 1f, 1f, fade);
            if (logo != null) GUI.DrawTexture(new Rect(40, 65, 485, 310), logo, ScaleMode.ScaleToFit, true);
            else GUI.Label(new Rect(76, 170, 650, 115), "무한옥", skin.Large);
            // Bright orange start button with a gentle pulse so it reads against the dark mine.
            float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 3f);
            skin.Card(new Rect(350, 628, 580, 64), new Color(1f, .62f + .08f * pulse, .12f, fade));
            GUI.color = new Color(.18f, .08f, .01f, fade);
            GUI.Label(new Rect(400, 639, 480, 42), "게임 시작", skin.Centered(28, FontStyle.Bold));
        }
    }
}
