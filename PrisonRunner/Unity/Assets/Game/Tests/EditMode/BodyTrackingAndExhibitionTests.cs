using System;
using System.Collections.Generic;
using System.IO;
using Muhanok.Domain;
using Muhanok.Infrastructure;
using NUnit.Framework;

namespace Muhanok.Tests
{
    public sealed class BodyTrackingAndExhibitionTests
    {
        // Synthetic standing body in normalised image coordinates; offsets move parts of it.
        private sealed class Body { public float Dx, Dy, Shoulder, Hip, KneeL, KneeR, Ankle, WristL = .6f, WristR = .6f; public bool Legs = true; }
        private static PoseFrame Frame(Body b, double time)
        {
            var frame = new PoseFrame { HasPose = true, Time = time, Aspect = 4f / 3f };
            void Set(int i, float x, float y, float v = .95f) => frame.Points[i] = new PoseLandmark { X = x + b.Dx, Y = y, Visibility = v };
            for (int i = 0; i < PoseFrame.Count; i++) Set(i, .5f, .5f, .2f);
            float sy = .35f + b.Dy + b.Shoulder, hy = .62f + b.Dy + b.Hip, legs = b.Legs ? .95f : .1f;
            Set(0, .5f, .25f + b.Dy + b.Shoulder); Set(11, .44f, sy); Set(12, .56f, sy);
            Set(23, .46f, hy, legs); Set(24, .54f, hy, legs);
            Set(25, .46f, .78f + b.Dy + b.Hip * .5f - b.KneeL, legs); Set(26, .54f, .78f + b.Dy + b.Hip * .5f - b.KneeR, legs);
            Set(27, .46f, .93f + b.Dy - b.Ankle, legs); Set(28, .54f, .93f + b.Dy - b.Ankle, legs);
            Set(15, .41f, b.WristL + b.Dy); Set(16, .59f, b.WristR + b.Dy);
            return frame;
        }
        private static List<string> Play(PlayStyle style, Func<double, Body> script, double seconds)
        {
            var classifier = new MotionClassifier(new MotionTuning()); classifier.SetStyle(style);
            for (int i = 0; i < 30; i++) classifier.Update(Frame(new Body { Legs = style == PlayStyle.FullBody }, i / 30.0));
            Assert.AreEqual(PoseStatus.Tracking, classifier.Status);
            var events = new List<string>(); int lane = 0; bool crouch = false, knee = false;
            for (double t = 0; t < seconds; t += 1 / 30.0)
            {
                var body = script(t); body.Legs = style == PlayStyle.FullBody;
                var r = classifier.Update(Frame(body, 1 + t));
                if (r.Lane != lane) { events.Add("lane" + r.Lane); lane = r.Lane; }
                if (r.JumpEdge) events.Add("jump");
                if (r.Crouch != crouch) { events.Add(r.Crouch ? "crouch" : "stand"); crouch = r.Crouch; }
                if (r.HighKnee != knee) { if (r.HighKnee) events.Add("knee"); knee = r.HighKnee; }
            }
            return events;
        }
        private static bool In(double t, double a, double b) => t >= a && t < b;

