const state = {
  token: localStorage.getItem("qltv.token") || "",
  user: JSON.parse(localStorage.getItem("qltv.user") || "null"),
  view: "dashboard",
  tenant: null,
  branches: [],
  selectedBranchId: "",
  books: [],
  members: [],
  loans: [],
  holds: [],
  dashboard: null,
  notifications: [],
  auditLogs: []
};

const qs = (selector) => document.querySelector(selector);
const viewIds = ["dashboard", "catalog", "circulation", "members", "ai", "audit"];

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatDate(value) {
  if (!value) return "";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function money(value) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(value || 0);
}

function statusBadge(value) {
  const text = String(value);
  const cls = text.includes("Overdue") || text.includes("Failed") || text.includes("Blocked") ? "bad"
    : text.includes("Available") || text.includes("Active") || text.includes("Sent") || text.includes("Ready") ? "ok"
    : "warn";
  return `<span class="badge ${cls}">${escapeHtml(text)}</span>`;
}

function showToast(message) {
  const toast = qs("#toast");
  toast.textContent = message;
  toast.hidden = false;
  window.clearTimeout(showToast.timer);
  showToast.timer = window.setTimeout(() => toast.hidden = true, 3600);
}

async function api(path, options = {}) {
  const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
  if (state.token) headers.Authorization = `Bearer ${state.token}`;
  const response = await fetch(path, { ...options, headers });
  if (!response.ok) {
    const problem = await response.json().catch(() => ({ detail: response.statusText }));
    throw new Error(problem.detail || problem.title || "Request failed");
  }
  if (response.status === 204) return null;
  return response.json();
}

function setSession(loginResponse) {
  state.token = loginResponse.accessToken;
  state.user = loginResponse.user;
  localStorage.setItem("qltv.token", state.token);
  localStorage.setItem("qltv.user", JSON.stringify(state.user));
}

function clearSession() {
  state.token = "";
  state.user = null;
  localStorage.removeItem("qltv.token");
  localStorage.removeItem("qltv.user");
}

async function loadAll() {
  state.tenant = await api("/api/tenants/current");
  state.branches = [...await api("/api/branches")];
  const branchQuery = state.selectedBranchId ? `?branchId=${state.selectedBranchId}` : "";
  const [dashboard, books, members, loans, holds, notifications, auditLogs] = await Promise.all([
    api("/api/dashboard/summary"),
    api(`/api/catalog/books${branchQuery}`),
    api(`/api/members${branchQuery}`),
    api(`/api/circulation/loans${state.selectedBranchId ? `?branchId=${state.selectedBranchId}` : ""}`),
    api(`/api/circulation/holds${state.selectedBranchId ? `?branchId=${state.selectedBranchId}` : ""}`),
    api(`/api/notifications${branchQuery}`),
    api(`/api/audit-logs${branchQuery}`)
  ]);
  Object.assign(state, { dashboard, books, members, loans, holds, notifications, auditLogs });
  renderApp();
}

function renderShell() {
  const signedIn = Boolean(state.token && state.user);
  qs("#loginView").hidden = signedIn;
  qs("#appView").hidden = !signedIn;
  qs("#topbar").hidden = !signedIn;
  qs("#nav").hidden = !signedIn;

  if (!signedIn) {
    qs("#accountBox").innerHTML = "<span>Chưa đăng nhập</span>";
    return;
  }

  qs("#accountBox").innerHTML = `
    <strong>${escapeHtml(state.user.fullName)}</strong>
    <span>${escapeHtml(state.user.email)}</span>
    <span>${escapeHtml(state.user.role)} · ${escapeHtml(state.user.locale)}</span>
  `;
}

function renderBranchFilter() {
  const select = qs("#branchFilter");
  select.innerHTML = `<option value="">Tất cả chi nhánh</option>` + state.branches.map(branch =>
    `<option value="${branch.id}" ${branch.id === state.selectedBranchId ? "selected" : ""}>${escapeHtml(branch.name)}</option>`
  ).join("");
}

function renderApp() {
  renderShell();
  if (!state.user) return;
  qs("#tenantKey").textContent = `${state.tenant?.key || state.user.tenantKey} · ${state.tenant?.plan || "MVP"}`;
  qs("#pageTitle").textContent = ({
    dashboard: "Dashboard",
    catalog: "Catalog",
    circulation: "Mượn trả",
    members: "Độc giả",
    ai: "AI Search",
    audit: "Audit log"
  })[state.view];
  renderBranchFilter();
  renderDashboard();
  renderCatalog();
  renderCirculation();
  renderMembers();
  renderAi();
  renderAudit();
  for (const id of viewIds) qs(`#${id}View`).hidden = id !== state.view;
}

