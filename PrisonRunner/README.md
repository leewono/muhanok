# 무한옥 (PrisonRunner)

키보드 또는 웹캠 몸동작으로 플레이하는 Windows 64비트 Unity 러너 게임입니다.

**처음 실행:** [Windows 실행용 ZIP](https://github.com/kihwan04/PrisonRunner/releases/latest/download/Muhanok-Windows-x64.zip) 전체를 압축 해제하고 `StartGame.cmd`를 더블클릭 → **게임 시작 → 입력 없이 시작**.

- [실행 방법과 폴더별 역할](START_HERE.md)
- [최신 화면별 게임기획서 V13](docs/무한옥_화면별_게임기획서_V13.docx)
- [문서 목록과 이전 버전 구분](docs/README.md)
- [최신 게임 아트 V10](docs/ART_V10.md), [실제 게임 캡처](docs/previews/ArtV10/index.html)
- [맵 운동·가속 기능과 검증](docs/MAP_EXERCISES_SPEED.md)
- [몸동작 인식·플레이 방식·전시 기능](docs/BODY_TRACKING.md)
- [리소스 출처와 공개 소스 애니메이션](THIRD_PARTY_NOTICES.md)

로컬에서는 `StartGame.cmd` 또는 `Builds/Windows/Muhanok.exe`를 실행합니다. 게임 빌드는 Git 소스 대신 GitHub Releases에서 배포합니다. 실행에는 Unity나 Python이 필요 없습니다. 웹캠 인식은 게임 안에서 처리합니다.

개발 실행은 Unity Hub에서 `Unity/`를 **6000.3.10f1**로 열고, 임포트·컴파일 완료 후 **Muhanok → Play GameScene**을 누릅니다. `PlayInUnity.cmd`도 같은 씬을 실행합니다. 설정은 `Unity/Assets/Game/Content/Data/GameSettings.asset`에 있습니다.

현재 게임은 V10 아트에 2026-10-05 운동·가속 기능을 적용한 버전입니다. 최신 기획서 V13은 거리와 속도 수치를 미정으로 둔 문서 수정본이며 게임 구현값과 구분합니다. 이전 기획서·캡처·시연 씬은 보존합니다. [이전 README 기록](docs/legacy/README_BEFORE_PUBLIC_RELEASE.md)은 과거 상태 설명이며 현재 실행 안내는 위 링크를 기준으로 합니다.

기본 경로는 광산 → 전용 수레 → 계단 → 감옥 복도 → 사육장입니다. A/D·←/→는 이동, Space는 점프, S·↓는 숙이기, W·↑는 하이니입니다. 수레에서는 좌우로 기울여 철로 붕괴를 피합니다. 몸동작 입력은 localhost UDP 5055를 사용합니다.

공개 소스에서는 재배포 제한이 있는 Mixamo 원본 FBX를 제외하고 자체 Blender 동작으로 자동 연결합니다. 실행용 ZIP은 기존 동작을 유지합니다. 실제 웹캠 인식 정확도와 최고 속도의 체감 난이도는 현장 검수가 남아 있습니다.

개발 규칙: [AGENTS.md](AGENTS.md), [설계](docs/ARCHITECTURE.md), [Git 작업 흐름](docs/GIT_WORKFLOW.md).
