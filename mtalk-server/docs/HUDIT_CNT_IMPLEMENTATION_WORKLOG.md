# HUDIT CNT 기능 구현 작업 정리

> 작성일: 2026-06-13  
> 프로젝트: `mtalk` (Spring Boot HUDIT 포털)  
> 배포 대상: `http://hudit.cafe24.com:8080`  
> 원칙: **cnt.re.kr 레거시 소스는 수정하지 않음** — DB 직접 조회·쓰기 + HTML 프록시 방식

---

## 1. 작업 개요

CAFE24 VPS에 운영 중인 **HUDIT 통합 포털**에 CNT(뇌인지기능검사) 관리 기능을 추가했다.  
기존 MTALK 관리 패턴(대시보드 카드 → 전용 페이지 → REST API)을 따르며, CNT 데이터는 Gabia MySQL(`dbhuditcnt`)에서 직접 읽고 일부 쓰기(센터 수정·회원 생성)를 수행한다.

### 핵심 성과

| 구분 | 내용 |
|------|------|
| UI | 센터/멤버 리스트, 회원생성·분석, 사용자별분석, PDF 개별·일괄 |
| API | `/api/hudit/cnt/*` 13개 엔드포인트 |
| PDF | cnt.re.kr `pdf4th` HTML 프록시 + HTTPS 에셋 프록시 + 브라우저 일괄 ZIP |
| 검증 | `Verify-HuditPortal.ps1` **27/27 PASS** |
| 레거시 | cnt.re.kr Java/템플릿 **미수정** |

---

## 2. 인프라·접속 정보

### 2.1 시스템 구성

| 시스템 | 호스트 | 용도 |
|--------|--------|------|
| HUDIT 포털 | `hudit.cafe24.com` (VPS `1.234.65.46`) | Spring Boot Docker, 포트 8080 |
| CNT DB | `db.cnt.re.kr:3306` / `dbhuditcnt` | 센터·멤버·게임·해석 데이터 |
| CNT 웹(레거시) | `http://cnt.re.kr` | PDF HTML 원본, 외부 링크 대상 |

### 2.2 환경 변수 파일 (git 제외)

| 파일 | 용도 |
|------|------|
| `mtalk/.env.cafe24` | CAFE24 VPS 배포 (PostgreSQL, HUDIT 로그인, CNT DB 등) |
| `mtalk/.env.gabia-cnt` | CNT DB 로컬 개발 참고 |

민감 정보(비밀번호)는 저장소에 커밋하지 않으며, 배포 시 `.env.cafe24` → VPS `/opt/mtalk/.env`로 업로드한다.

### 2.3 Spring 데이터소스

`CntDataSourceConfig.java` — 3번째 JDBC datasource (`cntJdbcTemplate`)

```yaml
# application.yml / application-cafe24.yml
spring.datasource.cnt:
  url: jdbc:mysql://${CNT_DB_HOST:db.cnt.re.kr}:3306/${CNT_DB_NAME:dbhuditcnt}
  username: ${CNT_DB_USER}
  password: ${CNT_DB_PASSWORD}

hudit.cnt:
  legacy-base-url: ${CNT_LEGACY_BASE_URL:http://cnt.re.kr}
  pdf-template-suffix: ${CNT_PDF_TEMPLATE_SUFFIX:pdf4th}
  pdf-fetch-timeout-ms: ${CNT_PDF_FETCH_TIMEOUT_MS:45000}
```

---

## 3. 구현 단계별 상세

### 3.1 1단계 — CNT 기본 인프라 (MTALK 패턴)

| # | 작업 | 상세 |
|---|------|------|
| 1 | 대시보드 연동 | `dashboard.html` CNT 카드 → `/hudit/cnt` 이동 |
| 2 | MySQL datasource | `CntDataSourceConfig.java` — read/write CNT DB |
| 3 | 인증 | `PortalAuthService.requirePortalAdmin()` — HUDIT 세션 필수 |
| 4 | 설정 API | `GET /api/hudit/cnt/config` — legacy URL, 생성·분석 링크 |
| 5 | 라우팅 | `PortalViewController` → `forward:/hudit/cnt.html` |

### 3.2 2단계 — 센터 리스트

| 항목 | 구현 |
|------|------|
| API | `GET /api/hudit/cnt/centers` |
| SORT | 순번(`no`), ID(`centerId`), 센터명(`centerNm`), 등록일(`auditDt`) |
| DISPLAY | 10 / 20 / 30 / 100 |
| 컬럼 | NO, 센터ID, 센터명, 노출여부, 검사대상, 비고 |
| 비고 버튼 | 회원생성, 분석, **PDF 일괄** |
| 센터 생성 | `POST /api/hudit/cnt/centers` — HUDIT 입력 화면에서 DB 직접 생성 |

