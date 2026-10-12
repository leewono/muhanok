using System;
using System.Collections.Generic;
using System.IO;
using Muhanok.Bootstrap;
using Muhanok.Content;
using Muhanok.Domain;
using Muhanok.Presentation;
using Muhanok.Presentation.Map;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace Muhanok.Editor
{
    public static class MuhanokContentBuilder
    {
        private const string Content = "Assets/Game/Content";
        private static Material stone, wood, steel, orange, navy, skin, brown, yellow, red, white;
        public static readonly string[] ChunkIds = { "CH_Mine_Start", "CH_Mine_Straight_A", "CH_Mine_Straight_B", "CH_Transition_MineToPrison",
            "CH_Prison_Corridor_A", "CH_Prison_Corridor_B", "CH_Checkpoint", "CH_Stair", "CH_Laundry", "CH_Kitchen", "CH_Maintenance", "CH_Yard", "CH_OuterWall" };
        private static readonly string[] MonkeyClips = { "MinePickaxe_Loop", "StopWork", "LookCart", "IdeaReact", "SprintStart", "Run", "LaneStep_L", "LaneStep_R", "Jump", "CrouchSlide", "HighKnee", "Stumble", "Caught" };
        private static readonly string[] GuardClips = { "Notice", "ChaseRun", "Catch" };

        public static void RepairIntroClip()
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(Content + "/Animations/Intro_8s.playable");
            var oldTracks = new List<TrackAsset>();
            foreach (var track in timeline.GetOutputTracks()) if (track is IntroTimelineTrack) oldTracks.Add(track);
            foreach (var track in oldTracks) timeline.DeleteTrack(track);
            var repaired = timeline.CreateTrack<IntroTimelineTrack>(null, "Mine / Cart / Idea / Escape / Chase / Takeover");
            var clip = repaired.CreateClip<IntroTimelineClip>(); clip.start = 0; clip.duration = 8; clip.displayName = "Continuous Intro";
            EditorUtility.SetDirty(timeline); AssetDatabase.SaveAssets(); Debug.Log("MUHANOK_TIMELINE_REPAIR_PASS");
        }

        [MenuItem("Muhanok/Build GameScene")]
        public static void Build()
        {
            foreach (string folder in new[] { "Prefabs", "Materials", "Animations", "Audio", "VFX", "Data" }) Directory.CreateDirectory(Content + "/" + folder);
            AssetDatabase.Refresh();
            stone = Mat("MineRock", new Color(.20f, .17f, .13f)); wood = Mat("WarmWood", new Color(.33f, .16f, .06f));
            steel = Mat("Steel", new Color(.13f, .22f, .29f)); orange = Mat("PrisonOrange", new Color(1, .27f, .035f));
            navy = Mat("PoliceNavy", new Color(.045f, .085f, .19f)); skin = Mat("Skin", new Color(.79f, .50f, .28f));
            brown = Mat("MonkeyBrown", new Color(.23f, .075f, .025f)); yellow = Mat("LampGold", new Color(1, .65f, .13f), true);
            red = Mat("AlarmRed", new Color(1, .035f, .02f), true); white = Mat("PaleSteel", new Color(.48f, .57f, .61f));
            var monkey = Save(Character(false)); var police = Save(Character(true));
            var cart = Save(Cart()); var pickaxe = Save(Pickaxe());
            var bulb = Save(Bulb()); var alarm = Save(Alarm());
            var dust = Save(Particles("VFX_Dust_Run", new Color(.61f, .5f, .36f), true));
            var hit = Save(Particles("VFX_Hit_Stumble", new Color(1, .55f, .13f), false));
            var hazards = new GameObject[6];
            hazards[0] = Save(Hazard("OBS_Crate_Low", ObstacleKind.Crate));
            hazards[1] = Save(Hazard("OBS_LowPipe", ObstacleKind.Pipe));
            hazards[2] = Save(Hazard("OBS_Barrier_Left", ObstacleKind.Barrier));
            hazards[3] = Save(Hazard("OBS_Cart_Crossing", ObstacleKind.Cart));
            hazards[4] = Save(Hazard("OBS_LaserGate", ObstacleKind.Laser));
            hazards[5] = Save(Hazard("OBS_Stair_HighKnee", ObstacleKind.Stair));
            Save(Hazard("OBS_Barrier_Right", ObstacleKind.Barrier));
            var chunks = new MuhanokMapChunk[ChunkIds.Length];
            for (int i = 0; i < chunks.Length; i++) chunks[i] = Save(Chunk(i, hazards, alarm)).GetComponent<MuhanokMapChunk>();
            Save(new GameObject("UI_Logo_Muhanok")); Save(new GameObject("UI_HUD_Set"));
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(Content + "/Data/GameSettings.asset");
            if (settings == null) { settings = ScriptableObject.CreateInstance<GameSettings>(); AssetDatabase.CreateAsset(settings, Content + "/Data/GameSettings.asset"); }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.32f, .35f, .40f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.035f, .055f, .075f); RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .018f;
            var sun = new GameObject("Soft Fill").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = .7f; sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.nearClipPlane = .06f; camera.farClipPlane = 140; camera.fieldOfView = 80;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = RenderSettings.fogColor;
            camera.gameObject.AddComponent<AudioListener>();
            var rig = new GameObject("Camera Rig").AddComponent<FirstPersonCameraRig>();
            rig.Menu = Vcam("VCAM_Menu", rig.transform, new Vector3(4.5f, 2.4f, -3.5f), new Vector3(0, 1, 2), 60);
            rig.IntroA = Vcam("VCAM_IntroA", rig.transform, new Vector3(4.7f, 2.6f, -2.8f), new Vector3(0, 1, 3), 65);
            rig.Chase = Vcam("VCAM_IntroChase", rig.transform, new Vector3(1.7f, 2.5f, -2.5f), new Vector3(0, 1.65f, 7), 65);
            rig.FirstPerson = Vcam("VCAM_FirstPerson", rig.transform, new Vector3(0, 1.65f, 2), new Vector3(0, 1.65f, 20), 80);
            var bootstrap = new GameObject("Muhanok Bootstrap").AddComponent<GameBootstrap>();
            bootstrap.Settings = settings; bootstrap.ChunkPrefabs = chunks; bootstrap.MonkeyPrefab = monkey; bootstrap.PolicePrefab = police;
            bootstrap.CartPrefab = cart; bootstrap.PickaxePrefab = pickaxe; bootstrap.IdeaPrefab = bulb; bootstrap.DustPrefab = dust; bootstrap.HitPrefab = hit;
            bootstrap.CameraRig = rig; bootstrap.OutputCamera = camera;
            bootstrap.IntroTimeline = Timeline();
            // The title stage extends behind Entry for cart approach and menu camera.
            var stage = new GameObject("Mine Title Stage");
            Cube("Ground", stage.transform, new Vector3(0, -.25f, -12), new Vector3(9, .5f, 24), stone, true);
            for (int side = -1; side <= 1; side += 2)
            {
                Cube("Back Mine Wall", stage.transform, new Vector3(side * 4.7f, 2.2f, -12), new Vector3(.6f, 4.4f, 24), stone);
                Cube("Rail", stage.transform, new Vector3(side * .7f, .04f, -12), new Vector3(.09f, .1f, 24), steel);
            }
            Directory.CreateDirectory("Assets/Game/Scenes");
            const string scenePath = "Assets/Game/Scenes/GameScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); Debug.Log("MUHANOK_CONTENT_PASS: 13 chunks, character/prop/obstacle/VFX IDs, GameScene.");
        }
        private static CinemachineCamera Vcam(string id, Transform parent, Vector3 pos, Vector3 target, float fov)
        {
            var result = new GameObject(id).AddComponent<CinemachineCamera>(); result.transform.SetParent(parent, false);
            result.transform.position = pos; result.transform.rotation = Quaternion.LookRotation(target - pos); result.Lens.FieldOfView = fov;
            result.Lens.NearClipPlane = .06f; result.Lens.FarClipPlane = 140; return result;
        }
        private static TimelineAsset Timeline()
        {
            string path = Content + "/Animations/Intro_8s.playable";
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (timeline != null)
            {
                foreach (var existingTrack in timeline.GetOutputTracks()) if (existingTrack is IntroTimelineTrack)
                {
                    var broken = new List<TimelineClip>(); foreach (var existingClip in existingTrack.GetClips()) if (existingClip.asset == null) broken.Add(existingClip);
                    foreach (var existingClip in broken) existingTrack.DeleteClip(existingClip);
                    if (broken.Count > 0) { var repaired = existingTrack.CreateClip<IntroTimelineClip>(); repaired.start = 0; repaired.duration = 8; EditorUtility.SetDirty(timeline); }
                }
                return timeline;
            }
            timeline = ScriptableObject.CreateInstance<TimelineAsset>(); timeline.name = "Intro_8s";
            AssetDatabase.CreateAsset(timeline, path);
            var track = timeline.CreateTrack<IntroTimelineTrack>(null, "Mine / Cart / Idea / Escape / Chase / Takeover");
            var clip = track.CreateClip<IntroTimelineClip>(); clip.start = 0; clip.duration = 8; clip.displayName = "Continuous Intro";
            var signals = timeline.CreateTrack<SignalTrack>(null, "StartRun Signal");
            var start = ScriptableObject.CreateInstance<SignalAsset>(); start.name = "StartRun";
            AssetDatabase.CreateAsset(start, Content + "/Animations/StartRun.signal");
            var marker = signals.CreateMarker<SignalEmitter>(7.99); marker.asset = start; marker.retroactive = true; marker.emitOnce = false;
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength; timeline.fixedDuration = 8;
            EditorUtility.SetDirty(timeline); return timeline;
        }
        private static Material Mat(string id, Color color, bool glow = false)
        {
            string path = Content + "/Materials/" + id + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color = color; m.SetFloat("_Smoothness", .25f);
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 3f); }
            AssetDatabase.CreateAsset(m, path); return m;
        }
        private static GameObject Save(GameObject root)
        {
            string path = Content + "/Prefabs/" + root.name + ".prefab";
            // Preserve imported replacement prefabs on subsequent builds.
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject result = existing != null ? existing : PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root); return result;
        }
        private static Transform Child(string name, Transform parent, Vector3 pos)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = pos; return t; }
        private static GameObject Shape(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collision = false)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = pos; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collision) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>()); return obj;
        }
        private static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collision = false) => Shape(PrimitiveType.Cube, name, parent, pos, size, mat, collision);
        private static GameObject Character(bool guard)
        {
            var root = new GameObject(guard ? "CHR_Police_Officer" : "CHR_Monkey_Prisoner");
            var body = Child("Body", root.transform, new Vector3(0, .85f, 0));
            Shape(PrimitiveType.Capsule, "Uniform", body, Vector3.zero, new Vector3(.8f, .5f, .55f), guard ? navy : orange);
            var head = Child("Head", root.transform, new Vector3(0, 1.55f, 0));
            Shape(PrimitiveType.Sphere, "Skull", head, Vector3.zero, new Vector3(.76f, .7f, .7f), guard ? skin : brown);
            Shape(PrimitiveType.Sphere, "Muzzle", head, new Vector3(0, -.05f, .3f), new Vector3(.53f, .42f, .27f), skin);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape(PrimitiveType.Sphere, "Ear", head, new Vector3(side * .39f, .02f, 0), new Vector3(.25f, .3f, .18f), skin);
                Shape(PrimitiveType.Sphere, "EyeWhite", head, new Vector3(side * .17f, .12f, .31f), new Vector3(.16f, .18f, .09f), white);
                Shape(PrimitiveType.Sphere, "Pupil", head, new Vector3(side * .17f, .12f, .36f), new Vector3(.075f, .09f, .06f), navy);
                string suffix = side < 0 ? "L" : "R";
                var arm = Child("Arm_" + suffix, root.transform, new Vector3(side * .48f, 1.08f, 0));
                Shape(PrimitiveType.Capsule, "Sleeve", arm, new Vector3(0, -.2f, 0), new Vector3(.24f, .25f, .24f), guard ? navy : orange);
                Shape(PrimitiveType.Sphere, "Hand", arm, new Vector3(0, -.5f, 0), Vector3.one * .27f, guard ? skin : brown);
                var leg = Child("Leg_" + suffix, root.transform, new Vector3(side * .22f, .48f, 0));
                Shape(PrimitiveType.Capsule, "Trouser", leg, new Vector3(0, -.14f, 0), new Vector3(.28f, .2f, .3f), guard ? navy : orange);
                Shape(PrimitiveType.Sphere, "Shoe", leg, new Vector3(0, -.38f, .08f), new Vector3(.33f, .19f, .46f), guard ? navy : brown);
            }
            if (guard)
            {
                Cube("Cap", head, new Vector3(0, .32f, 0), new Vector3(.74f, .15f, .64f), navy);
                Cube("CapPeak", head, new Vector3(0, .28f, .3f), new Vector3(.59f, .07f, .3f), navy);
                Cube("Badge", body, new Vector3(-.17f, .16f, .28f), new Vector3(.12f, .16f, .035f), yellow);
            }
            else
            {
                Shape(PrimitiveType.Sphere, "Tail", root.transform, new Vector3(0, .55f, -.47f), new Vector3(.14f, .14f, .7f), brown);
                Cube("PrisonNumberPatch", body, new Vector3(0, .08f, -.29f), new Vector3(.45f, .22f, .02f), white);
            }
            var animator = root.AddComponent<Animator>(); animator.applyRootMotion = false;
            animator.runtimeAnimatorController = Controller(guard); return root;
        }
        private static AnimatorController Controller(bool guard)
        {
            string prefix = guard ? "AN_Guard_" : "AN_Monkey_";
            string path = Content + "/Animations/" + (guard ? "Guard" : "Monkey") + ".controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); if (existing != null) return existing;
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            var names = guard ? GuardClips : MonkeyClips;
            foreach (string name in names)
            {
                var clip = new AnimationClip { name = prefix + name, frameRate = 30 };
                bool run = name == "Run" || name == "ChaseRun" || name == "HighKnee" || name == "SprintStart";
                bool mining = name == "MinePickaxe_Loop";
                float amplitude = run ? 38 : mining ? 65 : 12;
                float length = run ? .6f : 1f;
                foreach (string part in new[] { "Arm_L", "Arm_R", "Leg_L", "Leg_R" })
                {
                    float sign = part.EndsWith("L") ? 1 : -1;
                    if (part.StartsWith("Leg")) sign *= -1;
                    clip.SetCurve(part, typeof(Transform), "localEulerAnglesRaw.x", AnimationCurve.EaseInOut(0, -amplitude * sign, length / 2, amplitude * sign));
                    var curve = new AnimationCurve(new Keyframe(0, -amplitude * sign), new Keyframe(length / 2, amplitude * sign), new Keyframe(length, -amplitude * sign));
                    clip.SetCurve(part, typeof(Transform), "localEulerAnglesRaw.x", curve);
                }
                clip.SetCurve("Head", typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.EaseInOut(0, 0, length, name == "LookCart" ? 130 : 0));
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = run || mining; AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, Content + "/Animations/" + clip.name + ".anim");
                var state = machine.AddState(clip.name); state.motion = clip;
                if (name == "MinePickaxe_Loop" || name == "Notice") machine.defaultState = state;
            }
            return controller;
        }
        private static GameObject Cart()
        {
            var root = new GameObject("PROP_MineCart");
            Cube("Bed", root.transform, new Vector3(0, .4f, 0), new Vector3(1.35f, .15f, 1.8f), wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Cube("Side", root.transform, new Vector3(side * .7f, .85f, 0), new Vector3(.12f, .85f, 1.8f), wood);
                Cube("End", root.transform, new Vector3(0, .85f, side * .9f), new Vector3(1.5f, .85f, .12f), wood);
                for (int end = -1; end <= 1; end += 2)
                {
                    var wheel = Shape(PrimitiveType.Cylinder, "Wheel", root.transform, new Vector3(side * .78f, .26f, end * .57f), new Vector3(.46f, .12f, .46f), steel);
                    wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
            }
            Cube("Lantern", root.transform, new Vector3(0, 1.2f, .98f), Vector3.one * .2f, yellow);
            var source = root.AddComponent<AudioSource>(); source.clip = Tone(); source.loop = true; source.playOnAwake = false; source.spatialBlend = .7f; return root;
        }
        private static AudioClip Tone()
        {
            const string path = Content + "/Audio/MineCart_Rumble.wav";
            if (!File.Exists(path))
            {
                const int rate = 22050; int samples = rate * 2;
                using (var stream = File.Create(path)) using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                    var random = new System.Random(723);
                    for (int i = 0; i < samples; i++) { double t = (double)i / rate; double pulse = .4 + .6 * Math.Pow(Math.Sin(t * Math.PI * 8), 12); writer.Write((short)(4500 * pulse * (Math.Sin(t * Math.PI * 2 * 74) * .7 + (random.NextDouble() - .5) * .3))); }
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        private static GameObject Pickaxe()
        {
            var root = new GameObject("PROP_Pickaxe"); Cube("Handle", root.transform, Vector3.zero, new Vector3(.09f, 1.05f, .09f), wood);
            var blade = Cube("Blade", root.transform, new Vector3(0, .47f, 0), new Vector3(.8f, .09f, .12f), steel); blade.transform.localRotation = Quaternion.Euler(0, 0, -16); return root;
        }
        private static GameObject Bulb()
        {
            var root = new GameObject("VFX_LightBulb_Idea"); Shape(PrimitiveType.Sphere, "Bulb", root.transform, Vector3.zero, Vector3.one * .33f, yellow);
            Cube("Base", root.transform, new Vector3(0, -.2f, 0), new Vector3(.14f, .16f, .14f), steel);
            PointLight(root.transform, Vector3.zero, new Color(1, .55f, .15f), 3, 4); return root;
        }
        private static GameObject Alarm()
        {
            var root = new GameObject("VFX_AlarmLight"); Shape(PrimitiveType.Capsule, "Beacon", root.transform, Vector3.zero, new Vector3(.25f, .2f, .25f), red);
            PointLight(root.transform, Vector3.zero, Color.red, 2, 4); return root;
        }
        private static GameObject Particles(string id, Color color, bool loop)
        {
            var root = new GameObject(id); var particles = root.AddComponent<ParticleSystem>();
            var main = particles.main; main.startColor = color; main.startLifetime = .45f; main.startSize = .16f; main.startSpeed = loop ? .5f : 2.5f; main.maxParticles = 32; main.loop = loop; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.duration = .5f;
            var emission = particles.emission; emission.rateOverTime = loop ? 12 : 0;
            if (!loop) emission.SetBursts(new[] { new ParticleSystem.Burst(0, 18) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .15f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = yellow; return root;
        }
        private static GameObject Hazard(string id, ObstacleKind kind)
        {
            var root = new GameObject(id); root.AddComponent<ObstacleController>().Kind = kind;
            if (kind == ObstacleKind.Crate) Cube("Crate", root.transform, new Vector3(0, .4f, 0), new Vector3(1.6f, .8f, 1.2f), wood);
            else if (kind == ObstacleKind.Pipe) Cube("Pipe", root.transform, new Vector3(0, 1.5f, 0), new Vector3(1.9f, .35f, .6f), yellow);
            else if (kind == ObstacleKind.Stair)
            {
                for (int i = 0; i < 4; i++) Cube("Step", root.transform, new Vector3(0, .07f * (i + 1), .18f * i), new Vector3(1.8f, .14f * (i + 1), .25f), yellow);
            }
            else
            {
                Cube("LeftPost", root.transform, new Vector3(-.82f, 1, 0), new Vector3(.17f, 2, .3f), steel);
                Cube("RightPost", root.transform, new Vector3(.82f, 1, 0), new Vector3(.17f, 2, .3f), steel);
                if (kind == ObstacleKind.Laser) for (int i = 0; i < 3; i++) Cube("Laser", root.transform, new Vector3(0, .45f + i * .6f, 0), new Vector3(1.7f, .05f, .06f), red);
                else Cube("Barrier", root.transform, Vector3.up, new Vector3(1.8f, 1.7f, 1), kind == ObstacleKind.Cart ? wood : yellow);
            }
            return root;
        }
        private static GameObject Chunk(int index, GameObject[] hazards, GameObject alarm)
        {
            var root = new GameObject(ChunkIds[index]); var chunk = root.AddComponent<MuhanokMapChunk>(); chunk.AssetId = root.name;
            chunk.Entry = Child("Entry", root.transform, Vector3.zero); chunk.Exit = Child("Exit", root.transform, Vector3.forward * 24);
            for (int lane = -1; lane <= 1; lane++) Child(lane < 0 ? "Lane_Left" : lane > 0 ? "Lane_Right" : "Lane_Center", root.transform, new Vector3(lane * 2.2f, 0, 0));
            var sockets = Child("ObstacleSockets", root.transform, Vector3.zero); chunk.Obstacles = new ObstacleController[6];
            for (int row = 0; row < 2; row++) for (int lane = -1; lane <= 1; lane++)
            {
                var socket = Child("Socket_" + row + "_" + lane, sockets, new Vector3(lane * 2.2f, 0, 7 + row * 12));
                var contract = socket.gameObject.AddComponent<MuhanokObstacleSocket>(); contract.Row = row; contract.Lane = lane;
                int hazard = index == 7 ? 5 : (index + row * 3 + lane + 1) % 5;
                var obstacle = (GameObject)PrefabUtility.InstantiatePrefab(hazards[hazard]); obstacle.transform.SetParent(socket, false);
                chunk.Obstacles[row * 3 + lane + 1] = obstacle.GetComponent<ObstacleController>(); obstacle.SetActive(false);
            }
            var env = Child("EnvironmentRoot", root.transform, Vector3.zero);
            bool mine = index < 3, outdoor = index >= 11;
            Material walls = mine ? stone : steel;
            Cube("Floor", env, new Vector3(0, -.25f, 12), new Vector3(8.8f, .5f, 24), mine ? stone : steel, true);
            if (!outdoor) Cube("Ceiling", env, new Vector3(0, 4.5f, 12), new Vector3(9, .3f, 24), walls);
            for (int side = -1; side <= 1; side += 2)
            {
                Cube("Wall", env, new Vector3(side * 4.6f, 2.2f, 12), new Vector3(.5f, 4.4f, 24), walls);
                if (mine)
                {
                    Cube("Track", env, new Vector3(side * .7f, .05f, 12), new Vector3(.09f, .1f, 24), steel);
                    for (int z = 2; z < 24; z += 5)
                    {
                        Cube("TimberSupport", env, new Vector3(side * 3.8f, 2, z), new Vector3(.28f, 4, .28f), wood);
                        Cube("TimberCross", env, new Vector3(0, 4, z), new Vector3(8, .3f, .3f), wood);
                        Shape(PrimitiveType.Sphere, "Rock", env, new Vector3(side * 4.25f, .6f, z + 1), new Vector3(1.2f, 1.3f, 1.7f), stone);
                    }
                }
                else
                {
                    for (int z = 2; z < 24; z += 4)
                    {
                        Cube("Frame", env, new Vector3(side * 4.2f, 2, z), new Vector3(.18f, 4, .2f), white);
                        if (index == 4 || index == 5 || outdoor)
                            for (int b = 0; b < 5; b++) Cube("CellBar", env, new Vector3(side * 4.1f, 1.7f, z + b * .45f), new Vector3(.055f, 3.2f, .055f), white);
                        if (index == 8) { Cube("Washer", env, new Vector3(side * 3.5f, .65f, z), new Vector3(.9f, 1.3f, 1.5f), white); Shape(PrimitiveType.Sphere, "Drum", env, new Vector3(side * 2.98f, .65f, z), new Vector3(.05f, .75f, .75f), navy); }
                        if (index == 9) Cube("Counter", env, new Vector3(side * 3.45f, .8f, z), new Vector3(1, 1.6f, 3), white);
                        if (index == 10) Cube("UtilityPipe", env, new Vector3(side * 3.85f, 2.8f, z), new Vector3(.3f, .3f, 4), yellow);
                    }
                }
                for (int z = 3; z < 24; z += 8)
                {
                    Cube("Lamp", env, new Vector3(side * 3.7f, 3.2f, z), new Vector3(.15f, .35f, .2f), yellow);
                    PointLight(env, new Vector3(side * 3.4f, 3.1f, z), mine ? new Color(1, .47f, .13f) : new Color(.4f, .68f, 1), mine ? 3f : 1.7f, 7);
                }
            }
            if (index == 6)
            {
                var beacon = (GameObject)PrefabUtility.InstantiatePrefab(alarm); beacon.transform.SetParent(env, false); beacon.transform.localPosition = new Vector3(3.7f, 3.7f, 12);
            }
            if (index == 3)
            {
                Cube("TransitionArch", env, new Vector3(0, 3.9f, 12), new Vector3(8, .7f, 1.3f), wood);
                Cube("TransitionRails", env, new Vector3(0, .04f, 6), new Vector3(1.6f, .05f, 12), wood);
            }
            return root;
        }
        private static void PointLight(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var light = Child("PracticalLight", parent, pos).gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
        }
    }
}
