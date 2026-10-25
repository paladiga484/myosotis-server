namespace Server.Launcher;

/// <summary>
/// The Windows app's window. One file, no external assets, served at GET /launcher and opened
/// by "Limbus Private Server.exe" in an app window. Same brass-on-umber Material 3 roles as the
/// mod menu, so the two read as one app.
/// </summary>
public static class LauncherPage
{
    public const string Html = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Limbus Private Server</title>
<link rel="icon" href="/launcher/icon">
<style>
  :root{
    color-scheme:dark;
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
    --ok:#b7d38a;
    --display:"Noto Serif Display","Noto Serif",Georgia,serif;
    --sans:"Noto Sans","Segoe UI","DejaVu Sans",system-ui,sans-serif;
  }
  *{box-sizing:border-box}
  html,body{height:100%}
  body{margin:0;background:var(--surface);color:var(--on-surface);font:400 14px/20px var(--sans);
       display:grid;grid-template-rows:auto 1fr auto;overflow:hidden;user-select:none}
  svg{width:24px;height:24px;fill:currentColor;flex:none}
  [hidden]{display:none!important}
  input{user-select:text}

  /* top bar */
  .appbar{display:flex;align-items:center;gap:14px;padding:16px 28px 0}
  .appbar img{width:36px;height:36px;border-radius:9px;display:block}
  .appbar .names{display:flex;flex-direction:column;flex:1;min-width:0}
  .appbar b{font:500 16px/22px var(--sans)}
  .appbar small{font:400 12px/16px var(--sans);color:var(--on-surface-variant)}
  .chip{display:inline-flex;align-items:center;gap:8px;height:32px;padding:0 14px 0 12px;border-radius:8px;
        border:1px solid var(--outline-variant);color:var(--on-surface-variant);font:500 13px/18px var(--sans);
        white-space:nowrap}
  .dot{width:8px;height:8px;border-radius:4px;background:var(--outline);flex:none}
  .dot.on{background:var(--ok)} .dot.busy{background:var(--primary)} .dot.bad{background:var(--error)}

