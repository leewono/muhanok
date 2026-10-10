using System.Collections;
using Muhanok.Bootstrap;
using Muhanok.Domain;
using Muhanok.Infrastructure;
using Muhanok.Presentation.Map;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Muhanok.Tests
{
    public sealed class GameSceneTests
    {
        private float oldBestDistance;
        private int oldBestScore;
        [SetUp] public void PreserveRecords() { oldBestDistance = PlayerPrefs.GetFloat("Muhanok.BestDistance", 0); oldBestScore = PlayerPrefs.GetInt("Muhanok.BestScore", 0); }
        [TearDown] public void RestoreRecords() { PlayerPrefs.SetFloat("Muhanok.BestDistance", oldBestDistance); PlayerPrefs.SetInt("Muhanok.BestScore", oldBestScore); PlayerPrefs.Save(); }
        [UnityTest] public IEnumerator TitleClickAndKeyboardUseRealInputRouter()
        {
            yield return SceneManager.LoadSceneAsync("GameScene"); yield return null;
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            var background = InputSystem.settings.backgroundBehavior;
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var mouse = InputSystem.AddDevice<Mouse>(); var keys = InputSystem.AddDevice<Keyboard>();
            mouse.MakeCurrent(); keys.MakeCurrent(); InputSystem.EnableDevice(mouse); InputSystem.EnableDevice(keys);
            try
            {
                InputSystem.Update();
                InputState.Change(mouse, new MouseState().WithButton(MouseButton.Left)); game.TickInput();
                Assert.IsTrue(game.StartSetupOpen); Assert.AreEqual(GameFlowState.TitleIdle,game.Session.Flow.State);
                InputState.Change(mouse, new MouseState());
                game.BeginRun(""); yield return new WaitForSecondsRealtime(1.1f);
                Assert.AreEqual(GameFlowState.Running,game.Session.Flow.State);
                yield return new WaitUntil(()=>!game.IsRevealing);
                Assert.AreEqual(0,game.Intro.Elapsed,"Loading image is the intro; a second timeline must not run");
                InputState.Change(keys, new KeyboardState(Key.A)); game.TickInput();
                Assert.AreEqual(-1, game.Session.Runner.Lane);
                InputState.Change(keys, new KeyboardState(Key.S, Key.W)); game.TickInput();
                Assert.IsTrue(game.Session.Runner.Crouching && game.Session.Runner.HighKnee);
                InputState.Change(keys, new KeyboardState()); yield return new WaitForSeconds(1.1f);
                Assert.IsFalse(game.Session.Runner.Crouching || game.Session.Runner.HighKnee);
                InputState.Change(keys, new KeyboardState(Key.Space)); game.TickInput(); game.Session.Tick(.1f);
                Assert.Greater(game.Session.Runner.Y, 0);
            }
            finally { InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keys); InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior; }
        }
        [UnityTest] public IEnumerator FullFlowPoolRetryAndFirstPerson()
        {
            yield return SceneManager.LoadSceneAsync("GameScene"); yield return null;
            var game = Object.FindFirstObjectByType<GameBootstrap>(); Assert.IsNotNull(game);
            Assert.AreEqual(GameFlowState.TitleIdle, game.Session.Flow.State);
            Assert.IsNotNull(game.IntroTimeline); game.StartIntro();
            for (int i = 0; i < 82; i++) game.Intro.Tick(.1f);
            Assert.AreEqual(GameFlowState.Running, game.Session.Flow.State);
            Assert.Less(Vector3.Distance(game.CameraRig.FirstPerson.transform.position, game.Hero.Head), .001f);
            Assert.AreEqual(80, game.CameraRig.FirstPerson.Lens.FieldOfView);
            game.Session.Runner.Command(MotionCommand.Left);
            game.Session.Tick(.2f); Assert.AreEqual(-2.2f, game.Session.Runner.X, .0001f);
            game.Session.Runner.Command(MotionCommand.Jump); game.Session.Tick(.1f); Assert.Greater(game.Session.Runner.Y, .5f);
            for (int i = 0; i < 20; i++) game.Session.Tick(.1f);
            game.Session.Runner.SetStartZ(80);game.Session.Runner.Command(MotionCommand.Crouch); game.Session.Runner.Command(MotionCommand.HighKnee);
            Assert.IsTrue(game.Session.Runner.Crouching && game.Session.Runner.HighKnee);
            for (int i = 0; i < 11; i++) game.Session.Tick(.1f); Assert.IsFalse(game.Session.Runner.Crouching || game.Session.Runner.HighKnee);
            int created = game.Map.CreatedCount;
            for (int n = 0; n < 120; n++)
            {
                game.Map.Tick(n * 24f + 2);
                var active = game.Map.Active;
                Assert.GreaterOrEqual(active.Count, 4);
                for (int i = 0; i < active.Count; i++)
                {
                    var chunk = active[i].Chunk; Assert.AreEqual(24, chunk.Exit.position.z - chunk.Entry.position.z, .001f);
                    if (i > 0) Assert.Less(Vector3.Distance(active[i - 1].Chunk.Exit.position,chunk.Entry.position),.001f);
                    Assert.AreEqual(RouteSurface.EntryHeight(active[i].Type),chunk.Entry.position.y,.001f);
                    for (int row = 0; row < 2; row++)
                    {
                        int count = 0; for (int lane = 0; lane < 3; lane++) if (chunk.Obstacles[row * 3 + lane].gameObject.activeSelf) count++;
                        Assert.IsTrue(count<=1||count==3&&SafePatternValidator.IsActionRowSafe(7,chunk.Obstacles[row*3].Kind));
                    }
                }
                Assert.AreEqual(created, game.Map.CreatedCount);
            }
            for (int i = 0; i < 3; i++) { game.Session.Tick(.7f); game.Session.Hit(true); }
            Assert.AreEqual(GameFlowState.Caught, game.Session.Flow.State);
            yield return new WaitForSeconds(1.5f); Assert.AreEqual(GameFlowState.Result, game.Session.Flow.State);
            game.Retry(); Assert.AreEqual(GameFlowState.TitleIdle, game.Session.Flow.State);
            Assert.AreEqual(0, game.Session.Score.Score); Assert.AreEqual(0, game.Session.Chase.Pressure); Assert.AreEqual(2, game.Session.Runner.Z);
            Assert.AreEqual(created, game.Map.CreatedCount); Assert.AreEqual(0, game.Map.Active[0].Chunk.Entry.position.z);
            game.StartIntro(); for (int i = 0; i < 82; i++) game.Intro.Tick(.1f);
            Assert.AreEqual(GameFlowState.Running, game.Session.Flow.State);
        }
        [UnityTest] public IEnumerator CoinsUseSweptCrossingAndResetWhenPooled()
        {
            yield return SceneManager.LoadSceneAsync("GameScene"); yield return null;
            var game=Object.FindFirstObjectByType<GameBootstrap>();
            var node=new GameObject("Coin crossing regression"); var coin=node.AddComponent<CoinController>();
            try
            {
                node.transform.position=new Vector3(0,1,2.1f);
                coin.Evaluate(game.Session,0); Assert.AreEqual(0,game.Session.Score.Coins);
                game.StartIntro(); for(int i=0;i<82;i++) game.Intro.Tick(.1f);
                float before=game.Session.Runner.Z; node.transform.position=new Vector3(0,1,before+.1f);
                game.Session.Tick(.4f); int score=game.Session.Score.Score;
                coin.Evaluate(game.Session,before);
                Assert.AreEqual(1,game.Session.Score.Coins); Assert.AreEqual(score+10,game.Session.Score.Score);
                coin.Evaluate(game.Session,before); Assert.AreEqual(1,game.Session.Score.Coins);
                coin.ResetCoin(); node.SetActive(true);
                node.transform.position=new Vector3(4.4f,1,game.Session.Runner.Z+.1f);
                before=game.Session.Runner.Z; game.Session.Tick(.4f); coin.Evaluate(game.Session,before);
                Assert.AreEqual(1,game.Session.Score.Coins,"Wrong lane must not collect");
                coin.ResetCoin(); node.SetActive(true);
                before=game.Session.Runner.Z; node.transform.position=new Vector3(0,1,before+.1f);
                game.Session.Tick(.4f); coin.Evaluate(game.Session,before);
                Assert.AreEqual(2,game.Session.Score.Coins,"Reused coin must award again");
                game.Session.Flow.Transition(GameFlowState.Caught);
                game.Session.Flow.Transition(GameFlowState.Result);
                game.Retry(); Assert.AreEqual(0,game.Session.Score.Coins);
            }
            finally { Object.Destroy(node); }
        }
        [UnityTest] public IEnumerator AuthoredCoinsAndRunnerFollowCurvesAndStairElevation()
        {
            yield return SceneManager.LoadSceneAsync("GameScene"); yield return null;
            var game=Object.FindFirstObjectByType<GameBootstrap>(); game.StartIntro();
            for(int i=0;i<82;i++) game.Intro.Tick(.1f);
            game.enabled=false;
            foreach(int sequence in new[]{1,4,5,8})
            {
                int type=RouteSurface.TypeAt(sequence);
                for(float z=game.Map.Active[0].Chunk.Entry.position.z;z<=sequence*24;z+=12) game.Map.Tick(z+2);
                MuhanokMapChunk chunk=null;
                foreach(var active in game.Map.Active) if(active.Type==type) { chunk=active.Chunk; break; }
                Assert.IsNotNull(chunk); var coin=chunk.Coins[2]; Vector3 position=coin.transform.position;
                RouteSurface.Sample(position.z,out float offsetX,out float offsetY);
                int lane=Mathf.RoundToInt((position.x-offsetX)/2.2f);
                while(game.Session.Runner.Lane<lane) game.Session.Runner.Command(MotionCommand.Right);
                while(game.Session.Runner.Lane>lane) game.Session.Runner.Command(MotionCommand.Left);
                game.Session.Runner.Tick(.3f,0); game.Session.Runner.SetStartZ(position.z);
                game.Hero.Apply(game.Session.Runner);
                Assert.AreEqual(offsetY+(game.Session.Runner.Riding?.25f:0),game.Hero.transform.position.y,.001f);
                Assert.AreEqual(position.x,game.Hero.transform.position.x,.001f);
                int before=game.Session.Score.Coins; coin.Evaluate(game.Session,position.z-1);
                Assert.AreEqual(before+1,game.Session.Score.Coins,"Authored coin on curved or elevated route must collect");
                coin.Evaluate(game.Session,position.z-1); Assert.AreEqual(before+1,game.Session.Score.Coins);
            }
        }
        [UnityTest] public IEnumerator MissingCharacterAssetsUseFallback()
        {
            // Disable this scene's composition before loading a copy with empty art references.
            yield return SceneManager.LoadSceneAsync("GameScene"); yield return null;
            var template = Object.FindFirstObjectByType<GameBootstrap>();
            var copy = new GameObject("Fallback Test"); copy.SetActive(false);
            var game = copy.AddComponent<GameBootstrap>(); game.Settings = template.Settings; game.ChunkPrefabs = template.ChunkPrefabs;
            game.CameraRig = template.CameraRig; game.OutputCamera = template.OutputCamera; game.IntroTimeline = template.IntroTimeline;
            Object.Destroy(template.gameObject); yield return null;
            copy.SetActive(true); yield return null;
            Assert.AreEqual(GameFlowState.TitleIdle, game.Session.Flow.State); Assert.IsNotNull(game.Hero);
            game.StartIntro(); for (int i = 0; i < 82; i++) game.Intro.Tick(.1f);
            Assert.AreEqual(GameFlowState.Running, game.Session.Flow.State);
            Object.Destroy(copy); yield return null;
        }
    }
}
