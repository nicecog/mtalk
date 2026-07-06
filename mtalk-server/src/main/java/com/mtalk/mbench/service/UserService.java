package com.mtalk.mbench.service;

import com.mtalk.mbench.domain.UserAccount;
import com.mtalk.mbench.dto.CreateAccountRequest;
import com.mtalk.mbench.repository.MbenchUserAccountRepository;
import jakarta.servlet.http.HttpSession;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.web.server.ResponseStatusException;

@Service
public class UserService {

    public static final String SESSION_USER_SEQ = "MBENCH_USER_SEQ";
    public static final String SESSION_IS_ADMIN = "MBENCH_IS_ADMIN";

    private final MbenchUserAccountRepository MbenchUserAccountRepository;
    private final String adminId;
    private final String adminPassword;

    public UserService(
            MbenchUserAccountRepository MbenchUserAccountRepository,
            @Value("${mbench.admin-id}") String adminId,
            @Value("${mbench.admin-password}") String adminPassword
    ) {
        this.MbenchUserAccountRepository = MbenchUserAccountRepository;
        this.adminId = adminId;
        this.adminPassword = adminPassword;
    }

    public boolean accountExists(String loginId) {
        return MbenchUserAccountRepository.existsByLoginId(loginId);
    }

    public UserAccount login(String loginId, String password, HttpSession session) {
        UserAccount account = MbenchUserAccountRepository.findByLoginId(loginId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.UNAUTHORIZED, "invalid credentials"));

        if (!account.getPassword().equals(password)) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "invalid credentials");
        }

        session.setAttribute(SESSION_USER_SEQ, account.getSeq());
        session.setAttribute(SESSION_IS_ADMIN, account.isAdmin());
        return account;
    }

    public UserAccount createAccount(CreateAccountRequest request, HttpSession session) {
        Boolean isAdmin = (Boolean) session.getAttribute(SESSION_IS_ADMIN);
        if (isAdmin == null || !isAdmin) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "admin session required");
        }

        if (MbenchUserAccountRepository.existsByLoginId(request.id)) {
            throw new ResponseStatusException(HttpStatus.CONFLICT, "account already exists");
        }

        UserAccount account = new UserAccount();
        account.setLoginId(request.id);
        account.setPassword(request.password);
        account.setLevelId(request.levelId);
        account.setDomain(request.domain != null ? request.domain : "spike");
        account.setName(request.name);
        account.setEmail(request.email);
        account.setPhone(request.phone);
        account.setAdmin(false);
        return MbenchUserAccountRepository.save(account);
    }

    public void logout(HttpSession session) {
        session.invalidate();
    }

    public void ensureDefaultAdmin() {
        if (MbenchUserAccountRepository.existsByLoginId(adminId)) {
            return;
        }
        UserAccount admin = new UserAccount();
        admin.setLoginId(adminId);
        admin.setPassword(adminPassword);
        admin.setLevelId(10);
        admin.setDomain("spike");
        admin.setName("Super Admin");
        admin.setEmail("admin@mbench.local");
        admin.setPhone("000");
        admin.setAdmin(true);
        MbenchUserAccountRepository.save(admin);
    }
}
