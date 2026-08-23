let board = { projects: [] };
let selectedSeq = null;
let statusFilter = "all";

function statusClass(status) {
    return `project-status project-status--${status || "planned"}`;
}

function setModalOpen(id, open) {
    document.getElementById(id).classList.toggle("open", open);
}

function renderStats(data) {
    document.getElementById("stat-total").textContent = data.total ?? 0;
    document.getElementById("stat-in-progress").textContent = data.inProgress ?? 0;
    document.getElementById("stat-planned").textContent = data.planned ?? 0;
    document.getElementById("stat-paused").textContent = data.paused ?? 0;
    document.getElementById("stat-done").textContent = data.done ?? 0;
    document.querySelectorAll(".project-stat").forEach((el) => {
        el.classList.toggle("is-active", el.dataset.status === statusFilter);
    });
}

function renderList() {
    const root = document.getElementById("project-list");
    const projects = board.projects || [];
    if (!projects.length) {
        root.innerHTML = `<p class="hudit-empty">등록된 프로젝트가 없습니다.</p>`;
        return;
    }
    root.innerHTML = projects.map((project) => `
        <article class="project-item ${project.seq === selectedSeq ? "is-selected" : ""}" data-seq="${project.seq}" tabindex="0">
            <div class="project-item-top">
                <strong>${escapeHtml(project.title)}</strong>
                <span class="${statusClass(project.status)}">${escapeHtml(project.statusLabel)}</span>
            </div>
            <p class="project-item-summary">${escapeHtml(project.summary || "요약이 없습니다.")}</p>
            <div class="project-progress-row">
                <div class="project-progress-track">
                    <div class="project-progress-fill" style="width:${project.progress || 0}%"></div>
                </div>
                <span>${project.progress || 0}%</span>
            </div>
            <div class="project-item-meta">
                <span>${escapeHtml(project.category)}</span>
                <span>진행 ${project.updateCount}건</span>
                <span>${escapeHtml(project.updatedAt || "")}</span>
            </div>
        </article>
    `).join("");
}

async function loadBoard() {
    const category = document.getElementById("filter-category").value;
    const params = new URLSearchParams();
    if (statusFilter && statusFilter !== "all") {
        params.set("status", statusFilter);
    }
    if (category && category !== "all") {
        params.set("category", category);
    }
    const query = params.toString();
    board = await huditFetch(`/projects${query ? `?${query}` : ""}`);
    renderStats(board);
    renderList();
    if (selectedSeq) {
        const stillThere = (board.projects || []).some((item) => item.seq === selectedSeq);
        if (stillThere) {
            await loadDetail(selectedSeq);
        } else {
            selectedSeq = null;
            showDetail(false);
        }
    }
}

function showDetail(open) {
    document.getElementById("detail-empty").classList.toggle("hudit-hidden", open);
    document.getElementById("detail-body").classList.toggle("hudit-hidden", !open);
}

async function loadDetail(seq) {
    selectedSeq = seq;
    renderList();
    const data = await huditFetch(`/projects/${seq}`);
    const project = data.project;
    document.getElementById("detail-title").textContent = project.title;
    document.getElementById("detail-summary").textContent = project.summary || "요약이 없습니다.";
    document.getElementById("detail-category").textContent = project.category;
    const statusEl = document.getElementById("detail-status");
    statusEl.textContent = project.statusLabel;
    statusEl.className = `hudit-badge ${statusClass(project.status)}`;
    document.getElementById("detail-progress-fill").style.width = `${project.progress || 0}%`;
    document.getElementById("detail-progress-label").textContent = `${project.progress || 0}%`;
    document.getElementById("detail-owner").textContent =
        `담당 ${project.ownerName || "-"} · 수정 ${project.updatedBy || "-"} · ${project.updatedAt || ""}`;
    renderUpdates(data.updates || []);
    showDetail(true);
}

function renderUpdates(updates) {
    const root = document.getElementById("update-list");
    if (!updates.length) {
        root.innerHTML = `<p class="hudit-empty">아직 등록된 진행사항이 없습니다.</p>`;
        return;
    }
    root.innerHTML = updates.map((item) => `
        <article class="project-update">
            <div class="project-update-head">
                <strong>${escapeHtml(item.createdBy || "관리자")}</strong>
                <span>${escapeHtml(item.createdAt || "")}</span>
                ${item.progress == null ? "" : `<span class="hudit-badge">${item.progress}%</span>`}
                <button class="hudit-btn hudit-btn-secondary hudit-btn-xs" type="button" data-delete-update="${item.seq}">삭제</button>
            </div>
            <p>${escapeHtml(item.content).replaceAll("\n", "<br>")}</p>
        </article>
    `).join("");
}

function openCreate() {
    document.getElementById("project-modal-title").textContent = "프로젝트 등록";
    document.getElementById("edit-seq").value = "";
    document.getElementById("form-title").value = "";
    document.getElementById("form-category").value = "기타";
    document.getElementById("form-status").value = "planned";
    document.getElementById("form-progress").value = "0";
    document.getElementById("form-owner").value = "";
    document.getElementById("form-summary").value = "";
    clearAlert(document.getElementById("project-modal-alert"));
    setModalOpen("project-modal", true);
}

