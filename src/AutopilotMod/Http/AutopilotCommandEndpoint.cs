using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Timberborn.Coordinates;
using Timberborn.HttpApiSystem;
using Timberborn.PrioritySystem;
using TimberbornAutopilot.Acting;
using UnityEngine;

namespace TimberbornAutopilot.Http
{
    /// <summary>
    /// Manual/remote control over the Act primitives for testing and overrides:
    ///   GET /api/autopilot/build?name=WaterPump&x=10&y=12&z=5[&o=cw90][&priority=High]
    ///   GET /api/autopilot/path?x=10&y=12&z=5
    ///   GET /api/autopilot/zone?resource=Carrot&x1=..&y1=..&x2=..&y2=..&z=..
    ///   GET /api/autopilot/cut?x1=..&y1=..&x2=..&y2=..&z=..
    ///   GET /api/autopilot/pause?x=..&y=..&z=..&paused=true
    ///   GET /api/autopilot/workers?x=..&y=..&z=..&count=2
    ///   GET /api/autopilot/priority?x=..&y=..&z=..&priority=VeryHigh
    ///   GET /api/autopilot/speed?value=5
    /// Game thread safety: commands are queued and executed on the next game tick.
    /// </summary>
    public class AutopilotCommandEndpoint : IHttpApiEndpoint
    {
        private const string Prefix = "/api/autopilot/";

        private readonly BuildPlacer _buildPlacer;
        private readonly ZonePlanner _zonePlanner;
        private readonly CrewManager _crewManager;
        private readonly SpeedController _speedController;

        private readonly object _queueLock = new object();
        private readonly Queue<PendingCommand> _pending = new Queue<PendingCommand>();

        public AutopilotCommandEndpoint(BuildPlacer buildPlacer,
                                        ZonePlanner zonePlanner,
                                        CrewManager crewManager,
                                        SpeedController speedController)
        {
            _buildPlacer = buildPlacer;
            _zonePlanner = zonePlanner;
            _crewManager = crewManager;
            _speedController = speedController;
        }

        /// <summary>Called by AutopilotService.Tick() on the main thread.</summary>
        public void ExecutePending()
        {
            while (true)
            {
                PendingCommand command;
                lock (_queueLock)
                {
                    if (_pending.Count == 0)
                    {
                        return;
                    }
                    command = _pending.Dequeue();
                }
                try
                {
                    command.Result.SetResult(command.Action());
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Autopilot] Command failed: {e}");
                    command.Result.SetResult(new { ok = false, error = e.Message });
                }
            }
        }

        public async Task<bool> TryHandle(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath.TrimEnd('/');
            if (!path.StartsWith(Prefix) || path == "/api/autopilot/status")
            {
                return false;
            }
            string action = path.Substring(Prefix.Length);
            Func<object> gameThreadAction = BuildAction(action, context.Request.QueryString);
            if (gameThreadAction == null)
            {
                return false;
            }

            object result = await EnqueueAsync(gameThreadAction);
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result, Formatting.Indented));
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body, 0, body.Length);
            context.Response.Close();
            return true;
        }

        private Task<object> EnqueueAsync(Func<object> action)
        {
            var completion = new TaskCompletionSource<object>();
            lock (_queueLock)
            {
                _pending.Enqueue(new PendingCommand(action, completion));
            }
            return completion.Task;
        }

        private Func<object> BuildAction(string action, System.Collections.Specialized.NameValueCollection q)
        {
            switch (action)
            {
                case "build":
                    return () =>
                    {
                        bool ok = _buildPlacer.TryPlace(
                            q["name"], Coords(q), ParseOrientation(q["o"]), ParsePriority(q["priority"]),
                            out string error);
                        return new { ok, error };
                    };
                case "path":
                    return () =>
                    {
                        bool ok = _buildPlacer.TryPlacePath(Coords(q), out string error);
                        return new { ok, error };
                    };
                case "zone":
                    return () =>
                    {
                        int zoned = _zonePlanner.ZonePlanting(CoordsFrom(q), CoordsTo(q), q["resource"]);
                        return new { ok = zoned > 0, zoned };
                    };
                case "cut":
                    return () =>
                    {
                        _zonePlanner.MarkTreesForCutting(CoordsFrom(q), CoordsTo(q));
                        return new { ok = true };
                    };
                case "pause":
                    return () => new { ok = _crewManager.TrySetPaused(Coords(q), bool.Parse(q["paused"] ?? "true")) };
                case "workers":
                    return () => new { ok = _crewManager.TrySetDesiredWorkers(Coords(q), int.Parse(q["count"])) };
                case "priority":
                    return () => new { ok = _crewManager.TrySetWorkplacePriority(Coords(q), ParsePriority(q["priority"])) };
                case "speed":
                    return () =>
                    {
                        _speedController.SetSpeed(float.Parse(q["value"]));
                        return new { ok = true, speed = _speedController.CurrentSpeed };
                    };
                default:
                    return null;
            }
        }

        private static Vector3Int Coords(System.Collections.Specialized.NameValueCollection q)
        {
            return new Vector3Int(int.Parse(q["x"]), int.Parse(q["y"]), int.Parse(q["z"]));
        }

        private static Vector3Int CoordsFrom(System.Collections.Specialized.NameValueCollection q)
        {
            return new Vector3Int(int.Parse(q["x1"]), int.Parse(q["y1"]), int.Parse(q["z"]));
        }

        private static Vector3Int CoordsTo(System.Collections.Specialized.NameValueCollection q)
        {
            return new Vector3Int(int.Parse(q["x2"]), int.Parse(q["y2"]), int.Parse(q["z"]));
        }

        private static Orientation ParseOrientation(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "cw90" or "90" or "west" => Orientation.Cw90,
                "cw180" or "180" or "north" => Orientation.Cw180,
                "cw270" or "270" or "east" => Orientation.Cw270,
                _ => Orientation.Cw0,
            };
        }

        private static Priority ParsePriority(string value)
        {
            if (string.IsNullOrEmpty(value) || !Enum.TryParse(value, ignoreCase: true, out Priority priority))
            {
                return Priority.Normal;
            }
            return priority;
        }

        private readonly struct PendingCommand
        {
            public Func<object> Action { get; }
            public TaskCompletionSource<object> Result { get; }

            public PendingCommand(Func<object> action, TaskCompletionSource<object> result)
            {
                Action = action;
                Result = result;
            }
        }
    }
}
