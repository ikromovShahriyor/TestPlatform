const API_BASE = localStorage.getItem('tp_api_base') || (window.location.origin.includes('github.io') ? (window.TP_API_URL || 'http://localhost:5005/api') : '/api');

// Global state
let token = localStorage.getItem('tp_token') || null;
let currentUser = null; // { name: string, email: string, role: 'Admin' | 'Student' }
try {
  const savedUser = localStorage.getItem('tp_user');
  if (savedUser) currentUser = JSON.parse(savedUser);
} catch (e) {
  currentUser = null;
}

let activeAuthTab = 'login';
let currentStudentTest = null;
let currentAttemptId = null;
let currentQuestionIndex = 0;
let studentAnswersMap = {}; // questionId -> selectedOptionId
let quizTimerInterval = null;
let quizRemainingSeconds = 0;
let testTotalDurationSeconds = 0;

// Pagination states
let subjectsPageState = { page: 1, pageSize: 6, totalPages: 1, search: '' };
let testsPageState = { page: 1, pageSize: 6, totalPages: 1, search: '', subjectId: '', difficulty: '', topicId: '' };
let studentTestsPageState = { page: 1, pageSize: 6, totalPages: 1, search: '', subjectId: '', difficulty: '', topicId: '' };
let auditLogsPageState = { page: 1, pageSize: 10, totalPages: 1, action: '', entityName: '' };
let currentCertificate = null;

// ==================== API HELPERS ====================

async function fetchWithAuth(url, options = {}) {
  options.headers = options.headers || {};
  if (token) {
    options.headers['Authorization'] = `Bearer ${token}`;
  }
  
  if (options.body && typeof options.body === 'object' && !(options.body instanceof FormData)) {
    options.headers['Content-Type'] = 'application/json';
    options.body = JSON.stringify(options.body);
  }

  const res = await fetch(url, options);
  
  if (res.status === 401) {
    // Session expired - clear and redirect to login
    token = null;
    currentUser = null;
    localStorage.removeItem('tp_token');
    localStorage.removeItem('tp_user');
    clearQuizTimer();
    applyUserSession();
    alert("Sessiya tugadi. Iltimos qayta kiring.");
    throw new Error("Unauthorized");
  }
  if (res.status === 403) {
    throw new Error("Bu amalni bajarish uchun ruxsat yo'q (403 Forbidden)");
  }

  return res;
}

// ==================== AUTH & TAB NAVIGATION ====================

function switchAuthTab(tab) {
  activeAuthTab = tab;
  const btnLogin = document.getElementById('auth-tab-login');
  const btnRegister = document.getElementById('auth-tab-register');
  const viewLogin = document.getElementById('form-login-view');
  const viewRegister = document.getElementById('form-register-view');

  if (tab === 'login') {
    btnLogin?.classList.add('active');
    btnRegister?.classList.remove('active');
    if (viewLogin) viewLogin.style.display = 'block';
    if (viewRegister) viewRegister.style.display = 'none';
  } else {
    btnLogin?.classList.remove('active');
    btnRegister?.classList.add('active');
    if (viewLogin) viewLogin.style.display = 'none';
    if (viewRegister) viewRegister.style.display = 'block';
  }
}

async function handleLoginSubmit() {
  const emailEl = document.getElementById('login-email');
  const passwordEl = document.getElementById('login-password');
  
  if (!emailEl || !passwordEl || !emailEl.value.trim() || !passwordEl.value.trim()) {
    return alert('Email va parolni to\'ldiring!');
  }

  try {
    const res = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: emailEl.value.trim(),
        password: passwordEl.value
      })
    });

    const text = await res.text();
    let data = {};
    try { data = text ? JSON.parse(text) : {}; } catch { data = { message: text || 'Serverda xatolik yuz berdi' }; }

    if (!res.ok) throw new Error(data.message || 'Kirishda xatolik yuz berdi!');

    token = data.token;
    currentUser = {
      name: data.user.fullName,
      email: data.user.email,
      role: data.user.role // 'Admin' or 'Student'
    };

    localStorage.setItem('tp_token', token);
    localStorage.setItem('tp_user', JSON.stringify(currentUser));

    applyUserSession();
    alert(`Xush kelibsiz, ${currentUser.name}! 🎉`);
  } catch (err) {
    alert(err.message);
  }
}

async function handleSendOtpClick() {
  const emailEl = document.getElementById('register-email');
  const codeEl = document.getElementById('register-code');
  const btnSend = document.getElementById('btn-send-otp');

  if (!emailEl || !emailEl.value.trim() || !emailEl.value.includes('@')) {
    return alert('Iltimos, to\'g\'ri va to\'liq Gmail manzilingizni kiriting! (Masalan: ali@gmail.com)');
  }

  try {
    if (btnSend) {
      btnSend.disabled = true;
      btnSend.innerText = '⏳ Yuborilmoqda...';
    }

    const res = await fetch(`${API_BASE}/auth/send-otp`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: emailEl.value.trim() })
    });

    const text = await res.text();
    let data = {};
    try { data = JSON.parse(text); } catch { data = { message: text || 'Serverda xatolik yuz berdi' }; }

    if (!res.ok) throw new Error(data.message || 'Kod yuborishda xatolik!');

    if (data.code && codeEl) {
      codeEl.value = data.code;
    }

    alert(`📩 ${data.message}`);
  } catch (err) {
    alert(err.message);
  } finally {
    if (btnSend) {
      btnSend.disabled = false;
      btnSend.innerText = '📨 Kod yuborish';
    }
  }
}

async function handleRegisterSubmit() {
  const fullNameEl = document.getElementById('register-fullname');
  const emailEl = document.getElementById('register-email');
  const codeEl = document.getElementById('register-code');
  const passwordEl = document.getElementById('register-password');

  if (!fullNameEl || !emailEl || !codeEl || !passwordEl || !fullNameEl.value.trim() || !emailEl.value.trim() || !codeEl.value.trim() || !passwordEl.value.trim()) {
    return alert('Barcha maydonlarni, shu jumladan Gmail tasdiqlash kodini to\'ldiring!');
  }

  try {
    const res = await fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        fullName: fullNameEl.value.trim(),
        email: emailEl.value.trim(),
        code: codeEl.value.trim(),
        password: passwordEl.value,
        role: 'Student'
      })
    });

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Ro\'yxatdan o\'tishda xatolik yuz berdi!');

    token = data.token;
    currentUser = {
      name: data.user.fullName,
      email: data.user.email,
      role: data.user.role,
      avatarUrl: data.user.avatarUrl
    };

    localStorage.setItem('tp_token', token);
    localStorage.setItem('tp_user', JSON.stringify(currentUser));

    applyUserSession();
    alert('Muvaffaqiyatli ro\'yxatdan o\'tdingiz! 🎉');
  } catch (err) {
    alert(err.message);
  }
}

function logout() {
  token = null;
  currentUser = null;
  localStorage.removeItem('tp_token');
  localStorage.removeItem('tp_user');
  
  // Clear timer
  clearQuizTimer();
  
  applyUserSession();
}

function switchToStudentPortalView() {
  const viewAdmin = document.getElementById('view-admin');
  const viewStudent = document.getElementById('view-student');
  const navBtn = document.querySelector('#admin-nav-links .nav-btn:nth-child(2)');
  const adminBtn = document.querySelector('#admin-nav-links .nav-btn:nth-child(1)');

  if (viewAdmin && viewStudent) {
    if (viewAdmin.style.display !== 'none') {
      viewAdmin.style.display = 'none';
      viewStudent.style.display = 'block';
      navBtn?.classList.add('active');
      adminBtn?.classList.remove('active');
      switchStudentTab('available-tests');
    } else {
      viewAdmin.style.display = 'block';
      viewStudent.style.display = 'none';
      navBtn?.classList.remove('active');
      adminBtn?.classList.add('active');
      switchAdminTab('dashboard');
    }
  }
}

function applyUserSession() {
  const nav = document.getElementById('navbar');
  const viewLogin = document.getElementById('view-login');
  const viewAdmin = document.getElementById('view-admin');
  const viewStudent = document.getElementById('view-student');
  const adminNav = document.getElementById('admin-nav-links');
  const studentNav = document.getElementById('student-nav-links');
  const roleTag = document.getElementById('user-role-tag');
  const userName = document.getElementById('user-display-name');

  if (!token || !currentUser) {
    if (nav) nav.style.display = 'none';
    if (viewLogin) viewLogin.style.display = 'flex';
    if (viewAdmin) viewAdmin.style.display = 'none';
    if (viewStudent) viewStudent.style.display = 'none';
    switchAuthTab('login');
    return;
  }

  if (nav) nav.style.display = 'flex';
  if (viewLogin) viewLogin.style.display = 'none';
  if (userName) userName.innerText = currentUser.name;

  if (currentUser.role === 'Admin') {
    if (roleTag) {
      roleTag.innerText = 'ADMIN';
      roleTag.className = 'role-badge';
    }
    if (adminNav) adminNav.style.display = 'flex';
    if (studentNav) studentNav.style.display = 'none';
    if (viewAdmin) viewAdmin.style.display = 'block';
    if (viewStudent) viewStudent.style.display = 'none';
    switchAdminTab('dashboard');
  } else {
    if (roleTag) {
      roleTag.innerText = 'TALABA';
      roleTag.className = 'role-badge student-badge';
    }
    if (adminNav) adminNav.style.display = 'none';
    if (studentNav) studentNav.style.display = 'flex';
    if (viewAdmin) viewAdmin.style.display = 'none';
    if (viewStudent) viewStudent.style.display = 'block';
    switchStudentTab('available-tests');
  }
}

// ==================== ADMIN DASHBOARD TAB ====================

