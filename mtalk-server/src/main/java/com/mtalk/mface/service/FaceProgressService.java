package com.mtalk.mface.service;

import com.mtalk.mbody.domain.FaceActivityLog;
import com.mtalk.mbody.domain.FaceProgress;
import com.mtalk.mbody.domain.UserAccount;
import com.mtalk.mbody.repository.MfaceFaceActivityLogRepository;
import com.mtalk.mbody.repository.MfaceFaceProgressRepository;
import com.mtalk.mbody.repository.MbodyUserAccountRepository;
import com.mtalk.mbody.service.AuthService;
import com.mtalk.mface.dto.CreateFaceActivityLogRequest;
import com.mtalk.mface.dto.UpdateFaceProgressRequest;
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
public class FaceProgressService {

    private static final ZoneId KOREA = ZoneId.of("Asia/Seoul");

    private final MfaceFaceProgressRepository faceProgressRepository;
    private final MfaceFaceActivityLogRepository faceActivityLogRepository;
    private final MbodyUserAccountRepository userAccountRepository;
    private final AuthService authService;

    public FaceProgressService(
            MfaceFaceProgressRepository faceProgressRepository,
            MfaceFaceActivityLogRepository faceActivityLogRepository,
            MbodyUserAccountRepository userAccountRepository,
            AuthService authService
    ) {
        this.faceProgressRepository = faceProgressRepository;
        this.faceActivityLogRepository = faceActivityLogRepository;
        this.userAccountRepository = userAccountRepository;
        this.authService = authService;
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> syncMyProgress(HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        FaceProgress progress = getOrCreateProgress(userSeq);
        bumpDayCountForNewDay(progress);
        return toProgressResponse(progress);
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> updateMyProgress(UpdateFaceProgressRequest request, HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        FaceProgress progress = getOrCreateProgress(userSeq);
        progress.setDayCount(request.dayCount());
        progress.setItemString(FaceProgress.normalizeItemString(request.itemString()));
        progress.setLastVisitDate(LocalDate.now(KOREA));
        return toProgressResponse(faceProgressRepository.save(progress));
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> addMyActivityLog(CreateFaceActivityLogRequest request, HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        FaceActivityLog log = new FaceActivityLog();
        log.setUserAccountSeq(userSeq);
        log.setLogDate(request.logDate());
        log.setLogTime(request.logTime());
        log.setLogBody(request.logBody() != null ? request.logBody() : "");
        return toActivityLogResponse(faceActivityLogRepository.save(log));
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
    public Map<String, Object> updateProgressForAccountAsPortal(Long userSeq, UpdateFaceProgressRequest request) {
        UserAccount account = authService.getUserBySeq(userSeq);
        FaceProgress progress = getOrCreateProgress(userSeq);
        progress.setDayCount(request.dayCount());
        progress.setItemString(FaceProgress.normalizeItemString(request.itemString()));
        progress.setLastVisitDate(LocalDate.now(KOREA));
        return toAdminProgressResponse(account, faceProgressRepository.save(progress));
    }

    public List<Map<String, Object>> listActivityLogsForAccountAsPortal(Long userSeq) {
        authService.getUserBySeq(userSeq);
        return listActivityLogsInternal(userSeq);
    }

    @Transactional("mbodyTransactionManager")
    public void createDefaultProgress(Long userSeq) {
        if (faceProgressRepository.existsById(userSeq)) {
            return;
        }
        FaceProgress progress = new FaceProgress();
        progress.setUserAccountSeq(userSeq);
        progress.setDayCount(1);
        progress.setLastVisitDate(LocalDate.now(KOREA));
        progress.setItemString(FaceProgress.DEFAULT_ITEM_STRING);
        faceProgressRepository.save(progress);
    }

    private List<Map<String, Object>> listActivityLogsInternal(Long userSeq) {
        return faceActivityLogRepository.findTop100ByUserAccountSeqOrderBySeqDesc(userSeq).stream()
                .map(this::toActivityLogResponse)
                .toList();
    }

    private FaceProgress resolveProgress(Long userSeq) {
        return faceProgressRepository.findById(userSeq).orElseGet(() -> {
            createDefaultProgress(userSeq);
            return faceProgressRepository.findById(userSeq)
                    .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "Face progress not found"));
        });
    }

    private FaceProgress getOrCreateProgress(Long userSeq) {
        return faceProgressRepository.findById(userSeq).orElseGet(() -> {
            FaceProgress created = new FaceProgress();
            created.setUserAccountSeq(userSeq);
            created.setDayCount(1);
            created.setLastVisitDate(LocalDate.now(KOREA));
            created.setItemString(FaceProgress.DEFAULT_ITEM_STRING);
            return faceProgressRepository.save(created);
        });
    }

    private void bumpDayCountForNewDay(FaceProgress progress) {
        LocalDate today = LocalDate.now(KOREA);
        if (progress.getLastVisitDate().isBefore(today)) {
            progress.setDayCount(progress.getDayCount() + 1);
            progress.setLastVisitDate(today);
            faceProgressRepository.save(progress);
        }
    }

    private Map<String, Object> toProgressResponse(FaceProgress progress) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("userAccountSeq", progress.getUserAccountSeq());
        body.put("dayCount", progress.getDayCount());
        body.put("itemString", progress.getItemString());
        body.put("lastVisitDate", progress.getLastVisitDate().toString());
        return body;
    }

    private Map<String, Object> toAdminProgressResponse(UserAccount account, FaceProgress progress) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("seq", account.getSeq());
        body.put("id", account.getLoginId());
        body.put("name", account.getName());
        body.put("email", account.getEmail());
        body.put("phone", account.getPhone());
        body.put("dayCount", progress.getDayCount());
        body.put("itemString", progress.getItemString());
        body.put("lastVisitDate", progress.getLastVisitDate().toString());
        return body;
    }

    private Map<String, Object> toActivityLogResponse(FaceActivityLog log) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("seq", log.getSeq());
        body.put("userAccountSeq", log.getUserAccountSeq());
        body.put("logDate", log.getLogDate());
        body.put("logTime", log.getLogTime());
        body.put("logBody", log.getLogBody());
        return body;
    }
}
