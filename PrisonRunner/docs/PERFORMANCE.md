# 성능 최적화 (브랜치 feature/performance-optimization, 2026-10-12)

목표: 2024 LG 그램 내장 그래픽(Intel Arc / Core Ultra)에서 끊김 없이 30~60fps. 측정은 **F3** 성능 측정 표시로 합니다.

## 적용한 것

| 항목 | 내용 | 조절 위치 |
| --- | --- | --- |
| 프레임 상한 | 기본 60fps (저사양 프리셋 30fps). 노트북이 무제한으로 돌다 발열로 느려지는 것을 막음 | 설정 → 화면 |
| 화질 프리셋 | 높음 / 보통 / 저사양. 렌더 해상도(FSR 업스케일), 그림자, SSAO, 소품·조명 거리를 함께 바꿈 | 설정 → 화면 |
| GPU Resident Drawer | 청크의 반복 메시(복도 한 칸에 메시 24종을 렌더러 244개가 재사용)를 인스턴싱으로 묶음. 빌드에서도 동작하도록 Graphics 설정의 BatchRendererGroup 변형을 Keep All로 바꿈 | 설정 → 화면 |
| 작은 소품 그림자 끄기 | 1.2m보다 작은 소품은 그림자를 만들지 않음 (원래 모든 렌더러가 그림자 생성) | GameSettings → Render |
| 먼 소품 숨기기 | 45m보다 먼 청크의 작은 소품은 그리지 않음. 장애물·코인·바닥 아이템·레일은 제외 | GameSettings → Render, 설정의 표시 거리 배율 |
| 점광원 범위 | 플레이어 뒤 12m ~ 앞 55m 밖의 점광원은 끔 (복도 한 칸에 28개) | GameSettings → Render |
| 미리 까는 거리 | 96m → 72m. 인터벌 최고 속도 9.5m/s 기준으로 충분하고 동시에 그리는 청크가 한 칸 줄어듦 | GameSettings → Render → LookAhead |
| 메시 Read/Write | 아트 메시 554개의 Read/Write를 끔. CPU 쪽 메시 사본이 사라져 메모리가 줄어듦. 런타임에 메시를 읽거나 MeshCollider로 쓰는 곳이 없음을 확인함 | 메시 에셋 |
| 포즈 추론 상한 | 초당 최대 30회. 이어서 추적 중에는 랜드마크 모델만 돌아감 | GameSettings → Camera → MaxInferenceRate, 설정의 가벼운 모델 |

모든 런타임 조정은 프리팹과 에셋 원본을 바꾸지 않습니다(메시 Read/Write 제외). 그래픽 설정은 URP 에셋 복사본에 적용됩니다.

## 측정 방법

1. Development Build로 빌드하면 F3 표시에 CPU 메인 스레드 시간, 배치, SetPass, 삼각형 수가 함께 나옵니다.
2. 같은 위치(광산 시작 → 감옥 복도 → 사육장)를 화질 프리셋별로 달려 보며 FPS, 평균·최대 프레임 시간을 기록합니다.
3. 최대 프레임 시간이 평균보다 크게 튀면 그 순간의 구간(청크 생성, 동물 생성, 재검출)을 메모합니다.

## 남은 후보

- 청크 소품을 재질별로 합치고 광원 배치를 줄이는 아트 작업
- 프로젝트 전용 셰이더(HandPaintedTriplanar)는 GPU Resident Drawer 대상이 아님 → DOTS 인스턴싱 지원 추가 검토
- IL2CPP 빌드, 메시 압축
