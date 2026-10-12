using System;

namespace Muhanok.Domain
{
    // Turns pose frames into game intent for one play style.
    // Lanes follow body position (left zone / centre / right zone) with hysteresis, so two lanes is simply a bigger step
    // and returning to the centre returns the runner. Jump is an edge; crouch and high knee are held states.
    public sealed class MotionClassifier
    {
        private readonly MotionTuning tuning;
        private readonly float[] px = new float[PoseFrame.Count], py = new float[PoseFrame.Count], vis = new float[PoseFrame.Count];
        private bool smoothed;
        private PlayStyle style = PlayStyle.FullBody;
        // Baseline (calibrated standing pose).
        private bool calibrated;
        private int samples;
        private float bCx, bSy, bHy, bScale, bKneeL, bKneeR, bAnkle, bNose;
        private float sumCx, sumSy, sumHy, sumScale, sumKneeL, sumKneeR, sumAnkle, sumNose;
        private int ankleSamples, noseSamples, kneeSamples;
        // Tracking state.
        private double lostSince = -1, crouchPendingSince = -1, landingUntil, kneeUntil, lastStep, lastJump = -10, lastLaneChange = -10, pumpUntil;
        private bool airborne, handsUp, duckOnLoss;
        // How far the last tracked frame was toward a duck (1 = threshold); a loss after a dip counts as ducking.
        private float dropRatio;
        private int stepSign, pumpSign;
        private readonly double[] stepTimes = new double[8];
        private int stepIndex;
        public PoseStatus Status { get; private set; } = PoseStatus.Searching;
        public float CalibrationProgress => calibrated ? 1f : Math.Min(1f, samples / (float)Math.Max(1, tuning.CalibrationFrames));
        public readonly MotionReading Reading = new MotionReading();
        public float StepsPerMinute { get; private set; }
        public PlayStyle Style => style;
        public MotionClassifier(MotionTuning motionTuning) { tuning = motionTuning ?? new MotionTuning(); }
        public void SetStyle(PlayStyle next) { style = next; Recalibrate(); }
        public void Recalibrate()
        {
            calibrated = false; samples = 0; smoothed = false; Reading.Clear(); airborne = handsUp = duckOnLoss = false; dropRatio = 0;
            crouchPendingSince = -1; kneeUntil = pumpUntil = 0; stepSign = pumpSign = 0; StepsPerMinute = 0;
            Status = PoseStatus.Calibrating;
        }

        public MotionReading Update(PoseFrame frame)
        {
            Reading.JumpEdge = false;
            double now = frame.Time;
            if (!frame.HasPose || !Smooth(frame) || !RequiredVisible())
            {
                if (lostSince < 0) { lostSince = now; duckOnLoss = calibrated && Status == PoseStatus.Tracking && dropRatio > .5f; }
                double lost = now - lostSince;
                if (calibrated && lost <= tuning.LostGraceSeconds && !duckOnLoss) return Reading;
                if (calibrated && duckOnLoss && lost <= tuning.DuckHoldSeconds) { Reading.Crouch = true; Reading.Active = true; Status = PoseStatus.Tracking; return Reading; }
                if (now - lostSince > tuning.LostSeconds && calibrated) Recalibrate();
                Status = PoseStatus.Searching; Reading.Crouch = Reading.HighKnee = Reading.Active = false; crouchPendingSince = -1;
                return Reading;
            }
            lostSince = -1;
            if (!calibrated) { Calibrate(frame.Aspect); return Reading; }
            Status = PoseStatus.Tracking;
            if (style == PlayStyle.UpperBody) TrackUpper(frame.Aspect, now); else TrackFull(frame.Aspect, now);
            UpdateCadence(now);
            Reading.Active = now - lastStep < .8 || now - lastJump < 1 || Reading.Crouch || Reading.HighKnee || now - lastLaneChange < 1 || now < pumpUntil;
            return Reading;
        }

