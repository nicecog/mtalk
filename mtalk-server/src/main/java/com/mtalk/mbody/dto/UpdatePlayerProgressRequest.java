package com.mtalk.mbody.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record UpdatePlayerProgressRequest(
        @Min(1) @Max(9999) Integer playTimes,
        @Min(0) @Max(9999) Integer bodyPlayTimes,
        @NotBlank @Size(min = 6, max = 20) String danceStar
) {
}
