package com.mtalk.mface.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record UpdateFaceProgressRequest(
        @Min(1) int dayCount,
        @NotBlank @Size(min = 16, max = 16) String itemString
) {
}
