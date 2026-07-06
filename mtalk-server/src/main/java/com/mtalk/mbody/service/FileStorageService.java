package com.mtalk.mbody.service;

import com.mtalk.mbody.domain.UploadedFile;
import com.mtalk.mbody.repository.MbodyUploadedFileRepository;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;
import org.springframework.web.multipart.MultipartFile;
import org.springframework.web.server.ResponseStatusException;

import java.io.IOException;
import java.io.InputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.time.LocalDate;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;

import static org.springframework.http.HttpStatus.BAD_REQUEST;
import static org.springframework.http.HttpStatus.INTERNAL_SERVER_ERROR;

@Service
public class FileStorageService {

    private final MbodyUploadedFileRepository MbodyUploadedFileRepository;
    private final Path rootUploadDirectory;

    public FileStorageService(MbodyUploadedFileRepository MbodyUploadedFileRepository,
                              @Value("${app.upload-dir}") String uploadDir) {
        this.MbodyUploadedFileRepository = MbodyUploadedFileRepository;
        this.rootUploadDirectory = Paths.get(uploadDir).toAbsolutePath().normalize();
    }

    public Map<String, String> store(MultipartFile file) {
        if (file == null || file.isEmpty()) {
            throw new ResponseStatusException(BAD_REQUEST, "file is required");
        }

        String originalName = StringUtils.cleanPath(file.getOriginalFilename() == null ? "upload.bin" : file.getOriginalFilename());
        String storedName = UUID.randomUUID() + "-" + originalName;
        LocalDate today = LocalDate.now();
        Path targetDirectory = rootUploadDirectory
                .resolve(String.valueOf(today.getYear()))
                .resolve(String.format("%02d", today.getMonthValue()))
                .resolve(String.format("%02d", today.getDayOfMonth()));

        try {
            Files.createDirectories(targetDirectory);
            Path targetFile = targetDirectory.resolve(storedName);
            try (InputStream inputStream = file.getInputStream()) {
                Files.copy(inputStream, targetFile, StandardCopyOption.REPLACE_EXISTING);
            }

            Path userDir = Paths.get(System.getProperty("user.dir")).toAbsolutePath().normalize();
            String relativePath = userDir.relativize(targetFile).toString().replace('\\', '/');

            UploadedFile uploadedFile = new UploadedFile();
            uploadedFile.setOriginalName(originalName);
            uploadedFile.setStoredName(storedName);
            uploadedFile.setRelativePath(relativePath);
            uploadedFile.setContentType(file.getContentType() == null ? "application/octet-stream" : file.getContentType());
            uploadedFile.setSize(file.getSize());
            MbodyUploadedFileRepository.save(uploadedFile);

            Map<String, String> body = new LinkedHashMap<>();
            body.put("fileVideo", relativePath);
            return body;
        } catch (IOException ex) {
            throw new ResponseStatusException(INTERNAL_SERVER_ERROR, "Failed to store file", ex);
        }
    }
}
