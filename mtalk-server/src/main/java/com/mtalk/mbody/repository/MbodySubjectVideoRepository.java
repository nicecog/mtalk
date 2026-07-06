package com.mtalk.mbody.repository;

import com.mtalk.mbody.domain.SubjectVideo;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;

public interface MbodySubjectVideoRepository extends JpaRepository<SubjectVideo, Long> {

    @Query("""
            SELECT sv FROM SubjectVideo sv
            JOIN FETCH sv.userAccount
            WHERE sv.userAccount.seq = :userAccountSeq
            ORDER BY sv.createdAt DESC
            """)
    List<SubjectVideo> findByUserAccount_SeqOrderByCreatedAtDesc(@Param("userAccountSeq") Long userAccountSeq);

    @Query("""
            SELECT sv FROM SubjectVideo sv
            JOIN FETCH sv.userAccount
            ORDER BY sv.createdAt DESC
            """)
    List<SubjectVideo> findAllByOrderByCreatedAtDesc();

    @Query("""
            SELECT sv.userAccount.seq, COUNT(sv)
            FROM SubjectVideo sv
            GROUP BY sv.userAccount.seq
            """)
    List<Object[]> countGroupedByUserAccount();
}
