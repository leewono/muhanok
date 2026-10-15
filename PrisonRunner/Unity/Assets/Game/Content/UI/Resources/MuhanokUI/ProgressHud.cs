using System.Globalization;
using System.Reflection;
using Muhanok.Domain;
using UnityEngine;

namespace MuhanokOverlay
{
    public class ProgressHud : MonoBehaviour
    {
        public static ProgressHud Instance { get; private set; }

        [Header("Fonts")]
        public Font titleFont;
        public Font bodyFont;

        public string[] sectionNames = { "광산", "광산 수레", "감옥 복도", "사육장" };
        public Texture2D markerIcon;
        public Color accent = new Color32(0xFF, 0xC8, 0x3D, 0xFF);

        [Header("Layout")]
        public float meterTop = 78f;
        public Rect barArea = new Rect(280, 612, 720, 52);
        public int guiDepth = 10;

        [Header("Test (only used when the game isn't running)")]
        public bool demoMode = true;
        public float demoSpeed = 12f;
        public float demoSectionLength = 120f;

        public const string BestKey = "overlay_best_distance";
        static readonly CultureInfo Num = CultureInfo.InvariantCulture;

        OverlaySkin skin;
        float distance, best, progress;
        int section, lap = 1, lastSection = -1;
        float sectionChangedAt = -10f;
        string meterText, meterOutline, bestText;
        bool visible = true, live, saved;

        MonoBehaviour game;
        PropertyInfo sessionProp;
        float nextSearch;

        public float Distance { get { return distance; } }
        public float Best { get { return best; } }

        // Spawns itself in the game scene, so nothing has to be added to the scene by hand.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindAnyObjectByType<ProgressHud>() != null) return;
            var go = new GameObject("MuhanokOverlay");
            var hud = go.AddComponent<ProgressHud>();
            hud.demoMode = false;
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            Instance = this;
            if (titleFont == null) titleFont = Resources.Load<Font>("MuhanokUI/BlackHanSans-Regular");
            best = PlayerPrefs.GetFloat(BestKey, 0f);
            SetDistance(0);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (skin != null) skin.Dispose();
        }

        void Update()
        {
            var session = FindSession();
            live = session != null;

            if (live)
            {
                var state = session.Flow.State;
                bool running = state == GameFlowState.Running || state == GameFlowState.Caught;
                if (!running)
                {
                    if (state == GameFlowState.Result && !saved) { SaveBest(); saved = true; }
                    visible = false;
                    return;
                }
                visible = true;
                saved = false;
                SetDistance(session.Score.Distance);
                FollowRoute(session.Runner.Z);
                return;
            }

            if (!demoMode) return;
            visible = true;
            distance += demoSpeed * Time.deltaTime;
            SetDistance(distance);
            float lapLength = demoSectionLength * sectionNames.Length;
            float inLap = distance % lapLength;
            SetSectionProgress(Mathf.FloorToInt(inLap / demoSectionLength), (inLap % demoSectionLength) / demoSectionLength, 1 + Mathf.FloorToInt(distance / lapLength));
        }

