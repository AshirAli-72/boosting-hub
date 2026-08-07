// ── Dashboard Sidebar Toggle (mobile) ────────────────────────────────────────
function toggleSidebar() {
    var sidebar  = document.querySelector('.admin-sidebar');
    var overlay  = document.getElementById('sidebarOverlay');
    var toggleBtn = document.getElementById('sidebarToggle');
    if (!sidebar) return;
    var isOpen = sidebar.classList.toggle('sidebar-open');
    if (overlay) overlay.classList.toggle('open', isOpen);
    if (toggleBtn) toggleBtn.innerHTML = isOpen ? '<i class="bi bi-x-lg"></i>' : '<i class="bi bi-list"></i>';
    document.body.style.overflow = isOpen ? 'hidden' : '';
}
// Close sidebar on Escape
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
        var sidebar = document.querySelector('.admin-sidebar');
        if (sidebar && sidebar.classList.contains('sidebar-open')) toggleSidebar();
    }
});
// Close sidebar when a nav link is clicked on mobile
document.addEventListener('click', function(e) {
    var link = e.target.closest('.admin-sidebar .sidebar-link');
    if (link) {
        var sidebar = document.querySelector('.admin-sidebar');
        if (sidebar && sidebar.classList.contains('sidebar-open')) toggleSidebar();
    }
});

// ── Sidebar Theme Toggle (inside mobile sidebar) ─────────────────────────────
function sidebarToggleTheme() {
    var isLight = document.body.classList.toggle('light-mode');
    document.documentElement.classList.toggle('light-mode', isLight);
    localStorage.setItem('theme', isLight ? 'light' : 'dark');
    // Sync all theme-icon elements
    document.querySelectorAll('[data-theme-icon]').forEach(function(el) {
        el.className = isLight ? 'bi bi-sun-fill' : 'bi bi-moon-stars';
    });
    // Sync header theme toggle buttons
    document.querySelectorAll('#themeToggle i').forEach(function(el) {
        el.className = isLight ? 'bi bi-sun-fill fs-5' : 'bi bi-moon-stars fs-5';
    });
}

// ── Sync sidebar theme icon on load ──────────────────────────────────────────
(function syncSidebarThemeIcon() {
    var isLight = document.body.classList.contains('light-mode');
    document.querySelectorAll('[data-theme-icon]').forEach(function(el) {
        el.className = isLight ? 'bi bi-sun-fill' : 'bi bi-moon-stars';
    });
})();

// ── Toast Notifications ─────────────────────────────────────────────────────
function showToast(message, type) {
    type = type || 'success';
    var existing = document.querySelector('.bh-toast-container');
    if (!existing) {
        existing = document.createElement('div');
        existing.className = 'bh-toast-container';
        existing.style.cssText = 'position:fixed;top:20px;right:20px;z-index:99999;display:flex;flex-direction:column;gap:10px;max-width:400px;';
        document.body.appendChild(existing);
    }
    var toast = document.createElement('div');
    var colorMap = { success: '#10B981', danger: '#EF4444', warning: '#F59E0B', info: '#3B82F6' };
    var bgMap = { success: 'rgba(16,185,129,0.12)', danger: 'rgba(239,68,68,0.12)', warning: 'rgba(245,158,11,0.12)', info: 'rgba(59,130,246,0.12)' };
    var iconMap = { success: 'check-circle-fill', danger: 'exclamation-triangle-fill', warning: 'exclamation-circle-fill', info: 'info-circle-fill' };
    var color = colorMap[type] || '#10B981';
    var bg = bgMap[type] || 'rgba(16,185,129,0.12)';
    var icon = iconMap[type] || 'check-circle-fill';
    toast.style.cssText = 'display:flex;align-items:center;gap:10px;padding:12px 16px;border-radius:12px;font-size:0.88rem;font-weight:500;border-left:4px solid ' + color + ';background:' + bg + ';color:' + color + ';box-shadow:0 4px 16px rgba(0,0,0,0.08);transform:translateX(-120%);opacity:0;transition:all 0.35s cubic-bezier(0.22,1,0.36,1);';
    toast.innerHTML = '<i class="bi bi-' + icon + '" style="font-size:1.1rem;flex-shrink:0;"></i><span style="flex:1;">' + message + '</span><button onclick="this.parentElement.remove()" style="background:none;border:none;color:' + color + ';font-size:1.2rem;cursor:pointer;padding:0;line-height:1;opacity:0.6;flex-shrink:0;">&times;</button>';
    existing.appendChild(toast);
    requestAnimationFrame(function () { toast.style.transform = 'translateX(0)'; toast.style.opacity = '1'; });
    setTimeout(function () {
        toast.style.transform = 'translateX(-120%)';
        toast.style.opacity = '0';
        setTimeout(function () { toast.remove(); }, 350);
    }, 4500);
}

