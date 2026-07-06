package com.mtalk.mface.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record CreateFaceActivityLogRequest(
        @NotBlank @Size(max = 20) String logDate,
        @NotBlank @Size(max = 20) String logTime,
        @NotBlank @Size(max = 500) String logBody
) {
}
