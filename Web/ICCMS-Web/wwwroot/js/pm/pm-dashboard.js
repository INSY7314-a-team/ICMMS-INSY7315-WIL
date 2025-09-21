// === PM Dashboard widgets (KPI/Projects/Maintenance) ===
var QUOTE_ONLY = !!(window.PM && window.PM.QUOTE_ONLY); // page flag
const ROOT  = (window.PM && window.PM.root)  || '/';
const TOKEN = (window.PM && window.PM.token) || '';
const authHeaders = TOKEN ? { 'Authorization': `Bearer ${TOKEN}` } : {};
const $id = (x)=>document.getElementById(x);

const getJson = async (u)=>{ const r=await fetch(u,{headers:authHeaders}); if(!r.ok) throw new Error(r.status); return r.json(); };
const getJsonSafe = async (u,f=[])=>{ try{return await getJson(u);}catch{return f;} };
const safe=(o,...k)=>{for(const x of k){if(o && Object.prototype.hasOwnProperty.call(o,x))return o[x];}};
const fmtMoney=(v)=>'R '+(Number(v)||0).toLocaleString();
const dotClass=(s)=>{const v=(s||'').toLowerCase();if(['on track','in progress'].includes(v))return'dot-ok';if(['at risk','delayed','on hold'].includes(v))return'dot-warn';if(v==='completed')return'dot-neutral';return'dot-neutral';};
const statusChip=(s)=>{const v=(s||'').toLowerCase(); if(v==='completed')return'<span class="chip ok">Completed</span>'; if(['at risk','delayed','on hold'].includes(v))return`<span class="chip danger">${s}</span>`; if(['on track','in progress'].includes(v))return`<span class="chip warn">${s}</span>`; return `<span class="chip">${s||'—'}</span>`;};

let PROJECTS=[], PROJ_FILTER='all', CONTRACTORS=[], MAINT=[];
const DRAFT_KEY='pm.projects.drafts', ASSIGN_KEY='pm.maintenance.assignments';
const loadDrafts=()=>{try{return JSON.parse(localStorage.getItem(DRAFT_KEY)||'{}')}catch{return{}}};
const saveDrafts=(o)=>localStorage.setItem(DRAFT_KEY, JSON.stringify(o||{}));
const applyDrafts=(list)=>{const d=loadDrafts(); return list.map(p=> d[p.id||p.projectId] ? {...p, ...d[p.id||p.projectId]} : p);};
function setUpdated(){ $id('last-updated') && ($id('last-updated').textContent = new Date().toLocaleString()); }

function refreshKpis(){
  const active = PROJECTS.filter(p => (p.status||'').toLowerCase()!=='completed').length;
  const budget = PROJECTS.reduce((a,b)=>a+(Number(b.budgetPlanned)||0),0);
  $id('kpi-projects') && ($id('kpi-projects').textContent = active);
  $id('kpi-budget')   && ($id('kpi-budget').textContent   = 'R ' + budget.toLocaleString());
  $id('kpi-contractors') && ($id('kpi-contractors').textContent = CONTRACTORS.length);
  const openMaint = MAINT.filter(m => (m.status||'').toLowerCase()!=='assigned').length;
  $id('kpi-maint') && ($id('kpi-maint').textContent = openMaint);
}

