package com.mtalk.portal.service;

import com.mtalk.portal.dto.PortalCntCenterResponse;
import com.mtalk.portal.dto.PortalCntConfigResponse;
import com.mtalk.portal.dto.PortalCntCreateMembersResponse;
import com.mtalk.portal.dto.PortalCntMemberAnalysisResponse;
import com.mtalk.portal.dto.PortalCntMemberAnalysisResponse.PortalCntAnalysisSection;
import com.mtalk.portal.dto.PortalCntMemberAnalysisResponse.PortalCntMemberDetail;
import com.mtalk.portal.dto.PortalCntMemberResponse;
import com.mtalk.portal.dto.PortalCntPageResponse;
import com.mtalk.portal.dto.PortalCntUpdateCenterRequest;
import jakarta.servlet.http.HttpSession;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.web.server.ResponseStatusException;

import java.math.BigDecimal;
import java.sql.Date;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Timestamp;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Set;

@Service
public class PortalCntService {

    private static final Set<String> CENTER_SORTS = Set.of("no", "centerId", "centerNm", "auditDt");
    private static final Set<String> MEMBER_SORTS = Set.of("no", "userId", "userName", "centerId", "centerNm");
    private static final Set<Integer> PAGE_SIZES = Set.of(10, 20, 30, 100);
    private static final DateTimeFormatter AUDIT_FMT = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm");

    private static final Map<String, String> CENTER_SORT_COLUMNS = Map.of(
            "no", "no",
            "centerId", "center_id",
            "centerNm", "center_nm",
            "auditDt", "audit_dt"
    );

    private static final Map<String, String> MEMBER_SORT_COLUMNS = Map.of(
            "no", "m.no",
            "userId", "m.user_id",
            "userName", "m.user_name",
            "centerId", "m.center_id",
            "centerNm", "c.center_nm"
    );

    private final NamedParameterJdbcTemplate cntJdbcTemplate;
    private final PortalAuthService portalAuthService;
    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();
    private final String legacyBaseUrl;

    public PortalCntService(
            @Qualifier("cntJdbcTemplate") NamedParameterJdbcTemplate cntJdbcTemplate,
            PortalAuthService portalAuthService,
            @Value("${hudit.cnt.legacy-base-url:http://cnt.re.kr}") String legacyBaseUrl
    ) {
        this.cntJdbcTemplate = cntJdbcTemplate;
        this.portalAuthService = portalAuthService;
        this.legacyBaseUrl = trimTrailingSlash(legacyBaseUrl);
    }

    public PortalCntConfigResponse getConfig(HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        String base = legacyBaseUrl;
        return new PortalCntConfigResponse(
                base,
                base + "/cnt/admin/centers/create",
                base + "/cnt/admin/members/create",
                base + "/cnt/admin/result",
                base + "/cnt/admin/interpretation"
        );
    }

    public PortalCntPageResponse<PortalCntCenterResponse> listCenters(
            String sort,
            String order,
            int page,
            int size,
            HttpSession session
    ) {
        portalAuthService.requirePortalAdmin(session);
        String sortColumn = resolveSort(sort, CENTER_SORTS, CENTER_SORT_COLUMNS, "no");
        String sortOrder = resolveOrder(order);
        int pageSize = resolvePageSize(size);
        int pageNumber = Math.max(page, 1);
        int offset = (pageNumber - 1) * pageSize;

        long total = countRows("cnt_center");
        List<PortalCntCenterResponse> items = cntJdbcTemplate.query(
                """
                SELECT no, center_id, center_nm, open_yn, max_session, audit_dt
                FROM cnt_center
                ORDER BY %s %s
                LIMIT :limit OFFSET :offset
                """.formatted(sortColumn, sortOrder),
                new MapSqlParameterSource()
                        .addValue("limit", pageSize)
                        .addValue("offset", offset),
                (rs, rowNum) -> mapCenter(rs.getLong("no"), rs.getString("center_id"), rs.getString("center_nm"),
                        rs.getString("open_yn"), rs.getString("max_session"), rs.getTimestamp("audit_dt"))
        );

        return new PortalCntPageResponse<>(items, total, pageNumber, pageSize, totalPages(total, pageSize));
    }

