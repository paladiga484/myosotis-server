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
<title>Limbus mod manager</title>
<style>
""" + Look.Tokens + """
  :root{--s:var(--muted)}   /* a sinner's colour, set on each group */
  /* Quiet and clean: flat colours from the chosen theme (black, white, a colour or a picture),
     the system's own font, one accent. theme.js-style code below sets the variables. */
  *{box-sizing:border-box}
  html,body{height:100%}
  body{margin:0;display:grid;grid-template-columns:220px 1fr;overflow:hidden;font-variant-numeric:tabular-nums}
  body.picture .drawer,body.picture .appbar,body.picture .group,body.picture .block,body.picture .tools{
       background:color-mix(in srgb,var(--bg) 86%,transparent)}
  svg{width:18px;height:18px;fill:currentColor;flex:none}
  .hide{display:none!important}
  a{color:var(--accent)}

  /* navigation */
  .drawer{display:flex;flex-direction:column;gap:1px;padding:16px 10px;border-right:1px solid var(--line);
          overflow:auto;min-height:0}
  .drawer .dhead{margin:16px 10px 4px;font:600 12px/16px var(--font);color:var(--muted)}
  .drawer .dhead:first-child{margin-top:0}
  .drawer a{display:flex;align-items:center;gap:10px;height:34px;padding:0 10px;border-radius:6px;
            color:var(--fg);font:400 14px/20px var(--font);cursor:pointer;text-decoration:none}
  .drawer a svg{display:none}
  .drawer a span{flex:1}
  .drawer a em{font:400 12px/16px var(--font);font-style:normal;color:var(--muted)}
  .drawer a:hover{background:var(--hover)}
  .drawer a.on{background:color-mix(in srgb,var(--accent) 18%,transparent);color:var(--fg)}
  .drawer hr{border:0;border-top:1px solid var(--line);margin:12px 10px}
  .drawer .foot{margin-top:auto;display:flex;align-items:center;gap:10px;padding:16px 10px 0}
  .drawer .foot img{width:28px;height:28px;border-radius:6px;display:block}
  .drawer .foot div{display:flex;flex-direction:column;font:600 13px/18px var(--font)}
  .drawer .foot small{font:400 12px/16px var(--font);color:var(--muted)}

  /* top bar */
  .main{display:flex;flex-direction:column;min-height:0;min-width:0}
  .appbar{display:flex;align-items:center;gap:16px 24px;flex-wrap:wrap;padding:16px 28px;border-bottom:1px solid var(--line)}
  .brand{flex:1;min-width:220px}
  .mark{margin:0;font:600 18px/24px var(--font)}
  .mark span{display:none}
  .support{margin:2px 0 0;color:var(--muted);font:400 13px/18px var(--font)}
  .support b{color:var(--fg);font-weight:600}
  .account{display:flex;align-items:center;gap:12px}
  .acts{display:flex;gap:6px;flex-wrap:wrap}
  .sheet{flex:1;overflow:auto;padding:20px 28px 96px;min-height:0}

  /* buttons */
  button{font:500 14px/20px var(--font);color:inherit;cursor:pointer;border:0;background:none}
  .btn{height:32px;padding:0 14px;border-radius:6px;display:inline-flex;align-items:center;gap:8px;white-space:nowrap;
       border:1px solid var(--line);background:var(--panel)}
  .btn:hover{background:color-mix(in srgb,var(--fg) 10%,var(--bg))}
  .btn.filled{background:var(--accent);border-color:var(--accent);color:var(--on-accent)}
  .btn.filled:hover{background:color-mix(in srgb,var(--accent) 88%,var(--fg))}
  .btn.tonal,.btn.outlined{background:var(--panel)}
  .btn.text{border-color:transparent;background:none;padding:0 10px;color:var(--fg)}
  .btn.text:hover{background:var(--hover)}
  .btn.danger{border-color:var(--danger);color:var(--danger);background:none}
  .btn.danger:hover{background:color-mix(in srgb,var(--danger) 12%,transparent)}
  .btn.small{height:28px;padding:0 10px}
  button:focus-visible,select:focus-visible,input:focus-visible,.drawer a:focus-visible,.fab:focus-visible{
    outline:2px solid var(--accent);outline-offset:2px}

  /* save: only there when something is unsaved */
  .fab{position:fixed;right:24px;bottom:20px;z-index:20;height:36px;padding:0 16px;border-radius:6px;
       display:none;align-items:center;gap:8px;background:var(--accent);color:var(--on-accent)}
  .fab.dirty{display:inline-flex}
  .fab #pending{display:none}

  /* fields */
  select,input{font:inherit;color:var(--fg)}
  select,input[type=text],input[type=number]{height:32px;background:var(--bg);border:1px solid var(--line);
         border-radius:6px;padding:0 10px}
  select:hover,input[type=text]:hover,input[type=number]:hover{border-color:var(--muted)}
  select:focus,input[type=text]:focus,input[type=number]:focus{border-color:var(--accent);outline:none}
  select{appearance:none;-webkit-appearance:none;padding-right:28px;cursor:pointer;
         background:var(--bg) url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='%23888' d='M7 10l5 5 5-5z'/></svg>") no-repeat right 6px center/18px}
  select option{background:var(--bg);color:var(--fg)}
  .field{display:flex;flex-direction:column;gap:4px;font:400 12px/16px var(--font);color:var(--muted)}
  .field select{min-width:240px}
  .search{position:relative;flex:1 1 240px;max-width:380px}
  .search svg{position:absolute;left:9px;top:7px;color:var(--muted)}
  .search input{width:100%;padding-left:32px}
  input[type=number]{width:64px;height:28px;padding:0 8px;text-align:right;appearance:textfield;-moz-appearance:textfield}
  input[type=number]::-webkit-inner-spin-button,input[type=number]::-webkit-outer-spin-button{-webkit-appearance:none;margin:0}
  input[type=checkbox]{appearance:none;-webkit-appearance:none;width:16px;height:16px;margin:0;cursor:pointer;
         border:1px solid var(--muted);border-radius:4px;display:inline-block;flex:none;background:var(--bg)}
  input[type=checkbox]:checked{background:var(--accent);border-color:var(--accent);
         box-shadow:inset 0 0 0 3px var(--bg)}
  input.switch{width:36px;height:20px;border-radius:10px;position:relative;background:var(--line);border:0;box-shadow:none}
  input.switch::after{content:"";position:absolute;top:3px;left:3px;width:14px;height:14px;border-radius:7px;
         background:var(--fg);transition:transform .12s}
  input.switch:checked{background:var(--accent);box-shadow:none}
  input.switch:checked::after{transform:translateX(16px);background:var(--on-accent)}

  /* toolbar above each list */
  .tools{display:flex;gap:10px 12px;align-items:center;flex-wrap:wrap;margin:0 0 16px}
  .tools .gap{flex-basis:100%;height:0}
  .bulk{display:flex;gap:6px;align-items:center}
  .quiet{color:var(--muted)}
  .num{color:var(--muted);font:400 12px/16px var(--font)}

  /* one group per sinner */
  .group{border:1px solid var(--line);border-radius:8px;margin-bottom:16px;overflow:hidden}
  .ghead{display:flex;align-items:center;gap:12px;padding:10px 12px 10px 14px;position:sticky;top:-20px;z-index:2;
         background:var(--bg);border-bottom:1px solid var(--line)}
  .avatar{width:22px;height:22px;border-radius:4px;display:grid;place-items:center;flex:none;
          background:var(--s);color:#000;font:600 11px/1 var(--font)}
  .ghead .who{min-width:0}
  .ghead h3{margin:0;font:600 14px/20px var(--font)}
  .meter{width:120px;height:3px;border-radius:2px;background:var(--line);margin-top:4px;overflow:hidden}
  .meter i{display:block;height:100%;background:var(--s)}
  .tally{color:var(--muted);white-space:nowrap}
  .ghead .gap{flex:1}
  .line{display:grid;grid-template-columns:20px 48px 40px minmax(0,1fr) auto;gap:12px;align-items:center;
        min-height:40px;padding:4px 12px 4px 14px;border-top:1px solid var(--line)}
  .ghead + .line{border-top:0}
  #itemrows .line{grid-template-columns:20px 64px minmax(0,1fr) auto}
  #egorows .line{grid-template-columns:20px 64px minmax(0,1fr) auto}
  #itemrows input[type=number],#iall{width:96px}
  .group > .line:first-child{border-top:0}
  .line:hover{background:var(--hover)}
  .line.out .title,.line.out .num,.line.out .rar{opacity:.45}
  .title{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
  .title s{text-decoration:none;color:var(--muted)}
  .rar{height:20px;border-radius:4px;display:grid;place-items:center;font:600 11px/1 var(--font);letter-spacing:.06em;
       border:1px solid var(--line);color:var(--muted)}
  .rar.r2{color:color-mix(in srgb,#e5484d 80%,var(--fg));border-color:color-mix(in srgb,#e5484d 45%,var(--bg))}
  .rar.r3{color:color-mix(in srgb,#d9a521 70%,var(--fg));border-color:color-mix(in srgb,#d9a521 50%,var(--bg))}
  .set{display:flex;gap:6px;align-items:center}
  .set span{color:var(--muted);font:400 12px/16px var(--font)}
  .empty{padding:48px 4px;color:var(--muted);text-align:center}

  /* account and settings pages */
  .cols{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:16px;align-items:start;max-width:1100px}
  .block{border:1px solid var(--line);border-radius:8px;padding:16px 18px 18px}
  .block h2{margin:0 0 6px;font:600 15px/22px var(--font)}
  .block p{margin:0 0 10px;color:var(--muted);max-width:62ch}
  .block p b{color:var(--fg);font-weight:600}
  .block hr{border:0;border-top:1px solid var(--line);margin:16px 0}
  .block.danger{border-color:color-mix(in srgb,var(--danger) 50%,var(--line))}
  .fld{display:flex;gap:12px;align-items:center;min-height:44px;border-bottom:1px solid var(--line)}
  .fld:last-of-type{border-bottom:0}
  .fld span{flex:1}
  .fld select{max-width:280px}
  .block .btn{margin-top:14px}
  .ticks{display:flex;flex-wrap:wrap;gap:6px}
  .tick{display:inline-flex;gap:8px;align-items:center;height:30px;padding:0 10px 0 8px;border-radius:6px;
        border:1px solid var(--line);cursor:pointer}
  .tick:has(input:checked){border-color:var(--accent)}
  .tick .num{color:inherit}
  .swatches{display:flex;gap:8px;flex-wrap:wrap;margin:6px 0 4px}
  .swatch{display:inline-flex;align-items:center;gap:8px;height:32px;padding:0 12px 0 8px;border-radius:6px;
          border:1px solid var(--line);cursor:pointer}
  .swatch i{width:16px;height:16px;border-radius:4px;border:1px solid var(--line);display:block}
  .swatch:has(input:checked){border-color:var(--accent);box-shadow:inset 0 0 0 1px var(--accent)}
  .swatch input{position:absolute;opacity:0;pointer-events:none}
  input[type=color]{width:36px;height:28px;padding:0;border:1px solid var(--line);border-radius:6px;background:none;cursor:pointer}

  /* snackbar and dialog */
  #say{position:fixed;left:50%;bottom:20px;z-index:30;min-width:240px;max-width:520px;padding:10px 14px;
       border-radius:6px;background:var(--fg);color:var(--bg);opacity:0;transform:translateX(-50%);
       transition:opacity .15s;pointer-events:none}
  #say.show{opacity:1}
  #say.err{background:var(--danger);color:#fff}
  dialog{border:1px solid var(--line);border-radius:8px;padding:18px 20px;max-width:420px;background:var(--bg);color:var(--fg)}
  dialog::backdrop{background:rgba(0,0,0,.5)}
  dialog h2{margin:0 0 8px;font:600 15px/22px var(--font)}
  dialog p{margin:0 0 18px;color:var(--muted)}
  dialog .row{display:flex;justify-content:flex-end;gap:6px}
  dialog.danger [value=yes]{color:var(--danger)}

  /* the Black Forest keeps its own dark, whatever the theme */
  #tab-forest{position:relative;min-height:70vh;border-radius:8px;overflow:hidden;background:#000;color:#e8e2d6;
    display:grid;place-items:center;text-align:center}
  #tab-forest .door{background:none;border:0;padding:0;cursor:pointer}
  #tab-forest .door img{max-height:58vh;max-width:100%;display:block}
  #tab-forest .door-says{margin:14px 0 0;color:#b9ad97}
  #tab-forest .within{position:absolute;inset:0;background:#000 center/cover no-repeat;display:flex;
    flex-direction:column;justify-content:flex-end;align-items:center;padding:28px}
  #tab-forest .within > *{background:rgba(0,0,0,.72);margin:0;padding:6px 12px}
  #tab-forest .within h2{font:600 20px/28px var(--font)}
  #tab-forest .within p{color:#cdbfa6;max-width:60ch}
  #tab-forest .within .btn{margin-top:12px;color:#e8e2d6;border-color:#555;background:#111}
  .drawer a.forest{color:#e5484d}

  ::-webkit-scrollbar{width:10px;height:10px}
  ::-webkit-scrollbar-thumb{background:var(--line);border:2px solid var(--bg);border-radius:5px}
  ::selection{background:color-mix(in srgb,var(--accent) 40%,transparent)}
  @media (prefers-reduced-motion:reduce){*{transition:none!important}}
  @media (max-width:900px){
    body{grid-template-columns:1fr;grid-template-rows:auto 1fr}
    .drawer{flex-direction:row;overflow-x:auto;padding:8px;gap:4px;border-right:0;border-bottom:1px solid var(--line)}
    .drawer .dhead,.drawer hr,.drawer .foot{display:none}
    .drawer a{white-space:nowrap;flex:none}
    .appbar,.sheet{padding-left:16px;padding-right:16px}
    #idrows .line{grid-template-columns:20px 40px minmax(0,1fr);row-gap:4px}
    #idrows .line .num{display:none}
    #idrows .line .set{grid-column:2 / -1}
    #itemrows .line,#egorows .line{grid-template-columns:20px minmax(0,1fr) auto}
    #itemrows .line .num,#egorows .line .num{display:none}
  }
</style>
</head>
<body>

<nav class="drawer" id="index" aria-label="Sections">
  <p class="dhead">Roster</p>
  <a data-tab="ids" class="on"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 12.75c1.63 0 3.07.39 4.24.9 1.08.48 1.76 1.56 1.76 2.73V18H6v-1.61c0-1.18.68-2.26 1.76-2.73 1.17-.52 2.61-.91 4.24-.91zM4 13c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm1.13 1.1c-.37-.06-.74-.1-1.13-.1-.99 0-1.93.21-2.78.58C.48 14.9 0 15.62 0 16.43V18h4.5v-1.61c0-.83.23-1.61.63-2.29zM20 13c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm4 3.43c0-.81-.48-1.53-1.22-1.85-.85-.37-1.79-.58-2.78-.58-.39 0-.76.04-1.13.1.4.68.63 1.46.63 2.29V18H24v-1.57zM12 6c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3z"/></svg><span>Identities</span><em id="n-ids"></em></a>
  <a data-tab="ego"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M19.48 12.35c-1.57-4.08-7.16-4.3-5.81-10.23.1-.44-.37-.78-.75-.55C9.29 3.71 6.68 8 8.87 13.62c.18.46-.36.89-.75.59-1.81-1.37-2-3.34-1.84-4.75.06-.52-.62-.77-.91-.34C4.69 10.16 4 11.84 4 14.37c.38 5.6 5.11 7.32 6.81 7.54 2.43.31 5.06-.14 6.95-1.87 2.08-1.93 2.84-5.01 1.72-7.69zm-9.28 5.03c1.44-.35 2.18-1.39 2.38-2.31.33-1.43-.96-2.83-.09-5.09.33 1.87 3.27 3.04 3.27 5.08.08 2.53-2.66 4.7-5.56 2.32z"/></svg><span>E.G.O.</span><em id="n-ego"></em></a>
  <a data-tab="items"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 2H4c-1 0-2 .9-2 2v3.01c0 .72.43 1.34 1 1.69V20c0 1.1 1.1 2 2 2h14c.9 0 2-.9 2-2V8.7c.57-.35 1-.97 1-1.69V4c0-1.1-1-2-2-2zm-5 12H9v-2h6v2zm5-7H4V4l16-.02V7z"/></svg><span>Inventory</span><em id="n-items"></em></a>
  <a data-tab="cos"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9c.83 0 1.5-.67 1.5-1.5 0-.39-.15-.74-.39-1.01-.23-.26-.38-.61-.38-.99 0-.83.67-1.5 1.5-1.5H16c2.76 0 5-2.24 5-5 0-4.42-4.03-8-9-8zm-5.5 9c-.83 0-1.5-.67-1.5-1.5S5.67 9 6.5 9 8 9.67 8 10.5 7.33 12 6.5 12zm3-4C8.67 8 8 7.33 8 6.5S8.67 5 9.5 5s1.5.67 1.5 1.5S10.33 8 9.5 8zm5 0c-.83 0-1.5-.67-1.5-1.5S13.67 5 14.5 5s1.5.67 1.5 1.5S15.33 8 14.5 8zm3 4c-.83 0-1.5-.67-1.5-1.5S16.67 9 17.5 9s1.5.67 1.5 1.5-.67 1.5-1.5 1.5z"/></svg><span>Cosmetics</span><em></em></a>
  <hr>
  <p class="dhead">Accounts</p>
  <a data-tab="acct"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg><span>This account</span><em></em></a>
  <a data-tab="forest" class="forest hide" id="forestlink"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 2C7 2 4 6 4 10v12h4v-6a4 4 0 0 1 8 0v6h4V10c0-4-3-8-8-8zm0 6a2 2 0 1 1 0 4 2 2 0 0 1 0-4z"/></svg><span>The Black Forest</span><em></em></a>
  <a data-tab="look"><span>Appearance</span><em></em></a>
  <a data-tab="seed"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M15 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-9-2V7H4v3H1v2h3v3h2v-3h3v-2H6zm9 4c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg><span>New accounts</span><em></em></a>
  <div class="foot"><img id="appicon" src="tweak/icon" alt="" width="40" height="40">
    <div>Mirror<small>Limbus private server</small></div></div>
</nav>

<div class="main">
  <header class="appbar">
    <div class="brand">
      <h1 class="mark">Mirror</h1>
      <p class="support" id="tally"></p>
    </div>
    <div class="account"><label class="field">Account<select id="acct"></select></label></div>
    <div class="acts">
      <button id="maxAll" class="btn tonal" title="Own everything, levelled and uptied to the top">Max everything</button>
      <button id="clearAll" class="btn text" title="Own nothing">Strip everything</button>
      <button id="reload" class="btn text">Discard changes</button>
    </div>
  </header>

  <main class="sheet">
    <section id="tab-ids">
      <div class="tools">
        <label class="search"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M15.5 14h-.79l-.28-.27A6.47 6.47 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/></svg>
          <input type="text" id="search" placeholder="Search name, title or id" aria-label="Search identities"></label>
        <select id="sinnerSel" aria-label="Sinner"><option value="">Every sinner</option></select>
        <select id="rank" aria-label="Rarity"><option value="">Any rarity</option><option value="1">0</option>
          <option value="2">00</option><option value="3">000</option></select>
        <select id="owned" aria-label="Owned"><option value="">Owned and not</option><option value="y">Owned only</option>
          <option value="n">Missing only</option></select>
        <span class="gap"></span>
        <span class="bulk"><span class="quiet">Everything shown</span>
          <button class="btn small tonal" data-bulk="own">Own</button><button class="btn small text" data-bulk="disown">Disown</button></span>
        <span class="bulk"><span class="quiet">Level</span>
          <input type="number" id="blvl" min="1" max="60" value="60" aria-label="Level">
          <span class="quiet">Uptie</span><input type="number" id="bupt" min="1" max="4" value="4" aria-label="Uptie">
          <button class="btn small tonal" data-bulk="set">Set</button></span>
      </div>
      <div id="idrows"></div>
    </section>

    <section id="tab-ego" class="hide">
      <div class="tools">
        <label class="search"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M15.5 14h-.79l-.28-.27A6.47 6.47 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/></svg>
          <input type="text" id="esearch" placeholder="Search E.G.O. or id" aria-label="Search E.G.O."></label>
        <span class="gap"></span>
        <span class="bulk"><span class="quiet">Everything shown</span>
          <button class="btn small tonal" data-ebulk="own">Own</button><button class="btn small text" data-ebulk="disown">Disown</button></span>
        <span class="bulk"><span class="quiet">Uptie</span>
          <input type="number" id="ebupt" min="1" max="4" value="4" aria-label="Uptie">
          <button class="btn small tonal" data-ebulk="upt">Set</button></span>
      </div>
      <div id="egorows"></div>
    </section>

    <section id="tab-items" class="hide">
      <div class="tools">
        <label class="search"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M15.5 14h-.79l-.28-.27A6.47 6.47 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/></svg>
          <input type="text" id="isearch" placeholder="Search item name or id" aria-label="Search items"></label>
        <span class="gap"></span>
        <span class="quiet">Set everything shown to</span>
        <input type="number" id="iall" min="0" max="999999" value="500" aria-label="Count">
        <button class="btn small tonal" id="ibulk">Set</button>
      </div>
      <div class="group" id="itemrows"></div>
    </section>

    <section id="tab-cos" class="hide">
      <div class="tools">
        <select id="colsel" aria-label="Collection"></select>
        <span class="gap"></span>
        <button class="btn small tonal" id="cown">Own all</button><button class="btn small text" id="cdisown">Own none</button>
      </div>
      <div class="ticks" id="colrows"></div>
    </section>

    <section id="tab-acct" class="hide">
      <div class="cols">
        <div class="block">
          <h2>This account</h2>
          <p>Applies to the account chosen at the top of the page.</p>
          <div class="fld"><span>Account level</span><input type="number" id="ulvl" min="1" max="999"></div>
          <div class="fld"><span>Experience</span><input type="number" id="uexp" min="0" style="width:96px"></div>
          <div class="fld"><span>Enkephalin</span><input type="number" id="usta" min="0" max="999"></div>
          <p class="quiet" style="margin-top:16px">These save with everything else when you press Save changes.</p>
        </div>
        <div class="block">
          <h2>Copy another save over this one</h2>
          <p>Replaces every identity, E.G.O., item and cosmetic on this account with a copy of the
             one you choose. Handy for cloning a set-up onto a second account.</p>
          <div class="fld"><span>Copy from</span><select id="copysrc"></select></div>
          <button class="btn outlined" id="copybtn">Copy and overwrite</button>
        </div>
        <div class="block">
          <h2>What this page can't change</h2>
          <p>The battle pass comes from server constants rather than the save, and story and
             theatre unlocks are always fully open.</p>
          <p>Mirror Dungeon progress lives in its own record and is rebuilt when you enter a run.
             Shop purchases, gift crafting and starlight aren't implemented by the server at all.</p>
        </div>
      </div>
    </section>

    <section id="tab-forest" class="hide" aria-label="The Black Forest">
      <div id="forestdoor">
        <button class="door" id="enterforest" title="Enter"><img src="tweak/blackforest/gate" alt="A gate of black trees around a burning eye"></button>
        <p class="door-says">Something waits past the trees. Open the gate.</p>
      </div>
      <div class="within hide" id="forestwithin" style="background-image:url(tweak/blackforest/backdrop)">
        <h2>The Black Forest</h2>
        <p>Greencliff fell, and took everyone with him. Gebura and the green sinners will wait here,
           once they exist.</p>
        <button class="btn tonal" id="sealforest">Seal the gate</button>
      </div>
      <audio id="forestcry" src="tweak/blackforest/cry" preload="none"></audio>
    </section>

    <section id="tab-look" class="hide">
      <div class="cols">
        <div class="block">
          <h2>Appearance</h2>
          <p>For this mod menu and the app window. Saved on this computer as soon as you change it.</p>
          <div class="fld"><span>Background</span></div>
          <div class="swatches" role="radiogroup" aria-label="Background">
            <label class="swatch"><input type="radio" name="lookbg" value="black"><i style="background:#000"></i>Black</label>
            <label class="swatch"><input type="radio" name="lookbg" value="white"><i style="background:#fff"></i>White</label>
            <label class="swatch"><input type="radio" name="lookbg" value="colour"><i id="lookcolourchip"></i>Colour</label>
            <label class="swatch"><input type="radio" name="lookbg" value="picture"><i id="lookpicchip"></i>Picture</label>
          </div>
          <div class="fld" id="lookcolourrow"><span>Background colour</span><input type="color" id="lookcolour"></div>
          <div class="fld" id="lookpicrow"><span>Picture <span class="quiet" id="lookpicname"></span></span>
            <select id="looktone" aria-label="Text over the picture"><option value="dark">Light text</option>
              <option value="light">Dark text</option></select>
            <button class="btn small" id="lookpick">Choose a picture</button>
            <input type="file" id="lookfile" accept="image/png,image/jpeg,image/webp" class="hide"></div>
          <div class="fld"><span>Accent colour</span><input type="color" id="lookaccent"></div>
        </div>
      </div>
    </section>

    <section id="tab-seed" class="hide">
      <div class="cols">
        <div class="block">
          <h2>What a new account starts with</h2>
          <p>Applies the first time a new token logs in. Existing accounts are left alone: edit
             them on the other pages, or reset one here.</p>
          <div class="fld"><span>Roster</span>
            <select id="sprofile">
              <option value="everything">Everything: all 184 identities, all E.G.O.</option>
              <option value="balanced">Balanced: all 12 base, a slice of the rest</option>
              <option value="starter">Starter: the 12 base identities, no E.G.O.</option>
              <option value="empty">Empty: nothing at all</option>
            </select></div>
          <div class="fld"><span>Inventory</span>
            <select id="sitemprofile">
              <option value="curated">Curated: what you actually spend</option>
              <option value="all">Everything: the same count of all 196 items</option>
              <option value="none">Nothing</option>
            </select></div>
          <div class="fld"><span>Account level</span><input type="number" id="sulvl" min="1" max="999"></div>
          <div class="fld"><span>Enkephalin</span><input type="number" id="susta" min="0" max="999"></div>
          <div class="fld"><span>Identity level</span><input type="number" id="splvl" min="1" max="60"></div>
          <div class="fld"><span>Identity uptie</span><input type="number" id="supt" min="1" max="4"></div>
          <div class="fld"><span>E.G.O. uptie</span><input type="number" id="seupt" min="1" max="4"></div>
          <div class="fld"><span>Count of each item when the inventory is Everything</span>
            <input type="number" id="sitem" min="0" max="999999" style="width:96px"></div>
          <div class="fld"><span>Grant everything on first login</span>
            <input type="checkbox" class="switch" id="sgrant" role="switch"></div>
          <button class="btn filled" id="seedsave">Save these defaults</button>
          <p class="quiet" style="margin-top:12px">Written to Config.json, so they survive a server restart.</p>
        </div>
        <div class="block danger">
          <h2>Reset an account</h2>
          <p>Pick the roster and levels on the left, save them, then reset the account you play on.
             It keeps its uid and token; everything it owns is replaced.</p>
          <p><b>Balanced</b> is the one to pick if having everything maxed spoiled the game: all
             twelve base identities so every sinner is playable, plus a slice of the rest.</p>
          <div class="fld"><span>Account to reset</span><select id="rsuid"></select></div>
          <button class="btn danger" id="resetacct">Reset this account</button>
          <p class="quiet" style="margin-top:12px">This can't be undone. Other accounts are left
             as they are, so keep one around if you want the maxed roster back.</p>
        </div>
      </div>
    </section>
  </main>
</div>

<button id="apply" class="fab" title="Save changes (Ctrl+S)"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z"/></svg><span>Save changes</span><i id="pending" class="hide"></i></button>
<div id="say" role="status" aria-live="polite"></div>
<dialog id="ask"><h2></h2><p></p><div class="row">
  <button class="btn text" value="no">Cancel</button><button class="btn text" value="yes"></button></div></dialog>

<script>
""" + Look.Script + """
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
      <span class="avatar">${no ? pad2(no) : "?"}</span>
      <div class="who"><h3>${esc(name)}</h3><div class="meter"><i style="width:${pct}%"></i></div></div>
      <span class="tally">${own} of ${list.length}</span><span class="gap"></span>${controls}</div>`;
}
addEventListener("beforeunload", e => { if(dirty){ e.preventDefault(); e.returnValue = ""; } });
function ask(title, text, yes, danger){
  const d = $("#ask");
  d.querySelector("h2").textContent = title;
  d.querySelector("p").textContent = text;
  d.querySelector("[value=yes]").textContent = yes;
  d.classList.toggle("danger", !!danger);
  return new Promise(done => {
    d.onclose = () => done(d.returnValue === "yes");
    d.querySelectorAll("button").forEach(b => b.onclick = () => d.close(b.value));
    d.returnValue = ""; d.showModal();
  });
}
/* every write says it comes from this page: a web page elsewhere can't add the header */
const post = (url, body) => fetch(url, {method:"POST",
  headers: body === undefined ? {"X-Mirror":"1"} : {"X-Mirror":"1","Content-Type":"application/json"},
  body: body === undefined ? undefined : JSON.stringify(body)});
$("#appicon").addEventListener("error", e => e.target.remove());

/* ── loading ─────────────────────────────────────────────────────────── */
async function loadAccounts(){
  const rs = await fetch("tweak/api/accounts").then(r => r.json());
  if(!rs.length){ say("No accounts yet. Log into the game once, then reload.", "err"); return; }
  const opts = rs.map(u => `<option value="${u.uid}">uid ${u.uid} (${esc(u.credential)})</option>`).join("");
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
  await forestState();
  say("");
}

/* ── the Black Forest ────────────────────────────────────────────────── */
async function forestState(){
  const f = await fetch(`tweak/api/account/${uid}/blackforest`).then(x => x.json()).catch(() => ({open:false}));
  $("#forestlink").classList.toggle("hide", !f.open);
  if(!f.open && LS.get("tab","ids") === "forest") showTab("ids");
  $("#forestdoor").classList.remove("hide");
  $("#forestwithin").classList.add("hide");
}
$("#enterforest").onclick = () => {
  $("#forestdoor").classList.add("hide");
  $("#forestwithin").classList.remove("hide");
  const cry = $("#forestcry"); cry.currentTime = 0; cry.play().catch(() => {});
};
$("#sealforest").onclick = async () => {
  if(!await ask("Seal the gate?", "The Black Forest closes for this account until Greencliff falls again.", "Seal")) return;
  const r = await post(`tweak/api/account/${uid}/blackforest/seal`);
  say(r.ok ? "The gate is sealed." : "Could not seal it.", r.ok ? "ok" : "err");
  await forestState(); showTab("ids");
};

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
  $("#tally").innerHTML = `<b>${io}</b> of ${it} identities, <b>${eo}</b> of ${et} E.G.O., ` +
    `account level <b>${esc(state.user.level)}</b>`;
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
        `<button class="btn small text" data-grp="${esc(name)}" data-act="own">Own all</button>
         <button class="btn small text" data-grp="${esc(name)}" data-act="disown">Disown</button>`) +
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
  ["ids","ego","items","cos","acct","seed","forest","look"].forEach(t => $("#tab-"+t).classList.toggle("hide", t !== name));
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
  if(!await ask("Copy over this account?", `Everything uid ${uid} owns is replaced with a copy of uid ${src}.`, "Copy and overwrite")) return;
  say("Copying…");
  const r = await post(`tweak/api/account/${uid}/copy-from/${src}`);
  if(r.ok){ markClean(); await load(); say("Copied from uid " + src + ".", "ok"); }
  else say("Copy failed: HTTP " + r.status, "err");
};

$("#resetacct").onclick = async () => {
  const u = +$("#rsuid").value;
  if(!await ask("Reset this account?", `Everything uid ${u} owns is replaced. This can't be undone.`, "Reset", true)) return;
  say("Resetting…");
  const r = await post(`tweak/api/account/${u}/reset`);
  const j = await r.json().catch(() => ({}));
  say(r.ok ? `Account ${u} reset to ${j.profile}.` : (j.error || "Reset failed."), r.ok ? "ok" : "err");
  if(r.ok && u === uid){ markClean(); await load(); }
};

$("#seedsave").onclick = async () => {
  say("Saving defaults…");
  const body = { grantEverything: $("#sgrant").checked, userLevel: +$("#sulvl").value,
    stamina: +$("#susta").value, personalityLevel: +$("#splvl").value,
    personalityUptie: +$("#supt").value, egoUptie: +$("#seupt").value, itemCount: +$("#sitem").value,
    profile: $("#sprofile").value, itemProfile: $("#sitemprofile").value };
  const r = await post("tweak/api/seed", body);
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
  const r = await post("tweak/api/account/" + uid, body);
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

/* ── appearance ──────────────────────────────────────────────────────── */
function renderLook(){
  if(!theme) return;
  $$("[name=lookbg]").forEach(r => r.checked = r.value === theme.bg);
  $("#lookcolour").value = theme.colour; $("#lookaccent").value = theme.accent; $("#looktone").value = theme.tone;
  $("#lookcolourchip").style.background = theme.colour;
  $("#lookpicchip").style.background = theme.picture ? `center/cover url("/tweak/theme/picture?v=${theme.picture}")` : "var(--line)";
  $("#lookcolourrow").classList.toggle("hide", theme.bg !== "colour");
  $("#lookpicrow").classList.toggle("hide", theme.bg !== "picture");
  $("#lookpicname").textContent = theme.picture ? "" : "(none chosen yet)";
}
async function saveLook(t){
  applyTheme(t); renderLook();
  const r = await post("tweak/api/theme", t);
  if(!r.ok) say("Couldn't save the look.", "err");
}
$$("[name=lookbg]").forEach(r => r.onchange = () => {
  if(r.value === "picture" && !theme.picture){ renderLook(); $("#lookpicrow").classList.remove("hide"); $("#lookfile").click(); return; }
  saveLook({...theme, bg: r.value});
});
$("#lookcolour").oninput = e => saveLook({...theme, bg: "colour", colour: e.target.value});
$("#lookaccent").oninput = e => saveLook({...theme, accent: e.target.value});
$("#looktone").onchange = e => saveLook({...theme, tone: e.target.value});
$("#lookpick").onclick = () => $("#lookfile").click();
$("#lookfile").onchange = async e => {
  const f = e.target.files[0]; e.target.value = "";
  if(!f) return;
  if(f.size > 8 << 20){ say("Pictures can be up to 8 MB.", "err"); return; }
  const r = await fetch("tweak/api/theme/picture", {method:"POST", headers:{"X-Mirror":"1"}, body:f});
  const j = await r.json().catch(() => ({}));
  if(!r.ok){ say(j.error || "Couldn't use that picture.", "err"); renderLook(); return; }
  applyTheme(j); renderLook(); say("Picture set.", "ok");
};

loadTheme().then(renderLook);
if(/^#(ids|ego|items|cos|acct|seed|look)$/.test(location.hash)) showTab(location.hash.slice(1));
loadAccounts();
</script>
</body>
</html>
""";
}
