using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation
{
    // What the screens need from the running game; implemented by the bootstrap.
    public interface IHudData
    {
        VisitorProfile Visitor { get; }
        Texture CameraTexture { get; }
        PoseFrame PoseFrame { get; }
        PoseStatus PoseStatus { get; }
        string PoseMessage { get; }
        float CalibrationProgress { get; }
        float StepsPerMinute { get; }
        bool BodyActive { get; }
        ExhibitionProgress Progress { get; }
        DailyRanking Today { get; }
        RankingEntry LastEntry { get; }
        int LastRank { get; }
        float StartBanner { get; }
        float CaughtFlash { get; }
        void PreviewStyle(PlayStyle style);
        void Unlock(ItemKind kind);
        void Recalibrate();
    }

    // Draws the mirrored webcam image with the detected skeleton.
    internal static class PosePreview
    {
        private static readonly Color Bone = new Color(.3f, 1f, .55f), Joint = new Color(1f, .85f, .2f);
        public static void Draw(HudSkin skin, Rect area, IHudData data, bool showStatus)
        {
            skin.Card(area, new Color(.02f, .03f, .05f, .9f));
            var inner = new Rect(area.x + 6, area.y + 6, area.width - 12, area.height - 12);
            var texture = data.CameraTexture;
            var frame = data.PoseFrame;
            Rect view = inner;
            if (texture != null && texture.width > 16)
            {
                float aspect = texture.width / (float)texture.height;
                view = inner.width / inner.height > aspect ? new Rect(inner.center.x - inner.height * aspect * .5f, inner.y, inner.height * aspect, inner.height)
                    : new Rect(inner.x, inner.center.y - inner.width / aspect * .5f, inner.width, inner.width / aspect);
                GUI.color = Color.white;
                // Mirror horizontally so the player sees themselves like a mirror.
                GUI.DrawTextureWithTexCoords(view, texture, new Rect(1, 0, -1, 1));
            }
            if (frame != null && frame.HasPose)
            {
                for (int b = 0; b < PoseJoint.Bones.GetLength(0); b++)
                {
                    var p = frame.Points[PoseJoint.Bones[b, 0]]; var q = frame.Points[PoseJoint.Bones[b, 1]];
                    if (p.Visibility < .5f || q.Visibility < .5f) continue;
                    GUI.color = Bone;
                    for (int s = 0; s <= 10; s++)
                    {
                        float t = s / 10f;
                        Dot(view, Mathf.Lerp(p.X, q.X, t), Mathf.Lerp(p.Y, q.Y, t), 4);
                    }
                }
                GUI.color = Joint;
                for (int i = 11; i < 29; i++) if (frame.Points[i].Visibility >= .5f) Dot(view, frame.Points[i].X, frame.Points[i].Y, 8);
                if (frame.Points[0].Visibility >= .5f) Dot(view, frame.Points[0].X, frame.Points[0].Y, 10);
            }
            GUI.color = Color.white;
            if (!showStatus) return;
            var status = data.PoseStatus;
            if ((status == PoseStatus.NoCamera || status == PoseStatus.NoModel) && !string.IsNullOrEmpty(data.PoseMessage))
                GUI.Label(new Rect(area.x + 16, area.y + 16, area.width - 32, area.height - 70), data.PoseMessage, skin.Style(16, FontStyle.Normal));
            string text = PoseStatusText.Korean(status);
            if (status == PoseStatus.Tracking && data.StepsPerMinute > 0) text += "  ·  " + Mathf.RoundToInt(data.StepsPerMinute) + "걸음/분";
            var label = new Rect(area.x, area.yMax - 34, area.width, 30);
            skin.Card(label, new Color(0, 0, 0, .6f));
            GUI.Label(label, text, skin.Centered(18, FontStyle.Bold));
            if (status == PoseStatus.Calibrating)
            {
                GUI.color = new Color(1, 1, 1, .25f); GUI.DrawTexture(new Rect(area.x + 20, area.yMax - 44, area.width - 40, 6), Texture2D.whiteTexture);
                GUI.color = new Color(.3f, 1f, .55f); GUI.DrawTexture(new Rect(area.x + 20, area.yMax - 44, (area.width - 40) * data.CalibrationProgress, 6), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
        private static void Dot(Rect view, float x, float y, float size)
        {
            if (x < 0 || x > 1 || y < 0 || y > 1) return;
            GUI.DrawTexture(new Rect(view.x + x * view.width - size * .5f, view.y + y * view.height - size * .5f, size, size), Texture2D.whiteTexture);
        }
    }
}
