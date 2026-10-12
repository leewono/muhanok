using System;
using PrisonRunner.Core;

namespace Muhanok.Domain
{
    public enum GameFlowState { Boot, TitleIdle, IntroTimeline, FirstPersonTakeover, Running, Caught, Result, Retry, LoadingIntro }
    public enum MotionCommand { Center, Left, Right, Jump, Crouch, HighKnee }
    public enum ObstacleKind { Crate, Pipe, Barrier, Cart, Laser, Stair }
    public enum CaptureReason { Collision, BrokenRail, BananaPursuit }

    public sealed class GameFlowController
    {
        public GameFlowState State { get; private set; } = GameFlowState.Boot;
        public event Action<GameFlowState> Changed;
        public bool Transition(GameFlowState next)
        {
            bool valid = (State == GameFlowState.Boot && next == GameFlowState.TitleIdle)
                || (State == GameFlowState.TitleIdle && next == GameFlowState.IntroTimeline)
                || (State == GameFlowState.TitleIdle && next == GameFlowState.LoadingIntro)
                || (State == GameFlowState.LoadingIntro && next == GameFlowState.Running)
                || (State == GameFlowState.IntroTimeline && next == GameFlowState.FirstPersonTakeover)
                || (State == GameFlowState.FirstPersonTakeover && next == GameFlowState.Running)
                || (State == GameFlowState.Running && next == GameFlowState.Caught)
                || (State == GameFlowState.Caught && next == GameFlowState.Result)
                || (State == GameFlowState.Result && next == GameFlowState.Retry)
                || (State == GameFlowState.Retry && next == GameFlowState.TitleIdle);
            if (!valid) return false;
            State = next;
            Changed?.Invoke(State);
            return true;
        }
    }

    [Serializable]
    public sealed class RunConfig
    {
        public float LaneSpacing = 2.2f, LaneSeconds = .20f, BaseSpeed = 9f, MaxSpeed = 17f;
        public float JumpHeight = 1.8f, Gravity = 24f, CrouchSeconds = .8f, HighKneeSeconds = .9f;
        public float LightHit = .18f, HeavyHit = .35f, Recovery = .018f, StumbleSeconds = .8f;
        public int CleanBonus = 25;
        public float SlipSeconds = 2.4f, SlipSpeedMultiplier = .5f, CartSpeed = 9f;
        public float BananaCatchSeconds=1.2f;
        public float SpeedRampDistance = 120f;
        // Jump/slide pressed shortly before landing runs on landing. Zero keeps the strict legacy rule.
        public float InputBufferSeconds = 0f;
        // Interval mode: walking sections are sprints, the mine cart ride is the rest.
        public bool IntervalMode = false;
        public float SprintSpeed = 6f, SprintSpeedStep = .5f, MaxSprintSpeed = 9.5f, RestSpeed = 5f;
        public float SprintSeconds = 40f, RestSeconds = 20f, SpeedChangeRate = 2.5f;
        public bool RandomizeLayout = false;
    }

    public sealed class ChaseSystem
    {
        public float Pressure { get; private set; }
        public bool IsCaught => Pressure >= 1f;
        public void Hit(float amount) { Pressure = Math.Min(1f, Math.Max(0f, Pressure + amount)); }
        public void Recover(float amount) { if (!IsCaught) Hit(-Math.Max(0f, amount)); }
        public void Reset() { Pressure = 0f; }
    }

    public sealed class ScoreSystem
    {
        public float Distance { get; private set; }
        public int Coins { get; private set; }
        public int Score => (int)(Distance * 10f) + bonus + (int)extra;
        // Score items multiply points earned while they are active; already earned points never change.
        public int Multiplier { get; set; } = 1;
        private int bonus;
        private float extra;
        public void Advance(float metres)
        {
            float step = Math.Max(0f, metres); Distance += step;
            if (Multiplier > 1) extra += step * 10f * (Multiplier - 1);
        }
        public void CleanPass(int points) { bonus += Math.Max(0, points) * Math.Max(1, Multiplier); }
        public void CollectCoin() { Coins++; bonus += 10 * Math.Max(1, Multiplier); }
        public void Reset() { Distance = 0f; bonus = 0; Coins = 0; extra = 0f; Multiplier = 1; }
    }

    public sealed class DifficultyDirector
    {
        public float Speed(float distance, RunConfig config, bool riding = false)
            => Math.Min(config.MaxSpeed, (riding ? config.CartSpeed : config.BaseSpeed)
                + Math.Max(0f, distance) / Math.Max(1f, config.SpeedRampDistance));
        public float ObstacleChance(float distance) => Math.Min(.9f, .55f + distance / 2000f);
    }

