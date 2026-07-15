package com.mtalk.portal.controller;

import com.mtalk.portal.domain.PortalAdmin;
import com.mtalk.mbody.dto.UpdatePlayerProgressRequest;
import com.mtalk.mface.dto.UpdateFaceProgressRequest;
import com.mtalk.msocial.dto.UpdateSocialProgressRequest;
import com.mtalk.portal.dto.PortalCntCenterResponse;
import com.mtalk.portal.dto.PortalCntConfigResponse;
import com.mtalk.portal.dto.PortalCntCreateCenterRequest;
import com.mtalk.portal.dto.PortalCntCreateMembersRequest;
import com.mtalk.portal.dto.PortalCntCreateMembersResponse;
import com.mtalk.portal.dto.PortalCntMemberAnalysisResponse;
import com.mtalk.portal.dto.PortalCntMemberResponse;
import com.mtalk.portal.dto.PortalCntUpdateCenterRequest;
import com.mtalk.portal.dto.PortalCntPageResponse;
import com.mtalk.portal.dto.PortalCreateUserRequest;
import com.mtalk.portal.dto.PortalLoginRequest;
import com.mtalk.portal.dto.PortalResetPasswordRequest;
import com.mtalk.portal.dto.PortalResetPasswordResponse;
import com.mtalk.portal.dto.PortalServiceInfo;
import com.mtalk.portal.dto.PortalUpdateUserRequest;
import com.mtalk.portal.dto.PortalUserResponse;
import com.mtalk.portal.service.PortalAuthService;
import com.mtalk.portal.service.PortalCntPdfProxyService;
import com.mtalk.portal.service.PortalCntService;
import com.mtalk.portal.service.PortalMtalkUserService;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpSession;
import jakarta.validation.Valid;
import org.springframework.core.io.support.ResourceRegion;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
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
@RequestMapping("/api/hudit")
public class PortalApiController {

    private final PortalAuthService portalAuthService;
    private final PortalMtalkUserService portalMtalkUserService;
    private final PortalCntService portalCntService;
    private final PortalCntPdfProxyService portalCntPdfProxyService;

    public PortalApiController(
            PortalAuthService portalAuthService,
            PortalMtalkUserService portalMtalkUserService,
            PortalCntService portalCntService,
            PortalCntPdfProxyService portalCntPdfProxyService
    ) {
        this.portalAuthService = portalAuthService;
        this.portalMtalkUserService = portalMtalkUserService;
        this.portalCntService = portalCntService;
        this.portalCntPdfProxyService = portalCntPdfProxyService;
    }

    @PostMapping("/auth/login")
    public Map<String, Object> login(@Valid @RequestBody PortalLoginRequest request, HttpSession session) {
        PortalAdmin admin = portalAuthService.login(request.loginId(), request.password(), session);
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("authenticated", true);
        body.put("loginId", admin.getLoginId());
        body.put("displayName", admin.getDisplayName());
        return body;
    }

    @PostMapping("/auth/logout")
    public Map<String, Object> logout(HttpSession session) {
        portalAuthService.logout(session);
        return Map.of("authenticated", false);
    }

    @GetMapping("/auth/session")
    public Map<String, Object> session(HttpSession session) {
        Map<String, Object> body = new LinkedHashMap<>();
        boolean authenticated = portalAuthService.isAuthenticated(session);
        body.put("authenticated", authenticated);
        if (authenticated) {
            body.put("loginId", session.getAttribute(PortalAuthService.SESSION_LOGIN_ID));
            body.put("displayName", session.getAttribute(PortalAuthService.SESSION_DISPLAY_NAME));
        }
        return body;
    }

    @GetMapping("/mtalk/services")
    public List<PortalServiceInfo> services(HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        return portalMtalkUserService.listServices();
    }

    @GetMapping("/mtalk/{service}/users")
    public List<PortalUserResponse> users(@PathVariable String service, HttpSession session) {
        return portalMtalkUserService.listUsers(service, session);
    }

