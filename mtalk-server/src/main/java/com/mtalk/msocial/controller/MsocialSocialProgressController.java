package com.mtalk.msocial.controller;

import com.mtalk.msocial.dto.CreateSocialActivityLogRequest;
import com.mtalk.msocial.dto.UpdateSocialProgressRequest;
import com.mtalk.msocial.service.SocialProgressService;
import jakarta.servlet.http.HttpSession;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/msocial/api/players")
public class MsocialSocialProgressController {

    private final SocialProgressService socialProgressService;

    public MsocialSocialProgressController(SocialProgressService socialProgressService) {
        this.socialProgressService = socialProgressService;
    }

    @GetMapping("/me/progress")
    public Map<String, Object> syncMyProgress(HttpSession session) {
        return socialProgressService.syncMyProgress(session);
    }

    @PutMapping("/me/progress")
    public Map<String, Object> updateMyProgress(
            @Valid @RequestBody UpdateSocialProgressRequest request,
            HttpSession session
    ) {
        return socialProgressService.updateMyProgress(request, session);
    }

    @PostMapping("/me/activity-logs")
    public Map<String, Object> addMyActivityLog(
            @Valid @RequestBody CreateSocialActivityLogRequest request,
            HttpSession session
    ) {
        return socialProgressService.addMyActivityLog(request, session);
    }

    @GetMapping("/me/activity-logs")
    public List<Map<String, Object>> listMyActivityLogs(HttpSession session) {
        return socialProgressService.listMyActivityLogs(session);
    }
}
