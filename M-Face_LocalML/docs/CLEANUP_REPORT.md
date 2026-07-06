# M-Face_Clean 정리 보고서

> 작성: 2026-06-10  
> 원본: `M-Face` → 작업본: `M-Face_Clean`

## 삭제 항목

| 항목 | 사유 |
|------|------|
| `Assets/NatSuite/` (전체) | 게임 코드에서 미참조, MP4 녹화 미사용 |
| `Assets/Scenes/SampleScene.unity` | 빌드 미포함 2D 템플릿 잔재 |
| PNG 39개 | GUID·파일명·코드 미참조 (구 UI/중복 Item 스프라이트) |
| MP3 10개 | `MUZI 음성 파일/` + 루트 1개, GUID·코드 미참조 |
| **합계** | **59개 미디어 + NatSuite + SampleScene** (~11 MB) |

> `StreamingAssets/*.mp3` 264개는 `Stage1Manager` 동적 로드(`H2-05b.mp3` 패턴)로 **유지**.

목록: `docs/media-deleted-files.txt`

## 코드 변경

| 파일 | 변경 |
|------|------|
| `MFaceApiClient.cs` | 신규 — API URL/세션 중앙화 |
| `Login.cs` | MFaceApiClient 사용, CSV 버그 수정 |
| `WebCam.cs` | MFaceApiClient 업로드 |
| `WebCamStage2.cs` | seq 버그 수정, MFaceApiClient, BypassCertificate 제거 |

## 빌드 설정

- Android targetSdk: 28 → **33**
- Architecture: ARMv7 → **ARM64**
- Gradle: jcenter 제거, mavenCentral, AGP 4.0.1, R8 활성화