    @PostMapping("/mtalk/{service}/users")
    public PortalUserResponse createUser(
            @PathVariable String service,
            @Valid @RequestBody PortalCreateUserRequest request,
            HttpSession session
    ) {
        return portalMtalkUserService.createUser(service, request, session);
    }

    @PutMapping("/mtalk/{service}/users/{seq}")
    public PortalUserResponse updateUser(
            @PathVariable String service,
            @PathVariable long seq,
            @RequestBody PortalUpdateUserRequest request,
            HttpSession session
    ) {
        return portalMtalkUserService.updateUser(service, seq, request, session);
    }

    @PostMapping("/mtalk/{service}/users/{seq}/reset-password")
    public PortalResetPasswordResponse resetPassword(
            @PathVariable String service,
            @PathVariable long seq,
            @RequestBody(required = false) PortalResetPasswordRequest request,
            HttpSession session
    ) {
        return portalMtalkUserService.resetPassword(service, seq, request, session);
    }

    @GetMapping("/mtalk/mbody/progress")
    public List<Map<String, Object>> mbodyProgress(HttpSession session) {
        return portalMtalkUserService.listMbodyProgress(session);
    }

    @PutMapping("/mtalk/mbody/users/{seq}/progress")
    public Map<String, Object> updateMbodyProgress(
            @PathVariable long seq,
            @Valid @RequestBody UpdatePlayerProgressRequest request,
            HttpSession session
    ) {
        return portalMtalkUserService.updateMbodyProgress(seq, request, session);
    }

    @GetMapping("/mtalk/mbody/users/{seq}/training-results")
    public List<Map<String, Object>> mbodyTrainingResults(@PathVariable long seq, HttpSession session) {
        return portalMtalkUserService.listMbodyTrainingResults(seq, session);
    }

    @GetMapping("/mtalk/mbody/videos/summary")
    public Map<String, Object> mbodyVideoSummary(HttpSession session) {
        return portalMtalkUserService.mbodyVideoSummary(session);
    }

    @GetMapping("/mtalk/mbody/users/{seq}/videos")
    public List<Map<String, Object>> mbodyUserVideos(@PathVariable long seq, HttpSession session) {
        return portalMtalkUserService.listMbodyVideos(seq, session);
    }

    @GetMapping("/mtalk/mbody/videos/{seq}/stream")
    public ResponseEntity<ResourceRegion> mbodyVideoStream(
            @PathVariable long seq,
            HttpSession session,
            HttpServletRequest request
    ) {
        return portalMtalkUserService.streamMbodyVideo(seq, session, request);
    }

    @GetMapping("/mtalk/mbody/videos/{seq}/download")
    public ResponseEntity<org.springframework.core.io.Resource> mbodyVideoDownload(
            @PathVariable long seq,
            HttpSession session
    ) {
        return portalMtalkUserService.downloadMbodyVideo(seq, session);
    }

    @DeleteMapping("/mtalk/mbody/videos/{seq}")
    public Map<String, Object> deleteMbodyVideo(
            @PathVariable long seq,
            @RequestParam(defaultValue = "true") boolean deleteFile,
            HttpSession session
    ) {
        return portalMtalkUserService.deleteMbodyVideo(seq, deleteFile, session);
    }

    @GetMapping("/mtalk/mface/progress")
    public List<Map<String, Object>> mfaceProgress(HttpSession session) {
        return portalMtalkUserService.listMfaceProgress(session);
    }

    @PutMapping("/mtalk/mface/users/{seq}/progress")
    public Map<String, Object> updateMfaceProgress(
            @PathVariable long seq,
            @Valid @RequestBody UpdateFaceProgressRequest request,
            HttpSession session
    ) {
        return portalMtalkUserService.updateMfaceProgress(seq, request, session);
    }

    @GetMapping("/mtalk/mface/users/{seq}/activity-logs")
    public List<Map<String, Object>> mfaceActivityLogs(@PathVariable long seq, HttpSession session) {
        return portalMtalkUserService.listMfaceActivityLogs(seq, session);
    }

