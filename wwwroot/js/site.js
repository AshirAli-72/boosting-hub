function showToast(message, type) {
    type = type || 'success';
    var existing = document.querySelector('.bh-toast-container');
    if (!existing) {
        existing = document.createElement('div');
        existing.className = 'bh-toast-container';
        existing.style.cssText = 'position:fixed;top:20px;right:20px;z-index:99999;display:flex;flex-direction:column;gap:10px;max-width:380px;';
        document.body.appendChild(existing);
    }
    var toast = document.createElement('div');
    var bgMap = { success: '#10B981', danger: '#EF4444', warning: '#F59E0B', info: '#3B82F6' };
    var iconMap = { success: 'check-circle-fill', danger: 'exclamation-triangle-fill', warning: 'exclamation-circle-fill', info: 'info-circle-fill' };
    var bg = bgMap[type] || '#10B981';
    var icon = iconMap[type] || 'check-circle-fill';
    toast.style.cssText = 'display:flex;align-items:center;gap:12px;padding:14px 18px;border-radius:12px;color:#fff;font-size:0.9rem;font-weight:500;box-shadow:0 8px 24px rgba(0,0,0,0.2);background:' + bg + ';transform:translateX(120%);opacity:0;transition:all 0.35s cubic-bezier(0.22,1,0.36,1);';
    toast.innerHTML = '<i class="bi bi-' + icon + '" style="font-size:1.2rem;"></i><span style="flex:1;">' + message + '</span><button onclick="this.parentElement.remove()" style="background:none;border:none;color:#fff;font-size:1.2rem;cursor:pointer;padding:0;line-height:1;opacity:0.7;">&times;</button>';
    existing.appendChild(toast);
    requestAnimationFrame(function () { toast.style.transform = 'translateX(0)'; toast.style.opacity = '1'; });
    setTimeout(function () {
        toast.style.transform = 'translateX(120%)';
        toast.style.opacity = '0';
        setTimeout(function () { toast.remove(); }, 350);
    }, 4500);
}
