package com.mtalk.cnthudit;

import jakarta.servlet.http.HttpSession;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.http.HttpStatus;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.web.server.ResponseStatusException;

import java.sql.Timestamp;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Map;

@Service
public class CnthuditAuthService {

    public static final String SESSION_AUTHENTICATED = "CNTHUDIT_AUTHENTICATED";
    public static final String SESSION_ADMIN_SEQ = "CNTHUDIT_ADMIN_SEQ";
    public static final String SESSION_LOGIN_ID = "CNTHUDIT_LOGIN_ID";
    public static final String SESSION_DISPLAY_NAME = "CNTHUDIT_DISPLAY_NAME";
    public static final String SESSION_SOURCE = "CNTHUDIT_SOURCE";

    public static final String SOURCE_LOCAL = "local";
    public static final String SOURCE_HUDIT = "hudit";

    private final NamedParameterJdbcTemplate cntHuditJdbcTemplate;
    private final CnthuditEntryTicketService ticketService;
    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();

    public CnthuditAuthService(
            @Qualifier("cntHuditJdbcTemplate") NamedParameterJdbcTemplate cntHuditJdbcTemplate,
            CnthuditEntryTicketService ticketService
    ) {
        this.cntHuditJdbcTemplate = cntHuditJdbcTemplate;
        this.ticketService = ticketService;
    }

    public Map<String, Object> login(String loginId, String password, HttpSession session) {
        String trimmed = loginId == null ? "" : loginId.trim();
        List<Map<String, Object>> rows = cntHuditJdbcTemplate.queryForList(
                """
                SELECT seq, login_id, password_hash, display_name, enabled
                FROM cnthudit_admin
                WHERE login_id = :loginId
                """,
                new MapSqlParameterSource("loginId", trimmed)
        );
        if (rows.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "아이디 또는 비밀번호가 올바르지 않습니다.");
        }
        Map<String, Object> row = rows.getFirst();
        if (!isEnabled(row.get("enabled"))) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "비활성화된 계정입니다.");
        }
        String hash = hashValue(row.get("password_hash"));
        if (password == null || hash.isBlank() || !passwordEncoder.matches(password, hash)) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "아이디 또는 비밀번호가 올바르지 않습니다.");
        }
        long seq = ((Number) row.get("seq")).longValue();
        String displayName = row.get("display_name") == null ? trimmed : String.valueOf(row.get("display_name"));
        cntHuditJdbcTemplate.update(
                "UPDATE cnthudit_admin SET last_login_at = :now WHERE seq = :seq",
                new MapSqlParameterSource()
                        .addValue("seq", seq)
                        .addValue("now", Timestamp.valueOf(LocalDateTime.now()))
        );
        bindSession(session, seq, trimmed, displayName, SOURCE_LOCAL);
        return sessionBody(session);
    }

    public Map<String, Object> consumeTicket(String ticket, HttpSession session) {
        CnthuditEntryTicketService.TicketPayload payload = ticketService.consume(ticket);
        bindSession(session, -1L, payload.loginId(), payload.displayName(), SOURCE_HUDIT);
        return sessionBody(session);
    }

    public void logout(HttpSession session) {
        session.removeAttribute(SESSION_AUTHENTICATED);
        session.removeAttribute(SESSION_ADMIN_SEQ);
        session.removeAttribute(SESSION_LOGIN_ID);
        session.removeAttribute(SESSION_DISPLAY_NAME);
        session.removeAttribute(SESSION_SOURCE);
    }

    public void requireAdmin(HttpSession session) {
        if (!isAuthenticated(session)) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "cnthudit login required");
        }
    }

    public boolean isMaster(HttpSession session) {
        return SOURCE_HUDIT.equals(String.valueOf(session.getAttribute(SESSION_SOURCE)));
    }

    public void requireMaster(HttpSession session) {
        requireAdmin(session);
        if (!isMaster(session)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "센터 생성과 사용자 추가는 HUDIT 마스터 관리자만 가능합니다.");
        }
    }

    public boolean isAuthenticated(HttpSession session) {
        Boolean authenticated = (Boolean) session.getAttribute(SESSION_AUTHENTICATED);
        return authenticated != null && authenticated;
    }

    public Map<String, Object> sessionBody(HttpSession session) {
        boolean authenticated = isAuthenticated(session);
        if (!authenticated) {
            return Map.of("authenticated", false);
        }
        return Map.of(
                "authenticated", true,
                "loginId", String.valueOf(session.getAttribute(SESSION_LOGIN_ID)),
                "displayName", String.valueOf(session.getAttribute(SESSION_DISPLAY_NAME)),
                "source", String.valueOf(session.getAttribute(SESSION_SOURCE)),
                "master", isMaster(session)
        );
    }

    public void changePassword(String oldPassword, String newPassword, HttpSession session) {
        requireAdmin(session);
        String source = String.valueOf(session.getAttribute(SESSION_SOURCE));
        if (!SOURCE_LOCAL.equals(source)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "HUDIT 입장 세션에서는 비밀번호를 바꿀 수 없습니다.");
        }
        if (newPassword == null || newPassword.isBlank() || newPassword.length() < 4) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "비밀번호는 4자 이상이어야 합니다.");
        }
        Long seq = (Long) session.getAttribute(SESSION_ADMIN_SEQ);
        if (seq == null || seq <= 0) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "비밀번호를 바꿀 수 없는 세션입니다.");
        }
        List<Map<String, Object>> rows = cntHuditJdbcTemplate.queryForList(
                "SELECT password_hash FROM cnthudit_admin WHERE seq = :seq",
                new MapSqlParameterSource("seq", seq)
        );
        if (rows.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "관리자를 찾을 수 없습니다.");
        }
        String hash = hashValue(rows.getFirst().get("password_hash"));
        if (oldPassword == null || !passwordEncoder.matches(oldPassword, hash)) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "현재 비밀번호가 올바르지 않습니다.");
        }
        cntHuditJdbcTemplate.update(
                "UPDATE cnthudit_admin SET password_hash = :passwordHash WHERE seq = :seq",
                new MapSqlParameterSource()
                        .addValue("seq", seq)
                        .addValue("passwordHash", passwordEncoder.encode(newPassword))
        );
    }

    private boolean isEnabled(Object value) {
        if (value instanceof Boolean boolValue) {
            return boolValue;
        }
        if (value instanceof Number number) {
            return number.intValue() != 0;
        }
        return value != null && !"0".equals(String.valueOf(value));
    }

    private String hashValue(Object value) {
        if (value == null) {
            return "";
        }
        if (value instanceof byte[] bytes) {
            return new String(bytes, java.nio.charset.StandardCharsets.UTF_8);
        }
        return String.valueOf(value);
    }

    private void bindSession(HttpSession session, long seq, String loginId, String displayName, String source) {
        session.setAttribute(SESSION_AUTHENTICATED, true);
        session.setAttribute(SESSION_ADMIN_SEQ, seq);
        session.setAttribute(SESSION_LOGIN_ID, loginId);
        session.setAttribute(SESSION_DISPLAY_NAME, displayName);
        session.setAttribute(SESSION_SOURCE, source);
    }
}