    public static class SafePatternValidator
    {
        // A complete row always reserves an empty lane. Rows are separated by 12m.
        public static bool IsSafe(int occupiedLaneMask) => (occupiedLaneMask & 7) != 7;
        public static bool Avoided(ObstacleKind kind, float height, bool crouch, bool highKnee)
        {
            switch (kind)
            {
                case ObstacleKind.Crate: return height > .9f;
                case ObstacleKind.Pipe: return crouch;
                case ObstacleKind.Stair: return highKnee;
                case ObstacleKind.Cart: return height > .9f;
                default: return false;
            }
        }
        public static bool IsActionRowSafe(int mask,ObstacleKind kind)=>IsSafe(mask)||kind==ObstacleKind.Crate||kind==ObstacleKind.Pipe||kind==ObstacleKind.Stair;
    }

    public sealed class RunnerController
    {
        private readonly RunConfig config;
        private float laneFrom, laneTime, verticalSpeed, crouchLeft, kneeLeft, stumbleLeft, slowLeft, jumpBuffer, crouchBuffer;
        public int Lane { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        public bool Crouching => crouchLeft > 0f;
        public bool HighKnee => kneeLeft > 0f;
        public bool Stumbling => stumbleLeft > 0f;
        public bool Riding => RouteSurface.IsCart(Z);
        public float CartLean=>Riding?X/config.LaneSpacing:0;
        public float WorldLaneOffset=>Riding?CartLean*.16f:X;
        public bool Slowing => slowLeft > 0;
        public float SlowRemaining => slowLeft;
        public float SpeedMultiplier => slowLeft>0?config.SlipSpeedMultiplier:1;
        public RunnerController(RunConfig tuning) { config = tuning; Reset(); }
        public void Command(MotionCommand command)
        {
            int next = Lane;
            if (command == MotionCommand.Left) next = LaneRules.Move(Lane, -1);
            if (command == MotionCommand.Right) next = LaneRules.Move(Lane, 1);
            if(Riding)
            {
                if(command==MotionCommand.Left)next=-1;
                if(command==MotionCommand.Right)next=1;
                if(command==MotionCommand.Center)next=0;
            }
            if (next != Lane) { laneFrom = X; laneTime = 0f; Lane = next; }
            if (command == MotionCommand.Jump && !Riding)
            {
                if (Y <= .001f) StartJump();
                else jumpBuffer = config.InputBufferSeconds;
            }
            if (command == MotionCommand.Crouch)
            {
                if (Y <= .001f && verticalSpeed <= 0f) crouchLeft = config.CrouchSeconds;
                else crouchBuffer = config.InputBufferSeconds;
            }
            if (command == MotionCommand.HighKnee && !Riding) kneeLeft = config.HighKneeSeconds;
        }
        public void Stumble() { stumbleLeft = config.StumbleSeconds; }
        public void Slip() { slowLeft = config.SlipSeconds; }
        public void SetStartZ(float z) { Z = z; }
        public void Tick(float dt, float speed)
        {
            if (dt <= 0f) return;
            laneTime += dt;
            float t = Math.Min(1f, laneTime / Math.Max(.01f, config.LaneSeconds));
            t = t * t * (3f - 2f * t);
            X = laneFrom + (Lane * config.LaneSpacing - laneFrom) * t;
            // Analytic gravity keeps the arc stable even at low frame rates.
            Y = Math.Max(0f, Y + verticalSpeed * dt - config.Gravity * dt * dt * .5f);
            verticalSpeed = Y <= 0f ? Math.Max(0f, verticalSpeed - config.Gravity * dt) : verticalSpeed - config.Gravity * dt;
            if(Riding){Y=0;verticalSpeed=0;kneeLeft=0;jumpBuffer=0;}
            if (Y <= .001f && verticalSpeed <= 0f)
            {
                if (jumpBuffer > 0f) { jumpBuffer = 0f; crouchBuffer = 0f; StartJump(); }
                else if (crouchBuffer > 0f) { crouchBuffer = 0f; crouchLeft = config.CrouchSeconds; }
            }
            jumpBuffer = Math.Max(0f, jumpBuffer - dt);
            crouchBuffer = Math.Max(0f, crouchBuffer - dt);
            bool wasRiding=Riding;
            Z += speed * SpeedMultiplier * dt;
            if(wasRiding!=Riding){laneFrom=X;Lane=0;laneTime=0;}
            crouchLeft = Math.Max(0f, crouchLeft - dt);
            kneeLeft = Math.Max(0f, kneeLeft - dt);
            stumbleLeft = Math.Max(0f, stumbleLeft - dt);
            slowLeft = Math.Max(0f, slowLeft - dt);
        }
        private void StartJump()
        {
            crouchLeft = 0f;
            verticalSpeed = (float)Math.Sqrt(2f * config.Gravity * config.JumpHeight);
        }
        public void Reset() { Lane = 0; X = Y = 0f; Z = 2f; laneFrom = verticalSpeed = crouchLeft = kneeLeft = stumbleLeft = slowLeft = jumpBuffer = crouchBuffer = 0f; laneTime = 1f; }
    }

