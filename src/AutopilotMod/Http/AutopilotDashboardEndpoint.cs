using System.Net;
using System.Text;
using System.Threading.Tasks;
using Timberborn.HttpApiSystem;

namespace TimberbornAutopilot.Http
{
    /// <summary>
    /// Live dashboard at /api/autopilot/dashboard — self-refreshing view of
    /// colony vitals and the brain feed (polls every 2 seconds, no reloads).
    /// </summary>
    public class AutopilotDashboardEndpoint : IHttpApiEndpoint
    {
        private const string Path = "/api/autopilot/dashboard";

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
<html><head><meta charset='utf-8'><title>Timberborn Autopilot</title>
<style>
 body{font-family:Segoe UI,system-ui,sans-serif;background:#1b2a1b;color:#e8e0c8;margin:0;padding:16px}
 h1{font-size:18px;margin:0 0 12px;color:#a8d08d}
 .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:8px;margin-bottom:12px}
 .card{background:#243524;border:1px solid #3a4f3a;border-radius:8px;padding:10px}
 .card .label{font-size:11px;color:#9fb08a;text-transform:uppercase;letter-spacing:.5px}
 .card .value{font-size:22px;font-weight:600;margin-top:2px}
 .card .sub{font-size:11px;color:#8a9a78}
 .warn .value{color:#e8a04c}.crit .value{color:#e86c5c}.good .value{color:#a8d08d}
 .cols{display:grid;grid-template-columns:1fr 1fr;gap:12px}
 .panel{background:#243524;border:1px solid #3a4f3a;border-radius:8px;padding:10px;max-height:60vh;overflow-y:auto}
 .panel h2{font-size:13px;margin:0 0 8px;color:#9fb08a}
 .feed div{font-size:12px;padding:3px 0;border-bottom:1px solid #2c3f2c}
 .feed .sugg{color:#e8a04c}
 table{width:100%;font-size:12px;border-collapse:collapse}
 td{padding:2px 4px;border-bottom:1px solid #2c3f2c}
 td:last-child{text-align:right}
 .stamp{font-size:10px;color:#6a7a5a;margin-top:8px}
</style></head><body>
<h1>Timberborn Autopilot <span id='obj' style='font-size:12px;color:#9fb08a'></span></h1>
<div class='grid' id='cards'></div>
<div class='cols'>
 <div class='panel'><h2>Brain feed</h2><div class='feed' id='feed'></div></div>
 <div class='panel'><h2>Stocks</h2><table id='stocks'></table></div>
</div>
<div class='stamp' id='stamp'></div>
<script>
function card(label, value, sub, cls){return `<div class='card ${cls||''}'><div class='label'>${label}</div><div class='value'>${value}</div><div class='sub'>${sub||''}</div></div>`}
async function tick(){
 try{
  const s = await (await fetch('/api/autopilot/status')).json();
  const b = await (await fetch('/api/autopilot/brain')).json();
  document.getElementById('obj').textContent = `${s.Faction} — ${s.Objective}` + (s.IronTeethUnlocked ? ' (Iron Teeth unlocked!)' : '');
  const waterCls = s.WaterDaysLeft < 1 ? 'crit' : (s.WaterStock < s.WaterTargetForHazard ? 'warn' : 'good');
  const foodCls = s.FoodDaysLeft < 2 ? 'crit' : (s.FoodStock < s.FoodTargetForHazard ? 'warn' : 'good');
  const wbCls = s.AverageWellbeing >= s.WellbeingUnlockTarget ? 'good' : (s.AverageWellbeing < 0 ? 'crit' : 'warn');
  document.getElementById('cards').innerHTML =
   card('Cycle / Day', `C${s.Cycle} D${s.CycleDay}`, `${s.HoursPassedToday.toFixed(1)}h`) +
   card('Population', `${s.Adults}+${s.Children}`, `${s.Bots} bots, ${s.Homeless} homeless`) +
   card('Water', s.WaterStock, `${s.WaterDaysLeft.toFixed(1)}d — need ${Math.round(s.WaterTargetForHazard)}`, waterCls) +
   card('Food', s.FoodStock, `${s.FoodDaysLeft.toFixed(1)}d — need ${Math.round(s.FoodTargetForHazard)}`, foodCls) +
   card('Science', s.SciencePoints, '') +
   card('Wellbeing', `${s.AverageWellbeing}/${s.WellbeingUnlockTarget}`, 'Iron Teeth unlock', wbCls) +
   card(s.IsHazardousWeather ? 'HAZARD ACTIVE' : 'Next hazard', s.NextHazard.replace('Weather',''),
        s.IsHazardousWeather ? `${s.HazardDurationDays}d total` : `in ${s.DaysUntilHazard}d for ${s.HazardDurationDays}d`,
        s.IsHazardousWeather ? 'crit' : (s.DaysUntilHazard < 3 ? 'warn' : ''));
  document.getElementById('stocks').innerHTML =
   Object.entries(s.Stocks).sort((a,z)=>z[1]-a[1]).map(([k,v])=>`<tr><td>${k}</td><td>${v}</td></tr>`).join('');
  const feed = document.getElementById('feed');
  const atBottom = feed.scrollTop + feed.clientHeight >= feed.scrollHeight - 20;
  feed.innerHTML = b.messages.slice(-80).map(m=>`<div class='${m.startsWith('SUGGESTION')?'sugg':''}'>${m}</div>`).join('');
  if (atBottom) feed.scrollTop = feed.scrollHeight;
  document.getElementById('stamp').textContent = 'auto=' + b.auto + ' — updated ' + new Date().toLocaleTimeString();
 }catch(e){ document.getElementById('stamp').textContent = 'game not responding — ' + new Date().toLocaleTimeString(); }
}
tick(); setInterval(tick, 2000);
</script></body></html>";
    }
}
