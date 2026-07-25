# Timberborn Autopilot

A self-playing, max-strategy Timberborn platform: a C# game mod that senses the full game state every tick, plans with meta-strategy heuristics (see [docs/STRATEGY.md](docs/STRATEGY.md)), and acts — placing buildings, engineering water, managing the economy, and hunting achievements — while you watch at high speed.

**Game:** Timberborn 1.0.13.1 (Steam), Mono backend, official mod support.
**Verified:** this skeleton compiles against the game's assemblies and the mod-loading path is confirmed from decompiled game code.

## How it hooks in

- Official mod loading: folder in `Documents\Timberborn\Mods\Autopilot\` with `manifest.json` + DLL. The game loads every DLL and calls `IModStarter.StartMod()`.
- DI: `[Context("Game")]` `IConfigurator` classes are auto-discovered; our services constructor-inject any game singleton (placement, districts, weather, science, water sim...).
- Loop: `ITickableSingleton.Tick()` runs the sense → plan → act cycle in-sim.
- No BepInEx needed. Harmony patching is officially sanctioned if we ever need to intercept (community "Harmony" mod, Workshop ID 3284904751).
- Achievements are NOT disabled by mods (verified by decompiling `Timberborn.Achievements` — no modded-state gating; the `DateSalter` flag is analytics-only). Avoid the dev console during achievement runs anyway, out of caution.

## Architecture

```
┌────────────────────────── AutopilotService (ITickableSingleton) ─────────────────────────┐
│                                                                                          │
│  SENSE                      PLAN                              ACT                        │
│  WorldModel snapshot        GoalStack (utility AI):           Actuators:                 │
│  - stocks/goods             1. Survival (water/food/badtide)  - BuildPlacer (block objs) │
│  - population/wellbeing     2. Growth (pop, housing)          - ZonePlanner (crops/trees)│
│  - weather forecast         3. Science & unlocks              - PriorityManager (labor)  │
│  - water levels/moisture    4. Wellbeing milestones           - WaterEngineer (gates,    │
│  - power grid               5. Industry ratios                  dynamite, reservoirs)    │
│  - science points           6. Achievement objectives         - DistrictManager          │
│  - achievement progress     7. Wonder / endgame               - SpeedController          │
│                                                                                          │
│  CityPlanner: layout engine (road grid, housing blocks, farm blocks w/ beehive coverage, │
│  industry clusters near power, reservoir siting) — replans as terrain/economy changes    │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