  /* body: the play side and the cards side */
  main{display:grid;grid-template-columns:minmax(380px,1.15fr) minmax(330px,1fr);gap:28px;
       padding:12px 28px 8px;min-height:0;overflow:auto}
  .hero{display:flex;flex-direction:column;padding:8px 4px 0;min-width:0}
  /* the mark: the word over its reflection, split by two cracks (the mod menu's mirror) */
  .mark{position:relative;margin:18px 0 34px;font:300 76px/1 var(--display);letter-spacing:.01em;color:var(--on-surface)}
  .mark span{position:absolute;left:0;right:0;top:100%;height:30px;overflow:hidden;color:var(--primary);
             transform:scaleY(-1);opacity:.22;pointer-events:none;
             -webkit-mask-image:linear-gradient(to top,#000,transparent 90%);
             mask-image:linear-gradient(to top,#000,transparent 90%);
             clip-path:polygon(0 0,31% 0,28% 100%,0 100%,0 0,36% 0,55% 0,51% 100%,33% 100%,59% 0,100% 0,100% 100%,55% 100%)}
  .mark span i{position:absolute;left:0;bottom:0;font-style:normal;line-height:1;white-space:nowrap}
  .lede{margin:0 0 26px;max-width:470px;color:var(--on-surface-variant);font:400 15px/22px var(--sans)}

  /* where to play: a two-way segmented button */
  .seg{display:inline-flex;border:1px solid var(--outline);border-radius:20px;overflow:hidden;align-self:flex-start;
       margin-bottom:22px}
  .seg button{height:40px;padding:0 18px 0 14px;display:inline-flex;align-items:center;gap:8px;
              color:var(--on-surface);font:500 14px/20px var(--sans)}
  .seg button+button{border-left:1px solid var(--outline)}
  .seg button svg{width:18px;height:18px}
  .seg button .tick{display:none}
  .seg button.on{background:var(--secondary-container);color:var(--on-secondary-container)}
  .seg button.on .tick{display:block}
  .seg button.on .ico{display:none}

  /* the one loud thing on the page */
  .play{align-self:flex-start;height:64px;padding:0 40px 0 30px;border-radius:32px;display:inline-flex;
        align-items:center;gap:14px;background:var(--primary);color:var(--on-primary);
        font:600 20px/24px var(--sans);letter-spacing:.02em;box-shadow:0 2px 8px rgba(0,0,0,.4);
        position:relative;overflow:hidden;transition:box-shadow .15s,background .2s,color .2s}
  .play svg{width:28px;height:28px}
  .play:hover{box-shadow:0 4px 16px rgba(0,0,0,.5)}
  .play::after{content:"";position:absolute;inset:0;background:currentColor;opacity:0;transition:opacity .15s}
  .play:hover::after{opacity:.08} .play:active::after{opacity:.12}
  .play:disabled{background:var(--container-highest);color:var(--on-surface-variant);box-shadow:none;cursor:default}
  .play:disabled::after{display:none}
  .play .spin{display:none;width:22px;height:22px;border-radius:11px;border:3px solid currentColor;
              border-right-color:transparent;animation:turn .8s linear infinite}
  .play.busy .spin{display:block} .play.busy svg{display:none}
  @keyframes turn{to{transform:rotate(360deg)}}

  /* what is happening: three steps */
  .steps{display:flex;align-items:center;flex-wrap:wrap;row-gap:10px;margin:28px 0 0;padding:0;list-style:none}
  .steps li{display:flex;align-items:center;gap:10px;color:var(--on-surface-variant);font:500 13px/18px var(--sans);
            white-space:nowrap}
  .steps li+li::before{content:"";width:22px;height:1px;background:var(--outline-variant);margin:0 10px}
  .steps i{width:22px;height:22px;border-radius:11px;border:2px solid var(--outline-variant);flex:none;
           display:grid;place-items:center;font:700 12px/1 var(--sans);font-style:normal}
  .steps li.active{color:var(--on-surface)}
  .steps li.active i{border-color:var(--primary);border-right-color:transparent;animation:turn 1s linear infinite}
  .steps li.done{color:var(--on-surface)}
  .steps li.done i{background:var(--primary);border-color:var(--primary);color:var(--on-primary)}
  .steps li.done i::before{content:"\2713"}
  .steps li.fail{color:var(--error)}
  .steps li.fail i{border-color:var(--error);color:var(--error)}
  .steps li.fail i::before{content:"!"}
  .status{margin:18px 0 0;min-height:22px;font:400 15px/22px var(--sans);color:var(--on-surface-variant);max-width:520px}
  .status.good{color:var(--ok)} .status.bad{color:var(--error)}
  .hero .under{display:flex;align-items:center;gap:8px;margin-top:6px}

  /* the cards */
  .side{display:flex;flex-direction:column;gap:16px;min-width:0;padding-top:12px}
  .card{background:var(--container-low);border-radius:16px;padding:20px 22px 18px}
  .card h2{margin:0 0 4px;font:400 22px/28px var(--display);color:var(--on-surface)}
  .card p{margin:0 0 14px;color:var(--on-surface-variant);font:400 13px/19px var(--sans)}
  .card.quiet{opacity:.72}
  .field{display:flex;flex-direction:column;gap:6px;margin:0 0 12px;font:500 12px/16px var(--sans);
         color:var(--on-surface-variant)}
  .row{display:flex;align-items:center;gap:8px}
  .row.end{justify-content:flex-end;margin-top:4px}
  .note{flex:1;font:400 13px/18px var(--sans);color:var(--on-surface-variant);min-width:0}
  .note.good{color:var(--ok)} .note.bad{color:var(--error)}
  input[type=text],input[type=password]{flex:1;min-width:0;height:40px;background:transparent;color:var(--on-surface);
        border:1px solid var(--outline);border-radius:4px;padding:0 12px;font:400 14px/20px var(--sans)}
  input[type=text]:hover,input[type=password]:hover{border-color:var(--on-surface)}
  input[type=text]:focus,input[type=password]:focus{border-color:var(--primary);box-shadow:inset 0 0 0 1px var(--primary);outline:none}
  input::placeholder{color:var(--outline)}
  .addr{font:500 15px/20px var(--sans);color:var(--on-surface);letter-spacing:.02em}

  /* buttons */
  button{font:500 14px/20px var(--sans);color:inherit;cursor:pointer;border:0;background:none}
  .btn{height:40px;padding:0 20px;border-radius:20px;display:inline-flex;align-items:center;gap:8px;
       position:relative;overflow:hidden;white-space:nowrap}
  .btn svg{width:18px;height:18px}
  .btn::after{content:"";position:absolute;inset:0;background:currentColor;opacity:0;transition:opacity .15s}
  .btn:hover::after{opacity:.08} .btn:active::after{opacity:.12}
  .btn.filled{background:var(--primary);color:var(--on-primary)}
  .btn.tonal{background:var(--secondary-container);color:var(--on-secondary-container)}
  .btn.outlined{border:1px solid var(--outline);color:var(--primary)}
  .btn.text{padding:0 12px;color:var(--primary)}
  .btn.small{height:32px;padding:0 14px;border-radius:16px}
  .btn:disabled{opacity:.38;cursor:default} .btn:disabled::after{display:none}
  .icon{width:40px;height:40px;border-radius:20px;display:grid;place-items:center;color:var(--on-surface-variant);
        position:relative;overflow:hidden;flex:none}
  .icon svg{width:20px;height:20px}
  .icon:hover{background:rgba(237,227,209,.08)}
  button:focus-visible,input:focus-visible{outline:2px solid var(--primary);outline-offset:2px}

  /* a Material 3 switch */
  input.switch{appearance:none;-webkit-appearance:none;width:52px;height:32px;border-radius:16px;margin:0;flex:none;
         border:2px solid var(--outline);background:var(--container-highest);position:relative;cursor:pointer;
         transition:background .15s,border-color .15s}
  input.switch::before{content:"";position:absolute;top:6px;left:6px;width:16px;height:16px;border-radius:8px;
         background:var(--outline);transition:all .15s}
  input.switch:checked{background:var(--primary);border-color:var(--primary)}
  input.switch:checked::before{left:22px;top:2px;width:24px;height:24px;border-radius:12px;background:var(--on-primary)}
  input.switch:disabled{opacity:.38;cursor:default}

  /* bottom bar */
  .bar{display:flex;align-items:center;gap:8px;padding:12px 28px 18px;border-top:1px solid var(--outline-variant)}
  .bar .spacer{flex:1}
  .bar .running{display:inline-flex;align-items:center;gap:8px;color:var(--on-surface-variant);font:500 13px/18px var(--sans);
                margin-right:8px}

  /* messages and the stopped/restarting cover */
  .snack{position:fixed;left:50%;bottom:84px;transform:translate(-50%,16px);opacity:0;pointer-events:none;
         background:var(--inverse-surface);color:var(--inverse-on-surface);border-radius:4px;padding:14px 16px;
         max-width:560px;font:400 14px/20px var(--sans);box-shadow:0 3px 8px rgba(0,0,0,.4);
         transition:opacity .2s,transform .2s;z-index:30}
  .snack.show{opacity:1;transform:translate(-50%,0)}
  .snack.bad{background:var(--error-container);color:var(--on-error-container)}
  .cover{position:fixed;inset:0;background:rgba(21,18,14,.94);display:grid;place-items:center;z-index:40}
  .cover div{text-align:center;max-width:420px}
  .cover h2{margin:0 0 8px;font:300 34px/40px var(--display)}
  .cover p{margin:0;color:var(--on-surface-variant);font:400 15px/22px var(--sans)}

  @media (max-width:860px){main{grid-template-columns:1fr}.mark{font-size:60px}}
</style>
</head>
<body>
<header class="appbar">
  <img src="/launcher/icon" alt="" onerror="this.hidden=true">
  <div class="names"><b>Limbus Private Server</b><small>for Limbus Company <span id="ver"></span></small></div>
  <span class="chip" id="srvchip"><span class="dot on"></span><span>Server running</span></span>
</header>

<main>
  <section class="hero">
    <h1 class="mark">Limbus<span aria-hidden="true"><i>Limbus</i></span></h1>
    <p class="lede" id="lede">Your own server, on this computer. Every identity and E.G.O., and Mirror Dungeon
      on every difficulty, extreme included.</p>

    <div class="seg" role="radiogroup" aria-label="Where to play">
      <button role="radio" data-mode="local" class="on">
        <svg class="tick" viewBox="0 0 24 24"><path d="M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z"/></svg>
        <svg class="ico" viewBox="0 0 24 24"><path d="M20 18c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2H4c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2H0v2h24v-2h-4zM4 6h16v10H4V6z"/></svg>
        On this computer</button>
      <button role="radio" data-mode="join">
        <svg class="tick" viewBox="0 0 24 24"><path d="M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4z"/></svg>
        <svg class="ico" viewBox="0 0 24 24"><path d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"/></svg>
        On a friend's server</button>
    </div>

    <button class="play" id="play">
      <svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg><span class="spin"></span><span id="playlabel">Play</span>
    </button>

    <ol class="steps" id="steps" hidden>
      <li data-step="steam"><i></i>Steam</li>
      <li data-step="window"><i></i>Limbus opens</li>
      <li data-step="patch"><i></i>Patch attached</li>
    </ol>
    <p class="status" id="status"></p>
    <div class="under"><button class="btn text small" id="cancel" hidden>Cancel</button></div>
  </section>

  <aside class="side">
    <article class="card" id="friendcard">
      <h2>A friend's server</h2>
      <p>Their address on your shared Tailscale network, and a token of your own. The token <b>is</b> your
        account there - keep it secret, and use the same one every time.</p>
      <label class="field">Their address
        <span class="row"><input type="text" id="host" placeholder="100.64.0.7" spellcheck="false" autocomplete="off">
          <button class="btn tonal small" id="test">Test</button></span></label>
      <label class="field">Your token
        <span class="row"><input type="password" id="token" spellcheck="false" autocomplete="off">
          <button class="icon" id="show" title="Show the token" aria-label="Show the token">
            <svg viewBox="0 0 24 24"><path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z"/></svg></button>
          <button class="btn text small" id="gen">New</button></span></label>
      <div class="row end"><span class="note" id="joinnote"></span><button class="btn filled small" id="savejoin">Save</button></div>
    </article>

    <article class="card" id="hostcard">
      <h2>Host for friends</h2>
      <p id="hosttext">Checking for Tailscale...</p>
      <div class="row"><span class="addr" id="hostaddr"></span><span class="note"></span>
        <input type="checkbox" class="switch" id="hostsw" aria-label="Let friends join"></div>
    </article>
  </aside>
</main>

<footer class="bar">
  <button class="btn tonal" id="menu">
    <svg viewBox="0 0 24 24"><path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04a1 1 0 0 0 0-1.41l-2.34-2.34a1 1 0 0 0-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/></svg>
    Mod menu</button>
  <button class="btn text" id="logs">Logs</button>
  <span class="spacer"></span>
  <span class="running" id="gamechip" hidden><span class="dot on"></span>Limbus is running</span>
  <button class="btn outlined" id="quit">Quit</button>
</footer>

<div class="snack" id="snack" role="status"></div>
<div class="cover" id="cover" hidden><div><h2 id="covertitle"></h2><p id="covertext"></p></div></div>

<script>
const $ = s => document.querySelector(s);
let mode = localStorage.getItem("mode") === "join" ? "join" : "local";
let state = null, quitting = false, restarting = false, misses = 0, hostDirty = false;

async function api(path, body) {
  const r = await fetch("/launcher/api/" + path, body === undefined ? {} : {
    method: "POST", headers: {"Content-Type": "application/json", "X-Mirror": "1"}, body: JSON.stringify(body)});
  let data = null;
  try { data = await r.json(); } catch { }
  if (!r.ok) throw new Error(data && data.error ? data.error : "Something went wrong (" + r.status + ").");
  return data;
}

let snackTimer;
function snack(text, bad) {
  const s = $("#snack");
  s.textContent = text; s.classList.toggle("bad", !!bad); s.classList.add("show");
  clearTimeout(snackTimer); snackTimer = setTimeout(() => s.classList.remove("show"), bad ? 6000 : 3200);
}

function cover(title, text) {
  $("#covertitle").textContent = title; $("#covertext").textContent = text; $("#cover").hidden = !title;
}

function setMode(m) {
  mode = m; localStorage.setItem("mode", m);
  document.querySelectorAll(".seg button").forEach(b => {
    b.classList.toggle("on", b.dataset.mode === m); b.setAttribute("aria-checked", b.dataset.mode === m);
  });
  $("#lede").textContent = m === "join"
    ? "Play on a friend's server. Your save lives there, under your token - fill in the card on the right once."
    : "Your own server, on this computer. Every identity and E.G.O., and Mirror Dungeon on every difficulty, extreme included.";
  $("#friendcard").classList.toggle("quiet", m !== "join");
  render();
}

const STEPS = ["steam", "window", "patch"];
const AT = {launching: 0, waiting: 1, attaching: 2, attached: 3};

function render() {
  if (!state) return;
  $("#ver").textContent = state.version;
  const p = state.play, busy = ["launching", "waiting", "attaching"].includes(p.phase);
  const joinReady = state.join.host && state.join.tokenSet;

  // the play button
  const play = $("#play");
  play.classList.toggle("busy", busy);
  const here = p.phase === "attached" && state.game.running;
  play.disabled = busy || here || (mode === "join" && !joinReady);
  $("#playlabel").textContent = busy ? "Starting..." : here ? "Playing" : "Play";

  // steps and status
  const steps = $("#steps"), shown = p.phase !== "idle" && p.mode === mode;
  steps.hidden = !shown;
  const at = p.phase === "failed" ? Math.max(0, STEPS.indexOf(steps.dataset.last || "steam")) : (AT[p.phase] ?? -1);
  if (p.phase in AT) steps.dataset.last = STEPS[Math.min(2, AT[p.phase])];
  STEPS.forEach((name, i) => {
    const li = steps.querySelector(`[data-step=${name}]`);
    li.className = p.phase === "failed" ? (i < at ? "done" : i === at ? "fail" : "")
                 : (i < at ? "done" : i === at && busy ? "active" : "");
  });
  const status = $("#status");
  if (shown && p.message) { status.textContent = p.message; status.className = "status " + (p.phase === "failed" ? "bad" : p.phase === "attached" ? "good" : ""); }
  else if (mode === "join" && !joinReady) { status.textContent = "Fill in your friend's address and your token on the right first."; status.className = "status"; }
  else { status.textContent = ""; status.className = "status"; }
  $("#cancel").hidden = !(busy && p.phase !== "attaching");

  // a friend's server
  const host = $("#host");
  if (!hostDirty && document.activeElement !== host) host.value = state.join.host || "";
  $("#token").placeholder = state.join.tokenSet ? "Saved - type a new one to change it" : "Make one up, or press New";

  // hosting
  const sw = $("#hostsw"), h = state.hosting;
  if (h.address) {
    sw.checked = true; sw.disabled = false;
    $("#hostaddr").textContent = h.address;
    $("#hosttext").textContent = "Friends join at this address. Give it to them, along with this package. Keep the app open while they play.";
  } else if (h.candidates.length) {
    sw.checked = false; sw.disabled = false;
    $("#hostaddr").textContent = h.candidates[0];
    $("#hosttext").textContent = "Tailscale is on. Turn this on to let friends you've invited to your Tailscale network play on your server.";
  } else {
    sw.checked = false; sw.disabled = true;
    $("#hostaddr").textContent = "";
    $("#hosttext").textContent = "Install Tailscale and sign in to host - your friends need it too. Never open a port on your router instead.";
  }

  // the bar
  $("#gamechip").hidden = !state.game.running;
}

async function tick() {
  try {
    state = await api("state");
    misses = 0;
    if (restarting) { restarting = false; snack("Server restarted."); }
    if (!quitting) cover("");
    render();
  } catch {
    if (quitting) return;
    if (++misses >= 2)
      restarting ? cover("Restarting the server...", "This takes a few seconds.")
                 : cover("The server isn't running", "Open Limbus Private Server again to start it. This window reconnects by itself.");
  }
}

$("#play").onclick = async () => {
  try { await api("play", {mode}); await tick(); } catch (e) { snack(e.message, true); }
};
$("#cancel").onclick = async () => { try { await api("cancel", {}); await tick(); } catch (e) { snack(e.message, true); } };
document.querySelectorAll(".seg button").forEach(b => b.onclick = () => setMode(b.dataset.mode));

$("#host").oninput = () => { hostDirty = true; $("#joinnote").textContent = ""; };
$("#show").onclick = () => { const t = $("#token"); t.type = t.type === "password" ? "text" : "password"; };
$("#gen").onclick = () => {
  const abc = "abcdefghijkmnpqrstuvwxyz23456789", bytes = crypto.getRandomValues(new Uint8Array(24));
  let s = ""; bytes.forEach((v, i) => { s += abc[v % abc.length]; if (i % 6 === 5 && i < 23) s += "-"; });
  const t = $("#token"); t.value = s; t.type = "text";
  $("#joinnote").textContent = "Saved once you press Save. Write it down somewhere safe.";
  $("#joinnote").className = "note";
};
$("#test").onclick = async () => {
  const note = $("#joinnote"), host = $("#host").value.trim();
  if (!host) { note.textContent = "Type their address first."; note.className = "note bad"; return; }
  note.textContent = "Asking..."; note.className = "note";
  try {
    const r = await api("join/test", {host});
    if (!r.ok) { note.textContent = r.error; note.className = "note bad"; return; }
    const ours = state ? state.version : "";
    if (ours && !r.versions.includes(ours)) { note.textContent = "Reachable, but it takes " + r.versions.join(", ") + " and you have " + ours + "."; note.className = "note bad"; }
    else { note.textContent = "Reachable. Their server takes your version."; note.className = "note good"; }
  } catch (e) { note.textContent = e.message; note.className = "note bad"; }
};
$("#savejoin").onclick = async () => {
  const note = $("#joinnote");
  try {
    await api("join", {host: $("#host").value.trim(), token: $("#token").value.trim()});
    hostDirty = false; $("#token").value = ""; $("#token").type = "password";
    note.textContent = "Saved."; note.className = "note good";
    if (mode !== "join") setMode("join");
    await tick();
  } catch (e) { note.textContent = e.message; note.className = "note bad"; }
};

$("#hostsw").onchange = async e => {
  const on = e.target.checked, address = state && (state.hosting.address || state.hosting.candidates[0]);
  try {
    const r = await api("hosting", {on, address});
    if (r && r.restarting) { restarting = true; cover("Restarting the server...", on ? "Opening it to your Tailscale network." : "Closing it to everyone but you."); }
    else if (r && r.note) snack(r.note);
  } catch (err) { e.target.checked = !on; snack(err.message, true); }
};

$("#menu").onclick = () => window.open("/tweak", "_blank", "popup,width=1280,height=860");
$("#logs").onclick = async () => { try { await api("logs", {}); } catch (e) { snack(e.message, true); } };
$("#quit").onclick = async () => {
  try {
    await api("quit", {});
    quitting = true;
    cover("Server stopped", "You can close this window.");
    setTimeout(() => window.close(), 600);
  } catch (e) { snack(e.message, true); }
};

setMode(mode);
tick();
setInterval(tick, 1000);
</script>
</body>
</html>
""";
}
