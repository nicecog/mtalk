package com.mtalk.mbody.service;

import com.mtalk.mbody.domain.UserAccount;
import com.mtalk.mbody.dto.CreateUserAccountRequest;
import com.mtalk.mbody.dto.LoginRequest;
import com.mtalk.mbody.dto.UpdateCredentialsRequest;
import com.mtalk.mbody.repository.MbodyUserAccountRepository;
import com.mtalk.mface.service.FaceProgressService;
import com.mtalk.msocial.service.SocialProgressService;
import jakarta.servlet.http.HttpSession;
import org.springframework.context.annotation.Lazy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.web.server.ResponseStatusException;

import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import static org.springframework.http.HttpStatus.FORBIDDEN;
import static org.springframework.http.HttpStatus.NOT_FOUND;
import static org.springframework.http.HttpStatus.UNAUTHORIZED;

@Service
public class AuthService {

    public static final String SESSION_USER_SEQ = "userSeq";
    public static final String SESSION_ADMIN = "admin";

    private final MbodyUserAccountRepository MbodyUserAccountRepository;
    private final PlayerProgressService playerProgressService;
    private final FaceProgressService faceProgressService;
    private final SocialProgressService socialProgressService;
    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();

    public AuthService(MbodyUserAccountRepository MbodyUserAccountRepository,
                       @Lazy PlayerProgressService playerProgressService,
                       @Lazy FaceProgressService faceProgressService,
                       @Lazy SocialProgressService socialProgressService) {
        this.MbodyUserAccountRepository = MbodyUserAccountRepository;
        this.playerProgressService = playerProgressService;
        this.faceProgressService = faceProgressService;
        this.socialProgressService = socialProgressService;
    }

    public Map<String, Object> login(LoginRequest request, HttpSession session) {
        UserAccount account = MbodyUserAccountRepository.findByLoginId(request.id())
                .filter(user -> passwordEncoder.matches(request.password(), user.getPasswordHash()))
                .orElseThrow(() -> new ResponseStatusException(UNAUTHORIZED, "Invalid credentials"));

        session.setAttribute(SESSION_USER_SEQ, account.getSeq());
        session.setAttribute(SESSION_ADMIN, account.isAdminAccount());

        return toAccountResponse(account);
    }

    public UserAccount createAccount(CreateUserAccountRequest request, HttpSession session) {
        requireAdmin(session);

        if (MbodyUserAccountRepository.existsByLoginId(request.id())) {
            throw new ResponseStatusException(FORBIDDEN, "Account already exists");
        }

        UserAccount user = new UserAccount();
        user.setLevelId(request.levelId());
        user.setDomain(request.domain());
        user.setLoginId(request.id());
        user.setPasswordHash(passwordEncoder.encode(request.password()));
        user.setName(request.name());
        user.setEmail(request.email());
        user.setPhone(request.phone());
        user.setAdminAccount(false);
        UserAccount saved = MbodyUserAccountRepository.save(user);
        playerProgressService.createDefaultProgress(saved.getSeq());
        faceProgressService.createDefaultProgress(saved.getSeq());
        socialProgressService.createDefaultProgress(saved.getSeq());
        return saved;
    }

    public boolean hasAccount(String id) {
        return MbodyUserAccountRepository.existsByLoginId(id);
    }

    public UserAccount getUserBySeq(Long seq) {
        return MbodyUserAccountRepository.findById(seq)
                .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "User account not found"));
    }

    public List<Map<String, Object>> listAccounts(HttpSession session) {
        requireAdmin(session);
        return MbodyUserAccountRepository.findAll().stream()
                .sorted(Comparator.comparing(UserAccount::getSeq))
                .map(this::toAccountResponse)
                .toList();
    }

    public Map<String, Object> updateCredentials(Long seq, UpdateCredentialsRequest request, HttpSession session) {
        requireAdmin(session);

        UserAccount account = getUserBySeq(seq);
        MbodyUserAccountRepository.findByLoginIdAndSeqNot(request.id(), seq).ifPresent(existing -> {
            throw new ResponseStatusException(FORBIDDEN, "Login ID already exists");
        });

        account.setLoginId(request.id());
        account.setPasswordHash(passwordEncoder.encode(request.password()));
        return toAccountResponse(MbodyUserAccountRepository.save(account));
    }

    public Map<String, Object> currentSession(HttpSession session) {
        Object seq = session.getAttribute(SESSION_USER_SEQ);
        Object admin = session.getAttribute(SESSION_ADMIN);
        if (!(seq instanceof Long userSeq)) {
            Map<String, Object> body = new LinkedHashMap<>();
            body.put("authenticated", false);
            body.put("admin", false);
            return body;
        }

        UserAccount userAccount = getUserBySeq(userSeq);
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("authenticated", true);
        body.put("admin", admin instanceof Boolean isAdmin && isAdmin);
        body.put("account", toAccountResponse(userAccount));
        return body;
    }

    public void logout(HttpSession session) {
        session.invalidate();
    }

    public void requireAdmin(HttpSession session) {
        Object admin = session.getAttribute(SESSION_ADMIN);
        if (!(admin instanceof Boolean isAdmin) || !isAdmin) {
            throw new ResponseStatusException(FORBIDDEN, "Admin session required");
        }
    }

    public Long requireUserSeq(HttpSession session) {
        Object seq = session.getAttribute(SESSION_USER_SEQ);
        if (!(seq instanceof Long userSeq)) {
            throw new ResponseStatusException(UNAUTHORIZED, "Login required");
        }
        return userSeq;
    }

    public Map<String, Object> toAccountResponse(UserAccount account) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("seq", account.getSeq());
        body.put("id", account.getLoginId());
        body.put("name", account.getName());
        body.put("email", account.getEmail());
        body.put("phone", account.getPhone());
        body.put("levelId", account.getLevelId());
        body.put("domain", account.getDomain());
        body.put("admin", account.isAdminAccount());
        return body;
    }
}
