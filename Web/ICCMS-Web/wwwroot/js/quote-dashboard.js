// ============================================================================
// quote-dashboard.js — Quotation Workflow Logic
// ============================================================================

let currentDraftId = null;
let currentEstimateId = null;
let currentQuotationId = null;

// === Step 0: Create Draft Quote ===
document.getElementById("draftQuoteForm")?.addEventListener("submit", async function (e) {
    e.preventDefault();

    const projectId = document.getElementById("projectSelect").value;
    const clientId = document.getElementById("clientSelect").value;
    const description = document.getElementById("description").value;
    const validUntil = document.getElementById("validUntil").value;

    const requestData = { projectId, clientId, description, validUntil };

    try {
        const res = await fetch("/Quotes/Create", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(requestData)
        });

        const text = await res.text();
        let result;
        try { result = JSON.parse(text); } catch { result = text; }

        if (res.ok) {
            currentDraftId = result.quotationId || result.id || "draft-created";
            document.getElementById("step0Response").innerHTML = `<pre>${JSON.stringify(result, null, 2)}</pre>`;
            document.getElementById("processBlueprintBtn").disabled = false;
        } else {
            document.getElementById("step0Response").innerHTML = `<pre style="color:red">${text}</pre>`;
        }
    } catch (err) {
        document.getElementById("step0Response").innerHTML = `<pre style="color:red">${err.message}</pre>`;
    }
});

// === Step 1: Process Blueprint ===
document.getElementById("processBlueprintBtn")?.addEventListener("click", async function () {
    try {
        const res = await fetch("/api/estimates/process-blueprint", { method: "POST" });
        const result = await res.json();

        if (res.ok) {
            currentEstimateId = result.estimateId;
            document.getElementById("step1Response").innerHTML = `<pre>${JSON.stringify(result, null, 2)}</pre>`;
            document.getElementById("convertQuotationBtn").disabled = false;
        } else {
            document.getElementById("step1Response").innerHTML = `<pre style="color:red">${JSON.stringify(result)}</pre>`;
        }
    } catch (err) {
        document.getElementById("step1Response").innerHTML = `<pre style="color:red">${err.message}</pre>`;
    }
});

// === Step 2: Convert to Quotation ===
document.getElementById("convertQuotationBtn")?.addEventListener("click", async function () {
    try {
        const res = await fetch(`/api/quotations/from-estimate/${currentEstimateId}`, { method: "POST" });
        const text = await res.text();
        let result;
        try { result = JSON.parse(text); } catch { result = text; }

        if (res.ok) {
            currentQuotationId = typeof result === "string" ? result : result.quotationId || result.id;
            document.getElementById("step2Response").innerHTML = `<pre>${JSON.stringify(result, null, 2)}</pre>`;
            document.getElementById("pmReviewBtn").disabled = false;
        } else {
            document.getElementById("step2Response").innerHTML = `<pre style="color:red">${text}</pre>`;
        }
    } catch (err) {
        document.getElementById("step2Response").innerHTML = `<pre style="color:red">${err.message}</pre>`;
    }
});

// === Step 3: PM Review ===
document.getElementById("pmReviewBtn")?.addEventListener("click", async function () {
    try {
        const res = await fetch(`/api/quotations/${currentQuotationId}/pm-approve`, { method: "POST" });
        const text = await res.text();
        let result;
        try { result = JSON.parse(text); } catch { result = text; }

        if (res.ok) {
            document.getElementById("step3Response").innerHTML = `<pre>${JSON.stringify(result, null, 2)}</pre>`;
        } else {
            document.getElementById("step3Response").innerHTML = `<pre style="color:red">${text}</pre>`;
        }
    } catch (err) {
        document.getElementById("step3Response").innerHTML = `<pre style="color:red">${err.message}</pre>`;
    }
});

// === Load Projects & Clients into dropdowns ===
// === Load Projects & Clients into dropdowns ===
document.addEventListener("DOMContentLoaded", async () => {
    try {
        // Projects (with token)
        const projRes = await fetch("/api/projectmanager/projects", {
            headers: { "Authorization": "Bearer " + await getAuthToken() }
        });
        const projectSelect = document.getElementById("projectSelect");
        projectSelect.innerHTML = '<option value="">-- Select Project --</option>';

        if (projRes.ok) {
            const projects = await projRes.json();
            if (projects.length === 0) {
                projectSelect.innerHTML += `<option disabled>(No projects found)</option>`;
            } else {
                projects.forEach(p => {
                    projectSelect.innerHTML += `<option value="${p.projectId}">${p.name}</option>`;
                });
            }
        } else {
            const errText = await projRes.text();
            console.error("Projects fetch failed:", projRes.status, errText);
            projectSelect.innerHTML += `<option disabled>Error loading projects</option>`;
        }

        // Clients (with token too, just like pm-quote.js)
        const clientRes = await fetch("/api/users/clients", {
            headers: { "Authorization": "Bearer " + await getAuthToken() }
        });
        const clientSelect = document.getElementById("clientSelect");
        clientSelect.innerHTML = '<option value="">-- Select Client --</option>';

        if (clientRes.ok) {
            const clients = await clientRes.json();
            clients.forEach(c => {
                clientSelect.innerHTML += `<option value="${c.userId}">${c.fullName} (${c.email})</option>`;
            });
        } else {
            const errText = await clientRes.text();
            console.error("Clients fetch failed:", clientRes.status, errText);
            clientSelect.innerHTML += `<option disabled>Error loading clients</option>`;
        }
    } catch (err) {
        console.error("Dropdown load error:", err);
    }
});

