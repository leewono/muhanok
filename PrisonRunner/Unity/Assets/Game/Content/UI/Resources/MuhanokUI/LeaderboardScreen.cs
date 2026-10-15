using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

namespace MuhanokOverlay
{
    public class LeaderboardScreen : MonoBehaviour
    {
        public static LeaderboardScreen Instance { get; private set; }

        [Header("Fonts")]
        public Font titleFont;
        public Font bodyFont;

        public Texture2D background;
        public int guiDepth = -50;

        [Header("Events")]
        public UnityEvent onRetry = new UnityEvent();
        public UnityEvent onMain = new UnityEvent();

        [Header("Test")]
        public bool demoOnStart;

        static readonly CultureInfo Num = CultureInfo.InvariantCulture;
        // card slots left to right: 2nd, 1st, 3rd
        static readonly int[] SlotRank = { 1, 0, 2 };
        static readonly float[] SlotHeight = { 138, 158, 128 };

        OverlaySkin skin;
        bool open, todayOnly, newBest;
        long myId;
        List<LeaderboardRecord> list = new List<LeaderboardRecord>();
        List<LeaderboardRecord> demo;

        public bool IsOpen { get { return open; } }

        void Awake()
        {
            Instance = this;
            if (titleFont == null) titleFont = Resources.Load<Font>("MuhanokUI/BlackHanSans-Regular");
        }

        void Start() { if (demoOnStart) ShowDemo(); }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (skin != null) skin.Dispose();
        }

        public void ShowResult(float meters, string playerName = null)
        {
            demo = null;
            var before = LeaderboardStore.Sorted(false);
            float prevBest = before.Count > 0 ? before[0].meters : 0f;
            myId = LeaderboardStore.Add(meters, playerName).id;
            newBest = meters > prevBest;
            Open(false);
        }

        public void Open(bool today)
        {
            todayOnly = today;
            Reload();
            open = true;
        }

        public void Close() { open = false; }

        [ContextMenu("Show Demo")]
        public void ShowDemo()
        {
            demo = new List<LeaderboardRecord>();
            float[] m = { 3105, 2640, 2210, 2030, 1880, 1742, 1615, 1490, 1420, 1388, 1302, 1248, 1190 };
            for (int i = 0; i < m.Length; i++)
                demo.Add(new LeaderboardRecord { name = "PLAYER_" + (i + 1).ToString("00"), meters = m[i], date = LeaderboardStore.Today, id = i + 1 });
            demo[11].name = "나";
            myId = 12;
            newBest = true;
            todayOnly = false;
            Reload();
            open = true;
        }

        void Reload()
        {
            list = demo != null ? demo : LeaderboardStore.Sorted(todayOnly);
        }

        static string Dist(float m) { return Mathf.FloorToInt(m).ToString("N0", Num) + "m"; }

