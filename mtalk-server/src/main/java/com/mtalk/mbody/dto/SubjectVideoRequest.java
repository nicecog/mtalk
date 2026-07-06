package com.mtalk.mbody.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

public record SubjectVideoRequest(
        @NotNull Long userAccountSeq,
        @NotBlank String fileVideo,
        @NotNull Integer contentPoint,
        @NotBlank String contentType
) {
}
