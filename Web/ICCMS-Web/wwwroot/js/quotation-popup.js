// ===============================
//  QUOTATION POPUP LOGIC
// ===============================
document.addEventListener("DOMContentLoaded", () => {
  console.log("🧾 quotation-popup.js loaded and initializing...");

  // === Grab the modal element ===
  const quotationModal = document.getElementById("quotationModal");
  const addLineBtn = document.getElementById("btnAddLine");

  // Ensure Add Line Item button works even if modal reloads
  if (addLineBtn) {
    addLineBtn.addEventListener("click", () => {
      console.log("➕ Add Line Item button clicked.");
      const currentItems = collectItems();
      currentItems.push({
        name: "",
        description: "",
        category: "",
        unit: "",
        quantity: 0,
        unitPrice: 0
      });
      renderLineItems(currentItems);
    });
  }

  // === When modal is triggered ===
  quotationModal.addEventListener("show.bs.modal", async event => {
    const trigger = event.relatedTarget;
    const projectId = trigger?.getAttribute("data-project-id") || "";
    const projectName = trigger?.getAttribute("data-project-name") || "";
    const clientId = trigger?.getAttribute("data-client-id") || "";
    const description = trigger?.getAttribute("data-description") || "";

    console.log(`📦 Quotation popup opened for project: ${projectName} (${projectId})`);

    // Inject hidden field data for backend submission
    document.getElementById("projectId").value = projectId;
    document.getElementById("clientId").value = clientId;

    // Display visible header info in the modal
    document.getElementById("popupProjectName").innerText = projectName || "Unknown Project";
    document.getElementById("popupClientName").innerText = clientId
      ? `Client ID: ${clientId}`
      : "Client not linked";
    document.getElementById("popupDescription").innerText = description || "No description provided";

    // If no project, skip
    if (!projectId) {
      console.warn("⚠️ No projectId found — cannot preload estimate items.");
      return;
    }

    // Try fetching existing estimate line items for that project
    try {
      console.log(`📡 Fetching estimate items for projectId=${projectId}`);
      const response = await fetch(`/Quotes/GetEstimateItems?projectId=${projectId}`);

      if (!response.ok) {
        console.error(`❌ Server returned ${response.status} (${response.statusText})`);
        renderLineItems([]); // fallback empty
        return;
      }

      const items = await response.json();
      if (items?.length > 0) {
        console.log(`✅ Preloading ${items.length} estimate items...`);
        renderLineItems(items);
      } else {
        console.warn("⚠️ No items returned from estimate fetch.");
        renderLineItems([]);
      }
    } catch (err) {
      console.error("🔥 Error loading estimate items:", err);
      renderLineItems([]);
    }
  });
});

// ===============================
//  RENDER LINE ITEMS TABLE
// ===============================
function renderLineItems(items) {
  console.log(`🧮 Rendering ${items.length} line items...`);
  const table = document.getElementById("quotationItemsTable");
  table.innerHTML = "";

  // Loop through each line item and build row HTML
  items.forEach((item, i) => {
    const row = `
      <tr>
        <td><input class="form-control" value="${item.name || ""}" data-field="name"></td>
        <td><input class="form-control" value="${item.description || ""}" data-field="description"></td>
        <td><input class="form-control" value="${item.category || ""}" data-field="category"></td>
        <td><input class="form-control" value="${item.unit || ""}" data-field="unit"></td>
        <td><input type="number" step="0.01" value="${item.quantity || 0}" class="form-control qty-input"></td>
        <td><input type="number" step="0.01" value="${item.unitPrice || 0}" class="form-control price-input"></td>
        <td><input type="number" readonly class="form-control line-total" value="${item.lineTotal?.toFixed(2) || 0}"></td>
        <td>
          <button class="btn btn-sm btn-danger" onclick="this.closest('tr').remove(); recalcTotals();">
            ❌
          </button>
        </td>
      </tr>`;
    table.insertAdjacentHTML("beforeend", row);
  });

  // Rebind recalculation events and trigger initial total
  bindRecalc();
  recalcTotals();
}