### 3.3 3단계 — 멤버 리스트

| 항목 | 구현 |
|------|------|
| API | `GET /api/hudit/cnt/members` |
| SORT | NO, USER ID, NAME, 센터 ID, 센터명 |
| 컬럼 | NO, USER ID, NAME, 센터 ID, 센터명, 나이, 비고 |
| 비고 버튼 | 분석, PDF |

### 3.4 4단계 — 탭 구조 (`cnt.html`)

| 탭 | 접근 방식 |
|----|-----------|
| 센터 리스트 | 탭 클릭 가능 |
| 멤버 리스트 | 탭 클릭 가능 |
| 회원생성/분석 | 센터 행 **회원생성**·**분석** 버튼으로만 진입 (탭 직접 클릭 불가) |
| 사용자별분석 | 멤버 **분석** 버튼으로만 진입 (탭 직접 클릭 불가) |

### 3.5 5단계 — 회원생성/분석 탭 (센터 상세)

| 기능 | API / 동작 |
|------|------------|
| 센터 생성 | `POST /api/hudit/cnt/centers` — 센터 ID 중복 확인 후 직접 생성 |
| 센터 상세 조회 | `GET /api/hudit/cnt/centers/{no}` |
| 센터 정보 저장 | `PUT /api/hudit/cnt/centers/{no}` — 센터명, 노출여부, 검사대(`max_session`) |
| 회원 일괄 생성 | `POST /api/hudit/cnt/centers/{no}/members/bulk` |
| 센터별 멤버 목록 | `GET /api/hudit/cnt/members?centerId={id}` |
| 회원생성 모드 | 회원 생성 UI + 센터 정보 저장 표시 |
| 분석 모드 | 회원 생성 영역·센터 저장 버튼 숨김, 멤버 분석·PDF만 |

**회원 일괄 생성 규칙 (cnt.re.kr 동일)**

- User ID: `{센터ID}{3자리순번}` (예: `KMB041`)
- 비밀번호: User ID와 동일 (BCrypt 인코딩)
- 이름: `이름{n}`, 성별 교대, 나이 0

**센터 ID 권장 형식**

- 대상 구분 `K/E/M/H/U` + 영문 2자 (예: `KMB`)
- 권장 형식과 다른 ID도 경고만 표시하고 생성 가능
- 빈 ID와 기존 `center_id` 중복은 생성 불가

**센터 생성·수정 공통 — 검사 대(구분) `max_session`**

| value | 표시 |
|-------|------|
| `0` | 그룹 대상을 선택해주세요 |
| `1` | 유아 |
| `2` | 성인 |
| `3` | 3~4세용 |

- 생성: `POST /cnt/centers` body `maxSession`
- 수정: `PUT /cnt/centers/{no}` body `maxSession`
- cnt.re.kr edit 화면에는 검사대 수정 UI 없음 → HUDIT 상세에서 수정 가능

**회원 일괄 생성 완료 알림 (추가)**

- 성공 시 `window.alert()` 로 처리 내역 표시
- API 응답: `createdCount`, `centerId`, `firstUserId`, `lastUserId`
- 처리 중 버튼 `생성 중...` + 비활성화
- 실패 시 alert + 상단 배너 오류

### 3.6 6단계 — 사용자별분석 탭

| 항목 | 구현 |
|------|------|
| API | `GET /api/hudit/cnt/members/{userId}/analysis` |
| 서비스 | `PortalCntService.getMemberAnalysis()` |
| 섹션 (5개) | 01 Emotion, 02 NBack, 03 mentalrotation, 04 SORT(cardsort), 05 tap |
| 데이터 소스 | `cnt_emotion`, `cnt_nback`, `cnt_mental_rotation`, `cnt_cardsort`, `cnt_tap` |
| 진입 | 멤버 리스트·센터 멤버 리스트 **분석** 버튼 → 내부 탭 (cnt.re.kr 외부 이동 제거) |
| PDF 버튼 | 우측 상단 **상세화면(PDF)** |

---

## 4. PDF 기능 (개별 + 일괄)

### 4.1 cnt.re.kr PDF 처리 방식 (분석 결과)

레거시는 **서버에서 PDF 바이너리를 생성하지 않는다.**

