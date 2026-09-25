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
    --void:#060507;       /* the ground: not black, the absence of the bus lights */
    --hull:#0d0b0e;
    --plate:#141116;
    --seam:#27212a;
    --seam2:#3a3140;
    --bone:#ece4d6;       /* reading colour */
    --ash:#8d8391;
    --smoke:#5b5260;
    --gild:#d7a94a;       /* owned, saved, 000 */
    --gild-dim:#6e5626;
    --blood:#b3202c;      /* the seal: primary and destructive */
    --blood-hi:#e0323f;
    --moss:#7fa46a;
    --serif:"Noto Serif Display","Noto Serif",Georgia,"Palatino Linotype",serif;
    --cond:"DejaVu Sans Condensed","Liberation Sans Narrow","Arial Narrow",sans-serif;
    --mono:"JetBrains Mono","DejaVu Sans Mono",ui-monospace,monospace;
    --s:#8d8391;          /* per-sinner accent, set inline on each card */
  }
  *{box-sizing:border-box}
  html,body{height:100%}
  body{margin:0;color:var(--bone);font:14px/1.5 var(--serif);overflow:hidden;
       display:grid;grid-template-rows:auto 1fr;
       background:
         radial-gradient(1200px 700px at 78% -10%,rgba(179,32,44,.13),transparent 60%),
         radial-gradient(900px 600px at -10% 110%,rgba(215,169,74,.06),transparent 60%),
         var(--void)}
  /* film grain: an inline SVG turbulence, nothing fetched */
  body::after{content:"";position:fixed;inset:0;pointer-events:none;z-index:50;opacity:.06;
    background-image:url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='160' height='160'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='.9' numOctaves='3' stitchTiles='stitch'/></filter><rect width='100%' height='100%' filter='url(%23n)'/></svg>")}

  /* ── masthead ───────────────────────────────────────────────────────── */
  .masthead{position:relative;display:flex;align-items:center;gap:26px;padding:16px 24px 14px;
            border-bottom:1px solid var(--seam);background:rgba(13,11,14,.82);backdrop-filter:blur(6px)}
  .masthead::before{content:"";position:absolute;left:0;right:0;bottom:-1px;height:1px;
            background:linear-gradient(90deg,transparent,var(--blood) 20%,var(--gild) 50%,var(--blood) 80%,transparent);opacity:.55}
  .mark{position:relative;user-select:none;line-height:1;padding-right:6px}
  .mark b{display:block;font:400 34px/1 var(--serif);letter-spacing:.34em;color:var(--bone);
          text-shadow:0 0 18px rgba(179,32,44,.35)}
  /* the reflection: same word, flipped, shattered along two cracks */
  .mark i{display:block;font:400 34px/1 var(--serif);letter-spacing:.34em;color:var(--gild);
          transform:scaleY(-1);height:15px;overflow:hidden;opacity:.28;margin-top:1px;
          clip-path:polygon(0 0,31% 0,34% 100%,0 100%,0 0,38% 0,63% 0,58% 100%,40% 100%,66% 0,100% 0,100% 100%,62% 100%);
          -webkit-mask-image:linear-gradient(#000,transparent 90%);mask-image:linear-gradient(#000,transparent 90%)}
  .mark svg{position:absolute;left:0;top:0;width:100%;height:100%;pointer-events:none;opacity:.55}
  .mark .glint{position:absolute;top:0;bottom:16px;width:30%;left:-40%;pointer-events:none;
          background:linear-gradient(100deg,transparent,rgba(236,228,214,.18),transparent);
          animation:glint 7s ease-in-out infinite}
  @keyframes glint{0%,70%{left:-40%}100%{left:110%}}
  .whoami{flex:1;min-width:0;color:var(--ash);font-size:13px}
  .whoami .label{font:600 10px var(--cond);letter-spacing:.28em;color:var(--smoke);text-transform:uppercase}
  .whoami b{color:var(--gild);font-weight:400}
  .acts{display:flex;gap:8px;align-items:center;flex-wrap:wrap}

  /* ── shell ──────────────────────────────────────────────────────────── */
  .shell{display:grid;grid-template-columns:214px 1fr;min-height:0}
  .index{border-right:1px solid var(--seam);padding:14px 0;overflow:auto;background:rgba(13,11,14,.7)}
  .index a{position:relative;display:grid;grid-template-columns:24px 1fr auto;gap:6px;align-items:baseline;
           padding:10px 18px 14px;color:var(--ash);cursor:pointer;border-left:2px solid transparent;
           font:500 13px var(--cond);letter-spacing:.14em;text-transform:uppercase}
  .index a .no{font:400 12px var(--serif);color:var(--smoke);letter-spacing:0}
  .index a:hover{color:var(--bone);background:rgba(255,255,255,.015)}
  .index a.on{color:var(--bone);border-left-color:var(--blood);background:linear-gradient(90deg,rgba(179,32,44,.14),transparent)}
  .index a.on .no{color:var(--blood-hi)}
  .index a em{font:400 11px var(--mono);font-style:normal;color:var(--smoke);letter-spacing:0;text-transform:none}
  .index a .bar{position:absolute;left:48px;right:18px;bottom:7px;height:2px;background:var(--seam)}
  .index a .bar i{display:block;height:100%;width:0;background:var(--gild);transition:width .4s}
  .index h6{margin:20px 18px 6px;font:600 10px var(--cond);color:var(--smoke);letter-spacing:.3em;text-transform:uppercase}
  .sheet{overflow:auto;padding:20px 26px 90px;min-height:0;scroll-behavior:smooth}

  /* ── controls ───────────────────────────────────────────────────────── */
  button,select,input{font:inherit;color:var(--bone)}
  button{background:rgba(20,17,22,.8);border:1px solid var(--seam2);padding:6px 14px;cursor:pointer;
         font:600 11px var(--cond);letter-spacing:.16em;text-transform:uppercase;
         clip-path:polygon(6px 0,100% 0,100% calc(100% - 6px),calc(100% - 6px) 100%,0 100%,0 6px);
         transition:border-color .12s,color .12s,background .12s}
  button:hover{border-color:var(--gild);color:#fff;background:rgba(215,169,74,.08)}
  button:focus-visible,select:focus-visible,input:focus-visible,.index a:focus-visible{
    outline:1px solid var(--gild);outline-offset:2px}
  button.go{background:var(--gild);border-color:var(--gild);color:#140f06}
  button.go:hover{background:#e8bd5e;color:#140f06}
  button.go.dirty{animation:beat 1.6s ease-in-out infinite}
  @keyframes beat{0%,100%{box-shadow:0 0 0 0 rgba(215,169,74,.0)}50%{box-shadow:0 0 22px 2px rgba(215,169,74,.35)}}
  button.raze{background:var(--blood);border-color:var(--blood);color:#ffe9ea}
  button.raze:hover{background:var(--blood-hi);color:#fff}
  select,input[type=text],input[type=number]{background:var(--hull);border:1px solid var(--seam2);
         padding:5px 8px;border-radius:0}
  select{appearance:none;-webkit-appearance:none;padding-right:26px;cursor:pointer;
         background-image:linear-gradient(45deg,transparent 50%,var(--ash) 50%),
                          linear-gradient(135deg,var(--ash) 50%,transparent 50%);
         background-position:calc(100% - 15px) 52%,calc(100% - 10px) 52%;
         background-size:5px 5px,5px 5px;background-repeat:no-repeat}
  select option{background:var(--hull);color:var(--bone)}
  input[type=text]{min-width:0}
  input[type=number]{width:58px;font:12px var(--mono);text-align:right;
         appearance:textfield;-moz-appearance:textfield}
  input[type=number]::-webkit-inner-spin-button,
  input[type=number]::-webkit-outer-spin-button{-webkit-appearance:none;margin:0}
  /* ownership is a seal: an empty frame, or pressed gold */
  input[type=checkbox]{appearance:none;-webkit-appearance:none;width:16px;height:16px;margin:0;cursor:pointer;
         border:1px solid var(--seam2);background:var(--hull);display:inline-grid;place-content:center;
         transform:rotate(45deg);transition:background .12s,border-color .12s}
  input[type=checkbox]:hover{border-color:var(--gild)}
  input[type=checkbox]:checked{background:var(--gild);border-color:var(--gild);
         box-shadow:0 0 10px rgba(215,169,74,.35)}
  .tools{display:flex;gap:8px;align-items:center;flex-wrap:wrap;margin-bottom:18px;
         padding:12px 14px;border:1px solid var(--seam);background:rgba(13,11,14,.7)}
  .tools .gap{flex:1}
  .bulk{display:flex;gap:8px;align-items:center;flex-wrap:nowrap}
  .quiet{color:var(--ash);font-size:12.5px}
  .num{font:11px var(--mono);font-variant-numeric:tabular-nums;color:var(--smoke)}

  /* ── the manifest: one plate per sinner ─────────────────────────────── */
  .group{position:relative;margin-bottom:18px;border:1px solid var(--seam);background:rgba(13,11,14,.72)}
  .group::before{content:"";position:absolute;left:0;top:0;bottom:0;width:3px;background:var(--s)}
  .ghead{display:flex;align-items:center;gap:14px;padding:12px 14px 10px 18px;
         border-bottom:1px solid var(--seam);position:sticky;top:-20px;z-index:2;
         background:linear-gradient(90deg,color-mix(in srgb,var(--s) 14%,var(--hull)),var(--hull) 60%)}
  .ghead .sno{font:400 34px/0.9 var(--serif);color:transparent;-webkit-text-stroke:1px var(--s);
         min-width:48px;letter-spacing:-.02em}
  .ghead h3{margin:0;font:600 17px var(--cond);letter-spacing:.18em;text-transform:uppercase}
  .ghead .tally{font:11px var(--mono);color:var(--ash)}
  .ghead .meter{width:120px;height:3px;background:var(--seam);margin-top:5px}
  .ghead .meter i{display:block;height:100%;background:var(--s)}
  .ghead .gap{flex:1}
  .line{display:grid;grid-template-columns:22px 52px 44px 1fr auto;gap:12px;align-items:center;
        padding:6px 14px 6px 18px;border-bottom:1px solid rgba(39,33,42,.6);transition:background .1s}
  #itemrows .line{grid-template-columns:22px 52px 1fr auto}
  #egorows .line{grid-template-columns:22px 52px 1fr auto}
  #itemrows input[type=number],#iall{width:86px}
  .line:last-child{border-bottom:0}
  .line:hover{background:rgba(255,255,255,.025)}
  .line.out .title,.line.out .num,.line.out .rar{opacity:.33}
  .title{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
  .title s{text-decoration:none;color:var(--ash)}
  /* rarity the way the game writes it: 0, 00, 000 */
  .rar{font:700 12px var(--mono);letter-spacing:.06em;text-align:center;padding:1px 0;border:1px solid}
  .rar.r1{color:var(--ash);border-color:var(--seam2)}
  .rar.r2{color:var(--blood-hi);border-color:rgba(179,32,44,.55)}
  .rar.r3{color:var(--gild);border-color:var(--gild-dim);box-shadow:inset 0 0 8px rgba(215,169,74,.15)}
  .set{display:flex;gap:6px;align-items:center}
  .set span{color:var(--smoke);font:600 10px var(--cond);letter-spacing:.14em;text-transform:uppercase}
  .empty{padding:50px 4px;color:var(--ash);font-style:italic;text-align:center}

  /* ── prose panels ───────────────────────────────────────────────────── */
  .cols{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:22px;
        align-items:start;max-width:1150px}
  .block{border:1px solid var(--seam);background:rgba(13,11,14,.72);padding:18px 20px}
  .block h2{margin:0 0 6px;font:600 15px var(--cond);letter-spacing:.2em;text-transform:uppercase;color:var(--gild)}
  .block p{margin:0 0 12px;color:var(--ash);font-size:13.5px;max-width:62ch}
  .block hr{border:0;border-top:1px solid var(--seam);margin:18px 0}
  .block.danger{border-color:rgba(179,32,44,.45)}
  .block.danger h2{color:var(--blood-hi)}
  .fld{display:flex;gap:10px;align-items:center;padding:6px 0;border-bottom:1px dotted var(--seam)}
  .fld span{flex:1;color:var(--ash);font-size:13.5px}
  .ticks{display:grid;grid-template-columns:repeat(auto-fill,minmax(112px,1fr));gap:2px}
  .tick{display:flex;gap:10px;align-items:center;padding:7px 9px;border:1px solid transparent}
  .tick:hover{border-color:var(--seam)}
  .hide{display:none}

  /* ── the toast ──────────────────────────────────────────────────────── */
  #say{position:fixed;right:22px;bottom:22px;z-index:60;max-width:420px;padding:10px 16px;
       font:13px var(--serif);background:var(--plate);border:1px solid var(--seam2);border-left:3px solid var(--ash);
       opacity:0;transform:translateY(8px);transition:opacity .2s,transform .2s;pointer-events:none}
  #say.show{opacity:1;transform:none}
  #say.ok{border-left-color:var(--moss)} #say.err{border-left-color:var(--blood-hi);color:#f1c2c5}
  #pending{color:#140f06}
  ::-webkit-scrollbar{width:10px;height:10px}
  ::-webkit-scrollbar-track{background:transparent}
  ::-webkit-scrollbar-thumb{background:var(--seam);border:2px solid var(--void)}
  ::-webkit-scrollbar-thumb:hover{background:var(--seam2)}
  ::selection{background:rgba(179,32,44,.45)}
  @media (prefers-reduced-motion:reduce){*{transition:none!important;animation:none!important}}
  @media (max-width:760px){
    .shell{grid-template-columns:1fr;grid-template-rows:auto 1fr}
    .index{display:flex;overflow-x:auto;padding:0;border-right:0;border-bottom:1px solid var(--seam)}
    .index h6,.index a .bar{display:none}
    .index a{border-left:0;border-bottom:2px solid transparent;white-space:nowrap}
    .index a.on{border-left:0;border-bottom-color:var(--blood)}
    .line{grid-template-columns:22px 44px 1fr;row-gap:4px}
    .line .num{display:none}
    .line .set{grid-column:2 / -1}
  }
</style>
</head>
<body>

<div class="masthead">
  <div class="mark"><b>MIRROR</b><i>MIRROR</i>
    <svg viewBox="0 0 220 50" preserveAspectRatio="none" aria-hidden="true">
      <path d="M71 0 L78 19 L69 31 L80 50 M78 19 L96 24 M140 0 L133 14 L146 27 L138 50 M133 14 L118 11"
            fill="none" stroke="#ece4d6" stroke-width=".7"/>
    </svg><span class="glint"></span></div>
  <div class="whoami">
    <div class="label">Manifest of</div>
    <select id="acct"></select>
    <div id="tally" style="margin-top:6px"></div>
  </div>
  <div class="acts">
    <button id="maxAll" title="Own everything, maxed">Max everything</button>
    <button id="clearAll" title="Own nothing">Strip everything</button>
    <button id="reload">Discard changes</button>
    <button id="apply" class="go">Save <span id="pending" class="hide">&bull;</span></button>
  </div>
</div>

<div id="say" role="status" aria-live="polite"></div>

<div class="shell">
  <nav class="index" id="index">
    <h6>Manifest</h6>
    <a data-tab="ids" class="on"><span class="no">I</span>Identities <em id="n-ids"></em><span class="bar"><i id="b-ids"></i></span></a>
    <a data-tab="ego"><span class="no">II</span>E.G.O. <em id="n-ego"></em><span class="bar"><i id="b-ego"></i></span></a>
    <a data-tab="items"><span class="no">III</span>Inventory <em id="n-items"></em><span class="bar"><i id="b-items"></i></span></a>
    <a data-tab="cos"><span class="no">IV</span>Cosmetics <em></em></a>
    <h6>Records</h6>
    <a data-tab="acct"><span class="no">V</span>This account <em></em></a>
    <a data-tab="seed"><span class="no">VI</span>New accounts <em></em></a>
  </nav>

  <main class="sheet">
    <section id="tab-ids">
      <div class="tools">
        <input type="text" id="search" placeholder="Search name, title or id" style="width:220px">
        <select id="sinnerSel"><option value="">Every sinner</option></select>
        <select id="rank"><option value="">Any rarity</option><option value="1">0</option>
          <option value="2">00</option><option value="3">000</option></select>
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
        <div class="block danger">
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
let sayTimer = 0;
function say(m, k){
  const e = $("#say"); clearTimeout(sayTimer);
  if(!m){ e.className = k || ""; return; }
  e.textContent = m; e.className = (k || "") + " show";
  if(k !== "err") sayTimer = setTimeout(() => e.classList.remove("show"), m.endsWith("…") ? 15000 : 3200);
}
function markDirty(){ dirty = true; $("#pending").classList.remove("hide"); $("#apply").classList.add("dirty"); }
function markClean(){ dirty = false; $("#pending").classList.add("hide"); $("#apply").classList.remove("dirty"); }

/* the twelve, in bus order, each with the colour the game dresses them in */
const SINNERS = {1:["Yi Sang","#9fb8d4"],2:["Faust","#e8a8b8"],3:["Don Quixote","#f2d24b"],
  4:["Ryōshū","#d8342f"],5:["Meursault","#4a72c4"],6:["Hong Lu","#3fb8a8"],7:["Heathcliff","#7d5cb8"],
  8:["Ishmael","#f08a24"],9:["Rodion","#b82c38"],10:["Sinclair","#b8c83c"],11:["Outis","#4f8a5a"],
  12:["Gregor","#9a6a44"]};
const sinnerNo = id => Math.floor(id / 100) % 100;
const pad2 = n => String(n).padStart(2, "0");
const RAR = r => r ? `<span class="rar r${r}">${"0".repeat(r)}</span>` : `<span></span>`;
function plate(no, name, list, own, controls){
  const [, col] = SINNERS[no] || [name, "#8d8391"];
  const pct = list.length ? Math.round(100 * own / list.length) : 0;
  return `<div class="group" style="--s:${col}"><div class="ghead">
      <span class="sno">${no ? pad2(no) : "··"}</span>
      <div><h3>${esc(name)}</h3><div class="meter"><i style="width:${pct}%"></i></div></div>
      <span class="tally">${own}/${list.length}</span><span class="gap"></span>${controls}</div>`;
}
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
  const pc = (a,b) => (b ? 100*a/b : 0) + "%";
  $("#b-ids").style.width = pc(io,it); $("#b-ego").style.width = pc(eo,et);
  $("#b-items").style.width = pc(bo,state.items.length);
  $("#tally").innerHTML = `<span class="quiet"><b>${io}</b> of ${it} identities &middot; ` +
    `<b>${eo}</b> of ${et} E.G.O. &middot; level <b>${esc(state.user.level)}</b></span>`;
}

function line(p, kind){
  const sub = kind === "id"
    ? RAR(p.rank) + `<span class="title">${esc(p.title||("id "+p.id))}</span>`
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
  if(!rows.length){ $("#idrows").innerHTML = `<p class="empty">Nobody answers to that. Clear the filters to see the whole bus.</p>`; return; }
  const byS = new Map();
  rows.forEach(p => { const k = p.sinner || "Unassigned"; (byS.get(k) ?? byS.set(k,[]).get(k)).push(p); });
  $("#idrows").innerHTML = [...byS.entries()]
    .sort((a,b) => (sinnerNo(a[1][0].id) || 99) - (sinnerNo(b[1][0].id) || 99) || a[0].localeCompare(b[0]))
    .map(([name,list]) => {
      const own = list.filter(p=>p.owned).length;
      return plate(sinnerNo(list[0].id), name, list, own,
        `<button data-grp="${esc(name)}" data-act="own">Own all</button>
         <button data-grp="${esc(name)}" data-act="disown">Disown</button>`) +
        list.sort((a,b)=>(b.rank-a.rank)||(a.id-b.id)).map(p => line(p,"id")).join("") + `</div>`;
    }).join("");
}
function erender(){
  const rows = vEgos();
  if(!rows.length){ $("#egorows").innerHTML = `<p class="empty">No E.G.O. answers to that.</p>`; tallies(); return; }
  const byS = new Map();
  rows.forEach(p => { const n = sinnerNo(p.id); (byS.get(n) ?? byS.set(n,[]).get(n)).push(p); });
  $("#egorows").innerHTML = [...byS.entries()].sort((a,b)=>a[0]-b[0]).map(([no,list]) => {
    const own = list.filter(p=>p.owned).length;
    return plate(no, (SINNERS[no]||["Unassigned"])[0], list, own, "") +
      list.sort((a,b)=>a.id-b.id).map(p => line(p,"ego")).join("") + `</div>`;
  }).join("");
  tallies();
}
function irender(){
  const rows = vItems();
  $("#itemrows").innerHTML = rows.length ? rows.map(p => `
    <div class="line ${p.num>0?"":"out"}" data-id="${p.id}"><span></span><span class="num">${p.id}</span>
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
