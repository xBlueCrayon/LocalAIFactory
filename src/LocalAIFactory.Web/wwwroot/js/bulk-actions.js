/* ============================================================
   bulk-actions.js — reusable multi-select for every grid.
   Markup contract (no per-page JS needed):
     <form data-bulk method="post" asp-action="Bulk">
       <input type="hidden" name="action" data-bulk-action />
       <div class="bulk-bar" data-bulk-bar>
         <span data-bulk-count></span>
         <button type="button" data-action="approve">…</button>   (approve|deprecate|delete|promote)
         <button type="button" data-bulk-clear>Clear</button>
       </div>
       <table>
         <thead>…<input type="checkbox" data-bulk-all>…</thead>
         <tbody>…<input type="checkbox" name="ids" value="N" data-bulk-item>…</tbody>
       </table>
     </form>
   ============================================================ */
(function () {
  var CONFIRM = { 'delete': 'Delete the selected item(s)? This cannot be undone.',
                  'deprecate': 'Deprecate the selected item(s)?' };

  function initForm(form) {
    var all = form.querySelector('[data-bulk-all]');
    var bar = form.querySelector('[data-bulk-bar]');
    var countEl = form.querySelector('[data-bulk-count]');
    var actionInput = form.querySelector('[data-bulk-action]');
    var items = function () { return Array.prototype.slice.call(form.querySelectorAll('[data-bulk-item]')); };

    function selected() { return items().filter(function (c) { return c.checked; }); }

    function refresh() {
      var sel = selected().length;
      var total = items().length;
      if (countEl) countEl.textContent = sel + ' selected';
      if (bar) bar.classList.toggle('show', sel > 0);
      if (all) {
        all.checked = sel > 0 && sel === total;
        all.indeterminate = sel > 0 && sel < total;
      }
    }

    if (all) {
      all.addEventListener('change', function () {
        items().forEach(function (c) { c.checked = all.checked; });
        refresh();
      });
    }
    form.addEventListener('change', function (e) {
      if (e.target && e.target.hasAttribute && e.target.hasAttribute('data-bulk-item')) refresh();
    });

    var clear = form.querySelector('[data-bulk-clear]');
    if (clear) clear.addEventListener('click', function () {
      items().forEach(function (c) { c.checked = false; });
      if (all) { all.checked = false; all.indeterminate = false; }
      refresh();
    });

    form.querySelectorAll('[data-action]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var action = btn.getAttribute('data-action');
        if (selected().length === 0) return;
        if (CONFIRM[action] && !window.confirm(CONFIRM[action])) return;
        if (actionInput) actionInput.value = action;
        form.submit();
      });
    });

    refresh();
  }

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('form[data-bulk]').forEach(initForm);
  });
})();
