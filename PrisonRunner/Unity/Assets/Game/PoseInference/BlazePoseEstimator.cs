using System;
using System.Globalization;
using Muhanok.Domain;
using Muhanok.Infrastructure;
using Unity.InferenceEngine;
using UnityEngine;

namespace Muhanok.PoseInference
{
    // Webcam -> BlazePose detector (224) -> landmark model (256) on the GPU, read back without blocking.
    // Models: Unity's Inference Engine conversion of Google's MediaPipe BlazePose (Apache-2.0), in Resources/MuhanokPose.
    public sealed class BlazePoseEstimator : IPoseEstimator
    {
        private const string Folder = "MuhanokPose/";
        private const int DetectorSize = 224, LandmarkSize = 256, AnchorCount = 2254, Landmarks = 33;
        private enum Stage { Idle, Detecting, Landmarking }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register() => PoseEstimatorRegistry.Factory = options => new BlazePoseEstimator(options);

        private readonly PoseCameraOptions options;
        private WebCamTexture camera;
        private Worker detector, landmarker;
        private Model landmarkModel;
        private Tensor<float> detectorInput, landmarkInput;
        private Tensor<int> pendingIndex;
        private Tensor<float> pendingScore, pendingBox, pendingLandmarks;
        private float[,] anchors;
        private ComputeShader sampler;
        private int kernel;
        private bool loaded, logits, logitsKnown;
        private Stage stage;
        private Affine detectorToImage, landmarkToImage;
        private int frameWidth, frameHeight;
        private bool flipped;
        private readonly PoseFrame result = new PoseFrame();
        private bool hasResult;
        private double frameTime, openedAt;
        private string[] candidates = new string[0];
        private int candidate, attempt;
        public PoseStatus Status { get; private set; } = PoseStatus.Off;
        public string Message { get; private set; } = "";
        public Texture CameraTexture => camera;

        public BlazePoseEstimator(PoseCameraOptions cameraOptions) { options = cameraOptions ?? new PoseCameraOptions(); }

        public void Start()
        {
            if (!Load()) { Status = PoseStatus.NoModel; return; }
            stage = Stage.Idle;
            if (camera != null) { if (!camera.isPlaying) { camera.Play(); openedAt = Time.realtimeSinceStartupAsDouble; } Status = PoseStatus.Starting; return; }
            var devices = WebCamTexture.devices;
            if (devices.Length == 0) { Status = PoseStatus.NoCamera; Message = "연결된 웹캠이 없습니다."; Debug.LogWarning("Muhanok pose: no webcam devices."); return; }
            // Preferred device first, then real cameras, then virtual ones (OBS and similar often fail to start).
            var names = new System.Collections.Generic.List<string>();
            foreach (var d in devices) if (!string.IsNullOrEmpty(options.DeviceName) && d.name.Contains(options.DeviceName)) names.Add(d.name);
            foreach (var d in devices) if (!names.Contains(d.name) && !IsVirtual(d.name)) names.Add(d.name);
            foreach (var d in devices) if (!names.Contains(d.name)) names.Add(d.name);
            candidates = names.ToArray(); candidate = 0; attempt = 0;
            Debug.Log("Muhanok pose: webcams = " + string.Join(", ", candidates));
            Open();
        }
        private static bool IsVirtual(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("virtual") || n.Contains("obs") || n.Contains("snap camera") || n.Contains("xsplit") || n.Contains("nvidia broadcast");
        }
        // Attempt 0 asks for the configured size; attempt 1 lets the driver choose its default mode.
        private void Open()
        {
            string device = candidates[candidate];
            camera = attempt == 0 ? new WebCamTexture(device, options.Width, options.Height, options.Fps) : new WebCamTexture(device);
            camera.Play(); openedAt = Time.realtimeSinceStartupAsDouble;
            Message = device; Status = PoseStatus.Starting;
        }
        // The driver failed to deliver frames: try the default mode, then the next device.
        private void Retry()
        {
            Debug.LogWarning("Muhanok pose: webcam '" + candidates[candidate] + "' did not start" + (attempt == 0 ? " at " + options.Width + "x" + options.Height + "; trying its default mode." : "; trying the next device."));
            camera.Stop(); UnityEngine.Object.Destroy(camera); camera = null;
            if (++attempt > 1) { attempt = 0; candidate++; }
            if (candidate >= candidates.Length)
            {
                Status = PoseStatus.NoCamera;
                Message = "웹캠을 열 수 없습니다. 다른 프로그램이 카메라를 쓰고 있는지, Windows 설정 > 개인 정보 > 카메라에서 데스크톱 앱 접근이 켜져 있는지 확인하세요.";
                Debug.LogWarning("Muhanok pose: " + Message);
                return;
            }
            Open();
        }

