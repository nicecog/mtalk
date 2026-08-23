package com.mtalk.portal.controller;

import com.mtalk.cnthudit.CnthuditEntryTicketService;
import com.mtalk.portal.dto.PortalCntCenterResponse;
import com.mtalk.portal.dto.PortalCntConfigResponse;
import com.mtalk.portal.dto.PortalCntCreateCenterRequest;
import com.mtalk.portal.dto.PortalCntCreateMembersRequest;
import com.mtalk.portal.dto.PortalCntCreateMembersResponse;
import com.mtalk.portal.dto.PortalCntHuditAdminCreateRequest;
import com.mtalk.portal.dto.PortalCntHuditAdminResponse;
import com.mtalk.portal.dto.PortalCntHuditAdminUpdateRequest;
import com.mtalk.portal.dto.PortalCntMemberAnalysisResponse;
import com.mtalk.portal.dto.PortalCntMemberResponse;
import com.mtalk.portal.dto.PortalCntUpdateCenterRequest;
import com.mtalk.portal.dto.PortalCntPageResponse;
import com.mtalk.portal.dto.PortalResetPasswordRequest;
import com.mtalk.portal.dto.PortalResetPasswordResponse;
import com.mtalk.portal.service.PortalAuthService;
import com.mtalk.portal.service.PortalCntHuditAdminService;
import com.mtalk.portal.service.PortalCntPdfProxyService;
import com.mtalk.portal.service.PortalCntService;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpSession;
import jakarta.validation.Valid;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/hudit/cnt-cafe24")
public class PortalCntCafe24ApiController {

    private final PortalCntService portalCntHuditService;
    private final PortalCntPdfProxyService portalCntHuditPdfProxyService;
    private final PortalCntHuditAdminService portalCntHuditAdminService;
    private final PortalAuthService portalAuthService;
    private final CnthuditEntryTicketService cnthuditEntryTicketService;
    private final String cnthuditPublicBaseUrl;

    public PortalCntCafe24ApiController(
            @Qualifier("portalCntHuditService") PortalCntService portalCntHuditService,
            @Qualifier("portalCntHuditPdfProxyService") PortalCntPdfProxyService portalCntHuditPdfProxyService,
            PortalCntHuditAdminService portalCntHuditAdminService,
            PortalAuthService portalAuthService,
            CnthuditEntryTicketService cnthuditEntryTicketService,
            @Value("${hudit.cnt.cnthudit-public-base-url:https://huditcnt.cafe24.com}") String cnthuditPublicBaseUrl
    ) {
        this.portalCntHuditService = portalCntHuditService;
        this.portalCntHuditPdfProxyService = portalCntHuditPdfProxyService;
        this.portalCntHuditAdminService = portalCntHuditAdminService;
        this.portalAuthService = portalAuthService;
        this.cnthuditEntryTicketService = cnthuditEntryTicketService;
        this.cnthuditPublicBaseUrl = trimTrailingSlash(cnthuditPublicBaseUrl);
    }

    @GetMapping("/config")
    public PortalCntConfigResponse cntConfig(HttpSession session) {
        return portalCntHuditService.getConfig(session);
    }

