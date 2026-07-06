package com.mtalk.mbody.controller;

import com.mtalk.mbody.service.FileStorageService;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

import java.util.Map;

@RestController
@RequestMapping("/mbody/api/files")
public class MbodyFileController {

    private final FileStorageService fileStorageService;

    public MbodyFileController(FileStorageService fileStorageService) {
        this.fileStorageService = fileStorageService;
    }

    @PostMapping("/attach")
    public Map<String, String> attach(@RequestParam("file") MultipartFile file) {
        return fileStorageService.store(file);
    }
}
