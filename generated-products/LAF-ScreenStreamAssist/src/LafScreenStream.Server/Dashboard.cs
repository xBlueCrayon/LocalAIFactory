namespace LafScreenStream.Server;

public static class Dashboard
{
    public const string Html = """
<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8"><title>LAF ScreenStream Assist</title>
<style>
 body{font-family:system-ui,Arial,sans-serif;margin:0;color:#1f2937}
 header{background:#1d4ed8;color:#fff;padding:12px 20px;font-weight:600}
 main{padding:20px;max-width:1000px}
 .card{background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;padding:14px 18px;margin-bottom:14px}
 table{border-collapse:collapse;width:100%;font-size:14px;margin-top:8px}
 th,td{border:1px solid #e5e7eb;padding:6px 10px;text-align:left}
 th{background:#eff6ff}
 button{background:#1d4ed8;color:#fff;border:0;border-radius:6px;padding:8px 14px;cursor:pointer}
 input{padding:7px;border:1px solid #cbd5e1;border-radius:6px}
 code{background:#eef2ff;padding:2px 6px;border-radius:4px}
 #viewer{max-width:100%;border:1px solid #cbd5e1;image-rendering:pixelated;min-height:60px;background:#f1f5f9}
 .badge{padding:2px 8px;border-radius:10px;font-size:12px}.on{background:#d1fae5}.off{background:#fee2e2}
</style></head><body>
<header>LAF ScreenStream Assist &middot; Server Dashboard</header>
<main>
 <div class="card">
   <div data-testid="health">Status: <b id="health">checking...</b></div>
   <div>Server address for clients: <code data-testid="server-url" id="surl">...</code></div>
   <div>Session token: <code data-testid="token" id="tok">...</code> <small>(clients you generate include this automatically)</small></div>
 </div>
 <div class="card">
   <h3>Generate a Client</h3>
   <p>Type a name for the person, click the button, then send them the generated folder.</p>
   <input id="dn" data-testid="client-name" value="TestClient" />
   <button data-testid="generate-client-btn" onclick="gen()">Generate Client</button>
   <div data-testid="generate-result" id="genres" style="margin-top:8px;font-size:14px;color:#374151"></div>
 </div>
 <div class="card">
   <h3>Connected Clients</h3>
   <table data-testid="clients-table"><thead><tr><th>Name</th><th>Status</th><th>Frames</th><th>FPS</th><th>Last frame</th><th></th></tr></thead>
   <tbody id="rows"><tr><td colspan="6">No clients connected yet.</td></tr></tbody></table>
 </div>
 <div class="card">
   <h3>Screen Viewer</h3>
   <div id="vinfo" style="font-size:13px;color:#6b7280">Select a connected client to view their primary screen.</div>
   <img id="viewer" data-testid="viewer" alt="screen" />
 </div>
</main>
<script>
let sel=null;
async function health(){try{const r=await fetch('/api/health');const j=await r.json();
 document.getElementById('health').textContent=j.status==='ok'?'Running ('+j.product+')':'?';
 document.getElementById('surl').textContent=j.serverWsUrl;document.getElementById('tok').textContent=j.token;}catch(e){}}
async function gen(){const dn=document.getElementById('dn').value||'Client';
 const r=await fetch('/api/generate-client',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({displayName:dn})});
 const j=await r.json();document.getElementById('genres').textContent=j.ok?('Created: '+j.exePath):('Could not generate: '+(j.error||'unknown'));}
async function poll(){try{const r=await fetch('/api/clients');const list=await r.json();const tb=document.getElementById('rows');
 if(!list.length){tb.innerHTML='<tr><td colspan=6>No clients connected yet.</td></tr>';return;}
 tb.innerHTML=list.map(c=>`<tr><td>${c.displayName}</td><td><span class="badge ${c.connected?'on':'off'}">${c.connected?'sharing':'stopped'}</span></td>
  <td>${c.frameCount}</td><td>${c.fps}</td><td>${c.lastFrameMsAgo<0?'-':c.lastFrameMsAgo+' ms'}</td>
  <td><button onclick="view('${c.id}')">View</button> <button onclick="kick('${c.id}')">Disconnect</button></td></tr>`).join('');
 if(!sel&&list.length){sel=list[0].id;}}catch(e){}}
function view(id){sel=id;document.getElementById('vinfo').textContent='Viewing '+id;}
async function kick(id){await fetch('/api/disconnect/'+id,{method:'POST'});}
function refreshViewer(){if(sel){document.getElementById('viewer').src='/api/frame/'+sel+'?t='+Date.now();}}
health();setInterval(poll,1000);setInterval(refreshViewer,500);
</script>
</body></html>
""";
}
