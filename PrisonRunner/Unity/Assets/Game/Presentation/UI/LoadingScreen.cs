using UnityEngine;

namespace Muhanok.Presentation
{
    // Supplied loading image with its progress bar; fades out over the live world after loading.
    internal sealed class LoadingScreen
    {
        private readonly HudSkin skin;
        private readonly CachedText percent = new CachedText();
        public LoadingScreen(HudSkin hudSkin) { skin = hudSkin; }
        public void Draw(Texture2D image, float progress, bool loading, float revealOpacity)
        {
            float alpha = loading ? 1 : revealOpacity;
            if (loading) { GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture); }
            GUI.color = new Color(1, 1, 1, alpha);
            var imageRect = new Rect(0, 0, 1280, 720);
            if (image != null)
            {
                float scale = Mathf.Min(1280f / image.width, 720f / image.height);
                imageRect = new Rect((1280 - image.width * scale) * .5f, (720 - image.height * scale) * .5f, image.width * scale, image.height * scale);
                GUI.DrawTexture(imageRect, image, ScaleMode.StretchToFill);
            }
            // Coordinates of the reference's decorative bar, in its original 1672 x 941 image.
            // Repaint that same bar so no second, fake progress indicator remains visible.
            var bar = new Rect(imageRect.x + imageRect.width * 537f / 1672f, imageRect.y + imageRect.height * 846f / 941f, imageRect.width * 600f / 1672f, imageRect.height * 28f / 941f);
            GUI.color = new Color(1, .90f, .70f, alpha); GUI.DrawTexture(bar, Texture2D.whiteTexture);
            var inside = new Rect(bar.x + 2, bar.y + 2, bar.width - 4, bar.height - 4);
            GUI.color = new Color(.08f, .045f, .025f, alpha); GUI.DrawTexture(inside, Texture2D.whiteTexture);
            GUI.color = new Color(1, .77f, .12f, alpha); GUI.DrawTexture(new Rect(inside.x, inside.y, inside.width * Mathf.Clamp01(progress), inside.height), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.Label(new Rect(420, bar.yMax + 8, 440, 30), percent.Get(Mathf.RoundToInt(progress * 100), value => "탈출 준비 중  " + value + "%"), skin.Centered(20, FontStyle.Normal));
        }
    }
}
