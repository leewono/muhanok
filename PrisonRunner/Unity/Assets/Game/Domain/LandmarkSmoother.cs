using System;

namespace Muhanok.Domain
{
    // One Euro filter per landmark coordinate, as in MediaPipe's pose landmark smoothing:
    // a low cutoff removes jitter while standing, and the cutoff rises with speed so fast moves are not delayed.
    // Values are scaled by the body size so the same settings work near and far from the camera.
    public sealed class LandmarkSmoother
    {
        private sealed class OneEuro
        {
            private bool started;
            private double value, speed;
            public void Reset() => started = false;
            public float Filter(float raw, double dt, double scale, MotionTuning t)
            {
                double x = raw * scale;
                if (!started) { started = true; value = x; speed = 0; return raw; }
                double dx = (x - value) / dt;
                speed += Alpha(t.FilterDerivateCutoff, dt) * (dx - speed);
                double cutoff = t.FilterMinCutoff + t.FilterBeta * Math.Abs(speed);
                value += Alpha(cutoff, dt) * (x - value);
                return (float)(value / scale);
            }
            private static double Alpha(double cutoff, double dt) => 1.0 / (1.0 + 1.0 / (2 * Math.PI * Math.Max(1e-4, cutoff) * dt));
        }

        private readonly OneEuro[] x = new OneEuro[PoseFrame.Count], y = new OneEuro[PoseFrame.Count];
        private double lastTime = -1;
        public LandmarkSmoother() { for (int i = 0; i < PoseFrame.Count; i++) { x[i] = new OneEuro(); y[i] = new OneEuro(); } }
        public void Reset() { lastTime = -1; foreach (var f in x) f.Reset(); foreach (var f in y) f.Reset(); }
        public void Apply(PoseFrame frame, MotionTuning tuning)
        {
            if (!frame.HasPose) return;
            double dt = lastTime < 0 ? 1 / 30.0 : frame.Time - lastTime;
            // A long gap means a new sighting: start over instead of easing from a stale position.
            if (dt <= 0 || dt > .5) { Reset(); dt = 1 / 30.0; }
            lastTime = frame.Time;
            double scale = 1.0 / Math.Max(.05f, frame.ObjectScale);
            for (int i = 0; i < PoseFrame.Count; i++)
            {
                var p = frame.Points[i];
                p.X = x[i].Filter(p.X, dt, scale, tuning); p.Y = y[i].Filter(p.Y, dt, scale, tuning);
                frame.Points[i] = p;
            }
        }
    }
}