async function loadDashboardStats() {
  try {
    const [summaryRes, topRes, recentRes] = await Promise.all([
      fetchWithAuth(`${API_BASE}/dashboard/summary`),
      fetchWithAuth(`${API_BASE}/dashboard/top-tests?count=5`),
      fetchWithAuth(`${API_BASE}/dashboard/recent-attempts?count=5`)
    ]);

    const summary = await summaryRes.json();
    const topTests = await topRes.json();
    const recentAttempts = await recentRes.json();

    // Populate summary
    document.getElementById('dashboard-subjects-count').innerText = summary.subjectsCount;
    document.getElementById('dashboard-tests-count').innerText = summary.testsCount;
    document.getElementById('dashboard-published-count').innerText = summary.publishedTestsCount;
    document.getElementById('dashboard-questions-count').innerText = summary.questionsCount;
    document.getElementById('dashboard-attempts-count').innerText = summary.attemptsCount;
    document.getElementById('dashboard-avg-percentage').innerText = `${summary.averagePercentage}%`;
    document.getElementById('dashboard-passed-count').innerText = summary.passedAttemptsCount;
    document.getElementById('dashboard-failed-count').innerText = summary.failedAttemptsCount;

    // Populate recent attempts
    const recentTbody = document.getElementById('dashboard-recent-tbody');
    if (recentTbody) {
      if (recentAttempts.length === 0) {
        recentTbody.innerHTML = '<tr><td colspan="4" class="empty-state">Hali urinishlar yo\'q</td></tr>';
      } else {
        recentTbody.innerHTML = recentAttempts.map(a => {
          const statusBadge = a.isPassed ? 
            '<span class="badge-status-passed">O\'tgan</span>' : 
            '<span class="badge-status-failed">Yiqilgan</span>';
          return `
            <tr>
              <td><strong>${escapeHtml(a.studentName)}</strong></td>
              <td>${escapeHtml(a.testTitle)}</td>
              <td><strong>${a.percentage}%</strong></td>
              <td>${statusBadge}</td>
            </tr>
          `;
        }).join('');
      }
    }

    // Populate top tests
    const topTbody = document.getElementById('dashboard-top-tbody');
    if (topTbody) {
      if (topTests.length === 0) {
        topTbody.innerHTML = '<tr><td colspan="4" class="empty-state">Ma\'lumotlar mavjud emas</td></tr>';
      } else {
        topTbody.innerHTML = topTests.map(t => `
          <tr>
            <td><strong>${escapeHtml(t.title)}</strong></td>
            <td>${escapeHtml(t.subjectName)}</td>
            <td>${t.attemptsCount} ta</td>
            <td><strong>${t.averagePercentage}%</strong></td>
          </tr>
        `).join('');
      }
    }
  } catch (err) {
    console.error('Stats loading error:', err);
  }
}

// ==================== TABS SWITCHER ====================

function switchAdminTab(tabName) {
  document.querySelectorAll('.admin-tab-content').forEach(el => el.style.display = 'none');
  
  const btnDash = document.getElementById('atab-btn-dashboard');
  const btnSub = document.getElementById('atab-btn-subjects');
  const btnTopics = document.getElementById('atab-btn-topics');
  const btnTests = document.getElementById('atab-btn-tests');
  const btnResults = document.getElementById('atab-btn-results');
  const btnLeaderboard = document.getElementById('atab-btn-leaderboard');
  const btnAudit = document.getElementById('atab-btn-auditlogs');

  [btnDash, btnSub, btnTopics, btnTests, btnResults, btnLeaderboard, btnAudit].forEach(btn => btn?.classList.remove('active'));

  if (tabName === 'dashboard') {
    const tabEl = document.getElementById('admin-tab-dashboard');
    if (tabEl) tabEl.style.display = 'block';
    btnDash?.classList.add('active');
    loadDashboardStats();
  } else if (tabName === 'subjects') {
    const tabEl = document.getElementById('admin-tab-subjects');
    if (tabEl) tabEl.style.display = 'block';
    btnSub?.classList.add('active');
    loadSubjects();
  } else if (tabName === 'topics') {
    const tabEl = document.getElementById('admin-tab-topics');
    if (tabEl) tabEl.style.display = 'block';
    btnTopics?.classList.add('active');
    loadTopics();
  } else if (tabName === 'tests') {
    const tabEl = document.getElementById('admin-tab-tests');
    if (tabEl) tabEl.style.display = 'block';
    btnTests?.classList.add('active');
    loadTests();
    loadFilterSubjects();
    loadTopicFilterDropdowns();
  } else if (tabName === 'results') {
    const tabEl = document.getElementById('admin-tab-results');
    if (tabEl) tabEl.style.display = 'block';
    btnResults?.classList.add('active');
    loadAdminResultsTab();
  } else if (tabName === 'leaderboard') {
    const tabEl = document.getElementById('admin-tab-leaderboard');
    if (tabEl) tabEl.style.display = 'block';
    btnLeaderboard?.classList.add('active');
    loadAdminLeaderboardTab();
  } else if (tabName === 'auditlogs') {
    const tabEl = document.getElementById('admin-tab-auditlogs');
    if (tabEl) tabEl.style.display = 'block';
    btnAudit?.classList.add('active');
    loadAdminAuditLogs();
  }
}

function switchStudentTab(tabName) {
  // Hide student view sections
  const tabTests = document.getElementById('student-tab-tests');
  const tabAttempts = document.getElementById('student-tab-my-attempts');
  const tabLeaderboard = document.getElementById('student-tab-leaderboard');
  const tabProfile = document.getElementById('student-tab-profile');

  [tabTests, tabAttempts, tabLeaderboard, tabProfile].forEach(el => {
    if (el) el.style.display = 'none';
  });

  const btnTests = document.getElementById('snav-btn-tests');
  const btnAttempts = document.getElementById('snav-btn-attempts');
  const btnLeaderboard = document.getElementById('snav-btn-leaderboard');
  const btnProfile = document.getElementById('snav-btn-profile');

  [btnTests, btnAttempts, btnLeaderboard, btnProfile].forEach(btn => btn?.classList.remove('active'));

  if (tabName === 'available-tests') {
    cancelQuiz(false);
    if (tabTests) tabTests.style.display = 'block';
    btnTests?.classList.add('active');
    loadStudentAvailableTests();
    loadFilterSubjects();
    loadTopicFilterDropdowns();
  } else if (tabName === 'my-attempts') {
    cancelQuiz(false);
    if (tabAttempts) tabAttempts.style.display = 'block';
    btnAttempts?.classList.add('active');
    loadStudentAttempts();
  } else if (tabName === 'leaderboard') {
    cancelQuiz(false);
    if (tabLeaderboard) tabLeaderboard.style.display = 'block';
    btnLeaderboard?.classList.add('active');
    loadStudentLeaderboardTab();
  } else if (tabName === 'profile') {
    cancelQuiz(false);
    if (tabProfile) tabProfile.style.display = 'block';
    btnProfile?.classList.add('active');
    loadStudentProfile();
  }
}

// Modal Helpers
function openModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) modal.classList.add('active');
}

function closeModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) modal.classList.remove('active');
}

// ==================== SUBJECTS (ADMIN) ====================

async function loadSubjects() {
  const grid = document.getElementById('subjects-grid');
  if (!grid) return;
  grid.innerHTML = '<div class="empty-state">Yuklanmoqda...</div>';

  try {
    const res = await fetchWithAuth(`${API_BASE}/subjects?page=${subjectsPageState.page}&pageSize=${subjectsPageState.pageSize}&search=${encodeURIComponent(subjectsPageState.search)}`);
    const data = await res.json();
    
    subjectsPageState.totalPages = data.totalPages || 1;
    document.getElementById('subj-page-info').innerText = `Sahifa ${subjectsPageState.page} / ${subjectsPageState.totalPages}`;

    // Manage disabled states
    document.getElementById('subj-prev-btn').disabled = subjectsPageState.page <= 1;
    document.getElementById('subj-next-btn').disabled = subjectsPageState.page >= subjectsPageState.totalPages;

    const subjects = data.items || [];
    if (subjects.length === 0) {
      grid.innerHTML = '<div class="empty-state">Hech qanday fan topilmadi.</div>';
      return;
    }

    grid.innerHTML = subjects.map(sub => `
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">📚 ${escapeHtml(sub.name)}</h3>
          <span class="badge badge-published">Aktiv Fan</span>
        </div>
        <p class="card-desc">${escapeHtml(sub.description || 'Tavsif berilmagan')}</p>
        <div class="card-footer">
          <span style="font-size: 0.85rem; color: var(--text-muted);">ID: ${sub.id.substring(0, 8)}...</span>
          <button class="btn btn-sm btn-danger" onclick="deleteSubject('${sub.id}')">
            🗑 O'chirish
          </button>
        </div>
      </div>
    `).join('');
  } catch (err) {
    grid.innerHTML = `<div class="empty-state" style="color: var(--danger-color);">${err.message}</div>`;
  }
}

function handleSubjectFilterChange() {
  const searchInput = document.getElementById('filter-subject-search');
  subjectsPageState.search = searchInput ? searchInput.value.trim() : '';
  subjectsPageState.page = 1;
  loadSubjects();
}

function changeSubjectPage(offset) {
  const nextPage = subjectsPageState.page + offset;
  if (nextPage > 0 && nextPage <= subjectsPageState.totalPages) {
    subjectsPageState.page = nextPage;
    loadSubjects();
  }
}

async function deleteSubject(subjectId) {
  if (!confirm('Haqiqatan ham ushbu fanni va unga tegishli barcha testlarni o\'chirmoqchimisiz?')) return;

  try {
    const res = await fetchWithAuth(`${API_BASE}/subjects/${subjectId}`, { method: 'DELETE' });
    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      throw new Error(data.message || 'Fanni o\'chirib bo\'lmadi');
    }
    loadSubjects();
    alert('Fan muvaffaqiyatli o\'chirildi!');
  } catch (err) {
    alert(err.message);
  }
}

async function handleCreateSubject(e) {
  if (e) e.preventDefault();
  const nameEl = document.getElementById('subject-name');
  const descEl = document.getElementById('subject-desc');

  const name = nameEl ? nameEl.value.trim() : '';
  const description = descEl ? descEl.value.trim() : '';

  try {
    const res = await fetchWithAuth(`${API_BASE}/subjects`, {
      method: 'POST',
      body: { name, description }
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Fanni saqlab bo\'lmadi');
    }

    closeModal('modal-add-subject');
    nameEl.value = '';
    descEl.value = '';
    loadSubjects();
    alert('Fan muvaffaqiyatli qo\'shildi! 🎉');
  } catch (err) {
    alert(err.message);
  }
}

// ==================== TOPICS MANAGEMENT (ADMIN) ====================

async function loadTopics() {
  const grid = document.getElementById('topics-grid');
  if (!grid) return;
  grid.innerHTML = '<div class="empty-state">Yuklanmoqda...</div>';

  try {
    const res = await fetchWithAuth(`${API_BASE}/topics`);
    const topics = await res.json();

    if (!Array.isArray(topics) || topics.length === 0) {
      grid.innerHTML = '<div class="empty-state">Hali topiclar yaratilmagan.</div>';
      return;
    }

    grid.innerHTML = topics.map(top => `
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">🏷️ ${escapeHtml(top.name)}</h3>
          <span class="topic-badge">Topic</span>
        </div>
        <div class="card-footer" style="margin-top: 1rem;">
          <span style="font-size: 0.85rem; color: var(--text-muted);">ID: ${top.id.substring(0, 8)}...</span>
          <button class="btn btn-sm btn-danger" onclick="deleteTopic('${top.id}')">
            🗑 O'chirish
          </button>
        </div>
      </div>
    `).join('');
  } catch (err) {
    grid.innerHTML = `<div class="empty-state" style="color: var(--danger-color);">${err.message}</div>`;
  }
}

