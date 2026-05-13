const state = {
  token: localStorage.getItem("qltv.token") || "",
  user: JSON.parse(localStorage.getItem("qltv.user") || "null"),
  view: "dashboard",
  tenant: null,
  branches: [],
  selectedBranchId: "",
  books: [],
  copies: [],
  users: [],
  members: [],
  loans: [],
  holds: [],
  dashboard: null,
  notifications: [],
  auditLogs: []
};

const qs = (selector) => document.querySelector(selector);
const viewIds = ["dashboard", "catalog", "users", "circulation", "members", "ai", "audit"];
const roles = ["TenantAdmin", "Librarian", "InventoryStaff", "Member"];
const copyStatuses = ["Available", "OnLoan", "Reserved", "Damaged", "Lost", "Liquidated"];

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
  const cls = text.includes("Overdue") || text.includes("Failed") || text.includes("Blocked") || text.includes("Lost") ? "bad"
    : text.includes("Available") || text.includes("Active") || text.includes("Sent") || text.includes("Ready") || text === "true" ? "ok"
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
  const loanQuery = state.selectedBranchId ? `?branchId=${state.selectedBranchId}` : "";
  const [dashboard, books, copies, members, loans, holds, notifications, auditLogs] = await Promise.all([
    api("/api/dashboard/summary"),
    api(`/api/catalog/books${branchQuery}`),
    api(`/api/catalog/copies${branchQuery}`),
    api(`/api/members${branchQuery}`),
    api(`/api/circulation/loans${loanQuery}`),
    api(`/api/circulation/holds${loanQuery}`),
    api(`/api/notifications${branchQuery}`),
    api(`/api/audit-logs${branchQuery}`)
  ]);

  const users = canManageUsers() ? await api(`/api/users${branchQuery}`) : [];
  Object.assign(state, { dashboard, books, copies, users, members, loans, holds, notifications, auditLogs });
  renderApp();
}

function renderShell() {
  const signedIn = Boolean(state.token && state.user);
  qs("#loginView").hidden = signedIn;
  qs("#appView").hidden = !signedIn;
  qs("#topbar").hidden = !signedIn;
  qs("#nav").hidden = !signedIn;

  if (!signedIn) {
    qs("#accountBox").innerHTML = "<span>Chua dang nhap</span>";
    return;
  }

  qs("#accountBox").innerHTML = `
    <strong>${escapeHtml(state.user.fullName)}</strong>
    <span>${escapeHtml(state.user.email)}</span>
    <span>${escapeHtml(state.user.role)} - ${escapeHtml(state.user.locale)}</span>
  `;
}

function renderBranchFilter() {
  const select = qs("#branchFilter");
  select.innerHTML = `<option value="">Tat ca chi nhanh</option>` + state.branches.map(branch =>
    `<option value="${branch.id}" ${branch.id === state.selectedBranchId ? "selected" : ""}>${escapeHtml(branch.name)}</option>`
  ).join("");
}

function renderApp() {
  renderShell();
  if (!state.user) return;
  document.querySelector('[data-view="users"]').hidden = !canManageUsers();
  if (state.view === "users" && !canManageUsers()) {
    state.view = "dashboard";
  }
  qs("#tenantKey").textContent = `${state.tenant?.key || state.user.tenantKey} - ${state.tenant?.plan || "MVP"}`;
  qs("#pageTitle").textContent = ({
    dashboard: "Dashboard",
    catalog: "Catalog",
    users: "Quan ly nguoi dung",
    circulation: "Muon tra",
    members: "Doc gia",
    ai: "AI Search",
    audit: "Audit log"
  })[state.view];
  renderBranchFilter();
  renderDashboard();
  renderCatalog();
  renderUsers();
  renderCirculation();
  renderMembers();
  renderAi();
  renderAudit();
  for (const id of viewIds) qs(`#${id}View`).hidden = id !== state.view;
}

function canManageUsers() {
  return state.user?.role === "SuperAdmin" || state.user?.role === "TenantAdmin";
}