        private bool Smooth(PoseFrame frame)
        {
            float a = smoothed ? Clamp(tuning.Smoothing, .05f, 1f) : 1f;
            for (int i = 0; i < PoseFrame.Count; i++)
            {
                var p = frame.Points[i];
                if (float.IsNaN(p.X) || float.IsNaN(p.Y) || float.IsInfinity(p.X) || float.IsInfinity(p.Y)) return false;
                // Faint joints keep their last position instead of jumping to a guess.
                if (!smoothed || p.Visibility >= MinVisibility * .6f) { px[i] += (p.X * frame.Aspect - px[i]) * a; py[i] += (p.Y - py[i]) * a; }
                vis[i] = p.Visibility;
            }
            smoothed = true; return true;
        }
        private float MinVisibility => style == PlayStyle.UpperBody ? tuning.UpperMinVisibility : tuning.MinVisibility;
        private bool Seen(int i) => vis[i] >= MinVisibility;
        private bool RequiredVisible() => style == PlayStyle.UpperBody
            ? Seen(PoseJoint.LeftShoulder) && Seen(PoseJoint.RightShoulder)
            : Seen(PoseJoint.LeftShoulder) && Seen(PoseJoint.RightShoulder) && Seen(PoseJoint.LeftHip) && Seen(PoseJoint.RightHip);

        private float ShoulderX => (px[PoseJoint.LeftShoulder] + px[PoseJoint.RightShoulder]) * .5f;
        private float ShoulderY => (py[PoseJoint.LeftShoulder] + py[PoseJoint.RightShoulder]) * .5f;
        private float HipX => (px[PoseJoint.LeftHip] + px[PoseJoint.RightHip]) * .5f;
        private float HipY => (py[PoseJoint.LeftHip] + py[PoseJoint.RightHip]) * .5f;
        private float ShoulderWidth => Distance(PoseJoint.LeftShoulder, PoseJoint.RightShoulder);
        private float Torso => (float)Math.Sqrt((ShoulderX - HipX) * (ShoulderX - HipX) + (ShoulderY - HipY) * (ShoulderY - HipY));
        private float Distance(int a, int b) => (float)Math.Sqrt((px[a] - px[b]) * (px[a] - px[b]) + (py[a] - py[b]) * (py[a] - py[b]));
        // Shoulders lead a lean; hips follow a step. The mix responds to both.
        private float CentreX => style == PlayStyle.UpperBody ? ShoulderX : ShoulderX * .7f + HipX * .3f;
        private float CurrentScale => style == PlayStyle.UpperBody ? ShoulderWidth : Torso;
        private bool KneesSeen => Seen(PoseJoint.LeftKnee) && Seen(PoseJoint.RightKnee);
        private bool AnklesSeen => Seen(PoseJoint.LeftAnkle) && Seen(PoseJoint.RightAnkle);
        private float AnkleY => (py[PoseJoint.LeftAnkle] + py[PoseJoint.RightAnkle]) * .5f;
        private float KneeDrop(int knee, int hip) => py[knee] - py[hip];

        private void Calibrate(float aspect)
        {
            Status = PoseStatus.Calibrating;
            float scale = CurrentScale;
            if (scale < .02f) { samples = 0; return; }
            // Restart when the player is still moving: the baseline must be a calm standing pose.
            if (samples > 0)
            {
                float meanX = sumCx / samples, meanY = sumSy / samples, meanScale = sumScale / samples;
                if (Math.Abs(CentreX - meanX) > tuning.CalibrationStillness * meanScale || Math.Abs(ShoulderY - meanY) > tuning.CalibrationStillness * meanScale) samples = 0;
            }
            if (samples == 0) { sumCx = sumSy = sumHy = sumScale = sumKneeL = sumKneeR = sumAnkle = sumNose = 0; ankleSamples = noseSamples = kneeSamples = 0; }
            samples++;
            sumCx += CentreX; sumSy += ShoulderY; sumScale += scale;
            if (style != PlayStyle.UpperBody) sumHy += HipY;
            if (KneesSeen && style != PlayStyle.UpperBody) { sumKneeL += KneeDrop(PoseJoint.LeftKnee, PoseJoint.LeftHip); sumKneeR += KneeDrop(PoseJoint.RightKnee, PoseJoint.RightHip); kneeSamples++; }
            if (AnklesSeen && style != PlayStyle.UpperBody) { sumAnkle += AnkleY; ankleSamples++; }
            if (Seen(PoseJoint.Nose)) { sumNose += py[PoseJoint.Nose]; noseSamples++; }
            if (samples < tuning.CalibrationFrames) return;
            bCx = sumCx / samples; bSy = sumSy / samples; bHy = sumHy / samples; bScale = sumScale / samples;
            bKneeL = kneeSamples > 0 ? sumKneeL / kneeSamples : float.NaN; bKneeR = kneeSamples > 0 ? sumKneeR / kneeSamples : float.NaN;
            bAnkle = ankleSamples > 0 ? sumAnkle / ankleSamples : float.NaN; bNose = noseSamples > 0 ? sumNose / noseSamples : float.NaN;
            calibrated = true; Reading.Clear(); Status = PoseStatus.Tracking;
        }

