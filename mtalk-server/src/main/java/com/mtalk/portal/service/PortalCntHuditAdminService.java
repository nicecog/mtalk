package com.mtalk.portal.service;

import com.mtalk.portal.dto.PortalCntHuditAdminCreateRequest;
import com.mtalk.portal.dto.PortalCntHuditAdminResponse;
import com.mtalk.portal.dto.PortalCntHuditAdminUpdateRequest;
import com.mtalk.portal.dto.PortalResetPasswordRequest;
import com.mtalk.portal.dto.PortalResetPasswordResponse;
import jakarta.annotation.PostConstruct;
import jakarta.servlet.http.HttpSession;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.dao.DuplicateKeyException;
import org.springframework.http.HttpStatus;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.jdbc.support.GeneratedKeyHolder;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.web.server.ResponseStatusException;

import java.security.SecureRandom;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Timestamp;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.List;

@Service
public class PortalCntHuditAdminService {

    private static final DateTimeFormatter FMT = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm");
    private static final String RESET_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#";
    private static final SecureRandom RANDOM = new SecureRandom();

    private final NamedParameterJdbcTemplate cntHuditJdbcTemplate;
    private final PortalAuthService portalAuthService;
    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();

    public PortalCntHuditAdminService(
            @Qualifier("cntHuditJdbcTemplate") NamedParameterJdbcTemplate cntHuditJdbcTemplate,
            PortalAuthService portalAuthService
    ) {
        this.cntHuditJdbcTemplate = cntHuditJdbcTemplate;
        this.portalAuthService = portalAuthService;
    }

    @PostConstruct
    public void ensureTable() {
        cntHuditJdbcTemplate.getJdbcTemplate().execute(
                """
                CREATE TABLE IF NOT EXISTS cnthudit_admin (
                    seq BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    login_id VARCHAR(100) NOT NULL,
                    password_hash VARCHAR(255) NOT NULL,
                    display_name VARCHAR(100) NOT NULL DEFAULT 'Administrator',
                    enabled TINYINT(1) NOT NULL DEFAULT 1,
                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    last_login_at DATETIME NULL,
                    UNIQUE KEY uk_cnthudit_admin_login_id (login_id)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
                """
        );
    }

    public List<PortalCntHuditAdminResponse> listAdmins(HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        return cntHuditJdbcTemplate.query(
                """
                SELECT seq, login_id, display_name, enabled, created_at, last_login_at
                FROM cnthudit_admin
                ORDER BY seq DESC
                """,
                (rs, rowNum) -> mapAdmin(rs)
        );
    }

