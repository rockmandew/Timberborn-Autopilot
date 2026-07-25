using System.Net;
using System.Text;
using System.Threading.Tasks;
using Timberborn.HttpApiSystem;

namespace TimberbornAutopilot.Http
{
    /// <summary>
    /// Phone-friendly remote control at /api/autopilot/remote — big touch
    /// targets for brain on/off and game speed, compact vitals, live feed.
    /// </summary>
    public class AutopilotRemoteEndpoint : IHttpApiEndpoint
    {
        private const string Path = "/api/autopilot/remote";

        public async Task<bool> TryHandle(HttpListenerContext context)
        {
            if (context.Request.Url.AbsolutePath.TrimEnd('/') != Path)
            {
                return false;
            }
            byte[] body = Encoding.UTF8.GetBytes(Html);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body, 0, body.Length);
            context.Response.Close();
            return true;
        }

        private const string Html = @"<!doctype html>
<html><head><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1,user-scalable=no'>
<title>Autopilot Remote</title>
<style>
 :root{color-scheme:dark}
 body{font-family:system-ui,sans-serif;background:#162116;color:#e8e0c8;margin:0;padding:12px;padding-bottom:32px}
 h1{font-size:16px;margin:0 0 10px;color:#a8d08d;display:flex;justify-content:space-between;align-items:center}
 #state{font-size:11px;color:#8a9a78}
 .vitals{display:grid;grid-template-columns:repeat(3,1fr);gap:6px;margin-bottom:12px}
 .v{background:#243524;border:1px solid #3a4f3a;border-radius:10px;padding:8px;text-align:center}
 .v .l{font-size:10px;color:#9fb08a;text-transform:uppercase}
 .v .n{font-size:18px;font-weight:700;margin-top:2px}
 .warn .n{color:#e8a04c}.crit .n{color:#e86c5c}.good .n{color:#a8d08d}
 h2{font-size:12px;color:#9fb08a;margin:14px 0 6px;text-transform:uppercase;letter-spacing:.5px}
 .row{display:grid;grid-template-columns:repeat(3,1fr);gap:8px}
 button{background:#2e452e;border:1px solid #4a664a;color:#e8e0c8;border-radius:12px;padding:14px 0;font-size:15px;font-weight:600;touch-action:manipulation}
 button:active{background:#3c5a3c}
 button.on{background:#4a7a3c;border-color:#6a9a5c}
 button.off{background:#7a3c3c;border-color:#9a5c5c}
 button.sel{background:#4a7a3c;border-color:#6a9a5c}
 .feed{background:#243524;border:1px solid #3a4f3a;border-radius:10px;padding:8px;max-height:38vh;overflow-y:auto;-webkit-overflow-scrolling:touch}
 .feed div{font-size:12px;padding:4px 0;border-bottom:1px solid #2c3f2c}
 .feed .sugg{color:#e8a04c}
 #toast{position:fixed;bottom:12px;left:12px;right:12px;background:#4a7a3c;color:#fff;border-radius:10px;padding:10px;text-align:center;font-size:13px;opacity:0;transition:opacity .3s;pointer-events:none}
</style></head><body>
<h1>🦫 Autopilot Remote <span id='state'>…</span></h1>
<div class='vitals' id='vitals'></div>
<h2>Brain</h2>
<div class='row'>
 <button id='autoOn' onclick=""cmd('auto?enabled=true','Brain ON')"">Brain ON</button>
 <button id='autoOff' onclick=""cmd('auto?enabled=false','Brain paused')"">Brain OFF</button>
 <button onclick='tick()'>Refresh</button>
</div>
<h2>Game speed</h2>
<div class='row' id='speeds'>
 <button data-s='0' onclick=""spd(0)"">⏸ Pause</button>
 <button data-s='1' onclick=""spd(1)"">1×</button>
 <button data-s='3' onclick=""spd(3)"">3×</button>
 <button data-s='5' onclick=""spd(5)"">5×</button>
 <button data-s='7' onclick=""spd(7)"">7×</button>
 <button data-s='10' onclick=""spd(10)"">10×</button>
</div>
<h2>Brain feed</h2>
<div class='feed' id='feed'></div>
<div id='toast'></div>
<script>
let lastSpeed = -1;
function toast(t){const el=document.getElementById('toast');el.textContent=t;el.style.opacity=1;setTimeout(()=>el.style.opacity=0,1500)}
async function cmd(q,msg){try{await fetch('/api/autopilot/'+q);toast(msg);tick()}catch(e){toast('Failed — game running?')}}
async function spd(v){await cmd('speed?value='+v, v==0?'Paused':(v+'× speed'))}
function v(label,val,sub,cls){return `<div class='v ${cls||''}'><div class='l'>${label}</div><div class='n'>${val}</div><div class='l'>${sub||''}</div></div>`}
async function tick(){
 try{
  const s = await (await fetch('/api/autopilot/status')).json();
  const b = await (await fetch('/api/autopilot/brain')).json();
  document.getElementById('state').textContent = `C${s.Cycle} D${s.CycleDay} — ${s.Faction}`;
  const waterCls = s.WaterDaysLeft < 1 ? 'crit' : (s.WaterStock < s.WaterTargetForHazard ? 'warn' : 'good');
  const foodCls = s.FoodDaysLeft < 2 ? 'crit' : 'good';
  document.getElementById('vitals').innerHTML =
   v('Water', s.WaterStock, s.WaterDaysLeft.toFixed(1)+'d', waterCls) +
   v('Food', s.FoodStock, s.FoodDaysLeft.toFixed(1)+'d', foodCls) +
   v('Science', s.SciencePoints, '') +
   v('Wellbeing', s.AverageWellbeing+'/'+s.WellbeingUnlockTarget, 'Iron Teeth', s.AverageWellbeing>=s.WellbeingUnlockTarget?'good':'warn') +
   v('Pop', s.Adults+'+'+s.Children, s.Homeless+' homeless') +
   v(s.IsHazardousWeather?'HAZARD':'Hazard in', s.IsHazardousWeather?s.NextHazard.replace('Weather',''):s.DaysUntilHazard+'d', s.HazardDurationDays+'d long', s.IsHazardousWeather?'crit':'');
  document.getElementById('autoOn').className = b.auto ? 'on' : '';
  document.getElementById('autoOff').className = b.auto ? '' : 'off';
  const feed = document.getElementById('feed');
  feed.innerHTML = b.messages.slice(-30).reverse().map(m=>`<div class='${m.startsWith('SUGGESTION')?'sugg':''}'>${m}</div>`).join('');
 }catch(e){ document.getElementById('state').textContent = 'game offline?' }
}
tick(); setInterval(tick, 3000);
</script></body></html>";
    }
}