1. `GET /cnt/admin/members/{userId}/pdf4th` → Thymeleaf HTML (~600KB)
2. `commonpdf.css` + echarts + html2pdf.js
3. 브라우저 **Download PDF** 버튼 → 7~8개 section A4 병합
4. cnt.re.kr 소스의 bulk PDF(`CenterBulkPdfAsyncWorker`)는 HTML ZIP 수집 (운영 서버 미배포 확인)

### 4.2 CAFE24 개별 PDF — HTML 프록시

**선택 이유:** 레거시 미수정, 차트·해석 fidelity 유지

| 구성요소 | 파일 |
|----------|------|
| 프록시 서비스 | `PortalCntPdfProxyService.java` |
| HTML fetch | `http://cnt.re.kr/cnt/admin/members/{userId}/pdf4th` |
| Mixed Content 해결 | `/api/hudit/cnt/pdf-assets/**` — CSS·이미지 HTTPS 중계 |
| 자동 다운로드 | **비활성화** (수동 Download PDF) |

**API**

```
GET /api/hudit/cnt/members/{userId}/pdf
GET /api/hudit/cnt/pdf-assets/css/commonpdf.css
GET /api/hudit/cnt/pdf-assets/images/...
```

**프론트 연동 (3곳)**

1. 멤버 리스트 — PDF 버튼  
2. 회원생성/분석 — 상세화면(PDF)  
3. 사용자별분석 — 우측 상단 상세화면(PDF)

### 4.3 PDF 일괄 다운로드 — 1.5단계 하이브리드

서버 부하 최소 + 실제 `.pdf` ZIP — **브라우저에서 PDF 생성**

| 항목 | 내용 |
|------|------|
| 진입 | 센터 리스트 **PDF 일괄** 버튼 |
| 서버 역할 | 멤버 목록 API + HTML 프록시만 |
| 클라이언트 | iframe 로드 → echarts 대기 → html2pdf blob → JSZip |
| 산출물 | `center-{센터ID}-pdfs.zip` |
| ZIP 내용 | `CNT-{userId}_{이름}.pdf`, `README.txt`, 실패 시 `.pdf.error.txt` |

**진행 UI (`cnt-bulk-pdf.js` + 오버레이)**

- 전체 화면 차단 (다른 클릭 불가)
- 「다운로드 준비 중입니다. 잠시만 기다려주세요.」
- 처리 건수 `N / 총건수`, 진행률 바, %
- 진행바 아래: **총 N건 · 완료 N건 · 에러 N건**
- 현재 처리 User ID·이름, 단계(로딩/차트/PDF/ZIP)
- 취소 버튼, `beforeunload` 경고, 완료 후 닫기

**버그 수정 이력**

| 이슈 | 원인 | 해결 |
|------|------|------|
| CSS 깨짐 | HTTPS 페이지에서 HTTP cnt.re.kr 리소스 차단 | pdf-assets 프록시 |
| 잘못된 템플릿 | `/pdf` vs `/pdf4th` | `pdf-template-suffix: pdf4th` |
| `Invalid margin array` | 부모 창에서 html2pdf 체인 호출 | iframe 내부에서 페이지와 동일 스크립트 실행 |
| 0/0 진행 표시 | 총건수 조회 전 표시 | 멤버 수 선조회 후 `0 / 총건수` |
| 처리 건수 미갱신 | 완료 후에만 갱신 | 성공·실패 각각 후 `completed` 증가 |

### 4.4 PDF 방식 비교 (검토 기록)

| 방식 | 장점 | 단점 | 채택 |
|------|------|------|------|
| HTML ZIP (서버) | 서버 가벼움 | HTML마다 수동 PDF | △ |
| **1.5 브라우저 하이브리드** | PDF ZIP + 서버 가벼움 | 탭 유지·수 분 소요 | **○** |
| Playwright 서버 렌더 | 완전 자동 PDF | VPS 부하·Alpine Chromium | △ |
| cnt.re.kr bulk API 프록시 | 구현 간단 | 운영 미배포, HTML ZIP만 | ✕ |

---

## 5. REST API 목록

기본 prefix: `/api/hudit` (세션 인증 필요)

