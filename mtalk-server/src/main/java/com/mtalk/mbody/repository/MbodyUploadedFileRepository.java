package com.mtalk.mbody.repository;

import com.mtalk.mbody.domain.UploadedFile;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface MbodyUploadedFileRepository extends JpaRepository<UploadedFile, Long> {

    Optional<UploadedFile> findFirstByRelativePath(String relativePath);
}
