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
            // The whole screen starts the game; a pulsing prompt with a shadow stays readable over the dark mine.
            float pulse = .65f + .35f * Mathf.Sin(Time.unscaledTime * 3f);
            var prompt = new Rect(240, 630, 800, 56);
            GUI.color = new Color(0, 0, 0, .75f * fade * pulse); GUI.Label(new Rect(prompt.x + 2, prompt.y + 2, prompt.width, prompt.height), "아무 곳이나 눌러 시작하세요", skin.Centered(32, FontStyle.Bold));
            GUI.color = new Color(1f, .78f, .32f, fade * pulse); GUI.Label(prompt, "아무 곳이나 눌러 시작하세요", skin.Centered(32, FontStyle.Bold));
        }
    }
}
