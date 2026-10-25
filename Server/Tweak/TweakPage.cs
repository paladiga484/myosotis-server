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
  :root{
    color-scheme:dark;
    /* Material 3 colour roles, tuned to the bus at night: brass fittings on umber */
    --surface:#15120e;
    --container-low:#1c1813;
    --container:#211c16;
    --container-high:#2b251e;
    --container-highest:#363028;
    --on-surface:#ede3d1;
    --on-surface-variant:#cbbfac;
    --outline:#958a79;
    --outline-variant:#4b4337;
    --primary:#e8bf6a;
    --on-primary:#3d2c00;
    --primary-container:#5a4300;
    --on-primary-container:#ffdea1;
    --secondary-container:#4f4430;
    --on-secondary-container:#f3e0bd;
    --error:#ffb4a9;
    --error-container:#93231d;
    --on-error-container:#ffdad5;
    --inverse-surface:#ede3d1;
    --inverse-on-surface:#2b251e;
    --display:"Noto Serif Display","Noto Serif",Georgia,serif;
    --sans:"Noto Sans","DejaVu Sans",system-ui,sans-serif;
    --s:var(--outline);   /* a sinner's colour, set on each card */
  }
  *{box-sizing:border-box}
  html,body{height:100%}
  body{margin:0;background:var(--surface);color:var(--on-surface);font:400 14px/20px var(--sans);
       display:grid;grid-template-columns:256px 1fr;overflow:hidden;
       font-variant-numeric:tabular-nums}
  svg{width:24px;height:24px;fill:currentColor;flex:none}
  .hide{display:none!important}
  /* the Black Forest: no brass here */
  #tab-forest{position:relative;min-height:70vh;border-radius:12px;overflow:hidden;background:#050304;
    display:grid;place-items:center;text-align:center}
  #tab-forest .door{background:none;border:0;padding:0;cursor:pointer;filter:drop-shadow(0 0 24px #b3121244);
    transition:filter .6s,transform .6s}
  #tab-forest .door:hover,#tab-forest .door:focus-visible{filter:drop-shadow(0 0 48px #e01b1baa);transform:scale(1.02)}
  #tab-forest .door img{max-height:58vh;max-width:100%;display:block}
  #tab-forest .door-says{margin:16px 0 0;color:#c9b48a;font:italic 400 16px/24px var(--display)}
  #tab-forest .within{position:absolute;inset:0;background:#000 center/cover no-repeat;display:flex;
    flex-direction:column;justify-content:flex-end;padding:32px;animation:fall 2.4s ease-out}
  #tab-forest .within::before{content:"";position:absolute;inset:0;background:linear-gradient(transparent 30%,#000d 85%)}
  #tab-forest .within > *{position:relative}
  #tab-forest .within h2{margin:0;color:#e9d9b0;font:400 32px/40px var(--display);letter-spacing:.04em}
  #tab-forest .within p{color:#cbb995;max-width:60ch;margin:8px auto}
  #tab-forest .within .btn{margin:16px auto 0}
  @keyframes fall{from{filter:brightness(0) blur(6px)}to{filter:none}}
  .drawer a.forest{color:#d14b4b}
  @media (prefers-reduced-motion:reduce){#tab-forest .within{animation:none}#tab-forest .door{transition:none}}

  /* navigation drawer */
  .drawer{display:flex;flex-direction:column;gap:2px;padding:12px;background:var(--container-low);
          overflow:auto;min-height:0}
  .drawer .dhead{margin:18px 16px 6px;font:500 14px/20px var(--sans);color:var(--on-surface-variant)}
  .drawer .dhead:first-child{margin-top:8px}
  .drawer a{display:flex;align-items:center;gap:12px;height:56px;padding:0 24px 0 16px;border-radius:28px;
            color:var(--on-surface-variant);font:500 14px/20px var(--sans);cursor:pointer;
            text-decoration:none;position:relative}
  .drawer a span{flex:1}
  .drawer a em{font:500 12px/16px var(--sans);font-style:normal;color:var(--on-surface-variant)}
  .drawer a:hover{background:rgba(237,227,209,.08)}
  .drawer a.on{background:var(--secondary-container);color:var(--on-secondary-container)}
  .drawer a.on em{color:var(--on-secondary-container)}
  .drawer hr{border:0;border-top:1px solid var(--outline-variant);margin:12px 16px}
  .drawer .foot{margin-top:auto;display:flex;align-items:center;gap:12px;padding:20px 16px 8px}
  .drawer .foot img{width:40px;height:40px;border-radius:10px;display:block}
  .drawer .foot div{display:flex;flex-direction:column;font:500 14px/20px var(--sans)}
  .drawer .foot small{font:400 12px/16px var(--sans);color:var(--on-surface-variant)}

  /* top bar: the one loud thing on the page is the cracked mirror */
  .main{display:flex;flex-direction:column;min-height:0;min-width:0}
  .appbar{display:flex;align-items:flex-end;gap:24px;flex-wrap:wrap;padding:20px 32px 16px}
  .brand{flex:1;min-width:240px}
  .mark{position:relative;margin:0;font:300 57px/1 var(--display);letter-spacing:.01em;color:var(--on-surface)}
  /* its reflection: the lower half of the word, flipped under the baseline and split by two cracks */
  .mark span{position:absolute;left:0;right:0;top:100%;height:24px;overflow:hidden;color:var(--primary);
             transform:scaleY(-1);opacity:.2;pointer-events:none;
             -webkit-mask-image:linear-gradient(to top,#000,transparent 90%);
             mask-image:linear-gradient(to top,#000,transparent 90%);
             clip-path:polygon(0 0,31% 0,28% 100%,0 100%,0 0,36% 0,55% 0,51% 100%,33% 100%,59% 0,100% 0,100% 100%,55% 100%)}
  .mark span i{position:absolute;left:0;bottom:0;font-style:normal;line-height:1;white-space:nowrap}
  .support{margin:30px 0 0;color:var(--on-surface-variant);font:400 14px/20px var(--sans)}
  .support b{color:var(--on-surface);font-weight:500}
  .account{display:flex;align-items:center;gap:12px}
  .acts{display:flex;gap:8px;flex-wrap:wrap}
  .sheet{flex:1;overflow:auto;padding:4px 32px 120px;min-height:0;scroll-behavior:smooth}

  /* buttons */
  button{font:500 14px/20px var(--sans);color:inherit;cursor:pointer;border:0;background:none}
  .btn{height:40px;padding:0 24px;border-radius:20px;display:inline-flex;align-items:center;gap:8px;
       position:relative;overflow:hidden;white-space:nowrap}
  .btn::after{content:"";position:absolute;inset:0;background:currentColor;opacity:0;transition:opacity .15s}
  .btn:hover::after{opacity:.08} .btn:active::after{opacity:.12}
  .btn.filled{background:var(--primary);color:var(--on-primary)}
  .btn.tonal{background:var(--secondary-container);color:var(--on-secondary-container)}
  .btn.outlined{border:1px solid var(--outline);color:var(--primary)}
  .btn.text{padding:0 12px;color:var(--primary)}
  .btn.danger{background:var(--error-container);color:var(--on-error-container)}
  .btn.small{height:32px;padding:0 12px;border-radius:16px}
  button:focus-visible,select:focus-visible,input:focus-visible,.drawer a:focus-visible,.fab:focus-visible{
    outline:2px solid var(--primary);outline-offset:2px}

  /* the save button: quiet while there's nothing to save, brass when there is */
  .fab{position:fixed;right:24px;bottom:24px;z-index:20;height:56px;padding:0 20px 0 16px;border-radius:16px;
       display:inline-flex;align-items:center;gap:12px;background:var(--container-highest);
       color:var(--on-surface-variant);box-shadow:0 2px 6px rgba(0,0,0,.35);
       transition:background .2s,color .2s,box-shadow .2s}
  .fab:hover{box-shadow:0 4px 12px rgba(0,0,0,.45)}
  .fab.dirty{background:var(--primary);color:var(--on-primary)}
  .fab #pending{position:absolute;top:10px;left:34px;width:8px;height:8px;border-radius:4px;
                background:var(--error-container);box-shadow:0 0 0 2px var(--primary)}

  /* fields */
  select,input{font:inherit;color:var(--on-surface)}
  select,input[type=text],input[type=number]{height:40px;background:transparent;border:1px solid var(--outline);
         border-radius:4px;padding:0 12px}
  select:hover,input[type=text]:hover,input[type=number]:hover{border-color:var(--on-surface)}
  select:focus,input[type=text]:focus,input[type=number]:focus{border-color:var(--primary);
         box-shadow:inset 0 0 0 1px var(--primary);outline:none}
  select{appearance:none;-webkit-appearance:none;padding-right:36px;cursor:pointer;
         background:url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='%23cbbfac' d='M7 10l5 5 5-5z'/></svg>") no-repeat right 8px center/20px}
  select option{background:var(--container-high);color:var(--on-surface)}
  .field{display:flex;flex-direction:column;gap:4px;font:500 12px/16px var(--sans);color:var(--on-surface-variant)}
  .field select{min-width:260px}
  .search{position:relative;flex:1 1 260px;max-width:420px}
  .search svg{position:absolute;left:12px;top:8px;color:var(--on-surface-variant)}
  .search input{width:100%;height:40px;border-radius:20px;padding-left:44px;background:var(--container-high);
                border-color:transparent}
  input[type=number]{width:64px;height:32px;padding:0 8px;text-align:right;appearance:textfield;-moz-appearance:textfield}
  input[type=number]::-webkit-inner-spin-button,input[type=number]::-webkit-outer-spin-button{-webkit-appearance:none;margin:0}
  input[type=checkbox]{appearance:none;-webkit-appearance:none;width:18px;height:18px;margin:0;cursor:pointer;
         border:2px solid var(--on-surface-variant);border-radius:2px;display:inline-block;flex:none;
         transition:background-color .12s,border-color .12s}
  input[type=checkbox]:checked{background:var(--primary) url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='%233d2c00' d='M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z'/></svg>") center/16px no-repeat;
         border-color:var(--primary)}
  input.switch{width:52px;height:32px;border-radius:16px;border:2px solid var(--outline);position:relative;
         background:var(--container-highest)}
  input.switch::after{content:"";position:absolute;top:6px;left:6px;width:16px;height:16px;border-radius:8px;
         background:var(--outline);transition:transform .15s,width .15s,height .15s}
  input.switch:checked{background:var(--primary);border-color:var(--primary)}
  input.switch:checked::after{transform:translate(20px,-4px);width:24px;height:24px;border-radius:12px;background:var(--on-primary)}

  /* toolbar above each list */
  .tools{display:flex;gap:12px 16px;align-items:center;flex-wrap:wrap;margin:8px 0 20px}
  .tools .gap{flex-basis:100%;height:0}     /* filters on the first line, bulk actions on the second */
  .bulk{display:flex;gap:8px;align-items:center}
  .quiet{color:var(--on-surface-variant)}
  .num{color:var(--on-surface-variant);font:400 12px/16px var(--sans)}

  /* one card per sinner */
  .group{background:var(--container);border-radius:12px;margin-bottom:16px;overflow:hidden}
  .ghead{display:flex;align-items:center;gap:16px;padding:16px 16px 12px 20px;position:sticky;top:-4px;z-index:2;
         background:var(--container)}
  .avatar{width:40px;height:40px;border-radius:20px;display:grid;place-items:center;flex:none;
          background:var(--s);color:#15120e;font:600 16px/1 var(--display)}
  .ghead .who{min-width:0}
  .ghead h3{margin:0;font:400 22px/28px var(--display)}
  .meter{width:160px;height:4px;border-radius:2px;background:var(--container-highest);margin-top:6px;overflow:hidden}
  .meter i{display:block;height:100%;border-radius:2px;background:var(--s)}
  .tally{color:var(--on-surface-variant);white-space:nowrap}
  .ghead .gap{flex:1}
  .line{display:grid;grid-template-columns:24px 56px 48px 1fr auto;gap:12px;align-items:center;
        min-height:48px;padding:4px 16px 4px 20px;border-top:1px solid var(--outline-variant)}
  #itemrows .line{grid-template-columns:24px 56px 1fr auto}
  #egorows .line{grid-template-columns:24px 56px 1fr auto}
  #itemrows input[type=number],#iall{width:96px}
  .group > .line:first-child{border-top:0}
  .line:hover{background:rgba(237,227,209,.04)}
  .line.out .title,.line.out .num,.line.out .rar{opacity:.4}
  .title{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
  .title s{text-decoration:none;color:var(--on-surface-variant)}
  /* rarity the way the game writes it: 0, 00, 000 */
  .rar{height:24px;border-radius:8px;display:grid;place-items:center;font:600 12px/1 var(--sans);letter-spacing:.08em}
  .rar.r1{background:var(--container-highest);color:var(--on-surface-variant)}
  .rar.r2{background:rgba(147,35,29,.45);color:var(--error)}
  .rar.r3{background:var(--primary-container);color:var(--on-primary-container)}
  .set{display:flex;gap:8px;align-items:center}
  .set span{color:var(--on-surface-variant);font:500 12px/16px var(--sans)}
  .empty{padding:56px 4px;color:var(--on-surface-variant);text-align:center}

  /* account pages */
  .cols{display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:16px;align-items:start;max-width:1180px}
  .block{background:var(--container);border-radius:12px;padding:20px 24px 24px}
  .block h2{margin:0 0 8px;font:400 24px/32px var(--display)}
  .block p{margin:0 0 12px;color:var(--on-surface-variant);max-width:62ch}
  .block p b{color:var(--on-surface);font-weight:500}
  .block hr{border:0;border-top:1px solid var(--outline-variant);margin:20px 0}
  .block.danger{box-shadow:inset 0 0 0 1px var(--error-container)}
  .block.danger h2{color:var(--error)}
  .fld{display:flex;gap:16px;align-items:center;min-height:52px;border-bottom:1px solid var(--outline-variant)}
  .fld span{flex:1}
  .fld select{max-width:300px}
  .block .btn{margin-top:16px}
  .ticks{display:flex;flex-wrap:wrap;gap:8px}
  .tick{display:inline-flex;gap:8px;align-items:center;height:32px;padding:0 12px 0 8px;border-radius:8px;
        border:1px solid var(--outline-variant);cursor:pointer}
  .tick:has(input:checked){background:var(--secondary-container);border-color:transparent;color:var(--on-secondary-container)}
  .tick .num{color:inherit}

  /* snackbar and dialog */
  #say{position:fixed;left:50%;bottom:24px;z-index:30;min-width:288px;max-width:560px;padding:14px 16px;
       border-radius:4px;background:var(--inverse-surface);color:var(--inverse-on-surface);
       box-shadow:0 4px 12px rgba(0,0,0,.45);opacity:0;transform:translate(-50%,12px);
       transition:opacity .2s,transform .2s;pointer-events:none}
  #say.show{opacity:1;transform:translate(-50%,0)}
  #say.err{background:var(--error-container);color:var(--on-error-container)}
  dialog{border:0;border-radius:28px;padding:24px;max-width:420px;background:var(--container-high);color:var(--on-surface)}
  dialog::backdrop{background:rgba(0,0,0,.55)}
  dialog h2{margin:0 0 16px;font:400 24px/32px var(--display)}
  dialog p{margin:0 0 24px;color:var(--on-surface-variant)}
  dialog .row{display:flex;justify-content:flex-end;gap:8px}
  dialog.danger h2,dialog.danger [value=yes]{color:var(--error)}

  ::-webkit-scrollbar{width:12px;height:12px}
  ::-webkit-scrollbar-thumb{background:var(--container-highest);border:3px solid var(--surface);border-radius:6px}
  ::selection{background:var(--primary-container);color:var(--on-primary-container)}
  @media (prefers-reduced-motion:reduce){*{transition:none!important;scroll-behavior:auto!important}}
  @media (max-width:900px){
    body{grid-template-columns:1fr;grid-template-rows:auto 1fr}
    .drawer{flex-direction:row;overflow-x:auto;padding:8px;gap:4px}
    .drawer .dhead,.drawer hr,.drawer .foot{display:none}
    .drawer a{height:40px;padding:0 16px;white-space:nowrap;flex:none}
    .appbar,.sheet{padding-left:16px;padding-right:16px}
    .mark{font-size:44px}
    .line{grid-template-columns:24px 48px 1fr;row-gap:4px}
    .line .num{display:none}
    .line .set{grid-column:2 / -1}
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
  <a data-tab="seed"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M15 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-9-2V7H4v3H1v2h3v3h2v-3h3v-2H6zm9 4c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg><span>New accounts</span><em></em></a>
  <div class="foot"><img id="appicon" src="tweak/icon" alt="" width="40" height="40">
    <div>Mirror<small>Limbus private server</small></div></div>
</nav>

<div class="main">
  <header class="appbar">
    <div class="brand">
      <h1 class="mark">Mirror<span aria-hidden="true"><i>Mirror</i></span></h1>
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
        <p class="door-says">It's the monster! Big terrible monster lives in the dark, black forest!</p>
      </div>
      <div class="within hide" id="forestwithin" style="background-image:url(tweak/blackforest/backdrop)">
        <h2>The Black Forest</h2>
        <p>Greencliff fell, and took everyone with him. Gebura and the green sinners will wait here,
           once they exist.</p>
        <button class="btn tonal" id="sealforest">Seal the gate</button>
      </div>
      <audio id="forestcry" src="tweak/blackforest/cry" preload="none"></audio>
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
  ["ids","ego","items","cos","acct","seed","forest"].forEach(t => $("#tab-"+t).classList.toggle("hide", t !== name));
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

loadAccounts();
</script>
</body>
</html>
""";
}
