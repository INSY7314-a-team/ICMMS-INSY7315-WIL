// pm-quote.js — Quote-only flow (Client → Blueprint → Estimate → Preview)

// --- Boot/context ---
window.PM = window.PM || {};
const ROOT  = (window.PM.root  || '/');
const TOKEN = (window.PM.token || '');
const authHeaders = TOKEN ? { 'Authorization': `Bearer ${TOKEN}` } : {};
const money = v => 'R ' + (Number(v)||0).toLocaleString();

// --- State ---
const EST = { items: [] }; // flat items: {name, qty, unit, unitPrice}
let CLIENTS = [];

// --- Data: Clients ---
async function loadClients() {
  try {
    const r = await fetch(ROOT + 'mock/clients.json', { headers: authHeaders, cache: 'no-store' });
    CLIENTS = r.ok ? await r.json() : [];
  } catch { CLIENTS = []; }
  const sel = document.getElementById('clientSelect');
  if (sel) {
    sel.innerHTML = '<option value="">-- Choose client --</option>' +
      CLIENTS.map(c => `<option value="${c.id||''}">${(c.name||'').replace(/</g,'&lt;')}</option>`).join('');
  }
}

// --- Estimate render / totals ---
function renderEstimate() {
  const body = document.getElementById('estimateTableBody');
  if (!body) return;

  body.innerHTML = EST.items.map((it, i) => `
    <tr data-i="${i}">
      <td><input class="form-control form-control-sm est-name" value="${it.name||''}"></td>
      <td style="max-width:120px"><input type="number" min="0" step="0.01" class="form-control form-control-sm est-qty" value="${it.qty||0}"></td>
      <td style="max-width:100px"><input class="form-control form-control-sm est-unit" value="${it.unit||'ea'}"></td>
      <td style="max-width:160px"><input type="number" min="0" step="0.01" class="form-control form-control-sm est-price" value="${it.unitPrice||0}"></td>
      <td class="text-end">
        <span class="est-rowtotal"></span>
        <button class="btn btn-sm btn-outline-secondary ms-2 est-del" title="Remove row"><i class="fas fa-trash"></i></button>
      </td>
    </tr>
  `).join('');

  body.querySelectorAll('input').forEach(inp => {
    inp.addEventListener('input', () => {
      const tr = inp.closest('tr'); const i = Number(tr.dataset.i);
      EST.items[i] = {
        name:      tr.querySelector('.est-name').value || '',
        qty:       Number(tr.querySelector('.est-qty').value || 0),
        unit:      tr.querySelector('.est-unit').value || 'ea',
        unitPrice: Number(tr.querySelector('.est-price').value || 0)
      };
      recalcTotals();
    });
  });

  body.querySelectorAll('.est-del').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      const tr = btn.closest('tr'); const i = Number(tr.dataset.i);
      EST.items.splice(i, 1);
      renderEstimate();
    });
  });

  recalcTotals();
}

function recalcTotals() {
  const body  = document.getElementById('estimateTableBody');
  const subEl = document.getElementById('estimateSubtotal');
  const sumEl = document.getElementById('est-summary');
  const mEl   = document.getElementById('est-markup');
  const tEl   = document.getElementById('est-tax');

  const m = Number(mEl?.value || 0);
  const t = Number(tEl?.value || 0);

  let subtotal = 0;
  EST.items.forEach((it, idx) => {
    const row = (Number(it.qty)||0) * (Number(it.unitPrice)||0);
    subtotal += row;
    const tr = body?.querySelector(`tr[data-i="${idx}"]`);
    if (tr) tr.querySelector('.est-rowtotal').textContent = money(row);
  });

  const markup = subtotal * (m/100);
  const tax    = (subtotal + markup) * (t/100);
  const total  = subtotal + markup + tax;

  if (subEl) subEl.textContent = money(subtotal);
  if (sumEl) sumEl.innerHTML = `Subtotal: <strong>${money(subtotal)}</strong> • Markup (${m}%): <strong>${money(markup)}</strong> • Tax (${t}%): <strong>${money(tax)}</strong> • Total: <strong>${money(total)}</strong>`;
}

['input','change'].forEach(ev => {
  document.getElementById('est-markup')?.addEventListener(ev, recalcTotals);
  document.getElementById('est-tax')?.addEventListener(ev, recalcTotals);
});