window._rejectDotNetRef = null;
window._rejectProofId = 0;

window.showRejectModal = function (dotNetRef, proofId) {
    window._rejectDotNetRef = dotNetRef;
    window._rejectProofId = proofId;
    window.closeRejectModal();
    var overlay = document.createElement('div');
    overlay.id = 'bh-reject-overlay';
    overlay.style.cssText = 'position:fixed;inset:0;z-index:99999;display:flex;align-items:center;justify-content:center;background:rgba(0,0,0,0.5);backdrop-filter:blur(2px);';
    overlay.innerHTML =
        '<div style="background:' + getComputedStyle(document.documentElement).getPropertyValue('--bg-card').trim() + ',#1e293b);border:1px solid rgba(255,255,255,0.1);border-radius:16px;box-shadow:0 20px 60px rgba(0,0,0,0.3);width:440px;max-width:92vw;overflow:hidden;">' +
        '  <div style="padding:20px 24px;border-bottom:1px solid rgba(255,255,255,0.06);display:flex;align-items:center;justify-content:space-between;">' +
        '    <h5 style="margin:0;font-size:1rem;font-weight:600;color:#f1f5f9;"><i class="bi bi-x-circle text-danger me-2"></i>Reject Proof #' + proofId + '</h5>' +
        '    <button onclick="window._rejectCancel()" style="background:none;border:none;color:#94a3b8;font-size:1.2rem;cursor:pointer;padding:0;line-height:1;">&times;</button>' +
        '  </div>' +
        '  <div style="padding:20px 24px;">' +
        '    <div style="margin-bottom:16px;">' +
        '      <label style="display:block;margin-bottom:6px;font-weight:500;font-size:0.85rem;color:#94a3b8;">Rejection Reason</label>' +
        '      <textarea id="bh-reject-reason" rows="3" placeholder="Provide a reason for rejection..." style="width:100%;background:rgba(255,255,255,0.05);border:1px solid rgba(255,255,255,0.1);color:#f1f5f9;border-radius:10px;padding:10px 14px;font-size:0.9rem;resize:vertical;outline:none;"></textarea>' +
        '    </div>' +
        '    <div id="bh-reject-error" style="display:none;padding:10px 14px;border-radius:8px;font-size:0.85rem;margin-bottom:12px;background:rgba(239,68,68,0.12);color:#F87171;border-left:4px solid #EF4444;"></div>' +
        '    <div style="display:flex;justify-content:flex-end;gap:8px;">' +
        '      <button onclick="window._rejectCancel()" style="background:rgba(255,255,255,0.08);color:#f1f5f9;border:1px solid rgba(255,255,255,0.1);border-radius:8px;padding:8px 16px;font-size:0.85rem;cursor:pointer;">Cancel</button>' +
        '      <button id="bh-reject-btn" onclick="window._rejectConfirm()" style="background:#EF4444;color:#fff;border:none;border-radius:8px;padding:8px 16px;font-size:0.85rem;cursor:pointer;"><i class="bi bi-x-lg me-1"></i>Reject</button>' +
        '    </div>' +
        '  </div>' +
        '</div>';
    document.body.appendChild(overlay);
    overlay.addEventListener('click', function (e) { if (e.target === overlay) window._rejectCancel(); });
    setTimeout(function () { document.getElementById('bh-reject-reason').focus(); }, 100);
};

window._rejectConfirm = function () {
    var reason = document.getElementById('bh-reject-reason').value.trim();
    if (!reason) {
        var errEl = document.getElementById('bh-reject-error');
        errEl.textContent = 'Please provide a rejection reason.';
        errEl.style.display = 'block';
        return;
    }
    if (!window._rejectDotNetRef) {
        window.showRejectModalError('Connection lost. Please refresh the page.');
        return;
    }
    var btn = document.getElementById('bh-reject-btn');
    btn.disabled = true;
    btn.innerHTML = '<i class="bi bi-hourglass-split me-1"></i>Rejecting...';
    window._rejectDotNetRef.invokeMethodAsync('OnRejectConfirm', window._rejectProofId, reason);
};

