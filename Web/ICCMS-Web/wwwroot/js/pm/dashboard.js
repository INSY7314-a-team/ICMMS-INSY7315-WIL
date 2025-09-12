 const ROOT = (window.PM && window.PM.root) || '/';

 const TOKEN = (window.PM && window.PM.token) || '';
  const authHeaders = TOKEN ? { 'Authorization': `Bearer ${TOKEN}` } : {};
  const getJson = async (u)=>{ const r=await fetch(u,{headers:authHeaders}); if(!r.ok) throw new Error(r.status); return r.json(); };
  const getJsonSafe = async (u,f=[])=>{ try{return await getJson(u);}catch{return f;} };
  const safe=(o,...k)=>{for(const x of k){if(o && Object.prototype.hasOwnProperty.call(o,x))return o[x];}};
  const fmtMoney=(v)=>'R '+(Number(v)||0).toLocaleString();
  const dotClass=(s)=>{const v=(s||'').toLowerCase();if(['on track','in progress'].includes(v))return'dot-ok';if(['at risk','delayed','on hold'].includes(v))return'dot-warn';if(v==='completed')return'dot-neutral';return'dot-neutral';};
  const statusChip=(s)=>{const v=(s||'').toLowerCase();
    if(v==='completed') return '<span class="chip ok">Completed</span>';
    if(['at risk','delayed','on hold'].includes(v)) return `<span class="chip danger">${s}</span>`;
    if(['on track','in progress'].includes(v)) return `<span class="chip warn">${s}</span>`;
    return `<span class="chip">${s||'—'}</span>`;};

  // --- State & keys ---
  let PROJECTS=[], PROJ_FILTER='all', CONTRACTORS=[], MAINT=[];
  const DRAFT_KEY='pm.projects.drafts';
  const ASSIGN_KEY='pm.maintenance.assignments';

  const loadDrafts = ()=>{ try{return JSON.parse(localStorage.getItem(DRAFT_KEY)||'{}');}catch{return{}} };
  const saveDrafts = (obj)=> localStorage.setItem(DRAFT_KEY, JSON.stringify(obj||{}));
  const applyDrafts = (list)=>{
    const d=loadDrafts();
    return list.map(p=> d[p.id||p.projectId] ? {...p, ...d[p.id||p.projectId]} : p);
  };

  function setUpdated(){ document.getElementById('last-updated').textContent = new Date().toLocaleString(); }
  (() => {
    const PHASES = [
          { key: 'precon',  name: 'Precon',   host: '#q-rows-precon' },
          { key: 'demo',    name: 'Demo',     host: '#q-rows-demo' },
          { key: 'struct',  name: 'Struct',   host: '#q-rows-struct' },
          { key: 'mep',     name: 'MEP',      host: '#q-rows-mep' },
          { key: 'finish',  name: 'Finishes', host: '#q-rows-finish' },
          { key: 'closeout',name: 'Closeout', host: '#q-rows-closeout' }
    ];
    const state = { clients:[], contractors:[], items: { precon:[], demo:[], struct:[], mep:[], finish:[], closeout:[], maint:[] } };



    // helpers
    const fmt = v => 'R ' + (Number(v)||0).toLocaleString();
    function tabOrder() {
      return ['#tab-client','#tab-precon','#tab-demo','#tab-struct','#tab-mep','#tab-finish','#tab-closeout','#tab-breakdown'];
    }
    function gotoTab(id){
    new bootstrap.Tab(document.querySelector(`#quoteTabs button[data-bs-target="${id}"]`)).show();
    if(id==='#tab-breakdown') renderBreakdown();
    }


    // load clients
    async function loadClients(){
      try{
        const res = await fetch(ROOT + 'mock/clients.json');
        state.clients = res.ok ? await res.json() : [];
      }catch{ state.clients=[]; }
      const sel = document.getElementById('q-client-select');
      sel.innerHTML = '<option value="">— Select —</option>' +
        state.clients.map(c=>`<option value="${c.id}">${c.name} — ${c.org||''}</option>`).join('');
    }

    //load contractors
    async function loadContractors()
    {
      try {
        const r = await fetch(ROOT + 'mock/contractors.json');
        state.contractors = r.ok ? await r.json() : [];
     } catch { state.contractors = []; }
    }


    // rows
    function addRow(phaseKey, type, seed={}){
    const host = document.querySelector(PHASES.find(p=>p.key===phaseKey).host);
    const ctrOpts = ['<option value="">Unassigned</option>']
      .concat(state.contractors.map(c=>`<option value="${c.id||c.name}">${c.name} — ${c.specialty||'General'}</option>`))
      .join('');

    const row = document.createElement('div');
    row.className = 'border rounded p-2 mb-2';
    row.innerHTML = `
      <div class="row g-2 align-items-end">
        <div class="col-lg-4">
          <label class="form-label">${type} name</label>
          <input class="form-control q-name" value="${seed.name||''}" placeholder="${type==='Labour'?'e.g. Plumber':'e.g. 10mm rebar'}">
        </div>
        <div class="col-6 col-lg-2">
          <label class="form-label">Qty</label>
          <input type="number" min="0" step="0.01" class="form-control q-qty" value="${seed.qty??''}">
        </div>
        <div class="col-6 col-lg-2">
          <label class="form-label">Unit</label>
          <input class="form-control q-unit" value="${seed.unit|| (type==='Labour'?'hrs':'ea')}">
        </div>
        <div class="col-6 col-lg-2">
          <label class="form-label">Unit price</label>
          <input type="number" min="0" step="0.01" class="form-control q-price" value="${seed.unitPrice??''}">
        </div>
        <div class="col-6 col-lg-2">
          <label class="form-label">Line total</label>
          <div class="form-control-plaintext text-end q-total" title=""></div>
          <div class="small text-muted text-end q-calc"></div>
        </div>
      </div>
      <div class="row g-2 align-items-end mt-1">
        <div class="col-lg-6">
          <label class="form-label">Assign contractor</label>
          <select class="form-select q-ctr">${ctrOpts}</select>
        </div>
        <div class="col-lg-6 text-end">
          <button class="btn btn-outline-secondary mt-3 q-del"><i class="fas fa-trash"></i></button>
        </div>
      </div>`;

    host.appendChild(row);

    const rec = {
      type,
      name: seed.name||'',
      qty: Number(seed.qty)||0,
      unit: seed.unit|| (type==='Labour'?'hrs':'ea'),
      unitPrice: Number(seed.unitPrice)||0,
      contractorId: '',
      contractorName: '',
      _el: row
    };
    state.items[phaseKey].push(rec);

    const nameEl=row.querySelector('.q-name');
    const qtyEl=row.querySelector('.q-qty');
    const unitEl=row.querySelector('.q-unit');
    const priceEl=row.querySelector('.q-price');
    const totalEl=row.querySelector('.q-total');
    const calcEl=row.querySelector('.q-calc');
    const ctrEl=row.querySelector('.q-ctr');
    

    const fmt = v=>'R '+(Number(v)||0).toLocaleString();
    function updateCalc(){
      rec.name = nameEl.value;
      rec.qty = Number(qtyEl.value||0);
      rec.unit = unitEl.value;
      rec.unitPrice = Number(priceEl.value||0);
      const lt = (rec.qty||0) * (rec.unitPrice||0);
      totalEl.textContent = fmt(lt);
      totalEl.title = `${rec.qty||0} × ${fmt(rec.unitPrice||0)} = ${fmt(lt)}`;
      calcEl.textContent = `${rec.qty||0} × ${rec.unitPrice||0}`;
      refreshQuoteTotals();

    }
    
    [nameEl, qtyEl, unitEl, priceEl].forEach(el=> el.addEventListener('input', updateCalc));
    ctrEl.addEventListener('change', ()=>{
      rec.contractorId = ctrEl.value || '';
      const c = state.contractors.find(x=>(x.id||x.name)===rec.contractorId);
      rec.contractorName = c ? c.name : '';
    });
    row.querySelector('.q-del').addEventListener('click',(e)=>{
      e.preventDefault(); row.remove();
      const arr=state.items[phaseKey]; const i=arr.indexOf(rec); if(i>-1) arr.splice(i,1);
      refreshQuoteTotals();

    });

    updateCalc();
    refreshQuoteTotals();

  }



    //Running quote total
    function refreshQuoteTotals(){
    const m = Number(document.getElementById('q-markup')?.value || 0);
    const t = Number(document.getElementById('q-tax')?.value || 0);
    let subtotal = 0;

    PHASES.forEach(p=>{
        state.items[p.key].forEach(it=>{
        const lt = (Number(it.qty)||0) * (Number(it.unitPrice)||0);
        subtotal += lt;
        });
    });

    const markup = subtotal * (m/100);
    const taxed  = (subtotal + markup) * (t/100);
    const total  = subtotal + markup + taxed;

    const host = document.getElementById('q-sticky-summary');
    if(host){
        host.innerHTML = `
        <div class="d-flex flex-wrap justify-content-end gap-3">
            <div>Subtotal: <strong>${fmt(subtotal)}</strong></div>
            <div>Markup (${m}%): <strong>${fmt(markup)}</strong></div>
            <div>Tax (${t}%): <strong>${fmt(taxed)}</strong></div>
            <div class="ms-2">Total: <strong>${fmt(total)}</strong></div>
        </div>`;
    }
    // keep Breakdown view in sync live
    renderBreakdown();

    }

//rendering the breadown section
    function renderBreakdown(){
    const host = document.getElementById('q-breakdown');
    if(!host) return;

    const m = Number(document.getElementById('q-markup')?.value || 0);
    const t = Number(document.getElementById('q-tax')?.value || 0);

    const sections = [];

    PHASES.forEach(p=>{
        const items = state.items[p.key] || [];
        if(!items.length) return;

        let phaseSub=0, phaseMarkup=0, phaseTax=0, phaseTotal=0;

        const rows = items.map(it=>{
        const qty = Number(it.qty)||0;
        const up  = Number(it.unitPrice)||0;
        const sub = qty * up;
        const mu  = sub * (m/100);
        const tx  = (sub + mu) * (t/100);          // mirror sticky footer calc
        const tot = sub + mu + tx;

        phaseSub    += sub;
        phaseMarkup += mu;
        phaseTax    += tx;
        phaseTotal  += tot;

        return `
            <tr>
            <td>
                <div class="fw-semibold">${it.name||'—'}</div>
                <div class="small muted">${it.type||''}${it.contractorName ? ' • ' + it.contractorName : ''}</div>
            </td>
            <td class="text-end">${(qty||0).toLocaleString()}</td>
            <td>${it.unit||''}</td>
            <td class="text-end">${fmt(up)}</td>
            <td class="text-end">${fmt(sub)}</td>
            <td class="text-end text-muted">${fmt(mu)}</td>
            <td class="text-end text-muted">${fmt(tx)}</td>
            <td class="text-end"><strong>${fmt(tot)}</strong></td>
            </tr>`;
        }).join('');

        const collapseId = `bd-${p.key}`;

        sections.push(`
        <div class="accordion-item">
            <h2 class="accordion-header">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#${collapseId}">
                <div class="w-100 d-flex justify-content-between align-items-center">
                <span>${p.name}</span>
                <span class="small muted">
                    Subtotal ${fmt(phaseSub)} · Markup ${fmt(phaseMarkup)} · Tax ${fmt(phaseTax)} · <strong>Total ${fmt(phaseTotal)}</strong>
                </span>
                </div>
            </button>
            </h2>
            <div id="${collapseId}" class="accordion-collapse collapse" data-bs-parent="#bd-accordion">
            <div class="accordion-body p-0">
                <div class="table-responsive">
                <table class="table table-sm align-middle mb-0">
                    <thead>
                    <tr>
                        <th>Item</th>
                        <th class="text-end">Qty</th>
                        <th>Unit</th>
                        <th class="text-end">Unit Price</th>
                        <th class="text-end">Subtotal</th>
                        <th class="text-end">Markup (${m}%)</th>
                        <th class="text-end">Tax (${t}%)</th>
                        <th class="text-end">Total</th>
                    </tr>
                    </thead>
                    <tbody>
                    ${rows}
                    </tbody>
                </table>
                </div>
            </div>
            </div>
        </div>`);
    });

    if(!sections.length){
        host.innerHTML = `<div class="muted">No items yet. Add items in the phase tabs to see a breakdown.</div>`;
    }else{
        host.innerHTML = `<div class="accordion" id="bd-accordion">${sections.join('')}</div>`;
    }
    }


    //hook totals
    function hookTotalInputs(){
    const m = document.getElementById('q-markup');
    const t = document.getElementById('q-tax');
    ['input','change'].forEach(ev=>{
        m?.addEventListener(ev, refreshQuoteTotals);
        t?.addEventListener(ev, refreshQuoteTotals);
    });
    }



    // blueprint mock
    document.addEventListener('click', (e)=>{
      if(e.target.id==='q-btn-blueprint'){
        e.preventDefault();
        addRow('demo','Labour',{name:'Demo crew',qty:40,unit:'hrs',unitPrice:220});
        addRow('demo','Material',{name:'Skips & disposal',qty:3,unit:'ea',unitPrice:1450});
        addRow('struct','Material',{name:'Rebar 10mm',qty:120,unit:'m',unitPrice:35});
        addRow('mep','Labour',{name:'Electrician',qty:24,unit:'hrs',unitPrice:280});
      }
    });

    // tab nav
    document.getElementById('q-next').addEventListener('click', ()=> {
      const seq = tabOrder(); const active = document.querySelector('#quoteTabs .nav-link.active')?.dataset.bsTarget;
      const i = seq.indexOf(active); if(i>-1 && i<seq.length-1) gotoTab(seq[i+1]);
    });
    document.getElementById('q-prev').addEventListener('click', ()=> {
      const seq = tabOrder(); const active = document.querySelector('#quoteTabs .nav-link.active')?.dataset.bsTarget;
      const i = seq.indexOf(active); if(i>0) gotoTab(seq[i-1]);
    });

    // add buttons
    document.addEventListener('click', (e)=>{
      if(e.target.classList.contains('q-add')){
        e.preventDefault();
        addRow(e.target.dataset.phase, e.target.dataset.type);
      }
    });

    // open modal
    document.getElementById('btn-new-quote').addEventListener('click', async ()=>{
      // reset state
      PHASES.forEach(p=> (document.querySelector(p.host).innerHTML=''));
      Object.keys(state.items).forEach(k=> state.items[k]=[]);
      document.getElementById('btn-new-quote').addEventListener('click', async ()=>{
      // reset…
      await Promise.all([loadClients(), loadContractors()]);
      gotoTab('#tab-client');
      hookTotalInputs();
      refreshQuoteTotals();

      new bootstrap.Modal(document.getElementById('quoteModal')).show();
      });

    });

    // finalize -> POST to /Quotes/Create
    document.getElementById('q-finalize').addEventListener('click', ()=>{
      // client
      const selId = document.getElementById('q-client-select').value;
      let clientName = '';
      if(selId){
        const c = state.clients.find(x=>x.id===selId); clientName = c ? c.name : '';
      }else{
        clientName = document.getElementById('q-new-name').value.trim();
      }
      if(!clientName){ alert('Please select or enter a client.'); gotoTab('#tab-client'); return; }
      const title = (document.getElementById('q-title').value||`Quote for ${clientName}`).trim();

      // build hidden form fields
      const form = document.getElementById('quote-post-form');
      form.innerHTML = form.innerHTML; // keep antiforgery
      const add = (n,v)=>{ const i=document.createElement('input'); i.type='hidden'; i.name=n; i.value=v; form.appendChild(i); };

      add('ProjectId',''); // new project for now
      add('ClientName', clientName);
      add('Title', title);
      add('MarkupPercent', document.getElementById('q-markup').value||'10');
      add('TaxPercent', document.getElementById('q-tax').value||'15');

      // flatten items
      let idx=0;
      PHASES.forEach(p=>{
        state.items[p.key].forEach(it=>{
          add(`Items[${idx}].ContractorId`, it.contractorId || '');
          add(`Items[${idx}].ContractorName`, it.contractorName || '');
          add(`Items[${idx}].Type`, it.type);
          add(`Items[${idx}].Name`, `${p.name}: ${it.name||''}`);
          add(`Items[${idx}].Qty`,  String(it.qty||0));
          add(`Items[${idx}].Unit`, it.unit||'ea');
          add(`Items[${idx}].UnitPrice`, String(it.unitPrice||0));
          idx++;
        });
      });

      form.submit();
    });
  })();

  // --- KPIs ---
  function refreshKpis(){
    const active = PROJECTS.filter(p => (p.status||'').toLowerCase()!=='completed').length;
    const budget = PROJECTS.reduce((a,b)=>a+(Number(b.budgetPlanned)||0),0);
    document.getElementById('kpi-projects').textContent = active;
    document.getElementById('kpi-budget').textContent = 'R ' + budget.toLocaleString();
    document.getElementById('kpi-contractors').textContent = CONTRACTORS.length;
    const openMaint = MAINT.filter(m => (m.status||'').toLowerCase()!=='assigned').length;
    document.getElementById('kpi-maint').textContent = openMaint;
  }

  // --- Projects: render (cards) ---
  function renderProjects(){
    const q=document.getElementById('global-search').value.trim().toLowerCase();
    let list=PROJECTS.slice();
    if(PROJ_FILTER==='active') list=list.filter(p=>(p.status||'').toLowerCase()!=='completed');
    if(PROJ_FILTER==='at-risk') list=list.filter(p=>['at risk','delayed','on hold'].includes((p.status||'').toLowerCase()));
    if(PROJ_FILTER==='completed') list=list.filter(p=>(p.status||'').toLowerCase()==='completed');
    if(q) list=list.filter(p=>(p.name||'').toLowerCase().includes(q)||(p.description||'').toLowerCase().includes(q));

    const wrap=document.getElementById('projects-cards');
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
    document.getElementById('projects-sub').textContent = `${list.length} shown / ${PROJECTS.length} total`;
  }

  // --- Projects: view-all table ---
  function renderAllProjects(){
    const q=document.getElementById('all-proj-search').value.trim().toLowerCase();
    let list=PROJECTS.slice();
    if(q) list=list.filter(p=>(p.name||'').toLowerCase().includes(q)||(p.description||'').toLowerCase().includes(q));
    const tb=document.getElementById('all-proj-body');
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
    document.getElementById('all-proj-sub').textContent = `${list.length} / ${PROJECTS.length}`;
  }

  // --- Project modal helpers ---
  function openProjectModal(id){
    const p=PROJECTS.find(x=>x.id===id); if(!p) return;
    document.getElementById('proj-id').value = p.id || '';
    document.getElementById('proj-name').value = p.name || '';
    document.getElementById('proj-status').value = p.status || 'In Progress';
    document.getElementById('proj-desc').value = p.description || '';
    document.getElementById('proj-bp').value = p.budgetPlanned ?? '';
    document.getElementById('proj-ba').value = p.budgetActual ?? '';
    document.getElementById('proj-start').value = p.startDatePlanned ? new Date(p.startDatePlanned).toISOString().slice(0,10) : '';
    document.getElementById('proj-endp').value = p.endDatePlanned ? new Date(p.endDatePlanned).toISOString().slice(0,10) : '';
    document.getElementById('proj-enda').value = p.endDateActual ? new Date(p.endDateActual).toISOString().slice(0,10) : '';
    new bootstrap.Modal(document.getElementById('projectModal')).show();
  }
  function collectProjectForm(){
    return {
      id: document.getElementById('proj-id').value,
      name: document.getElementById('proj-name').value,
      status: document.getElementById('proj-status').value,
      description: document.getElementById('proj-desc').value,
      budgetPlanned: Number(document.getElementById('proj-bp').value||0),
      budgetActual : Number(document.getElementById('proj-ba').value||0),
      startDatePlanned: document.getElementById('proj-start').value || null,
      endDatePlanned  : document.getElementById('proj-endp').value || null,
      endDateActual   : document.getElementById('proj-enda').value || null
    };
  }

  // --- Maintenance: render list + view-all ---
  function renderMaint(){
    const listEl=document.getElementById('maint-list');
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
    const q=document.getElementById('maint-search').value?.trim().toLowerCase()||'';
    let list=MAINT.slice();
    if(q) list=list.filter(m=>(m.title||'').toLowerCase().includes(q)||(m.site||'').toLowerCase().includes(q));
    const tb=document.getElementById('maint-body');
    tb.innerHTML = list.map(m=>{
      const p=(m.priority||'').toLowerCase(); const chip = p==='high'?'danger':(p==='medium'?'warn':'ok');
      return `<tr>
        <td>${m.title||'Request'}</td>
        <td>${m.site||'—'}</td>
        <td><span class="chip ${chip}">${m.priority||'—'}</span></td>
        <td>${m.reportedAt?new Date(m.reportedAt).toLocaleString():'—'}</td>
        <td>${(m.status||'').toLowerCase()==='assigned' ? '<span class="chip ok">Assigned</span>' : '<span class="chip warn">Open</span>'}</td>
        <td class="text-end">${(m.status||'').toLowerCase()==='assigned'
          ? '<span class="muted small">—</span>'
          : `<button class="btn btn-sm btn-brand btn-assign" data-mid="${m.id}" data-bs-dismiss="modal">Assign</button>`}</td>
      </tr>`;
    }).join('');
    document.getElementById('maint-sub').textContent = `${list.length} / ${MAINT.length}`;
  }

  // --- Maintenance: assign modal ---
  let ASSIGN_WORKING_ID=null;
  function openAssignModal(mid){
    ASSIGN_WORKING_ID = mid;
    const m = MAINT.find(x=>x.id===mid); if(!m) return;
    document.getElementById('assign-header').innerHTML = `
      <div><strong>${m.title||'Request'}</strong>
      <div class="muted small">${m.site||'—'} • ${m.reportedAt?new Date(m.reportedAt).toLocaleString():'—'}</div>
      <div class="mt-1">${m.description||''}</div></div>`;
    document.getElementById('task-list').innerHTML = '';
    addTaskRow();
    new bootstrap.Modal(document.getElementById('assignModal')).show();
  }
  function addTaskRow(){
    const idx = document.querySelectorAll('#task-list .task-row').length;
    const contractorOptions = CONTRACTORS.map(c=>`<option value="${c.id||c.name}">${c.name} — ${c.specialty||'General'}</option>`).join('');
    const row = document.createElement('div');
    row.className='task-row border rounded p-2 mb-2';
    row.innerHTML = `
      <div class="row g-2 align-items-end">
        <div class="col-md-4">
          <label class="form-label">Task Name</label>
          <input class="form-control task-name" placeholder="e.g. Fix leak in unit 12">
        </div>
        <div class="col-md-3">
          <label class="form-label">Due Date</label>
          <input class="form-control task-due" type="date">
        </div>
        <div class="col-md-3">
          <label class="form-label">Contractor</label>
          <select class="form-select task-ctr">${contractorOptions}</select>
        </div>
        <div class="col-md-2">
          <button class="btn btn-outline-secondary w-100 btn-remove-task"><i class="fas fa-trash"></i></button>
        </div>
      </div>`;
    document.getElementById('task-list').appendChild(row);
  }
  function collectAssignments(){
    return [...document.querySelectorAll('#task-list .task-row')].map(r=>({
      name: r.querySelector('.task-name').value?.trim(),
      dueDate: r.querySelector('.task-due').value || null,
      contractor: r.querySelector('.task-ctr').value || null
    })).filter(t=>t.name);
  }
  function saveAssignments(mid, tasks){
    const all = JSON.parse(localStorage.getItem(ASSIGN_KEY)||'{}');
    all[mid] = { tasks, savedAt: new Date().toISOString() };
    localStorage.setItem(ASSIGN_KEY, JSON.stringify(all));
    // mark request as assigned locally
    const idx=MAINT.findIndex(x=>x.id===mid); if(idx>-1){ MAINT[idx]={...MAINT[idx], status:'Assigned'}; }
    renderMaint(); refreshKpis();
  }

  // --- Data loading ---
  async function loadProjects(){
    const raw = await getJsonSafe(ROOT + 'mock/projects.json', []);
    PROJECTS = raw.map(p=>({
      id: safe(p,'projectId','id'),
      name: safe(p,'name'), description: safe(p,'description'), status: safe(p,'status'),
      budgetPlanned: safe(p,'budgetPlanned'), budgetActual: safe(p,'budgetActual'),
      startDatePlanned: safe(p,'startDatePlanned'), endDatePlanned: safe(p,'endDatePlanned'), endDateActual: safe(p,'endDateActual')
    }));
    PROJECTS = applyDrafts(PROJECTS);
    refreshKpis(); renderProjects();
  }
  async function loadContractors(){
    CONTRACTORS = await getJsonSafe(ROOT + 'mock/contractors.json', []);
    document.getElementById('kpi-contractors').textContent = CONTRACTORS.length;
    const box=document.getElementById('contractors-list');
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
    // Try file; if missing, fallback to sample
    const fallback = [
      { id:'M-1001', title:'Burst pipe at Basement', site:'Sandton Office', priority:'High', reportedAt: new Date().toISOString(), description:'Water leaking near pump room', status:'Open' },
      { id:'M-1002', title:'HVAC not cooling', site:'Cape Town Residential - Block B', priority:'Medium', reportedAt: new Date(Date.now()-3600e3).toISOString(), description:'Unit 4 complaints', status:'Open' },
      { id:'M-1003', title:'Light fittings flicker', site:'Pretoria Commercial', priority:'Low', reportedAt: new Date(Date.now()-86400e3).toISOString(), description:'Lobby LED strip issue', status:'Open' }
    ];
    MAINT = await getJsonSafe(PM.root + 'mock/maintenance.json', fallback);
    renderMaint(); refreshKpis();
  }

  // --- Events ---
   document.addEventListener('input',(e)=>{
    switch(e.target.id){
        case 'global-search':      renderProjects(); break;
        case 'all-proj-search':    renderAllProjects(); break;
        case 'maint-search':       renderAllMaint(); break;
        case 'q-markup':
        case 'q-tax':
        refreshQuoteTotals(); break;
    }
    });
  document.addEventListener('click',(e)=>{
    if(e.target.matches('.proj-filter')){
      document.querySelectorAll('.proj-filter').forEach(b=>b.classList.remove('active'));
      e.target.classList.add('active'); PROJ_FILTER=e.target.dataset.filter; renderProjects();
    }
    if(e.target.matches('.btn-edit')){
      const id=e.target.dataset.pid; openProjectModal(id);
    }
    if(e.target.id==='btn-view-all-projects'){
      renderAllProjects(); new bootstrap.Modal(document.getElementById('allProjectsModal')).show();
    }
    if(e.target.id==='btn-save-proj'){
      const upd=collectProjectForm(); if(!upd.id) return;
      const d=loadDrafts(); d[upd.id]= {...(d[upd.id]||{}), ...upd}; saveDrafts(d);
      const idx=PROJECTS.findIndex(p=>p.id===upd.id); if(idx>-1) PROJECTS[idx]={...PROJECTS[idx], ...upd};
      renderProjects(); refreshKpis();
      bootstrap.Modal.getInstance(document.getElementById('projectModal'))?.hide();
    }
    if(e.target.id==='btn-reset-proj'){
      const id=document.getElementById('proj-id').value; const d=loadDrafts(); delete d[id]; saveDrafts(d);
      const p=PROJECTS.find(x=>x.id===id); if(p){ openProjectModal(id); }
    }
    if(e.target.id==='refresh-btn'){
      Promise.all([loadProjects(), loadContractors(), loadMaintenance()]).then(setUpdated);
    }
    if(e.target.id==='btn-new-project' || e.target.id==='btn-new-project-cta'){
      (new bootstrap.Modal(document.getElementById('newProjectModal'))).show();
    }
    if(e.target.id==='btn-create-proj'){
      const name=document.getElementById('np-name').value.trim();
      const bp=Number(document.getElementById('np-bp').value||0);
      const start=document.getElementById('np-start').value;
      const endp=document.getElementById('np-endp').value;
      if(!name || !bp || !start || !endp){ alert('Please complete required fields.'); return; }
      const id = 'P-' + (Date.now().toString().slice(-6));
      const proj = {
        id,
        name,
        description: document.getElementById('np-desc').value.trim(),
        status: 'On Track',
        budgetPlanned: bp,
        budgetActual: 0,
        startDatePlanned: start,
        endDatePlanned: endp,
        endDateActual: null,
        client: document.getElementById('np-client').value.trim(),
        location: document.getElementById('np-location').value.trim(),
        type: document.getElementById('np-type').value,
        risk: document.getElementById('np-risk').value,
        tags: document.getElementById('np-tags').value
      };
      // store as draft too
      const d=loadDrafts(); d[id]={...proj}; saveDrafts(d);
      PROJECTS.unshift(proj);
      renderProjects(); refreshKpis();
      bootstrap.Modal.getInstance(document.getElementById('newProjectModal'))?.hide();
      document.getElementById('new-proj-form').reset();
    }
    if(e.target.id==='btn-view-all-maint'){
      renderAllMaint(); new bootstrap.Modal(document.getElementById('allMaintModal')).show();
    }
    if(e.target.matches('.btn-assign')){
      const mid=e.target.dataset.mid; openAssignModal(mid);
    }
    if(e.target.id==='btn-add-task'){ addTaskRow(); }
    if(e.target.classList.contains('btn-remove-task')){
      e.target.closest('.task-row')?.remove();
    }
    if(e.target.id==='btn-save-assign'){
      const tasks=collectAssignments(); if(!tasks.length){ alert('Add at least one task.'); return; }
      saveAssignments(ASSIGN_WORKING_ID, tasks);
      bootstrap.Modal.getInstance(document.getElementById('assignModal'))?.hide();
    }
  });

  // --- Bootstrap load ---
  (async function(){
    await Promise.all([loadProjects(), loadContractors(), loadMaintenance()]);
    setUpdated();
  })();