| Method | Path | 설명 |
|--------|------|------|
| GET | `/cnt/config` | legacy URL, 외부 링크 |
| GET | `/cnt/centers` | 센터 페이징·정렬 |
| POST | `/cnt/centers` | 센터 생성 |
| GET | `/cnt/centers/{no}` | 센터 상세 |
| PUT | `/cnt/centers/{no}` | 센터 수정 |
| POST | `/cnt/centers/{no}/members/bulk` | 회원 일괄 생성 |
| GET | `/cnt/members` | 멤버 페이징·정렬 (`centerId` 필터) |
| GET | `/cnt/members/{userId}/analysis` | 사용자별 분석 5섹션 |
| GET | `/cnt/members/{userId}/pdf` | PDF HTML 프록시 |
| GET | `/cnt/pdf-assets/**` | PDF용 CSS·이미지 프록시 |

### DTO 요약

| DTO | 필드 |
|-----|------|
| `PortalCntCenterResponse` | no, centerId, centerNm, openYn, maxSession, sessionLabel, auditDt |
| `PortalCntCreateCenterRequest` | centerId, centerNm, openYn, maxSession |
| `PortalCntUpdateCenterRequest` | centerNm, openYn, maxSession |
| `PortalCntMemberResponse` | no, userId, userName, centerId, centerNm, userAge |
| `PortalCntCreateMembersResponse` | createdCount, centerId, firstUserId, lastUserId |
| `PortalCntMemberAnalysisResponse` | member, sections[] |
| `PortalCntPageResponse<T>` | items, total, page, size, totalPages |

---

## 6. DB 테이블 (CNT MySQL)

| 테이블 | 용도 |
|--------|------|
| `cnt_center` | 센터 |
| `cnt_member` | 멤버 |
| `cnt_emotion` | 01 감정지각 |
| `cnt_nback` | 02 작업기억 |
| `cnt_mental_rotation` | 03 시공간지각 |
| `cnt_cardsort` | 04 종합집행 |
| `cnt_tap` | 05 운동협응 |
| `cnt_interpretation` | 해석 (PDF HTML에 포함, analysis API에는 미포함) |

---

## 7. 파일·디렉터리 목록

### 7.1 백엔드 (Java)

| 경로 | 역할 |
|------|------|
| `config/CntDataSourceConfig.java` | CNT MySQL datasource |
| `portal/service/PortalCntService.java` | 센터·멤버·분석·회원생성 |
| `portal/service/PortalCntPdfProxyService.java` | PDF HTML·에셋 프록시 |
| `portal/controller/PortalApiController.java` | CNT REST 엔드포인트 |
| `portal/controller/PortalViewController.java` | `/hudit/cnt` 뷰 |
| `portal/dto/PortalCnt*.java` | CNT 응답·요청 DTO |

### 7.2 프론트엔드 (정적)

| 경로 | 역할 |
|------|------|
| `static/hudit/cnt.html` | CNT 메인 UI (탭·리스트·상세·분석) |
| `static/hudit/css/portal.css` | 포털·CNT·bulk PDF 오버레이 스타일 |
| `static/hudit/js/portal.js` | 공통 fetch·인증 |
| `static/hudit/js/cnt-bulk-pdf.js` | PDF 일괄 생성 (1.5단계) |
| `static/hudit/dashboard.html` | CNT 카드 |

### 7.3 설정·배포·검증

| 경로 | 역할 |
|------|------|
| `src/main/resources/application.yml` | CNT datasource·hudit.cnt 설정 |
| `src/main/resources/application-cafe24.yml` | CAFE24 프로필 |
| `docker-compose.prod.yml` | VPS Docker Compose |
| `Dockerfile` | JRE 21 Alpine 이미지 |
| `.env.cafe24` | VPS 환경 변수 (git 제외) |
| `scripts/Deploy-Cafe24Vps.ps1` | 빌드·업로드·Docker 배포 |
| `scripts/Verify-HuditPortal.ps1` | HUDIT 27단계 자동 검증 |

---

## 8. 배포·검증

### 8.1 배포 절차

```powershell
cd mtalk\scripts
.\Deploy-Cafe24Vps.ps1
```

- `mvn package` → JAR 빌드  
- VPS `/opt/mtalk` 업로드  
- `docker compose up -d`  
- `GET /health` 확인

### 8.2 검증 (`Verify-HuditPortal.ps1`)

최종: **27/27 PASS**

| 카테고리 | 검증 항목 |
|----------|-----------|
| 기본 | health, 로그인, 세션, 대시보드 |
| MTALK | mbench/mbody/mface/msocial 사용자 CRUD 샘플 |
| CNT | config, centers, members, center 상세, centerId 필터 |
| 분석 | `/members/{userId}/analysis` 5섹션 |
| PDF | HTML 프록시, pdf-assets CSS, bulk 페이지 asset |
| 인증 | logout 후 세션 무효 |

