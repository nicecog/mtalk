package com.mtalk.mbench.web;

import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;

@Controller
public class AdminViewController {

    @GetMapping({"/mbench/admin", "/mbench/admin/"})
    public String adminHome() {
        return "forward:/mbench/admin/index.html";
    }
}
