package com.mtalk.mbody.repository;

import com.mtalk.mbody.domain.UserAccount;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface MbodyUserAccountRepository extends JpaRepository<UserAccount, Long> {

    boolean existsByLoginId(String loginId);

    Optional<UserAccount> findByLoginId(String loginId);

    Optional<UserAccount> findByLoginIdAndSeqNot(String loginId, Long seq);
}
