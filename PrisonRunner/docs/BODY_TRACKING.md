# 몸동작 인식과 전시 기능 (2026-10-10)

브랜치 `feature/body-tracking-gameplay`의 내용입니다. Python PoseServer를 없애고 웹캠 인식을 Unity 안에서 처리합니다.

## 구조

```
웹캠(WebCamTexture)
  → PoseImageSampler.compute: 화면을 정사각형 입력으로 변환 (GPU)
  → 처음/놓쳤을 때만 BlazePose 검출기 224px → 몸 영역
  → 랜드마크 모델 256px (Unity Inference Engine, GPU). 이후 프레임은 직전 결과의 정렬점(33·34번)으로 영역을 정해 이어서 추적
  → One Euro 필터로 관절 떨림 보정
  → PoseFrame: 관절 33개 (거울 방향, 0..1)
  → MotionClassifier (Domain): 전신/상체 규칙으로 레인 구역·점프·숙이기·하이니·활동 판정
  → PoseCommandSource → 키보드와 같은 MotionCommand → RunnerController
```

| 위치 | 역할 |
| --- | --- |
| `Unity/Assets/Game/PoseInference/` | 웹캠과 BlazePose 추론. `com.unity.ai.inference`가 있을 때만 컴파일됩니다. |
| `Unity/Assets/Game/Content/Pose/Resources/MuhanokPose/` | 모델 3개, `anchors.csv`, 샘플링 컴퓨트 셰이더 |
| `Unity/Assets/Game/Domain/MotionClassifier.cs` | 동작 판정. Unity 없이 테스트됩니다. |
| `Unity/Assets/Game/Infrastructure/Pose/PoseInput.cs` | 추적기, 명령 변환, 추정기 인터페이스 |
| `GameSettings.asset` → Camera / Motion | 카메라 해상도·장치 이름, 모든 판정 임계값 |

