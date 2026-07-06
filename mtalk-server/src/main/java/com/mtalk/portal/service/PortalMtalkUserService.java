package com.mtalk.portal.service;

import com.mtalk.mbench.domain.UserAccount;
import com.mtalk.mbench.repository.MbenchUserAccountRepository;
import com.mtalk.mbody.dto.UpdatePlayerProgressRequest;
import com.mtalk.mbody.repository.MbodyUserAccountRepository;
import com.mtalk.mbody.service.MbodySubjectVideoAdminService;
import com.mtalk.mbody.service.PlayerProgressService;
import com.mtalk.mface.dto.UpdateFaceProgressRequest;
import com.mtalk.mface.service.FaceProgressService;
import com.mtalk.msocial.dto.UpdateSocialProgressRequest;
import com.mtalk.msocial.service.SocialProgressService;
import com.mtalk.portal.dto.PortalCreateUserRequest;
import com.mtalk.portal.dto.PortalResetPasswordRequest;
import com.mtalk.portal.dto.PortalResetPasswordResponse;
import com.mtalk.portal.dto.PortalServiceInfo;
import com.mtalk.portal.dto.PortalUpdateUserRequest;
import com.mtalk.portal.dto.PortalUserResponse;
import jakarta.servlet.http.HttpSession;
import org.springframework.http.HttpStatus;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.web.server.ResponseStatusException;

import java.security.SecureRandom;
import java.time.format.DateTimeFormatter;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

@Service
public class PortalMtalkUserService {

    private static final DateTimeFormatter FORMAT = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm");
    private static final String RESET_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#";
    private static final SecureRandom RANDOM = new SecureRandom();

    private final PortalAuthService portalAuthService;
    private final MbenchUserAccountRepository mbenchUserAccountRepository;
    private final MbodyUserAccountRepository mbodyUserAccountRepository;
    private final PlayerProgressService playerProgressService;
    private final FaceProgressService faceProgressService;
    private final SocialProgressService socialProgressService;
    private final MbodySubjectVideoAdminService subjectVideoAdminService;
    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();

    public PortalMtalkUserService(
            PortalAuthService portalAuthService,
            MbenchUserAccountRepository mbenchUserAccountRepository,
            MbodyUserAccountRepository mbodyUserAccountRepository,
            PlayerProgressService playerProgressService,
            FaceProgressService faceProgressService,
            SocialProgressService socialProgressService,
            MbodySubjectVideoAdminService subjectVideoAdminService
    ) {
        this.portalAuthService = portalAuthService;
        this.mbenchUserAccountRepository = mbenchUserAccountRepository;
        this.mbodyUserAccountRepository = mbodyUserAccountRepository;
        this.playerProgressService = playerProgressService;
        this.faceProgressService = faceProgressService;
        this.socialProgressService = socialProgressService;
        this.subjectVideoAdminService = subjectVideoAdminService;
    }

    public List<PortalServiceInfo> listServices() {
        return List.of(
                new PortalServiceInfo("mbench", "M-BENCH", "벤치마크·스파이크 분석", "chart-line", "#2271b1"),
                new PortalServiceInfo("mbody", "M-BODY", "신체 측정·자세 분석", "person", "#00a32a"),
                new PortalServiceInfo("mface", "M-FACE", "얼굴 분석·이미지", "face-smile", "#9b51e0"),
                new PortalServiceInfo("msocial", "M-SOCIAL", "소셜·커뮤니티", "people", "#d63638")
        );
    }

    public List<PortalUserResponse> listUsers(String service, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        String normalized = normalizeService(service);

        if ("mbench".equals(normalized)) {
            return mbenchUserAccountRepository.findAll().stream()
                    .filter(account -> !account.isAdmin())
                    .map(this::fromMbench)
                    .toList();
        }

        if ("mface".equals(normalized)) {
            return mbodyUserAccountRepository.findAll().stream()
                    .filter(account -> !account.isAdminAccount())
                    .map(this::fromMface)
                    .toList();
        }

        if ("msocial".equals(normalized)) {
            return mbodyUserAccountRepository.findAll().stream()
                    .filter(account -> !account.isAdminAccount())
                    .map(this::fromMsocial)
                    .toList();
        }

        return mbodyUserAccountRepository.findAll().stream()
                .filter(account -> !account.isAdminAccount())
                .map(this::fromMbody)
                .toList();
    }