    public PortalCntHuditAdminResponse createAdmin(PortalCntHuditAdminCreateRequest request, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        String loginId = normalizeLoginId(request.loginId());
        String displayName = normalizeDisplayName(request.displayName(), loginId);
        String password = request.password().trim();
        if (password.length() < 4) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "비밀번호는 4자 이상이어야 합니다.");
        }
        GeneratedKeyHolder keys = new GeneratedKeyHolder();
        try {
            cntHuditJdbcTemplate.update(
                    """
                    INSERT INTO cnthudit_admin (login_id, password_hash, display_name, enabled, created_at)
                    VALUES (:loginId, :passwordHash, :displayName, 1, :createdAt)
                    """,
                    new MapSqlParameterSource()
                            .addValue("loginId", loginId)
                            .addValue("passwordHash", passwordEncoder.encode(password))
                            .addValue("displayName", displayName)
                            .addValue("createdAt", Timestamp.valueOf(LocalDateTime.now())),
                    keys,
                    new String[] {"seq"}
            );
        } catch (DuplicateKeyException ex) {
            throw new ResponseStatusException(HttpStatus.CONFLICT, "이미 사용 중인 아이디입니다.");
        }
        Number seq = keys.getKey();
        if (seq == null) {
            return findByLoginId(loginId);
        }
        return getAdmin(seq.longValue());
    }

    public PortalCntHuditAdminResponse updateAdmin(
            long seq,
            PortalCntHuditAdminUpdateRequest request,
            HttpSession session
    ) {
        portalAuthService.requirePortalAdmin(session);
        PortalCntHuditAdminResponse current = getAdmin(seq);
        String displayName = request.displayName() == null
                ? current.displayName()
                : normalizeDisplayName(request.displayName(), current.loginId());
        boolean enabled = request.enabled() == null ? current.enabled() : request.enabled();
        cntHuditJdbcTemplate.update(
                """
                UPDATE cnthudit_admin
                SET display_name = :displayName, enabled = :enabled
                WHERE seq = :seq
                """,
                new MapSqlParameterSource()
                        .addValue("seq", seq)
                        .addValue("displayName", displayName)
                        .addValue("enabled", enabled ? 1 : 0)
        );
        return getAdmin(seq);
    }

    public PortalResetPasswordResponse resetPassword(
            long seq,
            PortalResetPasswordRequest request,
            HttpSession session
    ) {
        portalAuthService.requirePortalAdmin(session);
        PortalCntHuditAdminResponse admin = getAdmin(seq);
        String newPassword = resolveNewPassword(request == null ? null : request.password());
        cntHuditJdbcTemplate.update(
                "UPDATE cnthudit_admin SET password_hash = :passwordHash WHERE seq = :seq",
                new MapSqlParameterSource()
                        .addValue("seq", seq)
                        .addValue("passwordHash", passwordEncoder.encode(newPassword))
        );
        return new PortalResetPasswordResponse(admin.seq(), admin.loginId(), newPassword);
    }

    private PortalCntHuditAdminResponse getAdmin(long seq) {
        List<PortalCntHuditAdminResponse> rows = cntHuditJdbcTemplate.query(
                """
                SELECT seq, login_id, display_name, enabled, created_at, last_login_at
                FROM cnthudit_admin
                WHERE seq = :seq
                """,
                new MapSqlParameterSource("seq", seq),
                (rs, rowNum) -> mapAdmin(rs)
        );
        if (rows.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "관리자를 찾을 수 없습니다.");
        }
        return rows.getFirst();
    }

    private PortalCntHuditAdminResponse findByLoginId(String loginId) {
        List<PortalCntHuditAdminResponse> rows = cntHuditJdbcTemplate.query(
                """
                SELECT seq, login_id, display_name, enabled, created_at, last_login_at
                FROM cnthudit_admin
                WHERE login_id = :loginId
                """,
                new MapSqlParameterSource("loginId", loginId),
                (rs, rowNum) -> mapAdmin(rs)
        );
        if (rows.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, "관리자 생성 결과를 확인할 수 없습니다.");
        }
        return rows.getFirst();
    }

    private PortalCntHuditAdminResponse mapAdmin(ResultSet rs) throws SQLException {
        return new PortalCntHuditAdminResponse(
                rs.getLong("seq"),
                rs.getString("login_id"),
                rs.getString("display_name"),
                rs.getInt("enabled") != 0,
                formatTimestamp(rs.getTimestamp("created_at")),
                formatTimestamp(rs.getTimestamp("last_login_at"))
        );
    }

    private String formatTimestamp(Timestamp timestamp) {
        if (timestamp == null) {
            return null;
        }
        return FMT.format(timestamp.toLocalDateTime());
    }

    private String normalizeLoginId(String loginId) {
        String trimmed = loginId == null ? "" : loginId.trim();
        if (trimmed.length() < 2) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "아이디는 2자 이상이어야 합니다.");
        }
        if (!trimmed.matches("[A-Za-z0-9._-]+")) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "아이디는 영문, 숫자, ._- 만 사용할 수 있습니다.");
        }
        return trimmed;
    }

    private String normalizeDisplayName(String displayName, String loginId) {
        if (displayName == null || displayName.isBlank()) {
            return loginId;
        }
        return displayName.trim();
    }

    private String resolveNewPassword(String customPassword) {
        if (customPassword != null && !customPassword.isBlank()) {
            if (customPassword.length() < 4) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "비밀번호는 4자 이상이어야 합니다.");
            }
            return customPassword;
        }
        StringBuilder builder = new StringBuilder(12);
        for (int i = 0; i < 12; i++) {
            builder.append(RESET_CHARS.charAt(RANDOM.nextInt(RESET_CHARS.length())));
        }
        return builder.toString();
    }
}