    @GetMapping("/mtalk/msocial/progress")
    public List<Map<String, Object>> msocialProgress(HttpSession session) {
        return portalMtalkUserService.listMsocialProgress(session);
    }

    @PutMapping("/mtalk/msocial/users/{seq}/progress")
    public Map<String, Object> updateMsocialProgress(
            @PathVariable long seq,
            @Valid @RequestBody UpdateSocialProgressRequest request,
            HttpSession session
    ) {
        return portalMtalkUserService.updateMsocialProgress(seq, request, session);
    }

    @GetMapping("/mtalk/msocial/users/{seq}/activity-logs")
    public List<Map<String, Object>> msocialActivityLogs(@PathVariable long seq, HttpSession session) {
        return portalMtalkUserService.listMsocialActivityLogs(seq, session);
    }

    @GetMapping("/cnt/config")
    public PortalCntConfigResponse cntConfig(HttpSession session) {
        return portalCntService.getConfig(session);
    }

    @GetMapping("/cnt/centers")
    public PortalCntPageResponse<PortalCntCenterResponse> cntCenters(
            @RequestParam(defaultValue = "no") String sort,
            @RequestParam(defaultValue = "asc") String order,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "20") int size,
            HttpSession session
    ) {
        return portalCntService.listCenters(sort, order, page, size, session);
    }

    @GetMapping("/cnt/members")
    public PortalCntPageResponse<PortalCntMemberResponse> cntMembers(
            @RequestParam(defaultValue = "no") String sort,
            @RequestParam(defaultValue = "asc") String order,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "20") int size,
            @RequestParam(required = false) String centerId,
            HttpSession session
    ) {
        return portalCntService.listMembers(sort, order, page, size, centerId, session);
    }

    @GetMapping("/cnt/centers/{no}")
    public PortalCntCenterResponse cntCenter(
            @PathVariable long no,
            HttpSession session
    ) {
        return portalCntService.getCenter(no, session);
    }

    @PostMapping("/cnt/centers")
    public PortalCntCenterResponse createCntCenter(
            @Valid @RequestBody PortalCntCreateCenterRequest request,
            HttpSession session
    ) {
        return portalCntService.createCenter(request, session);
    }

    @PutMapping("/cnt/centers/{no}")
    public PortalCntCenterResponse updateCntCenter(
            @PathVariable long no,
            @RequestBody PortalCntUpdateCenterRequest request,
            HttpSession session
    ) {
        return portalCntService.updateCenter(no, request, session);
    }

    @PostMapping("/cnt/centers/{no}/members/bulk")
    public PortalCntCreateMembersResponse createCntCenterMembers(
            @PathVariable long no,
            @Valid @RequestBody PortalCntCreateMembersRequest request,
            HttpSession session
    ) {
        return portalCntService.createCenterMembers(no, request.count(), session);
    }

    @GetMapping("/cnt/members/{userId}/analysis")
    public PortalCntMemberAnalysisResponse cntMemberAnalysis(
            @PathVariable String userId,
            HttpSession session
    ) {
        return portalCntService.getMemberAnalysis(userId, session);
    }

    @GetMapping("/cnt/members/{userId}/pdf")
    public ResponseEntity<byte[]> cntMemberPdf(
            @PathVariable String userId,
            @RequestParam(defaultValue = "false") boolean autoDownload,
            HttpSession session
    ) {
        return portalCntPdfProxyService.proxyMemberPdf(userId, autoDownload, session);
    }

    @GetMapping("/cnt/pdf-assets/**")
    public ResponseEntity<byte[]> cntPdfAsset(HttpServletRequest request, HttpSession session) {
        String prefix = "/api/hudit/cnt/pdf-assets";
        String uri = request.getRequestURI();
        int index = uri.indexOf(prefix);
        if (index < 0) {
            return ResponseEntity.notFound().build();
        }
        String assetPath = uri.substring(index + prefix.length());
        return portalCntPdfProxyService.proxyPdfAsset(assetPath, session);
    }
}
