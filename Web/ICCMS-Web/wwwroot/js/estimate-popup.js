// ============================
//  AI ESTIMATE POPUP LOGIC
// ============================

document.addEventListener("DOMContentLoaded", function () {
    console.log("📦 estimate-popup.js loaded successfully.");

    const btnStartAi = document.getElementById("btnStartAi");
    if (!btnStartAi) return;

    const log = (msg) => {
        const logArea = document.getElementById("progressLog");
        if (logArea) logArea.textContent += msg + "\n";
    };

    btnStartAi.addEventListener("click", async () => {
        const blueprintUrl = document.getElementById("blueprintUrl").value.trim();
        const projectId = document.getElementById("projectId").value.trim();
        const contractorId = document.getElementById("contractorId").value.trim();

        if (!blueprintUrl) {
            alert("Please enter a blueprint URL first.");
            return;
        }

        // UI: move to progress view
        document.getElementById("step-upload").classList.add("d-none");
        document.getElementById("step-progress").classList.remove("d-none");

        log("📤 Uploading blueprint...");
        await new Promise((r) => setTimeout(r, 500));

        try {
            log("⚙️ AI scanning and parsing blueprint...");
            const response = await fetch("/ProjectManager/CreateEstimate", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ blueprintUrl, projectId, contractorId })
            });

            if (!response.ok) {
                log("❌ API error: " + response.status);
                alert("Failed to process blueprint.");
                return;
            }

            const estimate = await response.json();
            log("✅ Estimate received. Rendering summary...");

            // Populate summary
            const summary = `
                <strong>Description:</strong> ${estimate.description}<br/>
                <strong>Subtotal:</strong> R ${estimate.subtotal.toFixed(2)}<br/>
                <strong>Tax:</strong> R ${estimate.taxTotal.toFixed(2)}<br/>
                <strong>Total:</strong> R ${estimate.totalAmount.toFixed(2)}<br/>
                <hr/>
                <strong>Line Items:</strong><br/>
                <ul>${estimate.lineItems.map(i => `<li>${i.name} – ${i.quantity} ${i.unit}</li>`).join("")}</ul>
            `;
            document.getElementById("estimateSummary").innerHTML = summary;

            // Switch views
            document.getElementById("step-progress").classList.add("d-none");
            document.getElementById("step-result").classList.remove("d-none");
            // Immediately swap the AI button to "Create Quote" in the current card
            const cardBtn = document.querySelector(`[data-project-id="${projectId}"]`);
            if (cardBtn) {
                cardBtn.outerHTML = `
                    <a href="/Quotes/CreateDraft?projectId=${projectId}" 
                    class="btn btn-sm btn-workflow">
                        <i class="fas fa-file-invoice-dollar me-1"></i> Create Quote
                    </a>`;
            }


        } catch (err) {
            console.error(err);
            log("💥 Unexpected error: " + err.message);
        }
    });

    // Dynamically set project ID when modal opens
const estimateModal = document.getElementById("estimateModal");
estimateModal.addEventListener("show.bs.modal", function (event) {
    const trigger = event.relatedTarget;
    const projectId = trigger?.getAttribute("data-project-id") || "";
    document.getElementById("projectId").value = projectId;
});

// === After "Done" button click ===
const doneBtn = document.querySelector("#step-result .btn-success");
doneBtn?.addEventListener("click", async () => {
    console.log("✅ Done clicked — initiating Create Quote flow...");

    const projectId = document.getElementById("projectId")?.value;
    const contractorId = document.getElementById("contractorId")?.value;
    const clientId = localStorage.getItem("currentClientId") || ""; // optional if you store it

    if (!projectId) {
        alert("Missing Project ID — cannot create quote.");
        return;
    }

    try {
        const res = await fetch("/Quotes/create-from-estimate", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ projectId, clientId })
        });

        const result = await res.json();
        console.log("📦 CreateFromEstimate result:", result);

        if (res.ok && result.quotationId) {
            alert("Quotation created successfully!");
            // 🧭 Redirect straight to the Estimate (prefilled view)
            window.location.href = `/Quotes/Estimate/${result.quotationId}`;
        } else {
            alert(result.error || "Failed to create quotation.");
        }
    } catch (err) {
        console.error("❌ Error creating quote from estimate:", err);
        alert("Unexpected error: " + err.message);
    }
});


});
