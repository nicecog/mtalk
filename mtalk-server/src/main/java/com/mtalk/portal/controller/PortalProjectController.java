package com.mtalk.portal.controller;

import com.mtalk.portal.dto.PortalProjectBoardResponse;
import com.mtalk.portal.dto.PortalProjectDetailResponse;
import com.mtalk.portal.dto.PortalProjectResponse;
import com.mtalk.portal.dto.PortalProjectUpdateCreateRequest;
import com.mtalk.portal.dto.PortalProjectUpdateResponse;
import com.mtalk.portal.dto.PortalProjectUpsertRequest;
import com.mtalk.portal.service.PortalProjectService;
import jakarta.servlet.http.HttpSession;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/hudit/projects")
public class PortalProjectController {

    private final PortalProjectService portalProjectService;

    public PortalProjectController(PortalProjectService portalProjectService) {
        this.portalProjectService = portalProjectService;
    }

    @GetMapping
    public PortalProjectBoardResponse list(
            @RequestParam(required = false) String status,
            @RequestParam(required = false) String category,
            HttpSession session
    ) {
        return portalProjectService.list(status, category, session);
    }

    @GetMapping("/{seq}")
    public PortalProjectDetailResponse get(@PathVariable long seq, HttpSession session) {
        return portalProjectService.get(seq, session);
    }

    @PostMapping
    public PortalProjectResponse create(
            @Valid @RequestBody PortalProjectUpsertRequest request,
            HttpSession session
    ) {
        return portalProjectService.create(request, session);
    }

    @PutMapping("/{seq}")
    public PortalProjectResponse update(
            @PathVariable long seq,
            @Valid @RequestBody PortalProjectUpsertRequest request,
            HttpSession session
    ) {
        return portalProjectService.update(seq, request, session);
    }

    @DeleteMapping("/{seq}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable long seq, HttpSession session) {
        portalProjectService.delete(seq, session);
    }

    @PostMapping("/{seq}/updates")
    public PortalProjectUpdateResponse addUpdate(
            @PathVariable long seq,
            @Valid @RequestBody PortalProjectUpdateCreateRequest request,
            HttpSession session
    ) {
        return portalProjectService.addUpdate(seq, request, session);
    }

    @DeleteMapping("/{seq}/updates/{updateSeq}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteUpdate(
            @PathVariable long seq,
            @PathVariable long updateSeq,
            HttpSession session
    ) {
        portalProjectService.deleteUpdate(seq, updateSeq, session);
    }
}