```powershell
cd mtalk\scripts
.\Verify-HuditPortal.ps1
# 기본 URL: http://hudit.cafe24.com:8080
```

---

## 9. UI 화면별 기능 매트릭스

| 화면 | 주요 액션 |
|------|-----------|
| 대시보드 | CNT 관리 진입 |
| 센터 리스트 | 정렬·페이징, 센터·회원 생성, 분석, **PDF 일괄** |
| 멤버 리스트 | 정렬·페이징, 분석, PDF |
| 회원생성/분석 | 센터 저장, 회원 일괄 생성(alert), 멤버 목록, 분석, PDF |
| 사용자별분석 | 5섹션 테이블, PDF, 이전 목록 |
| PDF 일괄 오버레이 | 진행·취소·ZIP 다운로드 |

---

## 10. 제약·알려진 사항

1. **cnt.re.kr 소스 미수정** — PDF·해석 로직 변경은 CAFE24 측 프록시·UI만 적용  
2. **CNT DB 쓰기** — 센터 수정·회원 생성만 HUDIT에서 수행 (게임 데이터·해석은 레거시)  
3. **PDF 자동 다운로드** — 비활성화, 사용자가 Download PDF 클릭  
4. **PDF 일괄** — 브라우저 성능·메모리에 의존, 멤버 많을수록 시간 소요  
5. **cnt.re.kr HTTPS** — 미지원 → 에셋은 HUDIT 프록시 필수  
6. **회원 생성 이름** — `이름1`, `이름2` … (레거시와 동일, 추후 개선 가능)

---

## 11. 작업 타임라인 요약

| 순서 | 작업 |
|------|------|
| 1 | CNT MySQL datasource + 센터/멤버 API + 기본 UI |
| 2 | 대시보드·탭·정렬·페이징·컬럼 확장 |
| 3 | 회원생성/분석 탭, 센터 수정, 회원 bulk API |
| 4 | 사용자별분석 API·UI (5섹션) |
| 5 | 개별 PDF HTML 프록시 |
| 6 | Mixed Content 수정 (pdf-assets, pdf4th) |
| 7 | PDF 일괄 1.5단계 (브라우저 ZIP) |
| 8 | 일괄 진행 UI (총건수·완료·에러·취소) |
| 9 | html2pdf `Invalid margin array` 수정 (iframe 내부 실행) |
| 10 | 회원 일괄 생성 alert + firstUserId/lastUserId 응답 |
| 11 | HUDIT 센터 생성 (cnt.re.kr 외부 링크 제거, DB INSERT) — `a5480f3` |
| 12 | 센터 생성·수정에 `max_session`(검사대) 추가, 조직명·노출 수정 — `d1eee24` |
| 13 | 조직명 변경 DB 영향도 분석 (운영 DB 확인, 영향 낮음) |

---

## 12. 참고 링크·문서

| 문서 | 경로 |
|------|------|
| CAFE24 VPS 배포 리포트 | `docs/CAFE24_VPS_DEPLOYMENT_REPORT.md` |
| MTALK API 검증 | `docs/MTALK_API_VERIFICATION_LATEST.md` |
| 본 작업 정리 | `docs/HUDIT_CNT_IMPLEMENTATION_WORKLOG.md` (이 문서) |
| 2026-07-15 세션 종합 | `docs/HUDIT_CNT_SESSION_20260715.md` |

| 외부 URL | 용도 |
|----------|------|
| http://hudit.cafe24.com:8080/hudit/cnt | CNT 관리 UI |
| http://cnt.re.kr/cnt/admin/members/{userId}/pdf4th | PDF HTML 원본 |
| http://cnt.re.kr | 레거시 CNT 관리 및 PDF HTML 원본 |

---

## 13. 향후 개선 후보 (미구현)

- PDF 서버 캐시 (동일 멤버 재다운로드 시 부하 감소)
- Playwright 기반 서버 PDF (야간 배치 등)
- 회원 일괄 생성 시 이름·연령 규칙 커스터마이즈
- HTTPS nginx 리버스 프록시 정식 도메인 (`Setup-NginxHttps.ps1` 참고)
- `Verify-HuditPortal.ps1`에 bulk PDF E2E (브라우저 자동화) 추가

---

*이 문서는 HUDIT CNT CAFE24 이관 작업의 구현·검증·설계 결정을 한곳에 정리한 것이다. 코드 변경 시 본 문서도 함께 갱신하는 것을 권장한다.*
