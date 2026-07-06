package com.mtalk.mbody.controller;

import com.mtalk.mbody.domain.UserAccount;
import com.mtalk.mbody.dto.CreateUserAccountRequest;
import com.mtalk.mbody.dto.LoginRequest;
import com.mtalk.mbody.service.AuthService;
import jakarta.servlet.http.HttpSession;
import jakarta.validation.Valid;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

@RestController
@RequestMapping("/mbody/api/users")
public class MbodyUserController {

    private final AuthService authService;

    public MbodyUserController(AuthService authService) {
        this.authService = authService;
    }

    @GetMapping(value = "/accounts/id/has/{id}", produces = MediaType.APPLICATION_JSON_VALUE)
    public boolean hasAccount(@PathVariable String id) {
        return authService.hasAccount(id);
    }

    @PostMapping("/login")
    public Map<String, Object> login(@Valid @RequestBody LoginRequest request, HttpSession session) {
        return authService.login(request, session);
    }

    @PostMapping("/accounts")
    public Map<String, Object> createAccount(@Valid @RequestBody CreateUserAccountRequest request, HttpSession session) {
        UserAccount created = authService.createAccount(request, session);
        return authService.toAccountResponse(created);
    }

    @GetMapping("/logout")
    public Map<String, String> logout(HttpSession session) {
        authService.logout(session);
        return Map.of("message", "logged out");
    }
}
