# AGENTS.md

## 화면별 기획서 V13 2026년 10월 5일
- 최신 기획서는 docs/무한옥_화면별_게임기획서_V13.docx다. 문장을 자연스럽게 다듬고 맵 길이, 장애물 위치·간격, 거리별 가속 공식과 고정 속도 수치를 제거했다. 거리 배치는 미정이며 점프·슬라이드·하이니·수레 기울이기와 점진 가속 방향을 유지한다. 바닥글 없이 29페이지, 기존 화면 사진 20장, 회피 설명도 10장, 표 4개다.
- V12 원본을 보존한다. 작성은 Tools/AssetPipeline/refine_screen_plan.py, 렌더는 render_screen_plan.py --version V13 --qa-subdir final, 검증은 verify_screen_plan.py --version V13이다. 검수 기록은 docs/SCREEN_PLAN_V13_QA.md다.
- 이번 요청은 기획서 편집이다. 게임 코드나 런타임 설정은 변경하지 않았다. 아래 구현 기록의 거리·속도 수치는 기존 구현값이며 최종 기획에서 확정된 수치로 해석하지 않는다.

## 화면별 기획서 V12 2026년 10월 5일
- 최신 기획서는 docs/무한옥_화면별_게임기획서_V12.docx다. 모든 바닥글과 쪽번호를 제거했고, 기존 화면 사진 20장에 장애물 회피 설명도 10장을 추가한다. 장애물의 모습, 맵 위치, 몸동작, 키보드, 실패 조건을 함께 설명한다. 차단벽과 레이저는 본편에 없는 보조 유형으로 명시한다.
- V11 원본을 보존한다. 작성은 Tools/AssetPipeline/update_obstacle_plan.py, 렌더는 render_screen_plan.py --version V12 --qa-subdir final, 구조 검증은 verify_screen_plan.py --version V12다. 문서 검수 기록은 docs/SCREEN_PLAN_V12_QA.md다.

## 화면별 기획서 V11 2026년 10월 5일
- 최신 화면별 기획서는 docs/무한옥_화면별_게임기획서_V11.docx다. 24페이지에 맵별 운동 행, 점프 우선 처리, 9→17m/s 거리 가속과 조작 기준을 반영한다. 사진 20장은 기존 V10 참고 화면이며 새 장애물과 속도 HUD를 재촬영한 자료가 아니다.
- 문서 검수는 docs/SCREEN_PLAN_V11_QA.md에 기록한다. V10 원본을 보존하고 Tools/AssetPipeline/update_screen_plan.py로 V11을 작성한다. render_screen_plan.py --version V11 --qa-subdir final과 verify_screen_plan.py --version V11을 사용한다.

## 맵 운동과 가속 2026년 10월 5일
- docs/MAP_EXERCISES_SPEED.md를 확인한다. 모든 도보 청크의 ExerciseObstacles에 점프/슬라이드 행을 둔다. 계단은 추가 점프 Z3, 하이니 Z12, 기존 닫히는 문 슬라이드 Z19다. 감옥 완전 폐쇄 문은 Z23에서 한 레인만 막는다. 수레는 기존 철로 기울이기를 유지한다.
- 기본/수레 시작 속도 9m/s, 실제 이동 120m마다 1m/s 가속, 최대 17m/s. SpeedRampDistance는 GameSettings에서 조절한다. 슬라이드 중 바닥 점프는 슬라이드를 해제한다. 운동만 재생성할 때 MuhanokGameplayUpgrade.Apply를 사용하며 전체 아트 재생성에서도 Configure를 호출한다.

