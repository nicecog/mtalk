package com.mtalk.mbench.repository;

import com.mtalk.mbench.domain.UserAccount;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface MbenchUserAccountRepository extends JpaRepository<UserAccount, Long> {
    Optional<UserAccount> findByLoginId(String loginId);
    boolean existsByLoginId(String loginId);
    List<UserAccount> findByAdminFalseOrderByLoginIdAsc();
}
