package com.mtalk.common;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.LinkedHashMap;
import java.util.Map;

@RestController
public class RootController {

    @GetMapping("/health")
    public Map<String, Object> root() {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("status", "UP");
        body.put("service", "mtalk");
        body.put("routes", Map.of(
                "mbench", "/mbench/health",
                "mbody", "/mbody/api/health",
                "mface", "/mface/api/health",
                "msocial", "/msocial/api/health"
        ));
        return body;
    }
}
