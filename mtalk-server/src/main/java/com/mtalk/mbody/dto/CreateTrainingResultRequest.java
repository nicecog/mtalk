package com.mtalk.mbody.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record CreateTrainingResultRequest(
        @NotBlank @Size(max = 20) String recordedAt,
        @NotBlank @Size(max = 20) String resultType,
        @NotBlank @Size(max = 255) String trial,
        @NotBlank @Size(max = 255) String point,
        @Size(max = 500) String details
) {
}