    public PortalCntCenterResponse getCenter(long no, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        List<PortalCntCenterResponse> rows = cntJdbcTemplate.query(
                """
                SELECT no, center_id, center_nm, open_yn, max_session, audit_dt
                FROM cnt_center
                WHERE no = :no
                """,
                new MapSqlParameterSource("no", no),
                (rs, rowNum) -> mapCenter(rs.getLong("no"), rs.getString("center_id"), rs.getString("center_nm"),
                        rs.getString("open_yn"), rs.getString("max_session"), rs.getTimestamp("audit_dt"))
        );
        if (rows.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "센터를 찾을 수 없습니다.");
        }
        return rows.getFirst();
    }

    public PortalCntCenterResponse updateCenter(long no, PortalCntUpdateCenterRequest request, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        getCenter(no, session);
        cntJdbcTemplate.update(
                """
                UPDATE cnt_center
                SET center_nm = :centerNm, open_yn = :openYn, audit_dt = :auditDt
                WHERE no = :no
                """,
                new MapSqlParameterSource()
                        .addValue("no", no)
                        .addValue("centerNm", request.centerNm())
                        .addValue("openYn", request.openYn())
                        .addValue("auditDt", Timestamp.valueOf(LocalDateTime.now()))
        );
        return getCenter(no, session);
    }

    public PortalCntCreateMembersResponse createCenterMembers(long no, int count, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        PortalCntCenterResponse center = getCenter(no, session);
        String centerId = center.centerId();

        Integer maxUserId = cntJdbcTemplate.queryForObject(
                """
                SELECT MAX(CAST(SUBSTRING(user_id, LENGTH(:centerId) + 1) AS SIGNED))
                FROM cnt_member
                WHERE center_id = :centerId
                """,
                new MapSqlParameterSource("centerId", centerId),
                Integer.class
        );
        int startId = maxUserId != null ? maxUserId : 0;

        List<MapSqlParameterSource> batch = new ArrayList<>();
        Date today = Date.valueOf(java.time.LocalDate.now());
        for (int i = 1; i <= count; i++) {
            String userId = centerId + String.format("%03d", startId + i);
            String encodedPw = passwordEncoder.encode(userId);
            String gender = i % 2 == 0 ? "F" : "M";
            batch.add(new MapSqlParameterSource()
                    .addValue("userId", userId)
                    .addValue("userPw", encodedPw)
                    .addValue("userName", "이름" + i)
                    .addValue("userGender", gender)
                    .addValue("userAge", 0)
                    .addValue("centerId", centerId)
                    .addValue("createdAt", today));
        }

        cntJdbcTemplate.batchUpdate(
                """
                INSERT INTO cnt_member (user_id, user_pw, user_name, user_gender, user_age, center_id, created_at)
                VALUES (:userId, :userPw, :userName, :userGender, :userAge, :centerId, :createdAt)
                """,
                batch.toArray(MapSqlParameterSource[]::new)
        );

        String firstUserId = centerId + String.format("%03d", startId + 1);
        String lastUserId = centerId + String.format("%03d", startId + count);
        return new PortalCntCreateMembersResponse(count, centerId, firstUserId, lastUserId);
    }

    public PortalCntPageResponse<PortalCntMemberResponse> listMembers(
            String sort,
            String order,
            int page,
            int size,
            String centerId,
            HttpSession session
    ) {
        portalAuthService.requirePortalAdmin(session);
        String sortColumn = resolveSort(sort, MEMBER_SORTS, MEMBER_SORT_COLUMNS, "no");
        String sortOrder = resolveOrder(order);
        int pageSize = resolvePageSize(size);
        int pageNumber = Math.max(page, 1);
        int offset = (pageNumber - 1) * pageSize;

        MapSqlParameterSource params = new MapSqlParameterSource()
                .addValue("limit", pageSize)
                .addValue("offset", offset);

        String whereClause = "";
        if (centerId != null && !centerId.isBlank()) {
            whereClause = "WHERE m.center_id = :centerId";
            params.addValue("centerId", centerId.trim());
        }

        MapSqlParameterSource countParams = new MapSqlParameterSource();
        String memberWhere = "";
        if (centerId != null && !centerId.isBlank()) {
            memberWhere = "WHERE center_id = :centerId";
            countParams.addValue("centerId", centerId.trim());
        }

        long total = countRows("cnt_member", memberWhere, countParams);
        List<PortalCntMemberResponse> items = cntJdbcTemplate.query(
                """
                SELECT m.no, m.user_id, m.user_name, m.center_id, c.center_nm, m.user_age
                FROM cnt_member m
                LEFT JOIN cnt_center c ON m.center_id = c.center_id
                %s
                ORDER BY %s %s
                LIMIT :limit OFFSET :offset
                """.formatted(whereClause, sortColumn, sortOrder),
                params,
                (rs, rowNum) -> new PortalCntMemberResponse(
                        rs.getLong("no"),
                        rs.getString("user_id"),
                        rs.getString("user_name"),
                        rs.getString("center_id"),
                        rs.getString("center_nm"),
                        rs.getInt("user_age")
                )
        );

        return new PortalCntPageResponse<>(items, total, pageNumber, pageSize, totalPages(total, pageSize));
    }

    public PortalCntMemberAnalysisResponse getMemberAnalysis(String userId, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        if (userId == null || userId.isBlank()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "userId가 필요합니다.");
        }

        PortalCntMemberDetail member = loadMemberDetail(userId.trim());
        List<PortalCntAnalysisSection> sections = new ArrayList<>();
        sections.add(buildEmotionSection(userId.trim()));
        sections.add(buildNbackSection(userId.trim()));
        sections.add(buildMentalRotationSection(userId.trim()));
        sections.add(buildCardSortSection(userId.trim()));
        sections.add(buildTapSection(userId.trim()));
        return new PortalCntMemberAnalysisResponse(member, sections);
    }

    private PortalCntMemberDetail loadMemberDetail(String userId) {
        List<PortalCntMemberDetail> rows = cntJdbcTemplate.query(
                """
                SELECT m.no, m.user_id, m.user_name, m.user_age, m.center_id, c.center_nm
                FROM cnt_member m
                LEFT JOIN cnt_center c ON m.center_id = c.center_id
                WHERE m.user_id = :userId
                """,
                new MapSqlParameterSource("userId", userId),
                (rs, rowNum) -> new PortalCntMemberDetail(
                        rs.getLong("no"),
                        rs.getString("user_id"),
                        rs.getString("user_name"),
                        rs.getInt("user_age"),
                        rs.getString("center_id"),
                        rs.getString("center_nm")
                )
        );
        if (rows.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "멤버를 찾을 수 없습니다.");
        }
        return rows.getFirst();
    }

    private PortalCntAnalysisSection buildEmotionSection(String userId) {
        List<String> headers = List.of(
                "구분(emotion)", "Audit Date", "Session", "RT M", "CR M",
                "RT All Pos M", "RT All Neu M", "RT All Neg M", "RT Pos Neu M", "RT Pos Neg M",
                "RT Neu Pos M", "RT Neu Neg M", "RT Neg Pos M", "RT Neg Neu M",
                "CR All Pos M", "CR All Neu M", "CR All Neg M", "CR Pos Neu M", "CR Pos Neg M",
                "CR Neu Pos M", "CR Neu Neg M", "CR Neg Pos M", "CR Neg Neu M"
        );
        return queryLatestSection(
                "01. Emotion",
                headers,
                """
                SELECT * FROM cnt_emotion
                WHERE user_id = :userId
                ORDER BY audit_dt DESC
                LIMIT 1
                """,
                userId,
                rs -> List.of(
                        "emotion",
                        formatAuditDt(rs.getTimestamp("audit_dt")),
                        formatGameSession(rs.getObject("session")),
                        decimal(rs, "rt_m"), decimal(rs, "cr_m"),
                        decimal(rs, "rt_allpos_m"), decimal(rs, "rt_allneu_m"), decimal(rs, "rt_allneg_m"),
                        decimal(rs, "rt_posneu_m"), decimal(rs, "rt_posneg_m"),
                        decimal(rs, "rt_neupos_m"), decimal(rs, "rt_neuneg_m"),
                        decimal(rs, "rt_negpos_m"), decimal(rs, "rt_negneu_m"),
                        decimal(rs, "cr_allpos_m"), decimal(rs, "cr_allneu_m"), decimal(rs, "cr_allneg_m"),
                        decimal(rs, "cr_posneu_m"), decimal(rs, "cr_posneg_m"),
                        decimal(rs, "cr_neupos_m"), decimal(rs, "cr_neuneg_m"),
                        decimal(rs, "cr_negpos_m"), decimal(rs, "cr_negneu_m")
                )
        );
    }

    private PortalCntAnalysisSection buildNbackSection(String userId) {
        List<String> headers = List.of(
                "구분", "Audit Date", "Session", "RT M", "CR M",
                "RT 1-Back M", "RT 2-Back M", "RT 3-Back M",
                "CR 1-Back M", "CR 2-Back M", "CR 3-Back M"
        );
        return queryLatestSection(
                "02. NBack",
                headers,
                """
                SELECT * FROM cnt_nback
                WHERE user_id = :userId
                ORDER BY audit_dt DESC
                LIMIT 1
                """,
                userId,
                rs -> List.of(
                        "nBack",
                        formatAuditDt(rs.getTimestamp("audit_dt")),
                        formatGameSession(rs.getObject("session")),
                        decimal(rs, "rt_m"), decimal(rs, "cr_m"),
                        decimal(rs, "rt_1back_m"), decimal(rs, "rt_2back_m"), decimal(rs, "rt_3back_m"),
                        decimal(rs, "cr_1back_m"), decimal(rs, "cr_2back_m"), decimal(rs, "cr_3back_m")
                )
        );
    }

    private PortalCntAnalysisSection buildMentalRotationSection(String userId) {
        List<String> headers = List.of("구분", "Audit Date", "Session", "RT M", "CR M");
        return queryLatestSection(
                "03. mentalrotation",
                headers,
                """
                SELECT * FROM cnt_mental_rotation
                WHERE user_id = :userId
                ORDER BY audit_dt DESC
                LIMIT 1
                """,
                userId,
                rs -> List.of(
                        "mentalrotation",
                        formatAuditDt(rs.getTimestamp("audit_dt")),
                        formatGameSession(rs.getObject("session")),
                        decimal(rs, "rt_m"), decimal(rs, "cr_m")
                )
        );
    }

    private PortalCntAnalysisSection buildCardSortSection(String userId) {
        List<String> headers = List.of(
                "구분", "일시", "SESSION", "rtM", "stepCntM",
                "rtCard3M", "rtCard4M", "rtCard5M", "stepCard3CntM", "stepCard4CntM", "stepCard5CntM"
        );
        return queryLatestSection(
                "04.SORT[Tower of London (CST)]",
                headers,
                """
                SELECT * FROM cnt_cardsort
                WHERE user_id = :userId
                ORDER BY audit_dt DESC
                LIMIT 1
                """,
                userId,
                rs -> List.of(
                        "SORT",
                        formatAuditDt(rs.getTimestamp("audit_dt")),
                        formatGameSession(rs.getObject("session")),
                        decimal(rs, "rt_m"), decimal(rs, "step_cnt_m"),
                        decimal(rs, "rt_card3_m"), decimal(rs, "rt_card4_m"), decimal(rs, "rt_card5_m"),
                        decimal(rs, "step_card3_cnt_m"), decimal(rs, "step_card4_cnt_m"), decimal(rs, "step_card5_cnt_m")
                )
        );
    }

    private PortalCntAnalysisSection buildTapSection(String userId) {
        List<String> headers = List.of(
                "No", "User ID", "Audit Date", "Session", "Step M", "Step Left M", "Step Right M",
                "Step Both M", "Step Both Correct M", "RT M", "RT Left M", "RT Right M", "RT Both M", "RT Both Correct M"
        );
        return queryLatestSection(
                "05. tap (Motor Coordination)",
                headers,
                """
                SELECT * FROM cnt_tap
                WHERE user_id = :userId
                ORDER BY audit_dt DESC
                LIMIT 1
                """,
                userId,
                rs -> List.of(
                        String.valueOf(rs.getLong("no")),
                        rs.getString("user_id"),
                        formatAuditDt(rs.getTimestamp("audit_dt")),
                        formatGameSession(rs.getObject("session")),
                        decimal(rs, "step_m"), decimal(rs, "step_left_m"), decimal(rs, "step_right_m"),
                        decimal(rs, "step_both_m"), decimal(rs, "step_both_correct_m"),
                        decimal(rs, "rt_m"), decimal(rs, "rt_left_m"), decimal(rs, "rt_right_m"),
                        decimal(rs, "rt_both_m"), decimal(rs, "rt_both_correct_m")
                )
        );
    }

    private PortalCntAnalysisSection queryLatestSection(
            String title,
            List<String> headers,
            String sql,
            String userId,
            AnalysisRowMapper rowMapper
    ) {
        List<List<String>> rows = cntJdbcTemplate.query(
                sql,
                new MapSqlParameterSource("userId", userId),
                (rs, rowNum) -> rowMapper.map(rs)
        );
        return new PortalCntAnalysisSection(title, headers, rows);
    }

    @FunctionalInterface
    private interface AnalysisRowMapper {
        List<String> map(ResultSet rs) throws SQLException;
    }

    private String decimal(ResultSet rs, String column) throws SQLException {
        BigDecimal value = rs.getBigDecimal(column);
        if (value == null) {
            return "-";
        }
        return value.stripTrailingZeros().toPlainString();
    }

    private String formatGameSession(Object session) {
        if (session == null) {
            return "-";
        }
        String raw = String.valueOf(session).trim();
        return switch (raw) {
            case "1" -> "유아";
            case "2" -> "성인";
            case "3" -> "3,4세용";
            default -> raw;
        };
    }

    private PortalCntCenterResponse mapCenter(
            long no,
            String centerId,
            String centerNm,
            String openYn,
            String maxSession,
            Timestamp auditDt
    ) {
        return new PortalCntCenterResponse(
                no,
                centerId,
                centerNm,
                openYn,
                formatSession(maxSession),
                formatAuditDt(auditDt)
        );
    }

    private long countRows(String table) {
        Long count = cntJdbcTemplate.queryForObject(
                "SELECT COUNT(*) FROM " + table,
                new MapSqlParameterSource(),
                Long.class
        );
        return count == null ? 0L : count;
    }

    private long countRows(String table, String whereClause, MapSqlParameterSource params) {
        Long count = cntJdbcTemplate.queryForObject(
                "SELECT COUNT(*) FROM " + table + " " + whereClause,
                params,
                Long.class
        );
        return count == null ? 0L : count;
    }

    private String resolveSort(
            String sort,
            Set<String> allowed,
            Map<String, String> columns,
            String defaultSort
    ) {
        String key = sort == null || sort.isBlank() || !allowed.contains(sort) ? defaultSort : sort;
        return columns.get(key);
    }

    private String resolveOrder(String order) {
        return "desc".equalsIgnoreCase(order) ? "DESC" : "ASC";
    }

    private int resolvePageSize(int size) {
        return PAGE_SIZES.contains(size) ? size : 20;
    }

    private int totalPages(long total, int pageSize) {
        if (total == 0) {
            return 0;
        }
        return (int) Math.ceil((double) total / pageSize);
    }

    private String formatSession(String maxSession) {
        if (maxSession == null || maxSession.isBlank()) {
            return "-";
        }
        return switch (maxSession.trim()) {
            case "1" -> "유아 (1)";
            case "2" -> "성인 (2)";
            case "3" -> "3,4세용 (3)";
            default -> maxSession;
        };
    }

    private String formatAuditDt(Timestamp auditDt) {
        if (auditDt == null) {
            return "-";
        }
        return auditDt.toLocalDateTime().format(AUDIT_FMT);
    }

    private String trimTrailingSlash(String url) {
        if (url == null || url.isBlank()) {
            return "http://cnt.re.kr";
        }
        return url.endsWith("/") ? url.substring(0, url.length() - 1) : url;
    }
}
