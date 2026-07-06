package com.mtalk.portal.service;

import jakarta.servlet.http.HttpSession;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestTemplate;
import org.springframework.web.server.ResponseStatusException;
import org.springframework.web.util.UriComponentsBuilder;

import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.Locale;
import java.util.Set;

@Service
public class PortalCntPdfProxyService {

    public static final String ASSET_PROXY_PREFIX = "/api/hudit/cnt/pdf-assets";

    private static final int MIN_HTML_LENGTH = 200;
    private static final Set<String> ALLOWED_ASSET_PREFIXES = Set.of(
            "/css/",
            "/images/",
            "/js/",
            "/assets/",
            "/static/"
    );
    // 자동 다운로드는 비활성화 (수동 Download PDF 버튼 사용)
    /*
    private static final String AUTO_DOWNLOAD_SCRIPT = """
            <script>
            (function () {
                function triggerDownload() {
                    var btn = document.getElementById('downloadBtn');
                    if (!btn) {
                        return;
                    }
                    btn.click();
                }
                window.addEventListener('load', function () {
                    setTimeout(triggerDownload, 4500);
                });
            })();
            </script>
            """;
    */

    private final PortalAuthService portalAuthService;
    private final RestTemplate cntPdfRestTemplate;
    private final String legacyBaseUrl;
    private final String pdfTemplateSuffix;

    public PortalCntPdfProxyService(
            PortalAuthService portalAuthService,
            @Value("${hudit.cnt.legacy-base-url:http://cnt.re.kr}") String legacyBaseUrl,
            @Value("${hudit.cnt.pdf-template-suffix:pdf4th}") String pdfTemplateSuffix,
            @Value("${hudit.cnt.pdf-fetch-timeout-ms:45000}") long pdfFetchTimeoutMs
    ) {
        this.portalAuthService = portalAuthService;
        this.legacyBaseUrl = trimTrailingSlash(legacyBaseUrl);
        this.pdfTemplateSuffix = normalizeTemplateSuffix(pdfTemplateSuffix);
        var factory = new org.springframework.http.client.SimpleClientHttpRequestFactory();
        factory.setConnectTimeout((int) Duration.ofMillis(pdfFetchTimeoutMs).toMillis());
        factory.setReadTimeout((int) Duration.ofMillis(pdfFetchTimeoutMs).toMillis());
        this.cntPdfRestTemplate = new RestTemplate(factory);
    }

    public ResponseEntity<byte[]> proxyMemberPdf(String userId, boolean autoDownload, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        if (userId == null || userId.isBlank()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "userId가 필요합니다.");
        }

        String trimmedUserId = userId.trim();
        String sourceUrl = UriComponentsBuilder.fromUriString(legacyBaseUrl)
                .path("/cnt/admin/members/{userId}/")
                .path(pdfTemplateSuffix)
                .buildAndExpand(trimmedUserId)
                .toUriString();
        String html = fetchMemberPdfHtml(sourceUrl);
        html = rewriteHtmlForProxy(html, trimmedUserId, autoDownload);

