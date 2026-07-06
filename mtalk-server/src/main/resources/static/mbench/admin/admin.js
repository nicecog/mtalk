const API = {
    login: '/mbench/api/users/login',
    logout: '/mbench/api/users/logout',
    session: '/mbench/api/admin/session',
    users: '/mbench/api/admin/users',
};

const $ = (id) => document.getElementById(id);

const loginPanel = $('login-panel');
const mainPanel = $('main-panel');
const loginForm = $('login-form');
const loginError = $('login-error');
const userRows = $('user-rows');
const userDialog = $('user-dialog');
const userForm = $('user-form');
const formError = $('form-error');

let editingSeq = null;

async function api(path, options = {}) {
    const res = await fetch(path, {
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
        ...options,
    });
    const text = await res.text();
    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { data = { message: text }; }
    if (!res.ok) {
        const msg = data?.message || res.statusText;
        throw new Error(msg);
    }
    return data;
}

async function checkSession() {
    try {
        const s = await api(API.session);
        if (s.loggedIn) {
            showMain();
            await loadUsers();
            return true;
        }
    } catch { /* not logged in */ }
    showLogin();
    return false;
}

function showLogin() {
    loginPanel.hidden = false;
    mainPanel.hidden = true;
}

function showMain() {
    loginPanel.hidden = true;
    mainPanel.hidden = false;
}

loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    loginError.hidden = true;
    try {
        await api(API.login, {
            method: 'POST',
            body: JSON.stringify({
                id: $('login-id').value.trim(),
                password: $('login-password').value,
            }),
        });
        showMain();
        await loadUsers();
    } catch (err) {
        loginError.textContent = err.message || '로그인 실패';
        loginError.hidden = false;
    }
});

$('btn-logout').addEventListener('click', async () => {
    try { await fetch(API.logout, { credentials: 'include' }); } catch { /* ignore */ }
    showLogin();
});

$('btn-refresh').addEventListener('click', () => loadUsers());

$('btn-add').addEventListener('click', () => openDialog());

$('btn-cancel').addEventListener('click', () => userDialog.close());

async function loadUsers() {
    userRows.innerHTML = '<tr><td colspan="9" class="empty">불러오는 중…</td></tr>';
    try {
        const users = await api(API.users);
        if (!users.length) {
            userRows.innerHTML = '<tr><td colspan="9" class="empty">등록된 사용자가 없습니다.</td></tr>';
            return;
        }
        userRows.innerHTML = users.map((u) => `
            <tr>
                <td>${u.seq}</td>
                <td><code>${escapeHtml(u.id)}</code></td>
                <td>${escapeHtml(u.name || '')}</td>
                <td>${escapeHtml(u.email || '')}</td>
                <td>${escapeHtml(u.phone || '')}</td>
                <td>${u.levelId}</td>
                <td>${escapeHtml(u.domain || '')}</td>
                <td>${escapeHtml(u.createdAt || '')}</td>
                <td class="actions">
                    <button type="button" class="btn sm" data-edit="${u.seq}">수정</button>
                    <button type="button" class="btn sm danger" data-del="${u.seq}" data-id="${escapeHtml(u.id)}">삭제</button>
                </td>
            </tr>
        `).join('');

        userRows.querySelectorAll('[data-edit]').forEach((btn) => {
            btn.addEventListener('click', () => openDialog(Number(btn.dataset.edit)));
        });
        userRows.querySelectorAll('[data-del]').forEach((btn) => {
            btn.addEventListener('click', () => deleteUser(Number(btn.dataset.del), btn.dataset.id));
        });
    } catch (err) {
        userRows.innerHTML = `<tr><td colspan="9" class="empty error">${escapeHtml(err.message)}</td></tr>`;
    }
}

async function openDialog(seq) {
    editingSeq = seq || null;
    formError.hidden = true;
    userForm.reset();
    $('field-domain').value = 'spike';
    $('field-level').value = '1';
    $('field-password').required = !editingSeq;
    $('pw-label-req').hidden = !!editingSeq;
    $('dialog-title').textContent = editingSeq ? '사용자 수정' : '사용자 등록';

    if (editingSeq) {
        const u = await api(`${API.users}/${editingSeq}`);
        $('field-seq').value = u.seq;
        $('field-id').value = u.id;
        $('field-name').value = u.name || '';
        $('field-email').value = u.email || '';
        $('field-phone').value = u.phone || '';
        $('field-level').value = u.levelId;
        $('field-domain').value = u.domain || 'spike';
    } else {
        $('field-seq').value = '';
    }

    userDialog.showModal();
}

userForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    formError.hidden = true;

    const body = {
        id: $('field-id').value.trim(),
        name: $('field-name').value.trim(),
        email: $('field-email').value.trim(),
        phone: $('field-phone').value.trim(),
        levelId: Number($('field-level').value) || 1,
        domain: $('field-domain').value.trim() || 'spike',
    };
    const pw = $('field-password').value;
    if (pw) body.password = pw;

    try {
        if (editingSeq) {
            if (!pw) delete body.password;
            await api(`${API.users}/${editingSeq}`, { method: 'PUT', body: JSON.stringify(body) });
        } else {
            if (!pw) {
                formError.textContent = '비밀번호를 입력하세요.';
                formError.hidden = false;
                return;
            }
            body.password = pw;
            await api(API.users, { method: 'POST', body: JSON.stringify(body) });
        }
        userDialog.close();
        await loadUsers();
    } catch (err) {
        formError.textContent = err.message;
        formError.hidden = false;
    }
});

async function deleteUser(seq, loginId) {
    if (!confirm(`'${loginId}' 사용자를 삭제할까요?\n연결된 영상 메타도 함께 삭제됩니다.`)) return;
    try {
        await api(`${API.users}/${seq}`, { method: 'DELETE' });
        await loadUsers();
    } catch (err) {
        alert(err.message);
    }
}

function escapeHtml(s) {
    return String(s)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

checkSession();
