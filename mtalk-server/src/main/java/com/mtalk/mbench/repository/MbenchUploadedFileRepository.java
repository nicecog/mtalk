package com.mtalk.mbench.repository;

import com.mtalk.mbench.domain.UploadedFile;
import org.springframework.data.jpa.repository.JpaRepository;

public interface MbenchUploadedFileRepository extends JpaRepository<UploadedFile, String> {
}
