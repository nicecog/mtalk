package com.mtalk.mface.controller;

import com.mtalk.mface.dto.CreateFaceActivityLogRequest;
import com.mtalk.mface.dto.UpdateFaceProgressRequest;
import com.mtalk.mface.service.FaceProgressService;
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
@RequestMapping("/mface/api/players")
public class MfaceFaceProgressController {

    private final FaceProgressService faceProgressService;

    public MfaceFaceProgressController(FaceProgressService faceProgressService) {
        this.faceProgressService = faceProgressService;
    }

    @GetMapping("/me/progress")
    public Map<String, Object> syncMyProgress(HttpSession session) {
        return faceProgressService.syncMyProgress(session);
    }

    @PutMapping("/me/progress")
    public Map<String, Object> updateMyProgress(
            @Valid @RequestBody UpdateFaceProgressRequest request,
            HttpSession session
    ) {
        return faceProgressService.updateMyProgress(request, session);
    }

    @PostMapping("/me/activity-logs")
    public Map<String, Object> addMyActivityLog(
            @Valid @RequestBody CreateFaceActivityLogRequest request,
            HttpSession session
    ) {
        return faceProgressService.addMyActivityLog(request, session);
    }

    @GetMapping("/me/activity-logs")
    public List<Map<String, Object>> listMyActivityLogs(HttpSession session) {
        return faceProgressService.listMyActivityLogs(session);
    }
}