        private float Scale() => Clamp(CurrentScale, bScale * .6f, bScale * 1.6f);

        private void UpdateLane(float offset, float enter, float exit, double now)
        {
            int lane = Reading.Lane;
            if (lane == 0) { if (offset < -enter) lane = -1; else if (offset > enter) lane = 1; }
            else if (lane < 0) { if (offset > -exit) lane = offset > enter ? 1 : 0; }
            else if (offset < exit) lane = offset < -enter ? -1 : 0;
            if (lane != Reading.Lane) { Reading.Lane = lane; lastLaneChange = now; }
        }

        private void TrackFull(float aspect, double now)
        {
            float scale = Scale();
            float offset = (CentreX - bCx) / scale;
            UpdateLane(offset, tuning.LaneEnter, tuning.LaneExit, now);
            float drop = (ShoulderY - bSy) / scale, hipRise = (bHy - HipY) / scale, shoulderRise = (bSy - ShoulderY) / scale;
            dropRatio = drop / tuning.CrouchDrop;
            bool feetUp = !AnklesSeen || float.IsNaN(bAnkle) || (bAnkle - AnkleY) / scale > tuning.JumpRise * .5f;
            bool up = hipRise > tuning.JumpRise && shoulderRise > tuning.JumpRise * .8f && feetUp;
            if (up && !airborne && now - lastJump > .25)
            {
                Reading.JumpEdge = true; lastJump = now;
                // A dip right before take-off is part of the jump, not a slide.
                if (!Reading.Crouch) crouchPendingSince = -1;
            }
            if (up) airborne = true;
            else if (airborne && hipRise < tuning.JumpRise * .4f) { airborne = false; landingUntil = now + tuning.LandingGraceSeconds; }
            UpdateCrouch(drop > tuning.CrouchDrop && !airborne && now >= landingUntil, drop < tuning.CrouchDrop * .65f, now);
            // High knee: one knee rises toward its hip; squats and tucks move both knees, so require asymmetry.
            if (KneesSeen && !float.IsNaN(bKneeL))
            {
                float liftL = (bKneeL - KneeDrop(PoseJoint.LeftKnee, PoseJoint.LeftHip)) / scale;
                float liftR = (bKneeR - KneeDrop(PoseJoint.RightKnee, PoseJoint.RightHip)) / scale;
                float diff = liftL - liftR;
                if (!airborne && Math.Max(liftL, liftR) > tuning.HighKneeLift && Math.Abs(diff) > tuning.HighKneeLift * .7f) kneeUntil = now + tuning.HighKneeHoldSeconds;
                // Alternating small lifts are running in place.
                int sign = Math.Abs(diff) > tuning.StepLift ? Math.Sign(diff) : 0;
                if (sign != 0 && sign != stepSign) { if (stepSign != 0) RegisterStep(now); stepSign = sign; }
            }
            Reading.HighKnee = now < kneeUntil;
            if (Reading.Lane == 0 && !airborne && !Reading.Crouch && !Reading.HighKnee && Math.Abs(offset) < tuning.LaneExit * .5f) Drift();
        }

