package com.mtalk.msocial.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record CreateSocialActivityLogRequest(
        @NotBlank @Size(max = 20) String logDate,
        @NotBlank @Size(max = 20) String logTime,
        @NotBlank @Size(max = 500) String logBody
) {
}