function renderProjects(){
  const q=($id('global-search')?.value||'').trim().toLowerCase();
  let list=PROJECTS.slice();
  if(PROJ_FILTER==='active') list=list.filter(p=>(p.status||'').toLowerCase()!=='completed');
  if(PROJ_FILTER==='at-risk') list=list.filter(p=>['at risk','delayed','on hold'].includes((p.status||'').toLowerCase()));
  if(PROJ_FILTER==='completed') list=list.filter(p=>(p.status||'').toLowerCase()==='completed');
  if(q) list=list.filter(p=>(p.name||'').toLowerCase().includes(q)||(p.description||'').toLowerCase().includes(q));

  const wrap=$id('projects-cards'); if(!wrap) return;
  if(!list.length){ wrap.innerHTML='<div class="muted">No matching projects.</div>'; }
  else{
    wrap.innerHTML = list.map(p=>{
      const plan=Number(p.budgetPlanned)||0, act=Number(p.budgetActual)||0, pct=plan>0?Math.min(100,Math.round(act/plan*100)):0;
      return `
        <div class="card card-proj" data-pid="${p.id}">
          <div class="card-body">
            <div class="d-flex justify-content-between align-items-start mb-2">
              <div class="d-flex align-items-center"><span class="status-dot ${dotClass(p.status)}"></span><strong>${p.name||'—'}</strong></div>
              ${statusChip(p.status)}
            </div>
            <div class="muted small mb-2">${p.description||''}</div>
            <div class="mb-2">
              <div class="d-flex justify-content-between small"><span>Budget</span><span>${fmtMoney(act)} / ${fmtMoney(plan)}</span></div>
              <div class="progress" style="height:6px"><div class="progress-bar" style="width:${pct}%"></div></div>
            </div>
            <div class="small d-flex justify-content-between mb-2">
              <span><i class="fas fa-play me-1"></i>${p.startDatePlanned?new Date(p.startDatePlanned).toLocaleDateString():'—'}</span>
              <span><i class="fas fa-flag-checkered me-1"></i>${p.endDatePlanned?new Date(p.endDatePlanned).toLocaleDateString():'—'}</span>
              <span><i class="fas fa-check me-1"></i>${p.endDateActual?new Date(p.endDateActual).toLocaleDateString():'—'}</span>
            </div>
            <div class="d-flex justify-content-end">
              <button class="btn btn-sm btn-brand btn-edit" data-pid="${p.id}"><i class="fas fa-pen me-1"></i>Edit</button>
            </div>
          </div>
        </div>`;
    }).join('');
  }
  $id('projects-sub') && ($id('projects-sub').textContent = `${list.length} shown / ${PROJECTS.length} total`);
}

function renderAllProjects(){
  const q=($id('all-proj-search')?.value||'').trim().toLowerCase();
  let list=PROJECTS.slice(); if(q) list=list.filter(p=>(p.name||'').toLowerCase().includes(q)||(p.description||'').toLowerCase().includes(q));
  const tb=$id('all-proj-body'); if(!tb) return;
  tb.innerHTML = list.map(p=>`
    <tr>
      <td>${p.name||'—'}<div class="muted small">${p.description||''}</div></td>
      <td>${statusChip(p.status)}</td>
      <td>${fmtMoney(p.budgetPlanned)}</td>
      <td>${fmtMoney(p.budgetActual)}</td>
      <td>${p.startDatePlanned?new Date(p.startDatePlanned).toLocaleDateString():'—'}</td>
      <td>${p.endDatePlanned?new Date(p.endDatePlanned).toLocaleDateString():'—'}</td>
      <td>${p.endDateActual?new Date(p.endDateActual).toLocaleDateString():'—'}</td>
      <td class="text-end"><button class="btn btn-sm btn-brand btn-edit" data-pid="${p.id}" data-bs-dismiss="modal">Edit</button></td>
    </tr>`).join('');
  $id('all-proj-sub') && ($id('all-proj-sub').textContent = `${list.length} / ${PROJECTS.length}`);
}

function openProjectModal(id){
  const p=PROJECTS.find(x=>x.id===id); if(!p) return;
  $id('proj-id').value = p.id || '';
  $id('proj-name').value = p.name || '';
  $id('proj-status').value = p.status || 'In Progress';
  $id('proj-desc').value = p.description || '';
  $id('proj-bp').value = p.budgetPlanned ?? '';
  $id('proj-ba').value = p.budgetActual ?? '';
  $id('proj-start').value = p.startDatePlanned ? new Date(p.startDatePlanned).toISOString().slice(0,10) : '';
  $id('proj-endp').value = p.endDatePlanned ? new Date(p.endDatePlanned).toISOString().slice(0,10) : '';
  $id('proj-enda').value = p.endDateActual ? new Date(p.endDateActual).toISOString().slice(0,10) : '';
  new bootstrap.Modal($id('projectModal')).show();
}