## 최신 V10 명세 2026년 10월 5일
- docs/ART_V10.md와 ART_V10_QA.md를 먼저 확인한다. V9 이동·카트·바나나·문 규칙은 유지한다. 최신 전체 재생성은 MuhanokVisualPolish.UpgradeV10이다.
- 감옥 타입 4와 5는 철창 x=±3.97, 뒷벽 x=±7.9, 양쪽 칸막이와 독방 바닥, 잠긴 문, 침대와 소품을 갖춘 실제 방이다. 복도 청크마다 자체 Blender 스킨 원숭이 2명이 CellActors 아래에서 AN_Monkey_CellIdle을 재생한다. 애니메이션 객체를 EnvironmentRoot 정적 메시와 합치지 않는다.
- 원숭이 모델에는 주황 수감 밴드, 0723 등번호 패치와 손발톱을 추가했다. Humanoid 컨트롤러 IK Pass와 FootGrounding으로 바닥 높이·경사 및 짧은 범위의 발 고정을 보정한다. 실제 Mixamo 4개 동작은 유지하고 독방 대기는 자체 Blender 동작으로 구분한다.
- 화면별 기획서는 docs/무한옥_화면별_게임기획서_V10.docx, 실제 GPU 캡처는 docs/previews/ArtV10이다. CellCloseup과 CharacterContact는 제작 검수용 카메라다. --muhanok-capture 플래그가 없으면 검수 도구가 실행되지 않는다.
- 무료 경찰은 여전히 다운로드 인증 문제로 미임포트 상태이며 화면의 경찰은 자체 모델이다. 상용 benchmark 수준을 달성했다고 주장하지 않는다.

## 무한옥 현재 명세 (2026-10-03)
- 최신 V9은 docs/ART_V9.md / ART_V9_QA.md. 광산 → 전용 단선 절벽 카트 → 시설 연결 → 계단 → 내려오는 문 아래 슬라이딩 → 감옥 복도 → 출구 → 사육장. 카트에는 일반 고체 장애물이 없고 한쪽 레일 붕괴 네 곳을 반대쪽 몸 기울이기로 통과한다. 카트에서만 후방 3인칭, 하차하면 1인칭. 계단 끝 Z115 전폭 문은 숙여 지나갈 틈을 남기고 뒤에서 바닥까지 닫힌다. 계단 점프 대체 금지, 지정 운동 행은 3레인 모두 같은 동작 장애물이다. 바나나 한 플레이 누적 2회는 감속 중 1.2초 추격 후 잡힘, 물웅덩이는 감속만. 아래 V8 이전 규칙은 기록이며 최신 사용자 규칙이 우선한다. 전체 재생성 MuhanokVisualPolish.UpgradeV9. 무료 경찰은 미임포트, 현재 자체 모델. 상용 benchmark 품질 달성으로 보고하지 않는다.
- 최신 V8은 docs/ART_V8.md / ART_V8_QA.md. 정상 시작은 Title → 선택 몸무게 → LoadingIntro(제공 이미지) → 0.85초 전환 → Running이며 별도 8초 인트로를 이어 재생하지 않는다. 고체 장애물 실제 충돌 한 번 즉시 종료, 바나나/물웅덩이는 2.4초 50% 감속만 적용한다. 수레는 타입 1/2의 48m 연속 구간에서 조향/숙이기, 점프 금지, 실제 빈 수레/그립/소리/기울기. 서비스 출구/사육장 수목/밤하늘/HUD를 보강했다. 아래 V7 이전 흐름/누적 충돌은 과거 기록이며 최신 사용자 규칙이 우선한다. 전체 재생성 MuhanokVisualPolish.UpgradeV8. 무료 경찰은 현재 미임포트; 공식 0원 주문 경로도 확인했으나 이메일/주소 필수 입력은 사용자 직접 처리 대기. 상용 benchmark 품질 달성으로 보고하지 않는다.
- 최신 작업은 V7, docs/ART_V7.md. 갈색 연결 스킨 원숭이/실제 FBX 표정 2개, 불규칙 암벽과 6개 암석 재질, 사육장 외부 지면/상세 무료 수목, 실제 광산 그림자와 SSAO를 적용한다. 로딩 완료 후 첫 3D 포즈를 준비하고 0.85초 전환한 뒤 인트로를 재생한다. 전체 재생성은 MuhanokVisualPolish.UpgradeV7. 실제 그림과 benchmark 수준의 품질 달성으로 보고하지 않는다. 무료 경찰 107256은 계정 취득 확인이나 정상 편집기 다운로드 인증 실패로 현재 미임포트다. 사용자에게 반복 로그인 요구하지 않는다.
- 최신 품질 작업은 2026-10-04 V6, docs/ART_V6.md. 서브웨이 서퍼스/링피트를 벤치마크로 삼되 달성했다고 주장하지 않는다. 생성 atlas의 실제 맵 투영, Blender 손/동작 반응, 두 개 점광원 그림자 예산을 적용했다. 전체 재생성은 MuhanokVisualPolish.UpgradeV6로 재질까지 복원한다. 무료 경찰 107256은 Unity 계정 취득이 확인됐다. 편집기 다운로드 서비스의 로그인 확인/임포트가 남아 있다. Higgsfield 연결 도구가 없으며 실행했다고 표시하지 않는다.
- 최신 사용자 경로는 광산 → 계단 → 감옥 복도(바닥까지 닫히는 문) → 사육장이다. V5 구현 docs/ART_V5.md, 검증 docs/ART_V5_QA.md를 우선 읽는다. 10개 청크 순서 0,1,2,3,7,4,5,6,11,12. 몸무게 선택 입력 → 사용자 제공 로딩 이미지 → 기존 인트로. 몸무게는 메모리에만 보관하고 인식된 활동 시간으로 4 MET 예상 칼로리를 계산한다. 무료 경찰 107256은 재로그인 대기로 미임포트, 원숭이 108270은 유료여서 구매하지 않았다.
- 무료 에셋만 사용. 최신 작업 docs/TOOLS_V4.md, 검증 docs/TOOLS_V4_QA.md. Kenney CC0 732 FBX, Blender 환경 소품 8 FBX + 자체 스킨 캐릭터 2개/동작 16개/무골격 Mixamo 업로드 T-pose 1개. ProBuilder 6.1.2 편집용 13개 경로를 실제 런타임 메시로 베이크한다. 화면/연속 경로 V3 기록도 보존. Mine은 약관 승인 대기로 미임포트. Mixamo 실제 동작 4개를 다운로드/임포트했으며 출처와 해시는 References/MixamoDownloads/Provenance.json에 있다. 실제 적용·자체 제작·후보·품질 차이를 구분해 보고한다.
- 이번 사용자 명세는 기존 MVP 계획보다 우선한다. 최신 계획/상태는 docs/IMPLEMENTATION_PLAN.md, docs/PROGRESS.md.
- GameScene에서 title → 8초 Timeline → Cinemachine 1인칭 → Running → Caught → Result → Retry. Scene reload 금지.
- 본편 1인칭, 3 lane X -2.2 / 0 / +2.2, 24m chunk, pooling.
- 몸동작은 Unity Inference Engine에서 BlazePose(검출기+랜드마크)로 추정하고 Domain의 MotionClassifier가 전신/상체 명령으로 바꾼다. Python PoseServer와 UDP 입력은 2026-10-10에 제거했다. 상세는 docs/BODY_TRACKING.md.
- Domain은 UnityEngine 없이 유지. 기존 Core LaneRules는 재사용한다. 프로젝트에는 GameScene만 둔다. RunnerMVP·CellBlockVerticalSlice 시연 씬과 전용 스크립트·아트는 2026-10-10에 삭제했다.
- 캐릭터는 자체 Blender Humanoid 모델이다. 달리기/점프/숙이기/비틀거림은 실제 Mixamo 동작, 채굴/아이디어 등은 자체 Blender 동작이다. 참고 이미지 수준의 최종 아트는 남아 있다. 미임포트 상용 상품을 사용했다고 추정하지 않는다. 후보와 교체 지점: docs/ASSET_CATALOG.md.
- 기존 패키지 버전 업그레이드 금지. 필수 Timeline과 audio/animation/director/particlesystem 활성화 사유는 계획 문서에 기록되어 있다.

