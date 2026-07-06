package com.mtalk.msocial.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record UpdateSocialProgressRequest(
        @Min(1) int dayCount,
        @NotBlank @Size(min = 32, max = 32) String progressString
) {
}