function collectProjectForm(){
  return {
    id:$id('proj-id').value, name:$id('proj-name').value, status:$id('proj-status').value, description:$id('proj-desc').value,
    budgetPlanned:Number($id('proj-bp').value||0), budgetActual:Number($id('proj-ba').value||0),
    startDatePlanned:$id('proj-start').value || null, endDatePlanned:$id('proj-endp').value || null, endDateActual:$id('proj-enda').value || null
  };
}

function renderMaint(){
  const listEl=$id('maint-list'); if(!listEl) return;
  if(!MAINT.length){ listEl.innerHTML='<div class="muted">No maintenance requests.</div>'; return; }
  listEl.innerHTML = MAINT.slice(0,6).map(m=>{
    const p=(m.priority||'').toLowerCase(); const chip = p==='high'?'danger':(p==='medium'?'warn':'ok');
    return `
      <div class="d-flex align-items-start mb-2 pb-2 border-bottom">
        <div class="me-2"><i class="fas fa-screwdriver-wrench ${chip==='danger'?'text-danger':chip==='warn'?'text-warning':'text-success'}"></i></div>
        <div class="flex-grow-1">
          <div class="d-flex justify-content-between">
            <strong>${m.title||'Request'}</strong>
            <span class="chip ${chip}">${m.priority||'—'}</span>
          </div>
          <div class="muted small">${m.site||'—'} • ${m.reportedAt?new Date(m.reportedAt).toLocaleString():'—'}</div>
          <div class="mt-1">
            ${(m.status||'').toLowerCase()==='assigned'
              ? '<span class="chip ok">Assigned</span>'
              : `<button class="btn btn-sm btn-brand btn-assign" data-mid="${m.id}"><i class="fas fa-user-plus me-1"></i>Assign</button>`}
          </div>
        </div>
      </div>`;
  }).join('');
}

function renderAllMaint(){
  const q=$id('maint-search')?.value?.trim().toLowerCase()||'';
  let list=MAINT.slice(); if(q) list=list.filter(m=>(m.title||'').toLowerCase().includes(q)||(m.site||'').toLowerCase().includes(q));
  const tb=$id('maint-body'); if(!tb) return;
  tb.innerHTML = list.map(m=>{
    const p=(m.priority||'').toLowerCase(); const chip = p==='high'?'danger':(p==='medium'?'warn':'ok');
    return `<tr>
      <td>${m.title||'Request'}</td><td>${m.site||'—'}</td><td><span class="chip ${chip}">${m.priority||'—'}</span></td>
      <td>${m.reportedAt?new Date(m.reportedAt).toLocaleString():'—'}</td>
      <td>${(m.status||'').toLowerCase()==='assigned' ? '<span class="chip ok">Assigned</span>' : '<span class="chip warn">Open</span>'}</td>
      <td class="text-end">${(m.status||'').toLowerCase()==='assigned' ? '<span class="muted small">—</span>' : `<button class="btn btn-sm btn-brand btn-assign" data-mid="${m.id}" data-bs-dismiss="modal">Assign</button>`}</td>
    </tr>`;
  }).join('');
  $id('maint-sub') && ($id('maint-sub').textContent = `${list.length} / ${MAINT.length}`);
}

