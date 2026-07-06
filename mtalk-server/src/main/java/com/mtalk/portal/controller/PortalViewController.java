package com.mtalk.portal.controller;

import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;

@Controller
public class PortalViewController {

    @GetMapping({"/", "/hudit", "/hudit/"})
    public String loginPage() {
        return "forward:/hudit/index.html";
    }

    @GetMapping("/hudit/dashboard")
    public String dashboardPage() {
        return "forward:/hudit/dashboard.html";
    }

    @GetMapping("/hudit/mtalk")
    public String mtalkPage() {
        return "forward:/hudit/mtalk.html";
    }

    @GetMapping("/hudit/cnt")
    public String cntPage() {
        return "forward:/hudit/cnt.html";
    }
}
