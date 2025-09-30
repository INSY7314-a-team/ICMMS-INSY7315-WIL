// ============================================================================
// pm-quote.js — Full Quote Wizard logic
// Handles: load clients, manage estimate items, calculate totals,
// render preview, submit quotation to API
// ============================================================================

// --- Globals ---
let CLIENTS = [];          // Cached clients list
let EST = { items: [] };   // Current estimate state
const ROOT = window.PM?.root || "/"; // Root path injected from Razor
const TOKEN = window.PM?.token || "";
const AUTH_HEADERS = { "Authorization": `Bearer ${TOKEN}` };

// --- Debug logger with personality ---
function log(msg, obj) {
    console.log(`[QuoteWizard] ${msg}`, obj ?? "");
}

// ============================================================================
// 1. Load Clients into dropdown
// ============================================================================
async function loadClients() {
    log("🔥 Entered loadClients()");
    try {
        const res = await fetch(`${ROOT}api/users/clients`, { headers: AUTH_HEADERS });
        log("👉 API responded with status", res.status);

        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        CLIENTS = await res.json();
        log("✅ Clients fetched", CLIENTS);

        const sel = document.getElementById('clientSelect');
        if (sel) {
            sel.innerHTML = '<option value="">-- Choose client --</option>' +
                CLIENTS.map(c => `<option value="${c.userId}">${c.fullName}</option>`).join('');
            log("🎯 Client dropdown populated. Options count:", sel.options.length);
        } else {
            log("⚠️ clientSelect element not found in DOM!");
        }
    } catch (err) {
        log("💥 FAILED to load clients:", err);
        CLIENTS = [];
    }
}

// ============================================================================
// 2. Add Estimate Items
// ============================================================================
function addItem(type) {
    log(`🔥 Adding new item of type ${type}`);
    EST.items.push({
        name: type === "labour" ? "Labour" : "Material",
        qty: 1,
        unit: type === "labour" ? "hr" : "ea",
        unitPrice: 0,
        taxRate: 0.15
    });
    renderEstimate();
}

// ============================================================================
// 3. Render Estimate UI
// ============================================================================
function renderEstimate() {
    log("🔥 Entered renderEstimate()", EST.items);

    const body = document.getElementById('estimateTableBody');
    if (!body) {
        log("⚠️ No #estimateTableBody found in DOM, cannot render.");
        return;
    }

    body.innerHTML = EST.items.map((it, i) => `
        <tr data-i="${i}">
            <td><input class="form-control est-name" value="${it.name || ''}"></td>
            <td><input type="number" min="0" step="0.1" class="form-control est-qty" value="${it.qty || 0}"></td>
            <td><input class="form-control est-unit" value="${it.unit || 'ea'}"></td>
            <td><input type="number" min="0" step="0.01" class="form-control est-price" value="${it.unitPrice || 0}"></td>
            <td class="text-end">
                <span class="est-rowtotal"></span>
                <button class="btn btn-sm btn-outline-danger est-del"><i class="fas fa-trash"></i></button>
            </td>
        </tr>
    `).join('');

    // Bind events for delete + editing
    body.querySelectorAll('.est-del').forEach((btn, idx) => {
        btn.addEventListener('click', () => {
            log("🗑 Removing item", idx);
            EST.items.splice(idx, 1);
            renderEstimate();
        });
    });

    body.querySelectorAll('.est-name').forEach((el, idx) => {
        el.addEventListener('input', e => EST.items[idx].name = e.target.value);
    });
    body.querySelectorAll('.est-qty').forEach((el, idx) => {
        el.addEventListener('input', e => EST.items[idx].qty = parseFloat(e.target.value) || 0);
    });
    body.querySelectorAll('.est-unit').forEach((el, idx) => {
        el.addEventListener('input', e => EST.items[idx].unit = e.target.value);
    });
    body.querySelectorAll('.est-price').forEach((el, idx) => {
        el.addEventListener('input', e => EST.items[idx].unitPrice = parseFloat(e.target.value) || 0);
    });

    log("🎯 Table rows rendered:", EST.items.length);
    recalcTotals();
}