/* Assign modal helpers */
let ASSIGN_WORKING_ID=null;
function openAssignModal(mid){
  ASSIGN_WORKING_ID = mid;
  const m = MAINT.find(x=>x.id===mid); if(!m) return;
  $id('assign-header').innerHTML = `<div><strong>${m.title||'Request'}</strong>
    <div class="muted small">${m.site||'—'} • ${m.reportedAt?new Date(m.reportedAt).toLocaleString():'—'}</div>
    <div class="mt-1">${m.description||''}</div></div>`;
  $id('task-list').innerHTML = ''; addTaskRow();
  new bootstrap.Modal($id('assignModal')).show();
}
function addTaskRow(){
  const contractorOptions = CONTRACTORS.map(c=>`<option value="${c.id||c.name}">${c.name} — ${c.specialty||'General'}</option>`).join('');
  const row = document.createElement('div');
  row.className='task-row border rounded p-2 mb-2';
  row.innerHTML = `
    <div class="row g-2 align-items-end">
      <div class="col-md-4"><label class="form-label">Task Name</label><input class="form-control task-name" placeholder="e.g. Fix leak in unit 12"></div>
      <div class="col-md-3"><label class="form-label">Due Date</label><input class="form-control task-due" type="date"></div>
      <div class="col-md-3"><label class="form-label">Contractor</label><select class="form-select task-ctr">${contractorOptions}</select></div>
      <div class="col-md-2"><button class="btn btn-outline-secondary w-100 btn-remove-task"><i class="fas fa-trash"></i></button></div>
    </div>`;
  $id('task-list').appendChild(row);
}
function collectAssignments(){
  return [...document.querySelectorAll('#task-list .task-row')].map(r=>({
    name:r.querySelector('.task-name').value?.trim(),
    dueDate:r.querySelector('.task-due').value || null,
    contractor:r.querySelector('.task-ctr').value || null
  })).filter(t=>t.name);
}
function saveAssignments(mid, tasks){
  const all = JSON.parse(localStorage.getItem(ASSIGN_KEY)||'{}'); all[mid] = { tasks, savedAt: new Date().toISOString() };
  localStorage.setItem(ASSIGN_KEY, JSON.stringify(all));
  const idx=MAINT.findIndex(x=>x.id===mid); if(idx>-1){ MAINT[idx]={...MAINT[idx], status:'Assigned'}; }
  renderMaint(); refreshKpis();
}

/* Data loads (mock now) */
async function loadProjects(){
  const raw = await getJsonSafe(ROOT + 'mock/projects.json', []);
  PROJECTS = raw.map(p=>({ id:safe(p,'projectId','id'), name:safe(p,'name'), description:safe(p,'description'), status:safe(p,'status'),
    budgetPlanned:safe(p,'budgetPlanned'), budgetActual:safe(p,'budgetActual'),
    startDatePlanned:safe(p,'startDatePlanned'), endDatePlanned:safe(p,'endDatePlanned'), endDateActual:safe(p,'endDateActual') }));
  PROJECTS = applyDrafts(PROJECTS); refreshKpis(); renderProjects();
}
async function loadContractors(){
  CONTRACTORS = await getJsonSafe(ROOT + 'mock/contractors.json', []);
  $id('kpi-contractors') && ($id('kpi-contractors').textContent = CONTRACTORS.length);
  const box=$id('contractors-list'); if(!box) return;
  box.innerHTML = CONTRACTORS.slice(0,6).map(c=>`
    <div class="d-flex align-items-center mb-2">
      <div class="bg-secondary rounded-circle text-white d-flex align-items-center justify-content-center me-2" style="width:28px;height:28px;font-size:10px;">
        <i class="fa-solid fa-user"></i>
      </div>
      <div class="flex-grow-1">
        <div class="small fw-semibold">${c.name||'—'}</div>
        <div class="small muted">${c.specialty||''}</div>
      </div>
      <span class="chip ${((c.status||'').toLowerCase()==='active')?'ok':((c.status||'').toLowerCase()==='available')?'warn':'danger'}">${c.status||'—'}</span>
    </div>`).join('') || '<div class="muted">No contractor data.</div>';
}
async function loadMaintenance(){
  const fallback=[{id:'M-1001',title:'Burst pipe at Basement',site:'Sandton Office',priority:'High',reportedAt:new Date().toISOString(),description:'Water leaking near pump room',status:'Open'},
                  {id:'M-1002',title:'HVAC not cooling',site:'Cape Town Residential - Block B',priority:'Medium',reportedAt:new Date(Date.now()-3600e3).toISOString(),description:'Unit 4 complaints',status:'Open'},
                  {id:'M-1003',title:'Light fittings flicker',site:'Pretoria Commercial',priority:'Low',reportedAt:new Date(Date.now()-86400e3).toISOString(),description:'Lobby LED strip issue',status:'Open'}];
  MAINT = await getJsonSafe(ROOT + 'mock/maintenance.json', fallback);
  renderMaint(); refreshKpis();
}