        public void Stop()
        {
            if (camera != null && camera.isPlaying) camera.Stop();
            if (Status != PoseStatus.NoCamera) Status = PoseStatus.Off;
            hasResult = false;
        }

        private bool Load()
        {
            if (loaded) return true;
            if (!SystemInfo.supportsComputeShaders) { Message = "이 PC의 그래픽 카드가 컴퓨트 셰이더를 지원하지 않습니다."; return false; }
            var detectorAsset = Resources.Load<ModelAsset>(Folder + "pose_detection");
            var landmarkAsset = Resources.Load<ModelAsset>(Folder + (options.FullLandmarkModel ? "pose_landmarks_detector_full" : "pose_landmarks_detector_lite"));
            var anchorText = Resources.Load<TextAsset>(Folder + "anchors");
            sampler = Resources.Load<ComputeShader>(Folder + "PoseImageSampler");
            if (detectorAsset == null || landmarkAsset == null || anchorText == null || sampler == null) { Message = "포즈 모델 파일을 찾을 수 없습니다."; return false; }
            try
            {
                anchors = ParseAnchors(anchorText.text);
                var detectorModel = ModelLoader.Load(detectorAsset);
                // Keep only the best anchor on the GPU: index, sigmoid score and its box/keypoints.
                var graph = new FunctionalGraph();
                var input = graph.AddInput(detectorModel, 0);
                var outputs = Functional.Forward(detectorModel, input);
                var boxes = outputs[0];
                var scores = outputs[1];
                var best = Functional.ArgMax(scores, 1).Squeeze();
                var bestScore = Functional.Sigmoid(Functional.Clamp(scores, -100f, 100f)).IndexSelect(1, best).Unsqueeze(0);
                var bestBox = boxes.IndexSelect(1, best).Unsqueeze(0);
                detector = new Worker(graph.Compile(best, bestScore, bestBox), BackendType.GPUCompute);
                landmarkModel = ModelLoader.Load(landmarkAsset);
                landmarker = new Worker(landmarkModel, BackendType.GPUCompute);
                detectorInput = new Tensor<float>(new TensorShape(1, DetectorSize, DetectorSize, 3));
                landmarkInput = new Tensor<float>(new TensorShape(1, LandmarkSize, LandmarkSize, 3));
                kernel = sampler.FindKernel("SampleAffine");
                loaded = true;
            }
            catch (Exception error)
            {
                Debug.LogException(error); Message = "포즈 모델을 불러오지 못했습니다: " + error.Message; DisposeModels();
            }
            return loaded;
        }

        public void Tick(double now)
        {
            if (!loaded || camera == null || Status == PoseStatus.Off) return;
            if (!camera.isPlaying || camera.width <= 16)
            {
                if (now - openedAt > 3) Retry(); else Status = PoseStatus.Starting;
                return;
            }
            if (Status == PoseStatus.Starting) Status = PoseStatus.Tracking;
            switch (stage)
            {
                case Stage.Idle: if (camera.didUpdateThisFrame) Detect(now); break;
                case Stage.Detecting: if (Ready(pendingIndex) && Ready(pendingScore) && Ready(pendingBox)) ReadDetection(); break;
                case Stage.Landmarking: if (Ready(pendingLandmarks)) ReadLandmarks(); break;
            }
        }
        private static bool Ready(Tensor tensor) => tensor != null && tensor.IsReadbackRequestDone();

