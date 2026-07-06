package com.mtalk.mbody.service;

import com.mtalk.mbody.domain.SubjectVideo;
import com.mtalk.mbody.domain.UploadedFile;
import com.mtalk.mbody.repository.MbodySubjectVideoRepository;
import com.mtalk.mbody.repository.MbodyUploadedFileRepository;
import com.mtalk.mbody.repository.MbodyUserAccountRepository;
import com.mtalk.portal.service.PortalAuthService;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpSession;
import org.springframework.core.io.Resource;
import org.springframework.core.io.UrlResource;
import org.springframework.core.io.support.ResourceRegion;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpRange;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.format.DateTimeFormatter;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;
import static org.springframework.http.HttpStatus.INTERNAL_SERVER_ERROR;
import static org.springframework.http.HttpStatus.NOT_FOUND;

@Service
public class MbodySubjectVideoAdminService {

    private static final DateTimeFormatter FORMAT = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm");

    private final PortalAuthService portalAuthService;
    private final MbodySubjectVideoRepository subjectVideoRepository;
    private final MbodyUploadedFileRepository uploadedFileRepository;
    private final MbodyUserAccountRepository userAccountRepository;
    private final Path workingDirectory;

    public MbodySubjectVideoAdminService(
            PortalAuthService portalAuthService,
            MbodySubjectVideoRepository subjectVideoRepository,
            MbodyUploadedFileRepository uploadedFileRepository,
            MbodyUserAccountRepository userAccountRepository
    ) {
        this.portalAuthService = portalAuthService;
        this.subjectVideoRepository = subjectVideoRepository;
        this.uploadedFileRepository = uploadedFileRepository;
        this.userAccountRepository = userAccountRepository;
        this.workingDirectory = Paths.get(System.getProperty("user.dir")).toAbsolutePath().normalize();
    }

    @Transactional(readOnly = true, transactionManager = "mbodyTransactionManager")
    public Map<String, Object> summary(HttpSession session) {
        portalAuthService.requirePortalAdmin(session);

        List<SubjectVideo> all = subjectVideoRepository.findAllByOrderByCreatedAtDesc();
        Map<Long, Long> byUser = new LinkedHashMap<>();
        Map<String, Long> byType = new LinkedHashMap<>();
        long totalBytes = 0;
        long missingFiles = 0;

        for (SubjectVideo video : all) {
            byUser.merge(video.getUserAccount().getSeq(), 1L, Long::sum);
            byType.merge(video.getContentType(), 1L, Long::sum);
            Path file = resolveVideoPath(video.getFileVideo());
            if (Files.exists(file)) {
                try {
                    totalBytes += Files.size(file);
                } catch (IOException ignored) {
                    missingFiles++;
                }
            } else {
                missingFiles++;
            }
        }

        Map<String, Object> body = new LinkedHashMap<>();
        body.put("totalCount", all.size());
        body.put("totalBytes", totalBytes);
        body.put("missingFiles", missingFiles);
        body.put("byUser", byUser);
        body.put("byType", byType.entrySet().stream()
                .sorted(Map.Entry.<String, Long>comparingByValue(Comparator.reverseOrder()))
                .collect(Collectors.toMap(
                        Map.Entry::getKey,
                        Map.Entry::getValue,
                        (a, b) -> a,
                        LinkedHashMap::new
                )));
        return body;
    }

