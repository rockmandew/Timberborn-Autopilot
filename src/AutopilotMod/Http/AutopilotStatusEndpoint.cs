using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Timberborn.HttpApiSystem;
using TimberbornAutopilot.Sensing;

namespace TimberbornAutopilot.Http
{
    /// <summary>
    /// Adds GET /api/autopilot/status to the game's built-in HTTP API
    /// (default http://localhost:8080). Returns the live WorldSnapshot as JSON.
    /// </summary>
    public class AutopilotStatusEndpoint : IHttpApiEndpoint
    {
        private const string StatusPath = "/api/autopilot/status";

        private readonly WorldModel _worldModel;

        public AutopilotStatusEndpoint(WorldModel worldModel)
        {
            _worldModel = worldModel;
        }

        public async Task<bool> TryHandle(HttpListenerContext context)
        {
            if (context.Request.Url.AbsolutePath.TrimEnd('/') != StatusPath)
            {
                return false;
            }
            WorldSnapshot snapshot = _worldModel.Snapshot();
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(snapshot, Formatting.Indented));
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body, 0, body.Length);
            context.Response.Close();
            return true;
        }
    }
}
