package com.mtalk.cnthudit;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.servlet.view.RedirectView;

@Controller
public class CnthuditViewController {

    private final String cnthuditPublicBaseUrl;

    public CnthuditViewController(
            @Value("${hudit.cnt.cnthudit-public-base-url:https://huditcnt.cafe24.com}") String cnthuditPublicBaseUrl
    ) {
        this.cnthuditPublicBaseUrl = trimTrailingSlash(cnthuditPublicBaseUrl);
    }

    @GetMapping({"/cnthudit", "/cnthudit/"})
    public RedirectView loginPage() {
        return permanentRedirect(cnthuditPublicBaseUrl + "/cnthudit/");
    }

    @GetMapping("/cnthudit/app")
    public RedirectView appPage() {
        RedirectView view = permanentRedirect(cnthuditPublicBaseUrl + "/cnthudit/app");
        view.setPropagateQueryParams(true);
        return view;
    }

    private static RedirectView permanentRedirect(String url) {
        RedirectView view = new RedirectView(url);
        view.setStatusCode(HttpStatus.MOVED_PERMANENTLY);
        return view;
    }

    private static String trimTrailingSlash(String url) {
        if (url == null || url.isBlank()) {
            return "https://huditcnt.cafe24.com";
        }
        return url.endsWith("/") ? url.substring(0, url.length() - 1) : url.trim();
    }
}
