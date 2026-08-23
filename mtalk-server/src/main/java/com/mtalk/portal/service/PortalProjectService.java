package com.mtalk.portal.service;

import com.mtalk.portal.domain.PortalProject;
import com.mtalk.portal.domain.PortalProjectUpdate;
import com.mtalk.portal.dto.PortalProjectBoardResponse;
import com.mtalk.portal.dto.PortalProjectDetailResponse;
import com.mtalk.portal.dto.PortalProjectResponse;
import com.mtalk.portal.dto.PortalProjectUpdateCreateRequest;
import com.mtalk.portal.dto.PortalProjectUpdateResponse;
import com.mtalk.portal.dto.PortalProjectUpsertRequest;
import com.mtalk.portal.repository.PortalProjectRepository;
import com.mtalk.portal.repository.PortalProjectUpdateRepository;
import jakarta.servlet.http.HttpSession;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

import java.time.Instant;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;

@Service
public class PortalProjectService {

    private static final ZoneId ZONE = ZoneId.of("Asia/Seoul");
    private static final DateTimeFormatter FMT = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm");
    private static final Set<String> STATUSES = Set.of("planned", "in_progress", "paused", "done");
    private static final Map<String, String> STATUS_LABELS = new LinkedHashMap<>();

    static {
        STATUS_LABELS.put("planned", "예정");
        STATUS_LABELS.put("in_progress", "진행중");
        STATUS_LABELS.put("paused", "보류");
        STATUS_LABELS.put("done", "완료");
    }

    private final PortalProjectRepository projectRepository;
    private final PortalProjectUpdateRepository updateRepository;
    private final PortalAuthService portalAuthService;

    public PortalProjectService(
            PortalProjectRepository projectRepository,
            PortalProjectUpdateRepository updateRepository,
            PortalAuthService portalAuthService
    ) {
        this.projectRepository = projectRepository;
        this.updateRepository = updateRepository;
        this.portalAuthService = portalAuthService;
    }

