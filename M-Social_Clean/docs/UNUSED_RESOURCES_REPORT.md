# M-Social_Clean 미사용 자원 검증 보고서

> **갱신:** 2026-06-14 — NatSuite/SampleScene 삭제·MSocialApiClient 도입 이후에도 PNG/MP3 미참조 목록은 동일

> 작성: 2026-06-10 | **재검증: 2026-06-10**  
> 방법: GUID 교차 참조 + 코드 참조 + 파일명 문자열 검색 + 런타임 경로 분석

---

## 1. 버그 수정 (선행 완료)

| 파일 | 수정 |
|------|------|
| `Login.cs:271` | CSV `line[2]` → `tk[2]` |
| `Login.cs:298-301` | ItemString `tk[2].Trim()`, 길이 임계 **32자** |
| `Login.cs:168` | seq 파싱 trim |

---

## 2. 검증 방법

1. **스크립트 GUID 스캔** — `.meta` guid가 `MainScene.unity` / prefab / asset에 등장하는지
2. **코드 참조** — 클래스명이 다른 `.cs`에서 사용되는지
3. **런타임 경로** — `StreamingAssets` MP4는 씬 `vcs[]` 문자열로 로드 (GUID 미사용)
4. **파일명 검색** — GUID 미참조 미디어가 씬 YAML 문자열에 있는지

---

## 3. 삭제 완료 (확정 미사용)

| 항목 | 사유 | Scene GUID | 코드 참조 |
|------|------|------------|-----------|
| `audioso.cs` | 빈 Start/Update 스텁 | 0 | 0 |
| `Stage1Breath.cs` | 빈 클래스; Breath 페이지는 `Stage1Count` 사용 | 0 | 0 |
| `ItemSelect.cs` | 본문 전체 주석 처리 (M-Face 잔재) | 0 | 0 |
| `InfoWindow.cs` | 씬 미부착; `FlowManager.InfoPop: {fileID: 0}` | 0 | 0 |
| `InstructionChange.cs` | M-Face 잔재; 씬·코드 미참조 | 0 | 0 |
| `InstructionChange2.cs` | ItemSelect(주석)에서만 언급 | 0 | 0 |
| `NatSuite/NatCorder/` | 게임 코드 import 없음 (16 cs + 네이티브) | 0 | 0 |
| `Scenes/SampleScene.unity` | 빌드 미포함 2D 템플릿 | — | — |

**삭제 파일:** 8 스크립트 쌍(.meta) + NatSuite 트리 + SampleScene ≈ **40+ 파일**

---

## 4. 유지 확인 (사용 중)

### 게임 스크립트 (23개)

| 스크립트 | SceneRefs | 비고 |
|----------|-----------|------|
| AudioController | 13 | 전역 오디오 |
| ButtonFlow | 7 | 페이지 전환 |
| Login | 1 | 서버 동기화 |
| Stage1Manager | 1 | 1단계 허브 |
| SceneSelect | 1 | 시나리오 선택 |
| ScenePlayer | 1 | VideoClip[] 재생 |
| Stage1Count/Listen/Sing/Dance/Think/Imagine | 1 each | 훈련 모듈 |
| Stage1Positive/Negative | 1 | 피드백 |
| Stage1FeedbackManager | 1 | 피드백 UI |
| Stage2Manager/ScenePlay/PositiveFeedback | 1 | 2단계 |
| ActSelect, EmotionSelect | 1 | 행동·감정 선택 |
| FlowManager, TimerFlow, ButtonChangeViaVideo | 1–3 | 흐름 |
| screenDuplicate, ImageTransition | 3 | UI |

> **Breath** 페이지 GameObject는 `Stage1Count` 컴포넌트 사용 (호흡 영상 `vcs[]`).

### StreamingAssets MP4 (15개) — 전부 사용

씬 `vcs` / `VideoClip` 배열에 15개 파일명 직렬화. GUID 스캔으로는 미검출되나 **런타임 필수**.

### AudioSource MP3

대부분 `AudioController` / `ActSelect` / `Stage2PositiveFeedback`에서 직렬화 참조.  
GUID 미참조 MP3 일부는 **검토 필요** (아래 §5).

---

## 5. 삭제 검토 후보 (미삭제, 수동 확인 권장)

### PNG 93개 (~수 MB)

GUID·파일명 모두 씬 미참조. 예시:

| 경로 | 설명 |
|------|------|
| `Sprites/example movie.png` | 예시 이미지 |
| `Sprites/page 36.png`, `page 38.png` | 미사용 페이지 아트 |
| `Sprites/EmotionIcons/*_OLD.png`, `Option.png` | 구버전/중복 아이콘 |
| `Sprites/Page 33/*` | Inactive circle, 미사용 페이지 |
| `Sprites/Page9/Scenes/2-1.png` 등 | 미연결 씬 썸네일 |

### MP3 12개 (GUID 미참조)

`AudioSource/` 하위 일부 AG_*.mp3 — ActSelect 클립 배열과 대조 후 삭제 가능.

### 기타

| 항목 | 상태 |
|------|------|
| `FlowManager` 카메라 초기화 | 웹캠 미사용이나 Android 권한 요청에 사용 가능 — **유지** |
| `adminText` (Login) | 씬 미연결 가능 — **검토** |

---

## 6. 재검증 결과 (2026-06-10)

### 삭제 완료 재확인

| 항목 | 파일 시스템 |
|------|-------------|
| NatSuite | ✅ 없음 |
| SampleScene | ✅ 없음 |
| audioso, Stage1Breath, ItemSelect, InfoWindow, InstructionChange×2 | ✅ 없음 |

### StreamingAssets MP4 15개 — 전부 사용 (재확인)

유니코드 이스케이프 디코딩 후 씬 `vcs[]` ↔ 디스크 파일명 **1:1 일치** (고아 0개).  
GUID 미참조는 정상 — `Stage1Count`/`Stage1Sing` 등이 `Application.streamingAssetsPath + "/" + vcs[i]`로 로드.

### 미디어 삭제 완료 (2026-06-10)

| 유형 | 삭제 | 유지 |
|------|------|------|
| PNG | **93** | 208 (씬 참조) |
| MP3 | **12** | 266 (씬 AudioClip) |
| StreamingAssets MP4 | 0 | **15** (vcs[] 런타임 로드) |

검증 기준: GUID + 파일명 + `.cs` 문자열 모두 미참조. 잔여 미참조 PNG/MP3 = **0**.

목록: `docs/media-deleted-files.txt` (~23 MB)

---

## 7. 요약

| 구분 | 수량 |
|------|------|
| 삭제 완료 (스크립트+NatSuite+SampleScene+미디어) | ~145+ 파일 ✅ |
| 유지 게임 스크립트 | 23 (전부 SceneRefs ≥ 1) |
| StreamingAssets MP4 | 15 (전부 사용) ✅ |
| 잔여 미참조 PNG/MP3 | **0** |

---

## 8. 다음 단계 (선택)

1. PNG 93개 + MP3 12개 정리 (용량 절감 소규모)
2. `MSocialApiClient` 도입 (M-Face 패턴)
3. `msocial-api-full-verification.ps1` 작성
4. targetSdk 33 / ARM64 빌드 현대화

목록: `docs/cleanup-deleted-files.txt`