## 프로젝트
- Unity 기반 3레인 Endless Runner 캡스톤 프로젝트다.
- 배경은 Stylized 3D 감옥이다.
- 웹캠 + Unity Inference Engine(BlazePose)으로 몸동작을 인식한다. 플레이 방식은 전신/상체/키보드다.
- 인식 결과를 Unity 캐릭터 조작으로 연결한다.
- 구현은 프로젝트 개발 규칙을 따른다.
- 기능 완성을 그래픽 완성보다 우선한다.

## 반드시 읽을 문서
- docs/DEVELOPMENT_PLAN.md
- docs/ARCHITECTURE.md
- docs/ART_DIRECTION.md

## 개발 원칙
- 작업 전 기존 코드를 먼저 확인한다.
- 요청하지 않은 전체 리팩터링을 하지 않는다.
- 정상 동작하는 기능을 이유 없이 수정하지 않는다.
- 한 번에 하나의 기능 단위로 개발한다.
- Placeholder로 먼저 기능을 검증한다.
- 과도한 추상화를 하지 않는다.
- 컴파일 가능한 상태를 유지한다.

## 게임
- 캐릭터는 자동으로 전진한다.
- LEFT / RIGHT는 레인 이동이다.
- JUMP는 점프다.
- CROUCH는 숙이기다.
- HIGH_KNEE는 계단 동작이다.
- IDLE은 기본 달리기다.
- 기본적으로 3개의 레인을 유지한다.
- 충돌 시 속도 감소 또는 Stumble을 적용한다.