        byte[] body = html.getBytes(StandardCharsets.UTF_8);
        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(new MediaType("text", "html", StandardCharsets.UTF_8));
        headers.set(HttpHeaders.CONTENT_DISPOSITION, "inline; filename=\"CNT-" + safeFilename(trimmedUserId) + ".html\"");
        return new ResponseEntity<>(body, headers, HttpStatus.OK);
    }

    public ResponseEntity<byte[]> proxyPdfAsset(String assetPath, HttpSession session) {
        portalAuthService.requirePortalAdmin(session);
        String normalizedPath = normalizeAssetPath(assetPath);
        String sourceUrl = legacyBaseUrl + normalizedPath;
        try {
            ResponseEntity<byte[]> response = cntPdfRestTemplate.exchange(
                    sourceUrl,
                    HttpMethod.GET,
                    new HttpEntity<>(new HttpHeaders()),
                    byte[].class
            );
            if (!response.getStatusCode().is2xxSuccessful() || response.getBody() == null) {
                throw new ResponseStatusException(
                        HttpStatus.BAD_GATEWAY,
                        "CNT PDF 리소스 조회 실패 (HTTP " + response.getStatusCode().value() + ")"
                );
            }

            byte[] body = response.getBody();
            MediaType contentType = resolveContentType(normalizedPath, response.getHeaders().getContentType());
            if (isTextAsset(normalizedPath)) {
                String text = new String(body, StandardCharsets.UTF_8);
                text = rewriteTextAsset(text, normalizedPath);
                body = text.getBytes(StandardCharsets.UTF_8);
            }

            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(contentType);
            headers.setCacheControl("private, max-age=3600");
            return new ResponseEntity<>(body, headers, HttpStatus.OK);
        } catch (RestClientException ex) {
            throw new ResponseStatusException(
                    HttpStatus.BAD_GATEWAY,
                    "CNT PDF 리소스를 가져오지 못했습니다: " + ex.getMessage(),
                    ex
            );
        }
    }

    private String fetchMemberPdfHtml(String sourceUrl) {
        HttpHeaders headers = new HttpHeaders();
        headers.set(HttpHeaders.ACCEPT, "text/html;charset=UTF-8");
        HttpEntity<Void> entity = new HttpEntity<>(headers);
        try {
            ResponseEntity<String> response = cntPdfRestTemplate.exchange(
                    sourceUrl,
                    HttpMethod.GET,
                    entity,
                    String.class
            );
            if (!response.getStatusCode().is2xxSuccessful()) {
                throw new ResponseStatusException(
                        HttpStatus.BAD_GATEWAY,
                        "CNT PDF 화면 조회 실패 (HTTP " + response.getStatusCode().value() + ")"
                );
            }
            String html = response.getBody() != null ? response.getBody() : "";
            if (html.trim().length() < MIN_HTML_LENGTH) {
                throw new ResponseStatusException(HttpStatus.BAD_GATEWAY, "CNT PDF HTML 응답이 비어 있습니다.");
            }
            return html;
        } catch (RestClientException ex) {
            throw new ResponseStatusException(
                    HttpStatus.BAD_GATEWAY,
                    "CNT PDF 화면을 가져오지 못했습니다: " + ex.getMessage(),
                    ex
            );
        }
    }

    private String rewriteHtmlForProxy(String html, String userId, boolean autoDownload) {
        String proxy = ASSET_PROXY_PREFIX;
        String rewritten = html
                .replace("href=\"/", "href=\"" + proxy + "/")
                .replace("src=\"/", "src=\"" + proxy + "/")
                .replace("url(/", "url(" + proxy + "/")
                .replace("filename: 'merged-file.pdf'", "filename: 'CNT-" + safeFilename(userId) + ".pdf'")
                .replace("filename: \"merged-file.pdf\"", "filename: \"CNT-" + safeFilename(userId) + ".pdf\"");

        if (autoDownload && !rewritten.contains("hudit-auto-download")) {
            // 자동 다운로드 비활성화: 아래 스크립트 주입을 사용하지 않음
            // rewritten = rewritten.replace("</body>", AUTO_DOWNLOAD_SCRIPT + "\n</body>");
        }
        return rewritten;
    }

    private String rewriteTextAsset(String text, String assetPath) {
        if (!assetPath.endsWith(".css")) {
            return text;
        }
        return text
                .replace("url(../", "url(" + ASSET_PROXY_PREFIX + "/")
                .replace("url('../", "url('" + ASSET_PROXY_PREFIX + "/")
                .replace("url(\"../", "url(\"" + ASSET_PROXY_PREFIX + "/");
    }

    private String normalizeAssetPath(String assetPath) {
        if (assetPath == null || assetPath.isBlank()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "리소스 경로가 필요합니다.");
        }
        String normalized = assetPath.trim();
        if (!normalized.startsWith("/")) {
            normalized = "/" + normalized;
        }
        if (normalized.contains("..") || normalized.contains("\\")) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "허용되지 않은 리소스 경로입니다.");
        }
        boolean allowed = ALLOWED_ASSET_PREFIXES.stream().anyMatch(normalized::startsWith);
        if (!allowed) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "허용되지 않은 리소스 경로입니다.");
        }
        return normalized;
    }

    private MediaType resolveContentType(String assetPath, MediaType upstreamType) {
        if (upstreamType != null && upstreamType.getType() != null) {
            return upstreamType;
        }
        String lower = assetPath.toLowerCase(Locale.ROOT);
        if (lower.endsWith(".css")) {
            return new MediaType("text", "css", StandardCharsets.UTF_8);
        }
        if (lower.endsWith(".js")) {
            return MediaType.valueOf("application/javascript");
        }
        if (lower.endsWith(".png")) {
            return MediaType.IMAGE_PNG;
        }
        if (lower.endsWith(".jpg") || lower.endsWith(".jpeg")) {
            return MediaType.IMAGE_JPEG;
        }
        if (lower.endsWith(".svg")) {
            return MediaType.valueOf("image/svg+xml");
        }
        if (lower.endsWith(".woff2")) {
            return MediaType.valueOf("font/woff2");
        }
        if (lower.endsWith(".woff")) {
            return MediaType.valueOf("font/woff");
        }
        return MediaType.APPLICATION_OCTET_STREAM;
    }

    private boolean isTextAsset(String assetPath) {
        String lower = assetPath.toLowerCase(Locale.ROOT);
        return lower.endsWith(".css") || lower.endsWith(".js") || lower.endsWith(".svg");
    }

    private String normalizeTemplateSuffix(String suffix) {
        if (suffix == null || suffix.isBlank()) {
            return "pdf4th";
        }
        String trimmed = suffix.trim().replace("/", "");
        if (!trimmed.matches("[a-zA-Z0-9_-]+")) {
            throw new IllegalStateException("hudit.cnt.pdf-template-suffix 값이 올바르지 않습니다.");
        }
        return trimmed;
    }

    private String safeFilename(String userId) {
        return userId.replaceAll("[^a-zA-Z0-9._-]", "_");
    }

    private String trimTrailingSlash(String url) {
        if (url == null || url.isBlank()) {
            return "http://cnt.re.kr";
        }
        return url.endsWith("/") ? url.substring(0, url.length() - 1) : url;
    }
}