function openEdit(project) {
    document.getElementById("project-modal-title").textContent = "프로젝트 수정";
    document.getElementById("edit-seq").value = String(project.seq);
    document.getElementById("form-title").value = project.title || "";
    document.getElementById("form-category").value = project.category || "기타";
    document.getElementById("form-status").value = project.status || "planned";
    document.getElementById("form-progress").value = String(project.progress ?? 0);
    document.getElementById("form-owner").value = project.ownerName || "";
    document.getElementById("form-summary").value = project.summary || "";
    clearAlert(document.getElementById("project-modal-alert"));
    setModalOpen("project-modal", true);
}

function formPayload() {
    return {
        title: document.getElementById("form-title").value.trim(),
        category: document.getElementById("form-category").value,
        status: document.getElementById("form-status").value,
        progress: Number(document.getElementById("form-progress").value || 0),
        summary: document.getElementById("form-summary").value.trim(),
        ownerName: document.getElementById("form-owner").value.trim()
    };
}

async function saveProject() {
    const alertBox = document.getElementById("project-modal-alert");
    clearAlert(alertBox);
    const payload = formPayload();
    if (!payload.title) {
        showAlert(alertBox, "프로젝트 이름을 입력해 주세요.");
        return;
    }
    try {
        const seq = document.getElementById("edit-seq").value;
        const saved = seq
            ? await huditFetch(`/projects/${seq}`, { method: "PUT", body: JSON.stringify(payload) })
            : await huditFetch("/projects", { method: "POST", body: JSON.stringify(payload) });
        setModalOpen("project-modal", false);
        selectedSeq = saved.seq;
        await loadBoard();
    } catch (error) {
        showAlert(alertBox, error.message || "저장에 실패했습니다.");
    }
}

document.addEventListener("DOMContentLoaded", async () => {
    const session = await requireAuth();
    if (!session) {
        return;
    }

    document.getElementById("create-project-btn").addEventListener("click", openCreate);
    document.getElementById("project-modal-close").addEventListener("click", () => setModalOpen("project-modal", false));
    document.getElementById("project-modal-cancel").addEventListener("click", () => setModalOpen("project-modal", false));
    document.getElementById("project-modal-save").addEventListener("click", saveProject);
    document.getElementById("filter-category").addEventListener("change", loadBoard);

    document.getElementById("project-stats").addEventListener("click", (event) => {
        const button = event.target.closest(".project-stat");
        if (!button) {
            return;
        }
        statusFilter = button.dataset.status || "all";
        loadBoard();
    });

    document.getElementById("project-list").addEventListener("click", (event) => {
        const item = event.target.closest(".project-item");
        if (item) {
            loadDetail(Number(item.dataset.seq));
        }
    });
    document.getElementById("project-list").addEventListener("keydown", (event) => {
        if (event.key !== "Enter" && event.key !== " ") {
            return;
        }
        const item = event.target.closest(".project-item");
        if (!item) {
            return;
        }
        event.preventDefault();
        loadDetail(Number(item.dataset.seq));
    });

    document.getElementById("edit-project-btn").addEventListener("click", async () => {
        if (!selectedSeq) {
            return;
        }
        const data = await huditFetch(`/projects/${selectedSeq}`);
        openEdit(data.project);
    });

    document.getElementById("delete-project-btn").addEventListener("click", async () => {
        if (!selectedSeq) {
            return;
        }
        if (!window.confirm("이 프로젝트와 진행사항을 삭제할까요?")) {
            return;
        }
        await huditFetch(`/projects/${selectedSeq}`, { method: "DELETE" });
        selectedSeq = null;
        showDetail(false);
        await loadBoard();
    });

    document.getElementById("update-form").addEventListener("submit", async (event) => {
        event.preventDefault();
        if (!selectedSeq) {
            return;
        }
        const content = document.getElementById("update-content").value.trim();
        const progressValue = document.getElementById("update-progress").value;
        if (!content) {
            window.alert("진행사항을 입력해 주세요.");
            return;
        }
        const payload = { content };
        if (progressValue !== "") {
            payload.progress = Number(progressValue);
        }
        await huditFetch(`/projects/${selectedSeq}/updates`, {
            method: "POST",
            body: JSON.stringify(payload)
        });
        document.getElementById("update-content").value = "";
        document.getElementById("update-progress").value = "";
        await loadBoard();
    });

    document.getElementById("update-list").addEventListener("click", async (event) => {
        const button = event.target.closest("[data-delete-update]");
        if (!button || !selectedSeq) {
            return;
        }
        if (!window.confirm("이 진행사항을 삭제할까요?")) {
            return;
        }
        await huditFetch(`/projects/${selectedSeq}/updates/${button.dataset.deleteUpdate}`, { method: "DELETE" });
        await loadBoard();
    });

    await loadBoard();
});