        private void TrackUpper(float aspect, double now)
        {
            float scale = Scale();
            float offset = (CentreX - bCx) / scale;
            UpdateLane(offset, tuning.UpperLaneEnter, tuning.UpperLaneExit, now);
            float drop = (ShoulderY - bSy) / scale;
            float noseDrop = Seen(PoseJoint.Nose) && !float.IsNaN(bNose) ? (py[PoseJoint.Nose] - bNose) / scale : 0f;
            bool noseDuck = noseDrop > tuning.UpperCrouchDrop * 1.3f;
            dropRatio = Math.Max(drop / tuning.UpperCrouchDrop, noseDrop / (tuning.UpperCrouchDrop * 1.3f));
            bool wristsSeen = Seen(PoseJoint.LeftWrist) && Seen(PoseJoint.RightWrist);
            float head = Seen(PoseJoint.Nose) ? py[PoseJoint.Nose] : ShoulderY - scale;
            bool hands = wristsSeen && py[PoseJoint.LeftWrist] < head - tuning.UpperHandsAboveNose * scale && py[PoseJoint.RightWrist] < head - tuning.UpperHandsAboveNose * scale;
            if (hands && !handsUp && now - lastJump > .25) { Reading.JumpEdge = true; lastJump = now; crouchPendingSince = -1; }
            if (hands) handsUp = true;
            else if (!wristsSeen || py[PoseJoint.LeftWrist] > ShoulderY || py[PoseJoint.RightWrist] > ShoulderY) handsUp = false;
            UpdateCrouch((drop > tuning.UpperCrouchDrop || noseDuck) && !handsUp, drop < tuning.UpperCrouchDrop * .65f && !noseDuck, now);
            // Arm pumping replaces knee lifts: big alternation is the stair exercise, small alternation counts as running.
            if (wristsSeen && !handsUp)
            {
                float diff = (py[PoseJoint.LeftWrist] - py[PoseJoint.RightWrist]) / scale;
                int sign = Math.Abs(diff) > tuning.ArmSwingAmplitude ? Math.Sign(diff) : 0;
                if (sign != 0 && sign != pumpSign)
                {
                    if (pumpSign != 0) { RegisterStep(now); pumpUntil = now + .8; if (Math.Abs(diff) > tuning.ArmPumpAmplitude) kneeUntil = now + tuning.HighKneeHoldSeconds + .25; }
                    pumpSign = sign;
                }
            }
            Reading.HighKnee = now < kneeUntil;
            if (Reading.Lane == 0 && !handsUp && !Reading.Crouch && Math.Abs(offset) < tuning.UpperLaneExit * .5f) Drift();
        }

        private void UpdateCrouch(bool down, bool released, double now)
        {
            if (Reading.Crouch) { if (released) Reading.Crouch = false; return; }
            if (!down) { crouchPendingSince = -1; return; }
            if (crouchPendingSince < 0) crouchPendingSince = now;
            if (now - crouchPendingSince >= tuning.CrouchConfirmSeconds) { Reading.Crouch = true; crouchPendingSince = -1; }
        }

        // Slowly follow a player who drifts while standing still, so the centre zone stays centred.
        private void Drift()
        {
            float d = tuning.BaselineDrift;
            bCx += (CentreX - bCx) * d * .5f; bSy += (ShoulderY - bSy) * d; bScale += (CurrentScale - bScale) * d;
            if (style != PlayStyle.UpperBody) bHy += (HipY - bHy) * d;
            if (AnklesSeen && !float.IsNaN(bAnkle)) bAnkle += (AnkleY - bAnkle) * d;
            if (Seen(PoseJoint.Nose) && !float.IsNaN(bNose)) bNose += (py[PoseJoint.Nose] - bNose) * d;
        }

        private void RegisterStep(double now) { lastStep = now; stepTimes[stepIndex] = now; stepIndex = (stepIndex + 1) % stepTimes.Length; }
        private void UpdateCadence(double now)
        {
            int recent = 0; foreach (double t in stepTimes) if (t > 0 && now - t < 3) recent++;
            StepsPerMinute = recent * 20f;
        }
        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