    public PortalUserResponse createUser(String service, PortalCreateUserRequest request, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        String normalized = normalizeService(service);

        if ("mbench".equals(normalized)) {
            if (mbenchUserAccountRepository.existsByLoginId(request.id())) {
                throw new ResponseStatusException(HttpStatus.CONFLICT, "account already exists");
            }
            UserAccount account = new UserAccount();
            account.setLoginId(request.id());
            account.setPassword(request.password());
            account.setLevelId(request.levelId() != null ? request.levelId() : 1);
            account.setDomain(request.domain() != null ? request.domain() : "spike");
            account.setName(request.name() != null ? request.name() : request.id());
            account.setEmail(request.email() != null ? request.email() : "");
            account.setPhone(request.phone() != null ? request.phone() : "");
            account.setAdmin(false);
            return fromMbench(mbenchUserAccountRepository.save(account));
        }

        if (mbodyUserAccountRepository.existsByLoginId(request.id())) {
            throw new ResponseStatusException(HttpStatus.CONFLICT, "account already exists");
        }
        com.mtalk.mbody.domain.UserAccount account = new com.mtalk.mbody.domain.UserAccount();
        account.setLoginId(request.id());
        account.setPasswordHash(passwordEncoder.encode(request.password()));
        account.setLevelId(request.levelId() != null ? request.levelId() : 10);
        account.setDomain(request.domain() != null ? request.domain() : "mbody");
        account.setName(request.name() != null ? request.name() : request.id());
        account.setEmail(request.email() != null ? request.email() : "");
        account.setPhone(request.phone() != null ? request.phone() : "");
        account.setAdminAccount(false);
        com.mtalk.mbody.domain.UserAccount saved = mbodyUserAccountRepository.save(account);
        if ("mface".equals(normalized)) {
            faceProgressService.createDefaultProgress(saved.getSeq());
            applyMfaceProgressOnCreate(saved.getSeq(), request);
            return fromMface(saved);
        }
        if ("msocial".equals(normalized)) {
            socialProgressService.createDefaultProgress(saved.getSeq());
            applyMsocialProgressOnCreate(saved.getSeq(), request);
            return fromMsocial(saved);
        }
        playerProgressService.createDefaultProgress(saved.getSeq());
        applyMbodyProgressOnCreate(saved.getSeq(), request);
        return fromMbody(saved);
    }

