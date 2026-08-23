package com.mtalk.cnthudit;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.web.server.ResponseStatusException;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
import java.nio.charset.StandardCharsets;
import java.security.InvalidKeyException;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.security.SecureRandom;
import java.time.Instant;
import java.util.Base64;
import java.util.HexFormat;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class CnthuditEntryTicketService {

    public static final int TTL_SECONDS = 90;
    private static final String VERSION = "v1";
    private static final long USED_RETENTION_MS = 5 * 60 * 1000L;

    public record TicketPayload(String loginId, String displayName, Instant expiresAt) {
    }

    private final String secret;
    private final SecureRandom random = new SecureRandom();
    private final ConcurrentHashMap<String, Long> usedTickets = new ConcurrentHashMap<>();

    public CnthuditEntryTicketService(
            @Value("${hudit.cnt.entry-ticket-secret:}") String secret
    ) {
        this.secret = secret == null ? "" : secret.trim();
    }

    public String issue(String loginId, String displayName) {
        requireSecret();
        purgeUsed();
        String id = loginId == null ? "" : loginId.trim();
        if (id.isBlank()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "로그인 아이디가 필요합니다.");
        }
        String name = displayName == null || displayName.isBlank() ? id : sanitize(displayName);
        long expEpoch = Instant.now().plusSeconds(TTL_SECONDS).getEpochSecond();
        byte[] nonce = new byte[16];
        random.nextBytes(nonce);
        String jti = HexFormat.of().formatHex(nonce);
        String payload = VERSION + "|" + jti + "|" + sanitize(id) + "|" + name + "|" + expEpoch;
        return encodePayload(payload) + "." + sign(payload);
    }

    public TicketPayload consume(String ticket) {
        requireSecret();
        if (ticket == null || ticket.isBlank()) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "입장 토큰이 필요합니다.");
        }
        String trimmed = ticket.trim();
        Long previous = usedTickets.putIfAbsent(trimmed, Instant.now().toEpochMilli() + USED_RETENTION_MS);
        if (previous != null && previous > Instant.now().toEpochMilli()) {
            throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "입장 토큰이 유효하지 않습니다.");
        }
        try {
            int dot = trimmed.indexOf('.');
            if (dot <= 0 || dot == trimmed.length() - 1) {
                throw unauthorized();
            }
            String payload = decodePayload(trimmed.substring(0, dot));
            String signature = trimmed.substring(dot + 1);
            if (!MessageDigest.isEqual(
                    sign(payload).getBytes(StandardCharsets.US_ASCII),
                    signature.getBytes(StandardCharsets.US_ASCII)
            )) {
                throw unauthorized();
            }
            String[] parts = payload.split("\\|", -1);
            if (parts.length != 5 || !VERSION.equals(parts[0])) {
                throw unauthorized();
            }
            long expEpoch;
            try {
                expEpoch = Long.parseLong(parts[4]);
            } catch (NumberFormatException ex) {
                throw unauthorized();
            }
            Instant expiresAt = Instant.ofEpochSecond(expEpoch);
            if (expiresAt.isBefore(Instant.now())) {
                throw new ResponseStatusException(HttpStatus.UNAUTHORIZED, "입장 토큰이 만료되었습니다.");
            }
            String name = parts[3] == null || parts[3].isBlank() ? parts[2] : parts[3];
            return new TicketPayload(parts[2], name, expiresAt);
        } catch (ResponseStatusException ex) {
            usedTickets.remove(trimmed);
            throw ex;
        } catch (RuntimeException ex) {
            usedTickets.remove(trimmed);
            throw unauthorized();
        }
    }

    private void requireSecret() {
        if (secret.length() < 16) {
            throw new ResponseStatusException(HttpStatus.SERVICE_UNAVAILABLE, "입장 토큰 설정이 없습니다.");
        }
    }

    private String sign(String payload) {
        try {
            Mac mac = Mac.getInstance("HmacSHA256");
            mac.init(new SecretKeySpec(secret.getBytes(StandardCharsets.UTF_8), "HmacSHA256"));
            return encodePayloadBytes(mac.doFinal(payload.getBytes(StandardCharsets.UTF_8)));
        } catch (NoSuchAlgorithmException | InvalidKeyException ex) {
            throw new IllegalStateException("HMAC 계산에 실패했습니다.", ex);
        }
    }

    private String encodePayload(String payload) {
        return encodePayloadBytes(payload.getBytes(StandardCharsets.UTF_8));
    }

    private String encodePayloadBytes(byte[] bytes) {
        return Base64.getUrlEncoder().withoutPadding().encodeToString(bytes);
    }

    private String decodePayload(String encoded) {
        try {
            return new String(Base64.getUrlDecoder().decode(encoded), StandardCharsets.UTF_8);
        } catch (IllegalArgumentException ex) {
            throw unauthorized();
        }
    }

    private String sanitize(String value) {
        return value.trim().replace('|', '/').replace('\n', ' ').replace('\r', ' ');
    }

    private ResponseStatusException unauthorized() {
        return new ResponseStatusException(HttpStatus.UNAUTHORIZED, "입장 토큰이 유효하지 않습니다.");
    }

    private void purgeUsed() {
        long now = Instant.now().toEpochMilli();
        for (Map.Entry<String, Long> entry : usedTickets.entrySet()) {
            if (entry.getValue() <= now) {
                usedTickets.remove(entry.getKey(), entry.getValue());
            }
        }
    }
}