function renderDashboard() {
  const data = state.dashboard;
  if (!data) return;
  qs("#dashboardView").innerHTML = `
    <div class="grid cols-4">
      ${stat("Đầu sách", data.bookCount)}
      ${stat("Bản sao", data.copyCount)}
      ${stat("Sẵn sàng", data.availableCopies)}
      ${stat("Quá hạn", data.overdueLoans)}
    </div>
    <div class="split">
      <div class="table-card">
        <header><h2>KPI chi nhánh</h2><span class="badge">${data.memberCount} độc giả</span></header>
        <div class="table-scroll"><table>
          <thead><tr><th>Chi nhánh</th><th>Bản sao</th><th>Đang mượn</th><th>Quá hạn</th></tr></thead>
          <tbody>${data.branches.map(x => `<tr><td>${escapeHtml(x.branchName)}</td><td>${x.copies}</td><td>${x.activeLoans}</td><td>${x.overdueLoans}</td></tr>`).join("")}</tbody>
        </table></div>
      </div>
      <div class="card">
        <h2>AI Insight</h2>
        <p class="muted">Có ${data.loanedCopies} bản đang mượn, ${data.overdueLoans} lượt quá hạn, tiền phạt mở ${money(data.openFineAmount)}.</p>
        <div class="list">${data.popularBooks.map(x => `<div class="list-item"><strong>${escapeHtml(x.title)}</strong><span class="muted">${x.loanCount} lượt mượn</span></div>`).join("") || empty("Chưa có dữ liệu mượn")}</div>
      </div>
    </div>
    <div class="table-card">
      <header><h2>Hoạt động gần đây</h2><span class="badge">Audit scoped</span></header>
      <div class="table-scroll"><table>
        <thead><tr><th>Thời gian</th><th>Hành động</th><th>Nội dung</th></tr></thead>
        <tbody>${data.recentActivities.map(x => `<tr><td>${formatDate(x.createdAt)}</td><td>${escapeHtml(x.action)}</td><td>${escapeHtml(x.summary)}</td></tr>`).join("")}</tbody>
      </table></div>
    </div>
  `;
}

function stat(label, value) {
  return `<div class="stat"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></div>`;
}

function renderCatalog() {
  qs("#catalogView").innerHTML = `
    <div class="table-card">
      <header>
        <h2>Danh mục sách</h2>
        <div class="table-tools">
          <input id="bookSearch" placeholder="Tìm theo tên, ISBN, tag..." value="${escapeHtml(qs("#bookSearch")?.value || "")}">
          <button id="bookSearchBtn" class="secondary">Tìm</button>
        </div>
      </header>
      <div class="table-scroll"><table>
        <thead><tr><th>Sách</th><th>Tác giả</th><th>Thể loại</th><th>ISBN</th><th>Bản sao</th><th>Sẵn sàng</th></tr></thead>
        <tbody>${state.books.map(book => `
          <tr>
            <td><strong>${escapeHtml(book.title)}</strong><br><span class="muted">${escapeHtml(book.description)}</span></td>
            <td>${book.authors.map(escapeHtml).join(", ")}</td>
            <td>${book.categories.map(escapeHtml).join(", ")}</td>
            <td>${escapeHtml(book.isbn)}</td>
            <td>${book.totalCopies}</td>
            <td>${book.availableCopies}</td>
          </tr>`).join("") || `<tr><td colspan="6">${empty("Không có sách phù hợp")}</td></tr>`}
        </tbody>
      </table></div>
    </div>
    <div class="grid cols-2">
      <form id="bookForm" class="form-card">
        <h2>Thêm sách</h2>
        <div class="form-grid">
          <label class="wide">Tên sách<input name="title" required></label>
          <label>ISBN<input name="isbn" required></label>
          <label>Năm xuất bản<input name="publishedYear" type="number" min="1000" max="3000"></label>
          <label>Nhà xuất bản<input name="publisher" value="NXB Tre"></label>
          <label>Ngôn ngữ<input name="language" value="vi"></label>
          <label>Tác giả<input name="authors" value="Nguyen Nhat Anh"></label>
          <label>Thể loại<input name="categories" value="Van hoc"></label>
          <label class="wide">Tag<input name="tags" value="van-hoc, mvp"></label>
          <label class="wide">Mô tả<textarea name="description"></textarea></label>
        </div>
        <button type="submit">Lưu sách</button>
      </form>
      <form id="copyForm" class="form-card">
        <h2>Thêm bản sao</h2>
        <div class="form-grid">
          <label class="wide">Sách<select name="bookId">${state.books.map(book => `<option value="${book.id}">${escapeHtml(book.title)}</option>`).join("")}</select></label>
          <label>Chi nhánh<select name="branchId">${branchOptions()}</select></label>
          <label>Barcode<input name="barcode" placeholder="PV-MAIN-0100"></label>
          <label class="wide">Vị trí<input name="location" placeholder="Kệ A1"></label>
        </div>
        <button type="submit">Tạo bản sao</button>
      </form>
    </div>
  `;
  qs("#bookSearchBtn").onclick = async () => {
    const search = encodeURIComponent(qs("#bookSearch").value.trim());
    const branch = state.selectedBranchId ? `&branchId=${state.selectedBranchId}` : "";
    state.books = await api(`/api/catalog/books?search=${search}${branch}`);
    renderCatalog();
  };
  qs("#bookForm").onsubmit = submitBook;
  qs("#copyForm").onsubmit = submitCopy;
}

