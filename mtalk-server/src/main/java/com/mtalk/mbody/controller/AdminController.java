package com.mtalk.mbody.controller;

import com.mtalk.mbody.dto.UpdateCredentialsRequest;
import com.mtalk.mbody.dto.UpdatePlayerProgressRequest;
import com.mtalk.mbody.service.AuthService;
import com.mtalk.mbody.service.PlayerProgressService;
import jakarta.servlet.http.HttpSession;
import jakarta.validation.Valid;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseBody;

import java.util.List;
import java.util.Map;

@Controller
public class AdminController {

    private final AuthService authService;
    private final PlayerProgressService playerProgressService;

    public AdminController(AuthService authService, PlayerProgressService playerProgressService) {
        this.authService = authService;
        this.playerProgressService = playerProgressService;
    }

    @GetMapping({"/mbody/admin", "/mbody/admin/"})
    public String adminPage() {
        return "forward:/mbody/admin/index.html";
    }

    @GetMapping("/mbody/api/admin/session")
    @ResponseBody
    public Map<String, Object> currentSession(HttpSession session) {
        return authService.currentSession(session);
    }

    @GetMapping("/mbody/api/admin/accounts")
    @ResponseBody
    public List<Map<String, Object>> listAccounts(HttpSession session) {
        return authService.listAccounts(session);
    }

    @PutMapping("/mbody/api/admin/accounts/{seq}/credentials")
    @ResponseBody
    public Map<String, Object> updateCredentials(@PathVariable Long seq,
                                                 @Valid @RequestBody UpdateCredentialsRequest request,
                                                 HttpSession session) {
        return authService.updateCredentials(seq, request, session);
    }

    @GetMapping("/mbody/api/admin/progress")
    @ResponseBody
    public List<Map<String, Object>> listProgress(HttpSession session) {
        return playerProgressService.listAllProgress(session);
    }

    @GetMapping("/mbody/api/admin/accounts/{seq}/progress")
    @ResponseBody
    public Map<String, Object> getProgress(@PathVariable Long seq, HttpSession session) {
        return playerProgressService.getProgressForAccount(seq, session);
    }

    @PutMapping("/mbody/api/admin/accounts/{seq}/progress")
    @ResponseBody
    public Map<String, Object> updateProgress(@PathVariable Long seq,
                                            @Valid @RequestBody UpdatePlayerProgressRequest request,
                                            HttpSession session) {
        return playerProgressService.updateProgressForAccount(seq, request, session);
    }

    @GetMapping("/mbody/api/admin/accounts/{seq}/training-results")
    @ResponseBody
    public List<Map<String, Object>> listTrainingResults(@PathVariable Long seq, HttpSession session) {
        return playerProgressService.listTrainingResultsForAccount(seq, session);
    }
}
