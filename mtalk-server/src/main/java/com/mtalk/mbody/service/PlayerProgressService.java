package com.mtalk.mbody.service;

import com.mtalk.mbody.domain.PlayerProgress;
import com.mtalk.mbody.domain.TrainingResult;
import com.mtalk.mbody.domain.UserAccount;
import com.mtalk.mbody.dto.CreateTrainingResultRequest;
import com.mtalk.mbody.dto.UpdatePlayerProgressRequest;
import com.mtalk.mbody.repository.MbodyPlayerProgressRepository;
import com.mtalk.mbody.repository.MbodyTrainingResultRepository;
import com.mtalk.mbody.repository.MbodyUserAccountRepository;
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
public class PlayerProgressService {

    private static final ZoneId KOREA = ZoneId.of("Asia/Seoul");

    private final MbodyPlayerProgressRepository playerProgressRepository;
    private final MbodyTrainingResultRepository trainingResultRepository;
    private final MbodyUserAccountRepository userAccountRepository;
    private final AuthService authService;

    public PlayerProgressService(
            MbodyPlayerProgressRepository playerProgressRepository,
            MbodyTrainingResultRepository trainingResultRepository,
            MbodyUserAccountRepository userAccountRepository,
            AuthService authService
    ) {
        this.playerProgressRepository = playerProgressRepository;
        this.trainingResultRepository = trainingResultRepository;
        this.userAccountRepository = userAccountRepository;
        this.authService = authService;
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> syncMyProgress(HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        PlayerProgress progress = getOrCreateProgress(userSeq);
        bumpPlayTimesForNewDay(progress);
        return toProgressResponse(progress);
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> updateMyProgress(UpdatePlayerProgressRequest request, HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        PlayerProgress progress = getOrCreateProgress(userSeq);
        progress.setPlayTimes(request.playTimes());
        progress.setBodyPlayTimes(request.bodyPlayTimes());
        progress.setDanceStar(PlayerProgress.normalizeDanceStar(request.danceStar()));
        progress.setLastVisitDate(LocalDate.now(KOREA));
        return toProgressResponse(playerProgressRepository.save(progress));
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> addMyTrainingResult(CreateTrainingResultRequest request, HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        TrainingResult result = new TrainingResult();
        result.setUserAccountSeq(userSeq);
        result.setRecordedAt(request.recordedAt());
        result.setResultType(request.resultType());
        result.setTrial(request.trial());
        result.setPoint(request.point());
        result.setDetails(request.details() != null ? request.details() : "");
        return toTrainingResultResponse(trainingResultRepository.save(result));
    }

    public List<Map<String, Object>> listMyTrainingResults(HttpSession session) {
        Long userSeq = authService.requireUserSeq(session);
        return trainingResultRepository.findTop100ByUserAccountSeqOrderBySeqDesc(userSeq).stream()
                .map(this::toTrainingResultResponse)
                .toList();
    }

    public List<Map<String, Object>> listAllProgress(HttpSession session) {
        authService.requireAdmin(session);
        return userAccountRepository.findAll().stream()
                .sorted(Comparator.comparing(UserAccount::getSeq))
                .map(account -> toAdminProgressResponse(account, resolveProgress(account.getSeq())))
                .toList();
    }

    public Map<String, Object> getProgressForAccount(Long userSeq, HttpSession session) {
        authService.requireAdmin(session);
        UserAccount account = authService.getUserBySeq(userSeq);
        return toAdminProgressResponse(account, resolveProgress(userSeq));
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> updateProgressForAccount(
            Long userSeq,
            UpdatePlayerProgressRequest request,
            HttpSession session
    ) {
        authService.requireAdmin(session);
        UserAccount account = authService.getUserBySeq(userSeq);
        PlayerProgress progress = getOrCreateProgress(userSeq);
        progress.setPlayTimes(request.playTimes());
        progress.setBodyPlayTimes(request.bodyPlayTimes());
        progress.setDanceStar(PlayerProgress.normalizeDanceStar(request.danceStar()));
        progress.setLastVisitDate(LocalDate.now(KOREA));
        return toAdminProgressResponse(account, playerProgressRepository.save(progress));
    }

    public List<Map<String, Object>> listTrainingResultsForAccount(Long userSeq, HttpSession session) {
        authService.requireAdmin(session);
        authService.getUserBySeq(userSeq);
        return listTrainingResultsForAccountInternal(userSeq);
    }

    public List<Map<String, Object>> listTrainingResultsForAccountAsPortal(Long userSeq) {
        authService.getUserBySeq(userSeq);
        return listTrainingResultsForAccountInternal(userSeq);
    }

    private List<Map<String, Object>> listTrainingResultsForAccountInternal(Long userSeq) {
        return trainingResultRepository.findTop100ByUserAccountSeqOrderBySeqDesc(userSeq).stream()
                .map(this::toTrainingResultResponse)
                .toList();
    }

    @Transactional("mbodyTransactionManager")
    public void createDefaultProgress(Long userSeq) {
        if (playerProgressRepository.existsById(userSeq)) {
            return;
        }
        PlayerProgress progress = new PlayerProgress();
        progress.setUserAccountSeq(userSeq);
        progress.setPlayTimes(1);
        progress.setBodyPlayTimes(0);
        progress.setLastVisitDate(LocalDate.now(KOREA));
        progress.setDanceStar(PlayerProgress.DEFAULT_DANCE_STAR);
        playerProgressRepository.save(progress);
    }

    public Map<String, Object> getProgressForAccountAsAdmin(Long userSeq) {
        UserAccount account = authService.getUserBySeq(userSeq);
        return toAdminProgressResponse(account, resolveProgress(userSeq));
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> updateProgressForAccountAsPortal(Long userSeq, UpdatePlayerProgressRequest request) {
        UserAccount account = authService.getUserBySeq(userSeq);
        PlayerProgress progress = getOrCreateProgress(userSeq);
        progress.setPlayTimes(request.playTimes());
        progress.setBodyPlayTimes(request.bodyPlayTimes());
        progress.setDanceStar(PlayerProgress.normalizeDanceStar(request.danceStar()));
        progress.setLastVisitDate(LocalDate.now(KOREA));
        return toAdminProgressResponse(account, playerProgressRepository.save(progress));
    }

    public List<Map<String, Object>> listAllProgressForPortal() {
        return userAccountRepository.findAll().stream()
                .filter(account -> !account.isAdminAccount())
                .sorted(Comparator.comparing(UserAccount::getSeq))
                .map(account -> toAdminProgressResponse(account, resolveProgress(account.getSeq())))
                .toList();
    }

    private PlayerProgress resolveProgress(Long userSeq) {
        return playerProgressRepository.findById(userSeq).orElseGet(() -> {
            createDefaultProgress(userSeq);
            return playerProgressRepository.findById(userSeq)
                    .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "Player progress not found"));
        });
    }

    private PlayerProgress getOrCreateProgress(Long userSeq) {
        return playerProgressRepository.findById(userSeq).orElseGet(() -> {
            PlayerProgress created = new PlayerProgress();
            created.setUserAccountSeq(userSeq);
            created.setPlayTimes(1);
            created.setBodyPlayTimes(0);
            created.setLastVisitDate(LocalDate.now(KOREA));
            created.setDanceStar(PlayerProgress.DEFAULT_DANCE_STAR);
            return playerProgressRepository.save(created);
        });
    }

    private void bumpPlayTimesForNewDay(PlayerProgress progress) {
        LocalDate today = LocalDate.now(KOREA);
        if (progress.getLastVisitDate().isBefore(today)) {
            progress.setPlayTimes(progress.getPlayTimes() + 1);
            progress.setLastVisitDate(today);
            playerProgressRepository.save(progress);
        }
    }

    private Map<String, Object> toProgressResponse(PlayerProgress progress) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("userAccountSeq", progress.getUserAccountSeq());
        body.put("playTimes", progress.getPlayTimes());
        body.put("bodyPlayTimes", progress.getBodyPlayTimes());
        body.put("danceStar", progress.getDanceStar());
        body.put("lastVisitDate", progress.getLastVisitDate().toString());
        return body;
    }

    private Map<String, Object> toAdminProgressResponse(UserAccount account, PlayerProgress progress) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("seq", account.getSeq());
        body.put("id", account.getLoginId());
        body.put("name", account.getName());
        body.put("email", account.getEmail());
        body.put("phone", account.getPhone());
        body.put("playTimes", progress.getPlayTimes());
        body.put("bodyPlayTimes", progress.getBodyPlayTimes());
        body.put("danceStar", progress.getDanceStar());
        body.put("lastVisitDate", progress.getLastVisitDate().toString());
        return body;
    }

    private Map<String, Object> toTrainingResultResponse(TrainingResult result) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("seq", result.getSeq());
        body.put("userAccountSeq", result.getUserAccountSeq());
        body.put("recordedAt", result.getRecordedAt());
        body.put("resultType", result.getResultType());
        body.put("trial", result.getTrial());
        body.put("point", result.getPoint());
        body.put("details", result.getDetails());
        return body;
    }
}