window._rejectCancel = function () {
    window.closeRejectModal();
    if (window._rejectDotNetRef) {
        window._rejectDotNetRef.invokeMethodAsync('OnRejectCancel');
    }
};

window.closeRejectModal = function () {
    var el = document.getElementById('bh-reject-overlay');
    if (el) el.remove();
};

window.showRejectModalError = function (msg) {
    var errEl = document.getElementById('bh-reject-error');
    if (errEl) { errEl.textContent = msg; errEl.style.display = 'block'; }
    var btn = document.getElementById('bh-reject-btn');
    if (btn) { btn.disabled = false; btn.innerHTML = '<i class="bi bi-x-lg me-1"></i>Reject'; }
};

window.showImageOverlay = function (imagePath) {
    var overlay = document.createElement('div');
    overlay.id = 'bh-image-overlay';
    overlay.style.cssText = 'position:fixed;inset:0;z-index:99999;display:flex;align-items:center;justify-content:center;background:rgba(0,0,0,0.7);backdrop-filter:blur(4px);';
    overlay.innerHTML =
        '<div style="position:relative;max-width:90vw;max-height:90vh;">' +
        '  <button onclick="document.getElementById(\'bh-image-overlay\').remove()" style="position:absolute;top:-40px;right:0;background:none;border:none;color:#fff;font-size:1.8rem;cursor:pointer;padding:4px;line-height:1;opacity:0.7;">&times;</button>' +
        '  <img src="' + imagePath + '" alt="Proof screenshot" style="max-width:100%;max-height:85vh;border-radius:12px;box-shadow:0 20px 60px rgba(0,0,0,0.5);" />' +
        '</div>';
    document.body.appendChild(overlay);
    overlay.addEventListener('click', function (e) { if (e.target === overlay) overlay.remove(); });
};

window.__userCurrencyCache = null;
window.__getUserCurrency = function() {
    if (window.__userCurrencyCache) return Promise.resolve(window.__userCurrencyCache);
    return fetch('/api/geolocation/currency')
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (data.currency) {
                window.__userCurrencyCache = data.currency;
                localStorage.setItem('walletDisplayCurrency', data.currency);
                return data.currency;
            }
            return null;
        })
        .catch(function() { return null; });
};

window.__pkrRates = {
    'PKR': 1, 'USD': 285, 'EUR': 309.7826086956522, 'GBP': 360.7594936708861,
    'INR': 3.428777671080848, 'BDT': 2.594447883477469, 'SAR': 76, 'AED': 77.6566757493188,
    'CAD': 208, 'AUD': 186, 'TRY': 8.76923076923077, 'BRL': 56,
    'JPY': 1.906354515050167, 'CNY': 39.3646408839779, 'KRW': 0.215909090909091, 'NGN': 0.185064935064935,
    'PHP': 5.08928571428571, 'IDR': 0.0181528662420382, 'MYR': 60.6382978723404, 'THB': 8.02816901408451,
    'EGP': 5.90062111801242, 'ZAR': 15.4054054054054, 'MXN': 16.6666666666667,
    'KWD': 930, 'QAR': 78, 'BHD': 755, 'OMR': 742
};
window.__currencySymbols = {
    'PKR': '\u20A8', 'INR': '\u20B9', 'BDT': '\u09F3', 'GBP': '\u00A3',
    'EUR': '\u20AC', 'USD': '$', 'CAD': 'C$', 'AUD': 'A$',
    'SAR': '\uFDFC', 'AED': 'AED ', 'TRY': '\u20BA', 'BRL': 'R$',
    'JPY': '\u00A5', 'CNY': '\u00A5', 'KRW': '\u20A9', 'NGN': '\u20A6',
    'PHP': '\u20B1', 'IDR': 'Rp', 'MYR': 'RM', 'THB': '\u0E3F',
    'EGP': 'E\u00A3', 'ZAR': 'R', 'MXN': 'Mex$'
};

window.convertCurrency = function(amount, fromCurrency, toCurrency) {
    if (fromCurrency === toCurrency) return amount;
    var fromRate = window.__pkrRates[fromCurrency];
    var toRate = window.__pkrRates[toCurrency];
    if (!fromRate || !toRate) return amount;
    var inPkr = amount * fromRate;
    return Math.round((inPkr / toRate) * 100) / 100;
};