/* Events */
document.addEventListener('input',(e)=>{
  switch(e.target.id){
    case 'global-search':   renderProjects(); break;
    case 'all-proj-search': renderAllProjects(); break;
    case 'maint-search':    renderAllMaint(); break;
  }
});
document.addEventListener('click',(e)=>{
  if(e.target.matches('.proj-filter')){
    document.querySelectorAll('.proj-filter').forEach(b=>b.classList.remove('active'));
    e.target.classList.add('active'); PROJ_FILTER=e.target.dataset.filter; renderProjects();
  }
  if(e.target.matches('.btn-edit')){ openProjectModal(e.target.dataset.pid); }
  if(e.target.id==='btn-view-all-projects'){ renderAllProjects(); new bootstrap.Modal($id('allProjectsModal')).show(); }
  if(e.target.id==='btn-save-proj'){
    const upd=collectProjectForm(); if(!upd.id) return;
    const d=loadDrafts(); d[upd.id]={...(d[upd.id]||{}), ...upd}; saveDrafts(d);
    const idx=PROJECTS.findIndex(p=>p.id===upd.id); if(idx>-1) PROJECTS[idx]={...PROJECTS[idx], ...upd};
    renderProjects(); refreshKpis(); bootstrap.Modal.getInstance($id('projectModal'))?.hide();
  }
  if(e.target.id==='btn-reset-proj'){
    const id=$id('proj-id').value; const d=loadDrafts(); delete d[id]; saveDrafts(d);
    const p=PROJECTS.find(x=>x.id===id); if(p){ openProjectModal(id); }
  }
  if(e.target.id==='refresh-btn'){ Promise.all([loadProjects(), loadContractors(), loadMaintenance()]).then(setUpdated); }
  if(e.target.id==='btn-new-project' || e.target.id==='btn-new-project-cta'){ new bootstrap.Modal($id('newProjectModal')).show(); }
  if(e.target.id==='btn-create-proj'){
    const name=$id('np-name').value.trim(), bp=Number($id('np-bp').value||0), start=$id('np-start').value, endp=$id('np-endp').value;
    if(!name || !bp || !start || !endp){ alert('Please complete required fields.'); return; }
    const id='P-'+(Date.now().toString().slice(-6));
    const proj={ id, name, description:$id('np-desc').value.trim(), status:'On Track', budgetPlanned:bp, budgetActual:0,
      startDatePlanned:start, endDatePlanned:endp, endDateActual:null, client:$id('np-client').value.trim(),
      location:$id('np-location').value.trim(), type:$id('np-type').value, risk:$id('np-risk').value, tags:$id('np-tags').value };
    const d=loadDrafts(); d[id]={...proj}; saveDrafts(d); PROJECTS.unshift(proj); renderProjects(); refreshKpis();
    bootstrap.Modal.getInstance($id('newProjectModal'))?.hide(); $id('new-proj-form').reset();
  }
  if(e.target.id==='btn-view-all-maint'){ renderAllMaint(); new bootstrap.Modal($id('allMaintModal')).show(); }
  if(e.target.matches('.btn-assign')){ openAssignModal(e.target.dataset.mid); }
  if(e.target.id==='btn-add-task'){ addTaskRow(); }
  if(e.target.classList.contains('btn-remove-task')){ e.target.closest('.task-row')?.remove(); }
  if(e.target.id==='btn-save-assign'){
    const tasks=collectAssignments(); if(!tasks.length){ alert('Add at least one task.'); return; }
    saveAssignments(ASSIGN_WORKING_ID, tasks); bootstrap.Modal.getInstance($id('assignModal'))?.hide();
  }
});

/* Bootstrap on-load */
if (!QUOTE_ONLY) { (async function(){ await Promise.all([loadProjects(), loadContractors(), loadMaintenance()]); setUpdated(); })(); }
