package com.mtalk.portal.service;

import com.mtalk.portal.domain.PortalAdmin;
import com.mtalk.portal.repository.PortalAdminRepository;
import jakarta.servlet.http.HttpSession;
import org.springframework.http.HttpStatus;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.web.server.ResponseStatusException;

@Service
public class PortalAuthService {

    public static final String SESSION_AUTHENTICATED = "HUDIT_PORTAL_AUTHENTICATED";
    public static final String SESSION_ADMIN_SEQ = "HUDIT_PORTAL_ADMIN_SEQ";
    public static final String SESSION_LOGIN_ID = "HUDIT_PORTAL_LOGIN_ID";
    public static final String SESSION_DISPLAY_NAME = "HUDIT_PORTAL_DISPLAY_NAME";

    private final PortalAdminRepository portalAdminRepository;
    private final BCryptPasswordEncoder passwordEncoder;

    public PortalAuthService(PortalAdminRepository portalAdminRepository) {
        this.portalAdminRepository = portalAdminRepository;
        this.passwordEncoder = new BCryptPasswordEncoder();
    }

    public PortalAdmin login(String loginId, String password, HttpSession session) {
        PortalAdmin admin = portalAdminRepository.findByLoginId(loginId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.UNAUTHORIZED, "invalid credentials"));

        if (!passwordEncoder.matches(password, admin.getPasswordHash())) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "invalid credentials");
        }

        session.setAttribute(SESSION_AUTHENTICATED, true);
        session.setAttribute(SESSION_ADMIN_SEQ, admin.getSeq());
        session.setAttribute(SESSION_LOGIN_ID, admin.getLoginId());
        session.setAttribute(SESSION_DISPLAY_NAME, admin.getDisplayName());
        return admin;
    }

    public void logout(HttpSession session) {
        session.invalidate();
    }

    public void requirePortalAdmin(HttpSession session) {
        Boolean authenticated = (Boolean) session.getAttribute(SESSION_AUTHENTICATED);
        if (authenticated == null || !authenticated) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "portal login required");
        }
    }

    public boolean isAuthenticated(HttpSession session) {
        Boolean authenticated = (Boolean) session.getAttribute(SESSION_AUTHENTICATED);
        return authenticated != null && authenticated;
    }

    public String encodePassword(String rawPassword) {
        return passwordEncoder.encode(rawPassword);
    }
}