// --- Blueprint: mock + simple file hook ---
function applyMockBlueprint() {
  EST.items = [
    { name:'Demo crew',         qty:40,  unit:'hrs', unitPrice:220 },
    { name:'Skips & disposal',  qty:3,   unit:'ea',  unitPrice:1450 },
    { name:'Rebar 10mm',        qty:120, unit:'m',   unitPrice:35 },
    { name:'Electrician',       qty:24,  unit:'hrs', unitPrice:280 },
    { name:'Gypsum boards',     qty:80,  unit:'m²',  unitPrice:95 },
  ];
  renderEstimate();
  goTab('#tab-estimate');
}

function handleBlueprintUpload() {
  // Parser is TBD — reuse mock for now
  applyMockBlueprint();
}

// --- Preview render + submit ---
function renderPreview() {
  const area = document.getElementById('quotePreviewArea'); if (!area) return;
  const clientSel = document.getElementById('clientSelect');
  const clientText = clientSel?.options[clientSel.selectedIndex]?.text || '';
  const title = (document.getElementById('q-title')?.value || `Quote for ${clientText}`).trim();
  const m = Number(document.getElementById('est-markup')?.value || 0);
  const t = Number(document.getElementById('est-tax')?.value || 0);

  let subtotal = 0;
  const rows = EST.items.map(it => {
    const qty = Number(it.qty)||0, up = Number(it.unitPrice)||0, sub = qty*up;
    subtotal += sub;
    return `<tr>
      <td>${(it.name||'').replace(/</g,'&lt;')}</td>
      <td class="text-end">${qty}</td>
      <td>${(it.unit||'').replace(/</g,'&lt;')}</td>
      <td class="text-end">${money(up)}</td>
      <td class="text-end">${money(sub)}</td>
    </tr>`;
  }).join('');

  const markup = subtotal*(m/100);
  const tax    = (subtotal+markup)*(t/100);
  const total  = subtotal+markup+tax;

  area.innerHTML = `
    <div class="mb-2">
      <strong>${(title||'Quote').replace(/</g,'&lt;')}</strong>
      <div class="text-muted small">${(clientText||'').replace(/</g,'&lt;')}</div>
    </div>
    <div class="table-responsive">
      <table class="table table-sm">
        <thead><tr><th>Item</th><th class="text-end">Qty</th><th>Unit</th><th class="text-end">Unit Price</th><th class="text-end">Subtotal</th></tr></thead>
        <tbody>${rows || '<tr><td colspan="5" class="text-muted">No items.</td></tr>'}</tbody>
        <tfoot>
          <tr><th colspan="4" class="text-end">Subtotal</th><th class="text-end">${money(subtotal)}</th></tr>
          <tr><th colspan="4" class="text-end">Markup (${m}%)</th><th class="text-end">${money(markup)}</th></tr>
          <tr><th colspan="4" class="text-end">Tax (${t}%)</th><th class="text-end">${money(tax)}</th></tr>
          <tr><th colspan="4" class="text-end">Total</th><th class="text-end">${money(total)}</th></tr>
        </tfoot>
      </table>
    </div>`;
}

function submitQuote() {
  const pform = document.getElementById('quote-preview-form'); if (!pform) return;

  // keep antiforgery token
  const original = pform.innerHTML;
  pform.innerHTML = original;

  const add = (n,v) => { const i=document.createElement('input'); i.type='hidden'; i.name=n; i.value=(v??''); pform.appendChild(i); };

  const clientSel  = document.getElementById('clientSelect');
  const clientId   = clientSel?.value || '';
  const clientName = clientSel?.options[clientSel.selectedIndex]?.text || '';
  const title      = (document.getElementById('q-title')?.value || `Quote for ${clientName}`).trim();
  const m = document.getElementById('est-markup')?.value || '10';
  const t = document.getElementById('est-tax')?.value || '15';

  add('Title', title);
  add('ClientId', clientId);
  add('ClientName', clientName);
  add('MarkupPercent', m);
  add('TaxPercent', t);

  EST.items.forEach((it, idx) => {
    add(`Items[${idx}].Type`, (it.name||'').toLowerCase().includes('labour') ? 'Labour' : 'Material');
    add(`Items[${idx}].Name`, it.name||'');
    add(`Items[${idx}].Qty`, String(it.qty||0));
    add(`Items[${idx}].Unit`, it.unit||'ea');
    add(`Items[${idx}].UnitPrice`, String(it.unitPrice||0));
  });

  console.log("[Submit] fields:");
[...pform.querySelectorAll('input')].forEach(inp => {
  console.log("   ", inp.name, "=", inp.value);
});

pform.submit();

}

