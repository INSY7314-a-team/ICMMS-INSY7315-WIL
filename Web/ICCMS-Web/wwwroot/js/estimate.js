// estimate.js

// 🔹 Function to process blueprint using the input + project ID
async function processBlueprint(quotationId) {
    try {
        console.log("=== PROCESS BLUEPRINT STARTED ===");
        console.log("Quotation ID passed into function:", quotationId);

        // --- Grab inputs from DOM ---
        const blueprintInput = document.getElementById("blueprintUrl");
        const projectInput = document.getElementById("projectId");

        console.log("Blueprint input element:", blueprintInput);
        console.log("Blueprint input value (raw):", blueprintInput ? blueprintInput.value : "❌ no element found");
        console.log("Project input element:", projectInput);
        console.log("Project input value (raw):", projectInput ? projectInput.value : "❌ no element found");

        // --- Build request payload ---
        const requestData = {
            blueprintUrl: blueprintInput && blueprintInput.value 
                ? blueprintInput.value.trim() 
                : "❌ EMPTY INPUT",
            projectId: projectInput && projectInput.value 
                ? projectInput.value.trim() 
                : quotationId, // fallback to quotationId if hidden field empty
            contractorId: ""
        };

        console.log("📦 Final requestData payload being sent:", requestData);

        // --- Call our MVC Controller ---
        // ✅ Call MVC controller, not API directly
        const response = await fetch('https://localhost:7136/api/estimates/process-blueprint', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + await getAuthToken()
            },
            body: JSON.stringify(requestData)
        });
        



        console.log("HTTP response status:", response.status);

        // --- Handle errors early ---
        if (!response.ok) {
            const errText = await response.text();
            console.error("❌ Server returned error:", errText);
            throw new Error(errText || "Failed to process blueprint");
        }

        // --- Parse JSON result ---
        const result = await response.json();
        console.log("✅ Server returned JSON result:", result);

        // --- Render items if present (API returns lineItems not items) ---
        const items = result.items || result.lineItems;

        if (items && items.length > 0) {
            console.log("Rendering line items:", items);
            renderLineItems(items, true);

        } else {
            console.warn("⚠️ No line items found in response. Result object:", result);
            alert("No line items were returned from the blueprint.");
        }


    } catch (err) {
        console.error("❌ Exception in processBlueprint:", err);
        alert("Error: " + err.message);
    } finally {
        console.log("=== PROCESS BLUEPRINT FINISHED ===");
    }
}


// === Helper Functions ===

// Renders line items in the table
function renderLineItems(items, fromBlueprint = false) {
    const table = document.getElementById("itemsTable");

    // ❌ Only clear table if NOT from blueprint
    if (!fromBlueprint) {
        table.innerHTML = "";
    }

    // Start at current row count so we don’t overwrite manual rows
    let startIndex = table.querySelectorAll("tr").length;

    items.forEach((item, i) => {
        const rowIndex = startIndex + i;
        const row = `
            <tr>
                <td><input name="Items[${rowIndex}].Name" value="${item.name}" class="form-control" /></td>
                <td><input name="Items[${rowIndex}].Description" value="${item.description || ''}" class="form-control" /></td>
                <td><input name="Items[${rowIndex}].Category" value="${item.category || ''}" class="form-control" /></td>
                <td><input name="Items[${rowIndex}].Unit" value="${item.unit || ''}" class="form-control" /></td>
                <td><input name="Items[${rowIndex}].Quantity" type="number" step="0.01" value="${item.quantity}" class="form-control qty-input" /></td>
                <td><input name="Items[${rowIndex}].UnitPrice" type="number" step="0.01" value="${item.unitPrice}" class="form-control price-input" /></td>
                <td><input name="Items[${rowIndex}].LineTotal" type="number" readonly value="${item.lineTotal}" class="form-control line-total" /></td>
                <td><input name="Items[${rowIndex}].Notes" value="${item.notes || '-'}" class="form-control" /></td>

                <td>
                    ${item.isAiGenerated 
                        ? `<span class="badge bg-info">AI ${(item.aiConfidence*100).toFixed(0)}%</span>` 
                        : "-"}
                    <input type="hidden" name="Items[${rowIndex}].IsAiGenerated" value="${item.isAiGenerated}" />
                </td>
                <td><button type="button" class="btn btn-sm btn-danger" onclick="removeLineItem(this)">❌</button></td>
            </tr>
        `;
        table.insertAdjacentHTML("beforeend", row);
    });

    // Bind recalculation events to all qty/price inputs (new + old)
    document.querySelectorAll(".qty-input, .price-input").forEach(el => {
        el.addEventListener("input", recalcTotals);
    });

    recalcTotals();

    // Show AI controls if at least one AI item exists (blueprint run only)
    if (fromBlueprint) {
        if (items.some(i => i.isAiGenerated)) {
            document.getElementById("aiControls").style.display = "flex";
        }
    }
}


