(function () {
  class PlanEditor {
    constructor() {
      this.projectId = null;
      this.mode = "phases";
      this.contractors = [];
      this.currentStep = 1;
      this.bind();
    }

    bind() {
      document
        .getElementById("plan-add-phase")
        ?.addEventListener("click", () => {
          this.addPhaseRow();
        });
      document
        .getElementById("plan-add-task")
        ?.addEventListener("click", () => {
          this.addTaskRow();
        });
      document.getElementById("plan-save")?.addEventListener("click", () => {
        this.save();
      });

      document.getElementById("plan-next")?.addEventListener("click", () => {
        if (this.currentStep === 1) {
          this.currentStep = 2;
          this.updateStepDisplay();
        }
      });
      document.getElementById("plan-prev")?.addEventListener("click", () => {
        if (this.currentStep === 2) {
          this.currentStep = 1;
          this.updateStepDisplay();
        }
      });
    }

    async init(projectId, mode) {
      this.projectId = projectId;
      this.mode = mode || "phases";
      // preload contractors
      try {
        const res = await fetch("/ProjectManager/GetContractors");
        if (res.ok) this.contractors = await res.json();
      } catch {}
      await this.loadExisting();
      this.applyMode();
      this.currentStep = this.mode === "tasks" ? 2 : 1;
      this.updateStepDisplay();
    }

    applyMode() {
      const phasesCol = document.getElementById("plan-phases")?.parentElement;
      const tasksCol = document.getElementById("plan-tasks")?.parentElement;
      if (!phasesCol || !tasksCol) return;
      if (this.mode === "phases") {
        phasesCol.style.display = "block";
        tasksCol.style.display = "block";
      } else if (this.mode === "tasks") {
        phasesCol.style.display = "block";
        tasksCol.style.display = "block";
      }
    }

    async loadExisting() {
      const phasesWrap = document.getElementById("plan-phases");
      const tasksWrap = document.getElementById("plan-tasks");
      phasesWrap.innerHTML = "";
      tasksWrap.innerHTML = "";
      try {
        const [phasesRes, tasksRes] = await Promise.all([
          fetch(
            `/ProjectManager/GetProjectPhases?id=${encodeURIComponent(
              this.projectId
            )}`
          ),
          fetch(
            `/ProjectManager/GetProjectTasks?id=${encodeURIComponent(
              this.projectId
            )}`
          ),
        ]);
        const phases = await phasesRes.json();
        const tasks = await tasksRes.json();
        (phases || []).forEach((p) => this.addPhaseRow(p));
        (tasks || []).forEach((t) => this.addTaskRow(t));
      } catch (e) {
        console.warn("Load failed", e);
      }
    }

    addPhaseRow(phase) {
      const wrap = document.getElementById("plan-phases");
      const id = (phase && phase.phaseId) || "phase_" + crypto.randomUUID();
      const el = document.createElement("div");
      el.className = "card mb-2";
      el.innerHTML = `
        <div class="card-body">
          <div class="row g-2 align-items-end">
            <div class="col-md-6">
              <label class="form-label">Phase Name</label>
              <input type="text" class="form-control pe-name" data-id="${id}" value="${
        (phase && phase.name) || ""
      }">
            </div>
            <div class="col-md-5">
              <label class="form-label">Description</label>
              <input type="text" class="form-control pe-desc" value="${
                (phase && phase.description) || ""
              }">
            </div>
            <div class="col-md-1 text-end">
              <button class="btn btn-outline-danger btn-sm pe-remove">Remove</button>
            </div>
          </div>
        </div>`;
      el.querySelector(".pe-remove").addEventListener("click", () =>
        el.remove()
      );
      wrap.appendChild(el);
    }

    contractorOptions(selected) {
      const opts = ['<option value="">Select contractor...</option>'].concat(
        (this.contractors || []).map(
          (c) =>
            `<option value="${c.userId}" ${
              c.userId === selected ? "selected" : ""
            }>${c.fullName} (${c.email})</option>`
        )
      );
      return opts.join("");
    }

    addTaskRow(task) {
      const wrap = document.getElementById("plan-tasks");
      const id = (task && task.taskId) || "task_" + crypto.randomUUID();
      const el = document.createElement("div");
      el.className = "card mb-2";
      el.innerHTML = `
        <div class="card-body">
          <div class="row g-2 align-items-end">
            <div class="col-md-3">
              <label class="form-label">Task Name</label>
              <input type="text" class="form-control pt-name" data-id="${id}" value="${
        (task && task.name) || ""
      }">
            </div>
            <div class="col-md-2">
              <label class="form-label">Phase</label>
              <select class="form-select pt-phase"></select>
            </div>
            <div class="col-md-2">
              <label class="form-label">Contractor</label>
              <select class="form-select pt-contractor">${this.contractorOptions(
                task && task.assignedTo
              )}</select>
            </div>
            <div class="col-md-2">
              <label class="form-label">Start</label>
              <input type="date" class="form-control pt-start" value="${
                task && task.startDate
                  ? new Date(task.startDate).toISOString().split("T")[0]
                  : ""
              }">
            </div>
            <div class="col-md-2">
              <label class="form-label">Due</label>
              <input type="date" class="form-control pt-due" value="${
                task && task.dueDate
                  ? new Date(task.dueDate).toISOString().split("T")[0]
                  : ""
              }">
            </div>
            <div class="col-md-1 text-end">
              <button class="btn btn-outline-danger btn-sm pt-remove">Remove</button>
            </div>
          </div>
        </div>`;
      el.querySelector(".pt-remove").addEventListener("click", () =>
        el.remove()
      );
      wrap.appendChild(el);
      // populate phase options from current phase rows
      this.refreshTaskPhaseSelects();
      if (task && task.phaseId) {
        el.querySelector(".pt-phase").value = task.phaseId;
      }
    }

    getPhases() {
      return Array.from(document.querySelectorAll("#plan-phases .card")).map(
        (card) => ({
          phaseId: card.querySelector(".pe-name")?.dataset.id || "",
          projectId: this.projectId,
          name: card.querySelector(".pe-name")?.value || "",
          description: card.querySelector(".pe-desc")?.value || "",
        })
      );
    }

    getTasks() {
      return Array.from(document.querySelectorAll("#plan-tasks .card")).map(
        (card) => ({
          taskId: card.querySelector(".pt-name")?.dataset.id || "",
          projectId: this.projectId,
          name: card.querySelector(".pt-name")?.value || "",
          phaseId: card.querySelector(".pt-phase")?.value || "",
          assignedTo: card.querySelector(".pt-contractor")?.value || "",
          startDate:
            this.parseDateInput(card.querySelector(".pt-start")?.value) ||
            new Date().toISOString(),
          dueDate:
            this.parseDateInput(card.querySelector(".pt-due")?.value) ||
            new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
          status: "Pending",
          priority: "Medium",
          progress: 0,
          estimatedHours: 0,
          actualHours: 0,
          description: "",
        })
      );
    }

    parseDateInput(value) {
      if (!value) return null;
      const d = new Date(value);
      if (isNaN(d.getTime())) return null;
      return d.toISOString();
    }

    refreshTaskPhaseSelects() {
      const options = ['<option value="">Select phase...</option>']
        .concat(
          this.getPhases().map(
            (p) =>
              `<option value="${p.phaseId}">${p.name || p.phaseId}</option>`
          )
        )
        .join("");
      document.querySelectorAll(".pt-phase").forEach((sel) => {
        const cur = sel.value;
        sel.innerHTML = options;
        if (cur) sel.value = cur;
      });
    }

    async save() {
      const phases = this.getPhases().filter((p) => p.name.trim());
      const tasks = this.getTasks().filter((t) => t.name.trim());
      try {
        if (phases.length) {
          await fetch(
            `/ProjectManager/SavePhases?id=${encodeURIComponent(
              this.projectId
            )}`,
            {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify(phases),
            }
          );
        }
        if (tasks.length) {
          console.log("Saving tasks", tasks);
          try {
            const res = await fetch(
              `/ProjectManager/SaveTasks?id=${encodeURIComponent(
                this.projectId
              )}`,
              {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(tasks),
              }
            );
            if (res.ok) {
              console.log("Tasks saved successfully");
            } else {
              console.error("Failed to save tasks", res.status, res.statusText);
            }
          } catch (e) {
            console.error("Error saving tasks", e);
          }
        }
        // simple toast using existing helper if available
        if (window.showToast) showToast("Saved successfully", "success");
      } catch (e) {
        console.error(e);
        if (window.showToast) showToast("Save failed", "danger");
      }
    }

    updateStepDisplay() {
      const step1 = document.getElementById("plan-step1");
      const step2 = document.getElementById("plan-step2");
      const prevBtn = document.getElementById("plan-prev");
      const nextBtn = document.getElementById("plan-next");
      const saveBtn = document.getElementById("plan-save");
      const stepSpan = document.getElementById("planStep");
      const title = document.getElementById("planStepTitle");
      const progress = document.getElementById("planProgress");

      if (!step1 || !step2 || !prevBtn || !nextBtn || !saveBtn) return;

      if (this.currentStep === 1) {
        step1.classList.remove("d-none");
        step2.classList.add("d-none");
        prevBtn.style.display = "none";
        nextBtn.style.display = "inline-block";
        saveBtn.style.display = "none";
        if (stepSpan) stepSpan.textContent = "1";
        if (title) title.textContent = "Phases";
        if (progress) progress.style.width = "50%";
      } else {
        step1.classList.add("d-none");
        step2.classList.remove("d-none");
        prevBtn.style.display = "inline-block";
        nextBtn.style.display = "none";
        saveBtn.style.display = "inline-block";
        if (stepSpan) stepSpan.textContent = "2";
        if (title) title.textContent = "Tasks";
        if (progress) progress.style.width = "100%";
      }
    }
  }

  window.__planEditor = new PlanEditor();
})();