// ============================================================================
// 4. Totals
// ============================================================================
function recalcTotals() {
    log("🔥 Entered recalcTotals()");
    const m = Number(document.getElementById('est-markup')?.value || 0);
    const t = Number(document.getElementById('est-tax')?.value || 0);

    log(`👉 Using Markup=${m}% and Tax=${t}%`);

    let subtotal = 0;
    EST.items.forEach((it, idx) => {
        const rowTotal = (it.qty || 0) * (it.unitPrice || 0);
        subtotal += rowTotal;
        const tr = document.querySelector(`tr[data-i="${idx}"]`);
        if (tr) tr.querySelector('.est-rowtotal').textContent = MONEY(rowTotal);
    });

    const markup = subtotal * (m / 100);
    const tax = (subtotal + markup) * (t / 100);
    const grand = subtotal + markup + tax;

    EST.subtotal = subtotal;
    EST.taxTotal = tax;
    EST.grandTotal = grand;

    document.getElementById('estimateSubtotal').textContent = `Subtotal: R ${subtotal.toFixed(2)}`;
    document.getElementById('est-summary').textContent =
        `Subtotal: R${subtotal.toFixed(2)} | Markup: R${markup.toFixed(2)} | Tax: R${tax.toFixed(2)} | Grand Total: R${grand.toFixed(2)}`;

    log("✅ Totals recalculated:", { subtotal, markup, tax, grand });
}

// Helper: money formatting
function MONEY(num) {
    return "R " + (num || 0).toFixed(2);
}

// ============================================================================
// 5. Render Preview Tab
// ============================================================================
function renderPreview() {
    log("🔥 Entered renderPreview()");
    const area = document.getElementById('quotePreviewArea');
    if (!area) {
        log("⚠️ No preview area found!");
        return;
    }

    const clientSel = document.getElementById('clientSelect');
    const clientName = CLIENTS.find(c => c.userId === clientSel?.value)?.fullName || "(no client)";
    const title = document.getElementById('q-title')?.value || "(Untitled)";

    area.innerHTML = `
        <h5>${title}</h5>
        <p><b>Client:</b> ${clientName}</p>
        <p><b>Items:</b> ${EST.items.length}</p>
        <p><b>Grand Total:</b> ${MONEY(EST.grandTotal || 0)}</p>
        <p class="text-muted">Generated ${new Date().toLocaleString()}</p>
    `;

    log("✅ Preview rendered successfully.");
}

// ============================================================================
// 6. Submit Quote -> API
// ============================================================================
async function submitQuote() {
    log("🔥 Entered submitQuote()");
    try {
        const clientSel = document.getElementById('clientSelect');
        const clientId = clientSel?.value;
        if (!clientId) throw new Error("Client not selected!");

        const title = document.getElementById('q-title')?.value || "Untitled Quote";

        const body = {
            quotationId: "", // API will assign
            projectId: "",   // optional link later
            clientId,
            description: title,
            items: EST.items.map(it => ({
                name: it.name,
                quantity: it.qty,
                unitPrice: it.unitPrice,
                taxRate: it.taxRate,
                lineTotal: (it.qty || 0) * (it.unitPrice || 0)
            })),
            subtotal: EST.subtotal || 0,
            taxTotal: EST.taxTotal || 0,
            grandTotal: EST.grandTotal || 0,
            status: "Draft",
            validUntil: new Date(Date.now() + 7*24*60*60*1000).toISOString(), // +7 days
            createdAt: new Date().toISOString()
        };

        log("👉 Submitting body to API:", body);

        const res = await fetch(`${ROOT}api/quotations`, {
            method: "POST",
            headers: { ...AUTH_HEADERS, "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });

        log("👉 API responded with status", res.status);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);

        const quotationId = await res.json();
        log("✅ Quote created with ID", quotationId);

        alert(`Quote created successfully (ID: ${quotationId})`);
    } catch (err) {
        log("💥 FAILED in submitQuote:", err);
        alert("Error creating quote: " + err.message);
    }
}

// ============================================================================
// 7. Event Binding on Modal Open
// ============================================================================
document.addEventListener("DOMContentLoaded", () => {
    log("🚀 pm-quote.js bootstrapped");

    // Load clients immediately
    loadClients();

    // Buttons to add items
    document.getElementById("btn-add-labour")?.addEventListener("click", () => addItem("labour"));
    document.getElementById("btn-add-material")?.addEventListener("click", () => addItem("material"));

    // Totals recalculation on inputs
    document.getElementById("est-markup")?.addEventListener("input", recalcTotals);
    document.getElementById("est-tax")?.addEventListener("input", recalcTotals);

    // Render preview
    document.getElementById("btn-finalize-quote")?.addEventListener("click", renderPreview);

    // Submit
    document.getElementById("btn-submit-quote")?.addEventListener("click", submitQuote);
});
