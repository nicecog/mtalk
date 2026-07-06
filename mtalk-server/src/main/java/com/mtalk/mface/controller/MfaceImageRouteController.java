package com.mtalk.mface.controller;

import com.mtalk.mbody.service.MfaceImageService;
import org.springframework.core.io.Resource;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;
import org.springframework.web.multipart.MultipartHttpServletRequest;
import org.springframework.web.server.ResponseStatusException;

import java.util.List;
import java.util.Map;

import static org.springframework.http.HttpStatus.BAD_REQUEST;

@RestController
@RequestMapping("/mface/api/mface-images")
public class MfaceImageRouteController {

    private final MfaceImageService mfaceImageService;

    public MfaceImageRouteController(MfaceImageService mfaceImageService) {
        this.mfaceImageService = mfaceImageService;
    }

    @GetMapping
    public List<Map<String, Object>> list(@RequestParam("account") Long account) {
        return mfaceImageService.listByAccount(account);
    }

    @GetMapping("/{key}")
    public ResponseEntity<Resource> download(@PathVariable String key) {
        Resource resource = mfaceImageService.loadByKey(key);
        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=\"" + key + "\"")
                .contentType(MediaType.APPLICATION_OCTET_STREAM)
                .body(resource);
    }

    @PostMapping(consumes = MediaType.MULTIPART_FORM_DATA_VALUE)
    public Map<String, Object> upload(@RequestParam("user") Long userAccountSeq,
                                      MultipartHttpServletRequest request) {
        MultipartFile file = request.getFileMap().values().stream()
                .filter(part -> part != null && !part.isEmpty())
                .findFirst()
                .orElseThrow(() -> new ResponseStatusException(BAD_REQUEST, "file is required"));

        return mfaceImageService.store(userAccountSeq, file);
    }

    @DeleteMapping("/{seq}")
    public void delete(@PathVariable Long seq) {
        mfaceImageService.delete(seq);
    }
}