function renderCirculation() {
  qs("#circulationView").innerHTML = `
    <div class="grid cols-3">
      <form id="loanForm" class="form-card">
        <h2>Mượn sách</h2>
        <div class="stack">
          <label>Độc giả<select name="memberId">${state.members.map(x => `<option value="${x.id}">${escapeHtml(x.fullName)} · ${escapeHtml(x.code)}</option>`).join("")}</select></label>
          <label>Chi nhánh<select name="branchId">${branchOptions()}</select></label>
          <label>Barcode<input name="copyBarcode" placeholder="PV-MAIN-0002"></label>
          <button type="submit">Tạo phiếu mượn</button>
        </div>
      </form>
      <form id="returnForm" class="form-card">
        <h2>Trả sách</h2>
        <div class="stack">
          <label>Barcode<input name="copyBarcode" placeholder="PV-MAIN-0001"></label>
          <button type="submit" class="secondary">Ghi nhận trả</button>
        </div>
      </form>
      <form id="holdForm" class="form-card">
        <h2>Đặt chỗ</h2>
        <div class="stack">
          <label>Sách<select name="bookId">${state.books.map(x => `<option value="${x.id}">${escapeHtml(x.title)}</option>`).join("")}</select></label>
          <label>Độc giả<select name="memberId">${state.members.map(x => `<option value="${x.id}">${escapeHtml(x.fullName)}</option>`).join("")}</select></label>
          <label>Chi nhánh<select name="branchId">${branchOptions()}</select></label>
          <button type="submit">Tạo đặt chỗ</button>
        </div>
      </form>
    </div>
    <div class="table-card">
      <header><h2>Phiếu mượn</h2><span class="badge">${state.loans.length} records</span></header>
      <div class="table-scroll"><table>
        <thead><tr><th>Sách</th><th>Barcode</th><th>Độc giả</th><th>Hạn trả</th><th>Trạng thái</th><th>Phạt</th><th></th></tr></thead>
        <tbody>${state.loans.map(loan => `
          <tr>
            <td>${escapeHtml(loan.bookTitle)}</td><td>${escapeHtml(loan.barcode)}</td><td>${escapeHtml(loan.memberName)}</td>
            <td>${formatDate(loan.dueAt)}</td><td>${statusBadge(loan.status)}</td><td>${money(loan.fineAmount)}</td>
            <td>${loan.status === "Active" ? `<button class="ghost" data-renew="${loan.id}">Gia hạn</button>` : ""}</td>
          </tr>`).join("")}</tbody>
      </table></div>
    </div>
    <div class="table-card">
      <header><h2>Đặt chỗ</h2><span class="badge">${state.holds.length} records</span></header>
      <div class="table-scroll"><table>
        <thead><tr><th>Sách</th><th>Độc giả</th><th>Trạng thái</th><th>Ngày tạo</th><th>Hết hạn</th></tr></thead>
        <tbody>${state.holds.map(hold => `<tr><td>${escapeHtml(hold.bookTitle)}</td><td>${escapeHtml(hold.memberName)}</td><td>${statusBadge(hold.status)}</td><td>${formatDate(hold.requestedAt)}</td><td>${formatDate(hold.expiresAt)}</td></tr>`).join("")}</tbody>
      </table></div>
    </div>
  `;
  qs("#loanForm").onsubmit = submitLoan;
  qs("#returnForm").onsubmit = submitReturn;
  qs("#holdForm").onsubmit = submitHold;
  document.querySelectorAll("[data-renew]").forEach(button => {
    button.onclick = () => renewLoan(button.dataset.renew);
  });
}

