using System;

namespace Muhanok.Domain
{
    public struct PoseLandmark { public float X, Y, Visibility; }

    // BlazePose's 33 landmarks in the player's mirror view: X grows to the player's right, Y grows downward, both 0..1.
    public sealed class PoseFrame
    {
        public const int Count = 33;
        public readonly PoseLandmark[] Points = new PoseLandmark[Count];
        public float Aspect = 4f / 3f;
        public bool HasPose;
        public double Time;
        public void CopyFrom(PoseFrame other)
        {
            Array.Copy(other.Points, Points, Count); Aspect = other.Aspect; HasPose = other.HasPose; Time = other.Time;
        }
    }

    public static class PoseJoint
    {
        public const int Nose = 0, LeftShoulder = 11, RightShoulder = 12, LeftElbow = 13, RightElbow = 14, LeftWrist = 15, RightWrist = 16;
        public const int LeftHip = 23, RightHip = 24, LeftKnee = 25, RightKnee = 26, LeftAnkle = 27, RightAnkle = 28;
        public static readonly int[,] Bones =
        {
            { 11, 12 }, { 11, 13 }, { 13, 15 }, { 12, 14 }, { 14, 16 }, { 11, 23 }, { 12, 24 }, { 23, 24 },
            { 23, 25 }, { 25, 27 }, { 24, 26 }, { 26, 28 }, { 0, 11 }, { 0, 12 },
        };
    }

    public enum PoseStatus { Off, Starting, NoCamera, NoModel, Searching, Calibrating, Tracking }

    public static class PoseStatusText
    {
        public static string Korean(PoseStatus status) =>
            status == PoseStatus.Off ? "카메라 꺼짐" : status == PoseStatus.Starting ? "카메라 준비 중" : status == PoseStatus.NoCamera ? "웹캠 없음"
            : status == PoseStatus.NoModel ? "포즈 모듈 없음" : status == PoseStatus.Searching ? "사람을 찾는 중" : status == PoseStatus.Calibrating ? "가만히 서 주세요 (자세 맞추는 중)" : "인식 완료";
    }

    [Serializable]
    public sealed class PoseCameraOptions
    {
        public string DeviceName = "";
        public int Width = 640, Height = 480, Fps = 30;
        public bool FullLandmarkModel = true;
        public float DetectionThreshold = .55f;
        public bool ConvertToSrgb = false;
    }

    // Thresholds are in body units: torso length (shoulder centre to hip centre) for the full body,
    // shoulder width for the upper body. Both are measured in image-height units, so the camera aspect does not matter.
    [Serializable]
    public sealed class MotionTuning
    {
        public float MinVisibility = .5f, Smoothing = .55f;
        public int CalibrationFrames = 20;
        public float CalibrationStillness = .12f;
        public float LaneEnter = .30f, LaneExit = .17f;
        public float CrouchDrop = .26f, CrouchConfirmSeconds = .12f, LandingGraceSeconds = .3f;
        public float JumpRise = .15f;
        public float HighKneeLift = .22f, HighKneeHoldSeconds = .35f, StepLift = .06f;
        public float UpperLaneEnter = .38f, UpperLaneExit = .22f, UpperCrouchDrop = .35f, UpperHandsAboveNose = .2f;
        public float ArmPumpAmplitude = .6f, ArmSwingAmplitude = .3f;
        public float LostSeconds = 2.5f, BaselineDrift = .01f;
    }

    public sealed class MotionReading
    {
        public int Lane;
        public bool JumpEdge, Crouch, HighKnee, Active;
        public void Clear() { Lane = 0; JumpEdge = Crouch = HighKnee = Active = false; }
    }
}