async function handleCreateTopic(e) {
  if (e) e.preventDefault();
  const nameEl = document.getElementById('topic-name');
  const name = nameEl ? nameEl.value.trim() : '';

  if (!name) return alert('Topic nomini kiriting!');

  try {
    const res = await fetchWithAuth(`${API_BASE}/topics`, {
      method: 'POST',
      body: { name }
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Topic yaratishda xatolik');
    }

    closeModal('modal-add-topic');
    nameEl.value = '';
    loadTopics();
    loadTopicFilterDropdowns();
    alert('Yangi topic muvaffaqiyatli qo\'shildi! 🎉');
  } catch (err) {
    alert(err.message);
  }
}

async function deleteTopic(topicId) {
  if (!confirm('Haqiqatan ham ushbu topicni o\'chirmoqchimisiz?')) return;

  try {
    const res = await fetchWithAuth(`${API_BASE}/topics/${topicId}`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Topicni o\'chirib bo\'lmadi');

    loadTopics();
    loadTopicFilterDropdowns();
    alert('Topic muvaffaqiyatli o\'chirildi!');
  } catch (err) {
    alert(err.message);
  }
}

async function populateTopicCheckboxes() {
  const container = document.getElementById('test-topic-checkboxes');
  if (!container) return;

  try {
    const res = await fetchWithAuth(`${API_BASE}/topics`);
    const topics = await res.json();

    if (!Array.isArray(topics) || topics.length === 0) {
      container.innerHTML = '<span style="color: var(--text-muted); font-size: 0.85rem;">Hali topiclar mavjud emas.</span>';
      return;
    }

    container.innerHTML = topics.map(top => `
      <label class="topic-checkbox-item">
        <input type="checkbox" class="topic-checkbox-input" value="${top.id}">
        <span>${escapeHtml(top.name)}</span>
      </label>
    `).join('');
  } catch (err) {
    container.innerHTML = '<span style="color: var(--danger-color); font-size: 0.85rem;">Topiclarni yuklab bo\'lmadi.</span>';
  }
}

async function loadTopicFilterDropdowns() {
  try {
    const res = await fetchWithAuth(`${API_BASE}/topics`);
    const topics = await res.json();

    const selects = ['filter-test-topic', 'student-filter-topic'];
    selects.forEach(id => {
      const select = document.getElementById(id);
      if (!select) return;

      const cur = select.value;
      select.innerHTML = '<option value="">-- Barcha topiclar --</option>';

      if (Array.isArray(topics)) {
        topics.forEach(top => {
          const opt = document.createElement('option');
          opt.value = top.id;
          opt.innerText = top.name;
          select.appendChild(opt);
        });
      }

      if (cur) select.value = cur;
    });
  } catch (err) {
    console.error('Error loading topic filter dropdowns:', err);
  }
}

// ==================== TESTS MANAGEMENT (ADMIN) ====================

async function loadFilterSubjects() {
  try {
    const res = await fetchWithAuth(`${API_BASE}/subjects?pageSize=50`);
    const data = await res.json();
    const subjects = data.items || [];

    const selects = ['filter-test-subject', 'student-filter-subject', 'test-subject-id'];
    selects.forEach(id => {
      const select = document.getElementById(id);
      if (!select) return;
      
      // Save current selection
      const currentVal = select.value;
      
      // Clear options keeping the first default option
      select.innerHTML = select.options[0].outerHTML;

      subjects.forEach(sub => {
        const opt = document.createElement('option');
        opt.value = sub.id;
        opt.innerText = sub.name;
        select.appendChild(opt);
      });

      // Restore selection if existed
      if (currentVal) select.value = currentVal;
    });
  } catch (err) {
    console.error('Error loading subject dropdowns:', err);
  }
}

async function loadTests() {
  const grid = document.getElementById('tests-grid');
  if (!grid) return;
  grid.innerHTML = '<div class="empty-state">Yuklanmoqda...</div>';

  try {
    let url = `${API_BASE}/tests?page=${testsPageState.page}&pageSize=${testsPageState.pageSize}&search=${encodeURIComponent(testsPageState.search)}&subjectId=${testsPageState.subjectId}`;
    if (testsPageState.difficulty) url += `&difficulty=${encodeURIComponent(testsPageState.difficulty)}`;
    if (testsPageState.topicId) url += `&topicId=${encodeURIComponent(testsPageState.topicId)}`;

    const res = await fetchWithAuth(url);
    const data = await res.json();

    testsPageState.totalPages = data.totalPages || 1;
    document.getElementById('tests-page-info').innerText = `Sahifa ${testsPageState.page} / ${testsPageState.totalPages}`;

    // Manage disabled states
    document.getElementById('tests-prev-btn').disabled = testsPageState.page <= 1;
    document.getElementById('tests-next-btn').disabled = testsPageState.page >= testsPageState.totalPages;

    const tests = data.items || [];
    if (tests.length === 0) {
      grid.innerHTML = '<div class="empty-state">Bunday testlar topilmadi.</div>';
      return;
    }

    grid.innerHTML = tests.map(t => {
      const pubBadge = t.isPublished ? 
        '<span class="badge badge-published">🟢 Aktiv</span>' : 
        '<span class="badge badge-unpublished">🔴 Yopiq</span>';
      
      const pubBtnText = t.isPublished ? '🔴 Yopish' : '🟢 Nashr qilish';

      // Difficulty badge
      let diffBadge = '<span class="badge-diff badge-medium">🟡 O\'rta</span>';
      if (t.difficulty === 'Easy') diffBadge = '<span class="badge-diff badge-easy">🟢 Oson</span>';
      else if (t.difficulty === 'Hard') diffBadge = '<span class="badge-diff badge-hard">🔴 Qiyin</span>';

      // Topics list
      const topicsHtml = (t.topics && t.topics.length > 0) ? 
        `<div class="topic-tag-list">${t.topics.map(top => `<span class="topic-badge">${escapeHtml(top)}</span>`).join('')}</div>` : '';

      const maxAttemptsText = t.maxAttemptsPerStudent ? `${t.maxAttemptsPerStudent} ta` : 'Cheklovsiz';

      return `
        <div class="card">
          <div class="card-header">
            <h3 class="card-title">${escapeHtml(t.title)}</h3>
            <div style="display: flex; gap: 0.5rem; align-items: center;">
              ${diffBadge}
              ${pubBadge}
            </div>
          </div>
          <p class="card-desc">📚 Fan: <strong>${escapeHtml(t.subjectName)}</strong></p>
          ${topicsHtml}
          <div style="font-size: 0.9rem; color: var(--text-muted); margin: 0.8rem 0 1.2rem 0; display: flex; flex-direction: column; gap: 0.3rem;">
            <span>❓ Savollar soni: <strong>${t.questionsCount} ta</strong></span>
            <span>🎯 O'tish foizi: <strong>${t.passingPercentage}%</strong></span>
            <span>⏳ Vaqt limiti: <strong>${t.timeLimitMinutes || t.durationMinutes} daqiqa</strong></span>
            <span>🔄 Retake limit: <strong>${maxAttemptsText}</strong></span>
          </div>
          <div class="card-actions-row">
            <button class="btn btn-sm btn-secondary" onclick="openAddQuestionModal('${t.id}', '${escapeJs(t.title)}')">
              ❓ Savollar (${t.questionsCount})
            </button>
            <button class="btn btn-sm btn-primary-blue" onclick="togglePublish('${t.id}')">
              ${pubBtnText}
            </button>
            <button class="btn btn-sm btn-danger" onclick="deleteTest('${t.id}')">
              🗑 O'chirish
            </button>
          </div>
        </div>
      `;
    }).join('');
  } catch (err) {
    grid.innerHTML = `<div class="empty-state" style="color: var(--danger-color);">${err.message}</div>`;
  }
}

function handleTestFilterChange() {
  const searchInput = document.getElementById('filter-test-search');
  const subjectSelect = document.getElementById('filter-test-subject');
  const diffSelect = document.getElementById('filter-test-difficulty');
  const topicSelect = document.getElementById('filter-test-topic');
  
  testsPageState.search = searchInput ? searchInput.value.trim() : '';
  testsPageState.subjectId = subjectSelect ? subjectSelect.value : '';
  testsPageState.difficulty = diffSelect ? diffSelect.value : '';
  testsPageState.topicId = topicSelect ? topicSelect.value : '';
  testsPageState.page = 1;
  loadTests();
}

function changeTestPage(offset) {
  const nextPage = testsPageState.page + offset;
  if (nextPage > 0 && nextPage <= testsPageState.totalPages) {
    testsPageState.page = nextPage;
    loadTests();
  }
}

async function togglePublish(testId) {
  try {
    const res = await fetchWithAuth(`${API_BASE}/tests/${testId}/toggle-publish`, { method: 'PATCH' });
    if (!res.ok) throw new Error('Test statusini o\'zgartirib bo\'lmadi');
    loadTests();
  } catch (err) {
    alert(err.message);
  }
}

async function deleteTest(testId) {
  if (!confirm('Haqiqatan ham ushbu testni o\'chirmoqchimisiz? Unda barcha natijalar ham o\'chib ketadi!')) return;

  try {
    const res = await fetchWithAuth(`${API_BASE}/tests/${testId}`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Testni o\'chirishda xatolik');
    loadTests();
    alert('Test muvaffaqiyatli o\'chirildi!');
  } catch (err) {
    alert(err.message);
  }
}

function openCreateTestModal() {
  loadFilterSubjects();
  populateTopicCheckboxes();
  openModal('modal-add-test');
}

async function handleCreateTest(e) {
  if (e) e.preventDefault();
  const subIdEl = document.getElementById('test-subject-id');
  const titleEl = document.getElementById('test-title');
  const descEl = document.getElementById('test-desc');
  const passingEl = document.getElementById('test-passing');
  const timeLimitEl = document.getElementById('test-timelimit');
  const difficultyEl = document.getElementById('test-difficulty');
  const maxAttemptsEl = document.getElementById('test-max-attempts');
  const showReviewEl = document.getElementById('test-show-review');

  const isPublishedEl = document.getElementById('test-is-published');

  const subjectId = subIdEl ? subIdEl.value : '';
  const title = titleEl ? titleEl.value.trim() : '';
  const description = descEl ? descEl.value.trim() : '';
  const passingPercentage = passingEl ? parseInt(passingEl.value) : 60;
  const timeLimitMinutes = timeLimitEl ? parseInt(timeLimitEl.value) : 15;
  const difficulty = difficultyEl ? difficultyEl.value : 'Medium';
  const maxAttemptsPerStudent = (maxAttemptsEl && maxAttemptsEl.value.trim() !== '') ? parseInt(maxAttemptsEl.value) : null;
  const showReviewAfterSubmit = showReviewEl ? showReviewEl.checked : true;
  const isPublished = isPublishedEl ? isPublishedEl.checked : true;

  // Selected topic IDs
  const topicCheckboxes = document.querySelectorAll('.topic-checkbox-input:checked');
  const topicIds = Array.from(topicCheckboxes).map(cb => cb.value);

  try {
    const res = await fetchWithAuth(`${API_BASE}/tests`, {
      method: 'POST',
      body: { 
        subjectId, 
        title, 
        description, 
        passingPercentage, 
        timeLimitMinutes, 
        durationMinutes: timeLimitMinutes,
        difficulty,
        maxAttemptsPerStudent,
        showReviewAfterSubmit,
        isPublished,
        topicIds
      }
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Testni yaratib bo\'lmadi');
    }

    closeModal('modal-add-test');
    titleEl.value = '';
    descEl.value = '';
    if (maxAttemptsEl) maxAttemptsEl.value = '';
    loadTests();
    alert('Yangi test muvaffaqiyatli yaratildi! 🎉 Endi savollar qo\'shishingiz mumkin.');
  } catch (err) {
    alert(err.message);
  }
}

// ==================== SAVOLLAR QO'SHISH (ADMIN) ====================

function openAddQuestionModal(testId, testTitle) {
  const header = document.getElementById('modal-question-header');
  if (header) header.innerText = `❓ "${testTitle}" testiga savol qo'shish`;
  
  const testIdInput = document.getElementById('question-test-id');
  if (testIdInput) testIdInput.value = testId;

  // Reset fields
  document.getElementById('question-text').value = '';
  document.getElementById('question-points').value = '10';
  document.getElementById('opt-0').value = '';
  document.getElementById('opt-1').value = '';
  document.getElementById('opt-2').value = '';
  document.getElementById('opt-3').value = '';
  
  const rad0 = document.querySelector('input[name="correct-option"][value="0"]');
  if (rad0) rad0.checked = true;

  switchQuestionTab('single');
  openModal('modal-add-question');
}

function switchQuestionTab(mode) {
  const btnSingle = document.getElementById('qtab-single');
  const btnFile = document.getElementById('qtab-file');
  const formSingle = document.getElementById('form-add-question');
  const formFile = document.getElementById('form-upload-questions');

  if (mode === 'single') {
    btnSingle?.classList.add('active');
    btnFile?.classList.remove('active');
    if (formSingle) formSingle.style.display = 'block';
    if (formFile) formFile.style.display = 'none';
  } else {
    btnSingle?.classList.remove('active');
    btnFile?.classList.add('active');
    if (formSingle) formSingle.style.display = 'none';
    if (formFile) formFile.style.display = 'block';
  }
}

async function handleCreateQuestion(e) {
  if (e) e.preventDefault();
  const testId = document.getElementById('question-test-id').value;
  const text = document.getElementById('question-text').value.trim();
  const points = parseInt(document.getElementById('question-points').value);
  const correctOptionEl = document.querySelector('input[name="correct-option"]:checked');
  const correctIndex = correctOptionEl ? parseInt(correctOptionEl.value) : 0;

  const opt0 = document.getElementById('opt-0').value.trim();
  const opt1 = document.getElementById('opt-1').value.trim();
  const opt2 = document.getElementById('opt-2').value.trim();
  const opt3 = document.getElementById('opt-3').value.trim();

  if (!text || !opt0 || !opt1 || !opt2 || !opt3) {
    return alert('Savol matni va barcha 4 ta variant to\'ldirilishi shart!');
  }

  const options = [
    { text: opt0, isCorrect: correctIndex === 0 },
    { text: opt1, isCorrect: correctIndex === 1 },
    { text: opt2, isCorrect: correctIndex === 2 },
    { text: opt3, isCorrect: correctIndex === 3 }
  ];

  try {
    const res = await fetchWithAuth(`${API_BASE}/tests/${testId}/questions`, {
      method: 'POST',
      body: { text, points, options }
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Savol saqlashda xatolik yuz berdi');
    }

    closeModal('modal-add-question');
    loadTests();
    alert('Savol muvaffaqiyatli qo\'shildi! ✅');
  } catch (err) {
    alert(err.message);
  }
}

async function sendBulkQuestions(testId, questionsData) {
  if (!Array.isArray(questionsData) || questionsData.length === 0) {
    throw new Error('Fayl bo\'sh yoki formati to\'g\'ri kelmadi!');
  }

  const res = await fetchWithAuth(`${API_BASE}/tests/${testId}/questions/import`, {
    method: 'POST',
    body: questionsData
  });

  const result = await res.json();
  if (!res.ok) {
    throw new Error(result.message || 'Fayldan savollarni saqlashda xatolik yuz berdi');
  }

  closeModal('modal-add-question');
  loadTests();

  // Populate Import Result Modal
  document.getElementById('import-res-total').innerText = result.totalRows || 0;
  document.getElementById('import-res-success').innerText = result.importedCount || 0;
  document.getElementById('import-res-failed').innerText = result.failedCount || 0;

  const errorsContainer = document.getElementById('import-errors-container');
  const errorsList = document.getElementById('import-errors-list');

  if (result.errors && result.errors.length > 0) {
    errorsContainer.style.display = 'block';
    errorsList.innerHTML = result.errors.map(err => `<div style="margin-bottom: 0.3rem; color: #f87171;">• ${escapeHtml(err)}</div>`).join('');
  } else {
    errorsContainer.style.display = 'none';
  }

  openModal('modal-import-result');
}

async function handleUploadQuestionsFile(e) {
  if (e) e.preventDefault();
  const testId = document.getElementById('question-test-id').value;
  const fileInput = document.getElementById('questions-file-input');

  if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
    return alert('Iltimos, faylni tanlang!');
  }

  const file = fileInput.files[0];
  const filename = file.name.toLowerCase();

  if (filename.endsWith('.json')) {
    const reader = new FileReader();
    reader.onload = async function(event) {
      try {
        const questionsData = JSON.parse(event.target.result);
        await sendBulkQuestions(testId, questionsData);
      } catch (err) {
        alert(`JSON fayl o'qishda xatolik: ${err.message}`);
      }
    };
    reader.readAsText(file);
  } else if (filename.endsWith('.xlsx') || filename.endsWith('.xls') || filename.endsWith('.csv')) {
    const reader = new FileReader();
    reader.onload = async function(event) {
      try {
        const data = new Uint8Array(event.target.result);
        if (typeof XLSX === 'undefined') {
          throw new Error('XLSX kutubxonasi yuklanmadi');
        }
        const workbook = XLSX.read(data, { type: 'array' });
        const firstSheetName = workbook.SheetNames[0];
        const worksheet = workbook.Sheets[firstSheetName];
        const rows = XLSX.utils.sheet_to_json(worksheet);

        if (!rows || rows.length === 0) {
          throw new Error('Excel/CSV fayl bo\'sh!');
        }

        const questionsData = rows.map(r => {
          const text = r.Savol || r.Question || r.text || '';
          const points = parseInt(r.Points || r.points || 10);
          const optA = r.OptionA || r.optA || r.A || '';
          const optB = r.OptionB || r.optB || r.B || '';
          const optC = r.OptionC || r.optC || r.C || '';
          const optD = r.OptionD || r.optD || r.D || '';
          const correctIdx = parseInt(r.CorrectIndex || r.correctIndex || 0);

          return {
            text: String(text),
            points: points,
            options: [
              { text: String(optA), isCorrect: correctIdx === 0 },
              { text: String(optB), isCorrect: correctIdx === 1 },
              { text: String(optC), isCorrect: correctIdx === 2 },
              { text: String(optD), isCorrect: correctIdx === 3 }
            ]
          };
        });

        await sendBulkQuestions(testId, questionsData);
      } catch (err) {
        alert(`Excel/CSV fayl o'qishda xatolik: ${err.message}`);
      }
    };
    reader.readAsArrayBuffer(file);
  } else {
    alert('Faqat .xlsx, .csv yoki .json fayllarni yuklashingiz mumkin!');
  }
}

function downloadQuestionTemplate() {
  const template = [
    {
      text: "Amir Temur nechanchi yilda tug'ilgan?",
      points: 10,
      options: [
        { text: "1336-yilda", isCorrect: true },
        { text: "1441-yilda", isCorrect: false },
        { text: "1221-yilda", isCorrect: false },
        { text: "1501-yilda", isCorrect: false }
      ]
    }
  ];

  const blob = new Blob([JSON.stringify(template, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'savollar_shablon.json';
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

function downloadExcelTemplate() {
  const csvContent = "\ufeffSavol,Points,OptionA,OptionB,OptionC,OptionD,CorrectIndex\n" +
    "\"Amir Temur nechanchi yilda tug'ilgan?\",10,\"1336-yilda\",\"1441-yilda\",\"1221-yilda\",\"1501-yilda\",0\n" +
    "\"Yer quyosh atrofini taxminan necha kunda aylanib chiqadi?\",10,\"30 kunda\",\"365 kunda\",\"24 soatda\",\"100 kunda\",1\n" +
    "\"Alisher Navoiy qaysi asar muallifi?\",10,\"Xamsa\",\"Boburnoma\",\"O'tkan kunlar\",\"Sohibqiron\",0\n" +
    "\"Suvning kimyoviy formulasi qanday?\",10,\"CO2\",\"NaCl\",\"H2O\",\"O2\",2\n" +
    "\"Yer yuzidagi eng katta okean qaysi?\",10,\"Tinch okeani\",\"Atlantika okeani\",\"Hind okeani\",\"Shimoliy muz okeani\",0\n" +
    "\"Tarixdagi birinchi dasturchi kim bo'lgan?\",10,\"Ada Lovelace\",\"Alan Turing\",\"Charles Babbage\",\"Bill Gates\",0\n" +
    "\"Vakuumda yorug'lik tezligi qanchaga teng?\",10,\"300 000 km/s\",\"150 000 km/s\",\"1 000 000 km/s\",\"340 m/s\",0\n" +
    "\"O'zbekiston Respublikasi mustaqilligi nechanchi yilda e'lon qilingan?\",10,\"1991-yilda\",\"1990-yilda\",\"1992-yilda\",\"1989-yilda\",0\n" +
    "\"Fransiya davlatining poytaxti qaysi shahar?\",10,\"London\",\"Rim\",\"Berlin\",\"Parij\",3\n" +
    "\"O'tkan kunlar romanining muallifi kim?\",10,\"Abdulla Qodiriy\",\"Cho'lpon\",\"Fitrat\",\"G'afur G'ulom\",0\n";

  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'test_savollar_shablon.csv';
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

// ==================== RESULTS TAB (ADMIN) ====================

async function loadAdminResultsTab() {
  const select = document.getElementById('admin-results-test-select');
  if (!select) return;
  select.innerHTML = '<option value="all">-- Barcha Testlar Natijalari --</option>';

  try {
    const res = await fetchWithAuth(`${API_BASE}/tests?pageSize=50`);
    const data = await res.json();
    const tests = data.items || [];

    tests.forEach(t => {
      const opt = document.createElement('option');
      opt.value = t.id;
      opt.innerText = `${t.title} (${t.subjectName})`;
      select.appendChild(opt);
    });
  } catch (e) {
    console.error('Error loading results dropdown:', e);
  }

  loadAdminResultsForSelectedTest();
}

async function loadAdminResultsForSelectedTest() {
  const select = document.getElementById('admin-results-test-select');
  const tbody = document.getElementById('admin-results-tbody');
  if (!tbody || !select) return;

  const selectedTestId = select.value;
  tbody.innerHTML = '<tr><td colspan="7" class="empty-state">Yuklanmoqda...</td></tr>';

  try {
    let url = `${API_BASE}/attempts`;
    if (selectedTestId && selectedTestId !== 'all') {
      url = `${API_BASE}/attempts/test/${selectedTestId}`;
    }

    const res = await fetchWithAuth(url);
    const results = await res.json();

    if (!results || results.length === 0) {
      tbody.innerHTML = '<tr><td colspan="7" class="empty-state">Topshirilgan natijalar topilmadi.</td></tr>';
      return;
    }

    tbody.innerHTML = results.map(r => {
      const formattedDate = new Date(r.passedAt).toLocaleString('uz-UZ');
      
      let statusBadge = r.isPassed ? 
        '<span class="badge-status-passed">O\'TDI</span>' : 
        '<span class="badge-status-failed">YIQILDI</span>';
        
      if (r.isExpired) {
        statusBadge += ' <span style="background: var(--danger-color); color: #fff; font-size: 0.75rem; padding: 0.1rem 0.3rem; border-radius: 4px; margin-left: 0.3rem;">EXPIRED</span>';
      }

      // Convert duration seconds to formatted MM:SS
      const mins = Math.floor(r.durationSeconds / 60);
      const secs = r.durationSeconds % 60;
      const durationFormatted = `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;

      return `
        <tr>
          <td><strong>${escapeHtml(r.studentName || 'Talaba')}</strong></td>
          <td>${r.correctAnswersCount}/${r.totalQuestions}</td>
          <td>${r.earnedScore}/${r.totalScore}</td>
          <td><strong>${r.percentage}%</strong></td>
          <td>${durationFormatted}</td>
          <td>${statusBadge}</td>
          <td>${formattedDate}</td>
        </tr>
      `;
    }).join('');
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="7" class="empty-state" style="color: var(--danger-color);">${err.message}</td></tr>`;
  }
}

function exportResultsToExcel() {
  if (typeof XLSX === 'undefined') {
    return alert('XLSX kutubxonasi yuklanmadi');
  }

  const table = document.querySelector('.results-table');
  if (!table) return alert('Natijalar jadvali topilmadi');

  const wb = XLSX.utils.table_to_book(table, { sheet: "Natijalar" });
  XLSX.writeFile(wb, "test_natijalari.xlsx");
}

// ==================== STUDENT QUIZ & TIMER ====================

function startQuizTimer(durationMinutes) {
  clearQuizTimer();
  quizRemainingSeconds = durationMinutes * 60;
  testTotalDurationSeconds = 0;

  updateTimerDisplay();

  quizTimerInterval = setInterval(() => {
    quizRemainingSeconds--;
    testTotalDurationSeconds++;
    updateTimerDisplay();

    if (quizRemainingSeconds <= 0) {
      clearQuizTimer();
      alert('⏱ Ajratilgan vaqt limit tugadi! Test avtomatik topshirilmoqda.');
      submitQuiz(true);
    }
  }, 1000);
}

function updateTimerDisplay() {
  const clockEl = document.getElementById('quiz-timer-clock');
  if (!clockEl) return;

  const mins = Math.max(0, Math.floor(quizRemainingSeconds / 60));
  const secs = Math.max(0, quizRemainingSeconds % 60);
  clockEl.innerText = `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
}

function clearQuizTimer() {
  if (quizTimerInterval) {
    clearInterval(quizTimerInterval);
    quizTimerInterval = null;
  }
}

async function loadStudentAvailableTests() {
  const grid = document.getElementById('student-tests-grid');
  const quizInterface = document.getElementById('quiz-interface');
  const pag = document.getElementById('student-tests-pagination');
  
  clearQuizTimer();
  if (quizInterface) quizInterface.style.display = 'none';
  if (grid) grid.style.display = 'grid';
  if (pag) pag.style.display = 'flex';

  if (!grid) {
    console.error('student-tests-grid elementi topilmadi!');
    return;
  }
  grid.innerHTML = '<div class="empty-state">Mavjud testlar yuklanmoqda...</div>';

  try {
    let url = `${API_BASE}/tests?page=${studentTestsPageState.page}&pageSize=${studentTestsPageState.pageSize}&search=${encodeURIComponent(studentTestsPageState.search)}&subjectId=${studentTestsPageState.subjectId}`;
    if (studentTestsPageState.difficulty) url += `&difficulty=${encodeURIComponent(studentTestsPageState.difficulty)}`;
    if (studentTestsPageState.topicId) url += `&topicId=${encodeURIComponent(studentTestsPageState.topicId)}`;

    console.log('Loading student tests from:', url);
    const res = await fetchWithAuth(url);
    console.log('Response status:', res.status);
    const data = await res.json();
    console.log('Tests data:', data);
    
    studentTestsPageState.totalPages = data.totalPages || 1;
    document.getElementById('stud-page-info').innerText = `Sahifa ${studentTestsPageState.page} / ${studentTestsPageState.totalPages}`;

    // Manage disabled states
    document.getElementById('stud-prev-btn').disabled = studentTestsPageState.page <= 1;
    document.getElementById('stud-next-btn').disabled = studentTestsPageState.page >= studentTestsPageState.totalPages;

    const tests = data.items || [];
    if (tests.length === 0) {
      grid.innerHTML = '<div class="empty-state">Hozirda topshirish uchun nashr qilingan bunday testlar yo\'q.</div>';
      return;
    }

    grid.innerHTML = tests.map(t => {
      // Difficulty badge
      let diffBadge = '<span class="badge-diff badge-medium">🟡 O\'rta</span>';
      if (t.difficulty === 'Easy') diffBadge = '<span class="badge-diff badge-easy">🟢 Oson</span>';
      else if (t.difficulty === 'Hard') diffBadge = '<span class="badge-diff badge-hard">🔴 Qiyin</span>';

      // Topics list
      const topicsHtml = (t.topics && t.topics.length > 0) ? 
        `<div class="topic-tag-list">${t.topics.map(top => `<span class="topic-badge">${escapeHtml(top)}</span>`).join('')}</div>` : '';

      const maxAttemptsText = t.maxAttemptsPerStudent ? `${t.maxAttemptsPerStudent} ta` : 'Cheklovsiz';

      return `
        <div class="card">
          <div class="card-header">
            <h3 class="card-title">${escapeHtml(t.title)}</h3>
            ${diffBadge}
          </div>
          <p class="card-desc">📚 Fan: <strong>${escapeHtml(t.subjectName)}</strong></p>
          ${topicsHtml}
          <div style="font-size: 0.9rem; color: var(--text-muted); margin: 0.8rem 0 1.2rem 0; display: flex; flex-direction: column; gap: 0.3rem;">
            <span>❓ Savollar soni: <strong>${t.questionsCount} ta</strong></span>
            <span>🎯 O'tish foizi: <strong>${t.passingPercentage}%</strong></span>
            <span>⏳ Vaqt limiti: <strong>${t.timeLimitMinutes || t.durationMinutes} daqiqa</strong></span>
            <span>🔄 Urinishlar limiti: <strong>${maxAttemptsText}</strong></span>
          </div>
          <button class="btn btn-full" onclick="startStudentTest('${t.id}')">
            Testni Boshlash 🚀
          </button>
        </div>
      `;
    }).join('');
  } catch (err) {
    console.error('loadStudentAvailableTests error:', err);
    grid.innerHTML = `<div class="empty-state" style="color: var(--danger-color);">❌ Testlarni yuklashda xatolik: ${err.message}</div>`;
  }
}

function handleStudentTestFilterChange() {
  const searchInput = document.getElementById('student-filter-search');
  const subjectSelect = document.getElementById('student-filter-subject');
  const diffSelect = document.getElementById('student-filter-difficulty');
  const topicSelect = document.getElementById('student-filter-topic');

  studentTestsPageState.search = searchInput ? searchInput.value.trim() : '';
  studentTestsPageState.subjectId = subjectSelect ? subjectSelect.value : '';
  studentTestsPageState.difficulty = diffSelect ? diffSelect.value : '';
  studentTestsPageState.topicId = topicSelect ? topicSelect.value : '';
  studentTestsPageState.page = 1;
  loadStudentAvailableTests();
}

function changeStudentTestPage(offset) {
  const nextPage = studentTestsPageState.page + offset;
  if (nextPage > 0 && nextPage <= studentTestsPageState.totalPages) {
    studentTestsPageState.page = nextPage;
    loadStudentAvailableTests();
  }
}

async function startStudentTest(testId) {
  if (!confirm('Testni boshlamoqchimisiz? Vaqt limiti va savollar yuklanadi.')) return;

  const grid = document.getElementById('student-tests-grid');
  const quizInterface = document.getElementById('quiz-interface');
  const pag = document.getElementById('student-tests-pagination');

  if (grid) grid.style.display = 'none';
  if (pag) pag.style.display = 'none';
  if (quizInterface) quizInterface.style.display = 'block';

  try {
    // 1. Securely start the attempt in the database
    const startRes = await fetchWithAuth(`${API_BASE}/attempts/start/${testId}`, {
      method: 'POST'
    });
    
    const startData = await startRes.json();
    if (!startRes.ok) throw new Error(startData.message || 'Testni boshlab bo\'lmadi');
    
    currentAttemptId = startData.attemptId;

    // 2. Fetch the randomized questions layout (answers hidden)
    const testRes = await fetchWithAuth(`${API_BASE}/student-tests/${testId}`);
    if (!testRes.ok) throw new Error('Savollarni yuklashda xatolik');
    
    currentStudentTest = await testRes.json();
    currentQuestionIndex = 0;
    studentAnswersMap = {};

    const titleEl = document.getElementById('quiz-test-title');
    const descEl = document.getElementById('quiz-test-desc');
    if (titleEl) titleEl.innerText = currentStudentTest.title;
    if (descEl) descEl.innerText = currentStudentTest.description || '';

    // Start Timer
    const limitMinutes = currentStudentTest.timeLimitMinutes > 0 ? currentStudentTest.timeLimitMinutes : currentStudentTest.durationMinutes;
    startQuizTimer(limitMinutes || 15);

    renderCurrentQuestion();
  } catch (err) {
    const questionsList = document.getElementById('quiz-questions-list');
    if (questionsList) questionsList.innerHTML = `<div class="empty-state" style="color: var(--danger-color);">${err.message}</div>`;
  }
}

function selectOption(questionId, optionId) {
  studentAnswersMap[questionId] = optionId;
  
  // Highlight visually
  document.querySelectorAll('.option-item').forEach(el => el.classList.remove('active'));
  const activeInput = document.querySelector(`input[name="q_${questionId}"][value="${optionId}"]`);
  if (activeInput) {
    activeInput.closest('.option-item').classList.add('active');
  }
}

function renderCurrentQuestion() {
  const questionsList = document.getElementById('quiz-questions-list');
  if (!questionsList || !currentStudentTest || !currentStudentTest.questions) return;

  const questions = currentStudentTest.questions;
  if (questions.length === 0) {
    questionsList.innerHTML = '<div class="empty-state">Ushbu testda hali savollar mavjud emas.</div>';
    return;
  }

  const total = questions.length;
  const q = questions[currentQuestionIndex];
  const selectedOptionId = studentAnswersMap[q.id];
  const progressPercent = Math.round(((currentQuestionIndex + 1) / total) * 100);

  questionsList.innerHTML = `
    <!-- Progress Indicator -->
    <div style="margin-bottom: 1.5rem;">
      <div style="display: flex; justify-content: space-between; font-weight: 600; font-size: 0.9rem; margin-bottom: 0.4rem; color: var(--text-muted);">
        <span>Savol ${currentQuestionIndex + 1} / ${total}</span>
        <span>${progressPercent}% to'ldirildi</span>
      </div>
      <div style="width: 100%; height: 8px; background: rgba(255,255,255,0.06); border-radius: 4px; overflow: hidden;">
        <div style="width: ${progressPercent}%; height: 100%; background: linear-gradient(90deg, #6366f1, #10b981); transition: width 0.3s ease;"></div>
      </div>
    </div>

    <!-- Single Question Card -->
    <div style="background: rgba(15, 23, 42, 0.6); padding: 1.5rem; border-radius: var(--radius-lg); border: 1px solid var(--border-color); animation: fadeIn 0.3s ease;">
      <div class="question-title" style="margin-bottom: 1.2rem; font-size: 1.15rem; font-weight: 700;">
        <span>${currentQuestionIndex + 1}. ${escapeHtml(q.text)}</span>
        <span style="font-size: 0.85rem; color: var(--accent-primary); background: rgba(99, 102, 241, 0.1); padding: 0.2rem 0.6rem; border-radius: 12px; margin-left: 0.5rem;">
          ${q.points} ball
        </span>
      </div>

      <div class="options-list">
        ${q.options.map(opt => `
          <label class="option-item ${selectedOptionId === opt.id ? 'active' : ''}">
            <input type="radio" name="q_${q.id}" value="${opt.id}" ${selectedOptionId === opt.id ? 'checked' : ''} onchange="selectOption('${q.id}', '${opt.id}')">
            <span>${escapeHtml(opt.text)}</span>
          </label>
        `).join('')}
      </div>
    </div>

    <!-- Navigation Buttons -->
    <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 1.8rem; gap: 1rem;">
      <button class="btn btn-secondary" ${currentQuestionIndex === 0 ? 'disabled style="opacity: 0.4; cursor: not-allowed;"' : ''} onclick="prevQuestion()">
        ⬅️ Oldingi
      </button>

      ${currentQuestionIndex < total - 1 ? `
        <button class="btn btn-primary-blue" onclick="nextQuestion()">
          Keyingi ➡️
        </button>
      ` : `
        <button class="btn btn-success" style="padding: 0.8rem 1.4rem;" onclick="submitQuiz()">
          Testni Yakunlash 🎯
        </button>
      `}
    </div>
  `;
}

function prevQuestion() {
  if (currentQuestionIndex > 0) {
    currentQuestionIndex--;
    renderCurrentQuestion();
  }
}

function nextQuestion() {
  if (currentStudentTest && currentQuestionIndex < currentStudentTest.questions.length - 1) {
    currentQuestionIndex++;
    renderCurrentQuestion();
  }
}

function cancelQuiz(shouldReload = true) {
  clearQuizTimer();
  currentStudentTest = null;
  currentAttemptId = null;
  currentQuestionIndex = 0;
  studentAnswersMap = {};
  const quizInterface = document.getElementById('quiz-interface');
  const grid = document.getElementById('student-tests-grid');
  const pag = document.getElementById('student-tests-pagination');
  
  if (quizInterface) quizInterface.style.display = 'none';
  if (grid) grid.style.display = 'grid';
  if (pag) pag.style.display = 'flex';
  
  if (shouldReload) {
    loadStudentAvailableTests();
  }
}

async function submitQuiz(isAutoSubmit = false) {
  if (!currentStudentTest || !currentStudentTest.questions || !currentAttemptId) return;

  const questions = currentStudentTest.questions;
  const answers = [];
  let unansweredCount = 0;

  for (const q of questions) {
    const selectedOptionId = studentAnswersMap[q.id];
    if (!selectedOptionId) {
      unansweredCount++;
    } else {
      answers.push({
        questionId: q.id,
        selectedOptionId: selectedOptionId
      });
    }
  }

  if (!isAutoSubmit && unansweredCount > 0) {
    if (!confirm(`${unansweredCount} ta savolga javob belgilamadingiz. Testni baribir topshirasizmi?`)) return;
  }

  clearQuizTimer();

  try {
    const res = await fetchWithAuth(`${API_BASE}/attempts/${currentAttemptId}/submit`, {
      method: 'POST',
      body: answers
    });

    const result = await res.json();
    if (!res.ok) throw new Error(result.message || 'Natijani saqlashda xatolik yuz berdi');

    showAttemptResult(result);
  } catch (err) {
    alert(err.message);
  }
}

function showAttemptResult(res) {
  const percentEl = document.getElementById('res-percentage');
  const pointsEl = document.getElementById('res-points-text');
  const statusTitle = document.getElementById('res-status-title');
  const passedTimeEl = document.getElementById('res-passed-time');
  const expiredAlert = document.getElementById('res-expired-alert');
  const certBtn = document.getElementById('res-cert-btn');
  const emailNotice = document.getElementById('res-email-notice');

  if (percentEl) percentEl.innerText = `${res.percentage}%`;
  if (pointsEl) pointsEl.innerText = `${res.earnedScore} / ${res.totalScore} ball`;

  const isPassed = res.percentage >= (currentStudentTest?.passingPercentage || 60);

  if (statusTitle) {
    if (isPassed) {
      statusTitle.innerText = 'Muvaffaqiyatli O\'tdingiz! 🎉';
      statusTitle.className = 'status-passed';
    } else {
      statusTitle.innerText = 'Afsuski Yiqildingiz! ❌';
      statusTitle.className = 'status-failed';
    }
  }

  if (certBtn) {
    certBtn.style.display = isPassed ? 'inline-block' : 'none';
  }

  if (emailNotice) {
    emailNotice.style.display = 'block';
  }

  if (expiredAlert) {
    expiredAlert.style.display = res.isExpired ? 'block' : 'none';
  }

  const passedDate = new Date(res.passedAt).toLocaleString('uz-UZ');
  
  // Format duration
  const mins = Math.floor(res.durationSeconds / 60);
  const secs = res.durationSeconds % 60;
  const durationText = `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;

  if (passedTimeEl) {
    passedTimeEl.innerHTML = `Topshirilgan vaqt: <strong>${passedDate}</strong><br>Sarflangan vaqt: <strong>${durationText}</strong>`;
  }

  openModal('modal-result');
}

// ==================== STUDENT PROFILE ====================
async function loadStudentProfile() {
  const nameEl = document.getElementById('profile-fullname');
  const emailEl = document.getElementById('profile-email');
  if (!nameEl || !emailEl) return;

  try {
    const res = await fetchWithAuth(`${API_BASE}/profile`);
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Profil ma\'lumotlarini yuklab bo\'lmadi');

    nameEl.value = data.fullName || '';
    emailEl.value = data.email || '';
  } catch (err) {
    console.error(err);
  }
}

async function handleUpdateProfile(e) {
  if (e) e.preventDefault();
  const nameEl = document.getElementById('profile-fullname');
  const emailEl = document.getElementById('profile-email');
  const passEl = document.getElementById('profile-newpassword');

  const fullName = nameEl ? nameEl.value.trim() : '';
  const email = emailEl ? emailEl.value.trim() : '';
  const newPassword = passEl ? passEl.value : '';

  try {
    const res = await fetchWithAuth(`${API_BASE}/profile`, {
      method: 'PUT',
      body: { fullName, email, newPassword: newPassword || null }
    });

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Profilni yangilab bo\'lmadi');

    currentUser.name = data.fullName;
    currentUser.email = data.email;
    localStorage.setItem('tp_user', JSON.stringify(currentUser));
    applyUserSession();

    if (passEl) passEl.value = '';
    alert('Profil ma\'lumotlari muvaffaqiyatli yangilandi! 🎉');
  } catch (err) {
    alert(err.message);
  }
}

// ==================== MY ATTEMPTS ====================
async function loadStudentAttempts() {
  const tbody = document.getElementById('student-attempts-tbody');
  if (!tbody) return;
  tbody.innerHTML = '<tr><td colspan="7" class="empty-state">Yuklanmoqda...</td></tr>';

  try {
    const res = await fetchWithAuth(`${API_BASE}/profile/attempts`);
    const attempts = await res.json();

    if (!Array.isArray(attempts) || attempts.length === 0) {
      tbody.innerHTML = '<tr><td colspan="7" class="empty-state">Hali test topshirmagansiz.</td></tr>';
      return;
    }

    tbody.innerHTML = attempts.map(a => {
      const formattedDate = new Date(a.passedAt).toLocaleString('uz-UZ');
      const isPassed = a.isPassed;
      const statusBadge = isPassed ? 
        '<span class="badge-status-passed">PASSED</span>' : 
        '<span class="badge-status-failed">FAILED</span>';

      return `
        <tr>
          <td><strong>${escapeHtml(a.testTitle)}</strong></td>
          <td>${escapeHtml(a.subjectName)}</td>
          <td>${a.earnedScore} / ${a.totalScore}</td>
          <td><strong>${a.percentage}%</strong></td>
          <td>${statusBadge}</td>
          <td>${formattedDate}</td>
          <td>
            <div style="display: flex; gap: 0.4rem;">
              <button class="btn btn-sm btn-secondary" onclick="openReviewForAttempt('${a.attemptId}')">
                🔍 Review
              </button>
              ${isPassed ? `
                <button class="btn btn-sm btn-primary-blue" onclick="openCertificate('${a.attemptId}')">
                  📜 Sertifikat
                </button>
              ` : ''}
            </div>
          </td>
        </tr>
      `;
    }).join('');
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="7" class="empty-state" style="color: var(--danger-color);">${err.message}</div>`;
  }
}

async function openReviewForAttempt(attemptId) {
  currentAttemptId = attemptId;
  openReviewMistakesModal();
}

// ==================== LEADERBOARD ====================
async function loadAdminLeaderboardTab() {
  const select = document.getElementById('admin-leaderboard-test-select');
  if (!select) return;
  select.innerHTML = '<option value="global">🏆 Global Leaderboard (Barcha Testlar)</option>';

  try {
    const res = await fetchWithAuth(`${API_BASE}/tests?pageSize=50`);
    const data = await res.json();
    const tests = data.items || [];

    tests.forEach(t => {
      const opt = document.createElement('option');
      opt.value = t.id;
      opt.innerText = t.title;
      select.appendChild(opt);
    });
  } catch (e) {
    console.error('Error loading leaderboard test dropdown:', e);
  }

  loadAdminLeaderboard();
}

async function loadAdminLeaderboard() {
  const select = document.getElementById('admin-leaderboard-test-select');
  const tbody = document.getElementById('admin-leaderboard-tbody');
  if (!tbody || !select) return;

  const testId = select.value;
  tbody.innerHTML = '<tr><td colspan="7" class="empty-state">Yuklanmoqda...</td></tr>';

  try {
    let url = `${API_BASE}/leaderboard/global`;
    if (testId && testId !== 'global') {
      url = `${API_BASE}/tests/${testId}/leaderboard`;
    }

    const res = await fetchWithAuth(url);
    const list = await res.json();

    if (!Array.isArray(list) || list.length === 0) {
      tbody.innerHTML = '<tr><td colspan="7" class="empty-state">Leaderboard ma\'lumotlari topilmadi.</td></tr>';
      return;
    }

    tbody.innerHTML = list.map(item => {
      let rankClass = 'rank-other';
      if (item.rank === 1) rankClass = 'rank-1';
      else if (item.rank === 2) rankClass = 'rank-2';
      else if (item.rank === 3) rankClass = 'rank-3';

      const mins = Math.floor(item.durationSeconds / 60);
      const secs = item.durationSeconds % 60;
      const durationFormatted = `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
      const dateFormatted = new Date(item.submittedAt).toLocaleString('uz-UZ');

      return `
        <tr>
          <td><span class="rank-badge ${rankClass}">#${item.rank}</span></td>
          <td><strong>${escapeHtml(item.studentName)}</strong></td>
          <td>${escapeHtml(item.testTitle)}</td>
          <td>${item.score} ball</td>
          <td><strong>${item.percentage}%</strong></td>
          <td>${durationFormatted}</td>
          <td>${dateFormatted}</td>
        </tr>
      `;
    }).join('');
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="7" class="empty-state" style="color: var(--danger-color);">${err.message}</td></tr>`;
  }
}

async function loadStudentLeaderboardTab() {
  const select = document.getElementById('student-leaderboard-test-select');
  if (!select) return;
  select.innerHTML = '<option value="global">🏆 Global Leaderboard (Barcha Testlar)</option>';

  try {
    const res = await fetchWithAuth(`${API_BASE}/tests?pageSize=50`);
    const data = await res.json();
    const tests = data.items || [];

    tests.forEach(t => {
      const opt = document.createElement('option');
      opt.value = t.id;
      opt.innerText = t.title;
      select.appendChild(opt);
    });
  } catch (e) {
    console.error(e);
  }

  loadStudentLeaderboard();
}

async function loadStudentLeaderboard() {
  const select = document.getElementById('student-leaderboard-test-select');
  const tbody = document.getElementById('student-leaderboard-tbody');
  if (!tbody || !select) return;

  const testId = select.value;
  tbody.innerHTML = '<tr><td colspan="7" class="empty-state">Yuklanmoqda...</td></tr>';

  try {
    let url = `${API_BASE}/leaderboard/global`;
    if (testId && testId !== 'global') {
      url = `${API_BASE}/tests/${testId}/leaderboard`;
    }

    const res = await fetchWithAuth(url);
    const list = await res.json();

    if (!Array.isArray(list) || list.length === 0) {
      tbody.innerHTML = '<tr><td colspan="7" class="empty-state">Leaderboard ma\'lumotlari topilmadi.</td></tr>';
      return;
    }

    tbody.innerHTML = list.map(item => {
      let rankClass = 'rank-other';
      if (item.rank === 1) rankClass = 'rank-1';
      else if (item.rank === 2) rankClass = 'rank-2';
      else if (item.rank === 3) rankClass = 'rank-3';

      const mins = Math.floor(item.durationSeconds / 60);
      const secs = item.durationSeconds % 60;
      const durationFormatted = `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
      const dateFormatted = new Date(item.submittedAt).toLocaleString('uz-UZ');

      return `
        <tr>
          <td><span class="rank-badge ${rankClass}">#${item.rank}</span></td>
          <td><strong>${escapeHtml(item.studentName)}</strong></td>
          <td>${escapeHtml(item.testTitle)}</td>
          <td>${item.score} ball</td>
          <td><strong>${item.percentage}%</strong></td>
          <td>${durationFormatted}</td>
          <td>${dateFormatted}</td>
        </tr>
      `;
    }).join('');
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="7" class="empty-state" style="color: var(--danger-color);">${err.message}</td></tr>`;
  }
}

// ==================== AUDIT LOGS ====================
async function loadAdminAuditLogs() {
  const tbody = document.getElementById('admin-auditlogs-tbody');
  if (!tbody) return;

  const actionInput = document.getElementById('audit-filter-action');
  const entityInput = document.getElementById('audit-filter-entity');

  const action = actionInput ? actionInput.value.trim() : '';
  const entityName = entityInput ? entityInput.value.trim() : '';

  tbody.innerHTML = '<tr><td colspan="5" class="empty-state">Yuklanmoqda...</td></tr>';

  try {
    const res = await fetchWithAuth(`${API_BASE}/auditlogs?page=${auditLogsPageState.page}&pageSize=${auditLogsPageState.pageSize}&action=${encodeURIComponent(action)}&entityName=${encodeURIComponent(entityName)}`);
    const data = await res.json();

    auditLogsPageState.totalPages = data.totalPages || 1;
    document.getElementById('audit-page-info').innerText = `Sahifa ${auditLogsPageState.page} / ${auditLogsPageState.totalPages}`;

    document.getElementById('audit-prev-btn').disabled = auditLogsPageState.page <= 1;
    document.getElementById('audit-next-btn').disabled = auditLogsPageState.page >= auditLogsPageState.totalPages;

    const logs = data.items || [];
    if (logs.length === 0) {
      tbody.innerHTML = '<tr><td colspan="5" class="empty-state">Audit loglar topilmadi.</td></tr>';
      return;
    }

    tbody.innerHTML = logs.map(l => {
      const dateFormatted = new Date(l.createdAt).toLocaleString('uz-UZ');
      
      let actionBadge = 'action-update';
      const actLower = l.action.toLowerCase();
      if (actLower.includes('create') || actLower.includes('add') || actLower.includes('start') || actLower.includes('submit')) actionBadge = 'action-create';
      else if (actLower.includes('delete') || actLower.includes('remove')) actionBadge = 'action-delete';

      const detailsText = l.newValue ? `Yangi: ${escapeHtml(l.newValue)}` : (l.oldValue ? `Eski: ${escapeHtml(l.oldValue)}` : `ID: ${l.entityId.substring(0, 8)}...`);

      return `
        <tr>
          <td>${dateFormatted}</td>
          <td><strong>${escapeHtml(l.userName)}</strong></td>
          <td><span class="audit-action-badge ${actionBadge}">${escapeHtml(l.action)}</span></td>
          <td>${escapeHtml(l.entityName)}</td>
          <td style="font-size: 0.85rem; color: var(--text-muted);">${detailsText}</td>
        </tr>
      `;
    }).join('');
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="5" class="empty-state" style="color: var(--danger-color);">${err.message}</td></tr>`;
  }
}

function changeAuditPage(offset) {
  const nextPage = auditLogsPageState.page + offset;
  if (nextPage > 0 && nextPage <= auditLogsPageState.totalPages) {
    auditLogsPageState.page = nextPage;
    loadAdminAuditLogs();
  }
}

// ==================== CERTIFICATE FUNCTIONS ====================
async function openCertificate(attemptId) {
  const container = document.getElementById('cert-svg-container');
  if (!container) return;

  container.innerHTML = '<div class="empty-state">Sertifikat yaratilmoqda...</div>';
  openModal('modal-certificate-preview');

  try {
    const res = await fetchWithAuth(`${API_BASE}/attempts/${attemptId}/certificate`, { method: 'POST' });
    const certDetails = await res.json();
    if (!res.ok) throw new Error(certDetails.message || 'Sertifikat yaratishda xatolik');

    // Validate certificate number
    if (!certDetails.certificateNumber || !certDetails.certificateNumber.startsWith('TP-')) {
      throw new Error('Sertifikat raqami yaratilmadi. Iltimos qayta urinib ko\'ring.');
    }

    currentCertificate = certDetails;

    const encodedNum = encodeURIComponent(certDetails.certificateNumber);
    const svgRes = await fetch(`${API_BASE}/certificates/${encodedNum}/download`);
    if (!svgRes.ok) throw new Error('Sertifikat faylini yuklab bo\'lmadi (404)');
    const svgText = await svgRes.text();

    container.innerHTML = svgText;
  } catch (err) {
    console.error('Certificate error:', err);
    container.innerHTML = `<div class="empty-state" style="color: var(--danger-color);">❌ ${err.message}</div>`;
  }
}

function openCertificateFromCurrentAttempt() {
  if (currentAttemptId) {
    openCertificate(currentAttemptId);
  }
}

function downloadCurrentCertificate() {
  if (!currentCertificate || !currentCertificate.certificateNumber) {
    return alert('Yuklab olish uchun sertifikat topilmadi');
  }

  window.open(`${API_BASE}/certificates/${currentCertificate.certificateNumber}/download`, '_blank');
}

async function handleVerifyCertificate() {
  const numInput = document.getElementById('verify-cert-number');
  const resultDiv = document.getElementById('verify-cert-result');
  if (!numInput || !resultDiv) return;

  const number = numInput.value.trim();
  if (!number) return alert('Sertifikat raqamini kiriting!');

  resultDiv.style.display = 'block';
  resultDiv.innerHTML = '<div class="empty-state">Tekshirilmoqda...</div>';

  try {
    const res = await fetch(`${API_BASE}/certificates/${encodeURIComponent(number)}`);
    if (!res.ok) {
      resultDiv.innerHTML = `
        <div style="background: rgba(239,68,68,0.15); border: 1px solid var(--danger-color); padding: 1.2rem; border-radius: 8px; text-align: center;">
          <h4 style="color: var(--danger-color); margin-bottom: 0.5rem;">❌ Sertifikat Topilmadi</h4>
          <p style="font-size: 0.9rem;">"${escapeHtml(number)}" raqamli sertifikat tizimda mavjud emas yoki soxtalashtirilgan.</p>
        </div>
      `;
      return;
    }

    const cert = await res.json();
    const formattedDate = new Date(cert.issuedAt).toLocaleDateString('uz-UZ');

    resultDiv.innerHTML = `
      <div style="background: rgba(16,185,129,0.15); border: 1px solid var(--accent-green); padding: 1.2rem; border-radius: 8px;">
        <h4 style="color: #34d399; margin-bottom: 0.8rem; display: flex; align-items: center; gap: 0.5rem;">
          ✅ Sertifikat Tasdiqlandi (Verified)
        </h4>
        <div style="font-size: 0.9rem; display: flex; flex-direction: column; gap: 0.4rem; color: var(--text-main);">
          <div>Egasining ismi: <strong>${escapeHtml(cert.studentName)}</strong></div>
          <div>Test fani / nomi: <strong>${escapeHtml(cert.testTitle)}</strong></div>
          <div>Natija ko'rsatkichi: <strong>${cert.percentage}%</strong></div>
          <div>Berilgan sana: <strong>${formattedDate}</strong></div>
          <div>Sertifikat raqami: <strong>${escapeHtml(cert.certificateNumber)}</strong></div>
        </div>
      </div>
    `;
  } catch (err) {
    resultDiv.innerHTML = `<div class="empty-state" style="color: var(--danger-color);">${err.message}</div>`;
  }
}

// ==================== SECURE REVIEW MISTAKES ====================

async function openReviewMistakesModal() {
  if (!currentAttemptId) {
    return alert('Tahlil qilish uchun urinish IDsi topilmadi.');
  }

  const container = document.getElementById('review-mistakes-content');
  if (!container) return;

  container.innerHTML = '<div class="empty-state">Tahlil yuklanmoqda...</div>';
  openModal('modal-review-mistakes');

  try {
    const res = await fetchWithAuth(`${API_BASE}/attempts/${currentAttemptId}/review`);
    const reviewData = await res.json();
    if (!res.ok) throw new Error(reviewData.message || 'Tahlilni yuklab bo\'lmadi');

    container.innerHTML = (reviewData.questions || []).map((q, idx) => {
      return `
        <div class="review-item" style="margin-bottom: 1.5rem; background: rgba(255,255,255,0.02); padding: 1.2rem; border-radius: var(--radius-md); border: 1px solid var(--border-color);">
          <div style="font-weight: 700; margin-bottom: 0.8rem; font-size: 1.05rem;">
            ${idx + 1}. ${escapeHtml(q.text)} (${q.points} ball)
          </div>
          <div style="display: flex; flex-direction: column; gap: 0.5rem;">
            ${(q.options || []).map(opt => {
              let classNames = 'review-option review-opt-normal';
              let badgeText = '';

              const isSelected = opt.id === q.selectedOptionId;

              if (opt.isCorrect && isSelected) {
                classNames = 'review-option review-opt-correct';
                badgeText = '✅ Sizning to\'g\'ri javobingiz';
              } else if (opt.isCorrect) {
                classNames = 'review-option review-opt-correct';
                badgeText = '✨ To\'g\'ri javob';
              } else if (isSelected) {
                classNames = 'review-option review-opt-wrong';
                badgeText = '❌ Sizning xato javobingiz';
              }

              return `
                <div class="${classNames}" style="display: flex; justify-content: space-between; align-items: center; padding: 0.75rem 1rem; border-radius: 8px;">
                  <span>${escapeHtml(opt.text)}</span>
                  ${badgeText ? `<span style="font-size: 0.8rem; font-weight: 600;">${badgeText}</span>` : ''}
                </div>
              `;
            }).join('')}
          </div>
        </div>
      `;
    }).join('');
  } catch (err) {
    container.innerHTML = `<div class="empty-state" style="color: var(--danger-color);">${err.message}</div>`;
  }
}

// Helpers
function escapeHtml(str) {
  if (!str) return '';
  return str.replace(/[&<>"']/g, function(m) {
    return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' }[m];
  });
}

function escapeJs(str) {
  if (!str) return '';
  return str.replace(/'/g, "\\'");
}

// Initial Load & Direct URL Parameter Handling
document.addEventListener('DOMContentLoaded', () => {
  const urlParams = new URLSearchParams(window.location.search);
  const directTestId = urlParams.get('testId');

  if (directTestId) {
    if (!token) {
      alert("Testni topshirishdan avval login qilishingiz yoki ro'yxatdan o'tishingiz shart!");
    } else {
      const nav = document.getElementById('navbar');
      const viewLogin = document.getElementById('view-login');
      const viewAdmin = document.getElementById('view-admin');
      const viewStudent = document.getElementById('view-student');

      if (nav) nav.style.display = 'flex';
      if (viewLogin) viewLogin.style.display = 'none';
      if (viewAdmin) viewAdmin.style.display = 'none';
      if (viewStudent) viewStudent.style.display = 'block';

      startStudentTest(directTestId);
      return;
    }
  }

  applyUserSession();
});

async function loadStudentProfile() {
  try {
    const res = await fetchWithAuth(`${API_BASE}/profile`);
    if (!res.ok) return;
    const profile = await res.json();

    const nameEl = document.getElementById('profile-fullname');
    const emailEl = document.getElementById('profile-email');
    const avatarImg = document.getElementById('profile-avatar-img');

    if (nameEl) nameEl.value = profile.fullName;
    if (emailEl) emailEl.value = profile.email;
    if (avatarImg) {
      if (profile.avatarUrl) {
        avatarImg.src = profile.avatarUrl;
      } else {
        avatarImg.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(profile.fullName)}&background=4f46e5&color=fff&size=100`;
      }
    }
  } catch (err) {
    console.error('Profile loading error:', err);
  }
}

async function handleAvatarUpload(input) {
  if (!input || !input.files || input.files.length === 0) return;

  const file = input.files[0];
  const formData = new FormData();
  formData.append('file', file);

  try {
    const res = await fetchWithAuth(`${API_BASE}/profile/upload-avatar`, {
      method: 'POST',
      body: formData
    });

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Avatar yuklashda xatolik!');

    const avatarImg = document.getElementById('profile-avatar-img');
    if (avatarImg) avatarImg.src = data.avatarUrl;

    if (currentUser) {
      currentUser.avatarUrl = data.avatarUrl;
      localStorage.setItem('tp_user', JSON.stringify(currentUser));
    }

    alert('Profil rasmi muvaffaqiyatli yuklandi! 📸');
  } catch (err) {
    alert(err.message);
  }
}

async function handleUpdateProfile(e) {
  if (e) e.preventDefault();

  const fullName = document.getElementById('profile-fullname')?.value.trim();
  const email = document.getElementById('profile-email')?.value.trim();
  const newPassword = document.getElementById('profile-newpassword')?.value;

  if (!fullName || !email) return alert('Ism va emailni to\'ldiring!');

  try {
    const res = await fetchWithAuth(`${API_BASE}/profile`, {
      method: 'PUT',
      body: JSON.stringify({ fullName, email, newPassword: newPassword || null })
    });

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Profilni yangilashda xatolik!');

    if (currentUser) {
      currentUser.name = data.fullName;
      currentUser.email = data.email;
      localStorage.setItem('tp_user', JSON.stringify(currentUser));
    }

    applyUserSession();
    alert('Profil ma\'lumotlari yangilandi! 💾');
  } catch (err) {
    alert(err.message);
  }
}

// Bind globally for inline onclick handlers
window.switchAuthTab = switchAuthTab;
window.handleSendOtpClick = handleSendOtpClick;
window.handleLoginSubmit = handleLoginSubmit;
window.handleRegisterSubmit = handleRegisterSubmit;
window.logout = logout;
window.switchToStudentPortalView = switchToStudentPortalView;
window.switchAdminTab = switchAdminTab;
window.switchStudentTab = switchStudentTab;
window.openModal = openModal;
window.closeModal = closeModal;
window.deleteSubject = deleteSubject;
window.handleCreateSubject = handleCreateSubject;
window.loadTopics = loadTopics;
window.handleCreateTopic = handleCreateTopic;
window.deleteTopic = deleteTopic;
window.togglePublish = togglePublish;
window.deleteTest = deleteTest;
window.openCreateTestModal = openCreateTestModal;
window.handleCreateTest = handleCreateTest;
window.openAddQuestionModal = openAddQuestionModal;
window.switchQuestionTab = switchQuestionTab;
window.handleCreateQuestion = handleCreateQuestion;
window.handleUploadQuestionsFile = handleUploadQuestionsFile;
window.downloadQuestionTemplate = downloadQuestionTemplate;
window.downloadExcelTemplate = downloadExcelTemplate;
window.loadAdminResultsForSelectedTest = loadAdminResultsForSelectedTest;
window.exportResultsToExcel = exportResultsToExcel;
window.startStudentTest = startStudentTest;
window.selectOption = selectOption;
window.prevQuestion = prevQuestion;
window.nextQuestion = nextQuestion;
window.cancelQuiz = cancelQuiz;
window.submitQuiz = submitQuiz;
window.openReviewMistakesModal = openReviewMistakesModal;
window.handleSubjectFilterChange = handleSubjectFilterChange;
window.changeSubjectPage = changeSubjectPage;
window.handleTestFilterChange = handleTestFilterChange;
window.changeTestPage = changeTestPage;
window.handleStudentTestFilterChange = handleStudentTestFilterChange;
window.changeStudentTestPage = changeStudentTestPage;
window.loadStudentProfile = loadStudentProfile;
window.handleAvatarUpload = handleAvatarUpload;
window.handleUpdateProfile = handleUpdateProfile;
window.loadStudentAttempts = loadStudentAttempts;
window.openReviewForAttempt = openReviewForAttempt;
window.loadAdminLeaderboardTab = loadAdminLeaderboardTab;
window.loadAdminLeaderboard = loadAdminLeaderboard;
window.loadStudentLeaderboardTab = loadStudentLeaderboardTab;
window.loadStudentLeaderboard = loadStudentLeaderboard;
window.loadAdminAuditLogs = loadAdminAuditLogs;
window.changeAuditPage = changeAuditPage;
window.openCertificate = openCertificate;
window.openCertificateFromCurrentAttempt = openCertificateFromCurrentAttempt;
window.downloadCurrentCertificate = downloadCurrentCertificate;
window.handleVerifyCertificate = handleVerifyCertificate;
