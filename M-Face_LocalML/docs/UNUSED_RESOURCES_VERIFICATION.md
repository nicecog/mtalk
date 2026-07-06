# M-Face_Clean 미사용 자원 재검증 (2026-06-10)

> 방법: 파일 존재 확인 + GUID 교차 참조 + 코드 참조 + 런타임 경로 분석

---

## 1. 삭제 완료 확인

| 항목 | 상태 | 검증 |
|------|------|------|
| `Assets/NatSuite/` | ✅ 삭제됨 | 폴더 없음 |
| `Assets/Scenes/SampleScene.unity` | ✅ 삭제됨 | Scenes에 MainScene만 존재 |
| 빌드 씬 | ✅ | `EditorBuildSettings` → MainScene만 |

---

## 2. 삭제하지 않은 스크립트 (사용 중 — 유지 정당)

M-Social과 달리 M-Face는 아래 스크립트가 **MainScene에 부착·참조**되어 있어 삭제 대상이 아님.

| 스크립트 | SceneRefs | 참조 위치 |
|----------|-----------|-----------|
| `ItemSelect.cs` | 1 | Stage2ItemSelect, WebCamStage2.itemSel |
| `InfoWindow.cs` | 1 | UI 이벤트, ItemSelect 연동 |
| `InstructionChange.cs` | 1 | Stage1Manager.ic, WebCam.ic |
| `InstructionChange2.cs` | 1 | ItemSelect.ic2 |

전체 게임 스크립트 14개 — **전부 씬 또는 코드에서 사용**.

`MFaceApiClient.cs` — 런타임 `EnsureExists()` 생성, 씬 GUID 불필요 (`.meta` 없음, Unity 재생성 가능).

---

## 3. 미삭제 미디어 (검토 후보)

### StreamingAssets MP3 264개 — **유지 (런타임 사용)**

GUID·씬 문자열 모두 미참조이나, `Stage1Manager` / `WebCamStage2`가 패턴으로 동적 로드:

```
{H|N|S|A}{1-2}-{01-11}{a|b|c}.mp3  →  Application.streamingAssetsPath
```

예: `H2-05b.mp3`, `A1-01a.mp3` — **삭제 금지**.

### AudioSource 외 MP3 10개 — 삭제 후보

| 경로 | GUID | 씬 파일명 |
|------|------|-----------|
| `음성 획득 성공.mp3` (루트) | 미참조 | 미참조 |
| `MUZI 음성 파일/FACE_S12_1~4.mp3` | 미참조 | 미참조 |
| `MUZI 음성 파일/FACE_S13.mp3` | 미참조 | 미참조 |
| `MUZI 음성 파일/FACE_S15_1~4.mp3` | 미참조 | 미참조 |

### PNG 49개 — 삭제 후보

GUID·파일명 모두 씬 미참조. 예: `Logo1.png`, `Page/P28-*.png`, `Page/P26/*` 등 구 UI 아트.

### MP3 (씬 직렬화) 37개 — **유지**

`AudioController.clips[]` 등 씬에 GUID로 참조됨.

---

## 4. 요약

| 구분 | 삭제 완료 | 미삭제 (의도/검토) |
|------|-----------|-------------------|
| NatSuite + SampleScene | ✅ | — |
| 데드 스크립트 | 해당 없음 (M-Face는 사용 중) | — |
| StreamingAssets MP3 264 | — | 런타임 풀 → **유지** |
| 기타 MP3 10 | — | **삭제 후보** |
| PNG 49 | — | **삭제 후보** |

## 5. 미디어 삭제 완료 (2026-06-10)

| 유형 | 삭제 | 유지 |
|------|------|------|
| PNG | 39 | 125 (씬 참조) |
| MP3 (non-StreamingAssets) | 10 | 37 (씬 AudioClip) |
| MP3 (StreamingAssets) | 0 | **264** (런타임 풀) |

재검증: GUID·파일명·`.cs` 문자열 모두 미참조 항목만 삭제. 잔여 미참조 PNG/MP3 = **0**.

목록: `docs/media-deleted-files.txt`