function acceptAiItems() {
    alert("AI items accepted!");
    document.getElementById("aiControls").style.display = "none";
}

function removeAiItems() {
    console.log("❌ Removing AI-generated items...");

    // Grab all rows
    const rows = document.querySelectorAll("#itemsTable tr");

    rows.forEach(row => {
        const hiddenField = row.querySelector("input[name*='IsAiGenerated']");
        if (hiddenField && hiddenField.value === "true") {
            row.remove();
        }
    });

    // Re-index after removals so model binding still works
    const remainingRows = document.querySelectorAll("#itemsTable tr");
    remainingRows.forEach((r, i) => {
        r.querySelectorAll("input").forEach(input => {
            if (input.name.startsWith("Items[")) {
                input.name = input.name.replace(/Items\[\d+\]/, `Items[${i}]`);
            }
        });
    });

    recalcTotals();

    // Hide the AI controls since AI items are gone
    document.getElementById("aiControls").style.display = "none";
}


// === Add a new blank line item ===
function addLineItem() {
    const table = document.getElementById("itemsTable");
    const nextIndex = table.querySelectorAll("tr").length;

    const row = `
        <tr>
            <td><input name="Items[${nextIndex}].Name" value="" class="form-control" /></td>
            <td><input name="Items[${nextIndex}].Description" value="" class="form-control" /></td>
            <td><input name="Items[${nextIndex}].Category" value="" class="form-control" /></td>
            <td><input name="Items[${nextIndex}].Unit" value="" class="form-control" /></td>
            <td><input name="Items[${nextIndex}].Quantity" type="number" step="0.01" value="0" class="form-control qty-input" /></td>
            <td><input name="Items[${nextIndex}].UnitPrice" type="number" step="0.01" value="0" class="form-control price-input" /></td>
            <td><input name="Items[${nextIndex}].LineTotal" type="number" readonly value="0" class="form-control line-total" /></td>
            <td><input name="Items[${nextIndex}].Notes" value="-" class="form-control" /></td>

            <td>
                - 
                <input type="hidden" name="Items[${nextIndex}].IsAiGenerated" value="false" />
            </td>
            <td>
                <button type="button" class="btn btn-sm btn-danger" onclick="removeLineItem(this)">❌</button>
            </td>
        </tr>
    `;

    table.insertAdjacentHTML("beforeend", row);

    table.querySelectorAll(".qty-input, .price-input").forEach(el => {
        el.addEventListener("input", recalcTotals);
    });

    recalcTotals();
}




// === Remove a line item ===
function removeLineItem(button) {
    const row = button.closest("tr");
    row.remove();

    // After removal, re-index inputs so names remain sequential
    const rows = document.querySelectorAll("#itemsTable tr");
    rows.forEach((r, i) => {
        r.querySelectorAll("input").forEach(input => {
            if (input.name.startsWith("Items[")) {
                input.name = input.name.replace(/Items\[\d+\]/, `Items[${i}]`);
            }
        });
    });

    recalcTotals();
}


// Binds recalc events
function bindRecalcEvents() {
    console.log("Binding recalc events to qty/price/tax/markup inputs...");

    document.querySelectorAll(".qty-input, .price-input").forEach(el => {
        el.addEventListener("input", recalcTotals);
    });
    document.getElementById("taxRateInput").addEventListener("input", recalcTotals);
    document.getElementById("markupRateInput").addEventListener("input", recalcTotals);
}


// Recalculates totals
function recalcTotals() {
    console.log("Recalculating totals...");
    let subtotal = 0;

    document.querySelectorAll("#itemsTable tr").forEach((row, i) => {
        const qty = parseFloat(row.querySelector(".qty-input")?.value) || 0;
        const price = parseFloat(row.querySelector(".price-input")?.value) || 0;
        const lineTotal = qty * price;

        console.log(`Row ${i} → qty=${qty}, price=${price}, lineTotal=${lineTotal}`);

        row.querySelector(".line-total").value = lineTotal.toFixed(2);
        subtotal += lineTotal;
    });

    // ✅ Convert user input to decimals
    const taxRateInput = parseFloat(document.getElementById("taxRateInput")?.value) || 0;
    const markupRateInput = parseFloat(document.getElementById("markupRateInput")?.value) || 0;

    const taxRate = taxRateInput / 100;
    const markupRate = markupRateInput / 100;

    const tax = subtotal * taxRate;
    const markup = subtotal * markupRate;
    const grandTotal = subtotal + tax + markup;

    console.log(`Totals → subtotal=${subtotal}, tax=${tax}, markup=${markup}, grandTotal=${grandTotal}`);

    // ✅ Always format in ZAR
    const fmt = { style: "currency", currency: "ZAR" };
    document.getElementById("subtotalDisplay").innerText = subtotal.toLocaleString("en-ZA", fmt);
    document.getElementById("taxDisplay").innerText = tax.toLocaleString("en-ZA", fmt);
    document.getElementById("markupDisplay").innerText = markup.toLocaleString("en-ZA", fmt);
    document.getElementById("grandTotalDisplay").innerText = grandTotal.toLocaleString("en-ZA", fmt);
}



