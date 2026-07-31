/* Client-side table pagination (10 records per page by default).
   Rows hidden by page-level filters should be marked with class 'filtered-out';
   call window.tablePagerRerender(tableId) after filtering to refresh. */
(function (global) {
    'use strict';

    var PAGERS = {};

    function getActiveRows(pager) {
        return pager.rows.filter(function (r) {
            return !r.classList.contains('filtered-out');
        });
    }

    function renderPager(pager) {
        var active = getActiveRows(pager);
        var totalActive = active.length;
        var pages = Math.max(1, Math.ceil(totalActive / pager.pageSize));
        if (pager.page > pages) pager.page = pages;
        if (pager.page < 1) pager.page = 1;

        var page = pager.page;
        var start = (page - 1) * pager.pageSize;
        var end = Math.min(start + pager.pageSize, totalActive);

        var activeIdx = -1;
        pager.rows.forEach(function (row) {
            if (!row.classList.contains('filtered-out')) {
                activeIdx++;
                row.style.display = (activeIdx >= start && activeIdx < end) ? '' : 'none';
            } else {
                row.style.display = 'none';
            }
        });

        var first = totalActive === 0 ? 0 : start + 1;
        var info = 'Showing ' + first + '\u2013' + end + ' of ' + totalActive + ' records';

        var html = '<div class="table-pager-info">' + info + '</div>';
        html += '<div class="table-pager-controls">';
        html += '<button type="button" class="table-pager-btn" data-act="prev"' + (page <= 1 ? ' disabled' : '') + '>&laquo; Prev</button>';

        var startPg = Math.max(1, page - 2);
        var endPg = Math.min(pages, page + 2);
        for (var i = startPg; i <= endPg; i++) {
            html += '<button type="button" class="table-pager-btn' + (i === page ? ' active' : '') + '" data-page="' + i + '">' + i + '</button>';
        }

        html += '<button type="button" class="table-pager-btn" data-act="next"' + (page >= pages ? ' disabled' : '') + '>Next &raquo;</button>';
        html += '</div>';

        pager.el.innerHTML = html;
    }

    function bindPager(pager) {
        pager.el.addEventListener('click', function (e) {
            var btn = e.target.closest ? e.target.closest('.table-pager-btn') : null;
            if (!btn || btn.disabled) return;
            if (btn.dataset.page) {
                pager.page = parseInt(btn.dataset.page, 10);
            } else if (btn.dataset.act === 'prev') {
                pager.page = Math.max(1, pager.page - 1);
            } else if (btn.dataset.act === 'next') {
                var active = getActiveRows(pager);
                var pages = Math.max(1, Math.ceil(active.length / pager.pageSize));
                pager.page = Math.min(pages, pager.page + 1);
            }
            renderPager(pager);
        });
    }

    function initTablePager(tableId, options) {
        var table = document.getElementById(tableId);
        if (!table) return;
        var opts = options || {};
        var pageSize = opts.pageSize || 10;
        var tbody = table.tBodies[0];
        if (!tbody) return;
        var rows = Array.prototype.slice.call(tbody.rows);

        var pagerEl = document.createElement('div');
        pagerEl.className = 'table-pager';
        var container = table.closest ? (table.closest('.table-responsive') || table.parentNode) : table.parentNode;
        container.parentNode.appendChild(pagerEl);

        var pager = { id: tableId, rows: rows, pageSize: pageSize, page: 1, el: pagerEl };
        PAGERS[tableId] = pager;
        bindPager(pager);
        renderPager(pager);
    }

    function rerenderTablePager(tableId) {
        var pager = PAGERS[tableId];
        if (pager) renderPager(pager);
    }

    global.initTablePager = initTablePager;
    global.tablePagerRerender = rerenderTablePager;
})(window);