- **Continuous replanning:** the planner re-evaluates every in-game day; nothing is a fixed script. Weather forecast changes, badwater source discovery, terrain changes from dynamite, and population curves all reshape the plan.
- **Water engineering module:** dam/floodgate placement across river cross-sections, reservoir excavation (dynamite below waterline), badtide bypass channels, sluice/gate automation via the official signal network, storage target = `pop × 2.12 × hazard_days × 1.25`.
- **Achievement campaign:** most achievements fall out of one max run; specialty runs are scheduled for conflicting ones (power-wheels-only / water-wheels-only / wind-only, maple-pastry-only, no-dwellings pop, beaver-extinction + bots, both factions' wonders, cycle-15 wonder rush). Campaign state persists across saves.

## Repo layout

- `src/AutopilotMod/` — the C# mod (netstandard2.1, references game DLLs directly)
- `mod/manifest.json` — mod manifest deployed alongside the DLL
- `deploy.ps1` — build + copy to `Documents\Timberborn\Mods\Autopilot`
- `docs/STRATEGY.md` — researched meta-strategy compendium driving the planner
- `docs/MODDING.md` — modding API facts (verified from decompiled game code + ecosystem research)

## Build & run

```powershell
.\deploy.ps1
```

Then launch Timberborn → Mod Manager shows "Autopilot" → enable → start/load a game. Heartbeat logs appear in `Player.log` (`%USERPROFILE%\AppData\LocalLow\Mechanistry\Timberborn\Player.log`).

## Monitoring & remote control

All served by the game's built-in HTTP API (default `localhost:8080`, started automatically by the mod):

- `/api/autopilot/status` — full colony telemetry as JSON
- `/api/autopilot/brain` — the brain's decision feed + suggestions
- `/api/autopilot/dashboard` — live desktop dashboard (auto-refreshes every 2s)
- `/api/autopilot/remote` — mobile control pad: brain on/off, speed presets (pause–10×), vitals, feed
- Command endpoints: `/build`, `/path`, `/zone`, `/cut`, `/pause`, `/workers`, `/priority`, `/speed`, `/survey`, `/templates`, `/auto`

**Phone access (home Wi-Fi):** forward a LAN port to the localhost API and allow it through the firewall (run once, admin):

```powershell
netsh interface portproxy add v4tov4 listenaddress=0.0.0.0 listenport=8081 connectaddress=127.0.0.1 connectport=8080
netsh advfirewall firewall add rule name="Timberborn Autopilot Dashboard" dir=in action=allow protocol=TCP localport=8081
```

Then browse to `http://<your-pc-lan-ip>:8081/api/autopilot/remote` from your phone. (Exposes game control to your local network only; use a VPN like Tailscale for access away from home.)

## Planned: Brain Training mode (self-play loop)

A toggleable overnight training cycle — design settled, implementation upcoming:

1. **Episode runner (in-mod):** a MainMenu-context service reads `TrainingConfig.json` from the mod folder; when enabled it programmatically starts a new game (faction, difficulty, map, settlement name — the game's `NewGameConfiguration`/`GameSceneLoader` APIs support this) or loads a target save.
2. **Metrics & scoring:** every in-game day the brain snapshots the success measures (population, wellbeing, water/food stocks vs. targets, science, logs, buildings completed, drought survival) into an episode log (JSON on disk).
3. **Episode end conditions:** colony failure (extinction / no water production for N days / unprogressable) or a fixed cycle horizon. On failure the save is deleted; the episode is scored either way.
4. **Watchdog (external PowerShell):** relaunches the game after the mod calls the game's exit API, keeping the loop alive unattended.
5. **Improvement loop, two tiers:**
   - **Autonomous (overnight):** all strategy heuristics (goal thresholds, radii, worker counts, priorities, storage modes) externalized into a tunable parameter file; a simple optimizer mutates parameters between episodes and keeps what scores better — parameter search, fully unattended.
   - **Assisted (next session):** Claude reviews the episode logs and writes actual code/logic improvements — the deeper "spotted an improvement, write it" tier.

## Roadmap

1. **v0.1 (done):** skeleton mod, DI + tick loop verified, research corpus.
2. **v0.2 Sense:** WorldModel — read stocks, population, weather, water sim, science, achievements; JSON status endpoint piggybacking the game's built-in HTTP API (`localhost:8080`) for a live dashboard.
3. **v0.3 Act primitives:** place building/path, zone crops/trees, set priorities, pause/unpause, buy science unlocks, set game speed (unclamped — `SpeedManager.ChangeSpeed` takes any float).
4. **v0.4 Opening book:** days 1–20 build order (pump → lumberjacks → gatherer → farm → forester → inventor → mill → dam) with site selection on arbitrary maps.
5. **v0.5 Water engineer:** dam/reservoir/bypass planning, drought/badtide survival loop.
6. **v0.6 Full economy:** industry ratios, wellbeing ladder, districts, bots, wonder.
   - Pending refinements: stairs orientation verification, waterside site *scoring* (prefer the closest/least-travel river tile — current pick can land far from town when a nearer upstream bank exists), trunk-road layout planning to minimize future path rework.
7. **v0.7 Achievement campaign:** multi-run scheduler for the full 100%.
8. **v0.8 Interactive prompts:** in-game/remote decision prompts ("target X next?", removal recommendations) — suggestions channel exists, UI pending.
9. **v0.9 Brain Training mode:** the self-play loop above, plus a systematic deep-dive into the game's mechanics/gates (decompiled source) to derive optimal routes rather than heuristics.

## Prior art worth borrowing from

- **ihsoft "Advanced Automation"** — IFTTT + scripting engine for in-game rules ([github.com/ihsoft/TimberbornMods](https://github.com/ihsoft/TimberbornMods))
- **Timberbot** — HTTP API mod (port 8085) built for LLM-driven play: validated placement, A* path building, ASCII maps ([github.com/abix-/TimberbornMods](https://github.com/abix-/TimberbornMods))
- **datvm's mods + modding guide** — Building Blueprints, Configurable Game Speed ([datvm.github.io/TimberbornMods/ModdingGuide](https://datvm.github.io/TimberbornMods/ModdingGuide/))
- **Official modding docs** — [github.com/mechanistry/timberborn-modding](https://github.com/mechanistry/timberborn-modding)