// --- Tabs / modal helpers ---
function goTab(id) {
  const btn = document.querySelector(`#quoteTabs button[data-bs-target="${id}"]`);
  if (btn) new bootstrap.Tab(btn).show();
}

// --- Prefill the wizard from an existing quote ---
// --- Prefill the wizard from an existing quote ---
window.hydrateQuote = async function(q) {
  console.log("Hydrate called with:", q);

  // open modal first
  const modalEl = document.getElementById('quoteModal');
  if (modalEl) new bootstrap.Modal(modalEl).show();

  // ✅ ensure clients are loaded before trying to select
  await loadClients();

  // client (match by ClientName best effort)
  const sel = document.getElementById('clientSelect');
  if (sel && (q.ClientName || q.clientName)) {
    let matched = false;
    [...sel.options].forEach(opt => {
      if (!matched && (opt.text || '').trim().toLowerCase() === String(q.ClientName || q.clientName || '').trim().toLowerCase()) {
        opt.selected = true;
        matched = true;
      }
    });
  }

  // title / percentages
  const titleEl = document.getElementById('q-title');
  if (titleEl) titleEl.value = q.Title || q.title || '';

  const mEl = document.getElementById('est-markup');
  if (mEl && (q.MarkupPercent ?? q.markupPercent) != null) {
    mEl.value = q.MarkupPercent ?? q.markupPercent;
  }

  const tEl = document.getElementById('est-tax');
  if (tEl && (q.TaxPercent ?? q.taxPercent) != null) {
    tEl.value = q.TaxPercent ?? q.taxPercent;
  }

  // items
  if (Array.isArray(q.Items ?? q.items)) {
    const arr = q.Items ?? q.items;
    EST.items = arr.map(it => ({
      name: it.Name || it.name || '',
      qty: Number(it.Qty ?? it.qty ?? 0),
      unit: it.Unit || it.unit || 'ea',
      unitPrice: Number(it.UnitPrice ?? it.unitPrice ?? 0)
    }));
  } else {
    EST.items = [];
  }
  renderEstimate();

  // hidden field: OriginalQuoteId (for controller to know we reopened)
  const form = document.getElementById('quote-preview-form');
  if (form) {
    let hidden = form.querySelector('input[name="OriginalQuoteId"]');
    if (!hidden) {
      hidden = document.createElement('input');
      hidden.type = 'hidden';
      hidden.name = 'OriginalQuoteId';
      form.appendChild(hidden);
    }
    hidden.value = q.Id || q.id || '';
  }

  // jump to Estimate tab so PM can tweak then Preview
  const tabBtn = document.querySelector('#quoteTabs button[data-bs-target="#tab-estimate"]');
  if (tabBtn) new bootstrap.Tab(tabBtn).show();
};




// --- Wire UI ---
document.addEventListener('DOMContentLoaded', () => {
  const btnOpen = document.getElementById('btn-new-quote');
  const modalEl = document.getElementById('quoteModal');

  if (btnOpen && modalEl) {
    btnOpen.addEventListener('click', async () => {
      await loadClients();
      goTab('#tab-client');
      new bootstrap.Modal(modalEl).show();
    });
  }

  // Blueprint mock + file
  document.getElementById('btn-mock-blueprint')?.addEventListener('click', (e) => { e.preventDefault(); applyMockBlueprint(); });
  document.getElementById('blueprintUpload')?.addEventListener('change', handleBlueprintUpload);

  // Estimate add-row buttons
  document.getElementById('btn-add-labour')?.addEventListener('click', (e) => {
    e.preventDefault(); EST.items.push({ name:'Labour', qty:1, unit:'hrs', unitPrice:0 }); renderEstimate();
  });
  document.getElementById('btn-add-material')?.addEventListener('click', (e) => {
    e.preventDefault(); EST.items.push({ name:'Material', qty:1, unit:'ea', unitPrice:0 }); renderEstimate();
  });

  // Preview render on tab show
  document.addEventListener('shown.bs.tab', (e) => {
    if (e.target?.dataset?.bsTarget === '#tab-preview') renderPreview();
  });

  // Submit
  document.getElementById('btn-finalize-quote')?.addEventListener('click', (e) => {
  e.preventDefault();
  submitQuote(); // posts form, redirects to full Preview page
});

});
