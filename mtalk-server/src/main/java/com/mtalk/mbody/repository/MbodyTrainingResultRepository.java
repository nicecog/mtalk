package com.mtalk.mbody.repository;

import com.mtalk.mbody.domain.TrainingResult;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface MbodyTrainingResultRepository extends JpaRepository<TrainingResult, Long> {

    List<TrainingResult> findTop100ByUserAccountSeqOrderBySeqDesc(Long userAccountSeq);
}