## 아키텍처
- Clean Architecture Lite를 사용한다.
- Core는 순수 게임 규칙을 담당한다.
- Application은 게임 흐름을 담당한다.
- Infrastructure는 외부 시스템을 담당한다.
- Presentation은 Unity 표현 계층이다.
- 자세한 구조는 docs/ARCHITECTURE.md를 따른다.

## 입력
- 게임 로직은 Keyboard나 MediaPipe를 직접 참조하지 않는다.
- 모든 입력은 공통 입력 인터페이스를 통해 전달한다.
- KeyboardGameInput을 항상 유지한다.
- PoseInputProvider는 별도로 구현한다.
- Pose 입력이 실패해도 키보드 플레이가 가능해야 한다.

## 몸동작 인식
- PoseInference 어셈블리가 웹캠과 BlazePose 추론을 담당하고, Inference Engine 패키지가 없으면 컴파일에서 빠진다.
- MotionClassifier(Domain)가 관절 좌표를 레인 구역·점프·숙이기·하이니로 바꾸며 게임 로직은 MotionCommand만 받는다.
- 허용 명령은 CENTER LEFT RIGHT JUMP CROUCH HIGH_KNEE다.
- 사람이 안 보이면 명령을 보내지 않고, 오래 사라지면 다시 보정한다.

## Unity
- MonoBehaviour 하나에 여러 책임을 몰아넣지 않는다.
- Player 입력 이동 애니메이션 점수 맵 생성을 분리한다.
- 설정값은 가능한 ScriptableObject로 관리한다.
- Visual Mesh와 Collider를 분리한다.
- 모델 교체가 게임 로직에 영향을 주면 안 된다.

## Endless Map
- MapChunk 기반 구조를 사용한다.
- Chunk마다 EntrySocket과 ExitSocket을 가진다.
- 장애물 위치는 ObstacleSocket으로 관리한다.
- 모든 Chunk는 동일한 Lane 규격을 사용한다.
- 지나간 Chunk와 장애물은 Object Pooling으로 재사용한다.
- 피할 수 없는 장애물 패턴을 생성하지 않는다.

## Art
- 모든 그래픽은 docs/ART_DIRECTION.md를 따른다.
- Reference 이미지는 References/ 폴더에서 관리한다.
- Stylized 3D 스타일을 유지한다.
- 게임 가독성을 장식보다 우선한다.

## 외부 에셋
- AI 생성 에셋은 바로 Production에 넣지 않는다.
- 먼저 Staging에서 확인한다.
- 3D Model은 크기 Pivot Material Collider를 검수한다.
- API Key와 .env는 Git에 올리지 않는다.

## Git
- main은 발표 가능한 안정 버전이다.
- develop은 통합 개발 브랜치다.
- feature/*에서 개별 기능을 개발한다.
- 기능 완료 후 develop으로 병합한다.
- 발표 가능한 시점에 develop을 main으로 병합한다.
- 하나의 Commit은 하나의 목적만 가진다.
- 브랜치명과 커밋 메시지는 기능과 변경 목적만 표현한다.
- 작성 도구명이나 생성 주체에 대한 문구를 넣지 않는다.
- 자세한 작업 흐름은 docs/GIT_WORKFLOW.md를 따른다.

## 금지
- Unity 버전을 임의로 변경하지 않는다.
- 필요 없는 Package를 설치하지 않는다.
- API Key를 코드에 작성하지 않는다.
- Library Temp Logs Build를 Git에 Commit하지 않는다.
- Scene 전체를 이유 없이 다시 만들지 않는다.

## 작업 종료 보고
작업 완료 후 반드시 다음을 보고한다.
1. 생성한 파일
2. 수정한 파일
3. 구현한 기능
4. 테스트 결과
5. Compile Error 여부
6. 남은 문제
7. 다음 추천 작업
