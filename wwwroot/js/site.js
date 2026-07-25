function showToast(message, type) {
    type = type || 'success';
    var existing = document.querySelector('.bh-toast-container');
    if (!existing) {
        existing = document.createElement('div');
        existing.className = 'bh-toast-container';
        existing.style.cssText = 'position:fixed;top:20px;left:20px;z-index:99999;display:flex;flex-direction:column;gap:10px;max-width:400px;';
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

window.portalRejectModal = function () {
    var modal = document.getElementById('reject-modal-overlay');
    if (modal && modal.parentElement !== document.body) {
        document.body.appendChild(modal);
    }
};

window.closeRejectModalPortal = function () {
    var modal = document.getElementById('reject-modal-overlay');
    if (modal) modal.remove();
};

window.__userCurrencyCache = null;
window.__getUserCurrency = function() {
    if (window.__userCurrencyCache) return Promise.resolve(window.__userCurrencyCache);
    return fetch('https://ipapi.co/json/')
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (data.currency_code) {
                window.__userCurrencyCache = data.currency_code;
                return data.currency_code;
            }
            return 'PKR';
        })
        .catch(function() { return 'PKR'; });
};
