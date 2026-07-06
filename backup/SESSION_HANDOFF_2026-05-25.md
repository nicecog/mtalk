# MBody Revised Session Handoff

작성 시각: 2026-05-25 밤

## 현재 상태 요약

- Unity 프로젝트 `F:\PROJECT\Unity_Projects\MBody_Revised` 는 로컬 서버에 붙을 수 있도록 설정 완료.
- 서버 프로젝트 `F:\PROJECT\Unity_Projects\MBody_Revised_server` 는 Spring Boot + MySQL 기반으로 구성 완료.
- 서버 관리자 화면 추가 완료.
- 로컬 서버 현재 정상 응답 확인:
  - `http://127.0.0.1:21250/api/health` -> `ok`
  - `http://127.0.0.1:21250/admin/` -> `200 OK`

## 이번 세션에서 완료한 핵심 작업

### Unity 쪽

- `Assets/JsonRequest.cs`
  - API 대상 URL을 `Local / Remote / Custom` 으로 선택 가능하게 수정.
  - 데스크톱 로컬 URL: `http://127.0.0.1:21250`
  - 안드로이드 에뮬레이터 로컬 URL: `http://10.0.2.2:21250`
  - 세션 쿠키 수동 관리 추가.
  - 로그인/업로드 응답 JSON 파싱 강화.

- `Assets/Scenes/MBody.unity`
  - `JsonRequest` 직렬화 값 반영 완료:
    - `apiTarget: Local`
    - `localDesktopUrl: http://127.0.0.1:21250`
    - `localAndroidEmulatorUrl: http://10.0.2.2:21250`
    - `remoteUrl: https://59.0.80.194:21250`
  - 직렬화 필드명을 `name` -> `userName` 으로 맞춤.

### 서버 쪽

- 서버 프로젝트 경로:
  - `F:\PROJECT\Unity_Projects\MBody_Revised_server`

- 추가/수정 주요 파일:
  - `src/main/java/com/mbody/server/service/AuthService.java`
  - `src/main/java/com/mbody/server/controller/AdminController.java`
  - `src/main/java/com/mbody/server/dto/UpdateCredentialsRequest.java`
  - `src/main/java/com/mbody/server/repository/UserAccountRepository.java`
  - `src/main/resources/static/admin/index.html`

- 추가된 관리자 기능:
  - 관리자 세션 조회: `GET /api/admin/session`
  - 계정 목록 조회: `GET /api/admin/accounts`
  - 계정 ID/비밀번호 변경: `PUT /api/admin/accounts/{seq}/credentials`
  - 관리자 웹 화면: `GET /admin/`

## 관리자 화면 접속 정보

- URL:
  - `http://127.0.0.1:21250/admin/`

- 기본 관리자 계정:
  - ID: `super-admin`
  - PW: `super-admin!@#$`

## 스모크 테스트 결과

다음 항목 실제 확인 완료:

- 서버 빌드 성공:
  - `mvn -q -DskipTests package`
- 관리자 페이지 응답 성공
- `super-admin` 로그인 성공
- 계정 목록 조회 성공
- 테스트 계정 생성 성공
- 테스트 계정의 ID/비밀번호 변경 성공
- 변경된 자격증명으로 재로그인 성공

## 현재 주의사항

- 스모크 테스트용 계정이 DB에 1건 남아 있음:
  - `admin-smoke-20260525-2158-upd`
- 필요 시 내일 삭제 또는 정리 필요.

## 내일 바로 이어서 할 일

우선순위 추천:

1. Unity 에디터에서 실제 로컬 서버 로그인 흐름 확인
2. 안드로이드 에뮬레이터에서 `10.0.2.2:21250` 경로로 실제 통신 확인
3. 필요 시 관리자 화면에 계정 삭제 기능 추가
4. 스모크 테스트용 계정 정리
5. 실제 영상 업로드 -> `subject-videos` 등록까지 end-to-end 재검증

## 빠른 실행 메모

### 서버 프로젝트 빌드

```powershell
cd F:\PROJECT\Unity_Projects\MBody_Revised_server
mvn -q -DskipTests package
```

### 서버 실행

```powershell
java -jar "F:\PROJECT\Unity_Projects\MBody_Revised_server\target\mbody-revised-server-0.0.1-SNAPSHOT.jar"
```

### 서버 상태 확인

```powershell
Invoke-RestMethod -Uri "http://127.0.0.1:21250/api/health"
```

### 관리자 페이지 열기

브라우저에서:

```text
http://127.0.0.1:21250/admin/
```

## 다음 세션 시작용 한 줄 메모

다음 시작점은 "Unity 에디터/안드로이드 에뮬레이터에서 현재 로컬 서버에 실제 로그인/업로드가 정상 동작하는지 end-to-end 검증" 입니다.
