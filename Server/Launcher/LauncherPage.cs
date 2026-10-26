namespace Server.Launcher;

/// <summary>
/// The Windows app's window. One file, no external assets, served at GET /launcher and opened
/// by "Limbus Private Server.exe" in an app window. Built on the mod menu's look (Tweak.Look), so
/// the two read as one app and follow the same theme.
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
""" + Tweak.Look.Tokens + """
  *{box-sizing:border-box}
  html,body{height:100%}
  body{margin:0;display:grid;grid-template-rows:auto 1fr auto;overflow:hidden;user-select:none;font-variant-numeric:tabular-nums}
  body.picture header,body.picture main > *,body.picture footer{background:color-mix(in srgb,var(--bg) 86%,transparent)}
  svg{width:18px;height:18px;fill:currentColor;flex:none}
  [hidden]{display:none!important}
  input{user-select:text}

  header{display:flex;align-items:center;gap:12px;padding:16px 24px;border-bottom:1px solid var(--line)}
  header img{width:28px;height:28px;border-radius:6px;display:block}
  header .names{display:flex;flex-direction:column;flex:1;min-width:0}
  header b{font:600 15px/20px var(--font)}
  header small{font:400 12px/16px var(--font);color:var(--muted)}
  .chip{display:inline-flex;align-items:center;gap:8px;color:var(--muted);font:400 13px/18px var(--font);white-space:nowrap}
  .dot{width:8px;height:8px;border-radius:4px;background:var(--muted);flex:none}
  .dot.on{background:#3fb950} .dot.busy{background:var(--accent)} .dot.bad{background:var(--danger)}

  main{overflow:auto;padding:24px;display:flex;flex-direction:column;gap:24px;align-items:stretch}
  main > *{width:100%;max-width:560px;margin:0 auto}
  section,article{border:1px solid var(--line);border-radius:8px;padding:18px 20px}
  h2{margin:0 0 4px;font:600 15px/22px var(--font)}
  p{margin:0}
  .lede{margin:0 0 16px;color:var(--muted)}

  .seg{display:flex;border:1px solid var(--line);border-radius:6px;overflow:hidden;margin-bottom:16px}
  .seg button{flex:1;height:34px;display:inline-flex;align-items:center;justify-content:center;gap:8px;color:var(--fg)}
  .seg button+button{border-left:1px solid var(--line)}
  .seg button svg{display:none}
  .seg button.on{background:color-mix(in srgb,var(--accent) 20%,transparent)}

  .play{height:40px;padding:0 22px;border-radius:6px;display:inline-flex;align-items:center;gap:10px;
        background:var(--accent);color:var(--on-accent);font:600 15px/20px var(--font)}
  .play:hover{background:color-mix(in srgb,var(--accent) 88%,var(--fg))}
  .play svg{width:16px;height:16px}
  .play:disabled{background:var(--panel);color:var(--muted);cursor:default;border:1px solid var(--line)}
  .play .spin{display:none;width:14px;height:14px;border-radius:7px;border:2px solid currentColor;
              border-right-color:transparent;animation:turn .8s linear infinite}
  .play.busy .spin{display:block} .play.busy svg{display:none}
  @keyframes turn{to{transform:rotate(360deg)}}

  .steps{display:flex;flex-wrap:wrap;gap:6px 18px;margin:16px 0 0;padding:0;list-style:none}
  .steps li{display:flex;align-items:center;gap:8px;color:var(--muted);font:400 13px/18px var(--font)}
  .steps i{width:14px;height:14px;border-radius:7px;border:1px solid var(--line);flex:none}
  .steps li.active{color:var(--fg)}
  .steps li.active i{border:2px solid var(--accent);border-right-color:transparent;animation:turn 1s linear infinite}
  .steps li.done{color:var(--fg)} .steps li.done i{background:var(--accent);border-color:var(--accent)}
  .steps li.fail{color:var(--danger)} .steps li.fail i{border-color:var(--danger);background:var(--danger)}
  .status{margin:12px 0 0;min-height:20px;color:var(--muted)}
  .status.good{color:#3fb950} .status.bad{color:var(--danger)}
  .under{display:flex;margin-top:6px}

  .card.quiet{display:none}
  .card p{margin:0 0 14px;color:var(--muted)}
  .field{display:flex;flex-direction:column;gap:6px;margin:0 0 12px;font:400 12px/16px var(--font);color:var(--muted)}
  .row{display:flex;align-items:center;gap:8px}
  .row.end{justify-content:flex-end;margin-top:4px}
  .note{flex:1;font:400 13px/18px var(--font);color:var(--muted);min-width:0}
  .note.good{color:#3fb950} .note.bad{color:var(--danger)}
  input[type=text],input[type=password]{flex:1;min-width:0;height:32px;background:var(--bg);color:var(--fg);
        border:1px solid var(--line);border-radius:6px;padding:0 10px;font:400 14px/20px var(--font)}
  input[type=text]:hover,input[type=password]:hover{border-color:var(--muted)}
  input[type=text]:focus,input[type=password]:focus{border-color:var(--accent);outline:none}
  input::placeholder{color:var(--muted)}
  .addr{font:600 14px/20px var(--font)}

  button{font:500 14px/20px var(--font);color:inherit;cursor:pointer;border:0;background:none}
  .btn{height:32px;padding:0 14px;border-radius:6px;display:inline-flex;align-items:center;gap:8px;white-space:nowrap;
       border:1px solid var(--line);background:var(--panel)}
  .btn:hover{background:color-mix(in srgb,var(--fg) 10%,var(--bg))}
  .btn svg{display:none}
  .btn.filled{background:var(--accent);border-color:var(--accent);color:var(--on-accent)}
  .btn.text{border-color:transparent;background:none;padding:0 10px}
  .btn.text:hover{background:var(--hover)}
  .btn.small{height:28px;padding:0 10px}
  .btn:disabled{opacity:.4;cursor:default}
  .icon{width:32px;height:32px;border-radius:6px;display:grid;place-items:center;color:var(--muted);flex:none}
  .icon svg{width:18px;height:18px}
  .icon:hover{background:var(--hover)}
  button:focus-visible,input:focus-visible{outline:2px solid var(--accent);outline-offset:2px}

  input.switch{appearance:none;-webkit-appearance:none;width:36px;height:20px;border-radius:10px;margin:0;flex:none;
         background:var(--line);position:relative;cursor:pointer}
  input.switch::before{content:"";position:absolute;top:3px;left:3px;width:14px;height:14px;border-radius:7px;
         background:var(--fg);transition:transform .12s}
  input.switch:checked{background:var(--accent)}
  input.switch:checked::before{transform:translateX(16px);background:var(--on-accent)}
  input.switch:disabled{opacity:.4;cursor:default}

  footer{display:flex;align-items:center;gap:6px;padding:12px 24px;border-top:1px solid var(--line)}
  footer .spacer{flex:1}
  footer .running{display:inline-flex;align-items:center;gap:8px;color:var(--muted);font:400 13px/18px var(--font);margin-right:8px}

  .snack{position:fixed;left:50%;bottom:72px;transform:translateX(-50%);opacity:0;pointer-events:none;
         background:var(--fg);color:var(--bg);border-radius:6px;padding:10px 14px;max-width:520px;transition:opacity .15s;z-index:30}
  .snack.show{opacity:1}
  .snack.bad{background:var(--danger);color:#fff}
  .cover{position:fixed;inset:0;background:var(--bg);display:grid;place-items:center;z-index:40}
  .cover div{text-align:center;max-width:420px}
  .cover h2{margin:0 0 6px;font:600 18px/26px var(--font)}
  .cover p{color:var(--muted)}
  @media (prefers-reduced-motion:reduce){*{transition:none!important}}
</style>
</head>
<body>
<header>
  <img src="/launcher/icon" alt="" onerror="this.hidden=true">
  <div class="names"><b>Limbus Private Server</b><small>for Limbus Company <span id="ver"></span></small></div>
  <span class="chip" id="srvchip"><span class="dot on"></span><span>Server running</span></span>
</header>

<main>
  <section class="hero">
    <p class="lede" id="lede"></p>
    <div class="seg" role="radiogroup" aria-label="Where to play">
      <button role="radio" data-mode="local" class="on">This computer</button>
      <button role="radio" data-mode="join">A friend's server</button>
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

  <article class="card" id="friendcard">
    <h2>A friend's server</h2>
    <p>Their address on your shared Tailscale network, and a token of your own. The token is your
      account there, so keep it secret and use the same one every time.</p>
    <label class="field">Their address
      <span class="row"><input type="text" id="host" placeholder="100.64.0.7" spellcheck="false" autocomplete="off">
        <button class="btn small" id="test">Test</button></span></label>
    <label class="field">Your token
      <span class="row"><input type="password" id="token" spellcheck="false" autocomplete="off">
        <button class="icon" id="show" title="Show the token" aria-label="Show the token">
          <svg viewBox="0 0 24 24"><path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z"/></svg></button>
        <button class="btn text small" id="gen">New</button></span></label>
    <div class="row end"><span class="note" id="joinnote"></span><button class="btn filled small" id="savejoin">Save</button></div>
  </article>

  <article class="card" id="hostcard">
    <div class="row"><h2 style="flex:1">Host for friends</h2>
      <input type="checkbox" class="switch" id="hostsw" aria-label="Let friends join"></div>
    <p id="hosttext" style="color:var(--muted)">Checking for Tailscale...</p>
    <p class="addr" id="hostaddr"></p>
  </article>
</main>

<footer>
  <button class="btn" id="menu">Mod menu</button>
  <button class="btn text" id="logs">Logs</button>
  <span class="spacer"></span>
  <span class="running" id="gamechip" hidden><span class="dot on"></span>Limbus is running</span>
  <button class="btn" id="quit">Quit</button>
</footer>

<div class="snack" id="snack" role="status"></div>
<div class="cover" id="cover" hidden><div><h2 id="covertitle"></h2><p id="covertext"></p></div></div>

<script>
""" + Tweak.Look.Script + """
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
    ? "Plays on a friend's server. Your save lives there, under your token."
    : "Plays on the server on this computer.";
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
  else if (mode === "join" && !joinReady) { status.textContent = "Fill in your friend's address and your token below first."; status.className = "status"; }
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

loadTheme();
addEventListener("focus", loadTheme);      // the look may have changed in the mod menu meanwhile
setMode(mode);
tick();
setInterval(tick, 1000);
</script>
</body>
</html>
""";
}
