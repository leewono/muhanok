using System;
using System.Collections.Generic;
using Muhanok.Application;
using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Infrastructure
{
    public sealed class MotionCommandBuffer
    {
        private readonly Queue<MotionCommand> commands = new Queue<MotionCommand>(32);
        public void Add(MotionCommand command) { if (commands.Count < 32) commands.Enqueue(command); }
        public bool Read(out MotionCommand command) { command = MotionCommand.Center; if (commands.Count == 0) return false; command = commands.Dequeue(); return true; }
        public void Clear() { commands.Clear(); }
    }
    public sealed class PoseGameInput : IGameInput
    {
        public readonly MotionCommandBuffer Buffer = new MotionCommandBuffer();
        public bool TryRead(out MotionCommand command) => Buffer.Read(out command);
        public void Clear() => Buffer.Clear();
    }

    // Camera + neural network that produces pose frames. Implemented by the Inference Engine assembly.
    public interface IPoseEstimator : IDisposable
    {
        PoseStatus Status { get; }
        string Message { get; }
        Texture CameraTexture { get; }
        // One line for the preview: tracking mode, scores and inference rate.
        string Diagnostics { get; }
        void Start();
        void Stop();
        void Tick(double now);
        bool TryTakeFrame(PoseFrame target);
    }
    public static class PoseEstimatorRegistry
    {
        // Set by Muhanok.PoseInference when the Inference Engine package is installed.
        public static Func<PoseCameraOptions, IPoseEstimator> Factory;
    }

    // Converts classifier readings into the same commands the keyboard produces.
    // Walking lanes are relative steps toward the body zone; the cart uses absolute lean, as before.
    public sealed class PoseCommandSource
    {
        private readonly PoseGameInput output;
        private readonly Func<int> runnerLane;
        private readonly Func<bool> riding;
        private int lastZone;
        private bool lastRiding;
        private double lastCrouch = -10, lastKnee = -10;
        public PoseCommandSource(PoseGameInput target, Func<int> lane, Func<bool> cart) { output = target; runnerLane = lane; riding = cart; }
        public void Reset(int zone) { lastZone = zone; lastRiding = riding(); lastCrouch = lastKnee = -10; output.Clear(); }
        public void Feed(MotionReading reading, double now)
        {
            bool cart = riding();
            if (reading.Lane != lastZone || cart != lastRiding)
            {
                if (cart) output.Buffer.Add(reading.Lane < 0 ? MotionCommand.Left : reading.Lane > 0 ? MotionCommand.Right : MotionCommand.Center);
                else
                {
                    int steps = reading.Lane - runnerLane();
                    for (int i = 0; i < Math.Abs(steps); i++) output.Buffer.Add(steps < 0 ? MotionCommand.Left : MotionCommand.Right);
                }
                lastZone = reading.Lane; lastRiding = cart;
            }
            if (reading.JumpEdge) output.Buffer.Add(MotionCommand.Jump);
            // Held actions refresh the runner's timers while the body keeps the pose.
            if (reading.Crouch && now - lastCrouch >= .2) { output.Buffer.Add(MotionCommand.Crouch); lastCrouch = now; }
            if (reading.HighKnee && now - lastKnee >= .2) { output.Buffer.Add(MotionCommand.HighKnee); lastKnee = now; }
        }
    }

    // Owns the estimator and classifier for the chosen play style.
    public sealed class PoseTracker : IDisposable
    {
        private readonly PoseCameraOptions options;
        private readonly MotionClassifier classifier;
        private readonly MotionTuning tuning;
        private readonly LandmarkSmoother smoother = new LandmarkSmoother();
        private readonly PoseCommandSource commands;
        private readonly PoseFrame incoming = new PoseFrame();
        private IPoseEstimator estimator;
        private bool estimatorFailed;
        public readonly PoseFrame LastFrame = new PoseFrame();
        public PlayStyle Style { get; private set; } = PlayStyle.Keyboard;
        public MotionReading Reading => classifier.Reading;
        public float CalibrationProgress => classifier.CalibrationProgress;
        public float StepsPerMinute => classifier.StepsPerMinute;
        public double LastJumpTime { get; private set; } = -10;
        public Texture CameraTexture => estimator?.CameraTexture;
        public string Message => estimator?.Message ?? "";
        public string Diagnostics => estimator?.Diagnostics ?? "";
        public bool UsesCamera => PlayStyleInfo.UsesCamera(Style);
        public bool Tracking => Status == PoseStatus.Tracking;
        public PoseStatus Status
        {
            get
            {
                if (!UsesCamera) return PoseStatus.Off;
                if (estimator == null) return estimatorFailed ? PoseStatus.NoModel : PoseStatus.Starting;
                var camera = estimator.Status;
                return camera == PoseStatus.Tracking ? classifier.Status : camera;
            }
        }
        public PoseTracker(PoseGameInput output, PoseCameraOptions camera, MotionTuning tuning, Func<int> lane, Func<bool> riding)
        {
            options = camera ?? new PoseCameraOptions(); this.tuning = tuning ?? new MotionTuning(); classifier = new MotionClassifier(this.tuning);
            commands = new PoseCommandSource(output, lane, riding);
        }
        public void SetStyle(PlayStyle style)
        {
            if (style == Style && (estimator != null || !UsesCamera)) return;
            Style = style; classifier.SetStyle(style); commands.Reset(0);
            if (!UsesCamera) { estimator?.Stop(); return; }
            if (estimator == null && !estimatorFailed)
            {
                try { estimator = PoseEstimatorRegistry.Factory?.Invoke(options); }
                catch (Exception error) { Debug.LogException(error); estimator = null; }
                estimatorFailed = estimator == null;
            }
            estimator?.Start();
        }
        public void Recalibrate() { classifier.Recalibrate(); smoother.Reset(); commands.Reset(0); }
        public void ResetCommands() => commands.Reset(classifier.Reading.Lane);
        public void Tick(double now, bool feedCommands)
        {
            if (!UsesCamera || estimator == null) return;
            estimator.Tick(now);
            if (!estimator.TryTakeFrame(incoming)) return;
            smoother.Apply(incoming, tuning);
            LastFrame.CopyFrom(incoming);
            var reading = classifier.Update(incoming);
            if (reading.JumpEdge) LastJumpTime = now;
            if (feedCommands && classifier.Status == PoseStatus.Tracking) commands.Feed(reading, now);
            else commands.Reset(reading.Lane);
        }
        public void Dispose() { estimator?.Dispose(); estimator = null; }
    }
}