function renderMembers() {
  qs("#membersView").innerHTML = `
    <div class="split">
      <div class="table-card">
        <header><h2>Độc giả</h2><span class="badge">${state.members.length} records</span></header>
        <div class="table-scroll"><table>
          <thead><tr><th>Mã</th><th>Họ tên</th><th>Email</th><th>SĐT</th><th>Chi nhánh</th><th>Trạng thái</th></tr></thead>
          <tbody>${state.members.map(member => `<tr><td>${escapeHtml(member.code)}</td><td>${escapeHtml(member.fullName)}</td><td>${escapeHtml(member.email)}</td><td>${escapeHtml(member.phone)}</td><td>${escapeHtml(member.branchName)}</td><td>${statusBadge(member.status)}</td></tr>`).join("")}</tbody>
        </table></div>
      </div>
      <form id="memberForm" class="form-card">
        <h2>Thêm độc giả</h2>
        <div class="stack">
          <label>Chi nhánh<select name="branchId">${branchOptions()}</select></label>
          <label>Mã độc giả<input name="code" placeholder="SV230500"></label>
          <label>Họ tên<input name="fullName"></label>
          <label>Email<input name="email" type="email"></label>
          <label>SĐT<input name="phone"></label>
          <button type="submit">Lưu độc giả</button>
        </div>
      </form>
    </div>
  `;
  qs("#memberForm").onsubmit = submitMember;
}

function renderAi() {
  qs("#aiView").innerHTML = `
    <div class="grid cols-2">
      <form id="aiSearchForm" class="form-card">
        <h2>Semantic search</h2>
        <div class="stack">
          <label>Câu hỏi<input name="query" value="software architecture"></label>
          <button type="submit">Tìm bằng AI</button>
        </div>
      </form>
      <form id="aiChatForm" class="form-card">
        <h2>Tenant chat</h2>
        <div class="stack">
          <label>Nội dung<textarea name="message">Tinh hinh qua han hien tai the nao?</textarea></label>
          <button type="submit" class="secondary">Hỏi AI</button>
        </div>
      </form>
    </div>
    <div id="aiResult" class="card"><h2>Kết quả</h2><p class="muted">Kết quả AI sẽ hiển thị tại đây.</p></div>
  `;
  qs("#aiSearchForm").onsubmit = submitAiSearch;
  qs("#aiChatForm").onsubmit = submitAiChat;
}

function renderAudit() {
  qs("#auditView").innerHTML = `
    <div class="grid cols-2">
      <div class="table-card">
        <header><h2>Notification</h2><span class="badge">${state.notifications.length} records</span></header>
        <div class="table-scroll"><table>
          <thead><tr><th>Thời gian</th><th>Key</th><th>Channel</th><th>Trạng thái</th></tr></thead>
          <tbody>${state.notifications.map(x => `<tr><td>${formatDate(x.createdAt)}</td><td>${escapeHtml(x.messageKey)}</td><td>${escapeHtml(x.channel)}</td><td>${statusBadge(x.status)}</td></tr>`).join("")}</tbody>
        </table></div>
      </div>
      <div class="table-card">
        <header><h2>Audit log</h2><span class="badge">${state.auditLogs.length} records</span></header>
        <div class="table-scroll"><table>
          <thead><tr><th>Thời gian</th><th>Actor</th><th>Action</th><th>Nội dung</th></tr></thead>
          <tbody>${state.auditLogs.map(x => `<tr><td>${formatDate(x.createdAt)}</td><td>${escapeHtml(x.actorName)}</td><td>${escapeHtml(x.action)}</td><td>${escapeHtml(x.summary)}</td></tr>`).join("")}</tbody>
        </table></div>
      </div>
    </div>
  `;
}

function branchOptions() {
  return state.branches.map(branch => `<option value="${branch.id}">${escapeHtml(branch.name)}</option>`).join("");
}

function empty(message) {
  return `<span class="muted">${escapeHtml(message)}</span>`;
}

