using System;
using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Presentation
{
    // UI root: owns the start overlay state and draws exactly one screen for the current game state.
    public sealed class HUDPresenter : MonoBehaviour
    {
        private RunSession session;
        public Muhanok.Presentation.Map.ChunkSpawner Map;
        private Func<float> bestDistance;
        private Func<int> bestScore;
        private Func<string> poseStatus;
        private Action retry;
        public float Fade { get; set; } = 1f;
        public Texture2D TitleLogo;
        public Texture2D LoadingImage;
        public Action<string> BeginRun;
        public Action PauseRun;
        public bool SetupOpen { get; private set; }
        public bool Loading { get; private set; }
        public bool Revealing => revealRemaining > 0;
        public float RevealOpacity => Mathf.SmoothStep(0, 1, revealRemaining / .85f);
        public bool BlocksStart => SetupOpen || Loading || Revealing;
        public float LoadingProgress { get; set; }
        private float revealRemaining;
        private HudSkin skin;
        private TitleScreen title;
        private SetupScreen setup;
        private LoadingScreen loading;
        private RunHud runHud;
        private PauseScreen pause;
        private ResultScreen result;
        public void Configure(RunSession run, Func<float> distance, Func<int> score, Func<string> pose, Action again)
        {
            session = run; bestDistance = distance; bestScore = score; poseStatus = pose; retry = again;
            skin = new HudSkin();
            title = new TitleScreen(skin); setup = new SetupScreen(skin); loading = new LoadingScreen(skin);
            runHud = new RunHud(skin); pause = new PauseScreen(skin); result = new ResultScreen(skin);
        }
        public void OpenSetup() { SetupOpen = true; setup.Open(); }
        public void CloseSetup() { SetupOpen = false; Loading = false; revealRemaining = 0; }
        public void BeginLoading() { SetupOpen = false; Loading = true; }
        public void EndLoading() { Loading = false; revealRemaining = .85f; }
        private void Update() { revealRemaining = Mathf.Max(0, revealRemaining - Time.unscaledDeltaTime); }
        private void OnGUI()
        {
            if (session == null) return;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = ViewportLayout.GuiMatrix;
            var state = session.Flow.State;
            if (Loading || Revealing) loading.Draw(LoadingImage, LoadingProgress, Loading, RevealOpacity);
            else if (SetupOpen) setup.Draw(BeginRun);
            else if (state == GameFlowState.TitleIdle) title.Draw(TitleLogo, Fade);
            else if (state == GameFlowState.Running || state == GameFlowState.Caught)
            {
                runHud.Draw(session, Map, Fade, poseStatus(), PauseRun);
                if (session.Paused) pause.Draw(PauseRun);
            }
            else if (state == GameFlowState.Result) result.Draw(session, bestDistance(), bestScore(), retry);
            GUI.color = Color.white; GUI.matrix = old;
        }
        private void OnDestroy() { skin?.Dispose(); }
    }
}