    public sealed class RunSession
    {
        public readonly GameFlowController Flow = new GameFlowController();
        public readonly ChaseSystem Chase = new ChaseSystem();
        public readonly ScoreSystem Score = new ScoreSystem();
        public readonly DifficultyDirector Difficulty = new DifficultyDirector();
        public readonly RunnerController Runner;
        public readonly RunConfig Config;
        public readonly WorkoutEstimate Workout=new WorkoutEstimate();
        public readonly PowerUpState Items=new PowerUpState();
        public bool Paused {get; set;}
        public int BananaHits {get;private set;}
        public int ShieldSaves {get;private set;}
        public bool BananaPursuit=>bananaCatchLeft>0;
        public float TargetSpeed => Config.IntervalMode ? IntervalSpeed() : Difficulty.Speed(Score.Distance, Config, Runner.Riding);
        public float CurrentSpeed => Flow.State == GameFlowState.Running && !Paused ? MoveSpeed * Runner.SpeedMultiplier : 0f;
        private float MoveSpeed => Config.IntervalMode ? smoothSpeed : TargetSpeed;
        // Interval rhythm comes from the route: walking chunks sprint, the cart ride rests.
        public bool Resting => Config.IntervalMode && Runner.Riding;
        public int IntervalCycle => RouteSurface.CycleAt(RouteSurface.SequenceAt(Runner.Z));
        public float PhaseRemainingSeconds => RouteSurface.SegmentRemaining(Runner.Z) / Math.Max(1f, TargetSpeed);
        public CaptureReason Reason {get;private set;}
        private float bananaCatchLeft, smoothSpeed;
        private float lastHit = -100f, clock;
        public RunSession(RunConfig config) { Config = config; Runner = new RunnerController(config); smoothSpeed = TargetSpeed; }
        private float IntervalSpeed()
        {
            if (Runner.Riding) return Config.RestSpeed;
            return Math.Min(Config.MaxSprintSpeed, Config.SprintSpeed + Config.SprintSpeedStep * IntervalCycle);
        }
        public void Tick(float dt)
        {
            if (Flow.State != GameFlowState.Running || Paused) return;
            clock += dt;
            float target = TargetSpeed;
            smoothSpeed = smoothSpeed < target ? Math.Min(target, smoothSpeed + Config.SpeedChangeRate * dt) : Math.Max(target, smoothSpeed - Config.SpeedChangeRate * 1.6f * dt);
            Items.Tick(dt); Score.Multiplier = Items.IsActive(ItemKind.DoubleScore) ? 2 : 1;
            float z = Runner.Z;
            Runner.Tick(dt, MoveSpeed);
            Score.Advance(Runner.Z - z);
            if(BananaPursuit)
            {
                bananaCatchLeft=Math.Max(0,bananaCatchLeft-dt);
                Chase.Hit(.55f*dt/Math.Max(.1f,Config.BananaCatchSeconds));
                if(bananaCatchLeft<=.0001f)Fail(CaptureReason.BananaPursuit);
            }
            else if (BananaHits==0&&clock - lastHit > 2f) Chase.Recover(Config.Recovery * dt);
        }
        public bool Hit(bool heavy)
        {return Fail(CaptureReason.Collision);}
        public bool Fail(CaptureReason reason)
        {
            if (Flow.State != GameFlowState.Running || Paused) return false;
            if (reason == CaptureReason.Collision && Items.ConsumeShield())
            {
                // The shield absorbs one solid hit: a stumble instead of a capture.
                ShieldSaves++; Runner.Stumble(); lastHit = clock; return false;
            }
            Reason=reason;
            lastHit = clock;
            Runner.Stumble(); Chase.Hit(1f);
            Flow.Transition(GameFlowState.Caught);
            return true;
        }
        public bool Slip(bool banana=false)
        {
            if(Flow.State!=GameFlowState.Running||Paused||Runner.Riding)return false;
            Runner.Slip();
            if(banana)
            {
                BananaHits++;
                if(BananaHits==1)Chase.Hit(.45f);
                if(BananaHits>=2&&!BananaPursuit)bananaCatchLeft=Math.Max(.1f,Config.BananaCatchSeconds);
            }
            return true;
        }
        public void Reset() { Runner.Reset(); Chase.Reset(); Score.Reset(); Workout.Reset(); Items.Reset(); Paused=false; clock = 0f; lastHit = -100f;BananaHits=0;ShieldSaves=0;bananaCatchLeft=0;Reason=CaptureReason.Collision; smoothSpeed = TargetSpeed; }
    }
}
