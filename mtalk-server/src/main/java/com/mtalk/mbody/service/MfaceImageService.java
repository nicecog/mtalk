package com.mtalk.mbody.service;

import com.mtalk.mbody.domain.MfaceImage;
import com.mtalk.mbody.repository.MbodyMfaceImageRepository;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.io.Resource;
import org.springframework.core.io.UrlResource;
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
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.UUID;

import static org.springframework.http.HttpStatus.BAD_REQUEST;
import static org.springframework.http.HttpStatus.INTERNAL_SERVER_ERROR;
import static org.springframework.http.HttpStatus.NOT_FOUND;

@Service
public class MfaceImageService {

    private final MbodyMfaceImageRepository MbodyMfaceImageRepository;
    private final AuthService authService;
    private final Path rootUploadDirectory;

    public MfaceImageService(MbodyMfaceImageRepository MbodyMfaceImageRepository,
                             AuthService authService,
                             @Value("${app.upload-dir}") String uploadDir) {
        this.MbodyMfaceImageRepository = MbodyMfaceImageRepository;
        this.authService = authService;
        this.rootUploadDirectory = Paths.get(uploadDir).toAbsolutePath().normalize();
    }

    public List<Map<String, Object>> listByAccount(Long accountSeq) {
        authService.getUserBySeq(accountSeq);
        return MbodyMfaceImageRepository.findByUserAccountSeqOrderBySeqAsc(accountSeq).stream()
                .map(this::toListItem)
                .toList();
    }

    public Resource loadByKey(String key) {
        MfaceImage image = MbodyMfaceImageRepository.findByFileKey(key)
                .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "File not found"));

        try {
            Path file = Paths.get(System.getProperty("user.dir")).toAbsolutePath().normalize()
                    .resolve(image.getRelativePath()).normalize();
            Resource resource = new UrlResource(file.toUri());
            if (!resource.exists()) {
                throw new ResponseStatusException(NOT_FOUND, "File not found on disk");
            }
            return resource;
        } catch (IOException ex) {
            throw new ResponseStatusException(INTERNAL_SERVER_ERROR, "Failed to read file", ex);
        }
    }

    public Map<String, Object> store(Long userAccountSeq, MultipartFile file) {
        if (userAccountSeq == null || userAccountSeq <= 0) {
            throw new ResponseStatusException(BAD_REQUEST, "user is required");
        }
        if (file == null || file.isEmpty()) {
            throw new ResponseStatusException(BAD_REQUEST, "file is required");
        }

        authService.getUserBySeq(userAccountSeq);

        String originalName = StringUtils.cleanPath(
                file.getOriginalFilename() == null ? "upload.bin" : file.getOriginalFilename());
        String storedName = UUID.randomUUID() + "-" + originalName;
        LocalDate today = LocalDate.now();
        Path targetDirectory = rootUploadDirectory
                .resolve("mface")
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

            String fileKey = UUID.randomUUID().toString().replace("-", "");

            MfaceImage image = new MfaceImage();
            image.setUserAccountSeq(userAccountSeq);
            image.setFileKey(fileKey);
            image.setFileName(originalName);
            image.setRelativePath(relativePath);
            image.setContentType(file.getContentType() == null ? "application/octet-stream" : file.getContentType());
            image.setSize(file.getSize());
            image = MbodyMfaceImageRepository.save(image);

            Map<String, Object> body = new LinkedHashMap<>();
            body.put("seq", image.getSeq());
            body.put("user_account_seq", image.getUserAccountSeq());
            body.put("key", image.getFileKey());
            body.put("name", image.getFileName());
            body.put("type", image.getContentType());
            body.put("size", image.getSize());
            body.put("inserted", image.getCreatedAt().toString());

            if (isImage(originalName, image.getContentType())) {
                body.put("emotion", buildStubEmotion(originalName));
            }

            return body;
        } catch (IOException ex) {
            throw new ResponseStatusException(INTERNAL_SERVER_ERROR, "Failed to store file", ex);
        }
    }

    public void delete(Long seq) {
        MfaceImage image = MbodyMfaceImageRepository.findById(seq)
                .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "File not found"));

        try {
            Path file = Paths.get(System.getProperty("user.dir")).toAbsolutePath().normalize()
                    .resolve(image.getRelativePath()).normalize();
            Files.deleteIfExists(file);
        } catch (IOException ignored) {
            // best effort
        }

        MbodyMfaceImageRepository.delete(image);
    }

    private Map<String, Object> toListItem(MfaceImage image) {
        Map<String, Object> item = new LinkedHashMap<>();
        item.put("seq", image.getSeq());
        item.put("user_account_seq", image.getUserAccountSeq());
        item.put("key", image.getFileKey());
        item.put("name", image.getFileName());
        item.put("type", image.getContentType());
        item.put("size", image.getSize());
        return item;
    }

    private static boolean isImage(String name, String contentType) {
        String lower = name.toLowerCase(Locale.ROOT);
        return lower.endsWith(".jpg") || lower.endsWith(".jpeg")
                || (contentType != null && contentType.startsWith("image/"));
    }

    private static String buildStubEmotion(String fileName) {
        String lower = fileName.toLowerCase(Locale.ROOT);
        float happy = 0.05f;
        float neutral = 0.05f;
        float sad = 0.05f;
        float anger = 0.05f;

        if (lower.contains("happy")) {
            happy = 0.82f;
        } else if (lower.contains("neutral")) {
            neutral = 0.82f;
        } else if (lower.contains("sad")) {
            sad = 0.82f;
        } else if (lower.contains("anger")) {
            anger = 0.82f;
        } else {
            neutral = 0.55f;
            happy = 0.20f;
        }

        return String.format(Locale.US,
                "Neutral:%.2f,Happy:%.2f,Sad:%.2f,Surprise:0.02,Fear:0.02,Disgust:0.02,Anger:%.2f",
                neutral, happy, sad, anger);
    }
}