    @Transactional(readOnly = true, transactionManager = "mbodyTransactionManager")
    public List<Map<String, Object>> listByUser(long userSeq, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        userAccountRepository.findById(userSeq)
                .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "user not found"));

        return subjectVideoRepository.findByUserAccount_SeqOrderByCreatedAtDesc(userSeq).stream()
                .map(this::toListItem)
                .toList();
    }

    public ResponseEntity<ResourceRegion> streamVideo(long seq, HttpSession session, HttpServletRequest request) {
        portalAuthService.requirePortalAdmin(session);
        SubjectVideo video = subjectVideoRepository.findById(seq)
                .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "video not found"));

        Path file = resolveVideoPath(video.getFileVideo());
        if (!Files.exists(file)) {
            throw new ResponseStatusException(NOT_FOUND, "video file not found on disk");
        }

        try {
            Resource resource = new UrlResource(file.toUri());
            long contentLength = resource.contentLength();
            ResourceRegion region = toResourceRegion(request, resource, contentLength);
            String filename = file.getFileName().toString();
            String contentType = uploadedFileRepository.findFirstByRelativePath(video.getFileVideo())
                    .map(UploadedFile::getContentType)
                    .filter(type -> !type.isBlank())
                    .orElse(MediaType.APPLICATION_OCTET_STREAM_VALUE);
            HttpStatus status = region.getCount() < contentLength
                    ? HttpStatus.PARTIAL_CONTENT
                    : HttpStatus.OK;

            return ResponseEntity.status(status)
                    .header(HttpHeaders.ACCEPT_RANGES, "bytes")
                    .header(HttpHeaders.CONTENT_DISPOSITION, "inline; filename=\"" + filename + "\"")
                    .contentType(MediaType.parseMediaType(contentType))
                    .body(region);
        } catch (IOException ex) {
            throw new ResponseStatusException(
                    INTERNAL_SERVER_ERROR,
                    "Failed to read video file",
                    ex
            );
        }
    }

    private ResourceRegion toResourceRegion(HttpServletRequest request, Resource resource, long contentLength)
            throws IOException {
        List<HttpRange> ranges = HttpRange.parseRanges(request.getHeader(HttpHeaders.RANGE));
        if (ranges.isEmpty()) {
            return new ResourceRegion(resource, 0, contentLength);
        }

        HttpRange range = ranges.get(0);
        long start = range.getRangeStart(contentLength);
        long end = range.getRangeEnd(contentLength);
        long rangeLength = Math.min(end - start + 1, contentLength - start);
        return new ResourceRegion(resource, start, rangeLength);
    }

    public ResponseEntity<Resource> downloadVideo(long seq, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        SubjectVideo video = subjectVideoRepository.findById(seq)
                .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "video not found"));

        Path file = resolveVideoPath(video.getFileVideo());
        if (!Files.exists(file)) {
            throw new ResponseStatusException(NOT_FOUND, "video file not found on disk");
        }

        try {
            Resource resource = new UrlResource(file.toUri());
            String filename = uploadedFileRepository.findFirstByRelativePath(video.getFileVideo())
                    .map(UploadedFile::getOriginalName)
                    .filter(name -> name != null && !name.isBlank())
                    .orElseGet(() -> file.getFileName().toString());
            String contentType = uploadedFileRepository.findFirstByRelativePath(video.getFileVideo())
                    .map(UploadedFile::getContentType)
                    .filter(type -> !type.isBlank())
                    .orElse(MediaType.APPLICATION_OCTET_STREAM_VALUE);

            return ResponseEntity.ok()
                    .header(HttpHeaders.CONTENT_DISPOSITION, contentDispositionAttachment(filename))
                    .contentType(MediaType.parseMediaType(contentType))
                    .contentLength(resource.contentLength())
                    .body(resource);
        } catch (IOException ex) {
            throw new ResponseStatusException(
                    INTERNAL_SERVER_ERROR,
                    "Failed to download video file",
                    ex
            );
        }
    }

    private String contentDispositionAttachment(String filename) {
        String safe = filename.replace("\"", "'").replace("\r", "").replace("\n", "");
        return "attachment; filename=\"" + safe + "\"";
    }

    @Transactional("mbodyTransactionManager")
    public Map<String, Object> deleteVideo(long seq, boolean deleteFile, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        SubjectVideo video = subjectVideoRepository.findById(seq)
                .orElseThrow(() -> new ResponseStatusException(NOT_FOUND, "video not found"));

        String fileVideo = video.getFileVideo();
        boolean fileDeleted = false;
        if (deleteFile) {
            Path file = resolveVideoPath(fileVideo);
            if (Files.exists(file)) {
                try {
                    Files.delete(file);
                    fileDeleted = true;
                } catch (IOException ex) {
                    throw new ResponseStatusException(
                            INTERNAL_SERVER_ERROR,
                            "Failed to delete video file",
                            ex
                    );
                }
            }
            uploadedFileRepository.findFirstByRelativePath(fileVideo)
                    .ifPresent(uploadedFileRepository::delete);
        }

        subjectVideoRepository.delete(video);

        Map<String, Object> body = new LinkedHashMap<>();
        body.put("deleted", true);
        body.put("seq", seq);
        body.put("fileDeleted", fileDeleted);
        return body;
    }

    private Map<String, Object> toListItem(SubjectVideo video) {
        String fileVideo = video.getFileVideo();
        Path file = resolveVideoPath(fileVideo);
        boolean fileExists = Files.isRegularFile(file);

        UploadedFile uploaded = uploadedFileRepository.findFirstByRelativePath(fileVideo).orElse(null);
        String originalName = uploaded != null ? uploaded.getOriginalName() : extractFileName(fileVideo);
        Long fileSize = uploaded != null ? uploaded.getSize() : null;
        if (fileSize == null && fileExists) {
            try {
                fileSize = Files.size(file);
            } catch (IOException ignored) {
                fileSize = null;
            }
        }

        Map<String, Object> item = new LinkedHashMap<>();
        item.put("seq", video.getSeq());
        item.put("userAccountSeq", video.getUserAccount().getSeq());
        item.put("userLoginId", video.getUserAccount().getLoginId());
        item.put("contentType", video.getContentType());
        item.put("contentPoint", video.getContentPoint());
        item.put("fileVideo", fileVideo);
        item.put("originalName", originalName);
        item.put("fileSize", fileSize != null ? fileSize : 0);
        item.put("fileExists", fileExists);
        item.put("createdAt", video.getCreatedAt() != null ? FORMAT.format(video.getCreatedAt()) : "-");
        return item;
    }

    private Path resolveVideoPath(String fileVideo) {
        if (fileVideo == null || fileVideo.isBlank()) {
            return workingDirectory.resolve("missing");
        }
        Path relative = Paths.get(fileVideo.replace('\\', '/'));
        if (relative.isAbsolute()) {
            return relative.normalize();
        }
        return workingDirectory.resolve(relative).normalize();
    }

    private String extractFileName(String fileVideo) {
        if (fileVideo == null || fileVideo.isBlank()) {
            return "-";
        }
        String normalized = fileVideo.replace('\\', '/');
        int slash = normalized.lastIndexOf('/');
        return slash >= 0 ? normalized.substring(slash + 1) : normalized;
    }
}
