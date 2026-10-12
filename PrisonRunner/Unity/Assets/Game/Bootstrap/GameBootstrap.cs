using Muhanok.Application;
using System.Collections;
using Muhanok.Content;
using Muhanok.Domain;
using Muhanok.Infrastructure;
using Muhanok.Presentation;
using Muhanok.Presentation.Map;
using UnityEngine;
using UnityEngine.Timeline;

namespace Muhanok.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour, IHudData
    {
        public GameSettings Settings;
        public MuhanokMapChunk[] ChunkPrefabs;
        public GameObject MonkeyPrefab, PolicePrefab, CartPrefab, PickaxePrefab, IdeaPrefab, DustPrefab, HitPrefab;
        public FirstPersonCameraRig CameraRig;
        public Camera OutputCamera;
        public TimelineAsset IntroTimeline;
        public GameObject HandsPrefab;
        public Texture2D TitleLogo;
        public Texture2D LoadingImage;
        public GameObject CartRidePrefab;
        public bool StartSetupOpen=>hud!=null&&hud.SetupOpen;
        public bool IsLoading=>hud!=null&&hud.Loading;
        public bool IsRevealing=>hud!=null&&hud.Revealing;
        public RunSession Session { get; private set; }
        public ChunkSpawner Map { get; private set; }
        public IntroSequenceDirector Intro { get; private set; }
        public PoseTracker Pose { get; private set; }
        public ExhibitionStore Exhibition { get; private set; }
        public RunnerView Hero { get; private set; }
        private RunnerView guard;
        private KeyboardGameInput keyboard;
        private PoseGameInput poseInput;
        private InputRouter router;
        private SaveRepository save;
        private HUDPresenter hud;
        private GameAudio audioPlayer;
        private float caughtTime, runFade, runStartedAt = -10, caughtAt = -10;
        private ParticleSystem dust, hit;
        private bool previousStumble;
        // The visitor who is playing (or about to). Runs started from the setup screen record today's ranking.
        private VisitorProfile visitor = new VisitorProfile();
        private bool visitorRun, bannerSounded, goSounded;
        private RankingEntry lastEntry;
        private int lastRank;
        private void Awake()
        {
            Session = new RunSession(Settings != null ? Settings.Run : new RunConfig());
            if (ChunkPrefabs == null || ChunkPrefabs.Length < 5 || CameraRig == null || OutputCamera == null)
            { Debug.LogError("GameScene requires content references. Run Muhanok/Build GameScene."); enabled = false; return; }
            keyboard = new KeyboardGameInput(); poseInput = new PoseGameInput(); router = new InputRouter(Session, keyboard, poseInput);
            Pose = new PoseTracker(poseInput, Settings != null ? Settings.Camera : null, Settings != null ? Settings.Motion : null, () => Session.Runner.Lane, () => Session.Runner.Riding);
            save = new SaveRepository();
            Exhibition = new ExhibitionStore(null, Settings == null || Settings.UnlockAllItems);
            Hero = CreateActor("CHR_Monkey_Prisoner", MonkeyPrefab);
            guard = CreateActor("CHR_Police_Officer", PolicePrefab);
            gameObject.AddComponent<CartRideView>().Configure(Session,Hero,CartRidePrefab);
            OutputCamera.GetComponent<CinematicFocus>()?.Configure(Session,Hero);
            var mapRoot = new GameObject("ChunkPool"); mapRoot.transform.SetParent(transform, false);
            Map = new ChunkSpawner(ChunkPrefabs, mapRoot.transform, Session);
            Map.Decorator = new RuntimeContent(mapRoot.transform, () => Exhibition.Progress.Available(), Settings != null ? Settings.ItemChancePerChunk : .35f);
            CameraRig.Configure(OutputCamera);
            if(OutputCamera.GetComponent<GameViewport>()==null) OutputCamera.gameObject.AddComponent<GameViewport>();
            if (HandsPrefab != null)
            {
                var hands = Instantiate(HandsPrefab, OutputCamera.transform);
                hands.GetComponent<FirstPersonHands>().Configure(Session);
            }
            var cart = CreateProp("PROP_MineCart", CartPrefab, transform);
            var tool = CreateProp("PROP_Pickaxe", PickaxePrefab, Hero.ToolAnchor);
            tool.transform.localPosition = Hero.Humanoid ? Vector3.zero : new Vector3(.05f, -.35f, .35f);
            if(Hero.Humanoid) { tool.transform.localRotation=Quaternion.Euler(0,0,-90); tool.transform.localScale=Vector3.one*.75f; }
            var bulb = CreateProp("VFX_LightBulb_Idea", IdeaPrefab, Hero.transform); bulb.transform.localPosition = new Vector3(0, 2.4f, 0);
            if(Hero.Humanoid) bulb.transform.localScale=Vector3.one*.65f;
            dust = CreateProp("VFX_Dust_Run", DustPrefab, Hero.transform).GetComponentInChildren<ParticleSystem>();
            hit = CreateProp("VFX_Hit_Stumble", HitPrefab, Hero.transform).GetComponentInChildren<ParticleSystem>();
            var cartAudio = cart.GetComponent<AudioSource>();
            Intro = gameObject.AddComponent<IntroSequenceDirector>();
            Intro.Configure(Session, Hero, guard, CameraRig, cart.transform, tool.transform, bulb, cartAudio, IntroTimeline);
            hud = gameObject.AddComponent<HUDPresenter>();
            hud.Map=Map; hud.Data=this;
            hud.Configure(Session, RetrySameVisitor);
            hud.TitleLogo = TitleLogo;
            hud.LoadingImage=LoadingImage; hud.BeginRun=StartVisitorRun; hud.PauseRun=TogglePause; hud.NextVisitor=NextVisitor;
            audioPlayer = gameObject.AddComponent<GameAudio>();
            audioPlayer.Configure(Session, Settings != null ? Settings.MusicVolume : .35f, Settings != null ? Settings.EffectsVolume : .7f);
            Session.Flow.Changed += StateChanged;
            Session.Flow.Transition(GameFlowState.TitleIdle);
        }
        private RunnerView CreateActor(string id, GameObject prefab)
        {
            var root = new GameObject(id); root.transform.SetParent(transform, false);
            var visual = CreateProp("VisualRoot", prefab, root.transform);
            var view = root.AddComponent<RunnerView>(); view.Configure(visual); return view;
        }
        private static GameObject CreateProp(string id, GameObject prefab, Transform parent)
        {
            GameObject result = prefab != null ? Instantiate(prefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            result.name = id; result.transform.SetParent(parent, false);
            if (prefab == null) { var collider = result.GetComponent<Collider>(); if (collider != null) collider.enabled = false; }
            return result;
        }
        public void StartIntro()
        {
            if (Session.Flow.Transition(GameFlowState.IntroTimeline)) Intro.Begin();
        }
        public void OpenStartSetup()
        {
            if(Session.Flow.State!=GameFlowState.TitleIdle||hud.Loading) return;
            KeyboardGameInput.SetTextInput(true); hud.OpenSetup();
        }
        // Direct start (tests, tools): keeps the current play style and does not enter the ranking.
        public void BeginRun(string weight)
        {
            if(Session.Flow.State!=GameFlowState.TitleIdle||hud.Loading||!Session.Workout.SetWeight(weight)) return;
            visitorRun=false; Session.Workout.Met=PlayStyleInfo.Met(visitor.Style);
            StartCoroutine(PrepareRun(723));
        }
        // Start from the setup screen: this visitor's run counts for today's ranking.
        public void StartVisitorRun(VisitorProfile profile)
        {
            if(profile==null||Session.Flow.State!=GameFlowState.TitleIdle||hud.Loading||!Session.Workout.SetWeight(profile.Weight)) return;
            visitor=profile; visitorRun=true; KeyboardGameInput.SetTextInput(false);
            Session.Workout.Met=PlayStyleInfo.Met(profile.Style);
            Pose.SetStyle(profile.Style);
            StartCoroutine(PrepareRun(Settings != null && Settings.Run.RandomizeLayout ? Random.Range(1, int.MaxValue) : 723));
        }
        private IEnumerator PrepareRun(int seed)
        {
            Session.Flow.Transition(GameFlowState.LoadingIntro);
            hud.BeginLoading(); hud.LoadingProgress=.12f;
            // The player stands still during loading; that is the calibration pose.
            Pose.Recalibrate();
            yield return new WaitForSecondsRealtime(.25f);
            RouteSurface.UsePlan(new RoutePlan(Session.Config, seed));
            Session.Reset(); keyboard.Clear(); poseInput.Clear(); hud.LoadingProgress=.42f;
            yield return new WaitForSecondsRealtime(.25f);
            Map.Reset(seed); Map.AnimateGates(Session.Runner.Z); hud.LoadingProgress=.78f;
            yield return new WaitForSecondsRealtime(.25f);
            Intro.PrepareGameplay(); hud.LoadingProgress=1;
            yield return new WaitForSecondsRealtime(.25f);
            Session.Flow.Transition(GameFlowState.Running); hud.EndLoading();
        }
        public void TogglePause()
        {
            if(Session.Flow.State!=GameFlowState.Running) return;
            Session.Paused=!Session.Paused; Time.timeScale=Session.Paused?0:1;
            keyboard.Clear(); poseInput.Clear();
        }
        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            bool live = Session.Flow.State == GameFlowState.Running && !Session.Paused && !hud.Loading;
            Pose.Tick(now, live);
            OperatorKeys();
            TickInput();
            if (Session.Flow.State == GameFlowState.Caught && Time.timeScale < 1 && Time.realtimeSinceStartup - caughtAt > .7f) Time.timeScale = 1;
            if(Session.Paused||hud.Loading||hud.Revealing) return;
            float dt = Mathf.Min(Time.deltaTime, .1f);
            Intro.Tick(dt);
            if (Session.Flow.State == GameFlowState.IntroTimeline) hud.Fade = Mathf.Clamp01(1 - Intro.Elapsed * 3f);
            if (Session.Flow.State == GameFlowState.Running)
            {
                StartBannerSounds();
                float previousZ = Session.Runner.Z;
                Session.Tick(dt); Hero.Apply(Session.Runner); Map.Tick(Session.Runner.Z); Map.Evaluate(previousZ);
                Map.AnimateGates(Session.Runner.Z);
                bool exercising = Pose.UsesCamera ? Pose.Tracking && Pose.Reading.Active : true;
                Session.Workout.Tick(dt,Session.Flow.State==GameFlowState.Running,exercising);
                runFade += dt; hud.Fade = Mathf.Clamp01(runFade * 2f);
                if (dust != null && !dust.isPlaying) dust.Play();
                if (Session.Runner.Stumbling && !previousStumble && hit != null) hit.Play();
                previousStumble = Session.Runner.Stumbling;
                if (Session.Flow.State == GameFlowState.Running)
                {
                    guard.Pose(new Vector3(Session.Runner.X, 0, Session.Runner.Z - Mathf.Lerp(15, 1.8f, Session.Chase.Pressure)), !Session.Runner.Riding);
                    guard.Play("AN_Guard_ChaseRun");
                }
            }
            if (Session.Flow.State == GameFlowState.Caught)
            {
                caughtTime += dt;
                guard.PoseWorld(Vector3.Lerp(guard.transform.position,Hero.transform.position+Hero.transform.forward*2.2f+Hero.transform.right*.4f,dt*4),Session.Reason!=CaptureReason.BrokenRail);
                guard.transform.rotation=Quaternion.LookRotation(-Hero.transform.forward,Vector3.up);
                if (caughtTime >= .65f) Session.Flow.Transition(GameFlowState.Result);
            }
        }
        private void StartBannerSounds()
        {
            float banner = StartBanner;
            if (banner <= 0) return;
            if (!bannerSounded) { bannerSounded = true; audioPlayer.Countdown(false); }
            if (!goSounded && banner <= 1f && Pose.Status != PoseStatus.Calibrating) { goSounded = true; audioPlayer.Countdown(true); }
        }
        private void OperatorKeys()
        {
            if (KeyboardGameInput.RecalibratePressed && !hud.SetupOpen) Pose.Recalibrate();
            if (KeyboardGameInput.ClearRankingPressed) { Exhibition.ClearToday(); Debug.Log("Today's ranking cleared."); }
        }
        public void TickInput()
        {
            keyboard.Poll();
            if(hud.Loading||hud.Revealing){keyboard.Clear();poseInput.Clear();return;}
            router.Tick();
            if (Session.Flow.State == GameFlowState.TitleIdle && !hud.BlocksStart && KeyboardGameInput.StartPressed) OpenStartSetup();
        }
        private void LateUpdate()
        {
            if (Session != null && (Session.Flow.State == GameFlowState.Running || Session.Flow.State == GameFlowState.Caught))
            {
                if(Session.Runner.Riding)CameraRig.FollowCart(Hero.Head,Time.deltaTime,-Session.Runner.CartLean*12);
                else CameraRig.Follow(Hero.Head,Session.Runner.Crouching,Time.deltaTime,Session.Runner.Slowing?Mathf.Sin(Time.time*12)*2:0);
            }
        }
        private void StateChanged(GameFlowState state)
        {
            if (state == GameFlowState.Running) { runFade = 0; poseInput.Clear(); Pose.ResetCommands(); runStartedAt = Time.realtimeSinceStartup; bannerSounded = goSounded = false; }
            if (state == GameFlowState.Caught)
            {
                caughtTime = 0; guard.Play("AN_Guard_Catch"); Hero.Play("AN_Monkey_Caught");
                if(Session.Reason!=CaptureReason.BrokenRail)
                {
                    guard.PoseWorld(Hero.transform.position+Hero.transform.forward*2.5f+Hero.transform.right*.8f,true);
                    guard.transform.rotation=Quaternion.LookRotation(-Hero.transform.forward,Vector3.up);
                }
                if (dust != null) dust.Stop();
                // A short slow motion and red flash make the capture readable.
                caughtAt = Time.realtimeSinceStartup; Time.timeScale = .35f;
            }
            if (state == GameFlowState.Result)
            {
                Time.timeScale = 1;
                var args=System.Environment.GetCommandLineArgs();
                bool tooling=System.Array.IndexOf(args,"--muhanok-capture")>=0||System.Array.IndexOf(args,"--muhanok-smoke")>=0;
                if(!tooling)save.Record(Session.Score.Distance,Session.Score.Score);
                lastEntry=null; lastRank=0;
                if(!tooling&&visitorRun)
                {
                    lastEntry=new RankingEntry{Name=string.IsNullOrWhiteSpace(visitor.Name)?"관람객 "+(Exhibition.Today.Entries.Count+1):visitor.Name.Trim(),Score=Session.Score.Score,
                        Distance=Session.Score.Distance,Coins=Session.Score.Coins,Style=PlayStyleInfo.Label(visitor.Style),Ticks=System.DateTime.Now.Ticks};
                    lastRank=Exhibition.Record(lastEntry);
                    Exhibition.DepositCoins(Session.Score.Coins);
                }
            }
        }
        public void Retry()
        {
            if (!Session.Flow.Transition(GameFlowState.Retry)) return;
            Session.Reset(); keyboard.Clear(); poseInput.Clear(); Map.Reset(); Intro.ResetSequence();
            Time.timeScale=1; hud.CloseSetup();
            if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (hit != null) hit.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            previousStumble = false; runFade = caughtTime = 0; hud.Fade = 1;
            Session.Flow.Transition(GameFlowState.TitleIdle);
        }
        // "다시 하기": the same visitor runs again with the same settings.
        public void RetrySameVisitor()
        {
            bool again = visitorRun; var profile = visitor;
            Retry();
            if (again) StartVisitorRun(profile);
        }
        // "다음 관람객": back to an empty setup screen for the next person.
        public void NextVisitor()
        {
            var style = visitor.Style;
            Retry();
            visitor = new VisitorProfile { Style = style == PlayStyle.Keyboard ? PlayStyle.FullBody : style };
            visitorRun = false; OpenStartSetup();
        }
        private void OnDestroy() { Time.timeScale=1; Pose?.Dispose(); RouteSurface.UsePlan(null); KeyboardGameInput.SetTextInput(false); }

        // IHudData
        VisitorProfile IHudData.Visitor => visitor;
        Texture IHudData.CameraTexture => Pose.CameraTexture;
        PoseFrame IHudData.PoseFrame => Pose.LastFrame;
        PoseStatus IHudData.PoseStatus => Pose.Status;
        string IHudData.PoseMessage => Pose.Message;
        MotionReading IHudData.Motion => Pose.Reading;
        float IHudData.SecondsSinceJump => (float)(Time.realtimeSinceStartupAsDouble - Pose.LastJumpTime);
        float IHudData.CalibrationProgress => Pose.CalibrationProgress;
        float IHudData.StepsPerMinute => Pose.StepsPerMinute;
        bool IHudData.BodyActive => Pose.Tracking && Pose.Reading.Active;
        ExhibitionProgress IHudData.Progress => Exhibition.Progress;
        DailyRanking IHudData.Today => Exhibition.Today;
        RankingEntry IHudData.LastEntry => lastEntry;
        int IHudData.LastRank => lastRank;
        public float StartBanner => Session.Flow.State == GameFlowState.Running ? Mathf.Max(0, 2.2f - (Time.realtimeSinceStartup - runStartedAt)) : 0;
        float IHudData.CaughtFlash => Session.Flow.State == GameFlowState.Caught ? Mathf.Clamp01(1 - (Time.realtimeSinceStartup - caughtAt) / .6f) : 0;
        void IHudData.PreviewStyle(PlayStyle style) { visitor.Style = style; Pose.SetStyle(style); }
        void IHudData.Unlock(ItemKind kind) => Exhibition.Unlock(kind);
        void IHudData.Recalibrate() => Pose.Recalibrate();
    }
}