        void OnGUI()
        {
            if (!open) return;
            if (skin == null) skin = new OverlaySkin(titleFont, bodyFont);
            GUI.depth = guiDepth;

            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            skin.Fill(new Rect(0, 0, Screen.width, Screen.height), Palette.Ground);
            GUI.matrix = Screen720.Matrix;

            if (background != null)
            {
                GUI.DrawTexture(new Rect(0, 0, 1280, 720), background, ScaleMode.ScaleAndCrop);
                skin.Fill(new Rect(0, 0, 1280, 720), new Color(0.08f, 0.05f, 0.03f, 0.78f));
            }

            float x = 230, w = 820;
            skin.Text(new Rect(x, 36, 300, 48), "랭킹", 44, Palette.Cream, TextAnchor.MiddleLeft);
            DrawTabs(new Rect(x + w - 176, 38, 176, 44));
            DrawTop3(x, w);
            DrawRows(x, w);
            DrawMine(x, w);
            DrawButtons();

            GUI.matrix = oldMatrix;

            // keep clicks from reaching the game UI underneath
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp) Event.current.Use();
        }

        void DrawTabs(Rect r)
        {
            skin.Box(r, Palette.Panel);
            var all = new Rect(r.x + 4, r.y + 4, 84, 36);
            var today = new Rect(r.x + 88, r.y + 4, 84, 36);
            if (Tab(all, "전체", !todayOnly) && demo == null) { todayOnly = false; Reload(); }
            if (Tab(today, "오늘", todayOnly) && demo == null) { todayOnly = true; Reload(); }
        }

        bool Tab(Rect r, string label, bool on)
        {
            if (on) skin.Box(r, Palette.Gold);
            skin.Text(r, label, 16, on ? Palette.Outline : Palette.Sand, TextAnchor.MiddleCenter, false, true);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        void DrawTop3(float x, float w)
        {
            float cardW = (w - 32) / 3f, bottom = 102 + 158;
            Color[] rankColor = { Palette.Gold, Palette.Silver, Palette.Bronze };
            for (int s = 0; s < 3; s++)
            {
                int rank = SlotRank[s];
                if (rank >= list.Count) continue;
                var r = new Rect(x + s * (cardW + 16), bottom - SlotHeight[s], cardW, SlotHeight[s]);
                skin.BorderBox(r, Palette.A(Palette.Panel, 0.95f), rankColor[rank], 2);
                float cy = r.center.y;
                skin.Text(new Rect(r.x, cy - 54, r.width, 34), (rank + 1).ToString(), 30, rankColor[rank], TextAnchor.MiddleCenter);
                skin.Text(new Rect(r.x, cy - 16, r.width, 24), list[rank].name, 17, Palette.Cream, TextAnchor.MiddleCenter, false, true);
                skin.Text(new Rect(r.x, cy + 12, r.width, 34), Dist(list[rank].meters), 28, Palette.Cream, TextAnchor.MiddleCenter);
            }
            if (list.Count == 0)
                skin.Text(new Rect(x, 160, w, 60), "아직 기록이 없어요. 첫 기록을 남겨 보세요!", 18, Palette.Sand, TextAnchor.MiddleCenter, false, true);
        }

        void DrawRows(float x, float w)
        {
            for (int i = 0; i < 5; i++)
            {
                int rank = i + 3;
                if (rank >= list.Count) break;
                var r = new Rect(x, 278 + i * 48, w, 42);
                skin.BorderBox(r, Palette.A(Palette.Panel, 0.92f), Palette.WoodDim, 2);
                Columns(r, rank + 1, list[rank].name, Dist(list[rank].meters), 18, 20, Palette.Muted, Palette.Cream);
            }
        }

        void DrawMine(float x, float w)
        {
            int me = myId != 0 ? list.FindIndex(r => r.id == myId) : -1;
            if (me < 0) return;
            var row = new Rect(x, 530, w, 54);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
            skin.BorderBox(row, Palette.MeFill, Color.Lerp(Palette.Gold, Palette.Cream, pulse * 0.35f), 2);
            string name = "나" + (newBest ? "   <size=13><color=" + Palette.Hex(Palette.MeNote) + ">최고 기록 갱신!</color></size>" : "");
            Columns(row, me + 1, name, Dist(list[me].meters), 20, 24, Palette.Gold, Palette.Gold);
        }

        void Columns(Rect r, int rank, string name, string dist, int rankSize, int distSize, Color rankColor, Color distColor)
        {
            skin.Text(new Rect(r.x + 20, r.y, 60, r.height), rank.ToString(), rankSize, rankColor, TextAnchor.MiddleLeft);
            skin.Text(new Rect(r.x + 84, r.y, r.width - 260, r.height), name, 16, Palette.Cream, TextAnchor.MiddleLeft, false, true);
            skin.Text(new Rect(r.xMax - 180, r.y, 160, r.height), dist, distSize, distColor, TextAnchor.MiddleRight);
        }

        void DrawButtons()
        {
            if (skin.Button(new Rect(434, 606, 200, 56), "다시 하기", Palette.Gold, Color.clear, Palette.Outline, 22))
            {
                Close();
                onRetry.Invoke();
            }
            if (skin.Button(new Rect(646, 606, 200, 56), "메인으로", Palette.A(Palette.Ground, 0.9f), Palette.Wood, Palette.Cream, 22))
            {
                Close();
                onMain.Invoke();
            }
        }
    }
}