    public PortalUserResponse updateUser(
            String service,
            long seq,
            PortalUpdateUserRequest request,
            HttpSession session
    ) {
        portalAuthService.requirePortalAdmin(session);
        String normalized = normalizeService(service);

        if ("mbench".equals(normalized)) {
            UserAccount account = mbenchUserAccountRepository.findById(seq)
                    .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "user not found"));
            if (account.isAdmin()) {
                throw new ResponseStatusException(HttpStatus.FORBIDDEN, "cannot modify admin account");
            }
            if (request.id() != null && !request.id().isBlank() && !request.id().equals(account.getLoginId())) {
                if (mbenchUserAccountRepository.existsByLoginId(request.id())) {
                    throw new ResponseStatusException(HttpStatus.CONFLICT, "account already exists");
                }
                account.setLoginId(request.id());
            }
            if (request.password() != null && !request.password().isBlank()) {
                account.setPassword(request.password());
            }
            applyMbenchProfile(account, request);
            return fromMbench(mbenchUserAccountRepository.save(account));
        }

        com.mtalk.mbody.domain.UserAccount account = mbodyUserAccountRepository.findById(seq)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "user not found"));
        if (account.isAdminAccount()) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "cannot modify admin account");
        }
        if (request.id() != null && !request.id().isBlank() && !request.id().equals(account.getLoginId())) {
            if (mbodyUserAccountRepository.existsByLoginId(request.id())) {
                throw new ResponseStatusException(HttpStatus.CONFLICT, "account already exists");
            }
            account.setLoginId(request.id());
        }
        if (request.password() != null && !request.password().isBlank()) {
            account.setPasswordHash(passwordEncoder.encode(request.password()));
        }
        applyMbodyProfile(account, request);
        com.mtalk.mbody.domain.UserAccount saved = mbodyUserAccountRepository.save(account);
        if ("mface".equals(normalized)) {
            applyMfaceProgressOnUpdate(seq, request);
            return fromMface(saved);
        }
        if ("msocial".equals(normalized)) {
            applyMsocialProgressOnUpdate(seq, request);
            return fromMsocial(saved);
        }
        applyMbodyProgressOnUpdate(seq, request);
        return fromMbody(saved);
    }

    public PortalResetPasswordResponse resetPassword(
            String service,
            long seq,
            PortalResetPasswordRequest request,
            HttpSession session
    ) {
        portalAuthService.requirePortalAdmin(session);
        String normalized = normalizeService(service);
        String newPassword = resolveNewPassword(request != null ? request.password() : null);

        if ("mbench".equals(normalized)) {
            UserAccount account = mbenchUserAccountRepository.findById(seq)
                    .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "user not found"));
            if (account.isAdmin()) {
                throw new ResponseStatusException(HttpStatus.FORBIDDEN, "cannot reset admin account");
            }
            account.setPassword(newPassword);
            mbenchUserAccountRepository.save(account);
            return new PortalResetPasswordResponse(account.getSeq(), account.getLoginId(), newPassword);
        }

        com.mtalk.mbody.domain.UserAccount account = mbodyUserAccountRepository.findById(seq)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "user not found"));
        if (account.isAdminAccount()) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "cannot reset admin account");
        }
        account.setPasswordHash(passwordEncoder.encode(newPassword));
        mbodyUserAccountRepository.save(account);
        return new PortalResetPasswordResponse(account.getSeq(), account.getLoginId(), newPassword);
    }

    public List<Map<String, Object>> listMbodyProgress(HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        return playerProgressService.listAllProgressForPortal();
    }

    public Map<String, Object> updateMbodyProgress(
            long seq,
            UpdatePlayerProgressRequest request,
            HttpSession session
    ) {
        portalAuthService.requirePortalAdmin(session);
        return playerProgressService.updateProgressForAccountAsPortal(seq, request);
    }

    public List<Map<String, Object>> listMbodyTrainingResults(long seq, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        return playerProgressService.listTrainingResultsForAccountAsPortal(seq);
    }

    public Map<String, Object> mbodyVideoSummary(HttpSession session) {
        return subjectVideoAdminService.summary(session);
    }

    public List<Map<String, Object>> listMbodyVideos(long userSeq, HttpSession session) {
        return subjectVideoAdminService.listByUser(userSeq, session);
    }

    public org.springframework.http.ResponseEntity<org.springframework.core.io.support.ResourceRegion> streamMbodyVideo(
            long seq,
            HttpSession session,
            jakarta.servlet.http.HttpServletRequest request
    ) {
        return subjectVideoAdminService.streamVideo(seq, session, request);
    }

    public org.springframework.http.ResponseEntity<org.springframework.core.io.Resource> downloadMbodyVideo(
            long seq,
            HttpSession session
    ) {
        return subjectVideoAdminService.downloadVideo(seq, session);
    }

    public Map<String, Object> deleteMbodyVideo(long seq, boolean deleteFile, HttpSession session) {
        return subjectVideoAdminService.deleteVideo(seq, deleteFile, session);
    }

    public List<Map<String, Object>> listMfaceProgress(HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        return faceProgressService.listAllProgressForPortal();
    }

    public Map<String, Object> updateMfaceProgress(
            long seq,
            UpdateFaceProgressRequest request,
            HttpSession session
    ) {
        portalAuthService.requirePortalAdmin(session);
        return faceProgressService.updateProgressForAccountAsPortal(seq, request);
    }

    public List<Map<String, Object>> listMfaceActivityLogs(long seq, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        return faceProgressService.listActivityLogsForAccountAsPortal(seq);
    }

    public List<Map<String, Object>> listMsocialProgress(HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        return socialProgressService.listAllProgressForPortal();
    }

    public Map<String, Object> updateMsocialProgress(
            long seq,
            UpdateSocialProgressRequest request,
            HttpSession session
    ) {
        portalAuthService.requirePortalAdmin(session);
        return socialProgressService.updateProgressForAccountAsPortal(seq, request);
    }

    public List<Map<String, Object>> listMsocialActivityLogs(long seq, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        return socialProgressService.listActivityLogsForAccountAsPortal(seq);
    }

    private void applyMfaceProgressOnCreate(long seq, PortalCreateUserRequest request) {
        if (request.dayCount() == null && request.itemString() == null) {
            return;
        }
        faceProgressService.updateProgressForAccountAsPortal(
                seq,
                new UpdateFaceProgressRequest(
                        request.dayCount() != null ? request.dayCount() : 1,
                        request.itemString() != null
                                ? request.itemString()
                                : com.mtalk.mbody.domain.FaceProgress.DEFAULT_ITEM_STRING
                )
        );
    }

    private void applyMfaceProgressOnUpdate(long seq, PortalUpdateUserRequest request) {
        if (request.dayCount() == null || request.itemString() == null) {
            return;
        }
        faceProgressService.updateProgressForAccountAsPortal(
                seq,
                new UpdateFaceProgressRequest(request.dayCount(), request.itemString())
        );
    }

    private void applyMsocialProgressOnCreate(long seq, PortalCreateUserRequest request) {
        if (request.dayCount() == null && request.itemString() == null) {
            return;
        }
        socialProgressService.updateProgressForAccountAsPortal(
                seq,
                new UpdateSocialProgressRequest(
                        request.dayCount() != null ? request.dayCount() : 1,
                        request.itemString() != null
                                ? request.itemString()
                                : com.mtalk.mbody.domain.SocialProgress.DEFAULT_PROGRESS_STRING
                )
        );
    }

    private void applyMsocialProgressOnUpdate(long seq, PortalUpdateUserRequest request) {
        if (request.dayCount() == null || request.itemString() == null) {
            return;
        }
        socialProgressService.updateProgressForAccountAsPortal(
                seq,
                new UpdateSocialProgressRequest(request.dayCount(), request.itemString())
        );
    }

    private void applyMbodyProgressOnCreate(long seq, PortalCreateUserRequest request) {
        if (request.playTimes() == null && request.bodyPlayTimes() == null && request.danceStar() == null) {
            return;
        }
        playerProgressService.updateProgressForAccountAsPortal(
                seq,
                new UpdatePlayerProgressRequest(
                        request.playTimes() != null ? request.playTimes() : 1,
                        request.bodyPlayTimes() != null ? request.bodyPlayTimes() : 0,
                        request.danceStar() != null ? request.danceStar() : com.mtalk.mbody.domain.PlayerProgress.DEFAULT_DANCE_STAR
                )
        );
    }

    private void applyMbodyProgressOnUpdate(long seq, PortalUpdateUserRequest request) {
        if (request.playTimes() == null || request.bodyPlayTimes() == null || request.danceStar() == null) {
            return;
        }
        playerProgressService.updateProgressForAccountAsPortal(
                seq,
                new UpdatePlayerProgressRequest(request.playTimes(), request.bodyPlayTimes(), request.danceStar())
        );
    }

    private String resolveNewPassword(String customPassword) {
        if (customPassword != null && !customPassword.isBlank()) {
            if (customPassword.length() < 4) {
                throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "password must be at least 4 characters");
            }
            return customPassword;
        }
        return generateTempPassword();
    }

    private void applyMbenchProfile(UserAccount account, PortalUpdateUserRequest request) {
        if (request.name() != null) {
            account.setName(request.name());
        }
        if (request.email() != null) {
            account.setEmail(request.email());
        }
        if (request.phone() != null) {
            account.setPhone(request.phone());
        }
        if (request.levelId() != null) {
            account.setLevelId(request.levelId());
        }
        if (request.domain() != null) {
            account.setDomain(request.domain());
        }
    }

    private void applyMbodyProfile(com.mtalk.mbody.domain.UserAccount account, PortalUpdateUserRequest request) {
        if (request.name() != null) {
            account.setName(request.name());
        }
        if (request.email() != null) {
            account.setEmail(request.email());
        }
        if (request.phone() != null) {
            account.setPhone(request.phone());
        }
        if (request.levelId() != null) {
            account.setLevelId(request.levelId());
        }
        if (request.domain() != null) {
            account.setDomain(request.domain());
        }
    }

    private PortalUserResponse fromMbench(UserAccount account) {
        String created = account.getCreatedAt() != null ? account.getCreatedAt().format(FORMAT) : "";
        return new PortalUserResponse(
                account.getSeq(),
                account.getLoginId(),
                account.getName(),
                account.getEmail(),
                account.getPhone(),
                account.getLevelId(),
                account.getDomain(),
                account.isAdmin(),
                created,
                null,
                null,
                null,
                null,
                null,
                null
        );
    }

    private PortalUserResponse fromMbody(com.mtalk.mbody.domain.UserAccount account) {
        Map<String, Object> progress = playerProgressService.getProgressForAccountAsAdmin(account.getSeq());
        return new PortalUserResponse(
                account.getSeq(),
                account.getLoginId(),
                account.getName(),
                account.getEmail(),
                account.getPhone(),
                account.getLevelId(),
                account.getDomain(),
                account.isAdminAccount(),
                "",
                toInteger(progress.get("playTimes")),
                toInteger(progress.get("bodyPlayTimes")),
                (String) progress.get("danceStar"),
                (String) progress.get("lastVisitDate"),
                null,
                null
        );
    }

    private PortalUserResponse fromMface(com.mtalk.mbody.domain.UserAccount account) {
        Map<String, Object> progress = faceProgressService.getProgressForAccountAsAdmin(account.getSeq());
        return new PortalUserResponse(
                account.getSeq(),
                account.getLoginId(),
                account.getName(),
                account.getEmail(),
                account.getPhone(),
                account.getLevelId(),
                account.getDomain(),
                account.isAdminAccount(),
                "",
                null,
                null,
                null,
                (String) progress.get("lastVisitDate"),
                toInteger(progress.get("dayCount")),
                (String) progress.get("itemString")
        );
    }

    private PortalUserResponse fromMsocial(com.mtalk.mbody.domain.UserAccount account) {
        Map<String, Object> progress = socialProgressService.getProgressForAccountAsAdmin(account.getSeq());
        return new PortalUserResponse(
                account.getSeq(),
                account.getLoginId(),
                account.getName(),
                account.getEmail(),
                account.getPhone(),
                account.getLevelId(),
                account.getDomain(),
                account.isAdminAccount(),
                "",
                null,
                null,
                null,
                (String) progress.get("lastVisitDate"),
                toInteger(progress.get("dayCount")),
                (String) progress.get("progressString")
        );
    }

    private Integer toInteger(Object value) {
        if (value instanceof Integer intValue) {
            return intValue;
        }
        if (value instanceof Number number) {
            return number.intValue();
        }
        return null;
    }

    private String normalizeService(String service) {
        if (service == null) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "service required");
        }
        String normalized = service.toLowerCase(Locale.ROOT);
        if (!List.of("mbench", "mbody", "mface", "msocial").contains(normalized)) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "unknown service: " + service);
        }
        return normalized;
    }

    private String generateTempPassword() {
        StringBuilder builder = new StringBuilder(12);
        for (int i = 0; i < 12; i++) {
            builder.append(RESET_CHARS.charAt(RANDOM.nextInt(RESET_CHARS.length())));
        }
        return builder.toString();
    }
}
