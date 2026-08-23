const CNTHUDIT_API = "/cnthudit/api";

async function cnthuditFetch(path, options = {}) {
    const response = await fetch(`${CNTHUDIT_API}${path}`, {
        credentials: "same-origin",
        headers: {
            "Content-Type": "application/json",
            ...(options.headers || {})
        },
        ...options
    });

    if (!response.ok) {
        let message = `요청 실패 (${response.status})`;
        try {
            const body = await response.json();
            if (body.message) {
                message = body.message;
            } else if (body.error) {
                message = body.error;
            }
        } catch (_) {
            // ignore parse errors
        }
        throw new Error(message);
    }

    if (response.status === 204) {
        return null;
    }

    const contentType = response.headers.get("content-type") || "";
    if (contentType.includes("application/json")) {
        return response.json();
    }
    return response.text();
}

const huditFetch = cnthuditFetch;

async function requireAuth(redirectTo = "/cnthudit/") {
    const params = new URLSearchParams(window.location.search);
    const ticket = params.get("ticket");
    if (ticket) {
        try {
            await cnthuditFetch("/auth/consume-ticket", {
                method: "POST",
                body: JSON.stringify({ ticket })
            });
            window.history.replaceState({}, "", window.location.pathname);
        } catch (error) {
            window.location.href = redirectTo;
            return null;
        }
    }

    const session = await cnthuditFetch("/auth/session");
    if (!session.authenticated) {
        window.location.href = redirectTo;
        return null;
    }
    return session;
}

function showAlert(container, message, type = "error") {
    if (!container) {
        return;
    }
    container.innerHTML = `<div class="hudit-alert hudit-alert-${type}">${message}</div>`;
}

function clearAlert(container) {
    if (container) {
        container.innerHTML = "";
    }
}

function formatDate(value) {
    return value || "-";
}

function escapeHtml(text) {
    return String(text ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;");
}

async function logout() {
    await cnthuditFetch("/auth/logout", { method: "POST" });
    window.location.href = "/cnthudit/";
}

document.addEventListener("DOMContentLoaded", () => {
    const logoutBtn = document.getElementById("logout-btn");
    if (logoutBtn) {
        logoutBtn.addEventListener("click", async (event) => {
            event.preventDefault();
            await logout();
        });
    }
});
