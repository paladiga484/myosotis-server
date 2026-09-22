namespace Server.Tweak;

/// <summary>
/// The mod menu. One file, no external assets of any kind, served at GET /tweak.
///
/// Presented as a crew manifest rather than a dashboard: you are amending who is aboard and what
/// they carry. Identities are grouped under the sinner they belong to, because a flat list of 184
/// rows is unreadable and the grouping is how the player already thinks about the roster.
/// </summary>
public static class TweakPage
{
    public const string Html = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Mirror &mdash; manifest</title>
<style>
  :root{
    --ink:#0e0d0b;        /* warm near-black, the dossier ground */
    --field:#17150f;
    --raise:#1f1c14;
    --rule:#2c281f;
    --vellum:#e6dfce;     /* aged paper, the reading colour */
    --ash:#8c8475;
    --brass:#b08d3f;      /* owned / active */
    --seal:#8c2f22;       /* wax seal: destructive and primary */
    --moss:#6f8a5c;
    --serif:Georgia,"Iowan Old Style","Palatino Linotype",Palatino,serif;
    --mono:ui-monospace,"SF Mono",Menlo,Consolas,monospace;
  }
  *{box-sizing:border-box}
  html,body{height:100%}
  body{margin:0;background:var(--ink);color:var(--vellum);font:15px/1.5 var(--serif);
       display:grid;grid-template-rows:auto 1fr;overflow:hidden}