모델은 Google MediaPipe BlazePose를 Unity가 변환해 공개한 [unity/inference-engine-blaze-pose](https://huggingface.co/unity/inference-engine-blaze-pose) (Apache-2.0)입니다. 랜드마크는 기본 `full`, 느린 PC는 `Camera.FullLandmarkModel`을 끄면 `lite`를 씁니다.

## 플레이 방식

| 방식 | 화면에 보여야 할 부분 | 좌우 | 점프 | 숙이기 | 무릎 들기(계단) |
| --- | --- | --- | --- | --- | --- |
| 전신 | 어깨~발 | 몸을 왼쪽/오른쪽 구역으로 이동 | 실제 점프 | 상체 낮추기 | 한쪽 무릎 높이 들기 |
| 상체 | 허리 위 (앉아도 됨) | 상체 기울이기 | 두 손을 머리 위로 | 상체 숙이기 | 팔을 크게 번갈아 흔들기 |
| 키보드 | — | A/D | Space | S | W |

- 레인은 **몸 위치 = 레인**입니다. 왼쪽 구역에 서 있으면 왼쪽 레인, 가운데로 돌아오면 가운데 레인입니다. 수레도 같은 방식으로 기울입니다.
- 점프하기 전에 살짝 숙이는 동작은 슬라이드로 판정하지 않습니다(0.12초 확인 + 착지 후 0.3초 유예).
- 공중에서 누른 점프·숙이기는 0.2초 보관했다가 착지할 때 실행합니다(`Run.InputBufferSeconds`).
- 제자리 달리기(작은 무릎 교차)는 명령은 없지만 활동으로 인정되어 칼로리에 반영됩니다. 칼로리 계수는 전신 4 MET, 상체 2.5 MET, 키보드 1.3 MET입니다.

## 추적 안정화 (2026-10-12)

- MediaPipe와 같이 한 번 찾은 뒤에는 직전 관절의 정렬점으로 다음 영역을 정하고, 어깨 신뢰도가 0.5 아래로 떨어질 때만 검출기를 다시 돌립니다.
- 관절 좌표에 One Euro 필터(MediaPipe 기본값 min_cutoff 0.05, beta 80)를 적용합니다.
- 0.5초 이하로 사라지면 직전 판정을 유지합니다. 숙이던 중에 사라지면 최대 1.5초 숙이기로 처리합니다. 상체 모드는 어깨 신뢰도 0.3까지 허용합니다.
- 동작 충돌 방지: 숙이는 중(확인 중 포함)과 일어선 뒤 0.3초는 좌우·점프를 잠그고, 좌우는 0.1초 유지돼야 바뀌며, 상체 점프는 보정 때의 머리 높이를 기준으로 합니다. 손목·무릎은 신뢰도 0.6 이상만 동작 판정에 쓰고, 다시 잡은 직후 0.25초는 동작을 무시합니다. 값은 Motion의 `LaneHoldSeconds`, `RefractorySeconds`, `ActionVisibility`, `ReacquireLockSeconds`입니다.
- 시작 화면 미리보기 아래 진단 줄: 추적 방식(이어서 추적/사람 찾기), 어깨 신뢰도, 검출 점수, 초당 결과 수, 0.5초 동안 재검출·놓침 횟수.

## 보정

- 시작 화면에서 방식을 고르면 카메라 미리보기와 관절선이 보입니다.
- **시작하기**를 누른 뒤 로딩 동안 가만히 서 있으면 자동 보정됩니다(약 0.7초, 움직이면 다시 시작).
- 사람이 2.5초 이상 사라지면 다시 보정합니다. 가운데에 가만히 서 있는 동안 기준점이 천천히 따라옵니다.
- 운영 키: **R** 재보정, **Ctrl+F10** 오늘 순위 초기화.

## 전시 기능

- **관람객 시작 화면**: 이름(순위표), 몸무게(선택), 플레이 방식. "다시 하기"는 같은 관람객, "다음 관람객"은 빈 시작 화면입니다.
- **인터벌**: 첫 바퀴는 기존 경로 그대로입니다. 이후 매 바퀴는 광산 수레 휴식(`RestSeconds` 20초, `RestSpeed` 5m/s) → 질주(`SprintSeconds` 40초, 6m/s에서 세트마다 +0.5, 최대 9.5m/s)입니다. HUD 가운데 위에 "질주 n세트 / 휴식" 남은 초가 나옵니다.
- **맵**: 판마다 장애물 배치 시드가 바뀌고, 감옥 복도·사육장 길이가 세트 시간에 맞춰 늘어납니다. 사육장에는 **기린 목**(숙이기)과 **코끼리 코**(점프)가 나옵니다. 지금은 기본 도형으로 만든 임시 모델입니다.
- **아이템**: 자석(옆 레인 코인까지), 점수 2배, 보호막(충돌 1회 무시). `UnlockAllItems`가 켜져 있으면 모두 등장하고, 끄면 관람객 전체가 모은 공동 코인 풀로 시작 화면에서 해금합니다.
- **오늘의 순위**: 결과 화면에서 순위가 한 줄씩 들어오고 내 기록이 강조됩니다. 저장 위치는 `%USERPROFILE%\AppData\LocalLow\<회사>\<제품>\Exhibition\ranking-YYYYMMDD.json`입니다.
- **소리**: 질주/휴식에 따라 템포가 바뀌는 배경 음악, 경찰이 가까워지면 사이렌, 코인·점프·아이템·충돌 효과음. 모두 코드로 합성하므로 음원 파일이 없습니다.
- **연출**: 시작할 때 "바르게 서 주세요 → 준비 → 출발!" 배너, 잡힐 때 0.7초 슬로 모션과 붉은 화면.

## 웹캠 테스트 순서

1. Unity에서 프로젝트를 열고 패키지 설치와 임포트가 끝날 때까지 기다립니다. `Packages/manifest.json`에 `com.unity.ai.inference` 2.6.1을 추가했습니다.
2. Console에 컴파일 오류가 없는지 확인한 뒤 **Muhanok → Play GameScene**을 누릅니다.
3. 화면 클릭 → 시작 화면에서 **전신**을 고르고 미리보기에 관절선이 그려지는지 봅니다. "웹캠 없음"이나 "포즈 모듈 없음"이 뜨면 Console 메시지를 확인합니다.
4. 시작하기 → 로딩 동안 가만히 섭니다 → "출발!" 후 아래를 하나씩 확인합니다.
   - 왼쪽/오른쪽 구역으로 이동 → 레인 이동, 가운데로 돌아오기 → 가운데 레인
   - 점프 3번 연속, 숙이기 유지, 계단에서 무릎 들기
   - 수레 구간에서 몸을 좌우로 옮겨 기울이기
5. "다음 관람객" → **상체**로 같은 확인을 합니다(두 손 들기 = 점프, 팔 흔들기 = 무릎 들기).
6. **키보드**는 카메라가 꺼지는지 확인합니다.

판정이 너무 예민하거나 둔하면 `GameSettings.asset` → Motion에서 조정합니다. 레인은 `LaneEnter`/`LaneExit`(전신, 몸통 길이 비율), `UpperLaneEnter`/`UpperLaneExit`(상체, 어깨 너비 비율)이고, 점프는 `JumpRise`, 숙이기는 `CrouchDrop`, 무릎은 `HighKneeLift`입니다. 카메라 색이 이상하게 들어가면 `Camera.ConvertToSrgb`를 바꿔 봅니다.

## 검증 상태

- Domain·Application·Infrastructure·PoseInference는 Unity 6000.3.10f1의 C# 컴파일러로 컴파일했습니다. PoseInference는 Inference Engine 2.6.1 런타임 소스를 함께 컴파일해 API를 대조했습니다.
- EditMode의 규칙·운동량·몸동작·전시 테스트 53개 중 Unity 엔진 없이 실행 가능한 52개가 통과했습니다(순위 파일 저장 테스트는 JsonUtility가 필요해 Unity에서 실행해야 합니다).
- 실제 웹캠 인식, GPU 추론 속도, 화면 배치, PlayMode 테스트는 Unity 에디터에서 확인이 필요합니다.
