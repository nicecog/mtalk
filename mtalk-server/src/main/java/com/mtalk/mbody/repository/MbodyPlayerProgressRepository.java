package com.mtalk.mbody.repository;

import com.mtalk.mbody.domain.PlayerProgress;
import org.springframework.data.jpa.repository.JpaRepository;

public interface MbodyPlayerProgressRepository extends JpaRepository<PlayerProgress, Long> {
}
