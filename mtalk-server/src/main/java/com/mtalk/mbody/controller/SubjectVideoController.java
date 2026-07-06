package com.mtalk.mbody.controller;

import com.mtalk.mbody.dto.SubjectVideoRequest;
import com.mtalk.mbody.service.SubjectVideoService;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

@RestController
@RequestMapping("/mbody/api/subject-videos")
public class SubjectVideoController {

    private final SubjectVideoService subjectVideoService;

    public SubjectVideoController(SubjectVideoService subjectVideoService) {
        this.subjectVideoService = subjectVideoService;
    }

    @PostMapping
    public Map<String, Object> create(@Valid @RequestBody SubjectVideoRequest request) {
        return subjectVideoService.create(request);
    }
}
