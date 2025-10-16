// ============================================================================
// quote-index.js — Client/Project lookup + filters for Quotations Index
// ============================================================================


function applyFilters() {
    const search = document.getElementById("searchBox").value.toLowerCase();
    const status = document.getElementById("statusFilter").value;
    const date = document.getElementById("dateFilter").value;

    document.querySelectorAll(".quote-item").forEach(item => {
        const matchesSearch = item.dataset.client.includes(search) 
                            || item.dataset.project.includes(search) 
                            || item.dataset.description.includes(search);
        const matchesStatus = !status || item.dataset.status === status;
        const matchesDate = !date || item.dataset.date === date;

        if (matchesSearch && matchesStatus && matchesDate) {
            item.style.display = "block";
        } else {
            item.style.display = "none";
        }
    });
}
// 🚀 Bootstraps once DOM is loaded
document.addEventListener("DOMContentLoaded", async () => {
    console.log("🚀 quote-index.js loaded and running...");

    // === STEP 1: Fetch data from API endpoints ===
    let clients = [];
    let projects = [];

    try {
        console.log("🔄 Fetching clients from /api/users/clients...");
        const clientRes = await fetch("/api/users/clients");
        console.log("👉 Client fetch status:", clientRes.status);

        if (clientRes.ok) {
            clients = await clientRes.json();
            console.log("✅ Clients loaded successfully:", clients);
        } else {
            const errText = await clientRes.text();
            console.error("❌ Failed to load clients:", errText);
        }
    } catch (err) {
        console.error("💥 Exception while fetching clients:", err);
    }

    try {
        console.log("🔄 Fetching projects from /api/projectmanager/projects...");
        const projectRes = await fetch("/api/projectmanager/projects");
        console.log("👉 Project fetch status:", projectRes.status);

        if (projectRes.ok) {
            projects = await projectRes.json();
            console.log("✅ Projects loaded successfully:", projects);
        } else {
            const errText = await projectRes.text();
            console.error("❌ Failed to load projects:", errText);
        }
    } catch (err) {
        console.error("💥 Exception while fetching projects:", err);
    }

    // === STEP 2: Process each row in the quotes table ===
    const rows = document.querySelectorAll("#quotesTable tbody tr");
    console.log(`📊 Found ${rows.length} quotation rows in the table.`);

    rows.forEach(row => {
        const clientId = row.dataset.clientid;
        const projectId = row.dataset.projectid;
        const status = row.dataset.status;

        console.log(`\n📝 Processing quotation row — ClientId=${clientId}, ProjectId=${projectId}, Status=${status}`);

        // --- Replace Client cell ---
        const clientCell = row.querySelector(".quote-client");
        if (clientCell) {
            const client = clients.find(c => c.userId === clientId || c.id === clientId);
            if (client) {
                console.log("✅ Matched client:", client);
                clientCell.innerHTML = `
                    <i class="fas fa-user me-1"></i> ${client.fullName || client.name}
                    <br><small class="text-muted">${client.email || 'No email'} | ${client.phoneNumber || 'No phone'}</small>
                `;
            } else {
                console.warn("⚠️ No client found for ID:", clientId);
                clientCell.innerHTML = `<span class="text-danger">Unknown Client (${clientId})</span>`;
            }
        }

        // --- Replace Project cell ---
        const projectCell = row.querySelector(".quote-project");
        if (projectCell) {
            const project = projects.find(p => p.projectId === projectId || p.id === projectId);
            if (project) {
                console.log("✅ Matched project:", project);
                projectCell.innerHTML = `
                    <i class="fas fa-briefcase me-1"></i> ${project.name}
                `;
            } else {
                console.warn("⚠️ No project found for ID:", projectId);
                projectCell.innerHTML = `<span class="text-danger">Unknown Project (${projectId})</span>`;
            }
        }

        // --- Add extra actions if status is SentToClient ---
        if (status === "SentToClient") {
            console.log("📦 Adding extra buttons for SentToClient quotation...");

            const actions = row.querySelector(".btn-group");
            if (actions) {
                actions.insertAdjacentHTML("beforeend", `
                    <a href="/Quotes/Preview/${row.dataset.quotationid}?download=true"
                       class="btn btn-sm btn-outline-success">
                        <i class="fas fa-file-download"></i> Download
                    </a>
                    <a href="/Quotes/Estimate/${row.dataset.quotationid}"
                       class="btn btn-sm btn-outline-warning">
                        <i class="fas fa-copy"></i> Duplicate
                    </a>
                `);
            } else {
                console.warn("⚠️ No .btn-group found for actions injection.");
            }
        }
    });

    // === STEP 3: Filtering logic ===
    console.log("🔍 Setting up filters (search, status, date)...");

    const searchBox = document.getElementById("searchBox");
    const statusFilter = document.getElementById("statusFilter");
    const dateFilter = document.getElementById("dateFilter");

    function applyFilters() {
        const searchTerm = searchBox?.value.toLowerCase() || "";
        const statusTerm = statusFilter?.value || "";
        const dateTerm = dateFilter?.value ? new Date(dateFilter.value) : null;

        console.log(`\n🔎 Applying filters → Search="${searchTerm}", Status="${statusTerm}", Date="${dateTerm}"`);

        rows.forEach(row => {
            const text = row.innerText.toLowerCase();
            const status = row.dataset.status;
            const createdText = row.querySelector("td:nth-child(6)")?.innerText;
            const created = createdText ? new Date(createdText) : null;

            let matches = true;

            // Text search filter
            if (searchTerm && !text.includes(searchTerm)) {
                matches = false;
                console.log(`❌ Row filtered out by search: "${text}"`);
            }

            // Status filter
            if (statusTerm && status !== statusTerm) {
                matches = false;
                console.log(`❌ Row filtered out by status: ${status}`);
            }

            // Date filter
            if (dateTerm && created && created < dateTerm) {
                matches = false;
                console.log(`❌ Row filtered out by date: ${created}`);
            }

            row.style.display = matches ? "" : "none";
        });
    }

    // Attach filter events
    searchBox?.addEventListener("input", applyFilters);
    statusFilter?.addEventListener("change", applyFilters);
    dateFilter?.addEventListener("change", applyFilters);

    console.log("✅ Filters initialized successfully.");
});
