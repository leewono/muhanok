# 설정창과 성능 측정 표시 (UI/UX 작업 안내)

설정창은 **UI Toolkit**으로 만들었습니다. 기능은 모두 동작하고, 모양은 임시(단색 패널과 버튼)입니다. 그래픽은 코드 수정 없이 Unity에서 입힐 수 있습니다.

## 여는 방법

| 위치 | 방법 |
| --- | --- |
| 타이틀 화면 | **F1** (화면 클릭은 게임 시작이라 키로 엽니다) |
| 관람객 정보 입력 화면 | 오른쪽 위 **설정** 버튼 또는 F1 |
| 플레이 중 | 일시정지 → **설정**, 또는 F1 (자동으로 일시정지) |
| 성능 측정 표시 | **F3** 또는 설정 → 표시 → 성능 측정 표시 |

설정은 이 PC에 저장됩니다(PlayerPrefs `Muhanok.UserSettings`). **확인**이나 **닫기**를 누르면 저장되고, 값은 바꾸는 즉시 게임에 적용됩니다.

## 파일

| 파일 | 역할 | 수정 |
| --- | --- | --- |
| `Unity/Assets/Game/Content/UI/Resources/MuhanokUI/SettingsWindow.uxml` | 화면 구조 (UI Builder로 열기) | 배치·요소 추가 가능. **name은 유지** |
| `.../MuhanokUI/SettingsWindow.uss` | 색·크기·배경 이미지·폰트 | 자유롭게 수정 |
| `.../MuhanokUI/MuhanokRuntimeTheme.tss` | Unity 기본 런타임 테마 | 보통 그대로 |
| `Unity/Assets/Game/Presentation/UI/SettingsWindow.cs` | 이름으로 요소를 찾아 기능 연결 | 프로그래머 |

### 코드가 찾는 요소 이름 (바꾸면 기능이 끊깁니다)

- 창: `settings-root`, `settings-panel`, `close-button`, `apply-button`, `reset-button`
- 탭: `tab-graphics`, `tab-sound`, `tab-body`, `tab-display` / 페이지: `page-graphics`, `page-sound`, `page-body`, `page-display`
- 화면: `quality-preset`, `render-scale`, `frame-cap`, `vsync`, `shadow-quality`, `ambient-occlusion`, `detail-distance`, `gpu-resident-drawer`, `fullscreen`
- 소리: `music-volume`, `effects-volume`
- 몸동작: `camera-device`, `lite-model`, `lane-sensitivity`, `camera-preview`(웹캠 영상이 들어가는 칸), `pose-status`, `recalibrate-button`
- 표시: `performance-overlay`, `pose-diagnostics`

요소가 없으면 Console에 경고만 나오고 나머지는 동작합니다. 선택된 탭에는 코드가 `settings-tab--selected` 클래스를 붙입니다.

### 그래픽 입히는 예

```css
.settings-panel {
    background-image: url("project://database/Assets/Art/UI/settings_panel.png");
    -unity-slice-left: 32; -unity-slice-right: 32; -unity-slice-top: 32; -unity-slice-bottom: 32;
    background-color: rgba(0, 0, 0, 0);
}
.settings-tab--selected { background-image: url("project://database/Assets/Art/UI/tab_on.png"); }
.settings-root { -unity-font-definition: url("project://database/Assets/Art/Fonts/OurFont SDF.asset"); }
```

폰트를 지정하지 않으면 한글을 위해 Windows의 맑은 고딕을 씁니다.

## 설정 항목과 실제 동작

| 항목 | 적용 |
| --- | --- |
| 화질 프리셋 | 높음(100%, 그림자 높음, SSAO, 60fps) / 보통(85%, 그림자 낮음, SSAO 끔) / 저사양(70%, 그림자 끔, 30fps) |
| 렌더 해상도 | URP 렌더 스케일 50~100%, 100% 미만이면 FSR 업스케일 |
| 프레임 제한 / 수직 동기화 | `Application.targetFrameRate`, `QualitySettings.vSyncCount` |
| 그림자 | 그림자 거리·해상도와 방향광 그림자(끄기/하드/원래 설정) |
| SSAO | 렌더러의 Screen Space Ambient Occlusion 기능 켜고 끄기 |
| 소품·조명 표시 거리 | 맵의 작은 소품과 점광원이 켜져 있는 거리 배율(50~100%) |
| GPU 대량 렌더링 | URP GPU Resident Drawer (반복되는 메시를 인스턴싱으로 묶어 그림) |
| 웹캠 / 가벼운 모델 / 좌우 민감도 | 포즈 추정기를 해당 장치·모델로 다시 시작, 레인 판정 기준을 민감도로 나눔 |

그래픽 설정은 실행 중 URP 에셋의 **복사본**에 적용하므로 에디터에서 플레이해도 프로젝트 에셋은 바뀌지 않습니다. 게임이 끝나면 원래대로 돌립니다.

## 성능 측정 표시 (F3)

오른쪽 위에 0.5초마다 갱신됩니다.

- FPS, 최근 1초 평균·최대 프레임 시간
- CPU 메인 스레드 시간, 배치·SetPass·삼각형 수 (에디터와 Development Build에서만)
- 메모리, 현재 화질 설정
- 포즈: 이어서 추적/사람 찾기, 어깨 신뢰도, 검출 점수, 초당 결과 수, 재검출·놓침 횟수