    @GetMapping("/centers")
    public PortalCntPageResponse<PortalCntCenterResponse> cntCenters(
            @RequestParam(defaultValue = "no") String sort,
            @RequestParam(defaultValue = "asc") String order,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "20") int size,
            HttpSession session
    ) {
        return portalCntHuditService.listCenters(sort, order, page, size, session);
    }

    @GetMapping("/members")
    public PortalCntPageResponse<PortalCntMemberResponse> cntMembers(
            @RequestParam(defaultValue = "no") String sort,
            @RequestParam(defaultValue = "asc") String order,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "20") int size,
            @RequestParam(required = false) String centerId,
            HttpSession session
    ) {
        return portalCntHuditService.listMembers(sort, order, page, size, centerId, session);
    }

    @GetMapping("/centers/{no}")
    public PortalCntCenterResponse cntCenter(
            @PathVariable long no,
            HttpSession session
    ) {
        return portalCntHuditService.getCenter(no, session);
    }

    @PostMapping("/centers")
    public PortalCntCenterResponse createCntCenter(
            @Valid @RequestBody PortalCntCreateCenterRequest request,
            HttpSession session
    ) {
        return portalCntHuditService.createCenter(request, session);
    }

    @PutMapping("/centers/{no}")
    public PortalCntCenterResponse updateCntCenter(
            @PathVariable long no,
            @Valid @RequestBody PortalCntUpdateCenterRequest request,
            HttpSession session
    ) {
        return portalCntHuditService.updateCenter(no, request, session);
    }

    @PostMapping("/centers/{no}/members/bulk")
    public PortalCntCreateMembersResponse createCntCenterMembers(
            @PathVariable long no,
            @Valid @RequestBody PortalCntCreateMembersRequest request,
            HttpSession session
    ) {
        return portalCntHuditService.createCenterMembers(no, request.count(), session);
    }

    @GetMapping("/members/{userId}/analysis")
    public PortalCntMemberAnalysisResponse cntMemberAnalysis(
            @PathVariable String userId,
            HttpSession session
    ) {
        return portalCntHuditService.getMemberAnalysis(userId, session);
    }

    @GetMapping("/members/{userId}/pdf")
    public ResponseEntity<byte[]> cntMemberPdf(
            @PathVariable String userId,
            @RequestParam(defaultValue = "false") boolean autoDownload,
            HttpSession session
    ) {
        return portalCntHuditPdfProxyService.proxyMemberPdf(userId, autoDownload, session);
    }

    @GetMapping("/pdf-assets/**")
    public ResponseEntity<byte[]> cntPdfAsset(HttpServletRequest request, HttpSession session) {
        String prefix = "/api/hudit/cnt-cafe24/pdf-assets";
        String uri = request.getRequestURI();
        int index = uri.indexOf(prefix);
        if (index < 0) {
            return ResponseEntity.notFound().build();
        }
        String assetPath = uri.substring(index + prefix.length());
        return portalCntHuditPdfProxyService.proxyPdfAsset(assetPath, session);
    }

    @GetMapping("/admins")
    public List<PortalCntHuditAdminResponse> listAdmins(HttpSession session) {
        return portalCntHuditAdminService.listAdmins(session);
    }

    @PostMapping("/admins")
    public PortalCntHuditAdminResponse createAdmin(
            @Valid @RequestBody PortalCntHuditAdminCreateRequest request,
            HttpSession session
    ) {
        return portalCntHuditAdminService.createAdmin(request, session);
    }

    @PutMapping("/admins/{seq}")
    public PortalCntHuditAdminResponse updateAdmin(
            @PathVariable long seq,
            @RequestBody PortalCntHuditAdminUpdateRequest request,
            HttpSession session
    ) {
        return portalCntHuditAdminService.updateAdmin(seq, request, session);
    }

    @PostMapping("/admins/{seq}/reset-password")
    public PortalResetPasswordResponse resetAdminPassword(
            @PathVariable long seq,
            @RequestBody(required = false) PortalResetPasswordRequest request,
            HttpSession session
    ) {
        return portalCntHuditAdminService.resetPassword(seq, request, session);
    }

    @PostMapping("/entry-ticket")
    public Map<String, Object> entryTicket(HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        String loginId = String.valueOf(session.getAttribute(PortalAuthService.SESSION_LOGIN_ID));
        Object displayName = session.getAttribute(PortalAuthService.SESSION_DISPLAY_NAME);
        String ticket = cnthuditEntryTicketService.issue(
                loginId,
                displayName == null ? loginId : String.valueOf(displayName)
        );
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("ticket", ticket);
        body.put("expiresInSeconds", CnthuditEntryTicketService.TTL_SECONDS);
        body.put("redirectUrl", cnthuditPublicBaseUrl + "/cnthudit/app?ticket=" + ticket);
        return body;
    }

    private static String trimTrailingSlash(String url) {
        if (url == null || url.isBlank()) {
            return "https://huditcnt.cafe24.com";
        }
        return url.endsWith("/") ? url.substring(0, url.length() - 1) : url.trim();
    }
}
