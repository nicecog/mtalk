package com.mtalk.mbench.service;

import com.mtalk.mbench.domain.SubjectVideo;
import com.mtalk.mbench.domain.UploadedFile;
import com.mtalk.mbench.dto.SubjectVideoRequest;
import com.mtalk.mbench.repository.MbenchSubjectVideoRepository;
import com.mtalk.mbench.repository.MbenchUploadedFileRepository;
import com.mtalk.mbench.repository.MbenchUserAccountRepository;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;
import org.springframework.web.server.ResponseStatusException;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.UUID;

@Service
public class FileService {

    private final Path uploadRoot;
    private final MbenchUploadedFileRepository MbenchUploadedFileRepository;
    private final MbenchSubjectVideoRepository MbenchSubjectVideoRepository;
    private final MbenchUserAccountRepository MbenchUserAccountRepository;

    public FileService(
            Path uploadRoot,
            MbenchUploadedFileRepository MbenchUploadedFileRepository,
            MbenchSubjectVideoRepository MbenchSubjectVideoRepository,
            MbenchUserAccountRepository MbenchUserAccountRepository
    ) {
        this.uploadRoot = uploadRoot;
        this.MbenchUploadedFileRepository = MbenchUploadedFileRepository;
        this.MbenchSubjectVideoRepository = MbenchSubjectVideoRepository;
        this.MbenchUserAccountRepository = MbenchUserAccountRepository;
    }

    public String storeVideo(MultipartFile file) {
        if (file == null || file.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "file is required");
        }

        String fileKey = UUID.randomUUID().toString().replace("-", "");
        String originalName = file.getOriginalFilename() != null ? file.getOriginalFilename() : "video.mp4";
        Path target = uploadRoot.resolve(fileKey + ".mp4");

        try {
            Files.copy(file.getInputStream(), target);
        } catch (IOException e) {
            throw new ResponseStatusException(HttpStatus.INTERNAL_SERVER_ERROR, "failed to store file", e);
        }

        UploadedFile entity = new UploadedFile();
        entity.setFileKey(fileKey);
        entity.setOriginalName(originalName);
        entity.setStoredPath(target.toString());
        entity.setMimeType(file.getContentType() != null ? file.getContentType() : "video/mp4");
        entity.setSizeBytes(file.getSize());
        MbenchUploadedFileRepository.save(entity);
        return fileKey;
    }

    public SubjectVideo registerSubjectVideo(SubjectVideoRequest request) {
        if (!MbenchUserAccountRepository.existsById(request.userAccountSeq)) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "unknown userAccountSeq");
        }
        if (!MbenchUploadedFileRepository.existsById(request.fileVideo)) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "unknown fileVideo");
        }

        SubjectVideo video = new SubjectVideo();
        video.setUserAccountSeq(request.userAccountSeq);
        video.setFileVideo(request.fileVideo);
        video.setContentPoint(request.contentPoint);
        video.setContentType(request.contentType != null ? request.contentType : "post");
        return MbenchSubjectVideoRepository.save(video);
    }
}