// Get auth token from Firebase
async function getAuthToken() {
    try {
        // Check if Firebase is available and user is signed in
        if (typeof firebase !== 'undefined' && firebase.auth().currentUser) {
            const user = firebase.auth().currentUser;
            console.log('Firebase user found:', user.email);
            const token = await user.getIdToken();
            console.log('Firebase token obtained');
            return token;
        }
        
        console.warn('No Firebase user found, checking localStorage...');
        
        // Fallback to localStorage if available
        const storedToken = localStorage.getItem('authToken');
        if (storedToken) {
            console.log('Using stored token from localStorage');
            return storedToken;
        }
        
        // For testing purposes, return a test token
        console.warn('No valid auth token found, using test token');
        return 'test-token';
    } catch (error) {
        console.error('Error getting auth token:', error);
        return 'test-token';
    }
}

// Ensure events are bound after page load
document.addEventListener("DOMContentLoaded", () => {
    bindRecalcEvents();
    recalcTotals(); // initialize totals once
});


// ========================================
// 🚨 VALIDATION FUNCTION for Estimate form
// ========================================
// ========================================
// 🚨 VALIDATION FUNCTION for Estimate form
// ========================================
function validateEstimateForm() {
    console.log("=== VALIDATING ESTIMATE FORM START ===");

    let isValid = true;
    const itemsTable = document.getElementById("itemsTable");

    // Clear previous errors
    document.querySelectorAll(".validation-error").forEach(el => el.remove());
    document.querySelectorAll(".is-invalid").forEach(el => el.classList.remove("is-invalid"));

    // ---- 1. Validate line items ----
    const rows = itemsTable.querySelectorAll("tr");
    if (rows.length === 0) {
        console.warn("❌ No line items found");
        addInlineError(itemsTable, "At least one line item is required.");
        return false;
    }

    rows.forEach((row, i) => {
        console.log(`Checking row ${i}...`);

        const nameInput = row.querySelector(`input[name*=".Name"]`);
        const descInput = row.querySelector(`input[name*=".Description"]`);
        const catInput = row.querySelector(`input[name*=".Category"]`);
        const unitInput = row.querySelector(`input[name*=".Unit"]`);
        const qtyInput = row.querySelector(`input[name*=".Quantity"]`);
        const priceInput = row.querySelector(`input[name*=".UnitPrice"]`);
        const notesInput = row.querySelector(`input[name*=".Notes"]`);

        // Name
        if (!nameInput.value.trim()) {
            markInvalid(nameInput, "Name is required"); isValid = false;
        }
        // Description
        if (!descInput.value.trim()) {
            markInvalid(descInput, "Description is required"); isValid = false;
        }
        // Category
        if (!catInput.value.trim()) {
            markInvalid(catInput, "Category is required"); isValid = false;
        }
        // Unit
        if (!unitInput.value.trim()) {
            markInvalid(unitInput, "Unit is required"); isValid = false;
        }
        // Quantity
        const qty = parseFloat(qtyInput.value) || 0;
        if (qty <= 0) {
            markInvalid(qtyInput, "Quantity must be greater than 0"); isValid = false;
        }
        // Unit Price
        const price = parseFloat(priceInput.value) || 0;
        if (price <= 0) {
            markInvalid(priceInput, "Unit Price must be greater than 0"); isValid = false;
        }
        // Notes
        if (!notesInput.value.trim()) {
            markInvalid(notesInput, "Notes are required"); isValid = false;
        }
    });

    // ---- 2. Validate Tax / Markup ----
    const taxInput = document.getElementById("taxRateInput");
    const markupInput = document.getElementById("markupRateInput");

    const tax = parseFloat(taxInput.value) || 0;
    const markup = parseFloat(markupInput.value) || 0;

    if (tax < 0 || tax > 100) {
        markInvalid(taxInput, "Tax must be between 0 and 100%"); isValid = false;
    }
    if (markup < 0 || markup > 100) {
        markInvalid(markupInput, "Markup must be between 0 and 100%"); isValid = false;
    }

    console.log("=== VALIDATING ESTIMATE FORM END === isValid =", isValid);
    return isValid;
}

// ========================================
// 🚨 HELPERS for validation
// ========================================

// Adds an inline error message after an element
function addInlineError(targetElement, message) {
    console.log("Adding inline error:", message);

    const errorDiv = document.createElement("div");
    errorDiv.classList.add("validation-error");
    errorDiv.style.color = "red";
    errorDiv.style.fontSize = "0.9em";
    errorDiv.innerText = message;

    targetElement.insertAdjacentElement("afterend", errorDiv);
}

// Marks an input invalid and attaches an inline message
function markInvalid(inputElement, message) {
    inputElement.classList.add("is-invalid");
    addInlineError(inputElement, message);
}