function formData(form) {
  return Object.fromEntries(new FormData(form).entries());
}

async function submitBook(event) {
  event.preventDefault();
  const data = formData(event.currentTarget);
  await api("/api/catalog/books", {
    method: "POST",
    body: JSON.stringify({
      title: data.title,
      isbn: data.isbn,
      description: data.description,
      publishedYear: data.publishedYear ? Number(data.publishedYear) : null,
      language: data.language,
      publisher: data.publisher,
      authors: splitInput(data.authors),
      categories: splitInput(data.categories),
      tags: splitInput(data.tags)
    })
  });
  showToast("Đã thêm sách");
  await loadAll();
}

async function submitCopy(event) {
  event.preventDefault();
  const data = formData(event.currentTarget);
  await api("/api/catalog/copies", { method: "POST", body: JSON.stringify(data) });
  showToast("Đã thêm bản sao");
  await loadAll();
}

async function submitMember(event) {
  event.preventDefault();
  await api("/api/members", { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
  showToast("Đã thêm độc giả");
  await loadAll();
}

async function submitLoan(event) {
  event.preventDefault();
  await api("/api/circulation/loans", { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
  showToast("Đã tạo phiếu mượn");
  await loadAll();
}

async function submitReturn(event) {
  event.preventDefault();
  await api("/api/circulation/returns", { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
  showToast("Đã ghi nhận trả sách");
  await loadAll();
}

async function submitHold(event) {
  event.preventDefault();
  await api("/api/circulation/holds", { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
  showToast("Đã tạo đặt chỗ");
  await loadAll();
}

async function renewLoan(loanId) {
  await api("/api/circulation/renewals", { method: "POST", body: JSON.stringify({ loanId }) });
  showToast("Đã gia hạn");
  await loadAll();
}

async function submitAiSearch(event) {
  event.preventDefault();
  const { query } = formData(event.currentTarget);
  const result = await api("/api/ai/search", { method: "POST", body: JSON.stringify({ query }) });
  qs("#aiResult").innerHTML = `
    <h2>Kết quả semantic search</h2>
    <p class="muted">Guardrail: ${result.guardrails.map(escapeHtml).join(", ")}</p>
    <div class="list">${result.results.map(x => `<div class="list-item"><strong>${escapeHtml(x.title)}</strong><span class="muted">${escapeHtml(x.isbn)} · ${x.availableCopies} bản sẵn sàng · score ${x.score}</span><p>${escapeHtml(x.explanation)}</p></div>`).join("") || empty("Không tìm thấy kết quả")}</div>
  `;
}

async function submitAiChat(event) {
  event.preventDefault();
  const { message } = formData(event.currentTarget);
  const result = await api("/api/ai/chat", { method: "POST", body: JSON.stringify({ message }) });
  qs("#aiResult").innerHTML = `
    <h2>Tenant chat</h2>
    <p>${escapeHtml(result.answer)}</p>
    <p class="muted">Citation: ${result.citations.map(escapeHtml).join(", ")}</p>
  `;
}

function splitInput(value) {
  return String(value || "").split(",").map(x => x.trim()).filter(Boolean);
}

function wireEvents() {
  qs("#loginForm").onsubmit = async (event) => {
    event.preventDefault();
    try {
      const data = formData(event.currentTarget);
      const response = await api("/api/auth/login", { method: "POST", body: JSON.stringify(data) });
      setSession(response);
      showToast("Đăng nhập thành công");
      await loadAll();
    } catch (error) {
      showToast(error.message);
    }
  };

  qs("#logoutBtn").onclick = () => {
    clearSession();
    renderShell();
  };

  qs("#refreshBtn").onclick = () => loadAll().catch(error => showToast(error.message));
  qs("#branchFilter").onchange = async (event) => {
    state.selectedBranchId = event.target.value;
    await loadAll();
  };

  document.querySelectorAll("#nav button").forEach(button => {
    button.onclick = () => {
      state.view = button.dataset.view;
      document.querySelectorAll("#nav button").forEach(x => x.classList.toggle("active", x === button));
      renderApp();
    };
  });
}

window.addEventListener("error", (event) => showToast(event.message));
window.addEventListener("unhandledrejection", (event) => showToast(event.reason?.message || "Request failed"));

wireEvents();
renderShell();
if (state.token) {
  loadAll().catch(error => {
    clearSession();
    renderShell();
    showToast(error.message);
  });
}