function renderDashboard() {
  const data = state.dashboard;
  if (!data) return;
  qs("#dashboardView").innerHTML = `
    <div class="grid cols-4">
      ${stat("Dau sach", data.bookCount)}
      ${stat("Ban sao", data.copyCount)}
      ${stat("San sang", data.availableCopies)}
      ${stat("Qua han", data.overdueLoans)}
    </div>
    <div class="split">
      <div class="table-card">
        <header><h2>KPI chi nhanh</h2><span class="badge">${data.memberCount} doc gia</span></header>
        <div class="table-scroll"><table>
          <thead><tr><th>Chi nhanh</th><th>Ban sao</th><th>Dang muon</th><th>Qua han</th></tr></thead>
          <tbody>${data.branches.map(x => `<tr><td>${escapeHtml(x.branchName)}</td><td>${x.copies}</td><td>${x.activeLoans}</td><td>${x.overdueLoans}</td></tr>`).join("")}</tbody>
        </table></div>
      </div>
      <div class="card">
        <h2>AI Insight</h2>
        <p class="muted">Co ${data.loanedCopies} ban dang muon, ${data.overdueLoans} luot qua han, tien phat mo ${money(data.openFineAmount)}.</p>
        <div class="list">${data.popularBooks.map(x => `<div class="list-item"><strong>${escapeHtml(x.title)}</strong><span class="muted">${x.loanCount} luot muon</span></div>`).join("") || empty("Chua co du lieu muon")}</div>
      </div>
    </div>
    <div class="table-card">
      <header><h2>Hoat dong gan day</h2><span class="badge">Audit scoped</span></header>
      <div class="table-scroll"><table>
        <thead><tr><th>Thoi gian</th><th>Hanh dong</th><th>Noi dung</th></tr></thead>
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
        <h2>Danh muc sach</h2>
        <div class="table-tools">
          <input id="bookSearch" placeholder="Tim theo ten, ISBN, tag..." value="${escapeHtml(qs("#bookSearch")?.value || "")}">
          <button id="bookSearchBtn" class="secondary">Tim</button>
        </div>
      </header>
      <div class="table-scroll"><table>
        <thead><tr><th>Sach</th><th>Tac gia</th><th>The loai</th><th>ISBN</th><th>Ban sao</th><th>San sang</th><th></th></tr></thead>
        <tbody>${state.books.map(book => `
          <tr>
            <td><strong>${escapeHtml(book.title)}</strong><br><span class="muted">${escapeHtml(book.description)}</span></td>
            <td>${book.authors.map(escapeHtml).join(", ")}</td>
            <td>${book.categories.map(escapeHtml).join(", ")}</td>
            <td>${escapeHtml(book.isbn)}</td>
            <td>${book.totalCopies}</td>
            <td>${book.availableCopies}</td>
            <td class="actions">
              <button class="ghost" data-edit-book="${book.id}">Sua</button>
              <button class="danger" data-delete-book="${book.id}">Xoa</button>
            </td>
          </tr>`).join("") || `<tr><td colspan="7">${empty("Khong co sach phu hop")}</td></tr>`}
        </tbody>
      </table></div>
    </div>
    <div class="grid cols-2">
      <form id="bookForm" class="form-card">
        <h2 id="bookFormTitle">Them sach</h2>
        <input type="hidden" name="id">
        <div class="form-grid">
          <label class="wide">Ten sach<input name="title" required></label>
          <label>ISBN<input name="isbn" required></label>
          <label>Nam xuat ban<input name="publishedYear" type="number" min="1000" max="3000"></label>
          <label>Nha xuat ban<input name="publisher" value="NXB Tre"></label>
          <label>Ngon ngu<input name="language" value="vi"></label>
          <label>Tac gia<input name="authors" value="Nguyen Nhat Anh"></label>
          <label>The loai<input name="categories" value="Van hoc"></label>
          <label class="wide">Tag<input name="tags" value="van-hoc, mvp"></label>
          <label class="wide">Mo ta<textarea name="description"></textarea></label>
        </div>
        <div class="actions">
          <button type="submit">Luu sach</button>
          <button type="button" class="ghost" id="bookCancelBtn">Huy sua</button>
        </div>
      </form>
      <form id="copyForm" class="form-card">
        <h2 id="copyFormTitle">Them ban sao</h2>
        <input type="hidden" name="id">
        <div class="form-grid">
          <label class="wide">Sach<select name="bookId">${state.books.map(book => `<option value="${book.id}">${escapeHtml(book.title)}</option>`).join("")}</select></label>
          <label>Chi nhanh<select name="branchId">${branchOptions()}</select></label>
          <label>Barcode<input name="barcode" placeholder="PV-MAIN-0100"></label>
          <label>Status<select name="status">${copyStatuses.map(x => `<option value="${x}">${x}</option>`).join("")}</select></label>
          <label class="wide">Vi tri<input name="location" placeholder="Ke A1"></label>
        </div>
        <div class="actions">
          <button type="submit">Luu ban sao</button>
          <button type="button" class="ghost" id="copyCancelBtn">Huy sua</button>
        </div>
      </form>
    </div>
    <div class="table-card">
      <header><h2>Ban sao sach</h2><span class="badge">${state.copies.length} records</span></header>
      <div class="table-scroll"><table>
        <thead><tr><th>Barcode</th><th>Sach</th><th>Chi nhanh</th><th>Vi tri</th><th>Status</th><th></th></tr></thead>
        <tbody>${state.copies.map(copy => `
          <tr>
            <td>${escapeHtml(copy.barcode)}</td>
            <td>${escapeHtml(bookTitle(copy.bookId))}</td>
            <td>${escapeHtml(copy.branchName)}</td>
            <td>${escapeHtml(copy.location)}</td>
            <td>${statusBadge(copy.status)}</td>
            <td class="actions">
              <button class="ghost" data-edit-copy="${copy.id}">Sua</button>
              <button class="danger" data-delete-copy="${copy.id}">Xoa</button>
            </td>
          </tr>`).join("") || `<tr><td colspan="6">${empty("Chua co ban sao")}</td></tr>`}
        </tbody>
      </table></div>
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
  qs("#bookCancelBtn").onclick = () => resetBookForm();
  qs("#copyCancelBtn").onclick = () => resetCopyForm();
  document.querySelectorAll("[data-edit-book]").forEach(button => button.onclick = () => editBook(button.dataset.editBook));
  document.querySelectorAll("[data-delete-book]").forEach(button => button.onclick = () => deleteBook(button.dataset.deleteBook));
  document.querySelectorAll("[data-edit-copy]").forEach(button => button.onclick = () => editCopy(button.dataset.editCopy));
  document.querySelectorAll("[data-delete-copy]").forEach(button => button.onclick = () => deleteCopy(button.dataset.deleteCopy));
}

function renderUsers() {
  qs("#usersView").innerHTML = `
    <div class="split">
      <div class="table-card">
        <header>
          <h2>Nguoi dung</h2>
          <div class="table-tools">
            <input id="userSearch" placeholder="Tim ten, email, vai tro..." value="${escapeHtml(qs("#userSearch")?.value || "")}">
            <button id="userSearchBtn" class="secondary">Tim</button>
          </div>
        </header>
        <div class="table-scroll"><table>
          <thead><tr><th>Ho ten</th><th>Email</th><th>Vai tro</th><th>Chi nhanh</th><th>Locale</th><th>Status</th><th></th></tr></thead>
          <tbody>${state.users.map(user => `
            <tr>
              <td>${escapeHtml(user.fullName)}</td>
              <td>${escapeHtml(user.email)}</td>
              <td>${escapeHtml(user.role)}</td>
              <td>${escapeHtml(user.branchNames.join(", ") || "All")}</td>
              <td>${escapeHtml(user.locale)}</td>
              <td>${statusBadge(user.isActive ? "Active" : "Inactive")}</td>
              <td class="actions">
                <button class="ghost" data-edit-user="${user.id}">Sua</button>
                <button class="danger" data-delete-user="${user.id}">Xoa</button>
              </td>
            </tr>`).join("") || `<tr><td colspan="7">${empty("Chua co nguoi dung")}</td></tr>`}
          </tbody>
        </table></div>
      </div>
      <form id="userForm" class="form-card">
        <h2 id="userFormTitle">Them nguoi dung</h2>
        <input type="hidden" name="id">
        <div class="stack">
          <label>Ho ten<input name="fullName" required></label>
          <label>Email<input name="email" type="email" required></label>
          <label>Mat khau<input name="password" type="password" placeholder="Nhap khi tao moi hoac doi mat khau"></label>
          <label>Vai tro<select name="role">${roles.map(role => `<option value="${role}">${role}</option>`).join("")}</select></label>
          <label>Chi nhanh<select name="branchIds" multiple size="4">${branchOptions()}</select></label>
          <label>Locale<input name="locale" value="vi"></label>
          <label><input name="isActive" type="checkbox" checked> Dang hoat dong</label>
          <div class="actions">
            <button type="submit">Luu nguoi dung</button>
            <button type="button" class="ghost" id="userCancelBtn">Huy sua</button>
          </div>
        </div>
      </form>
    </div>
  `;
  qs("#userForm").onsubmit = submitUser;
  qs("#userCancelBtn").onclick = () => resetUserForm();
  qs("#userSearchBtn").onclick = async () => {
    const search = encodeURIComponent(qs("#userSearch").value.trim());
    const branch = state.selectedBranchId ? `&branchId=${state.selectedBranchId}` : "";
    state.users = await api(`/api/users?search=${search}${branch}`);
    renderUsers();
  };
  document.querySelectorAll("[data-edit-user]").forEach(button => button.onclick = () => editUser(button.dataset.editUser));
  document.querySelectorAll("[data-delete-user]").forEach(button => button.onclick = () => deleteUser(button.dataset.deleteUser));
}

function renderCirculation() {
  qs("#circulationView").innerHTML = `
    <div class="grid cols-3">
      <form id="loanForm" class="form-card">
        <h2>Muon sach</h2>
        <div class="stack">
          <label>Doc gia<select name="memberId">${state.members.map(x => `<option value="${x.id}">${escapeHtml(x.fullName)} - ${escapeHtml(x.code)}</option>`).join("")}</select></label>
          <label>Chi nhanh<select name="branchId">${branchOptions()}</select></label>
          <label>Barcode<input name="copyBarcode" placeholder="PV-MAIN-0002"></label>
          <button type="submit">Tao phieu muon</button>
        </div>
      </form>
      <form id="returnForm" class="form-card">
        <h2>Tra sach</h2>
        <div class="stack">
          <label>Barcode<input name="copyBarcode" placeholder="PV-MAIN-0001"></label>
          <button type="submit" class="secondary">Ghi nhan tra</button>
        </div>
      </form>
      <form id="holdForm" class="form-card">
        <h2>Dat cho</h2>
        <div class="stack">
          <label>Sach<select name="bookId">${state.books.map(x => `<option value="${x.id}">${escapeHtml(x.title)}</option>`).join("")}</select></label>
          <label>Doc gia<select name="memberId">${state.members.map(x => `<option value="${x.id}">${escapeHtml(x.fullName)}</option>`).join("")}</select></label>
          <label>Chi nhanh<select name="branchId">${branchOptions()}</select></label>
          <button type="submit">Tao dat cho</button>
        </div>
      </form>
    </div>
    <div class="table-card">
      <header><h2>Phieu muon</h2><span class="badge">${state.loans.length} records</span></header>
      <div class="table-scroll"><table>
        <thead><tr><th>Sach</th><th>Barcode</th><th>Doc gia</th><th>Han tra</th><th>Trang thai</th><th>Phat</th><th></th></tr></thead>
        <tbody>${state.loans.map(loan => `
          <tr>
            <td>${escapeHtml(loan.bookTitle)}</td><td>${escapeHtml(loan.barcode)}</td><td>${escapeHtml(loan.memberName)}</td>
            <td>${formatDate(loan.dueAt)}</td><td>${statusBadge(loan.status)}</td><td>${money(loan.fineAmount)}</td>
            <td>${loan.status === "Active" ? `<button class="ghost" data-renew="${loan.id}">Gia han</button>` : ""}</td>
          </tr>`).join("")}</tbody>
      </table></div>
    </div>
    <div class="table-card">
      <header><h2>Dat cho</h2><span class="badge">${state.holds.length} records</span></header>
      <div class="table-scroll"><table>
        <thead><tr><th>Sach</th><th>Doc gia</th><th>Trang thai</th><th>Ngay tao</th><th>Het han</th></tr></thead>
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
        <header><h2>Doc gia</h2><span class="badge">${state.members.length} records</span></header>
        <div class="table-scroll"><table>
          <thead><tr><th>Ma</th><th>Ho ten</th><th>Email</th><th>SDT</th><th>Chi nhanh</th><th>Trang thai</th></tr></thead>
          <tbody>${state.members.map(member => `<tr><td>${escapeHtml(member.code)}</td><td>${escapeHtml(member.fullName)}</td><td>${escapeHtml(member.email)}</td><td>${escapeHtml(member.phone)}</td><td>${escapeHtml(member.branchName)}</td><td>${statusBadge(member.status)}</td></tr>`).join("")}</tbody>
        </table></div>
      </div>
      <form id="memberForm" class="form-card">
        <h2>Them doc gia</h2>
        <div class="stack">
          <label>Chi nhanh<select name="branchId">${branchOptions()}</select></label>
          <label>Ma doc gia<input name="code" placeholder="SV230500"></label>
          <label>Ho ten<input name="fullName"></label>
          <label>Email<input name="email" type="email"></label>
          <label>SDT<input name="phone"></label>
          <button type="submit">Luu doc gia</button>
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
          <label>Cau hoi<input name="query" value="software architecture"></label>
          <button type="submit">Tim bang AI</button>
        </div>
      </form>
      <form id="aiChatForm" class="form-card">
        <h2>Tenant chat</h2>
        <div class="stack">
          <label>Noi dung<textarea name="message">Tinh hinh qua han hien tai the nao?</textarea></label>
          <button type="submit" class="secondary">Hoi AI</button>
        </div>
      </form>
    </div>
    <div id="aiResult" class="card"><h2>Ket qua</h2><p class="muted">Ket qua AI se hien thi tai day.</p></div>
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
          <thead><tr><th>Thoi gian</th><th>Key</th><th>Channel</th><th>Trang thai</th></tr></thead>
          <tbody>${state.notifications.map(x => `<tr><td>${formatDate(x.createdAt)}</td><td>${escapeHtml(x.messageKey)}</td><td>${escapeHtml(x.channel)}</td><td>${statusBadge(x.status)}</td></tr>`).join("")}</tbody>
        </table></div>
      </div>
      <div class="table-card">
        <header><h2>Audit log</h2><span class="badge">${state.auditLogs.length} records</span></header>
        <div class="table-scroll"><table>
          <thead><tr><th>Thoi gian</th><th>Actor</th><th>Action</th><th>Noi dung</th></tr></thead>
          <tbody>${state.auditLogs.map(x => `<tr><td>${formatDate(x.createdAt)}</td><td>${escapeHtml(x.actorName)}</td><td>${escapeHtml(x.action)}</td><td>${escapeHtml(x.summary)}</td></tr>`).join("")}</tbody>
        </table></div>
      </div>
    </div>
  `;
}

function branchOptions(selectedIds = []) {
  return state.branches.map(branch => `<option value="${branch.id}" ${selectedIds.includes(branch.id) ? "selected" : ""}>${escapeHtml(branch.name)}</option>`).join("");
}

function bookTitle(bookId) {
  return state.books.find(book => book.id === bookId)?.title || "";
}

function empty(message) {
  return `<span class="muted">${escapeHtml(message)}</span>`;
}

function formData(form) {
  return Object.fromEntries(new FormData(form).entries());
}

function selectedValues(select) {
  return Array.from(select.selectedOptions).map(option => option.value);
}

function bookPayload(data) {
  return {
    title: data.title,
    isbn: data.isbn,
    description: data.description,
    publishedYear: data.publishedYear ? Number(data.publishedYear) : null,
    language: data.language,
    publisher: data.publisher,
    authors: splitInput(data.authors),
    categories: splitInput(data.categories),
    tags: splitInput(data.tags)
  };
}

async function submitBook(event) {
  event.preventDefault();
  const data = formData(event.currentTarget);
  const id = data.id;
  await api(id ? `/api/catalog/books/${id}` : "/api/catalog/books", {
    method: id ? "PUT" : "POST",
    body: JSON.stringify(bookPayload(data))
  });
  showToast(id ? "Da cap nhat sach" : "Da them sach");
  await loadAll();
}

async function submitCopy(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = formData(form);
  const id = data.id;
  const payload = id
    ? { branchId: data.branchId, barcode: data.barcode, location: data.location, status: data.status }
    : { bookId: data.bookId, branchId: data.branchId, barcode: data.barcode, location: data.location };

  await api(id ? `/api/catalog/copies/${id}` : "/api/catalog/copies", {
    method: id ? "PUT" : "POST",
    body: JSON.stringify(payload)
  });
  showToast(id ? "Da cap nhat ban sao" : "Da them ban sao");
  await loadAll();
}

function editBook(id) {
  const book = state.books.find(x => x.id === id);
  if (!book) return;
  const form = qs("#bookForm");
  form.elements.id.value = book.id;
  form.elements.title.value = book.title;
  form.elements.isbn.value = book.isbn;
  form.elements.publishedYear.value = book.publishedYear || "";
  form.elements.publisher.value = book.publisher;
  form.elements.language.value = book.language;
  form.elements.authors.value = book.authors.join(", ");
  form.elements.categories.value = book.categories.join(", ");
  form.elements.tags.value = book.tags.join(", ");
  form.elements.description.value = book.description;
  qs("#bookFormTitle").textContent = "Sua sach";
}

async function deleteBook(id) {
  if (!confirm("Xoa sach nay? Neu sach con ban sao, hay xoa ban sao truoc.")) return;
  await api(`/api/catalog/books/${id}`, { method: "DELETE" });
  showToast("Da xoa sach");
  await loadAll();
}

function resetBookForm() {
  qs("#bookForm").reset();
  qs("#bookForm").elements.id.value = "";
  qs("#bookFormTitle").textContent = "Them sach";
}

function editCopy(id) {
  const copy = state.copies.find(x => x.id === id);
  if (!copy) return;
  const form = qs("#copyForm");
  form.elements.id.value = copy.id;
  form.elements.bookId.value = copy.bookId;
  form.elements.bookId.disabled = true;
  form.elements.branchId.value = copy.branchId;
  form.elements.barcode.value = copy.barcode;
  form.elements.status.value = copy.status;
  form.elements.location.value = copy.location;
  qs("#copyFormTitle").textContent = "Sua ban sao";
}

async function deleteCopy(id) {
  if (!confirm("Xoa ban sao nay?")) return;
  await api(`/api/catalog/copies/${id}`, { method: "DELETE" });
  showToast("Da xoa ban sao");
  await loadAll();
}

function resetCopyForm() {
  qs("#copyForm").reset();
  qs("#copyForm").elements.id.value = "";
  qs("#copyForm").elements.bookId.disabled = false;
  qs("#copyFormTitle").textContent = "Them ban sao";
}

async function submitUser(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const data = formData(form);
  const id = data.id;
  const payload = {
    fullName: data.fullName,
    email: data.email,
    password: data.password || null,
    role: data.role,
    branchIds: selectedValues(form.elements.branchIds),
    locale: data.locale || "vi",
    isActive: form.elements.isActive.checked
  };

  if (!id && !payload.password) {
    showToast("Mat khau la bat buoc khi tao user");
    return;
  }

  await api(id ? `/api/users/${id}` : "/api/users", {
    method: id ? "PUT" : "POST",
    body: JSON.stringify(payload)
  });
  showToast(id ? "Da cap nhat nguoi dung" : "Da them nguoi dung");
  await loadAll();
}

function editUser(id) {
  const user = state.users.find(x => x.id === id);
  if (!user) return;
  const form = qs("#userForm");
  form.elements.id.value = user.id;
  form.elements.fullName.value = user.fullName;
  form.elements.email.value = user.email;
  form.elements.password.value = "";
  form.elements.role.value = user.role;
  form.elements.locale.value = user.locale;
  form.elements.isActive.checked = user.isActive;
  Array.from(form.elements.branchIds.options).forEach(option => {
    option.selected = user.branchIds.includes(option.value);
  });
  qs("#userFormTitle").textContent = "Sua nguoi dung";
}

async function deleteUser(id) {
  if (!confirm("Xoa nguoi dung nay?")) return;
  await api(`/api/users/${id}`, { method: "DELETE" });
  showToast("Da xoa nguoi dung");
  await loadAll();
}

function resetUserForm() {
  qs("#userForm").reset();
  qs("#userForm").elements.id.value = "";
  qs("#userFormTitle").textContent = "Them nguoi dung";
}

async function submitMember(event) {
  event.preventDefault();
  await api("/api/members", { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
  showToast("Da them doc gia");
  await loadAll();
}

async function submitLoan(event) {
  event.preventDefault();
  await api("/api/circulation/loans", { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
  showToast("Da tao phieu muon");
  await loadAll();
}

async function submitReturn(event) {
  event.preventDefault();
  await api("/api/circulation/returns", { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
  showToast("Da ghi nhan tra sach");
  await loadAll();
}

async function submitHold(event) {
  event.preventDefault();
  await api("/api/circulation/holds", { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
  showToast("Da tao dat cho");
  await loadAll();
}

async function renewLoan(loanId) {
  await api("/api/circulation/renewals", { method: "POST", body: JSON.stringify({ loanId }) });
  showToast("Da gia han");
  await loadAll();
}

async function submitAiSearch(event) {
  event.preventDefault();
  const { query } = formData(event.currentTarget);
  const result = await api("/api/ai/search", { method: "POST", body: JSON.stringify({ query }) });
  qs("#aiResult").innerHTML = `
    <h2>Ket qua semantic search</h2>
    <p class="muted">Guardrail: ${result.guardrails.map(escapeHtml).join(", ")}</p>
    <div class="list">${result.results.map(x => `<div class="list-item"><strong>${escapeHtml(x.title)}</strong><span class="muted">${escapeHtml(x.isbn)} - ${x.availableCopies} ban san sang - score ${x.score}</span><p>${escapeHtml(x.explanation)}</p></div>`).join("") || empty("Khong tim thay ket qua")}</div>
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
      showToast("Dang nhap thanh cong");
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
