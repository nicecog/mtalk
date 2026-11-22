-- Add JMCS project from 2026-08-23 local architecture/deploy review.
INSERT INTO hudit_projects (title, category, status, progress, summary, owner_name, created_by, updated_by)
SELECT
    'JMCS 진료 연계 시스템',
    'JMCS',
    'in_progress',
    75,
    '환자 앱·의사 태블릿·진료실 PC·환자/관리 서버 모노레포. 일상 기록은 온라인 API, 진료 전달은 QR·BLE·TCP로 오프라인 전달합니다. 운영은 camaplus.cafe24.com /jmcs · /jmcs_admin.',
    'HUDIT',
    'system',
    'system'
WHERE NOT EXISTS (
    SELECT 1 FROM hudit_projects WHERE title = 'JMCS 진료 연계 시스템'
);

INSERT INTO hudit_project_updates (project_seq, content, progress, created_by)
SELECT p.seq, u.content, u.progress, 'system'
FROM hudit_projects p
CROSS JOIN (
    VALUES
        ('GitHub nicecog/JMCS를 F:\PROJECT\JMCS로 클론했습니다. HEAD 212b3db — 의료진별 유연 질문지(admin/server/patient).', 40),
        ('로컬 소스 분석: 7개 앱(patient-server, jmcs-admin, patient/doctor Android·iOS, jmcs-windows). 온라인은 /jmcs/api + Postgres, 진료실은 QR·BLE 후 TCP 18765로 Windows PC에 전달하며 이 구간은 환자 서버를 호출하지 않습니다. 문의는 PIN+AES-GCM, 문진·바이탈은 DB 평문입니다.', 60),
        ('Cafe24 배포 확인(2026-08-23): camaplus.cafe24.com/jmcs, /jmcs_admin 헬스 UP. 호스트 Nginx → 127.0.0.1:8090/8091. 원격 /opt/jmcs/app, /opt/jmcs_admin/app, DB명 jmcs. 환자 웹·관리자·crypto 가동. 앱/Windows는 VPS에 없고 기기 설치. camaplus.co.kr은 DNS가 없습니다.', 75)
) AS u(content, progress)
WHERE p.title = 'JMCS 진료 연계 시스템'
  AND NOT EXISTS (
      SELECT 1 FROM hudit_project_updates x
      WHERE x.project_seq = p.seq
        AND x.content = u.content
  );

UPDATE hudit_projects
SET updated_at = NOW(),
    updated_by = 'system',
    progress = 75,
    status = 'in_progress'
WHERE title = 'JMCS 진료 연계 시스템';