        private void Detect(double now)
        {
            frameWidth = camera.width; frameHeight = camera.height; flipped = camera.videoVerticallyMirrored; frameTime = now;
            float size = Mathf.Max(frameWidth, frameHeight), scale = size / DetectorSize;
            // Letterbox the frame into the square detector input; tensor rows go down, texture rows go up.
            detectorToImage = Affine.Translation(.5f * (frameWidth - size), .5f * (frameHeight + size)) * Affine.Scale(scale, -scale);
            Sample(detectorInput, DetectorSize, detectorToImage);
            detector.Schedule(detectorInput);
            pendingIndex = detector.PeekOutput(0) as Tensor<int>;
            pendingScore = detector.PeekOutput(1) as Tensor<float>;
            pendingBox = detector.PeekOutput(2) as Tensor<float>;
            pendingIndex.ReadbackRequest(); pendingScore.ReadbackRequest(); pendingBox.ReadbackRequest();
            stage = Stage.Detecting;
        }

        private void ReadDetection()
        {
            int index; float score; var box = new float[12];
            using (var i = pendingIndex.ReadbackAndClone()) index = i[0];
            using (var s = pendingScore.ReadbackAndClone()) score = s[0];
            using (var b = pendingBox.ReadbackAndClone()) for (int k = 0; k < 12; k++) box[k] = b[0, 0, k];
            if (score < options.DetectionThreshold || index < 0 || index >= AnchorCount)
            {
                Publish(false); stage = Stage.Idle; return;
            }
            // BlazePose box keypoint 0 is the hip centre, keypoint 1 a point above the head; they define the body ROI.
            float ax = DetectorSize * anchors[index, 0], ay = DetectorSize * anchors[index, 1];
            var hip = detectorToImage.Apply(ax + box[4], ay + box[5]);
            var top = detectorToImage.Apply(ax + box[6], ay + box[7]);
            var delta = top - hip;
            float radius = 1.25f * delta.magnitude;
            float theta = Mathf.Atan2(delta.y, delta.x);
            float scale = radius / (.5f * LandmarkSize);
            landmarkToImage = Affine.Translation(hip.x, hip.y) * Affine.Scale(scale, -scale) * Affine.Rotation(.5f * Mathf.PI - theta) * Affine.Translation(-.5f * LandmarkSize, -.5f * LandmarkSize);
            Sample(landmarkInput, LandmarkSize, landmarkToImage);
            landmarker.Schedule(landmarkInput);
            pendingLandmarks = LandmarkOutput();
            pendingLandmarks.ReadbackRequest();
            stage = Stage.Landmarking;
        }

        private Tensor<float> LandmarkOutput()
        {
            foreach (var output in landmarkModel.outputs) if (output.name == "Identity") return landmarker.PeekOutput("Identity") as Tensor<float>;
            return landmarker.PeekOutput(0) as Tensor<float>;
        }

        private void ReadLandmarks()
        {
            using (var values = pendingLandmarks.ReadbackAndClone())
            {
                // Visibility/presence may come as logits depending on the conversion; detect once.
                if (!logitsKnown)
                {
                    for (int i = 0; i < Landmarks; i++) { float v = values[5 * i + 3]; if (v < -.01f || v > 1.01f) { logits = true; break; } }
                    logitsKnown = logits || frameTime > 0;
                }
                for (int i = 0; i < Landmarks; i++)
                {
                    var image = landmarkToImage.Apply(values[5 * i], values[5 * i + 1]);
                    float u = image.x / frameWidth, v = image.y / frameHeight;
                    if (flipped) v = 1 - v;
                    float visibility = Activate(values[5 * i + 3]), presence = Activate(values[5 * i + 4]);
                    // Mirror view: the player's right side appears on the right of the screen.
                    result.Points[i] = new PoseLandmark { X = 1 - u, Y = 1 - v, Visibility = Mathf.Min(visibility, presence) };
                }
            }
            Publish(true); stage = Stage.Idle;
        }
        private float Activate(float value) => logits ? 1f / (1f + Mathf.Exp(-value)) : Mathf.Clamp01(value);

