package com.mtalk.cnthudit;

import com.mtalk.cnthudit.dto.CnthuditChangePasswordRequest;
import com.mtalk.cnthudit.dto.CnthuditConsumeTicketRequest;
import com.mtalk.portal.dto.PortalCntCenterResponse;
import com.mtalk.portal.dto.PortalCntConfigResponse;
import com.mtalk.portal.dto.PortalCntCreateCenterRequest;
import com.mtalk.portal.dto.PortalCntCreateMembersRequest;
import com.mtalk.portal.dto.PortalCntCreateMembersResponse;
import com.mtalk.portal.dto.PortalCntMemberAnalysisResponse;
import com.mtalk.portal.dto.PortalCntMemberResponse;
import com.mtalk.portal.dto.PortalCntPageResponse;
import com.mtalk.portal.dto.PortalCntUpdateCenterRequest;
import com.mtalk.portal.dto.PortalCntUpdateMemberRequest;
import com.mtalk.portal.dto.PortalLoginRequest;
import com.mtalk.portal.service.PortalCntPdfProxyService;
import com.mtalk.portal.service.PortalCntService;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpSession;
import jakarta.validation.Valid;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

@RestController
@RequestMapping("/cnthudit/api")
public class CnthuditApiController {

    private static final String PDF_ASSET_PREFIX = "/cnthudit/api/pdf-assets";

    private final CnthuditAuthService cnthuditAuthService;
    private final PortalCntService portalCntHuditService;
    private final PortalCntPdfProxyService cnthuditPdfProxyService;

    public CnthuditApiController(
            CnthuditAuthService cnthuditAuthService,
            @Qualifier("portalCntHuditService") PortalCntService portalCntHuditService,
            @Qualifier("cnthuditPdfProxyService") PortalCntPdfProxyService cnthuditPdfProxyService
    ) {
        this.cnthuditAuthService = cnthuditAuthService;
        this.portalCntHuditService = portalCntHuditService;
        this.cnthuditPdfProxyService = cnthuditPdfProxyService;
    }

    @PostMapping("/auth/login")
    public Map<String, Object> login(@Valid @RequestBody PortalLoginRequest request, HttpSession session) {
        return cnthuditAuthService.login(request.loginId(), request.password(), session);
    }

    @PostMapping("/auth/logout")
    public Map<String, Object> logout(HttpSession session) {
        cnthuditAuthService.logout(session);
        return Map.of("authenticated", false);
    }

    @GetMapping("/auth/session")
    public Map<String, Object> session(HttpSession session) {
        return cnthuditAuthService.sessionBody(session);
    }

    @PostMapping("/auth/consume-ticket")
    public Map<String, Object> consumeTicket(
            @Valid @RequestBody CnthuditConsumeTicketRequest request,
            HttpSession session
    ) {
        return cnthuditAuthService.consumeTicket(request.ticket(), session);
    }

    @PostMapping("/auth/change-password")
    public Map<String, Object> changePassword(
            @Valid @RequestBody CnthuditChangePasswordRequest request,
            HttpSession session
    ) {
        cnthuditAuthService.changePassword(request.oldPassword(), request.newPassword(), session);
        return Map.of("ok", true);
    }

    @GetMapping("/config")
    public PortalCntConfigResponse config(HttpSession session) {
        cnthuditAuthService.requireAdmin(session);
        return portalCntHuditService.getConfig();
    }

    @GetMapping("/centers")
    public PortalCntPageResponse<PortalCntCenterResponse> centers(
            @RequestParam(defaultValue = "no") String sort,
            @RequestParam(defaultValue = "asc") String order,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "20") int size,
            HttpSession session
    ) {
        cnthuditAuthService.requireAdmin(session);
        return portalCntHuditService.listCenters(sort, order, page, size);
    }

    @GetMapping("/members")
    public PortalCntPageResponse<PortalCntMemberResponse> members(
            @RequestParam(defaultValue = "no") String sort,
            @RequestParam(defaultValue = "asc") String order,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "20") int size,
            @RequestParam(required = false) String centerId,
            HttpSession session
    ) {
        cnthuditAuthService.requireAdmin(session);
        return portalCntHuditService.listMembers(sort, order, page, size, centerId);
    }

    @GetMapping("/centers/{no}")
    public PortalCntCenterResponse center(@PathVariable long no, HttpSession session) {
        cnthuditAuthService.requireAdmin(session);
        return portalCntHuditService.getCenter(no);
    }

    @PostMapping("/centers")
    public PortalCntCenterResponse createCenter(
            @Valid @RequestBody PortalCntCreateCenterRequest request,
            HttpSession session
    ) {
        cnthuditAuthService.requireMaster(session);
        return portalCntHuditService.createCenter(request);
    }

    @PutMapping("/centers/{no}")
    public PortalCntCenterResponse updateCenter(
            @PathVariable long no,
            @Valid @RequestBody PortalCntUpdateCenterRequest request,
            HttpSession session
    ) {
        cnthuditAuthService.requireAdmin(session);
        return portalCntHuditService.updateCenter(no, request);
    }

    @PostMapping("/centers/{no}/members/bulk")
    public PortalCntCreateMembersResponse createMembers(
            @PathVariable long no,
            @Valid @RequestBody PortalCntCreateMembersRequest request,
            HttpSession session
    ) {
        cnthuditAuthService.requireMaster(session);
        return portalCntHuditService.createCenterMembers(no, request.count());
    }

    @GetMapping("/members/{userId}")
    public PortalCntMemberAnalysisResponse.PortalCntMemberDetail member(
            @PathVariable String userId,
            HttpSession session
    ) {
        cnthuditAuthService.requireAdmin(session);
        return portalCntHuditService.getMember(userId);
    }

    @PutMapping("/members/{userId}")
    public PortalCntMemberAnalysisResponse.PortalCntMemberDetail updateMember(
            @PathVariable String userId,
            @Valid @RequestBody PortalCntUpdateMemberRequest request,
            HttpSession session
    ) {
        cnthuditAuthService.requireAdmin(session);
        return portalCntHuditService.updateMember(userId, request);
    }

    @GetMapping("/members/{userId}/analysis")
    public PortalCntMemberAnalysisResponse memberAnalysis(@PathVariable String userId, HttpSession session) {
        cnthuditAuthService.requireAdmin(session);
        return portalCntHuditService.getMemberAnalysis(userId);
    }

    @GetMapping("/members/{userId}/pdf")
    public ResponseEntity<byte[]> memberPdf(
            @PathVariable String userId,
            @RequestParam(defaultValue = "false") boolean autoDownload,
            HttpSession session
    ) {
        cnthuditAuthService.requireAdmin(session);
        return cnthuditPdfProxyService.proxyMemberPdf(userId, autoDownload);
    }

    @GetMapping("/pdf-assets/**")
    public ResponseEntity<byte[]> pdfAsset(HttpServletRequest request, HttpSession session) {
        cnthuditAuthService.requireAdmin(session);
        String uri = request.getRequestURI();
        int index = uri.indexOf(PDF_ASSET_PREFIX);
        if (index < 0) {
            return ResponseEntity.notFound().build();
        }
        String assetPath = uri.substring(index + PDF_ASSET_PREFIX.length());
        return cnthuditPdfProxyService.proxyPdfAsset(assetPath);
    }
}
