package com.mtalk.mbench.web;

import com.mtalk.mbench.domain.SubjectVideo;
import com.mtalk.mbench.dto.ApiResponses;
import com.mtalk.mbench.dto.SubjectVideoRequest;
import com.mtalk.mbench.service.FileService;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

import java.util.LinkedHashMap;
import java.util.Map;

@RestController
@RequestMapping("/mbench/api")
public class MbenchFileController {

    private final FileService fileService;

    public MbenchFileController(FileService fileService) {
        this.fileService = fileService;
    }

    /** Unity: POST /api/files/attach multipart field "file" */
    @PostMapping(value = "/files/attach", consumes = MediaType.MULTIPART_FORM_DATA_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> attach(@RequestParam("file") MultipartFile file) {
        String fileKey = fileService.storeVideo(file);
        return ResponseEntity.ok(ApiResponses.fileAttached(fileKey));
    }

    /** Unity: POST /api/subject-videos */
    @PostMapping(value = "/subject-videos", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<Map<String, Object>> registerSubjectVideo(@RequestBody SubjectVideoRequest request) {
        SubjectVideo saved = fileService.registerSubjectVideo(request);
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("success", true);
        body.put("id", saved.getId());
        body.put("userAccountSeq", saved.getUserAccountSeq());
        body.put("fileVideo", saved.getFileVideo());
        body.put("contentType", saved.getContentType());
        return ResponseEntity.ok(body);
    }
}
