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
            GUI.color = new Color(0, 0, 0, .65f * fade); GUI.DrawTexture(new Rect(350, 632, 580, 57), Texture2D.whiteTexture);
            GUI.color = new Color(1f, .88f, .61f, fade);
            GUI.Label(new Rect(400, 641, 480, 42), "게임 시작", skin.Centered(25, FontStyle.Bold));
        }
    }
}
