package com.mtalk.mbody.controller;

import com.mtalk.mbody.dto.CreateTrainingResultRequest;
import com.mtalk.mbody.dto.UpdatePlayerProgressRequest;
import com.mtalk.mbody.service.PlayerProgressService;
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
@RequestMapping("/mbody/api/players")
public class MbodyPlayerProgressController {

    private final PlayerProgressService playerProgressService;

    public MbodyPlayerProgressController(PlayerProgressService playerProgressService) {
        this.playerProgressService = playerProgressService;
    }

    @GetMapping("/me/progress")
    public Map<String, Object> syncMyProgress(HttpSession session) {
        return playerProgressService.syncMyProgress(session);
    }

    @PutMapping("/me/progress")
    public Map<String, Object> updateMyProgress(
            @Valid @RequestBody UpdatePlayerProgressRequest request,
            HttpSession session
    ) {
        return playerProgressService.updateMyProgress(request, session);
    }

    @PostMapping("/me/training-results")
    public Map<String, Object> addMyTrainingResult(
            @Valid @RequestBody CreateTrainingResultRequest request,
            HttpSession session
    ) {
        return playerProgressService.addMyTrainingResult(request, session);
    }

    @GetMapping("/me/training-results")
    public List<Map<String, Object>> listMyTrainingResults(HttpSession session) {
        return playerProgressService.listMyTrainingResults(session);
    }
}