  /* ── masthead: the one loud thing on the page ───────────────────────── */
  .masthead{display:flex;align-items:flex-end;gap:22px;padding:14px 22px 10px;
            border-bottom:1px solid var(--rule);background:linear-gradient(#141209,var(--ink))}
  .mark{position:relative;line-height:.84;user-select:none}
  .mark b{display:block;font-weight:400;font-size:30px;letter-spacing:.3em;color:var(--vellum)}
  .mark i{display:block;font-style:normal;font-size:30px;letter-spacing:.3em;color:var(--brass);
          transform:scaleY(-1);transform-origin:top;height:11px;overflow:hidden;opacity:.3;
          -webkit-mask-image:linear-gradient(#000,transparent);mask-image:linear-gradient(#000,transparent)}
  .whoami{flex:1;min-width:0;color:var(--ash);font-size:13px;padding-bottom:4px}
  .whoami b{color:var(--vellum);font-weight:400}
  .acts{display:flex;gap:8px;align-items:center;flex-wrap:wrap;padding-bottom:3px}

  /* ── shell ──────────────────────────────────────────────────────────── */
  .shell{display:grid;grid-template-columns:186px 1fr;min-height:0}
  .index{border-right:1px solid var(--rule);padding:16px 0;overflow:auto;background:#110f0a}
  .index a{display:flex;justify-content:space-between;gap:8px;align-items:baseline;
           padding:9px 20px;color:var(--ash);cursor:pointer;border-left:2px solid transparent}
  .index a:hover{color:var(--vellum)}
  .index a.on{color:var(--vellum);border-left-color:var(--brass);background:#181509}
  .index a em{font-style:normal;font:11px var(--mono);color:var(--ash)}
  .index h6{margin:18px 20px 6px;font-weight:400;font-size:11px;color:#5d5748;letter-spacing:.08em}
  .sheet{overflow:auto;padding:18px 22px 70px;min-height:0}

  /* ── controls ───────────────────────────────────────────────────────── */
  button,select,input{font:inherit;color:var(--vellum)}
  button{background:transparent;border:1px solid var(--rule);padding:5px 12px;cursor:pointer;
         border-radius:2px;transition:border-color .12s,color .12s}
  button:hover{border-color:var(--brass);color:#fff}
  button:focus-visible,select:focus-visible,input:focus-visible,.index a:focus-visible{
    outline:2px solid var(--brass);outline-offset:1px}
  button.go{background:var(--brass);border-color:var(--brass);color:#17150f;font-weight:700}
  button.go:hover{filter:brightness(1.12);border-color:var(--brass);color:#17150f}
  button.raze{background:var(--seal);border-color:var(--seal);color:#ffeae6;font-weight:700}
  button.raze:hover{filter:brightness(1.18);border-color:var(--seal);color:#ffeae6}
  select,input[type=text],input[type=number]{background:var(--field);border:1px solid var(--rule);
         padding:5px 8px;border-radius:2px}
  select{appearance:none;-webkit-appearance:none;padding-right:26px;cursor:pointer;
         background-image:linear-gradient(45deg,transparent 50%,var(--ash) 50%),
                          linear-gradient(135deg,var(--ash) 50%,transparent 50%);
         background-position:calc(100% - 15px) 52%,calc(100% - 10px) 52%;
         background-size:5px 5px,5px 5px;background-repeat:no-repeat}
  select option{background:var(--field);color:var(--vellum)}
  input[type=number]{width:60px;font:13px var(--mono);text-align:right;
         appearance:textfield;-moz-appearance:textfield}
  input[type=number]::-webkit-inner-spin-button,
  input[type=number]::-webkit-outer-spin-button{-webkit-appearance:none;margin:0}
  input[type=checkbox]{accent-color:var(--brass);width:15px;height:15px;cursor:pointer}
  .tools{display:flex;gap:8px;align-items:center;flex-wrap:wrap;margin-bottom:16px;
         padding-bottom:14px;border-bottom:1px solid var(--rule)}
  .tools .gap{flex:1}
  /* keep a bulk-action cluster together so it wraps as one unit instead of shedding
     its trailing label onto a line of its own */
  .bulk{display:flex;gap:8px;align-items:center;flex-wrap:nowrap}
  .quiet{color:var(--ash);font-size:13px}
  .num{font:12px var(--mono);font-variant-numeric:tabular-nums;color:var(--ash)}

  /* ── the manifest itself ────────────────────────────────────────────── */
  .group{margin-bottom:6px}
  .ghead{display:flex;align-items:baseline;gap:10px;padding:10px 4px 6px;
         border-bottom:1px solid var(--rule);position:sticky;top:0;background:var(--ink);z-index:1}
  .ghead h3{margin:0;font-weight:400;font-size:17px;letter-spacing:.02em}
  .ghead .tally{font:12px var(--mono);color:var(--ash)}
  .ghead .gap{flex:1}
  .line{display:grid;grid-template-columns:22px 62px 1fr auto;gap:12px;align-items:center;
        padding:5px 4px;border-bottom:1px solid #1b1811}
  /* item counts reach six digits, so they need more room than a level or an uptie */
  #itemrows input[type=number],#iall{width:86px}
  .line:hover{background:#15130d}
  .line.out .title,.line.out .num{opacity:.38}
  .title{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
  .title s{text-decoration:none;color:var(--ash)}
  .rank{color:var(--brass);font-size:12px}
  .set{display:flex;gap:6px;align-items:center}
  .set span{color:var(--ash);font-size:12px}
  .empty{padding:40px 4px;color:var(--ash)}

  /* ── prose panels ───────────────────────────────────────────────────── */
  .cols{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:26px;
        align-items:start;max-width:1100px}
  .block h2{margin:0 0 4px;font-weight:400;font-size:18px}
  .block p{margin:0 0 12px;color:var(--ash);font-size:13.5px;max-width:62ch}
  .block hr{border:0;border-top:1px solid var(--rule);margin:18px 0}
  .fld{display:flex;gap:10px;align-items:center;padding:5px 0}
  .fld span{flex:1;color:var(--ash);font-size:13.5px}
  .ticks{display:grid;grid-template-columns:repeat(auto-fill,minmax(112px,1fr));gap:2px}
  .tick{display:flex;gap:8px;align-items:center;padding:5px 7px}
  .tick:hover{background:#15130d}
  .hide{display:none}
  #say{font-size:13px;color:var(--ash)}
  #say.ok{color:var(--moss)} #say.err{color:#d98d80}
  #pending{color:var(--brass)}
  @media (prefers-reduced-motion:reduce){*{transition:none!important}}
  @media (max-width:720px){
    .shell{grid-template-columns:1fr;grid-template-rows:auto 1fr}
    .index{display:flex;overflow-x:auto;padding:0;border-right:0;border-bottom:1px solid var(--rule)}
    .index h6{display:none}
    .index a{border-left:0;border-bottom:2px solid transparent;white-space:nowrap}
    .index a.on{border-left:0;border-bottom-color:var(--brass)}
    .line{grid-template-columns:22px 1fr;row-gap:4px}
    .line .num,.line .set{grid-column:2}
  }
</style>
</head>
<body>

<div class="masthead">
  <div class="mark"><b>MIRROR</b><i>MIRROR</i></div>
  <div class="whoami">
    <select id="acct"></select>
    <div id="tally" style="margin-top:5px"></div>
  </div>
  <div class="acts">
    <span id="say"></span>
    <button id="maxAll" title="Own everything, maxed">Max everything</button>
    <button id="clearAll" title="Own nothing">Strip everything</button>
    <button id="reload">Discard changes</button>
    <button id="apply" class="go">Save <span id="pending" class="hide">&bull;</span></button>
  </div>
</div>

<div class="shell">
  <nav class="index" id="index">
    <h6>Manifest</h6>
    <a data-tab="ids" class="on">Identities <em id="n-ids"></em></a>
    <a data-tab="ego">E.G.O. <em id="n-ego"></em></a>
    <a data-tab="items">Inventory <em id="n-items"></em></a>
    <a data-tab="cos">Cosmetics</a>
    <h6>Records</h6>
    <a data-tab="acct">This account</a>
    <a data-tab="seed">New accounts</a>
  </nav>

  <main class="sheet">
    <section id="tab-ids">
      <div class="tools">
        <input type="text" id="search" placeholder="Search name, title or id" style="width:220px">
        <select id="sinnerSel"><option value="">Every sinner</option></select>
        <select id="rank"><option value="">Any rarity</option><option value="1">1 star</option>
          <option value="2">2 star</option><option value="3">3 star</option></select>
        <select id="owned"><option value="">Owned and not</option><option value="y">Owned only</option>
          <option value="n">Missing only</option></select>
        <span class="gap"></span>
        <span class="bulk"><span class="quiet">Everything shown</span>
          <button data-bulk="own">Own</button><button data-bulk="disown">Disown</button></span>
        <span class="bulk"><span class="quiet">level</span>
          <input type="number" id="blvl" min="1" max="60" value="60">
          <span class="quiet">uptie</span><input type="number" id="bupt" min="1" max="4" value="4">
          <button data-bulk="set">Set</button></span>
      </div>
      <div id="idrows"></div>
    </section>

    <section id="tab-ego" class="hide">
      <div class="tools">
        <input type="text" id="esearch" placeholder="Search E.G.O. or id" style="width:220px">
        <span class="gap"></span>
        <span class="bulk"><span class="quiet">Everything shown</span>
          <button data-ebulk="own">Own</button><button data-ebulk="disown">Disown</button></span>
        <span class="bulk"><span class="quiet">uptie</span>
          <input type="number" id="ebupt" min="1" max="4" value="4">
          <button data-ebulk="upt">Set</button></span>
      </div>
      <div id="egorows"></div>
    </section>

    <section id="tab-items" class="hide">
      <div class="tools">
        <input type="text" id="isearch" placeholder="Search item name or id" style="width:220px">
        <span class="gap"></span>
        <span class="quiet">Set everything shown to</span>
        <input type="number" id="iall" min="0" max="999999" value="500">
        <button id="ibulk">Set</button>
      </div>
      <div id="itemrows"></div>
    </section>

    <section id="tab-cos" class="hide">
      <div class="tools">
        <select id="colsel"></select>
        <span class="gap"></span>
        <button id="cown">Own all</button><button id="cdisown">Own none</button>
      </div>
      <div class="ticks" id="colrows"></div>
    </section>

    <section id="tab-acct" class="hide">
      <div class="cols">
        <div class="block">
          <h2>This account</h2>
          <p>Applies to the save you have selected at the top of the page.</p>
          <div class="fld"><span>Account level</span><input type="number" id="ulvl" min="1" max="999"></div>
          <div class="fld"><span>Experience</span><input type="number" id="uexp" min="0"></div>
          <div class="fld"><span>Enkephalin</span><input type="number" id="usta" min="0" max="999"></div>
          <p class="quiet">Changes here save with everything else &mdash; press Save.</p>
        </div>
        <div class="block">
          <h2>Copy another save over this one</h2>
          <p>Replaces every identity, E.G.O., item and cosmetic on the current account with a copy
             from the one you choose. Useful for cloning a set-up you liked onto a second account.</p>
          <div class="fld"><span>Copy from</span><select id="copysrc"></select></div>
          <button id="copybtn">Copy and overwrite</button>
        </div>
        <div class="block">
          <h2>What this page cannot change</h2>
          <p>The battle pass is generated from server constants rather than stored per save, and
             story and theatre unlocks are always fully open.</p>
          <p>Mirror Dungeon progress lives in its own record and is rebuilt when you enter a run,
             so there is nothing to edit here. Shop purchases, gift crafting and starlight are not
             implemented by the server at all.</p>
        </div>
      </div>
    </section>

    <section id="tab-seed" class="hide">
      <div class="cols">
        <div class="block">
          <h2>What a new account starts with</h2>
          <p>Applies the first time a token string logs in. Accounts that already exist are left
             alone &mdash; edit those on the other pages, or reset one below.</p>
          <div class="fld"><span>Roster</span>
            <select id="sprofile">
              <option value="everything">Everything &mdash; all 184 identities, all E.G.O.</option>
              <option value="balanced">Balanced &mdash; all 12 base, a slice of the rest</option>
              <option value="starter">Starter &mdash; the 12 base identities, no E.G.O.</option>
              <option value="empty">Empty &mdash; nothing at all</option>
            </select></div>
          <div class="fld"><span>Inventory</span>
            <select id="sitemprofile">
              <option value="curated">Curated &mdash; what you actually spend</option>
              <option value="all">Everything &mdash; the same count of all 196 items</option>
              <option value="none">Nothing</option>
            </select></div>
          <div class="fld"><span>Account level</span><input type="number" id="sulvl" min="1" max="999"></div>
          <div class="fld"><span>Enkephalin</span><input type="number" id="susta" min="0" max="999"></div>
          <div class="fld"><span>Identity level</span><input type="number" id="splvl" min="1" max="60"></div>
          <div class="fld"><span>Identity uptie</span><input type="number" id="supt" min="1" max="4"></div>
          <div class="fld"><span>E.G.O. uptie</span><input type="number" id="seupt" min="1" max="4"></div>
          <div class="fld"><span>Count per item, when inventory is Everything</span>
            <input type="number" id="sitem" min="0" max="999999"></div>
          <div class="fld"><span>Grant everything on first login</span>
            <input type="checkbox" id="sgrant"></div>
          <hr>
          <button id="seedsave" class="go">Save these defaults</button>
          <p class="quiet" style="margin-top:12px">Written into Config.json, so it survives a
             server restart.</p>
        </div>
        <div class="block">
          <h2>Reset an account</h2>
          <p>Choose the roster and levels on the left, save them, then reset the account you play
             on. It keeps its uid and token; everything it owns is replaced.</p>
          <p><b>Balanced</b> is the one to pick if having everything maxed spoiled it: all twelve
             base identities so every sinner is playable, plus a slice of the rest. Enough to build
             teams with, not enough to trivialise the game.</p>
          <div class="fld"><span>Account to reset</span>
            <select id="rsuid"></select></div>
          <button id="resetacct" class="raze">Reset this account</button>
          <p class="quiet" style="margin-top:12px">This cannot be undone. Other accounts are
             untouched, so keep one as a fallback if you want the maxed roster back.</p>
        </div>
      </div>
    </section>
  </main>
</div>

<script>
const $ = s => document.querySelector(s);
const $$ = s => [...document.querySelectorAll(s)];
let state = null, seed = null, uid = null, curCol = 0, dirty = false;

const LS = { get:(k,d)=>{try{return localStorage.getItem("mirror."+k) ?? d}catch{return d}},
             set:(k,v)=>{try{localStorage.setItem("mirror."+k,v)}catch{}} };
const esc = s => String(s ?? "").replace(/[&<>"]/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;"}[c]));
function say(m, k){ const e = $("#say"); e.textContent = m || ""; e.className = k || ""; }
function markDirty(){ dirty = true; $("#pending").classList.remove("hide"); }
function markClean(){ dirty = false; $("#pending").classList.add("hide"); }
addEventListener("beforeunload", e => { if(dirty){ e.preventDefault(); e.returnValue = ""; } });

/* ── loading ─────────────────────────────────────────────────────────── */
async function loadAccounts(){
  const rs = await fetch("tweak/api/accounts").then(r => r.json());
  if(!rs.length){ say("No accounts yet. Log into the game once, then reload.", "err"); return; }
  const opts = rs.map(u => `<option value="${u.uid}">uid ${u.uid} &mdash; ${esc(u.credential)}</option>`).join("");
  $("#acct").innerHTML = opts; $("#copysrc").innerHTML = opts; $("#rsuid").innerHTML = opts;
  const remembered = +LS.get("uid", 0);
  uid = rs.some(u => u.uid === remembered) ? remembered : rs[0].uid;
  $("#acct").value = uid;
  showTab(LS.get("tab", "ids"));
  seed = await fetch("tweak/api/seed").then(r => r.json());
  renderSeed();
  await load();
}

async function load(){
  say("Loading…");
  const r = await fetch("tweak/api/account/" + uid);
  if(!r.ok){ say("Could not load that account.", "err"); return; }
  state = await r.json();
  $("#ulvl").value = state.user.level;
  $("#uexp").value = state.user.exp;
  $("#usta").value = state.user.stamina;
  $("#colsel").innerHTML = state.collections.map((c,i) => `<option value="${i}">${esc(c.label)}</option>`).join("");
  const names = [...new Set(state.personalities.map(x => x.sinner).filter(Boolean))].sort();
  $("#sinnerSel").innerHTML = `<option value="">Every sinner</option>` +
    names.map(n => `<option value="${esc(n)}">${esc(n)}</option>`).join("");
  render(); erender(); irender(); crender(); markClean();
  say("");
}

function renderSeed(){
  if(!seed) return;
  $("#sgrant").checked = seed.grantEverything;
  $("#sprofile").value = seed.profile || "everything";
  $("#sitemprofile").value = seed.itemProfile || "all";
  $("#sulvl").value = seed.userLevel;   $("#susta").value = seed.stamina;
  $("#splvl").value = seed.personalityLevel; $("#supt").value = seed.personalityUptie;
  $("#seupt").value = seed.egoUptie;    $("#sitem").value = seed.itemCount;
}

/* ── views ───────────────────────────────────────────────────────────── */
const vIds = () => {
  const q = $("#search").value.trim().toLowerCase(), sn = $("#sinnerSel").value,
        rk = $("#rank").value, ow = $("#owned").value;
  return state.personalities.filter(p =>
    (!sn || p.sinner === sn) && (!rk || String(p.rank) === rk) &&
    (!ow || (ow === "y") === !!p.owned) &&
    (!q || (p.sinner+" "+p.title+" "+p.id).toLowerCase().includes(q)));
};
const vEgos  = () => { const q = $("#esearch").value.trim().toLowerCase();
  return state.egos.filter(p => !q || ((p.name||"")+" "+p.id).toLowerCase().includes(q)); };
const vItems = () => { const q = $("#isearch").value.trim().toLowerCase();
  return state.items.filter(p => !q || (String(p.id)+" "+(p.name||"")).toLowerCase().includes(q)); };

function tallies(){
  const io = state.personalities.filter(p=>p.owned).length, it = state.personalities.length;
  const eo = state.egos.filter(p=>p.owned).length, et = state.egos.length;
  const bo = state.items.filter(i=>i.num>0).length;
  $("#n-ids").textContent = io+"/"+it;
  $("#n-ego").textContent = eo+"/"+et;
  $("#n-items").textContent = bo+"/"+state.items.length;
  $("#tally").innerHTML = `<span class="quiet">${io} of ${it} identities &middot; ` +
    `${eo} of ${et} E.G.O. &middot; account level <b>${esc(state.user.level)}</b></span>`;
}

function line(p, kind){
  const sub = kind === "id"
    ? `<span class="title">${esc(p.title||("id "+p.id))}` +
      `${p.rank?` <span class="rank">${"★".repeat(p.rank)}</span>`:""}</span>`
    : `<span class="title">${esc(p.name||("id "+p.id))}</span>`;
  const set = kind === "id"
    ? `<span>lvl</span><input type="number" min="1" max="60" value="${p.level}" data-f="level">
       <span>uptie</span><input type="number" min="1" max="4" value="${p.uptie}" data-f="uptie">
       <span>art</span><input type="number" min="1" max="9" value="${p.illust}" data-f="illust">`
    : `<span>uptie</span><input type="number" min="1" max="4" value="${p.uptie}" data-f="uptie">`;
  return `<div class="line ${p.owned?"":"out"}" data-id="${p.id}">
    <input type="checkbox" ${p.owned?"checked":""} data-f="owned" aria-label="Own ${esc(p.title||p.name||p.id)}">
    <span class="num">${p.id}</span>${sub}<span class="set">${set}</span></div>`;
}

function render(){
  tallies();
  const rows = vIds();
  if(!rows.length){ $("#idrows").innerHTML = `<p class="empty">Nothing matches that. Clear the filters to see the roster.</p>`; return; }
  const byS = new Map();
  rows.forEach(p => { const k = p.sinner || "Unassigned"; (byS.get(k) ?? byS.set(k,[]).get(k)).push(p); });
  $("#idrows").innerHTML = [...byS.entries()].sort((a,b)=>a[0].localeCompare(b[0])).map(([name,list]) => {
    const own = list.filter(p=>p.owned).length;
    return `<div class="group"><div class="ghead"><h3>${esc(name)}</h3>
      <span class="tally">${own}/${list.length}</span><span class="gap"></span>
      <button data-grp="${esc(name)}" data-act="own">Own all</button>
      <button data-grp="${esc(name)}" data-act="disown">Disown</button></div>
      ${list.sort((a,b)=>a.id-b.id).map(p => line(p,"id")).join("")}</div>`;
  }).join("");
}
function erender(){
  const rows = vEgos();
  $("#egorows").innerHTML = rows.length
    ? rows.map(p => line(p,"ego")).join("")
    : `<p class="empty">No E.G.O. matches that.</p>`;
  tallies();
}
function irender(){
  const rows = vItems();
  $("#itemrows").innerHTML = rows.length ? rows.map(p => `
    <div class="line" data-id="${p.id}"><span></span><span class="num">${p.id}</span>
      <span class="title">${p.name ? esc(p.name) : `<s>Item ${p.id}</s>`}</span>
      <span class="set"><input type="number" min="0" max="999999" value="${p.num}" data-f="num"></span>
    </div>`).join("") : `<p class="empty">No item matches that id.</p>`;
}
function crender(){
  const c = state.collections[curCol]; if(!c) return;
  const own = new Set(c.owned);
  $("#colrows").innerHTML = c.all.map(id =>
    `<label class="tick"><input type="checkbox" ${own.has(id)?"checked":""} data-cid="${id}">
     <span class="num">${id}</span></label>`).join("");
}

/* ── editing ─────────────────────────────────────────────────────────── */
function bind(sel, list, after){
  $(sel).addEventListener("input", e => {
    const row = e.target.closest(".line"); if(!row) return;
    const item = list().find(x => x.id === +row.dataset.id); if(!item) return;
    const f = e.target.dataset.f;
    if(f === "owned"){ item.owned = e.target.checked; row.classList.toggle("out", !item.owned); }
    else item[f] = +e.target.value;
    markDirty(); if(after) after();
  });
}
bind("#idrows",   () => state.personalities, tallies);
bind("#egorows",  () => state.egos, tallies);
bind("#itemrows", () => state.items);

$("#idrows").addEventListener("click", e => {
  const b = e.target.closest("[data-grp]"); if(!b) return;
  const want = b.dataset.act === "own";
  state.personalities.filter(p => (p.sinner || "Unassigned") === b.dataset.grp)
                     .forEach(p => p.owned = want);
  markDirty(); render();
});

$("#colrows").addEventListener("input", e => {
  const id = +e.target.dataset.cid; if(!id) return;
  const c = state.collections[curCol], set = new Set(c.owned);
  e.target.checked ? set.add(id) : set.delete(id);
  c.owned = [...set]; markDirty();
});
$("#colsel").onchange  = e => { curCol = +e.target.value; crender(); };
$("#cown").onclick     = () => { state.collections[curCol].owned = [...state.collections[curCol].all]; markDirty(); crender(); };
$("#cdisown").onclick  = () => { state.collections[curCol].owned = []; markDirty(); crender(); };

["#search","#sinnerSel","#rank","#owned"].forEach(s => $(s).addEventListener("input", render));
$("#esearch").oninput = erender;
$("#isearch").oninput = irender;
$("#ibulk").onclick   = () => { const v = +$("#iall").value; vItems().forEach(i => i.num = v); markDirty(); irender(); };

$$("[data-bulk]").forEach(b => b.onclick = () => {
  const k = b.dataset.bulk, lvl = +$("#blvl").value, upt = +$("#bupt").value;
  vIds().forEach(p => {
    if(k === "own") p.owned = true;
    if(k === "disown") p.owned = false;
    if(k === "set"){ p.level = lvl; p.uptie = upt; p.owned = true; }
  });
  markDirty(); render();
});
$$("[data-ebulk]").forEach(b => b.onclick = () => {
  const k = b.dataset.ebulk, upt = +$("#ebupt").value;
  vEgos().forEach(p => {
    if(k === "own") p.owned = true;
    if(k === "disown") p.owned = false;
    if(k === "upt"){ p.uptie = upt; p.owned = true; }
  });
  markDirty(); erender();
});

/* ── navigation ──────────────────────────────────────────────────────── */
function showTab(name){
  $$("#index a").forEach(x => x.classList.toggle("on", x.dataset.tab === name));
  ["ids","ego","items","cos","acct","seed"].forEach(t => $("#tab-"+t).classList.toggle("hide", t !== name));
  LS.set("tab", name);
}
$$("#index a").forEach(a => { a.tabIndex = 0; a.onclick = () => showTab(a.dataset.tab);
  a.onkeydown = e => { if(e.key === "Enter" || e.key === " "){ e.preventDefault(); showTab(a.dataset.tab); } }; });

$("#acct").onchange = e => { uid = +e.target.value; LS.set("uid", uid); load(); };
$("#reload").onclick = () => { markClean(); load(); };

/* ── presets ─────────────────────────────────────────────────────────── */
$("#maxAll").onclick = () => {
  state.personalities.forEach(p => { p.owned = true; p.level = 60; p.uptie = 4; });
  state.egos.forEach(e => { e.owned = true; e.uptie = 4; });
  state.collections.forEach(c => c.owned = [...c.all]);
  markDirty(); render(); erender(); crender();
  say("Staged. Press Save to write it.", "ok");
};
$("#clearAll").onclick = () => {
  state.personalities.forEach(p => p.owned = false);
  state.egos.forEach(e => e.owned = false);
  state.items.forEach(i => i.num = 0);
  state.collections.forEach(c => c.owned = []);
  markDirty(); render(); erender(); irender(); crender();
  say("Staged. Press Save to write it.", "ok");
};

/* ── writes ──────────────────────────────────────────────────────────── */
$("#copybtn").onclick = async () => {
  const src = +$("#copysrc").value;
  if(!src || src === uid){ say("Pick a different account to copy from.", "err"); return; }
  if(!confirm("Overwrite uid " + uid + " with a copy of uid " + src + "?")) return;
  say("Copying…");
  const r = await fetch(`tweak/api/account/${uid}/copy-from/${src}`, {method:"POST"});
  if(r.ok){ markClean(); await load(); say("Copied from uid " + src + ".", "ok"); }
  else say("Copy failed: HTTP " + r.status, "err");
};

$("#resetacct").onclick = async () => {
  const u = +$("#rsuid").value;
  if(!confirm(`Reset account ${u}? Everything it owns is replaced. This cannot be undone.`)) return;
  say("Resetting…");
  const r = await fetch(`tweak/api/account/${u}/reset`, {method:"POST"});
  const j = await r.json().catch(() => ({}));
  say(r.ok ? `Account ${u} reset to "${j.profile}".` : (j.error || "Reset failed."), r.ok ? "ok" : "err");
  if(r.ok && u === uid){ markClean(); await load(); }
};

$("#seedsave").onclick = async () => {
  say("Saving defaults…");
  const body = { grantEverything: $("#sgrant").checked, userLevel: +$("#sulvl").value,
    stamina: +$("#susta").value, personalityLevel: +$("#splvl").value,
    personalityUptie: +$("#supt").value, egoUptie: +$("#seupt").value, itemCount: +$("#sitem").value,
    profile: $("#sprofile").value, itemProfile: $("#sitemprofile").value };
  const r = await fetch("tweak/api/seed", {method:"POST",
    headers:{"Content-Type":"application/json"}, body: JSON.stringify(body)});
  const j = await r.json().catch(() => ({}));
  say(r.ok ? (j.saved === false ? "Applied for this session only." : "Defaults saved.") : "Save failed.", r.ok ? "ok" : "err");
  if(r.ok){ seed = await fetch("tweak/api/seed").then(x => x.json()); renderSeed(); }
};

$("#apply").onclick = async () => {
  if(!state) return;
  say("Saving…");
  const cols = {}; state.collections.forEach(c => cols[c.key] = c.owned);
  const body = {
    user: { level:+$("#ulvl").value, exp:+$("#uexp").value, stamina:+$("#usta").value },
    personalities: state.personalities.map(p => ({id:p.id,owned:p.owned,level:p.level,uptie:p.uptie,illust:p.illust})),
    egos: state.egos.map(p => ({id:p.id,owned:p.owned,uptie:p.uptie})),
    items: state.items.map(i => ({id:i.id,num:i.num})),
    collections: cols
  };
  const r = await fetch("tweak/api/account/" + uid, {method:"POST",
    headers:{"Content-Type":"application/json"}, body: JSON.stringify(body)});
  if(r.ok){ markClean(); await load(); say("Saved. Restart the game or pull to refresh to see it.", "ok"); }
  else say("Save failed: HTTP " + r.status, "err");
};

addEventListener("keydown", e => {
  if((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "s"){ e.preventDefault(); $("#apply").click(); }
  if(e.key === "/" && !/^(INPUT|SELECT|TEXTAREA)$/.test(document.activeElement.tagName)){
    e.preventDefault();
    const box = {ids:"#search", ego:"#esearch", items:"#isearch"}[LS.get("tab","ids")];
    if(box) $(box).focus();
  }
});

loadAccounts();
</script>
</body>
</html>
""";
}