    @Transactional(readOnly = true)
    public PortalProjectBoardResponse list(String status, String category, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        String statusFilter = blankToNull(status);
        String categoryFilter = blankToNull(category);
        if (statusFilter != null && !STATUSES.contains(statusFilter)) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "상태를 확인해 주세요.");
        }

        List<PortalProject> rows;
        if (statusFilter != null && categoryFilter != null) {
            rows = projectRepository.findByStatusAndCategoryOrderByUpdatedAtDesc(statusFilter, categoryFilter);
        } else if (statusFilter != null) {
            rows = projectRepository.findByStatusOrderByUpdatedAtDesc(statusFilter);
        } else if (categoryFilter != null) {
            rows = projectRepository.findByCategoryOrderByUpdatedAtDesc(categoryFilter);
        } else {
            rows = projectRepository.findAllByOrderByUpdatedAtDesc();
        }

        List<PortalProjectResponse> projects = rows.stream().map(this::toListItem).toList();
        return new PortalProjectBoardResponse(
                projectRepository.count(),
                projectRepository.countByStatus("planned"),
                projectRepository.countByStatus("in_progress"),
                projectRepository.countByStatus("paused"),
                projectRepository.countByStatus("done"),
                projects
        );
    }

    @Transactional(readOnly = true)
    public PortalProjectDetailResponse get(long seq, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        PortalProject project = requireProject(seq);
        List<PortalProjectUpdateResponse> updates = updateRepository.findByProjectSeqOrderBySeqDesc(seq).stream()
                .map(this::toUpdate)
                .toList();
        return new PortalProjectDetailResponse(toListItem(project), updates);
    }

    @Transactional
    public PortalProjectResponse create(PortalProjectUpsertRequest request, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        String actor = actorName(session);
        PortalProject project = new PortalProject();
        apply(project, request);
        project.setCreatedBy(actor);
        project.setUpdatedBy(actor);
        return toListItem(projectRepository.save(project));
    }

    @Transactional
    public PortalProjectResponse update(long seq, PortalProjectUpsertRequest request, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        PortalProject project = requireProject(seq);
        apply(project, request);
        project.setUpdatedBy(actorName(session));
        return toListItem(projectRepository.save(project));
    }

    @Transactional
    public void delete(long seq, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        requireProject(seq);
        updateRepository.deleteByProjectSeq(seq);
        projectRepository.deleteById(seq);
    }

    @Transactional
    public PortalProjectUpdateResponse addUpdate(long seq, PortalProjectUpdateCreateRequest request, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        PortalProject project = requireProject(seq);
        String actor = actorName(session);
        PortalProjectUpdate update = new PortalProjectUpdate();
        update.setProjectSeq(seq);
        update.setContent(request.content().trim());
        update.setProgress(request.progress());
        update.setCreatedBy(actor);
        PortalProjectUpdate saved = updateRepository.save(update);
        if (request.progress() != null) {
            project.setProgress(request.progress());
        }
        project.setUpdatedBy(actor);
        project.setUpdatedAt(Instant.now());
        projectRepository.save(project);
        return toUpdate(saved);
    }

    @Transactional
    public void deleteUpdate(long projectSeq, long updateSeq, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        requireProject(projectSeq);
        PortalProjectUpdate update = updateRepository.findById(updateSeq)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "진행사항을 찾을 수 없습니다."));
        if (update.getProjectSeq() == null || update.getProjectSeq() != projectSeq) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "진행사항을 찾을 수 없습니다.");
        }
        updateRepository.delete(update);
    }

    private PortalProject requireProject(long seq) {
        return projectRepository.findById(seq)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "프로젝트를 찾을 수 없습니다."));
    }

    private void apply(PortalProject project, PortalProjectUpsertRequest request) {
        String status = request.status() == null ? "" : request.status().trim();
        if (!STATUSES.contains(status)) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "상태를 확인해 주세요.");
        }
        project.setTitle(request.title().trim());
        project.setCategory(blankToDefault(request.category(), "기타"));
        project.setStatus(status);
        project.setProgress(request.progress());
        project.setSummary(request.summary() == null ? "" : request.summary().trim());
        project.setOwnerName(request.ownerName() == null ? "" : request.ownerName().trim());
    }

    private PortalProjectResponse toListItem(PortalProject project) {
        long seq = project.getSeq();
        long updateCount = updateRepository.countByProjectSeq(seq);
        String latest = updateRepository.findFirstByProjectSeqOrderBySeqDesc(seq)
                .map(PortalProjectUpdate::getContent)
                .orElse("");
        return new PortalProjectResponse(
                seq,
                project.getTitle(),
                project.getCategory(),
                project.getStatus(),
                STATUS_LABELS.getOrDefault(project.getStatus(), project.getStatus()),
                project.getProgress(),
                project.getSummary(),
                project.getOwnerName(),
                latest,
                updateCount,
                project.getCreatedBy(),
                project.getUpdatedBy(),
                format(project.getCreatedAt()),
                format(project.getUpdatedAt())
        );
    }

    private PortalProjectUpdateResponse toUpdate(PortalProjectUpdate update) {
        return new PortalProjectUpdateResponse(
                update.getSeq(),
                update.getContent(),
                update.getProgress(),
                update.getCreatedBy(),
                format(update.getCreatedAt())
        );
    }

    private String actorName(HttpSession session) {
        Object displayName = session.getAttribute(PortalAuthService.SESSION_DISPLAY_NAME);
        if (displayName != null && !String.valueOf(displayName).isBlank()) {
            return String.valueOf(displayName).trim();
        }
        Object loginId = session.getAttribute(PortalAuthService.SESSION_LOGIN_ID);
        return loginId == null ? "관리자" : String.valueOf(loginId);
    }

    private static String format(Instant value) {
        if (value == null) {
            return "";
        }
        return FMT.format(value.atZone(ZONE));
    }

    private static String blankToNull(String value) {
        if (value == null || value.isBlank() || "all".equalsIgnoreCase(value)) {
            return null;
        }
        return value.trim();
    }

    private static String blankToDefault(String value, String fallback) {
        if (value == null || value.isBlank()) {
            return fallback;
        }
        return value.trim();
    }
}
