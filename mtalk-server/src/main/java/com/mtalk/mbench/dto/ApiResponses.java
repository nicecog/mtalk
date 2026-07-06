package com.mtalk.mbench.dto;

import java.util.LinkedHashMap;
import java.util.Map;

public final class ApiResponses {

    private ApiResponses() {
    }

    /** Unity Contains("true") 호환 — plain text */
    public static String accountExistsBody(boolean exists) {
        return Boolean.toString(exists);
    }

    /** Unity seq 파싱 호환 — 반드시 "seq":숫자 형태 포함 */
    public static Map<String, Object> loginSuccess(long seq, String loginId) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("success", true);
        body.put("seq", seq);
        body.put("id", loginId);
        body.put("message", "login ok");
        return body;
    }

    /** Unity Split(':')[1].Split('"')[1] 파싱 호환 */
    public static Map<String, Object> fileAttached(String fileVideo) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("success", true);
        body.put("fileVideo", fileVideo);
        return body;
    }

    public static Map<String, Object> ok(String message) {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("success", true);
        body.put("message", message);
        return body;
    }
}
