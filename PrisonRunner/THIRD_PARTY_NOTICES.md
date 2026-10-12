# 외부 리소스와 공개 배포

## Kenney

Factory Kit, Furniture Kit, Modular Cave Kit, Nature Kit, Survival Kit은 CC0 리소스입니다. 키트 출처와 원본 SHA256은 [docs/FREE_ASSETS_IMPORTED.json](docs/FREE_ASSETS_IMPORTED.json)에 기록되어 있습니다. 원본 라이선스 파일을 리소스 폴더와 함께 보존합니다.

## Adobe Mixamo

게임 빌드는 Running With Intention, Jumping In Place, Low Crouching Idle, Brief Stumble While Jogging 동작을 사용합니다. 출처는 https://www.mixamo.com/ 이며 다운로드 설정과 원본 해시는 [References/MixamoDownloads/Provenance.json](References/MixamoDownloads/Provenance.json)에 있습니다.

Mixamo 원본 캐릭터·애니메이션 파일은 일반 공개 재배포 대상에서 제외합니다. [Adobe 배포 안내](https://community.adobe.com/questions-696/mixamo-faq-licensing-royalties-ownership-eula-and-tos-589400?lang=en)에 따라 게임에 포함한 동작과 원본 FBX 공개 배포를 구분합니다.

공개 소스는 자체 Blender 동작을 자동 연결해 실행합니다. 기존 동작을 개발 환경에 복원하려면 각자가 Adobe 계정으로 위 동작을 FBX for Unity / Without Skin / 30fps / keyframe reduction none으로 직접 내려받아 `Unity/Assets/External/Staging/Mixamo/Animations/Run.fbx`, `Jump.fbx`, `Crouch.fbx`, `Stumble.fbx`로 저장하고 Unity의 **Muhanok → Import Downloaded Mixamo Motions**를 실행합니다. Running 및 Stumble은 In Place를 사용합니다.

## 그 외

Blender 캐릭터·환경·자체 동작의 제작 원본은 `References/Blender/`, 실제 사용 FBX는 `Unity/Assets/External/Staging/Blender/`에 있습니다. 생성 재질 출처는 `References/GeneratedV6/Provenance.md`에 있습니다. 로딩 이미지와 참고 이미지는 해당 프로젝트에서 제공받은 자료입니다.

몸동작 인식은 Unity Inference Engine(com.unity.ai.inference)으로 게임 안에서 실행합니다. 모델은 Google MediaPipe BlazePose를 Unity가 Inference Engine용 ONNX로 변환해 공개한 [unity/inference-engine-blaze-pose](https://huggingface.co/unity/inference-engine-blaze-pose)(Apache-2.0)의 `pose_detection.onnx`, `pose_landmarks_detector_full.onnx`, `pose_landmarks_detector_lite.onnx`, `anchors.csv`이며 `Unity/Assets/Game/Content/Pose/Resources/MuhanokPose/`에 있습니다. 배경 음악과 효과음은 실행 중 코드로 합성하며 외부 음원 파일을 쓰지 않습니다. Unity 패키지의 라이선스는 각 공급자의 조건을 따릅니다. 현재 저장소에 프로젝트 전체를 포괄하는 오픈소스 라이선스를 새로 지정하지 않았습니다.