        private void Publish(bool pose)
        {
            result.HasPose = pose; result.Time = frameTime; result.Aspect = frameHeight > 0 ? frameWidth / (float)frameHeight : 4f / 3f;
            hasResult = true;
        }

        public bool TryTakeFrame(PoseFrame target)
        {
            if (!hasResult) return false;
            target.CopyFrom(result); hasResult = false; return true;
        }

        private void Sample(Tensor<float> tensor, int size, Affine transform)
        {
            var data = ComputeTensorData.Pin(tensor);
            sampler.SetTexture(kernel, "Source", camera);
            sampler.SetBuffer(kernel, "Output", data.buffer);
            sampler.SetInt("OutWidth", size); sampler.SetInt("OutHeight", size);
            sampler.SetInt("SourceWidth", frameWidth); sampler.SetInt("SourceHeight", frameHeight);
            sampler.SetVector("Row0", new Vector4(transform.A, transform.B, transform.C, 0));
            sampler.SetVector("Row1", new Vector4(transform.D, transform.E, transform.F, 0));
            sampler.SetInt("ToSrgb", options.ConvertToSrgb ? 1 : 0);
            sampler.Dispatch(kernel, (size + 7) / 8, (size + 7) / 8, 1);
        }

        private static float[,] ParseAnchors(string csv)
        {
            var result = new float[AnchorCount, 4];
            var lines = csv.Split('\n');
            if (lines.Length < AnchorCount) throw new FormatException("anchors.csv has " + lines.Length + " lines");
            for (int i = 0; i < AnchorCount; i++)
            {
                var values = lines[i].Split(',');
                for (int j = 0; j < 4; j++) result[i, j] = float.Parse(values[j], CultureInfo.InvariantCulture);
            }
            return result;
        }

        private void DisposeModels()
        {
            detector?.Dispose(); landmarker?.Dispose(); detectorInput?.Dispose(); landmarkInput?.Dispose();
            detector = landmarker = null; detectorInput = landmarkInput = null; loaded = false;
        }
        public void Dispose()
        {
            Stop();
            if (camera != null) { UnityEngine.Object.Destroy(camera); camera = null; }
            DisposeModels();
        }

        // Row-major 2x3 affine transform: (x, y) -> (A x + B y + C, D x + E y + F).
        private readonly struct Affine
        {
            public readonly float A, B, C, D, E, F;
            public Affine(float a, float b, float c, float d, float e, float f) { A = a; B = b; C = c; D = d; E = e; F = f; }
            public static Affine Translation(float x, float y) => new Affine(1, 0, x, 0, 1, y);
            public static Affine Scale(float x, float y) => new Affine(x, 0, 0, 0, y, 0);
            public static Affine Rotation(float theta)
            {
                float s = Mathf.Sin(theta), c = Mathf.Cos(theta);
                return new Affine(c, -s, 0, s, c, 0);
            }
            public static Affine operator *(Affine l, Affine r) => new Affine(
                l.A * r.A + l.B * r.D, l.A * r.B + l.B * r.E, l.A * r.C + l.B * r.F + l.C,
                l.D * r.A + l.E * r.D, l.D * r.B + l.E * r.E, l.D * r.C + l.E * r.F + l.F);
            public Vector2 Apply(float x, float y) => new Vector2(A * x + B * y + C, D * x + E * y + F);
        }
    }
}
