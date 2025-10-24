// =========================================
//  QUOTE BUILDER LOGIC (new modal version)
// =========================================
document.addEventListener("DOMContentLoaded", () => {
  console.log("🧱 quote-builder.js loaded");

  const modal = document.getElementById("quoteBuilderModal");
  const addItemBtn = document.getElementById("qb-btnAddItem");
  const submitBtn = document.getElementById("qb-btnSubmit");
  const aiBtn = document.getElementById("qb-btnRunAI");

  // --- Add Item ---
  if (addItemBtn) {
    addItemBtn.addEventListener("click", () => {
      console.log("➕ Add Line Item");
      const current = collectItems();
      current.push({
        name: "",
        description: "",
        category: "",
        unit: "",
        quantity: 0,
        unitPrice: 0,
      });
      renderItems(current);
    });
  }

  // --- Run AI Parser ---
  if (aiBtn) {
    aiBtn.addEventListener("click", async () => {
      console.log("🧠 Running AI Parser...");
      const projectId = document.getElementById("qb-projectId").value;
      const blueprintUrl = document.getElementById("qb-blueprintUrl").value;

      if (!projectId || !blueprintUrl)
        return alert("Please provide a valid blueprint URL.");

      document.getElementById("qb-aiProgress").classList.remove("d-none");

      try {
        const res = await fetch("/ProjectManager/CreateEstimate", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ projectId, blueprintUrl }),
        });

        const data = await res.json();
        document.getElementById("qb-aiProgress").classList.add("d-none");

        if (!res.ok) throw new Error(data.error || "AI processing failed");

        console.log("✅ AI Estimate complete:", data);

        // ✅ Store the estimateId globally for later use during quote submission
        if (data.estimateId) {
          window.latestEstimateId = data.estimateId;
          console.log(
            "📎 Saved estimateId for quote submission:",
            window.latestEstimateId
          );
        } else {
          console.warn(
            "⚠️ No estimateId returned from CreateEstimate endpoint!"
          );
        }

        document.getElementById("qb-aiResult").classList.remove("d-none");
        renderItems(data.lineItems || []);
      } catch (err) {
        console.error("🔥 AI Parse Error:", err);
        alert(err.message);
      }
    });
  }

  // --- Submit Quote ---
  if (submitBtn) {
    submitBtn.addEventListener("click", async () => {
      console.log("🚀 Submitting quotation...");

      // Ensure we have an estimateId saved globally after AI parser
      const estimateId = window.latestEstimateId || null;
      if (!estimateId) {
        console.warn(
          "⚠️ No estimateId found. Make sure CreateEstimate completed before submitting quote."
        );
      }

      const quote = {
        projectId: document.getElementById("qb-projectId").value,
        clientId: document.getElementById("qb-clientId").value,
        contractorId: document.getElementById("qb-contractorId").value,
        estimateId: estimateId,
        markupRate:
          (parseFloat(document.getElementById("qb-markupRate").value) || 0) /
          100,
        taxRate: parseFloat(document.getElementById("qb-taxRate").value) || 0,
        items: collectItems(),
        isAiGenerated: true,
        description: "AI-generated estimate from blueprint",
      };

      console.log("🧾 Payload being submitted:", quote);

      try {
        const res = await fetch("/Quotes/submit-quotation", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(quote),
        });

        let data;
        try {
          data = await res.json();
        } catch {
          console.warn("⚠️ Empty or non-JSON response, treating as success.");
          data = {};
        }

        if (!res.ok)
          throw new Error(data.error || "Failed to submit quotation.");

        showToast("✅ Quotation submitted successfully!");
        console.log("✅ Quotation submitted:", data);

        // Auto refresh to update dashboard
        setTimeout(() => window.location.reload(), 1200);
      } catch (err) {
        console.error("🔥 Submission error:", err);
        alert(err.message);
      }
    });
  }

  // Bind static controls once to prevent multiple event listeners
  bindStaticControls();
});

// ===================================================
//  Helpers
// ===================================================
// ================================================
//  RENDER, COLLECT, RECALCULATE (Fixed Version)
// ================================================
function renderItems(items) {
  const tableBody = document.querySelector("#qb-itemsTable tbody");
  if (!tableBody) {
    console.warn("⚠️ Table body not found (#qb-itemsTable tbody)");
    return;
  }

  tableBody.innerHTML = "";
  items.forEach((it) => {
    const row = `
      <tr>
        <td><input class="form-control" value="${
          it.name || ""
        }" data-field="name"></td>
        <td><input class="form-control" value="${
          it.description || ""
        }" data-field="description"></td>
        <td><input class="form-control" value="${
          it.category || ""
        }" data-field="category"></td>
        <td><input class="form-control" value="${
          it.unit || ""
        }" data-field="unit"></td>
        <td><input type="number" value="${
          it.quantity || 0
        }" class="form-control qty-input"></td>
        <td><input type="number" value="${
          it.unitPrice || 0
        }" class="form-control price-input"></td>
        <td><input type="number" readonly class="form-control line-total" value="${
          it.lineTotal?.toFixed(2) || 0
        }"></td>
        <td><button class="btn btn-sm btn-danger btn-remove">❌</button></td>
      </tr>`;
    tableBody.insertAdjacentHTML("beforeend", row);
  });

  bindRecalc();
  recalcTotals();
}

