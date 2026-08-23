package com.mtalk.cnthudit.dto;

import jakarta.validation.constraints.NotBlank;

public record CnthuditConsumeTicketRequest(
        @NotBlank String ticket
) {
}
