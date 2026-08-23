/**
 * CNT 센터 PDF 일괄 생성 (1.5단계: 브라우저 하이브리드)
 * 서버는 HTML 프록시만 제공하고, PDF 변환·ZIP은 클라이언트에서 수행합니다.
 */
const CntBulkPdf = (() => {
    const CHART_RENDER_MS = 5500;
    const IFRAME_LOAD_TIMEOUT_MS = 120000;

    let active = false;
    let cancelled = false;
    let beforeUnloadHandler = null;

    function cntMembersPath() {
        const prefix = (window.HUDIT_CNT && window.HUDIT_CNT.apiPrefix) || "/cnt";
        return `${prefix}/members`;
    }

    function memberPdfUrl(userId) {
        const base = (window.HUDIT_CNT && window.HUDIT_CNT.pdfApiBase) || "/api/hudit/cnt";
        return `${base}/members/${encodeURIComponent(userId)}/pdf`;
    }

    function safeZipName(value, fallback) {
        const cleaned = String(value ?? "")
            .replace(/[^\w\u3131-\uD79D.-]+/g, "_")
            .replace(/_+/g, "_")
            .replace(/^_|_$/g, "");
        return cleaned || fallback;
    }

    function pdfFileName(member) {
        const base = safeZipName(member.userName, member.userId);
        return `CNT-${safeZipName(member.userId, "member")}_${base}.pdf`;
    }

    function wait(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    async function waitFor(predicate, timeoutMs, intervalMs = 100) {
        const started = Date.now();
        while (Date.now() - started < timeoutMs) {
            if (cancelled) {
                throw new Error("사용자가 작업을 취소했습니다.");
            }
            if (predicate()) {
                return;
            }
            await wait(intervalMs);
        }
        throw new Error("대기 시간이 초과되었습니다.");
    }

    function calcPercent(processed, total) {
        if (!total) {
            return 0;
        }
        return Math.min(100, Math.round((processed / total) * 100));
    }

    function buildProgressFields(processed, total, successCount, failCount) {
        const safeTotal = Math.max(0, Number(total) || 0);
        const safeProcessed = Math.min(Math.max(0, Number(processed) || 0), safeTotal || Number(processed) || 0);
        const percent = calcPercent(safeProcessed, safeTotal);
        return {
            countLabel: safeTotal ? `${safeProcessed} / ${safeTotal}` : `${safeProcessed} / -`,
            percent,
            percentLabel: `${percent}%`,
            totalLabel: `총 ${safeTotal}건`,
            successLabel: `완료 ${successCount}건`,
            failLabel: `에러 ${failCount}건`
        };
    }

    function setOverlayVisible(visible) {
        const overlay = document.getElementById("bulk-pdf-overlay");
        overlay.classList.toggle("hudit-hidden", !visible);
        overlay.setAttribute("aria-hidden", String(!visible));
        document.body.classList.toggle("hudit-bulk-pdf-active", visible);
    }

    function updateOverlay(state) {
        document.getElementById("bulk-pdf-title").textContent = state.title || "PDF 일괄 생성 중";
        document.getElementById("bulk-pdf-message").textContent = state.message || "다운로드 준비 중입니다. 잠시만 기다려주세요.";
        document.getElementById("bulk-pdf-center").textContent = state.centerLabel || "";
        document.getElementById("bulk-pdf-current").textContent = state.currentLabel || "";
        document.getElementById("bulk-pdf-step").textContent = state.stepLabel || "";

        const progress = buildProgressFields(
            state.processed ?? 0,
            state.total ?? 0,
            state.successCount ?? 0,
            state.failCount ?? 0
        );
        document.getElementById("bulk-pdf-count").textContent = state.countLabel || progress.countLabel;
        document.getElementById("bulk-pdf-percent").textContent = state.percentLabel || progress.percentLabel;
        document.getElementById("bulk-pdf-progress").style.width = `${state.percent ?? progress.percent}%`;
        document.getElementById("bulk-pdf-total").textContent = state.totalLabel || progress.totalLabel;
        document.getElementById("bulk-pdf-success").textContent = state.successLabel || progress.successLabel;
        document.getElementById("bulk-pdf-fail").textContent = state.failLabel || progress.failLabel;

        const cancelBtn = document.getElementById("bulk-pdf-cancel-btn");
        const closeBtn = document.getElementById("bulk-pdf-close-btn");
        cancelBtn.classList.toggle("hudit-hidden", !state.showCancel);
        closeBtn.classList.toggle("hudit-hidden", !state.showClose);

        const log = document.getElementById("bulk-pdf-log");
        if (state.logHtml != null) {
            log.innerHTML = state.logHtml;
        }
    }

    function attachBeforeUnload() {
        beforeUnloadHandler = (event) => {
            event.preventDefault();
            event.returnValue = "";
        };
        window.addEventListener("beforeunload", beforeUnloadHandler);
    }

    function detachBeforeUnload() {
        if (beforeUnloadHandler) {
            window.removeEventListener("beforeunload", beforeUnloadHandler);
            beforeUnloadHandler = null;
        }
    }

    function removeIframe(iframe) {
        if (iframe && iframe.parentNode) {
            iframe.parentNode.removeChild(iframe);
        }
    }

    function loadIframe(userId) {
        return new Promise((resolve, reject) => {
            const iframe = document.createElement("iframe");
            iframe.className = "hudit-bulk-pdf-iframe";
            iframe.setAttribute("aria-hidden", "true");
            iframe.setAttribute("title", `PDF 리포트 ${userId}`);

            const timer = setTimeout(() => {
                cleanup();
                reject(new Error("리포트 로딩 시간 초과"));
            }, IFRAME_LOAD_TIMEOUT_MS);

            function cleanup() {
                clearTimeout(timer);
                iframe.onload = null;
                iframe.onerror = null;
            }

            iframe.onload = () => {
                cleanup();
                resolve(iframe);
            };
            iframe.onerror = () => {
                cleanup();
                reject(new Error("리포트 화면을 불러오지 못했습니다."));
            };

            document.body.appendChild(iframe);
            iframe.src = memberPdfUrl(userId);
        });
    }

    async function generatePdfBlob(iframe, member) {
        const win = iframe.contentWindow;
        const doc = win?.document;
        if (!doc || !win) {
            throw new Error("리포트 문서에 접근할 수 없습니다.");
        }

        await waitFor(() => typeof win.html2pdf === "function", 20000);
        await waitFor(() => doc.getElementById("content1"), 15000);

        const fileName = pdfFileName(member);
        const callbackName = `__huditPdfCb_${Date.now()}_${Math.random().toString(36).slice(2)}`;
        const PDF_TIMEOUT_MS = 180000;

        return new Promise((resolve, reject) => {
            let settled = false;

            function cleanup(scriptEl) {
                if (scriptEl?.parentNode) {
                    scriptEl.parentNode.removeChild(scriptEl);
                }
                try {
                    delete win[callbackName];
                } catch (_) {
                    win[callbackName] = undefined;
                }
            }

            function finish(result, scriptEl) {
                if (settled) {
                    return;
                }
                settled = true;
                cleanup(scriptEl);
                if (result?.error) {
                    reject(new Error(result.error));
                    return;
                }
                if (!result?.blob) {
                    reject(new Error("PDF blob을 생성하지 못했습니다."));
                    return;
                }
                resolve(result.blob);
            }

            const timer = setTimeout(() => {
                finish({ error: "PDF 변환 시간 초과" }, script);
            }, PDF_TIMEOUT_MS);

            win[callbackName] = (result) => {
                clearTimeout(timer);
                finish(result, script);
            };

            const script = doc.createElement("script");
            script.textContent = `
(function() {
    var finish = window[${JSON.stringify(callbackName)}];
    try {
        var options = {
            margin: 0,
            filename: ${JSON.stringify(fileName)},
            image: { type: "jpeg", quality: 0.98 },
            html2canvas: { scale: 2, useCORS: true, logging: false },
            jsPDF: { unit: "in", format: "a4", orientation: "portrait" }
        };
        var sections = ["content1","content2","content3","content4","content5","content6","content7","content8","content11"];
        var excludeItems = [];
        if (typeof doneEmo !== "undefined" && doneEmo === "N") excludeItems.push("content4");
        if (typeof doneNB !== "undefined" && doneNB === "N") excludeItems.push("content5");
        if (typeof doneMR !== "undefined" && doneMR === "N") excludeItems.push("content6");
        if (typeof doneCS !== "undefined" && doneCS === "N") excludeItems.push("content7");
        if (typeof doneTap !== "undefined" && doneTap === "N") excludeItems.push("content8");
        var filteredSections = sections.filter(function(section) {
            return excludeItems.indexOf(section) === -1;
        });
        if (!filteredSections.length) {
            throw new Error("PDF로 변환할 섹션이 없습니다.");
        }
        var pdf = html2pdf().set(options);
        filteredSections.forEach(function(sectionId, index) {
            var element = document.getElementById(sectionId);
            if (!element) {
                throw new Error("섹션을 찾을 수 없습니다: " + sectionId);
            }
            pdf = pdf.from(element).toContainer().toCanvas().toPdf();
            if (index < filteredSections.length - 1) {
                pdf = pdf.get("pdf").then(function(pdfInstance) {
                    pdfInstance.addPage();
                });
            }
        });
        pdf.output("blob").then(function(blob) {
            finish({ blob: blob });
        }).catch(function(err) {
            finish({ error: err && err.message ? err.message : String(err) });
        });
    } catch (err) {
        finish({ error: err && err.message ? err.message : String(err) });
    }
})();
            `;
            doc.body.appendChild(script);
        });
    }

    async function fetchMemberTotal(centerId) {
        const params = new URLSearchParams({
            centerId,
            sort: "no",
            order: "asc",
            page: "1",
            size: "1"
        });
        const data = await huditFetch(`${cntMembersPath()}?${params.toString()}`);
        return Number(data.total) || 0;
    }

    async function fetchAllCenterMembers(centerId, onPageLoaded) {
        const members = [];
        let currentPage = 1;
        let totalPages = 1;
        const size = 100;

        do {
            if (cancelled) {
                throw new Error("사용자가 작업을 취소했습니다.");
            }
            const params = new URLSearchParams({
                centerId,
                sort: "no",
                order: "asc",
                page: String(currentPage),
                size: String(size)
            });
            const data = await huditFetch(`${cntMembersPath()}?${params.toString()}`);
            members.push(...(data.items || []));
            totalPages = data.totalPages || 1;
            if (typeof onPageLoaded === "function") {
                onPageLoaded(members.length, Number(data.total) || members.length);
            }
            currentPage += 1;
        } while (currentPage <= totalPages);

        return members;
    }

    function buildReadme(center, successes, failures) {
        const lines = [
            "HUDIT CNT 센터 PDF 일괄 생성",
            `센터: ${center.centerId} (${center.centerNm || "-"})`,
            `생성 시각: ${new Date().toLocaleString("ko-KR")}`,
            "",
            `성공: ${successes.length}건`,
            `실패: ${failures.length}건`,
            "",
            "※ 브라우저에서 html2pdf로 생성한 PDF 파일입니다.",
            "※ 창을 닫지 않고 완료될 때까지 기다려 주세요."
        ];
        if (failures.length) {
            lines.push("", "[실패 목록]");
            failures.forEach(item => {
                lines.push(`- ${item.member.userId} (${item.member.userName || "-"}): ${item.message}`);
            });
        }
        return lines.join("\r\n");
    }

    async function downloadZip(center, successes, failures) {
        if (typeof JSZip !== "function") {
            throw new Error("ZIP 라이브러리를 불러오지 못했습니다.");
        }
        const zip = new JSZip();
        zip.file("README.txt", buildReadme(center, successes, failures));

        successes.forEach(item => {
            zip.file(item.fileName, item.blob);
        });

        failures.forEach(item => {
            const errorName = `${safeZipName(item.member.userId, "member")}.pdf.error.txt`;
            zip.file(errorName, `userId=${item.member.userId}\r\nname=${item.member.userName || ""}\r\nmessage=${item.message}\r\n`);
        });

        return zip.generateAsync({ type: "blob", compression: "DEFLATE", compressionOptions: { level: 6 } });
    }

    function triggerDownload(blob, fileName) {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }

    function progressUpdate(base, processed, total, successCount, failCount) {
        return {
            ...base,
            processed,
            total,
            successCount,
            failCount
        };
    }

    async function run(center) {
        if (active) {
            return;
        }

        const confirmed = window.confirm(
            `${center.centerNm || center.centerId} 센터 소속 멤버의 PDF를 일괄 생성합니다.\n\n` +
            "브라우저에서 처리되며 수 분이 걸릴 수 있습니다.\n" +
            "진행 중 창을 닫거나 새로고침하지 마세요.\n\n계속하시겠습니까?"
        );
        if (!confirmed) {
            return;
        }

        active = true;
        cancelled = false;
        setOverlayVisible(true);
        attachBeforeUnload();

        const successes = [];
        const failures = [];
        let members = [];
        let total = 0;

        try {
            updateOverlay({
                title: "PDF 일괄 생성 준비",
                message: "다운로드 준비 중입니다. 잠시만 기다려주세요.",
                centerLabel: `센터: ${center.centerId} · ${center.centerNm || "-"}`,
                currentLabel: "현재 작업: 멤버 수 확인",
                stepLabel: "단계: 준비",
                processed: 0,
                total: 0,
                successCount: 0,
                failCount: 0,
                showCancel: true,
                showClose: false,
                logHtml: "<ul class=\"hudit-bulk-pdf-log-list\"><li>멤버 수를 확인하는 중...</li></ul>"
            });

            total = await fetchMemberTotal(center.centerId);
            if (!total) {
                throw new Error("이 센터에 등록된 멤버가 없습니다.");
            }

            updateOverlay({
                currentLabel: "현재 작업: 멤버 목록 조회",
                stepLabel: "단계: 준비",
                processed: 0,
                total,
                successCount: 0,
                failCount: 0,
                logHtml: `<ul class="hudit-bulk-pdf-log-list"><li>총 ${total}명의 PDF를 준비합니다.</li></ul>`
            });

            members = await fetchAllCenterMembers(center.centerId);
            total = members.length || total;
            if (!members.length) {
                throw new Error("이 센터에 등록된 멤버가 없습니다.");
            }

            let logItems = [];

            updateOverlay({
                title: "PDF 일괄 생성 중",
                currentLabel: "현재 작업: 생성 대기",
                stepLabel: "단계: 시작",
                processed: 0,
                total,
                successCount: 0,
                failCount: 0,
                logHtml: `<ul class="hudit-bulk-pdf-log-list"><li>총 ${total}명 처리를 시작합니다.</li></ul>`
            });

            for (let index = 0; index < total; index += 1) {
                if (cancelled) {
                    throw new Error("사용자가 작업을 취소했습니다.");
                }

                const member = members[index];
                const processed = index;

                updateOverlay(progressUpdate({
                    title: "PDF 일괄 생성 중",
                    message: "다운로드 준비 중입니다. 잠시만 기다려주세요.",
                    centerLabel: `센터: ${center.centerId} · ${center.centerNm || "-"}`,
                    currentLabel: `현재 처리: ${member.userId} · ${member.userName || "-"}`,
                    stepLabel: "단계: 리포트 로딩",
                    showCancel: true,
                    showClose: false
                }, processed, total, successes.length, failures.length));

                let iframe = null;
                try {
                    iframe = await loadIframe(member.userId);

                    updateOverlay(progressUpdate({
                        stepLabel: "단계: 차트 렌더링 대기",
                        currentLabel: `현재 처리: ${member.userId} · ${member.userName || "-"}`
                    }, processed, total, successes.length, failures.length));

                    await wait(CHART_RENDER_MS);

                    if (cancelled) {
                        throw new Error("사용자가 작업을 취소했습니다.");
                    }

                    updateOverlay(progressUpdate({
                        stepLabel: "단계: PDF 변환",
                        currentLabel: `현재 처리: ${member.userId} · ${member.userName || "-"}`
                    }, processed, total, successes.length, failures.length));

                    const blob = await generatePdfBlob(iframe, member);
                    const fileName = pdfFileName(member);
                    successes.push({ member, blob, fileName });
                    logItems.unshift(`<li class="ok">${escapeHtml(member.userId)} · PDF 생성 완료</li>`);
                } catch (error) {
                    failures.push({ member, message: error.message || String(error) });
                    logItems.unshift(`<li class="fail">${escapeHtml(member.userId)} · ${escapeHtml(error.message || "실패")}</li>`);
                } finally {
                    removeIframe(iframe);
                }

                const completed = index + 1;
                updateOverlay(progressUpdate({
                    title: "PDF 일괄 생성 중",
                    message: "다운로드 준비 중입니다. 잠시만 기다려주세요.",
                    currentLabel: completed < total
                        ? `다음 대기: ${members[completed]?.userId || "-"}`
                        : "모든 멤버 처리 완료",
                    stepLabel: completed < total ? "단계: 다음 항목 준비" : "단계: 마무리",
                    logHtml: `<ul class="hudit-bulk-pdf-log-list">${logItems.slice(0, 8).join("")}</ul>`
                }, completed, total, successes.length, failures.length));
            }

            if (cancelled) {
                throw new Error("사용자가 작업을 취소했습니다.");
            }

            updateOverlay(progressUpdate({
                title: "ZIP 파일 생성 중",
                message: "다운로드 준비 중입니다. 잠시만 기다려주세요.",
                stepLabel: "단계: ZIP 압축",
                currentLabel: `압축 준비 중 (${successes.length}건 포함)`,
                showCancel: false,
                showClose: false
            }, total, total, successes.length, failures.length));

            const zipBlob = await downloadZip(center, successes, failures);
            const zipName = `center-${safeZipName(center.centerId, "center")}-pdfs.zip`;
            triggerDownload(zipBlob, zipName);

            updateOverlay(progressUpdate({
                title: "PDF 일괄 생성 완료",
                message: failures.length
                    ? `ZIP 다운로드가 시작되었습니다. 완료 ${successes.length}건, 에러 ${failures.length}건입니다.`
                    : `ZIP 다운로드가 시작되었습니다. 총 ${successes.length}건이 포함되었습니다.`,
                stepLabel: "단계: 완료",
                currentLabel: `파일명: ${zipName}`,
                showCancel: false,
                showClose: true,
                logHtml: `<ul class="hudit-bulk-pdf-log-list">${successes.slice(-5).map(item => `<li class="ok">${escapeHtml(item.member.userId)} · 포함됨</li>`).join("")}${failures.map(item => `<li class="fail">${escapeHtml(item.member.userId)} · ${escapeHtml(item.message)}</li>`).join("")}</ul>`
            }, total, total, successes.length, failures.length));
        } catch (error) {
            const message = error.message || "PDF 일괄 생성 중 오류가 발생했습니다.";
            updateOverlay(progressUpdate({
                title: cancelled ? "PDF 일괄 생성 취소됨" : "PDF 일괄 생성 실패",
                message,
                stepLabel: "단계: 중단",
                currentLabel: successes.length ? `${successes.length + failures.length}건까지 처리됨` : "처리된 PDF 없음",
                showCancel: false,
                showClose: true
            }, successes.length + failures.length, total, successes.length, failures.length));
        } finally {
            active = false;
            detachBeforeUnload();
        }
    }

    function cancel() {
        if (!active) {
            setOverlayVisible(false);
            return;
        }
        cancelled = true;
        updateOverlay({
            message: "취소 요청을 처리하는 중입니다. 잠시만 기다려주세요.",
            stepLabel: "단계: 취소 중",
            showCancel: false
        });
    }

    function close() {
        if (active && !cancelled) {
            return;
        }
        setOverlayVisible(false);
        active = false;
        cancelled = false;
    }

    function init() {
        document.getElementById("bulk-pdf-cancel-btn")?.addEventListener("click", cancel);
        document.getElementById("bulk-pdf-close-btn")?.addEventListener("click", close);
    }

    return {
        init,
        run,
        isActive: () => active
    };
})();
