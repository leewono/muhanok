# 무한옥: 처음 읽기

## 바로 플레이

1. [Windows 실행용 ZIP](https://github.com/kihwan04/PrisonRunner/releases/latest/download/Muhanok-Windows-x64.zip)을 받습니다.
2. ZIP 전체를 압축 해제합니다.
3. `StartGame.cmd`를 더블클릭합니다.
4. 게임 화면에서 **게임 시작 → 입력 없이 시작**을 누릅니다. 몸무게는 원하면 입력합니다.

실행 대상은 `Game/Muhanok.exe`입니다. 이 PC의 개발 폴더에서는 저장소 맨 위 또는 `PrisonRunner/StartGame.cmd`를 실행하면 `Builds/Windows/Muhanok.exe`가 열립니다.

`Muhanok.exe`와 같은 위치의 `Muhanok_Data`, `UnityPlayer.dll`, `MonoBleedingEdge`, `D3D12` 등은 실행에 필요한 구성 파일입니다. 실행 파일 하나만 옮기지 말고 폴더 전체를 유지하세요. Windows 64비트용이며 키보드 플레이에는 Unity·Python·웹캠이 필요 없습니다.

| 키 | 동작 |
| --- | --- |
| A / ←, D / → | 레인 변경; 수레에서는 좌우 기울이기 |
| Space | 점프 |
| S / ↓ 누르고 있기 | 숙이기 / 슬라이드 |
| W / ↑ | 하이니 (계단 운동) |

웹캠 몸동작은 게임 안에서 바로 인식합니다(Unity Inference Engine, Python 설치 불필요). 시작 화면에서 **전신 / 상체 / 키보드**를 고르고 카메라 미리보기에 몸이 보이게 섭니다. 로딩 중 가만히 서 있으면 자동 보정되고, R은 재보정, Ctrl+F10은 오늘 순위 초기화입니다. 상세 안내는 [docs/BODY_TRACKING.md](docs/BODY_TRACKING.md)를 확인하세요.

## 파일을 찾는 기준

| 위치 | 용도 | 처음 확인할 파일 |
| --- | --- | --- |
| `StartGame.cmd` | 완성된 Windows 게임 실행 | 더블클릭 |
| `PlayInUnity.cmd` | Unity 편집기에서 개발 실행 | Unity 6000.3.10f1 필요 |
| `Unity/` | 게임 코드·씬·리소스 | `Assets/Game/Scenes/GameScene.unity` |
| `docs/` | 기획서·설계·검수 기록 | `README.md`, **기획서 V13** |
| `docs/previews/ArtV10/` | 최신 아트 실제 캡처 | `index.html` |
| `References/` | 참고 자료·Blender 제작 원본·출처 | 제작 시 확인 |
| `Tools/AssetPipeline/` | 제작·문서 검증용 도구 | 플레이에는 실행 불필요 |
| `Tools/Distribution/` | 배포 ZIP 생성 도구 | 제작자용 |
| `Builds/Windows/` | 이 PC의 로컬 실행 파일 | GitHub에는 Release ZIP으로 배포 |

기획서 V10~V12와 ArtV2~V9 캡처는 이전 기록입니다. 현재 안내는 **기획서 V13**, **게임 아트 V10 + 운동·가속 구현**을 기준으로 합니다. V13에서 거리와 속도 수치를 미정으로 둔 것은 문서 기획이며, 현재 게임 구현 수치를 변경한 작업은 아닙니다.

## Unity로 개발하려면

소스를 내려받고 Unity Hub에서 `PrisonRunner/Unity`를 **6000.3.10f1**로 엽니다. 임포트·컴파일이 끝나면 상단 **Muhanok → Play GameScene**을 누릅니다.

공개 소스에는 Mixamo 원본 FBX가 포함되지 않습니다. 처음 열면 빠진 동작을 프로젝트의 자체 Blender 애니메이션으로 자동 연결합니다. 공개 실행용 게임에는 기존 Mixamo 동작이 들어 있으며, 소스 기본 애니메이션과 모양이 다를 수 있습니다. 원본을 직접 취득해 복원하는 방법은 [외부 리소스 안내](THIRD_PARTY_NOTICES.md)에 있습니다.