// ===============================
//  COLLECT LINE ITEMS
// ===============================
function collectItems() {
  const rows = document.querySelectorAll("#quotationItemsTable tr");
  const collected = Array.from(rows).map(row => ({
    name: row.querySelector('[data-field="name"]').value,
    description: row.querySelector('[data-field="description"]').value,
    category: row.querySelector('[data-field="category"]').value,
    unit: row.querySelector('[data-field="unit"]').value,
    quantity: parseFloat(row.querySelector(".qty-input").value) || 0,
    unitPrice: parseFloat(row.querySelector(".price-input").value) || 0,
    lineTotal: parseFloat(row.querySelector(".line-total").value) || 0
  }));

  console.log(`📋 Collected ${collected.length} items from table.`);
  return collected;
}

// ===============================
//  BIND EVENTS FOR TOTALS
// ===============================
function bindRecalc() {
  const inputs = document.querySelectorAll(".qty-input, .price-input, #markupRateInput, #taxRateInput");
  inputs.forEach(el => el.addEventListener("input", recalcTotals));
}

// ===============================
//  RECALCULATE TOTALS
// ===============================
function recalcTotals() {
  console.log("🔁 Recalculating totals...");

  // Collect all rows
  const rows = document.querySelectorAll("#quotationItemsTable tr");
  let subtotal = 0;

  // Fetch global rates
  const markupRate = (parseFloat(document.getElementById("markupRateInput").value) || 0) / 100;
  const taxRate = (parseFloat(document.getElementById("taxRateInput").value) || 0) / 100;

  // --- Calculate subtotal ---
  rows.forEach(row => {
    const qty = parseFloat(row.querySelector(".qty-input").value) || 0;
    const price = parseFloat(row.querySelector(".price-input").value) || 0;
    const lineTotal = qty * price;
    row.querySelector(".line-total").value = lineTotal.toFixed(2);
    subtotal += lineTotal;
  });

  // --- Apply markup ---
  const subtotalWithMarkup = subtotal * (1 + markupRate);

  // --- Apply tax ---
  const taxTotal = subtotalWithMarkup * taxRate;

  // --- Final grand total ---
  const grandTotal = subtotalWithMarkup + taxTotal;

  // --- Update UI displays ---
  document.getElementById("subtotalDisplay").innerText =
    subtotalWithMarkup.toLocaleString("en-ZA", { style: "currency", currency: "ZAR" });
  document.getElementById("taxDisplay").innerText =
    taxTotal.toLocaleString("en-ZA", { style: "currency", currency: "ZAR" });
  document.getElementById("grandTotalDisplay").innerText =
    grandTotal.toLocaleString("en-ZA", { style: "currency", currency: "ZAR" });

  console.log(
    `💰 subtotal=${subtotal} | subtotalWithMarkup=${subtotalWithMarkup} | markup=${markupRate} | tax=${taxRate} | taxTotal=${taxTotal} | grandTotal=${grandTotal}`
  );
}


// ===============================
//  SUBMIT QUOTATION
// ===============================
async function submitQuotation() {
  const quotation = {
    quotationId: document.getElementById("quotationId").value,
    projectId: document.getElementById("projectId").value,
    clientId: document.getElementById("clientId").value,
    contractorId: document.getElementById("contractorId").value,
    markupRate: parseFloat(document.getElementById("markupRateInput").value) || 0,
    taxRate: parseFloat(document.getElementById("taxRateInput").value) || 0,
    items: collectItems()
  };

  console.log("🚀 Submitting quotation:", quotation);

  try {
    const res = await fetch("/Quotes/SubmitQuotation", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(quotation)
    });

    const result = await res.json();
    if (res.ok) {
      console.log("✅ Quotation submitted successfully!");
      alert("✅ Quotation submitted successfully!");
      window.location.reload();
    } else {
      console.error("❌ Submission failed:", result.error);
      alert(result.error || "Failed to submit quotation.");
    }
  } catch (err) {
    console.error("🔥 Error submitting quotation:", err);
    alert("Unexpected error: " + err.message);
  }
}
