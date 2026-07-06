package com.mtalk.mbench.service;

import com.mtalk.mbench.domain.UserAccount;
import com.mtalk.mbench.dto.CreateAccountRequest;
import com.mtalk.mbench.dto.UpdateAccountRequest;
import com.mtalk.mbench.dto.UserAccountResponse;
import com.mtalk.mbench.repository.MbenchSubjectVideoRepository;
import com.mtalk.mbench.repository.MbenchUserAccountRepository;
import jakarta.servlet.http.HttpSession;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

import java.util.List;

@Service
public class AdminUserService {

    private final MbenchUserAccountRepository MbenchUserAccountRepository;
    private final MbenchSubjectVideoRepository MbenchSubjectVideoRepository;
    private final UserService userService;

    public AdminUserService(
            MbenchUserAccountRepository MbenchUserAccountRepository,
            MbenchSubjectVideoRepository MbenchSubjectVideoRepository,
            UserService userService
    ) {
        this.MbenchUserAccountRepository = MbenchUserAccountRepository;
        this.MbenchSubjectVideoRepository = MbenchSubjectVideoRepository;
        this.userService = userService;
    }

    public void requireAdmin(HttpSession session) {
        Boolean isAdmin = (Boolean) session.getAttribute(UserService.SESSION_IS_ADMIN);
        if (isAdmin == null || !isAdmin) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "admin login required");
        }
    }

    public List<UserAccountResponse> listSubjects(HttpSession session) {
        requireAdmin(session);
        return MbenchUserAccountRepository.findByAdminFalseOrderByLoginIdAsc().stream()
                .map(UserAccountResponse::from)
                .toList();
    }

    public UserAccountResponse getSubject(long seq, HttpSession session) {
        requireAdmin(session);
        UserAccount account = findSubjectOrThrow(seq);
        return UserAccountResponse.from(account);
    }

    public UserAccountResponse createSubject(CreateAccountRequest request, HttpSession session) {
        UserAccount saved = userService.createAccount(request, session);
        return UserAccountResponse.from(saved);
    }

    public UserAccountResponse updateSubject(long seq, UpdateAccountRequest request, HttpSession session) {
        requireAdmin(session);
        UserAccount account = findSubjectOrThrow(seq);

        if (request.id != null && !request.id.isBlank() && !request.id.equals(account.getLoginId())) {
            if (MbenchUserAccountRepository.existsByLoginId(request.id)) {
                throw new ResponseStatusException(HttpStatus.CONFLICT, "login id already exists");
            }
            account.setLoginId(request.id.trim());
        }
        if (request.password != null && !request.password.isBlank()) {
            account.setPassword(request.password);
        }
        if (request.levelId != null) {
            account.setLevelId(request.levelId);
        }
        if (request.domain != null && !request.domain.isBlank()) {
            account.setDomain(request.domain.trim());
        }
        if (request.name != null) {
            account.setName(request.name.trim());
        }
        if (request.email != null) {
            account.setEmail(request.email.trim());
        }
        if (request.phone != null) {
            account.setPhone(request.phone.trim());
        }

        return UserAccountResponse.from(MbenchUserAccountRepository.save(account));
    }

    @Transactional
    public void deleteSubject(long seq, HttpSession session) {
        requireAdmin(session);
        UserAccount account = findSubjectOrThrow(seq);
        MbenchSubjectVideoRepository.deleteByUserAccountSeq(account.getSeq());
        MbenchUserAccountRepository.delete(account);
    }

    private UserAccount findSubjectOrThrow(long seq) {
        UserAccount account = MbenchUserAccountRepository.findById(seq)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "user not found"));
        if (account.isAdmin()) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "cannot manage admin accounts here");
        }
        return account;
    }
}
