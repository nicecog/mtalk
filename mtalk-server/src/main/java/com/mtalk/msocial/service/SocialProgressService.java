package com.mtalk.msocial.service;

import com.mtalk.mbody.domain.SocialActivityLog;
import com.mtalk.mbody.domain.SocialProgress;
import com.mtalk.mbody.domain.UserAccount;
import com.mtalk.mbody.repository.MbodyUserAccountRepository;
import com.mtalk.mbody.repository.MsocialSocialActivityLogRepository;
import com.mtalk.mbody.repository.MsocialSocialProgressRepository;
import com.mtalk.mbody.service.AuthService;
import com.mtalk.msocial.dto.CreateSocialActivityLogRequest;
import com.mtalk.msocial.dto.UpdateSocialProgressRequest;
import jakarta.servlet.http.HttpSession;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

import java.time.LocalDate;
import java.time.ZoneId;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import static org.springframework.http.HttpStatus.NOT_FOUND;

@Service
public class SocialProgressService {

    private static final ZoneId KOREA = ZoneId.of("Asia/Seoul");

    private final MsocialSocialProgressRepository socialProgressRepository;
    private final MsocialSocialActivityLogRepository socialActivityLogRepository;
    private final MbodyUserAccountRepository userAccountRepository;
    private final AuthService authService;

    public SocialProgressService(
            MsocialSocialProgressRepository socialProgressRepository,
            MsocialSocialActivityLogRepository socialActivityLogRepository,
            MbodyUserAccountRepository userAccountRepository,
            AuthService authService
    ) {
        this.socialProgressRepository = socialProgressRepository;
        this.socialActivityLogRepository = socialActivityLogRepository;
        this.userAccountRepository = userAccountRepository;
        this.authService = authService;
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> syncMyProgress(HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        SocialProgress progress = getOrCreateProgress(userSeq);
        bumpDayCountForNewDay(progress);
        return toProgressResponse(progress);
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> updateMyProgress(UpdateSocialProgressRequest request, HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        SocialProgress progress = getOrCreateProgress(userSeq);
        progress.setDayCount(request.dayCount());
        progress.setProgressString(SocialProgress.normalizeProgressString(request.progressString()));
        progress.setLastVisitDate(LocalDate.now(KOREA));
        return toProgressResponse(socialProgressRepository.save(progress));
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> addMyActivityLog(CreateSocialActivityLogRequest request, HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        SocialActivityLog log = new SocialActivityLog();
        log.setUserAccountSeq(userSeq);
        log.setLogDate(request.logDate());
        log.setLogTime(request.logTime());
        log.setLogBody(request.logBody() != null ? request.logBody() : "");
        return toActivityLogResponse(socialActivityLogRepository.save(log));
    }

    public List<Map<String, Object>> listMyActivityLogs(HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        return listActivityLogsInternal(userSeq);
    }

    public List<Map<String, Object>> listAllProgressForPortal() {
        return userAccountRepository.findAll().stream()
                .filter(account -> !account.isAdminAccount())
                .sorted(Comparator.comparing(UserAccount::getSeq))
                .map(account -> toAdminProgressResponse(account, resolveProgress(account.getSeq())))
                .toList();
    }

    public Map<String, Object> getProgressForAccountAsAdmin(Long userSeq) {
        UserAccount account = authService.getUserBySeq(userSeq);
        return toAdminProgressResponse(account, resolveProgress(userSeq));
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> updateProgressForAccountAsPortal(Long userSeq, UpdateSocialProgressRequest request) {
        UserAccount account = authService.getUserBySeq(userSeq);
        SocialProgress progress = getOrCreateProgress(userSeq);
        progress.setDayCount(request.dayCount());
        progress.setProgressString(SocialProgress.normalizeProgressString(request.progressString()));
        progress.setLastVisitDate(LocalDate.now(KOREA));
        return toAdminProgressResponse(account, socialProgressRepository.save(progress));
    }

    public List<Map<String, Object>> listActivityLogsForAccountAsPortal(Long userSeq) {
        authService.getUserBySeq(userSeq);
        return listActivityLogsInternal(userSeq);
    }

    @Transactional("mbodyTransactionManager")
    public void createDefaultProgress(Long userSeq) {
        if (socialProgressRepository.existsById(userSeq)) {
            return;
        }
        SocialProgress progress = new SocialProgress();
        progress.setUserAccountSeq(userSeq);
        progress.setDayCount(1);
        progress.setLastVisitDate(LocalDate.now(KOREA));
        progress.setProgressString(SocialProgress.DEFAULT_PROGRESS_STRING);
        socialProgressRepository.save(progress);
    }

    private List<Map<String, Object>> listActivityLogsInternal(Long userSeq) {
        return socialActivityLogRepository.findTop100ByUserAccountSeqOrderBySeqDesc(userSeq).stream()
                .map(this::toActivityLogResponse)
                .toList();
    }

    private SocialProgress resolveProgress(Long userSeq) {
        return socialProgressRepository.findById(userSeq).orElseGet(() -> {
            createDefaultProgress(userSeq);
            return socialProgressRepository.findById(userSeq)
                    .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "Social progress not found"));
        });
    }

    private SocialProgress getOrCreateProgress(Long userSeq) {
        return socialProgressRepository.findById(userSeq).orElseGet(() -> {
            SocialProgress created = new SocialProgress();
            created.setUserAccountSeq(userSeq);
            created.setDayCount(1);
            created.setLastVisitDate(LocalDate.now(KOREA));
            created.setProgressString(SocialProgress.DEFAULT_PROGRESS_STRING);
            return socialProgressRepository.save(created);
        });
    }

    private void bumpDayCountForNewDay(SocialProgress progress) {
        LocalDate today = LocalDate.now(KOREA);
        if (progress.getLastVisitDate().isBefore(today)) {
            progress.setDayCount(progress.getDayCount() + 1);
            progress.setLastVisitDate(today);
            socialProgressRepository.save(progress);
        }
    }

    private Map<String, Object> toProgressResponse(SocialProgress progress) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("userAccountSeq", progress.getUserAccountSeq());
        body.put("dayCount", progress.getDayCount());
        body.put("progressString", progress.getProgressString());
        body.put("lastVisitDate", progress.getLastVisitDate().toString());
        return body;
    }

    private Map<String, Object> toAdminProgressResponse(UserAccount account, SocialProgress progress) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("seq", account.getSeq());
        body.put("id", account.getLoginId());
        body.put("name", account.getName());
        body.put("email", account.getEmail());
        body.put("phone", account.getPhone());
        body.put("dayCount", progress.getDayCount());
        body.put("progressString", progress.getProgressString());
        body.put("lastVisitDate", progress.getLastVisitDate().toString());
        return body;
    }

    private Map<String, Object> toActivityLogResponse(SocialActivityLog log) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("seq", log.getSeq());
        body.put("userAccountSeq", log.getUserAccountSeq());
        body.put("logDate", log.getLogDate());
        body.put("logTime", log.getLogTime());
        body.put("logBody", log.getLogBody());
        return body;
    }
}