        [Test] public void FullBodyStepsSelectLanesAndReturnToCentre()
        {
            CollectionAssert.AreEqual(new[] { "lane-1", "lane0" }, Play(PlayStyle.FullBody, t => new Body { Dx = In(t, .2, .8) ? -.08f : 0 }, 1.5));
            CollectionAssert.AreEqual(new[] { "lane-1", "lane0", "lane1", "lane0" }, Play(PlayStyle.FullBody, t => new Body { Dx = In(t, .2, .6) ? -.08f : In(t, .6, 1) ? .08f : 0 }, 1.5));
        }
        [Test] public void DipBeforeJumpIsNotASlideAndRepeatedJumpsAllCount()
        {
            CollectionAssert.AreEqual(new[] { "jump" }, Play(PlayStyle.FullBody, t => new Body { Dy = In(t, .2, .3) ? .085f : In(t, .3, .6) ? -.06f : 0, Ankle = In(t, .3, .6) ? .06f : 0 }, 1.5));
            var three = Play(PlayStyle.FullBody, t => { double p = (t - .2) % .5; bool up = t > .2 && t < 1.6 && p < .28; return new Body { Dy = up ? -.06f : 0, Ankle = up ? .06f : 0 }; }, 1.8);
            Assert.AreEqual(3, three.FindAll(e => e == "jump").Count);
            Assert.IsFalse(three.Contains("crouch"));
        }
        [Test] public void HeldCrouchAndAlternatingKneesAreHeldActions()
        {
            CollectionAssert.AreEqual(new[] { "crouch", "stand" }, Play(PlayStyle.FullBody, t => new Body { Shoulder = In(t, .2, 1.2) ? .09f : 0, Hip = In(t, .2, 1.2) ? .04f : 0 }, 1.6));
            CollectionAssert.AreEqual(new[] { "knee" }, Play(PlayStyle.FullBody, t => { int s = (int)((t - .2) / .3) % 2; bool on = In(t, .2, 1.6); return new Body { KneeL = on && s == 0 ? .09f : 0, KneeR = on && s == 1 ? .09f : 0 }; }, 1.8));
        }
        [Test] public void StandingStillAndJoggingInPlaceTriggerNothing()
        {
            Assert.IsEmpty(Play(PlayStyle.FullBody, t => new Body(), 3));
            Assert.IsEmpty(Play(PlayStyle.FullBody, t => { int s = (int)(t / .3) % 2; return new Body { KneeL = s == 0 ? .025f : 0, KneeR = s == 1 ? .025f : 0 }; }, 3));
        }
        [Test] public void UpperBodyUsesLeanHandsDuckAndArmPumps()
        {
            CollectionAssert.AreEqual(new[] { "lane-1", "lane0" }, Play(PlayStyle.UpperBody, t => new Body { Dx = In(t, .2, .8) ? -.06f : 0 }, 1.3));
            CollectionAssert.AreEqual(new[] { "jump" }, Play(PlayStyle.UpperBody, t => new Body { WristL = In(t, .2, .6) ? .08f : .6f, WristR = In(t, .2, .6) ? .08f : .6f }, 1.2));
            CollectionAssert.AreEqual(new[] { "crouch", "stand" }, Play(PlayStyle.UpperBody, t => new Body { Shoulder = In(t, .2, 1) ? .07f : 0 }, 1.4));
            CollectionAssert.AreEqual(new[] { "knee" }, Play(PlayStyle.UpperBody, t => { int s = (int)(t / .3) % 2; bool on = In(t, .2, 1.8); return new Body { WristL = on ? (s == 0 ? .4f : .62f) : .6f, WristR = on ? (s == 1 ? .4f : .62f) : .6f }; }, 2.0));
        }
        [Test] public void LostPoseRecalibratesAfterTimeout()
        {
            var classifier = new MotionClassifier(new MotionTuning()); classifier.SetStyle(PlayStyle.FullBody);
            for (int i = 0; i < 30; i++) classifier.Update(Frame(new Body(), i / 30.0));
            Assert.AreEqual(PoseStatus.Tracking, classifier.Status);
            classifier.Update(new PoseFrame { HasPose = false, Time = 2 }); Assert.AreEqual(PoseStatus.Searching, classifier.Status);
            classifier.Update(new PoseFrame { HasPose = false, Time = 6 });
            classifier.Update(Frame(new Body(), 6.1)); Assert.AreEqual(PoseStatus.Calibrating, classifier.Status);
        }
        [Test] public void PoseCommandsStepTowardTheZoneAndUseAbsoluteLeanInTheCart()
        {
            var output = new PoseGameInput(); int lane = 0; bool cart = false;
            var source = new PoseCommandSource(output, () => lane, () => cart); source.Reset(0);
            source.Feed(new MotionReading { Lane = 1 }, 0);
            Assert.IsTrue(output.TryRead(out var command)); Assert.AreEqual(MotionCommand.Right, command); lane = 1;
            source.Feed(new MotionReading { Lane = -1 }, .1);
            Assert.IsTrue(output.TryRead(out command)); Assert.AreEqual(MotionCommand.Left, command);
            Assert.IsTrue(output.TryRead(out command)); Assert.AreEqual(MotionCommand.Left, command); lane = -1;
            cart = true; source.Feed(new MotionReading { Lane = 0 }, .2);
            Assert.IsTrue(output.TryRead(out command)); Assert.AreEqual(MotionCommand.Center, command);
            source.Feed(new MotionReading { Lane = 0, Crouch = true }, .3); source.Feed(new MotionReading { Lane = 0, Crouch = true }, .35);
            Assert.IsTrue(output.TryRead(out command)); Assert.AreEqual(MotionCommand.Crouch, command); Assert.IsFalse(output.TryRead(out _));
        }
        [Test] public void BufferedJumpRunsOnLandingOnlyWhenEnabled()
        {
            var runner = new RunnerController(new RunConfig { InputBufferSeconds = .2f });
            runner.Command(MotionCommand.Jump); for (int i = 0; i < 13; i++) runner.Tick(.05f, 0);
            Assert.Greater(runner.Y, 0f); runner.Command(MotionCommand.Jump);
            for (int i = 0; i < 4 && runner.Y > 0; i++) runner.Tick(.05f, 0);
            for (int i = 0; i < 2; i++) runner.Tick(.05f, 0);
            Assert.Greater(runner.Y, .3f, "The jump pressed just before landing must start on landing");
        }
        [Test] public void IntervalRouteKeepsAuthoredFirstLapThenAlternatesRestAndSprint()
        {
            var config = new RunConfig { IntervalMode = true };
            var plan = new RoutePlan(config, 5);
            for (int i = 0; i < RoutePlan.AuthoredLap.Length; i++) Assert.AreEqual(RoutePlan.AuthoredLap[i], plan.TypeAt(i));
            int seq = RoutePlan.AuthoredLap.Length; Assert.AreEqual(0, plan.TypeAt(seq));
            int rest = 0; while (plan.TypeAt(seq + 1 + rest) == 1 || plan.TypeAt(seq + 1 + rest) == 2) rest++;
            Assert.AreEqual((int)Math.Round(config.RestSeconds * config.RestSpeed / RouteSurface.Length), rest);
            for (int i = 0; i < 80; i++)
            {
                int a = plan.TypeAt(i), b = plan.TypeAt(i + 1);
                Assert.AreEqual(RouteSurface.EntryHeight(a) + RouteSurface.Rise(a), RouteSurface.EntryHeight(b), .001f, "Height break between " + a + " and " + b);
                int same = 0; for (int j = i; j < i + 6; j++) if (plan.TypeAt(j) == a) same++;
                Assert.LessOrEqual(same, 3, "More copies of type " + a + " than the chunk pool keeps warm");
            }
            Assert.AreEqual(1, plan.CycleAt(seq));
        }
        [Test] public void IntervalSpeedRestsInTheCartAndRisesEachSprint()
        {
            var config = new RunConfig { IntervalMode = true };
            try
            {
                RouteSurface.UsePlan(new RoutePlan(config, 1));
                var run = new RunSession(config);
                run.Flow.Transition(GameFlowState.TitleIdle); run.Flow.Transition(GameFlowState.LoadingIntro); run.Flow.Transition(GameFlowState.Running);
                Assert.AreEqual(config.SprintSpeed, run.TargetSpeed, .001f);
                run.Runner.SetStartZ(30); Assert.IsTrue(run.Resting); Assert.AreEqual(config.RestSpeed, run.TargetSpeed, .001f);
                run.Runner.SetStartZ(RoutePlan.AuthoredLap.Length * RouteSurface.Length + 1); Assert.AreEqual(config.SprintSpeed + config.SprintSpeedStep, run.TargetSpeed, .001f);
            }
            finally { RouteSurface.UsePlan(null); }
        }
        [Test] public void ItemsDoubleScoreShieldOneHitAndExpire()
        {
            var run = new RunSession(new RunConfig());
            run.Flow.Transition(GameFlowState.TitleIdle); run.Flow.Transition(GameFlowState.LoadingIntro); run.Flow.Transition(GameFlowState.Running);
            run.Items.Activate(ItemKind.DoubleScore); run.Tick(.01f); int before = run.Score.Score; run.Score.CollectCoin();
            Assert.AreEqual(before + 20, run.Score.Score);
            run.Items.Activate(ItemKind.Shield);
            Assert.IsFalse(run.Hit(true)); Assert.AreEqual(GameFlowState.Running, run.Flow.State); Assert.AreEqual(1, run.ShieldSaves);
            Assert.IsTrue(run.Hit(true)); Assert.AreEqual(GameFlowState.Caught, run.Flow.State);
            var items = new PowerUpState(); items.Activate(ItemKind.Magnet); items.Tick(100); Assert.IsFalse(items.IsActive(ItemKind.Magnet));
        }
        [Test] public void SharedCoinPoolUnlocksItemsAndUnlockAllOpensEverything()
        {
            var progress = new ExhibitionProgress(); Assert.IsEmpty(progress.Available());
            progress.Deposit(40); Assert.IsTrue(progress.TryUnlock(ItemKind.Magnet)); Assert.AreEqual(10, progress.CoinPool);
            Assert.IsFalse(progress.TryUnlock(ItemKind.Shield)); CollectionAssert.AreEqual(new[] { ItemKind.Magnet }, progress.Available());
            progress.UnlockAll = true; Assert.AreEqual(ItemCatalog.All.Length, progress.Available().Count);
        }
        [Test] public void DailyRankingSortsAndPersistsPerDay()
        {
            string root = Path.Combine(Path.GetTempPath(), "muhanok-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new ExhibitionStore(root);
                Assert.AreEqual(1, store.Record(new RankingEntry { Name = "A", Score = 100, Ticks = 1 }));
                Assert.AreEqual(1, store.Record(new RankingEntry { Name = "B", Score = 300, Ticks = 2 }));
                Assert.AreEqual(2, store.Record(new RankingEntry { Name = "C", Score = 200, Ticks = 3 }));
                store.DepositCoins(55);
                var reopened = new ExhibitionStore(root);
                CollectionAssert.AreEqual(new[] { "B", "C", "A" }, new List<RankingEntry>(reopened.Today.Entries).ConvertAll(e => e.Name));
                Assert.AreEqual(55, reopened.Progress.CoinPool);
                reopened.ClearToday(); Assert.AreEqual(0, new ExhibitionStore(root).Today.Entries.Count);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
