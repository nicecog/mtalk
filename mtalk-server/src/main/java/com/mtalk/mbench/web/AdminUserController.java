package com.mtalk.mbench.web;

import com.mtalk.mbench.dto.CreateAccountRequest;
import com.mtalk.mbench.dto.UpdateAccountRequest;
import com.mtalk.mbench.dto.UserAccountResponse;
import com.mtalk.mbench.service.AdminUserService;
import com.mtalk.mbench.service.UserService;
import jakarta.servlet.http.HttpSession;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/mbench/api/admin")
public class AdminUserController {

    private final AdminUserService adminUserService;
    private final UserService userService;

    public AdminUserController(AdminUserService adminUserService, UserService userService) {
        this.adminUserService = adminUserService;
        this.userService = userService;
    }

    @GetMapping(value = "/session", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> session(HttpSession session) {
        Boolean isAdmin = (Boolean) session.getAttribute(UserService.SESSION_IS_ADMIN);
        Long seq = (Long) session.getAttribute(UserService.SESSION_USER_SEQ);
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("loggedIn", Boolean.TRUE.equals(isAdmin));
        body.put("seq", seq);
        return ResponseEntity.ok(body);
    }

    @GetMapping(value = "/users", produces = MediaType.APPLICATION_JSON_VALUE)
    public List<UserAccountResponse> listUsers(HttpSession session) {
        return adminUserService.listSubjects(session);
    }

    @GetMapping(value = "/users/{seq}", produces = MediaType.APPLICATION_JSON_VALUE)
    public UserAccountResponse getUser(@PathVariable long seq, HttpSession session) {
        return adminUserService.getSubject(seq, session);
    }

    @PostMapping(value = "/users", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public UserAccountResponse createUser(@RequestBody CreateAccountRequest request, HttpSession session) {
        return adminUserService.createSubject(request, session);
    }

    @PutMapping(value = "/users/{seq}", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public UserAccountResponse updateUser(
            @PathVariable long seq,
            @RequestBody UpdateAccountRequest request,
            HttpSession session
    ) {
        return adminUserService.updateSubject(seq, request, session);
    }

    @DeleteMapping(value = "/users/{seq}", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> deleteUser(@PathVariable long seq, HttpSession session) {
        adminUserService.deleteSubject(seq, session);
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("success", true);
        body.put("message", "deleted");
        return ResponseEntity.ok(body);
    }
}
