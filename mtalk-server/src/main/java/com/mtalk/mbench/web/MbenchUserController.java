package com.mtalk.mbench.web;

import com.mtalk.mbench.domain.UserAccount;
import com.mtalk.mbench.dto.ApiResponses;
import com.mtalk.mbench.dto.CreateAccountRequest;
import com.mtalk.mbench.dto.LoginRequest;
import com.mtalk.mbench.service.UserService;
import jakarta.servlet.http.HttpSession;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/mbench/api/users")
public class MbenchUserController {

    private final UserService userService;

    public MbenchUserController(UserService userService) {
        this.userService = userService;
    }

    /** Unity: GET /api/users/accounts/id/has/{id} ??body??true/false ?얜챷???*/
    @GetMapping(value = "/accounts/id/has/{loginId}", produces = MediaType.TEXT_PLAIN_VALUE)
    public ResponseEntity<String> hasAccount(@PathVariable String loginId) {
        return ResponseEntity.ok(ApiResponses.accountExistsBody(userService.accountExists(loginId)));
    }

    /** Unity: POST /api/users/login ??body {id,password}, response??seq ??釉?JSON */
    @PostMapping(value = "/login", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> login(@RequestBody LoginRequest request, HttpSession session) {
        UserAccount account = userService.login(request.id, request.password, session);
        return ResponseEntity.ok(ApiResponses.loginSuccess(account.getSeq(), account.getLoginId()));
    }

    /** Unity: POST /api/users/accounts ??admin ?紐꾨??袁⑹뒄 */
    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> createAccount(@RequestBody CreateAccountRequest request, HttpSession session) {
        UserAccount account = userService.createAccount(request, session);
        return ResponseEntity.ok(ApiResponses.loginSuccess(account.getSeq(), account.getLoginId()));
    }

    /** Unity: GET /api/users/logout */
    @GetMapping(value = "/logout", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> logout(HttpSession session) {
        userService.logout(session);
        return ResponseEntity.ok(ApiResponses.ok("logout ok"));
    }
}
