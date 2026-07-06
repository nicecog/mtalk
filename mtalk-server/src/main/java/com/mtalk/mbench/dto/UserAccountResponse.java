package com.mtalk.mbench.dto;

import com.mtalk.mbench.domain.UserAccount;

import java.time.format.DateTimeFormatter;

public record UserAccountResponse(
        long seq,
        String id,
        int levelId,
        String domain,
        String name,
        String email,
        String phone,
        boolean admin,
        String createdAt
) {
    private static final DateTimeFormatter FORMAT = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm");

    public static UserAccountResponse from(UserAccount account) {
        String created = account.getCreatedAt() != null
                ? account.getCreatedAt().format(FORMAT)
                : "";
        return new UserAccountResponse(
                account.getSeq(),
                account.getLoginId(),
                account.getLevelId(),
                account.getDomain(),
                account.getName(),
                account.getEmail(),
                account.getPhone(),
                account.isAdmin(),
                created
        );
    }
}
