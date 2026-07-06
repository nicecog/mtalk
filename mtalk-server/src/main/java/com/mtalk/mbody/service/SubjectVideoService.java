package com.mtalk.mbody.service;

import com.mtalk.mbody.domain.SubjectVideo;
import com.mtalk.mbody.domain.UserAccount;
import com.mtalk.mbody.dto.SubjectVideoRequest;
import com.mtalk.mbody.repository.MbodySubjectVideoRepository;
import org.springframework.stereotype.Service;

import java.util.LinkedHashMap;
import java.util.Map;

@Service
public class SubjectVideoService {

    private final MbodySubjectVideoRepository MbodySubjectVideoRepository;
    private final AuthService authService;

    public SubjectVideoService(MbodySubjectVideoRepository MbodySubjectVideoRepository, AuthService authService) {
        this.MbodySubjectVideoRepository = MbodySubjectVideoRepository;
        this.authService = authService;
    }

    public Map<String, Object> create(SubjectVideoRequest request) {
        UserAccount userAccount = authService.getUserBySeq(request.userAccountSeq());

        SubjectVideo subjectVideo = new SubjectVideo();
        subjectVideo.setUserAccount(userAccount);
        subjectVideo.setFileVideo(request.fileVideo());
        subjectVideo.setContentPoint(request.contentPoint());
        subjectVideo.setContentType(request.contentType());

        SubjectVideo saved = MbodySubjectVideoRepository.save(subjectVideo);
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("seq", saved.getSeq());
        body.put("userAccountSeq", userAccount.getSeq());
        body.put("fileVideo", saved.getFileVideo());
        body.put("contentPoint", saved.getContentPoint());
        body.put("contentType", saved.getContentType());
        return body;
    }
}