/// ✅ Fixed Version
function collectItems() {
  const rows = document.querySelectorAll("#qb-itemsTable tbody tr");
  const taxRate =
    (parseFloat(document.getElementById("qb-taxRate").value) || 0) / 100;

  return Array.from(rows).map((r) => ({
    name: r.querySelector('[data-field="name"]').value,
    description: r.querySelector('[data-field="description"]').value,
    category: r.querySelector('[data-field="category"]').value,
    unit: r.querySelector('[data-field="unit"]').value,
    quantity: parseFloat(r.querySelector(".qty-input").value) || 0,
    unitPrice: parseFloat(r.querySelector(".price-input").value) || 0,
    lineTotal: parseFloat(r.querySelector(".line-total").value) || 0,
    taxRate: taxRate, // ✅ add normalized tax rate for each item
  }));
}

// Add listeners for recalculation and deletion
function bindRecalc() {
  // Only bind row-level inputs to prevent accumulation on re-renders
  document
    .querySelectorAll(".qty-input, .price-input")
    .forEach((el) => el.addEventListener("input", recalcTotals));

  document.querySelectorAll(".btn-remove").forEach((btn) =>
    btn.addEventListener("click", (e) => {
      e.preventDefault();
      btn.closest("tr").remove();
      recalcTotals();
    })
  );
}

// Bind static controls once to prevent multiple event listeners
function bindStaticControls() {
  const markupRateEl = document.getElementById("qb-markupRate");
  const taxRateEl = document.getElementById("qb-taxRate");

  // Guard against multiple bindings using a custom property
  if (markupRateEl && !markupRateEl._recalcBound) {
    markupRateEl.addEventListener("input", recalcTotals);
    markupRateEl._recalcBound = true;
  }

  if (taxRateEl && !taxRateEl._recalcBound) {
    taxRateEl.addEventListener("input", recalcTotals);
    taxRateEl._recalcBound = true;
  }
}

// Recalculate subtotal, tax, grand total
function recalcTotals() {
  const rows = document.querySelectorAll("#qb-itemsTable tbody tr");
  let subtotal = 0;
  rows.forEach((r) => {
    const qty = parseFloat(r.querySelector(".qty-input").value) || 0;
    const price = parseFloat(r.querySelector(".price-input").value) || 0;
    const total = qty * price;
    r.querySelector(".line-total").value = total.toFixed(2);
    subtotal += total;
  });

  const markupRate =
    (parseFloat(document.getElementById("qb-markupRate").value) || 0) / 100;
  const taxRate =
    (parseFloat(document.getElementById("qb-taxRate").value) || 0) / 100;

  const subtotalWithMarkup = subtotal * (1 + markupRate);
  const tax = subtotalWithMarkup * taxRate;
  const grand = subtotalWithMarkup + tax;

  // Update UI
  document.getElementById("qb-subtotal").innerText =
    subtotalWithMarkup.toLocaleString("en-ZA", {
      style: "currency",
      currency: "ZAR",
    });
  document.getElementById("qb-tax").innerText = tax.toLocaleString("en-ZA", {
    style: "currency",
    currency: "ZAR",
  });
  document.getElementById("qb-grandTotal").innerText = grand.toLocaleString(
    "en-ZA",
    { style: "currency", currency: "ZAR" }
  );

  // Store totals for reference
  window._qbTotals = { subtotal, markupRate, taxRate, tax, grand };
}

// --- Patch navigation to Preview Tab ---
document.addEventListener("click", (e) => {
  if (
    e.target.matches(
      '[data-bs-target="#tab-preview"], button[onclick*="#tab-preview"]'
    )
  ) {
    recalcTotals();
    console.log("📊 Preview totals updated:", window._qbTotals);
  }
});

// =====================================
//  Render Review Section Line Items
// =====================================
function renderReviewItems() {
  const reviewBody = document.querySelector("#qb-reviewItems tbody");
  if (!reviewBody) return;

  const items = collectItems();
  reviewBody.innerHTML = "";

  items.forEach((it) => {
    const total = (it.quantity * it.unitPrice).toFixed(2);
    const row = `
      <tr>
        <td>${it.name || "-"}</td>
        <td>${it.category || "-"}</td>
        <td>${it.quantity || 0}</td>
        <td>R ${it.unitPrice.toFixed(2)}</td>
        <td><strong>R ${total}</strong></td>
      </tr>`;
    reviewBody.insertAdjacentHTML("beforeend", row);
  });
}

// =====================================
//  Update Review Items on Tab Switch
// =====================================
document.addEventListener("click", (e) => {
  if (
    e.target.matches(
      '[data-bs-target="#tab-preview"], button[onclick*="#tab-preview"]'
    )
  ) {
    recalcTotals();
    renderReviewItems();
    console.log("📋 Review section refreshed with current line items.");
  }
});

// Toast functionality removed - using console logging instead
function showToast(msg) {
  console.log(`📢 INFO: ${msg}`);
}

async function simulateClientApproval(quoteId) {
  if (!quoteId) return alert("No quote ID found.");
  if (!confirm("Simulate client approval for this quote?")) return;

  try {
    const res = await fetch(`/Quotes/simulate-client-approval/${quoteId}`, {
      method: "POST",
    });
    if (!res.ok) throw new Error(`API error ${res.status}`);
    showToast("✅ Simulated client approval successfully!");
    setTimeout(() => window.location.reload(), 1000);
  } catch (err) {
    console.error("🔥 Simulate approval failed:", err);
    showToast("❌ Failed to simulate approval.");
  }
}