        // Reads the running session from GameBootstrap without needing a reference to it.
        RunSession FindSession()
        {
            if (game == null)
            {
                if (Time.unscaledTime < nextSearch) return null;
                nextSearch = Time.unscaledTime + 1f;
                foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (mb.GetType().Name != "GameBootstrap") continue;
                    game = mb;
                    sessionProp = mb.GetType().GetProperty("Session", BindingFlags.Public | BindingFlags.Instance);
                    break;
                }
                if (game == null || sessionProp == null) return null;
            }
            return sessionProp.GetValue(game) as RunSession;
        }

        // Map chunk types to bar sections. Stairs and the facility link count as part of the prison.
        static int SectionOf(int type)
        {
            if (type == 0) return 0;
            if (type == 1 || type == 2) return 1;
            if (type >= 11) return 3;
            return 2;
        }

        void FollowRoute(float z)
        {
            int seq = RouteSurface.SequenceAt(z);
            int cycle = RouteSurface.CycleAt(seq);
            int sec = SectionOf(RouteSurface.TypeAt(seq));

            int start = seq, end = seq;
            while (start > 0 && seq - start < 64 && RouteSurface.CycleAt(start - 1) == cycle && SectionOf(RouteSurface.TypeAt(start - 1)) == sec) start--;
            while (end - seq < 64 && RouteSurface.CycleAt(end + 1) == cycle && SectionOf(RouteSurface.TypeAt(end + 1)) == sec) end++;

            float length = (end - start + 1) * RouteSurface.Length;
            float within = (z - start * RouteSurface.Length) / length;
            SetSectionProgress(sec, within, cycle + 1);
        }

        public void SetDistance(float meters)
        {
            distance = meters;
            string n = Mathf.FloorToInt(meters).ToString("N0", Num);
            meterText = n + "<size=38><color=" + Palette.Hex(accent) + "> m</color></size>";
            meterOutline = n + "<size=38> m</size>";
            if (meters > best) best = meters;
            bestText = "최고 기록 " + Mathf.FloorToInt(best).ToString("N0", Num) + "m";
        }

        public void SetSectionProgress(int index, float withinSection, int lapCount)
        {
            section = Mathf.Clamp(index, 0, sectionNames.Length - 1);
            progress = Mathf.Clamp01((section + Mathf.Clamp01(withinSection)) / sectionNames.Length);
            lap = Mathf.Max(1, lapCount);
            if (section != lastSection)
            {
                lastSection = section;
                sectionChangedAt = Time.unscaledTime;
            }
        }

        public void SaveBest()
        {
            PlayerPrefs.SetFloat(BestKey, best);
            PlayerPrefs.Save();
        }

        public void ResetRun()
        {
            SetDistance(0);
            SetSectionProgress(0, 0, 1);
        }

        public void Show(bool on) { visible = on; }

        void OnGUI()
        {
            if (!visible) return;
            if (skin == null) skin = new OverlaySkin(titleFont, bodyFont);
            GUI.depth = guiDepth;
            var oldMatrix = GUI.matrix;
            GUI.matrix = Screen720.Matrix;

            DrawMeter();
            DrawBar();

            GUI.matrix = oldMatrix;
        }

        void DrawMeter()
        {
            skin.OutlinedText(new Rect(290, meterTop, 700, 90), meterOutline, meterText, 80, Palette.Cream, Palette.Outline, 2.5f, TextAnchor.UpperCenter);

            var pill = new Rect(640 - 115, meterTop + 92, 230, 34);
            skin.BorderBox(pill, Palette.A(Palette.Ground, 0.85f), Palette.Wood, 2);
            skin.Text(pill, bestText, 16, Palette.MeNote, TextAnchor.MiddleCenter, false, true);
        }

        void DrawBar()
        {
            var r = barArea;
            int count = sectionNames.Length;
            skin.BorderBox(r, Palette.A(Palette.Ground, 0.85f), Palette.Wood, 3);

            var track = new Rect(r.x + 18, r.y + 9, r.width - 36 - 64, 16);
            float cell = track.width / count;

            skin.BorderBox(track, Palette.Track, Palette.Dark, 2);
            // current section glows a little so it reads as "you are here"
            skin.Box(new Rect(track.x + cell * section + 2, track.y + 2, cell - 4, track.height - 4), Palette.A(accent, 0.18f));
            if (progress > 0.01f)
                skin.Box(new Rect(track.x + 2, track.y + 2, (track.width - 4) * progress, track.height - 4), accent);

            for (int i = 1; i < count; i++)
                skin.Fill(new Rect(track.x + cell * i - 1.5f, track.y, 3, track.height), Palette.Dark);

            float pop = Mathf.Clamp01(1f - (Time.unscaledTime - sectionChangedAt) / 0.4f);
            float size = 30 + 8 * pop;
            float mx = track.x + track.width * progress;
            var marker = new Rect(mx - size / 2, track.center.y - size / 2, size, size);
            skin.Circle(new Rect(marker.x, marker.y + 2, marker.width, marker.height), new Color(0, 0, 0, 0.4f));
            skin.Circle(marker, Palette.Cream);
            skin.Circle(new Rect(marker.x + 3, marker.y + 3, size - 6, size - 6), Palette.Monkey);
            if (markerIcon != null) GUI.DrawTexture(new Rect(marker.x + 5, marker.y + 5, size - 10, size - 10), markerIcon, ScaleMode.ScaleToFit);

            for (int i = 0; i < count; i++)
            {
                Color c = i == section ? accent : (i < section ? Palette.Sand : Palette.Muted);
                skin.Text(new Rect(track.x + cell * i, track.yMax + 4, cell, 18), sectionNames[i], i == section ? 14 : 13, c, TextAnchor.MiddleCenter, false, true);
            }

            skin.Text(new Rect(track.xMax + 8, r.y + 4, 56, r.height - 8), lap + "바퀴", 16, Palette.Sand, TextAnchor.MiddleCenter, true);
        }
    }
}
