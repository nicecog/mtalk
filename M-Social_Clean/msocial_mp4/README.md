# M-Social MP4 목록

> **최종 갱신:** 2026-06-14  
> 원본: `M-Social_Clean/Assets/`  
> 백업: `M-Social_Clean/msocial_mp4/` (원본 보존, 수정 안 함)

## 요약

| 항목 | 백업 (원본) | Assets (재인코딩 후) |
|------|-------------|----------------------|
| 파일 수 | 83 | 83 |
| 총 용량 | **1,229 MB** | **123 MB** (-90%) |

## 카테고리별

| Category | 파일 수 | 원본 | 재인코딩 후 |
|----------|---------|------|-------------|
| **Movie** | 67 | 611 MB | 57 MB |
| **StreamingAssets** | 15 | 609 MB | 65 MB |
| **Animations** | 1 | 10 MB | 0.6 MB |

## 디렉터리

| 경로 | 역할 |
|------|------|
| `msocial_mp4/` | **원본 백업** (1,229 MB) |
| `msocial_mp4/optimized/` | 재인코딩 결과 |
| `Assets/**/*.mp4` | **앱이 사용하는 파일** (교체 완료) |
| `MSOCIAL_MP4_MANIFEST.csv` | 파일 목록·용량·권장 액션 |
| `REENCODE_LOG.csv` | 파일별 CRF·절감률 로그 |

## 재인코딩 설정 (적용 완료)

- **비디오:** H.264 Main, 1280×720, yuv420p, `-movflags +faststart`
- **오디오:** AAC 128 kbps (무음은 `-an`)
- **CRF:** 춤/바디 23, Movie 24, 호흡·숫자 25, Animations 26

```bash
ffmpeg -i input.mp4 \
  -vf "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2" \
  -c:v libx264 -profile:v main -level 4.0 -crf 23 -preset medium \
  -c:a aac -b:a 128k -movflags +faststart output.mp4
```

재실행:

```powershell
powershell -File "F:\PROJECT\Unity_Projects\M-Social_Clean\scripts\Reencode-MsocialMp4.ps1"
```

## APK 용량 영향

| APK | MP4 상태 | 용량 |
|-----|----------|------|
| 이전 | 원본 MP4 | ~1.37 GB |
| 현재 | 재인코딩 MP4 | **~272 MB** |

## 서버 이전 시 (선택)

- CAFE24: `/api/mface-images` 또는 전용 static 경로에 업로드
- 클라이언트: `VideoPlayer.url` 또는 `persistentDataPath` 캐시

## 저사양 기기 (Tab A 8.0)

앱 런타임에서 `MSocialDeviceProfile`이 RAM에 따라 비디오 버퍼를 자동 축소:

- RAM < 2.8 GB: 960×540
- RAM < 3.5 GB: 1280×720

상세 작업 이력: [docs/SESSION_HANDOFF_2026-06-14.md](../docs/SESSION_HANDOFF_2026-06-14.md)
